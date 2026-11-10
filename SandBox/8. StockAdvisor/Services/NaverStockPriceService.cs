using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace GateHelper.StockAdvisor.Services
{
    public class StockPriceDto
    {
        public string StockCode { get; set; } = string.Empty;
        public string StockName { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; } = 0m;
        public decimal ChangeRate { get; set; } = 0m;
        public decimal ChangePrice { get; set; } = 0m;
        public bool IsSuccess { get; set; } = false;
        public string ErrorMessage { get; set; } = string.Empty;
        public bool IsForeign { get; set; } = false;
        public string ReutersCode { get; set; } = string.Empty;
        public DateTime ScrapedAt { get; set; } = DateTime.Now;
    }

    public class StockSearchResultItem
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ReutersCode { get; set; } = string.Empty;
        public string NationCode { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public bool IsForeign => NationCode != "KOR";
    }

    /// <summary>
    /// 네이버 증권 공개 REST API 기반 초경량 실시간 시세 및 지능형 종목 검색기 (국내/해외 통합 지원)
    /// (Zero-Resource: 정적 클라이언트, 0-GC 지향)
    /// </summary>
    public static class NaverStockPriceService
    {
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(6)
        };

        static NaverStockPriceService()
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            }
            catch { }

            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }

        /// <summary>
        /// 종목명(예: "카카오", "테슬라", "삼성전자") 또는 코드/티커(예: "035720", "NVDA")로 네이버 증권 공식 종목 검색
        /// </summary>
        public static async Task<List<StockSearchResultItem>> SearchStockAsync(string query)
        {
            var results = new List<StockSearchResultItem>();
            if (string.IsNullOrWhiteSpace(query)) return results;

            string encodedQuery = WebUtility.UrlEncode(query.Trim());
            string url = $"https://ac.stock.naver.com/ac?q={encodedQuery}&target=stock";

            try
            {
                string json = await _httpClient.GetStringAsync(url).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(json)) return results;

                var jobj = JObject.Parse(json);
                var items = jobj["items"] as JArray;
                if (items != null)
                {
                    foreach (var item in items)
                    {
                        string code = item["code"]?.ToString() ?? string.Empty;
                        string name = item["name"]?.ToString() ?? string.Empty;
                        string reuters = item["reutersCode"]?.ToString() ?? string.Empty;
                        string nation = item["nationCode"]?.ToString() ?? "KOR";
                        string typeName = item["typeName"]?.ToString() ?? string.Empty;

                        if (!string.IsNullOrEmpty(code))
                        {
                            results.Add(new StockSearchResultItem
                            {
                                Code = code,
                                Name = name,
                                ReutersCode = string.IsNullOrEmpty(reuters) ? code : reuters,
                                NationCode = nation,
                                TypeName = typeName
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, $"NaverStockPriceService.SearchStockAsync({query})");
            }

            return results;
        }

        /// <summary>
        /// 종목 코드 또는 종목명으로 실시간 시세 및 정보 조회 (국내/해외 지능형 자동 판별)
        /// </summary>
        public static async Task<StockPriceDto> FetchPriceAsync(string queryOrCode)
        {
            var result = new StockPriceDto();
            if (string.IsNullOrWhiteSpace(queryOrCode))
            {
                result.ErrorMessage = "종목명 또는 종목코드를 입력해주세요.";
                return result;
            }

            string raw = queryOrCode.Trim();

            // 1. 순수 6자리 숫자인 경우: 국내 주식 직통 조회 우선 시도
            if (raw.Length <= 6 && Regex.IsMatch(raw, @"^\d+$"))
            {
                string standardCode = raw.PadLeft(6, '0');
                var directDto = await FetchDomesticPriceDirectAsync(standardCode).ConfigureAwait(false);
                if (directDto.IsSuccess)
                {
                    return directDto;
                }
            }

            // 2. 종목명 또는 해외 티커(또는 위 직통 조회가 실패한 경우): 네이버 지능형 검색 시도
            var searchResults = await SearchStockAsync(raw).ConfigureAwait(false);
            if (searchResults.Count > 0)
            {
                var matched = searchResults[0]; // 가장 정확한 첫 번째 매칭

                if (matched.IsForeign)
                {
                    // 해외 주식 시세 조회
                    var foreignDto = await FetchForeignPriceDirectAsync(matched.ReutersCode, matched.Name).ConfigureAwait(false);
                    foreignDto.StockCode = matched.Code;
                    return foreignDto;
                }
                else
                {
                    // 국내 주식 시세 조회
                    var domesticDto = await FetchDomesticPriceDirectAsync(matched.Code).ConfigureAwait(false);
                    if (string.IsNullOrEmpty(domesticDto.StockName)) domesticDto.StockName = matched.Name;
                    return domesticDto;
                }
            }

            result.ErrorMessage = $"'{raw}' 종목을 네이버 증권에서 찾을 수 없습니다. 오타가 없는지 확인해주세요.";
            return result;
        }

        private static async Task<StockPriceDto> FetchDomesticPriceDirectAsync(string stockCode)
        {
            var result = new StockPriceDto { StockCode = stockCode, IsForeign = false };
            string url = $"https://m.stock.naver.com/api/stock/{stockCode}/basic";

            try
            {
                using (var response = await _httpClient.GetAsync(url).ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        result.IsSuccess = false;
                        if ((int)response.StatusCode == 429)
                            result.ErrorMessage = "네이버 요청 한도 초과 (429 Too Many Requests - 30초 후 재시도 필요)";
                        else if (response.StatusCode == HttpStatusCode.NotFound)
                            result.ErrorMessage = $"종목코드({stockCode})를 네이버 증권에서 찾을 수 없습니다 (404)";
                        else
                            result.ErrorMessage = $"네이버 서버 통신 오류 (HTTP {(int)response.StatusCode})";
                        return result;
                    }

                    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        result.IsSuccess = false;
                        result.ErrorMessage = "네이버 서버로부터 빈 응답 데이터 수신 (Empty Body)";
                        return result;
                    }

                    var jobj = JObject.Parse(json);
                    result.StockName = jobj["stockName"]?.ToString() ?? stockCode;

                    string closeStr = jobj["closePrice"]?.ToString() ?? "0";
                    closeStr = Regex.Replace(closeStr, @"[^\d.-]", "");
                    if (decimal.TryParse(closeStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal closePrice))
                    {
                        result.CurrentPrice = closePrice;
                    }

                    string ratioStr = jobj["fluctuationsRatio"]?.ToString() ?? "0";
                    ratioStr = Regex.Replace(ratioStr, @"[^\d.-]", "");
                    if (decimal.TryParse(ratioStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal changeRate))
                    {
                        result.ChangeRate = changeRate;
                    }

                    string compStr = jobj["compareToPreviousClosePrice"]?.ToString() ?? "0";
                    compStr = Regex.Replace(compStr, @"[^\d.-]", "");
                    if (decimal.TryParse(compStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal compPrice))
                    {
                        result.ChangePrice = compPrice;
                    }

                    if (result.CurrentPrice <= 0m)
                    {
                        result.IsSuccess = false;
                        result.ErrorMessage = "현재가 수치 이상 (0원 응답 - 거래정지 종목이거나 네이버 데이터 파싱 실패)";
                    }
                    else
                    {
                        result.IsSuccess = true;
                        result.ScrapedAt = DateTime.Now;
                    }
                }
            }
            catch (TaskCanceledException)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "네이버 서버 응답 시간 초과 (Timeout - 인터넷 연결 확인 필요)";
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"네이버 통신 오류: {ex.Message}";
            }

            return result;
        }

        private static async Task<StockPriceDto> FetchForeignPriceDirectAsync(string reutersCode, string stockName)
        {
            var result = new StockPriceDto { StockCode = reutersCode, StockName = stockName, IsForeign = true, ReutersCode = reutersCode };
            string url = $"https://api.stock.naver.com/stock/{reutersCode}/basic";

            try
            {
                using (var response = await _httpClient.GetAsync(url).ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        result.IsSuccess = false;
                        result.ErrorMessage = $"해외 주식 서버 통신 오류 (HTTP {(int)response.StatusCode})";
                        return result;
                    }

                    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        result.IsSuccess = false;
                        result.ErrorMessage = "해외 주식 시세 응답이 비어 있습니다.";
                        return result;
                    }

                    var jobj = JObject.Parse(json);
                    string name = jobj["stockName"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(name)) result.StockName = name;

                    string closeStr = jobj["closePrice"]?.ToString() ?? "0";
                    closeStr = Regex.Replace(closeStr, @"[^\d.-]", "");
                    if (decimal.TryParse(closeStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal closePrice))
                    {
                        result.CurrentPrice = closePrice;
                    }

                    string ratioStr = jobj["fluctuationsRatio"]?.ToString() ?? "0";
                    ratioStr = Regex.Replace(ratioStr, @"[^\d.-]", "");
                    if (decimal.TryParse(ratioStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal changeRate))
                    {
                        result.ChangeRate = changeRate;
                    }

                    if (result.CurrentPrice <= 0m)
                    {
                        result.IsSuccess = false;
                        result.ErrorMessage = "해외 주식 현재가 수치 이상 (0원 응답)";
                    }
                    else
                    {
                        result.IsSuccess = true;
                        result.ScrapedAt = DateTime.Now;
                    }
                }
            }
            catch (TaskCanceledException)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "해외 주식 서버 응답 시간 초과 (Timeout)";
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"해외 시세 통신 오류: {ex.Message}";
            }

            return result;
        }
    }
}
