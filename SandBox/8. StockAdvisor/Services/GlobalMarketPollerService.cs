using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using GateHelper.StockAdvisor.Models;

namespace GateHelper.StockAdvisor.Services
{
    public class GlobalMacroItem
    {
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ClosePrice { get; set; } = string.Empty;
        public decimal ChangeRate { get; set; } = 0m;
        public bool IsPositive => ChangeRate >= 0;
    }

    public class MarketNewsItem
    {
        public string TargetSymbol { get; set; } = string.Empty;
        public string TargetName { get; set; } = string.Empty;
        public string ArticleId { get; set; } = string.Empty;
        public string OfficeId { get; set; } = string.Empty;
        public string OfficeName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string FormattedTime { get; set; } = string.Empty;
        public string NewsUrl { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.MinValue;
        public bool IsUserHolding { get; set; } = false;
    }

    public class GlobalMarketSnapshot
    {
        public List<GlobalMacroItem> Indices { get; set; } = new List<GlobalMacroItem>();
        public List<string> RecentPolicyHeadlines { get; set; } = new List<string>();
        public DateTime FetchedAt { get; set; } = DateTime.Now;

        public string GetTickerText()
        {
            if (Indices.Count == 0) return "글로벌 매크로 지표 수집 대기 중...";
            var parts = new List<string>();
            foreach (var idx in Indices)
            {
                string sign = idx.ChangeRate >= 0 ? "+" : "";
                parts.Add($"{idx.Name}: {idx.ClosePrice} ({sign}{idx.ChangeRate:F2}%)");
            }
            return string.Join("  |  ", parts);
        }
    }

    /// <summary>
    /// 미국 주요 반도체/지수(SOX, NVDA, 나스닥) 및 실시간 뉴스(네이버 금융 & 미 BIS 수출통제) 초고속 비동기 수집기
    /// (Zero-Resource: 정적 HttpClient, 프레임당 GC 0, SSD I/O 0)
    /// </summary>
    public static class GlobalMarketPollerService
    {
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(6)
        };

        static GlobalMarketPollerService()
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            }
            catch { }

            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }

        public static async Task<GlobalMarketSnapshot> FetchMarketIntelAsync()
        {
            var snapshot = new GlobalMarketSnapshot();

            // 1. 병렬 수집 태스크 준비
            var taskNvda = FetchNaverStockAsync("NVDA.O", "엔비디아");
            var taskSox = FetchNaverIndexAsync(".SOX", "필라델피아 반도체");
            var taskIxic = FetchNaverIndexAsync(".IXIC", "나스닥");
            var taskFederalReg = FetchFederalRegisterHeadlinesAsync();

            try
            {
                await Task.WhenAll(taskNvda, taskSox, taskIxic, taskFederalReg).ConfigureAwait(false);

                if (taskNvda.Result != null) snapshot.Indices.Add(taskNvda.Result);
                if (taskSox.Result != null) snapshot.Indices.Add(taskSox.Result);
                if (taskIxic.Result != null) snapshot.Indices.Add(taskIxic.Result);
                snapshot.RecentPolicyHeadlines = taskFederalReg.Result ?? new List<string>();
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "GlobalMarketPollerService.FetchMarketIntelAsync");
            }

            return snapshot;
        }

        public static async Task<GlobalMacroItem> FetchNaverStockAsync(string symbol, string defaultName)
        {
            string url = $"https://api.stock.naver.com/stock/{symbol}/basic";
            try
            {
                string json = await _httpClient.GetStringAsync(url).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(json)) return null;

                var jobj = JObject.Parse(json);
                string name = jobj["stockName"]?.ToString() ?? defaultName;
                string price = jobj["closePrice"]?.ToString() ?? "0";
                string ratioStr = jobj["fluctuationsRatio"]?.ToString() ?? "0";

                // 프리마켓/애프터마켓 시세 우선 또는 보조 반영
                var overMarket = jobj["overMarketPriceInfo"];
                if (overMarket != null && overMarket.Type == JTokenType.Object && overMarket.HasValues)
                {
                    string overPrice = overMarket["overPrice"]?.ToString();
                    string overRatio = overMarket["fluctuationsRatio"]?.ToString();
                    string marketStatus = overMarket["marketStatus"]?.ToString() ?? "";
                    if (!string.IsNullOrWhiteSpace(overPrice) && overPrice != "0")
                    {
                        price = $"{overPrice}$ (시간외)";
                        ratioStr = overRatio;
                    }
                }
                else
                {
                    price = price + "$";
                }

                ratioStr = Regex.Replace(ratioStr, @"[^\d.-]", "");
                decimal.TryParse(ratioStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal changeRate);

                return new GlobalMacroItem
                {
                    Symbol = symbol,
                    Name = name,
                    ClosePrice = price,
                    ChangeRate = changeRate
                };
            }
            catch
            {
                return null;
            }
        }

        public static async Task<GlobalMacroItem> FetchNaverIndexAsync(string indexCode, string defaultName)
        {
            string url = $"https://api.stock.naver.com/index/{indexCode}/basic";
            try
            {
                string json = await _httpClient.GetStringAsync(url).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(json)) return null;

                var jobj = JObject.Parse(json);
                string name = jobj["stockName"]?.ToString();
                if (string.IsNullOrWhiteSpace(name)) name = defaultName;
                string price = jobj["closePrice"]?.ToString() ?? "0";
                string ratioStr = jobj["fluctuationsRatio"]?.ToString() ?? "0";

                ratioStr = Regex.Replace(ratioStr, @"[^\d.-]", "");
                decimal.TryParse(ratioStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal changeRate);

                return new GlobalMacroItem
                {
                    Symbol = indexCode,
                    Name = name,
                    ClosePrice = price,
                    ChangeRate = changeRate
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 특정 종목(해외/국내) 네이버 모바일 실시간 증권 뉴스 비동기 수집
        /// </summary>
        public static async Task<List<MarketNewsItem>> FetchStockNewsAsync(string symbolOrCode, string stockName, int pageSize = 10)
        {
            var list = new List<MarketNewsItem>();
            string url = $"https://m.stock.naver.com/api/news/stock/{symbolOrCode}?pageSize={pageSize}&page=1";

            try
            {
                string json = await _httpClient.GetStringAsync(url).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(json)) return list;

                var jarr = JArray.Parse(json);
                if (jarr != null && jarr.Count > 0)
                {
                    // 네이버 뉴스는 그룹별 JArray로 반환되므로 모든 그룹의 items를 전수 수집
                    foreach (var group in jarr)
                    {
                        var items = group["items"] as JArray;
                        if (items == null) continue;

                        foreach (var item in items)
                        {
                            string officeId = item["officeId"]?.ToString() ?? string.Empty;
                            string articleId = item["articleId"]?.ToString() ?? string.Empty;
                            string officeName = item["officeName"]?.ToString() ?? string.Empty;
                            string rawTitle = item["title"]?.ToString() ?? string.Empty;
                            string rawBody = item["body"]?.ToString() ?? string.Empty;
                            string dtStr = item["datetime"]?.ToString() ?? string.Empty;

                            string title = WebUtility.HtmlDecode(rawTitle);
                            string body = WebUtility.HtmlDecode(rawBody);

                            // datetime 포맷: "202611092324" -> "2026-11-09 23:24"
                            string formattedTime = dtStr;
                            DateTime parsedDt = DateTime.MinValue;
                            if (dtStr.Length >= 12)
                            {
                                if (DateTime.TryParseExact(dtStr.Substring(0, 12), "yyyyMMddHHmm", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDt))
                                {
                                    formattedTime = parsedDt.ToString("yyyy-MM-dd HH:mm");
                                }
                            }

                            string newsUrl = !string.IsNullOrEmpty(officeId) && !string.IsNullOrEmpty(articleId)
                                ? $"https://n.news.naver.com/mnews/article/{officeId}/{articleId}"
                                : $"https://m.stock.naver.com/worldstock/stock/{symbolOrCode}";

                            list.Add(new MarketNewsItem
                            {
                                TargetSymbol = symbolOrCode,
                                TargetName = stockName,
                                OfficeId = officeId,
                                ArticleId = articleId,
                                OfficeName = officeName,
                                Title = title,
                                Body = body,
                                FormattedTime = formattedTime,
                                NewsUrl = newsUrl,
                                Timestamp = parsedDt != DateTime.MinValue ? parsedDt : DateTime.Now
                            });

                            if (list.Count >= pageSize) break;
                        }

                        if (list.Count >= pageSize) break;
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, $"GlobalMarketPollerService.FetchStockNewsAsync({symbolOrCode})");
            }

            return list;
        }

        /// <summary>
        /// 엔비디아, 미국 BIS 정책 뉴스 및 사용자 보유 종목(유동적) 뉴스를 통합 병렬 수집
        /// </summary>
        public static async Task<List<MarketNewsItem>> FetchMarketIntelNewsAsync(IEnumerable<StockHoldingItem> userHoldings = null)
        {
            var allList = new List<MarketNewsItem>();
            var tasks = new List<Task<List<MarketNewsItem>>>();

            // 1. 글로벌 매크로 대표: 엔비디아 & 미 BIS 관보 공시 기본 수집
            tasks.Add(FetchStockNewsAsync("NVDA.O", "엔비디아 (NVDA)", 10));
            tasks.Add(FetchFederalRegisterNewsAsync());

            // 2. 사용자 보유 종목이 있는 경우: 등록된 종목들의 뉴스를 동적으로 병렬 수집
            var holdingCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (userHoldings != null)
            {
                foreach (var h in userHoldings)
                {
                    if (string.IsNullOrWhiteSpace(h.StockCode)) continue;
                    if (holdingCodes.Add(h.StockCode))
                    {
                        string targetCode = h.StockCode;
                        string targetName = !string.IsNullOrWhiteSpace(h.StockName) ? h.StockName : targetCode;
                        tasks.Add(FetchHoldingNewsWrapperAsync(targetCode, targetName));
                    }
                }
            }

            // 사용자 보유종목이 없거나 2개 미만일 때 기본 대표 반도체 보강
            if (!holdingCodes.Contains("005930"))
            {
                tasks.Add(FetchStockNewsAsync("005930", "삼성전자", 6));
            }
            if (!holdingCodes.Contains("000660"))
            {
                tasks.Add(FetchStockNewsAsync("000660", "SK하이닉스", 6));
            }

            try
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);

                foreach (var t in tasks)
                {
                    if (t.Result != null && t.Result.Count > 0)
                    {
                        allList.AddRange(t.Result);
                    }
                }

                // 최신 시간순 정렬
                allList.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "GlobalMarketPollerService.FetchMarketIntelNewsAsync");
            }

            return allList;
        }

        private static async Task<List<MarketNewsItem>> FetchHoldingNewsWrapperAsync(string code, string name)
        {
            var list = await FetchStockNewsAsync(code, name, 8).ConfigureAwait(false);
            foreach (var item in list)
            {
                item.IsUserHolding = true;
            }
            return list;
        }

        /// <summary>
        /// 구버전 호환용 (기본 반도체 뉴스 수집)
        /// </summary>
        public static Task<List<MarketNewsItem>> FetchAllSemiconductorNewsAsync()
        {
            return FetchMarketIntelNewsAsync(null);
        }

        private static async Task<List<MarketNewsItem>> FetchFederalRegisterNewsAsync()
        {
            var list = new List<MarketNewsItem>();
            string url = "https://www.federalregister.gov/api/v1/documents.json?conditions[term]=semiconductor&per_page=5&order=newest";
            try
            {
                string json = await _httpClient.GetStringAsync(url).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(json)) return list;

                var jobj = JObject.Parse(json);
                var results = jobj["results"] as JArray;
                if (results != null)
                {
                    foreach (var item in results)
                    {
                        string title = item["title"]?.ToString() ?? string.Empty;
                        string pubDate = item["publication_date"]?.ToString() ?? string.Empty;
                        string htmlUrl = item["html_url"]?.ToString() ?? string.Empty;
                        string summary = item["abstract"]?.ToString() ?? "미 연방정부 관보 반도체 규제 및 수출 통제 고시";

                        DateTime dt = DateTime.MinValue;
                        DateTime.TryParse(pubDate, out dt);

                        if (!string.IsNullOrWhiteSpace(title))
                        {
                            list.Add(new MarketNewsItem
                            {
                                TargetSymbol = "BIS",
                                TargetName = "미 상무부 (BIS)",
                                OfficeName = "Federal Register",
                                Title = WebUtility.HtmlDecode(title),
                                Body = WebUtility.HtmlDecode(summary),
                                FormattedTime = pubDate,
                                NewsUrl = htmlUrl,
                                Timestamp = dt != DateTime.MinValue ? dt : DateTime.Now.AddDays(-3)
                            });
                        }
                    }
                }
            }
            catch
            {
                // 타임아웃 방어
            }
            return list;
        }

        public static async Task<List<string>> FetchFederalRegisterHeadlinesAsync()
        {
            var list = new List<string>();
            var news = await FetchFederalRegisterNewsAsync().ConfigureAwait(false);
            foreach (var n in news)
            {
                list.Add($"[{n.FormattedTime}] {n.Title}");
            }
            return list;
        }
    }
}
