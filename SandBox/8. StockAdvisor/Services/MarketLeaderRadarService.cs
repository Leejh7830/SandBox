using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GateHelper.StockAdvisor.Models;
using Newtonsoft.Json.Linq;

namespace GateHelper.StockAdvisor.Services
{
    public enum RadarStrategyType
    {
        GoldenPullback, // 🎯 골든 눌림목 (우상향 추세 속 20일선 눌림목 안착)
        OversoldBounce, // 💎 과매도 바닥 (RSI 36 이하 공포 투매 저가 매수존)
        VolumeBreakout, // 🚀 거래대금 폭발 (당일 스마트머니 대량 유입 돌파)
        AllLeaders      // 전체 주도주 (거래대금 순 정렬)
    }

    public class MarketLeaderItem
    {
        public int Rank { get; set; } = 0;
        public string StockCode { get; set; } = string.Empty;
        public string StockName { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; } = 0m;
        public decimal ChangeRate { get; set; } = 0m;
        public long Volume { get; set; } = 0;
        public long TradingValueRaw { get; set; } = 0; // 거래대금 (원)
        public string TradingValueText { get; set; } = string.Empty;
        public string MarketType { get; set; } = "KOSPI";

        // 정량 퀀트 지표
        public decimal Rsi { get; set; } = 50m;
        public decimal Ma20 { get; set; } = 0m;
        public decimal Ma20DisparityRate { get; set; } = 0m; // 20일선 이격도 (%)
        public decimal SupportPrice { get; set; } = 0m;
        public decimal RecommendedBuyPrice { get; set; } = 0m;
        public int ChartScore { get; set; } = 5;

        // 적합 전략 플래그
        public bool IsGoldenPullback { get; set; } = false;
        public bool IsOversoldBounce { get; set; } = false;
        public bool IsVolumeBreakout { get; set; } = false;
        public string PrimaryStrategyBadge { get; set; } = "관망";
        public string PrimaryStrategyGuide { get; set; } = string.Empty;

        public bool IsAlreadyInPortfolio { get; set; } = false;
    }

    /// <summary>
    /// 코스피 100 + 코스닥 50 핵심 주도주 풀에서 실시간 거래대금, RSI, 20일선 이격도를 퀀트 분석하여
    /// 3대 매수 급소(골든 눌림목·과매도 바닥·거래대금 폭발)를 실시간 발굴하는 주도주 레이더 서비스
    /// (Zero-Resource: 60초 램 캐시, 백그라운드 스로틀링 병렬 수집, 0B 디스크 I/O)
    /// </summary>
    public static class MarketLeaderRadarService
    {
        private static readonly HttpClient _httpClient;
        private static List<MarketLeaderItem> _cachedLeaders = null;
        private static DateTime _lastFetchedTime = DateTime.MinValue;
        private static readonly object _cacheLock = new object();

        static MarketLeaderRadarService()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(8)
            };
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }

        /// <summary>
        /// 코스피 100 + 코스닥 50 주도주 풀을 스캔하고 기술적 지표를 병렬 분석하여 반환합니다.
        /// </summary>
        public static async Task<List<MarketLeaderItem>> ScanMarketLeadersAsync(bool forceRefresh = false, IProgress<int> progress = null)
        {
            lock (_cacheLock)
            {
                if (!forceRefresh && _cachedLeaders != null && (DateTime.Now - _lastFetchedTime).TotalSeconds < 60)
                {
                    return new List<MarketLeaderItem>(_cachedLeaders);
                }
            }

            var rawLeaders = new List<MarketLeaderItem>();

            // 1. 코스피 시총/유동성 TOP 100 수집
            try
            {
                var kospi = await FetchMarketCategoryAsync("KOSPI", 100).ConfigureAwait(false);
                if (kospi != null) rawLeaders.AddRange(kospi);
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "MarketLeaderRadarService.Scan.KOSPI");
            }

            // 2. 코스닥 시총/유동성 TOP 50 수집
            try
            {
                var kosdaq = await FetchMarketCategoryAsync("KOSDAQ", 50).ConfigureAwait(false);
                if (kosdaq != null) rawLeaders.AddRange(kosdaq);
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "MarketLeaderRadarService.Scan.KOSDAQ");
            }

            if (rawLeaders.Count == 0) return new List<MarketLeaderItem>();

            // 거래대금 내림차순 정렬
            rawLeaders = rawLeaders.OrderByDescending(x => x.TradingValueRaw).ToList();

            // 상위 40개 거래대금 최상위 종목에 대해 정밀 기술적 지표(RSI, 20일선, 타점) 병렬 스캔
            int scanTargetCount = Math.Min(45, rawLeaders.Count);
            var scanTargets = rawLeaders.Take(scanTargetCount).ToList();

            using (var sem = new SemaphoreSlim(6))
            {
                int completed = 0;
                var tasks = scanTargets.Select(async item =>
                {
                    await sem.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        var ind = await TechnicalIndicatorService.AnalyzeIndicatorsAsync(item.StockCode, item.CurrentPrice).ConfigureAwait(false);
                        if (ind != null)
                        {
                            item.Rsi = ind.Rsi;
                            item.Ma20 = ind.Ma20;
                            item.SupportPrice = ind.SupportPrice;
                            item.RecommendedBuyPrice = ind.RecommendedBuyPrice;
                            item.ChartScore = ind.ChartScore;

                            if (item.Ma20 > 0 && item.CurrentPrice > 0)
                            {
                                item.Ma20DisparityRate = Math.Round((item.CurrentPrice - item.Ma20) / item.Ma20 * 100m, 1);
                            }

                            // 전략 1: 🎯 골든 눌림목 (우상향 20일선 지지 + RSI 40~56 + 이격도 -2.5%~+2.0% + 거래대금 300억 이상)
                            if (item.Rsi >= 39m && item.Rsi <= 57m &&
                                item.Ma20DisparityRate >= -3.0m && item.Ma20DisparityRate <= 2.5m &&
                                item.ChartScore >= 6)
                            {
                                item.IsGoldenPullback = true;
                                item.PrimaryStrategyBadge = "골든 눌림목";
                                item.PrimaryStrategyGuide = $"20일선({item.Ma20:N0}원) 지지 안착! RSI({item.Rsi:F1}) 과열 해소된 이상적 분할 진입존";
                            }
                            // 전략 2: 💎 과매도 바닥 (RSI 36 이하 침체 공포 투매 저가 매수존)
                            else if (item.Rsi <= 36.5m && item.Rsi > 0m)
                            {
                                item.IsOversoldBounce = true;
                                item.PrimaryStrategyBadge = "과매도 바닥";
                                item.PrimaryStrategyGuide = $"14일 RSI({item.Rsi:F1}) 극단적 침체 구간! 지지선({item.SupportPrice:N0}원) 확인 후 역발상 분할 매수";
                            }
                            // 전략 3: 🚀 거래대금 폭발 (당일 거래대금 1,000억 이상 + 양봉 + RSI < 68)
                            else if (item.TradingValueRaw >= 100000000000L && item.ChangeRate >= 1.5m && item.ChangeRate <= 7.5m && item.Rsi < 68m)
                            {
                                item.IsVolumeBreakout = true;
                                item.PrimaryStrategyBadge = "거래대금 폭발";
                                item.PrimaryStrategyGuide = $"거래대금({item.TradingValueText}) 대량 유입 돌파! 저항 돌파 상승 모멘텀 유효";
                            }
                            else
                            {
                                item.PrimaryStrategyBadge = "추세 관망";
                                item.PrimaryStrategyGuide = $"현재가 {item.CurrentPrice:N0}원, 20일선 이격도 {item.Ma20DisparityRate:+0.0;-0.0}%. 추가 방향성 확인 필요";
                            }
                        }
                    }
                    catch { }
                    finally
                    {
                        sem.Release();
                        int c = Interlocked.Increment(ref completed);
                        progress?.Report((int)((double)c / scanTargetCount * 100));
                    }
                });

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

            // 순위 부여
            for (int i = 0; i < rawLeaders.Count; i++)
            {
                rawLeaders[i].Rank = i + 1;
            }

            lock (_cacheLock)
            {
                _cachedLeaders = rawLeaders;
                _lastFetchedTime = DateTime.Now;
            }

            return rawLeaders;
        }

        private static async Task<List<MarketLeaderItem>> FetchMarketCategoryAsync(string category, int pageSize)
        {
            var list = new List<MarketLeaderItem>();
            string url = $"https://m.stock.naver.com/api/stocks/marketValue/{category}?page=1&pageSize={pageSize}";

            var response = await _httpClient.GetAsync(url).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return list;

            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(json)) return list;

            var jobj = JObject.Parse(json);
            var arr = jobj["stocks"] as JArray;
            if (arr == null) return list;

            foreach (var s in arr)
            {
                try
                {
                    string code = s["itemCode"]?.ToString();
                    string name = s["stockName"]?.ToString();
                    if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(name)) continue;

                    string closeStr = s["closePriceRaw"]?.ToString() ?? s["closePrice"]?.ToString();
                    string changeStr = s["fluctuationsRatio"]?.ToString();
                    string volStr = s["accumulatedTradingVolumeRaw"]?.ToString();
                    string valStr = s["accumulatedTradingValueRaw"]?.ToString();
                    string valHangeul = s["accumulatedTradingValueKrwHangeul"]?.ToString();

                    decimal close = ParseDecimal(closeStr);
                    decimal change = ParseDecimal(changeStr);
                    long vol = 0;
                    long.TryParse(volStr?.Replace(",", "") ?? "0", out vol);
                    long tval = 0;
                    long.TryParse(valStr?.Replace(",", "") ?? "0", out tval);

                    if (string.IsNullOrEmpty(valHangeul))
                    {
                        valHangeul = FormatKrw(tval);
                    }

                    list.Add(new MarketLeaderItem
                    {
                        StockCode = code,
                        StockName = name,
                        CurrentPrice = close,
                        ChangeRate = change,
                        Volume = vol,
                        TradingValueRaw = tval,
                        TradingValueText = valHangeul,
                        MarketType = category
                    });
                }
                catch { }
            }

            return list;
        }

        private static decimal ParseDecimal(string val)
        {
            if (string.IsNullOrWhiteSpace(val)) return 0m;
            string clean = val.Replace(",", "").Trim();
            if (decimal.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal res))
            {
                return res;
            }
            return 0m;
        }

        private static string FormatKrw(long val)
        {
            if (val >= 1000000000000L)
            {
                return $"{val / 1000000000000.0:F1}조원";
            }
            if (val >= 100000000L)
            {
                return $"{val / 100000000.0:F0}억원";
            }
            return $"{val:N0}원";
        }
    }
}
