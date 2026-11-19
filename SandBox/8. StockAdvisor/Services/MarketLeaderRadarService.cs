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
        GoldenPullback, // 🎯 골든 눌림목 (우상향 추세 속 20일선 완만 눌림목 안착)
        OversoldBounce, // 🟢 과매도 반등 (RSI 36.5 이하 지지선 안착 역발상 분할 매수존)
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

        // 정량 퀀트 및 4대 팩터 통합 지표 (종목평가 StockEvaluatorDialog와 100% 동일 기준)
        // [정밀 진단 여부 플래그] 지표 스캔이 완료된 종목만 true (미스캔 종목은 50.0 기본값이 아닌 '-' 미분석 표시)
        public bool IsScanned { get; set; } = false;
        public decimal Rsi { get; set; } = 0m;
        public decimal VolumeRatio { get; set; } = 0m; // 5일 평균 대비 거래량 (%)
        public decimal ProjectedVolumeRatio { get; set; } = 0m; // 장중 경과시간 보정 환산 거래량 (%)
        public decimal Ma20 { get; set; } = 0m;
        public decimal Ma20DisparityRate { get; set; } = 0m; // 20일선 이격도 (%)
        public decimal SupportPrice { get; set; } = 0m;
        public decimal RecommendedBuyPrice { get; set; } = 0m;
        public int ChartScore { get; set; } = 0;
        public int SupplyFinanceScore { get; set; } = 0;
        public int RiskScore { get; set; } = 0;
        public int FinalScore { get; set; } = 0;
        public PositionGrade Grade { get; set; } = PositionGrade.B;
        public bool HasCriticalLandmine { get; set; } = false;
        public bool CanBuy { get; set; } = false;
        public string SupplyBadge { get; set; } = "수급대기";
        public string LandmineBadge { get; set; } = "-";

        // 적합 전략 플래그
        public bool IsGoldenPullback { get; set; } = false;
        public bool IsOversoldBounce { get; set; } = false;
        public bool IsVolumeBreakout { get; set; } = false;
        public string PrimaryStrategyBadge { get; set; } = "-";
        public string PrimaryStrategyGuide { get; set; } = string.Empty;

        public bool IsAlreadyInPortfolio { get; set; } = false;

        /// <summary>
        /// ETF/ETN 여부 (종목명 기준 KODEX, TIGER, ACE, SOL, RISE, HANARO, KBSTAR, KOSEF, PLUS 등)
        /// </summary>
        public bool IsEtf { get; set; } = false;
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

            // 1. 시가총액 상위 (코스피 100 + 코스닥 50) + 당일 거래대금 최상위 (코스피 40 + 코스닥 40) 병렬 수집
            // (중소형 테마 주도주까지 누락 없이 포착 후 종목코드 기준 중복 제거)
            var fetchTasks = new[]
            {
                FetchMarketCategorySafeAsync("marketValue", "KOSPI", 100),
                FetchMarketCategorySafeAsync("marketValue", "KOSDAQ", 50),
                FetchMarketCategorySafeAsync("priceTop", "KOSPI", 40),
                FetchMarketCategorySafeAsync("priceTop", "KOSDAQ", 40)
            };
            var fetchedGroups = await Task.WhenAll(fetchTasks).ConfigureAwait(false);
            var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var grp in fetchedGroups)
            {
                if (grp == null) continue;
                foreach (var itm in grp)
                {
                    if (seenCodes.Add(itm.StockCode))
                    {
                        rawLeaders.Add(itm);
                    }
                }
            }

            if (rawLeaders.Count == 0) return new List<MarketLeaderItem>();

            // 거래대금 내림차순 정렬
            rawLeaders = rawLeaders.OrderByDescending(x => x.TradingValueRaw).ToList();

            // 거래대금 최상위 종목 상위 85개 정밀 분석
            // (기술적 지표 + 외인·기관 수급 + 재무·지뢰 4대 팩터, 세마포어 병렬 스캔)
            int scanTargetCount = Math.Min(85, rawLeaders.Count);
            var scanTargets = rawLeaders.Take(scanTargetCount).ToList();

            using (var sem = new SemaphoreSlim(12))
            {
                int completed = 0;
                var tasks = scanTargets.Select(async item =>
                {
                    await sem.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        var indTask = TechnicalIndicatorService.AnalyzeIndicatorsAsync(item.StockCode, item.CurrentPrice);
                        var intelTask = StockIntelligenceService.AnalyzeAsync(item.StockCode);
                        await Task.WhenAll(indTask, intelTask).ConfigureAwait(false);

                        var ind = indTask.Result;
                        var intel = intelTask.Result;

                        if (ind != null && intel != null)
                        {
                            var unified = StockIntelligenceService.CalculateUnifiedScores(ind, intel, 0);

                            var tempItem = new StockHoldingItem
                            {
                                StockCode = item.StockCode,
                                StockName = item.StockName,
                                CurrentPrice = item.CurrentPrice,
                                ChangeRate = item.ChangeRate,
                                Quantity = 0,
                                ChartScore = unified.ChartScore,
                                SupplyFinanceScore = unified.SupplyFinanceScore,
                                NewsScore = unified.NewsScore,
                                RiskScore = unified.RiskScore,
                                FinalScore = unified.FinalScore,
                                Rsi = ind.Rsi,
                                SupportPrice = ind.SupportPrice,
                                TargetStopPrice = ind.TargetStopPrice,
                                RecommendedBuyPrice = ind.RecommendedBuyPrice,
                                SecondaryBuyPrice = ind.SecondaryBuyPrice,
                                TimingBadge = ind.TimingBadge,
                                HasCriticalLandmine = intel.HasCriticalLandmine
                            };
                            var verdict = StockVerdictHelper.Evaluate(tempItem);

                            item.IsScanned = true;
                            item.Rsi = ind.Rsi;
                            item.VolumeRatio = ind.VolumeRatio;
                            item.ProjectedVolumeRatio = ind.ProjectedVolumeRatio;
                            item.Ma20 = ind.Ma20;
                            item.SupportPrice = ind.SupportPrice;
                            item.ChartScore = unified.ChartScore;
                            item.SupplyFinanceScore = unified.SupplyFinanceScore;
                            item.RiskScore = unified.RiskScore;
                            item.FinalScore = unified.FinalScore;
                            item.Grade = unified.Grade;
                            item.HasCriticalLandmine = intel.HasCriticalLandmine;
                            item.CanBuy = verdict.CanBuy;
                            item.SupplyBadge = intel.SupplyBadge;
                            item.LandmineBadge = intel.LandmineBadge;

                            if (item.Ma20 > 0 && item.CurrentPrice > 0)
                            {
                                item.Ma20DisparityRate = Math.Round((item.CurrentPrice - item.Ma20) / item.Ma20 * 100m, 1);
                            }

                            // [단일 기준 안전 인터락 및 3대 전략 정밀 분류]
                            // 1. 당일 급락(-4.0% 초과) 종목은 지지선 붕괴 위험(떨어지는 칼날)으로 레이더 추천 제외 및 급락주의 경보
                            if (item.ChangeRate <= -4.0m)
                            {
                                item.RecommendedBuyPrice = 0m;
                                item.PrimaryStrategyBadge = $"급락주의({item.FinalScore}점)";
                                item.PrimaryStrategyGuide = $"당일 급락({item.ChangeRate:+0.00;-0.00}%) 발생! 떨어지는 칼날 진입 금지 및 지지선 안착 확인 대기";
                                item.CanBuy = false;
                            }
                            // 2. 치명적 지뢰(적자/CB 등)가 있거나 종합 4점 이하(F·하위 C등급)는 매수금지, 지뢰 없는 5점 중립은 관망대기
                            else if (item.HasCriticalLandmine || item.FinalScore <= 4 || item.Grade == PositionGrade.F)
                            {
                                item.RecommendedBuyPrice = 0m;
                                item.PrimaryStrategyBadge = $"매수금지({item.FinalScore}점)";
                                item.PrimaryStrategyGuide = $"종합 {item.FinalScore}점({item.Grade}등급) · {item.SupplyBadge}/{item.LandmineBadge} — 수급·재무 기준 미달로 매수 제외";
                                item.CanBuy = false;
                            }
                            else if (item.FinalScore <= 5 || item.Grade == PositionGrade.C)
                            {
                                item.RecommendedBuyPrice = 0m;
                                item.PrimaryStrategyBadge = $"관망대기({item.FinalScore}점)";
                                item.PrimaryStrategyGuide = $"종합 {item.FinalScore}점({item.Grade}등급, 중립) · 방향성 탐색 중으로 추가 점수 개선 확인 전까지 관망";
                                item.CanBuy = false;
                            }
                            // 3. 종합 6점 이상(B등급 이상) & 지뢰 없는 우량 주도주군 대상 3대 핵심 전략 포착
                            else
                            {
                                item.RecommendedBuyPrice = ind.RecommendedBuyPrice;

                                // [독립 전략 플래그 판정] — 복합 신호(예: 골든 눌림목 + 거래대금 폭발)도 모든 해당 필터 탭에 누락 없이 노출
                                bool isGolden = item.ChangeRate > -4.0m &&
                                                item.Rsi > 36.5m && item.Rsi <= 60.0m &&
                                                item.Ma20DisparityRate >= -4.5m && item.Ma20DisparityRate <= 3.5m &&
                                                item.ChartScore >= 6;

                                bool isOversold = item.Rsi <= 36.5m && item.Rsi > 0m &&
                                                  item.RiskScore >= 7 && item.ChangeRate > -4.0m;

                                bool isVolBreak = item.TradingValueRaw >= 50000000000L &&
                                                  (item.VolumeRatio >= 120m || item.ProjectedVolumeRatio >= 140m) &&
                                                  item.ChangeRate >= 1.0m && item.ChangeRate <= 8.5m && item.Rsi < 68m;

                                item.IsGoldenPullback = isGolden;
                                item.IsOversoldBounce = isOversold;
                                item.IsVolumeBreakout = isVolBreak;

                                string volInfo = item.VolumeRatio >= 120m
                                    ? $"5일 평균 대비 {item.VolumeRatio:F0}%"
                                    : $"현재 {item.VolumeRatio:F0}%·장마감 환산 {item.ProjectedVolumeRatio:F0}%";

                                // 대표 전략 배지 및 가이드 (복합 신호 우선 표기)
                                if (isGolden && isVolBreak)
                                {
                                    item.PrimaryStrategyBadge = $"골든 눌림목+폭발({item.FinalScore}점)";
                                    item.PrimaryStrategyGuide = $"종합 {item.FinalScore}점({item.Grade}등급) · 20일선({item.Ma20:N0}원) 지지 + 거래대금({item.TradingValueText}, {volInfo}) 동시 폭발! 최상급 진입존";
                                }
                                else if (isGolden)
                                {
                                    item.PrimaryStrategyBadge = $"골든 눌림목({item.FinalScore}점)";
                                    item.PrimaryStrategyGuide = $"종합 {item.FinalScore}점({item.Grade}등급) · 20일선({item.Ma20:N0}원) 부근 지지 안착! 이상적 분할 진입존";
                                }
                                else if (isOversold)
                                {
                                    item.PrimaryStrategyBadge = $"과매도 반등({item.FinalScore}점)";
                                    item.PrimaryStrategyGuide = $"종합 {item.FinalScore}점({item.Grade}등급) · RSI({item.Rsi:F1}) 침체 및 지지선({item.SupportPrice:N0}원) 안착 확인된 역발상 분할 매수존";
                                }
                                else if (isVolBreak)
                                {
                                    item.PrimaryStrategyBadge = $"거래대금 폭발({item.FinalScore}점)";
                                    item.PrimaryStrategyGuide = $"종합 {item.FinalScore}점({item.Grade}등급) · 거래대금({item.TradingValueText}, {volInfo}) 대량 유입 돌파! 상승 모멘텀 유효";
                                }
                                else if (verdict.CanBuy)
                                {
                                    item.PrimaryStrategyBadge = $"분할매수({item.FinalScore}점)";
                                    item.PrimaryStrategyGuide = $"종합 {item.FinalScore}점({item.Grade}등급) 우량 · 1차 매수가({item.RecommendedBuyPrice:N0}원) 부근 분할 진입 유효";
                                }
                                else
                                {
                                    item.PrimaryStrategyBadge = $"관망대기({item.FinalScore}점)";
                                    item.PrimaryStrategyGuide = $"종합 {item.FinalScore}점({item.Grade}등급) · {verdict.GuideText}";
                                }
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

        private static async Task<List<MarketLeaderItem>> FetchMarketCategorySafeAsync(string sortType, string category, int pageSize)
        {
            try
            {
                return await FetchMarketCategoryAsync(sortType, category, pageSize).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, $"MarketLeaderRadarService.Scan.{sortType}.{category}");
                return new List<MarketLeaderItem>();
            }
        }

        private static async Task<List<MarketLeaderItem>> FetchMarketCategoryAsync(string sortType, string category, int pageSize)
        {
            var list = new List<MarketLeaderItem>();
            string url = $"https://m.stock.naver.com/api/stocks/{sortType}/{category}?page=1&pageSize={pageSize}";

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

                    bool isEtf = IsEtfStock(name);

                    list.Add(new MarketLeaderItem
                    {
                        StockCode = code,
                        StockName = name,
                        CurrentPrice = close,
                        ChangeRate = change,
                        Volume = vol,
                        TradingValueRaw = tval,
                        TradingValueText = valHangeul,
                        MarketType = category,
                        IsEtf = isEtf
                    });
                }
                catch { }
            }

            return list;
        }

        public static bool IsEtfStock(string stockName)
        {
            if (string.IsNullOrWhiteSpace(stockName)) return false;
            string upper = stockName.ToUpperInvariant();
            return upper.Contains("KODEX") ||
                   upper.Contains("TIGER") ||
                   upper.Contains("ACE") ||
                   upper.Contains("SOL") ||
                   upper.Contains("RISE") ||
                   upper.Contains("HANARO") ||
                   upper.Contains("KBSTAR") ||
                   upper.Contains("KOSEF") ||
                   upper.Contains("PLUS") ||
                   upper.Contains("TIMEFOLIO") ||
                   upper.Contains("ARIRANG") ||
                   upper.Contains("WOORI") ||
                   upper.Contains("UNICORN") ||
                   upper.EndsWith(" ETF") ||
                   upper.EndsWith(" ETN");
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

