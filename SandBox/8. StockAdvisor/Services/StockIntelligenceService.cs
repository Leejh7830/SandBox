using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace GateHelper.StockAdvisor.Services
{
    public class SupplyAndLandmineDto
    {
        public string StockCode { get; set; } = string.Empty;

        // 수급 지표
        public string SupplyBadge { get; set; } = "⚪ 수급대기";
        public string SupplyDetail { get; set; } = string.Empty;
        public long ForeignNetQuantity { get; set; } = 0;
        public long OrganNetQuantity { get; set; } = 0;
        public long IndividualNetQuantity { get; set; } = 0;

        // 지뢰 탐지 지표
        public string LandmineBadge { get; set; } = "🛡️ 클린안전";
        public string LandmineDetail { get; set; } = "재무·공시 지뢰 없음";
        public bool HasCriticalLandmine { get; set; } = false;
        public decimal DebtRatio { get; set; } = 0m;
        public int ConsecutiveLossYears { get; set; } = 0;
        public string RecentDisclosuresSummary { get; set; } = string.Empty;
    }

    /// <summary>
    /// 네이버 증권 오픈 API 기반 실시간 외인/기관 수급 분석 및 재무·공시 지뢰 탐지기
    /// (Zero-Resource: 정적 클라이언트, 0-GC, 3개 엔드포인트 병렬 수집)
    /// </summary>
    public static class StockIntelligenceService
    {
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        static StockIntelligenceService()
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            }
            catch { }
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }

        public static async Task<SupplyAndLandmineDto> AnalyzeAsync(string stockCode)
        {
            var result = new SupplyAndLandmineDto { StockCode = stockCode };
            if (string.IsNullOrWhiteSpace(stockCode)) return result;

            try
            {
                // 3개 엔드포인트 병렬 비동기 조회
                var trendTask = FetchTrendAsync(stockCode);
                var financeTask = FetchFinanceAsync(stockCode);
                var disclosureTask = FetchDisclosureAsync(stockCode);

                await Task.WhenAll(trendTask, financeTask, disclosureTask);

                // 1. 수급 분석 결과 반영
                var trendData = trendTask.Result;
                if (trendData != null)
                {
                    result.SupplyBadge = trendData.Badge;
                    result.SupplyDetail = trendData.Detail;
                    result.ForeignNetQuantity = trendData.ForeignNet;
                    result.OrganNetQuantity = trendData.OrganNet;
                    result.IndividualNetQuantity = trendData.IndividualNet;
                }

                // 2. 재무/공시 지뢰 결과 통합 분석
                var finData = financeTask.Result;
                var discData = disclosureTask.Result;

                result.DebtRatio = finData.DebtRatio;
                result.ConsecutiveLossYears = finData.ConsecutiveLossYears;
                result.RecentDisclosuresSummary = discData.RecentSummary;

                // 지뢰 판정
                if (discData.HasCriticalLandmine)
                {
                    result.HasCriticalLandmine = true;
                    result.LandmineBadge = discData.CriticalBadge;
                    result.LandmineDetail = $"[공시지뢰] {discData.CriticalDetail}";
                }
                else if (finData.ConsecutiveLossYears >= 3)
                {
                    result.HasCriticalLandmine = true;
                    result.LandmineBadge = "💀 3년적자";
                    result.LandmineDetail = $"[재무지뢰] 3개년 연속 영업손실 (관리종목/상폐 위험)";
                }
                else if (finData.DebtRatio >= 250m)
                {
                    result.HasCriticalLandmine = false;
                    result.LandmineBadge = "⚠️ 부채과다";
                    result.LandmineDetail = $"[재무주의] 부채비율 {finData.DebtRatio:F1}% 과다 (재무 건전성 점검 필요)";
                }
                else if (discData.HasShareholderFriendly)
                {
                    result.HasCriticalLandmine = false;
                    result.LandmineBadge = "💎 주주환원";
                    result.LandmineDetail = $"[호재공시] {discData.ShareholderDetail}";
                }
                else
                {
                    result.HasCriticalLandmine = false;
                    result.LandmineBadge = "🛡️ 클린안전";
                    result.LandmineDetail = $"부채비율 {finData.DebtRatio:F0}% 건전 | 최근 CB/유증 지뢰 0건";
                }
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, $"StockIntelligenceService.AnalyzeAsync({stockCode})");
            }

            return result;
        }

        #region Internal Fetchers

        private class TrendResult
        {
            public string Badge { get; set; } = "⚪ 수급대기";
            public string Detail { get; set; } = string.Empty;
            public long ForeignNet { get; set; }
            public long OrganNet { get; set; }
            public long IndividualNet { get; set; }
        }

        private static async Task<TrendResult> FetchTrendAsync(string stockCode)
        {
            var res = new TrendResult();
            try
            {
                string url = $"https://m.stock.naver.com/api/stock/{stockCode}/trend";
                string json = await _httpClient.GetStringAsync(url);
                var arr = JArray.Parse(json);
                if (arr.Count == 0) return res;

                // 가장 최근 일자 및 최근 3일간 누적 계산
                long latestFor = 0;
                long latestOrg = 0;
                long latestInd = 0;

                int forBuyDays = 0;
                int orgBuyDays = 0;
                int checkDays = Math.Min(3, arr.Count);

                for (int i = 0; i < checkDays; i++)
                {
                    var item = arr[i];
                    long f = ParseQuantity(item["foreignerPureBuyQuant"]?.ToString());
                    long o = ParseQuantity(item["organPureBuyQuant"]?.ToString());
                    long ind = ParseQuantity(item["individualPureBuyQuant"]?.ToString());

                    if (i == 0)
                    {
                        latestFor = f;
                        latestOrg = o;
                        latestInd = ind;
                    }

                    if (f > 0) forBuyDays++;
                    if (o > 0) orgBuyDays++;
                }

                res.ForeignNet = latestFor;
                res.OrganNet = latestOrg;
                res.IndividualNet = latestInd;

                // 수급 뱃지 판정
                if (latestFor > 0 && latestOrg > 0)
                {
                    res.Badge = "🌊 메이저쌍끌이";
                    res.Detail = $"외인(+{latestFor:N0}) & 기관(+{latestOrg:N0}) 동시 순매수 유입";
                }
                else if (forBuyDays >= 2 && orgBuyDays >= 2)
                {
                    res.Badge = "🌊 메이저쌍끌이";
                    res.Detail = $"최근 3일 중 2일 이상 외인·기관 연속 매집";
                }
                else if (latestFor > 0 && latestOrg <= 0)
                {
                    res.Badge = forBuyDays >= 2 ? "🦅 외인연속매수" : "🦅 외인매수";
                    res.Detail = $"외국인 순매수(+{latestFor:N0}주) 주도";
                }
                else if (latestOrg > 0 && latestFor <= 0)
                {
                    res.Badge = orgBuyDays >= 2 ? "🏦 기관연속매수" : "🏦 기관매수";
                    res.Detail = $"기관 순매수(+{latestOrg:N0}주) 주도";
                }
                else if (latestFor < 0 && latestOrg < 0 && latestInd > 0)
                {
                    res.Badge = "🐜 개미홀로매수";
                    res.Detail = $"외인({latestFor:N0})/기관({latestOrg:N0}) 쌍끌이 매도, 개인만 매수 중 (주의)";
                }
                else if (latestFor < 0 && latestOrg < 0)
                {
                    res.Badge = "🌧️ 메이저쌍끌이매도";
                    res.Detail = $"외인과 기관이 동시 이탈 중 (리스크 관리 필요)";
                }
                else
                {
                    res.Badge = "⚪ 수급공방";
                    res.Detail = $"외인 {latestFor:+0;-0;0} / 기관 {latestOrg:+0;-0;0}";
                }
            }
            catch { }
            return res;
        }

        private class FinanceResult
        {
            public decimal DebtRatio { get; set; } = 0m;
            public int ConsecutiveLossYears { get; set; } = 0;
        }

        private static async Task<FinanceResult> FetchFinanceAsync(string stockCode)
        {
            var res = new FinanceResult();
            try
            {
                string url = $"https://m.stock.naver.com/api/stock/{stockCode}/finance/annual";
                string json = await _httpClient.GetStringAsync(url);
                var root = JObject.Parse(json);
                var rowList = root["financeInfo"]?["rowList"] as JArray;
                if (rowList == null) return res;

                foreach (var row in rowList)
                {
                    string title = row["title"]?.ToString() ?? string.Empty;
                    var cols = row["columns"] as JObject;
                    if (cols == null) continue;

                    if (title == "영업이익")
                    {
                        // 최근 연도별 적자(cx == "minus") 카운트
                        int minusCount = 0;
                        foreach (var prop in cols.Properties())
                        {
                            if (prop.Value["cx"]?.ToString() == "minus")
                            {
                                minusCount++;
                            }
                        }
                        res.ConsecutiveLossYears = minusCount;
                    }
                    else if (title == "부채비율")
                    {
                        // 가장 최근 결산 연도의 부채비율 추출
                        foreach (var prop in cols.Properties())
                        {
                            string valStr = prop.Value["value"]?.ToString();
                            if (!string.IsNullOrEmpty(valStr) && valStr != "-")
                            {
                                if (decimal.TryParse(valStr.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal d))
                                {
                                    res.DebtRatio = d; // 가장 최근 값으로 갱신
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            return res;
        }

        private class DisclosureResult
        {
            public bool HasCriticalLandmine { get; set; } = false;
            public string CriticalBadge { get; set; } = string.Empty;
            public string CriticalDetail { get; set; } = string.Empty;

            public bool HasShareholderFriendly { get; set; } = false;
            public string ShareholderDetail { get; set; } = string.Empty;

            public string RecentSummary { get; set; } = string.Empty;
        }

        private static async Task<DisclosureResult> FetchDisclosureAsync(string stockCode)
        {
            var res = new DisclosureResult();
            try
            {
                string url = $"https://m.stock.naver.com/api/stock/{stockCode}/disclosure";
                string json = await _httpClient.GetStringAsync(url);
                var arr = JArray.Parse(json);
                if (arr == null || arr.Count == 0) return res;

                int checkCount = Math.Min(20, arr.Count);
                for (int i = 0; i < checkCount; i++)
                {
                    string title = arr[i]["title"]?.ToString() ?? string.Empty;

                    // 1. 치명적 지뢰 공시 키워드 필터
                    if (title.Contains("전환사채") || title.Contains("신주인수권부사채") || title.Contains("CB발행") || title.Contains("BW발행"))
                    {
                        res.HasCriticalLandmine = true;
                        res.CriticalBadge = "🚨 CB/BW발행";
                        res.CriticalDetail = title;
                        break;
                    }
                    if (title.Contains("유상증자 결정"))
                    {
                        res.HasCriticalLandmine = true;
                        res.CriticalBadge = "🚨 유상증자";
                        res.CriticalDetail = title;
                        break;
                    }
                    if (title.Contains("감사의견") && (title.Contains("거절") || title.Contains("한정") || title.Contains("부적정")))
                    {
                        res.HasCriticalLandmine = true;
                        res.CriticalBadge = "💀 감사의견거절";
                        res.CriticalDetail = title;
                        break;
                    }
                    if (title.Contains("관리종목") || title.Contains("상장폐지") || title.Contains("거래정지"))
                    {
                        res.HasCriticalLandmine = true;
                        res.CriticalBadge = "💀 관리종목위기";
                        res.CriticalDetail = title;
                        break;
                    }
                    if (title.Contains("횡령") || title.Contains("배임"))
                    {
                        res.HasCriticalLandmine = true;
                        res.CriticalBadge = "💀 횡령배임발생";
                        res.CriticalDetail = title;
                        break;
                    }

                    // 2. 주주 친화 호재 공시
                    if (!res.HasShareholderFriendly)
                    {
                        if (title.Contains("자기주식 취득") || title.Contains("주식 소각") || title.Contains("배당 결정"))
                        {
                            res.HasShareholderFriendly = true;
                            res.ShareholderDetail = title;
                        }
                    }
                }

                if (arr.Count > 0)
                {
                    res.RecentSummary = arr[0]["title"]?.ToString() ?? string.Empty;
                }
            }
            catch { }
            return res;
        }

        private static long ParseQuantity(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return 0;
            string clean = raw.Replace("+", "").Replace(",", "").Trim();
            if (long.TryParse(clean, out long val)) return val;
            return 0;
        }

        #endregion
    }
}
