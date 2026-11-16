using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using GateHelper.StockAdvisor.Models;
using Newtonsoft.Json.Linq;

namespace GateHelper.StockAdvisor.Services
{
    /// <summary>
    /// 네이버 증권 20일 일봉 데이터를 기반으로 RSI(14일), 거래량 급증률, 이평선, 지지선 및 손익비를 계산하는 팩트 지표 엔진
    /// </summary>
    public static class TechnicalIndicatorService
    {
        private static readonly HttpClient _httpClient;

        static TechnicalIndicatorService()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            };
            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(8)
            };
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
        }

        public class IndicatorAnalysisResult
        {
            public decimal Rsi { get; set; } = 50m;
            public decimal VolumeRatio { get; set; } = 100m;
            public decimal Ma5 { get; set; } = 0m;
            public decimal Ma20 { get; set; } = 0m;
            public decimal Ma60 { get; set; } = 0m;
            public decimal SupportPrice { get; set; } = 0m;
            public decimal ResistancePrice { get; set; } = 0m;
            public decimal DownsideRiskPercent { get; set; } = 0m;
            public int ChartScore { get; set; } = 5;
            public int RiskScore { get; set; } = 5;
            public string TimingBadge { get; set; } = "관망유지";
            public string TimingWarning { get; set; } = string.Empty;

            // 20일 일봉 캔들 데이터 목록 (미니 차트 시각화용)
            public List<DailyCandle> Candles { get; set; } = new List<DailyCandle>();

            // AI 퀀트 권고 매수가 및 진입 타점
            public decimal RecommendedBuyPrice { get; set; } = 0m;
            public decimal SecondaryBuyPrice { get; set; } = 0m;
            public decimal TargetStopPrice { get; set; } = 0m;
            public string EntryTimingBadge { get; set; } = "관망대기";
            public string EntryTimingGuide { get; set; } = string.Empty;

            // AI 퀀트 권고 매도(익절) 타점 및 청산 시나리오
            public decimal RecommendedTargetPrice { get; set; } = 0m;
            public decimal SecondaryTargetPrice { get; set; } = 0m;
            public string ExitTimingBadge { get; set; } = "추세보유";
            public string ExitTimingGuide { get; set; } = string.Empty;
        }

        /// <summary>
        /// 특정 종목의 최근 20일 일봉을 수집하여 객관적 지표와 점수를 산출합니다.
        /// </summary>
        public static async Task<IndicatorAnalysisResult> AnalyzeIndicatorsAsync(string stockCode, decimal currentPrice)
        {
            var result = new IndicatorAnalysisResult();
            try
            {
                var candles = await Fetch20DailyCandlesAsync(stockCode);
                result.Candles = candles ?? new List<DailyCandle>();
                if (candles == null || candles.Count < 5)
                {
                    // 데이터가 부족하면 기본 안전값 유지
                    result.SupportPrice = currentPrice * 0.95m;
                    result.ResistancePrice = currentPrice * 1.05m;
                    result.DownsideRiskPercent = 5.0m;
                    return result;
                }

                // 1. 지지선(최근 20일 최저가) & 저항선(최고가)
                decimal minLow = decimal.MaxValue;
                decimal maxHigh = decimal.MinValue;
                int scan20 = Math.Min(20, candles.Count);
                for (int i = 0; i < scan20; i++)
                {
                    var c = candles[i];
                    if (c.Low > 0 && c.Low < minLow) minLow = c.Low;
                    if (c.High > maxHigh) maxHigh = c.High;
                }
                if (minLow == decimal.MaxValue) minLow = currentPrice * 0.95m;
                if (maxHigh == decimal.MinValue) maxHigh = currentPrice * 1.05m;

                result.SupportPrice = minLow;
                result.ResistancePrice = maxHigh;

                if (currentPrice > 0 && currentPrice >= minLow)
                {
                    result.DownsideRiskPercent = Math.Round((currentPrice - minLow) / currentPrice * 100m, 1);
                }
                else
                {
                    result.DownsideRiskPercent = 3.0m;
                }

                // 2. 이평선 (MA5, MA20, MA60)
                decimal sum5 = 0m;
                int count5 = Math.Min(5, candles.Count);
                for (int i = 0; i < count5; i++) sum5 += candles[i].Close;
                result.Ma5 = count5 > 0 ? Math.Round(sum5 / count5, 2) : 0m;

                decimal sum20 = 0m;
                int count20 = Math.Min(20, candles.Count);
                for (int i = 0; i < count20; i++) sum20 += candles[i].Close;
                result.Ma20 = count20 > 0 ? Math.Round(sum20 / count20, 2) : 0m;

                decimal sum60 = 0m;
                int count60 = Math.Min(60, candles.Count);
                for (int i = 0; i < count60; i++) sum60 += candles[i].Close;
                result.Ma60 = count60 > 0 ? Math.Round(sum60 / count60, 2) : 0m;

                // 3. 거래량 급증률 (당일 vs 직전 5영업일 평균)
                if (candles.Count >= 2 && candles[0].Volume > 0)
                {
                    long prevSum = 0;
                    int prevCount = 0;
                    for (int i = 1; i < Math.Min(6, candles.Count); i++)
                    {
                        if (candles[i].Volume > 0)
                        {
                            prevSum += candles[i].Volume;
                            prevCount++;
                        }
                    }
                    if (prevCount > 0 && prevSum > 0)
                    {
                        decimal avgVol = (decimal)prevSum / prevCount;
                        result.VolumeRatio = Math.Round(((decimal)candles[0].Volume / avgVol) * 100m, 1);
                    }
                }

                // 4. 14일 RSI 계산 (시간 순 정렬 필요: 과거 -> 현재)
                var sortedAsc = new List<DailyCandle>(candles);
                sortedAsc.Reverse(); // 과거가 인덱스 0

                if (sortedAsc.Count >= 14)
                {
                    decimal gainSum = 0m;
                    decimal lossSum = 0m;
                    int period = Math.Min(14, sortedAsc.Count - 1);

                    int startIdx = sortedAsc.Count - period;
                    for (int i = startIdx; i < sortedAsc.Count; i++)
                    {
                        decimal diff = sortedAsc[i].Close - sortedAsc[i - 1].Close;
                        if (diff > 0) gainSum += diff;
                        else if (diff < 0) lossSum += (-diff);
                    }

                    decimal avgGain = gainSum / period;
                    decimal avgLoss = lossSum / period;

                    if (avgLoss == 0m)
                    {
                        result.Rsi = 100m;
                    }
                    else
                    {
                        decimal rs = avgGain / avgLoss;
                        result.Rsi = Math.Round(100m - (100m / (1m + rs)), 1);
                    }
                }

                // 5. 차트 점수 (1~10점) 정밀 계산
                double cScore = 5.0;

                // 정배열 및 단기 상승 추세
                if (currentPrice > result.Ma5) cScore += 1.5;
                if (result.Ma5 > result.Ma20) cScore += 1.5;

                // 거래량 실린 상승
                if (result.VolumeRatio >= 130m && candles[0].Close >= candles[0].Open) cScore += 1.5;
                else if (result.VolumeRatio >= 100m) cScore += 0.5;

                // 건강한 RSI (45~65)
                if (result.Rsi >= 45m && result.Rsi <= 65m) cScore += 1.0;

                // [중기 역배열 및 하락 추세 감점] 60일선(Ma60) 하회 시 하락 추세로 감점
                bool isUnderMa60 = result.Ma60 > 0m && currentPrice < result.Ma60;
                bool isDeadCross60 = result.Ma60 > 0m && result.Ma20 < result.Ma60; // 20일선이 60일선 아래 (역배열)
                if (isDeadCross60)
                {
                    cScore -= 2.0; // 중기 역배열 강력 감점
                }
                else if (isUnderMa60)
                {
                    cScore -= 1.0;
                }

                // 역배열 하락 추세 감점 (단, 20일 지지선 근접 과매도 반등 구간은 지지 테스트로 인정하여 과도한 감점 방지)
                if (currentPrice < result.Ma5 && result.Ma5 < result.Ma20)
                {
                    if (result.Rsi > 0m && result.Rsi <= 36.5m && result.DownsideRiskPercent <= 4.5m)
                        cScore = Math.Max(cScore, 5.5);
                    else
                        cScore -= 2.5;
                }
                else if (result.Rsi > 0m && result.Rsi <= 36.5m && result.DownsideRiskPercent <= 4.5m)
                {
                    cScore = Math.Max(cScore, 5.5);
                }

                // [역배열 차트 점수 캡 인터락] 20일선 < 60일선 완전 역배열이면서 현재가가 60일선 아래인 경우 최대 5점 캡
                if (isDeadCross60 && isUnderMa60)
                {
                    cScore = Math.Min(cScore, 5.0);
                }

                result.ChartScore = (int)Math.Max(1, Math.Min(10, Math.Round(cScore)));

                // 6. 손익비/안전 점수 (1~10점) 초기 계산 (하방 리스크 기반)
                // [신저가 왜곡 방지 인터락] 현재가가 20일 최저가를 계속 깨며 하락 중인 역배열 종목은 손익비 만점 왜곡 방지
                bool isBreakingNewLow = currentPrice <= minLow * 1.005m && (result.Ma5 < result.Ma20 || currentPrice < result.Ma5);
                if (isBreakingNewLow)
                {
                    // 바닥 이탈 하락 지속 구간: 손익비 3점 이하로 제한하여 매수 추천 방지
                    result.RiskScore = 3;
                }
                else if (result.DownsideRiskPercent <= 3.0m) result.RiskScore = 9;
                else if (result.DownsideRiskPercent <= 4.5m) result.RiskScore = 8;
                else if (result.DownsideRiskPercent <= 6.0m) result.RiskScore = 7;
                else if (result.DownsideRiskPercent <= 8.0m) result.RiskScore = 5;
                else if (result.DownsideRiskPercent <= 11.0m) result.RiskScore = 3;
                else result.RiskScore = 2;

                // 7. 타이밍 안전벨트 뱃지 & 경고 판정 (주도주 레이더와 100% 동일한 단일 기준 일치화)
                decimal ma20Disp = result.Ma20 > 0 ? ((currentPrice - result.Ma20) / result.Ma20 * 100m) : 0m;

                if (result.Rsi >= 70.0m)
                {
                    result.TimingBadge = "과열(추격금지)";
                    result.TimingWarning = $"[단기 과열] RSI({result.Rsi:F1}) 과열 구간 진입! 신규 매수 금지, 20일선 눌림목까지 대기 권고";
                }
                else if (result.Rsi <= 36.5m && result.Rsi > 0m)
                {
                    result.TimingBadge = "과매도 반등";
                    result.TimingWarning = $"[과매도 반등] RSI({result.Rsi:F1}) 침체권. 20일 지지선 확인 후 역발상 분할 매수 유효";
                }
                else if (result.ChartScore >= 6 && ma20Disp >= -4.5m && ma20Disp <= 3.5m && result.Rsi >= 36.5m && result.Rsi <= 60m)
                {
                    // 주도주 레이더와 동일한 유연한 '골든 눌림목' 조건 (차트 6점 이상 + 20일선 이격 -4.5%~+3.5% 안착)
                    result.TimingBadge = "골든 눌림목";
                    result.TimingWarning = $"[최적 진입] 20일선({result.Ma20:N0}원, 이격 {ma20Disp:+0.0;-0.0}%) 지지선 안착된 골든 눌림목 매수 구간";
                }
                else if (result.ChartScore >= 6)
                {
                    result.TimingBadge = "눌림목 대기";
                    result.TimingWarning = $"[눌림목 대기] 우상향 추세이나 20일선 이격 존재({result.DownsideRiskPercent:F1}%). 20일선 눌림목까지 대기 권고";
                }
                else if (result.ChartScore <= 3)
                {
                    result.TimingBadge = "비중축소";
                    result.TimingWarning = $"[비중 축소] 역배열 추세 이탈 및 하방 리스크 확대. 반등 시 비중 축소 권고";
                }
                else
                {
                    result.TimingBadge = "관망";
                    result.TimingWarning = $"[관망 대기] 뚜렷한 추세 모멘텀 형성 전까지 시장 관망";
                }

                // 8. AI 퀀트 권고 매수가 및 진입 타점 계산
                // [단일 진실 공급원(SSOT)] 손절가는 지지선(최근 20일 최저가) 기준 -3% 단일 고정 (KRX 호가 단위 정돈)
                decimal stopBase = result.SupportPrice > 0 ? result.SupportPrice : currentPrice * 0.95m;
                result.TargetStopPrice = RoundToKrxTick(stopBase * 0.97m, roundDown: true); // 지지선 -3% 이탈 시 손절선

                if (result.Rsi >= 68.0m || (result.Ma20 > 0 && currentPrice > result.Ma20 * 1.12m))
                {
                    decimal pullbackPrice = result.Ma20 > 0 ? result.Ma20 : currentPrice * 0.93m;
                    // 과열 시 1차 매수가도 절대 현재가보다 높을 수 없음
                    pullbackPrice = Math.Min(pullbackPrice, currentPrice * 0.96m);
                    result.RecommendedBuyPrice = RoundToKrxTick(pullbackPrice, roundDown: true);
                    result.EntryTimingBadge = "추격매수 금지 (대기)";
                    result.EntryTimingGuide = $"현재가 과열권! 지금 사지 말고 20일선 눌림목({result.RecommendedBuyPrice:N0}원) 도달 시 진입 권고";
                }
                else if (result.Rsi <= 36.5m && result.Rsi > 0m && result.ChartScore >= 6)
                {
                    // 과매도 반등: 현재가가 이미 지지선 부근이거나 침체권이므로 현재가 또는 지지선 안착가를 1차 매수가로 부여 (절대 현재가 초과 금지)
                    decimal entryBase = (result.SupportPrice > 0 && result.SupportPrice <= currentPrice)
                        ? result.SupportPrice
                        : currentPrice;
                    result.RecommendedBuyPrice = RoundToKrxTick(Math.Min(entryBase, currentPrice), roundDown: true);
                    result.EntryTimingBadge = "과매도 분할매수존";
                    result.EntryTimingGuide = $"RSI({result.Rsi:F1}) 침체 및 지지선 안착 구간! 권고 매수가({result.RecommendedBuyPrice:N0}원) 분할 진입 유효 (방어선: {result.TargetStopPrice:N0}원)";
                }
                else
                {
                    // 일반 눌림목: 20일선 및 지지선 중간선 (단, 역배열이거나 지지선 이탈 시 1차 매수가가 현재가를 넘지 못하도록 엄격히 상한선 제한)
                    decimal safeEntry = (result.Ma20 > 0 && result.SupportPrice > 0)
                        ? (result.Ma20 + result.SupportPrice) / 2m
                        : currentPrice * 0.97m;

                    // [철벽 인터락] 1차 매수가는 신규 진입/추매 시점의 '현재가'보다 비쌀 수 없음!
                    if (safeEntry > currentPrice)
                    {
                        safeEntry = currentPrice;
                    }

                    result.RecommendedBuyPrice = RoundToKrxTick(safeEntry, roundDown: true);
                    result.EntryTimingBadge = "눌림목 분할 진입존";
                    result.EntryTimingGuide = $"20일 지지선 부근({result.RecommendedBuyPrice:N0}원) 도달 시 분할 진입 유효 (방어선: {result.TargetStopPrice:N0}원)";
                }

                // [철벽 가격 계단 인터락] 권고 매수가는 어떠한 경우에도 손절가보다 높아야 함!
                if (result.RecommendedBuyPrice <= result.TargetStopPrice)
                {
                    result.RecommendedBuyPrice = RoundToKrxTick(result.TargetStopPrice * 1.02m, roundDown: false);
                }

                // 9. AI 퀀트 권고 매도(익절) 타점 및 청산 시나리오 계산
                // [닻(Anchor) 고정 원칙 & 래칫(Ratchet) 상향 락: 장중 틱/순간 펌핑 및 경계선 핑퐁 왜곡 원천 차단]
                // 익절가는 객관적인 20일 일봉 최고 매물대(ResistancePrice)에 닻을 내리고 고정합니다.
                decimal baseTarget;
                if (result.ResistancePrice > 0m)
                {
                    // 저항선 1.8% 선제 익절 안전 버퍼
                    baseTarget = result.ResistancePrice * 0.982m;
                }
                else
                {
                    baseTarget = currentPrice * 1.08m;
                }

                // [돌파 히스테리시스 필터]
                // 현재가가 저항선을 완전히 돌파 안착(저항선 +0.8% 이상 돌파)했을 때만 상향 목표 적용
                // 단순 경계선 틱 흔들림으로 161,500원 <-> 168,000원이 번갈아 스위칭되는 핑퐁 현상을 원천 방지
                if (currentPrice >= baseTarget * 1.008m)
                {
                    baseTarget = Math.Max(baseTarget, currentPrice * 1.05m);
                }

                result.RecommendedTargetPrice = RoundToKrxTick(baseTarget, roundDown: true);
                result.SecondaryTargetPrice = RoundToKrxTick(result.RecommendedTargetPrice * 1.09m, roundDown: true);

                decimal upside1 = currentPrice > 0 ? ((result.RecommendedTargetPrice - currentPrice) / currentPrice * 100m) : 0m;

                // [손익비(Risk-Reward Ratio) 최종 정밀 보정 인터락]
                // 목표가까지 먹을 여력(upside1) 대비 손절가까지의 위험(DownsideRiskPercent)을 비교하여 왜곡 방지
                // 손익비 R/R = 상승여력% / 하방위험%
                decimal stopLossRiskPercent = currentPrice > 0 && result.TargetStopPrice > 0 && currentPrice > result.TargetStopPrice
                    ? ((currentPrice - result.TargetStopPrice) / currentPrice * 100m)
                    : result.DownsideRiskPercent;
                if (stopLossRiskPercent <= 0m) stopLossRiskPercent = 3.0m;

                decimal riskRewardRatio = stopLossRiskPercent > 0 ? (upside1 / stopLossRiskPercent) : 1.0m;

                if (riskRewardRatio < 0.8m)
                {
                    // 먹을 여력(+3.7%)보다 손절 위험(-6.0%)이 더 큰 나쁜 손익비인 경우: 손익비 점수 3~4점 이하로 대폭 감점
                    result.RiskScore = Math.Min(result.RiskScore, 4);
                }
                else if (riskRewardRatio < 1.2m)
                {
                    // 먹을 여력과 손절 위험이 비슷한 경우: 최대 6점 캡
                    result.RiskScore = Math.Min(result.RiskScore, 6);
                }
                else if (riskRewardRatio >= 2.0m && result.DownsideRiskPercent <= 4.0m)
                {
                    // 상승 여력이 위험의 2배 이상인 이상적 손익비: 9~10점 유지
                    result.RiskScore = Math.Max(result.RiskScore, 9);
                }

                if (result.Rsi >= 70.0m)
                {
                    result.ExitTimingBadge = "RSI 과열 분할익절 (30~50%)";
                    result.ExitTimingGuide = $"14일 RSI({result.Rsi:F1}) 과열 슈팅 구간! 1차 목표가({result.RecommendedTargetPrice:N0}원) 도달 시 물량의 30~50%를 분할 매도하여 수익을 확정하세요.";
                }
                else if (currentPrice >= result.RecommendedTargetPrice * 0.98m)
                {
                    result.ExitTimingBadge = "1차 목표가 근접 (익절 대기)";
                    result.ExitTimingGuide = $"1차 저항 목표가({result.RecommendedTargetPrice:N0}원) 도달 직전! 분할 매도를 준비하세요. (돌파 시 2차: {result.SecondaryTargetPrice:N0}원)";
                }
                else if (result.ChartScore <= 3)
                {
                    result.ExitTimingBadge = "추세 이탈 (반등 시 비중축소)";
                    result.ExitTimingGuide = $"역배열 하향 이탈 국면! 반등 시마다 비중을 축소하고, 손절 방어선({result.TargetStopPrice:N0}원) 이탈 시 전량 손절 권고";
                }
                else
                {
                    result.ExitTimingBadge = "추세 유지 (목표가 대기)";
                    result.ExitTimingGuide = $"1차 목표가({result.RecommendedTargetPrice:N0}원, +{upside1:F1}%)까지 상승 여력 유효. 손절 방어선({result.TargetStopPrice:N0}원)을 지키며 홀딩";
                }
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "TechnicalIndicatorService.AnalyzeIndicatorsAsync");
            }

            return result;
        }

        public static async Task<List<DailyCandle>> Fetch20DailyCandlesAsync(string stockCode)
        {
            var list = new List<DailyCandle>();
            string cleanCode = (stockCode ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(cleanCode)) return list;

            bool isOverseas = cleanCode.EndsWith(".O") || cleanCode.EndsWith(".N") || cleanCode.EndsWith(".A") ||
                              System.Text.RegularExpressions.Regex.IsMatch(cleanCode, @"^[A-Z]{1,5}$");

            string url = isOverseas
                ? (cleanCode.Contains(".") ? $"https://api.stock.naver.com/stock/{cleanCode}/price?pageSize=60&page=1" : $"https://api.stock.naver.com/stock/{cleanCode}.O/price?pageSize=60&page=1")
                : $"https://m.stock.naver.com/api/stock/{cleanCode}/price?pageSize=60&page=1";

            var response = await _httpClient.GetAsync(url).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode && isOverseas && !cleanCode.Contains("."))
            {
                // .O 실패 시 .N 시도
                response = await _httpClient.GetAsync($"https://api.stock.naver.com/stock/{cleanCode}.N/price?pageSize=60&page=1").ConfigureAwait(false);
            }

            if (!response.IsSuccessStatusCode) return list;

            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var token = JToken.Parse(json);

            // 네이버 응답이 JArray이거나 JObject 내 "value" 키에 배열이 들어있는 경우 둘 다 처리
            JArray arr = null;
            if (token is JArray jarr) arr = jarr;
            else if (token is JObject jobj && jobj["value"] is JArray varr) arr = varr;

            if (arr == null) return list;

            foreach (var item in arr)
            {
                try
                {
                    var candle = new DailyCandle();
                    string closeStr = item["closePrice"]?.ToString() ?? "0";
                    string openStr = item["openPrice"]?.ToString() ?? "0";
                    string highStr = item["highPrice"]?.ToString() ?? "0";
                    string lowStr = item["lowPrice"]?.ToString() ?? "0";
                    long vol = 0;
                    long.TryParse(item["accumulatedTradingVolume"]?.ToString() ?? "0", out vol);

                    candle.Close = ParseDecimal(closeStr);
                    candle.Open = ParseDecimal(openStr);
                    candle.High = ParseDecimal(highStr);
                    candle.Low = ParseDecimal(lowStr);
                    candle.Volume = vol;

                    string dateStr = item["localTradedAt"]?.ToString();
                    if (DateTime.TryParse(dateStr, out DateTime dt)) candle.Date = dt;

                    if (candle.Close > 0)
                    {
                        list.Add(candle);
                    }
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

        /// <summary>
        /// KRX 한국거래소 표준 호가 단위(Tick Size) 정돈 헬퍼
        /// (실제 MTS/HTS 호가창에서 지정가 주문이 100% 체결될 수 있도록 실제 호가로 정돈)
        /// </summary>
        public static decimal RoundToKrxTick(decimal price, bool roundDown = true)
        {
            if (price <= 0) return 0m;

            decimal tick;
            if (price < 2000m) tick = 1m;
            else if (price < 5000m) tick = 5m;
            else if (price < 20000m) tick = 10m;
            else if (price < 50000m) tick = 50m;
            else if (price < 200000m) tick = 100m;
            else if (price < 500000m) tick = 500m;
            else tick = 1000m;

            if (roundDown)
                return Math.Floor(price / tick) * tick;
            else
                return Math.Ceiling(price / tick) * tick;
        }
    }
}

