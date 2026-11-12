using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace GateHelper.StockAdvisor.Services
{
    public class MarketTemperatureResult
    {
        public decimal Temperature { get; set; } = 36.5m;          // 시장 체온 (℃)
        public string StatusName { get; set; } = "정상 균형";       // 상태 명칭
        public string BadgeText { get; set; } = "체온 36.5℃ [정상]";
        public Color BadgeForeColor { get; set; } = Color.FromArgb(70, 220, 140);
        public Color BadgeBackColor { get; set; } = Color.FromArgb(24, 38, 48);

        public decimal KospiCurrent { get; set; } = 0m;            // 코스피 현재 지수
        public decimal KospiChangeRate { get; set; } = 0m;         // 당일 등락률 (%)
        public decimal KospiChangePoint { get; set; } = 0m;        // 당일 대비 포인트
        public decimal KospiMa20 { get; set; } = 0m;               // 코스피 20일 이동평균선
        public decimal DisparityPercent { get; set; } = 0m;        // 20일선 이격도 (%)

        public string MarketPhase { get; set; } = string.Empty;    // 국면 진단
        public string CashRatioGuide { get; set; } = string.Empty; // 권고 현금 비중
        public string ActionGuide { get; set; } = string.Empty;    // 실전 매매 행동 강령
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public bool IsSuccess { get; set; } = false;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// KOSPI 지수 및 20일선 이격도를 기반으로 실시간 시장 과열/공포 체온(36.5℃ 기준)을 진단하는 지능형 서비스
    /// </summary>
    public static class MarketThermometerService
    {
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(6)
        };

        private static MarketTemperatureResult _cachedResult = null;
        private static DateTime _lastFetchTime = DateTime.MinValue;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);
        private static readonly object _cacheLock = new object();

        static MarketThermometerService()
        {
            try
            {
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
            }
            catch { }

            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }

        public static async Task<MarketTemperatureResult> GetMarketTemperatureAsync(bool forceRefresh = false)
        {
            lock (_cacheLock)
            {
                if (!forceRefresh && _cachedResult != null && (DateTime.Now - _lastFetchTime) < CacheDuration)
                {
                    return _cachedResult;
                }
            }

            var res = new MarketTemperatureResult();
            string url = "https://m.stock.naver.com/api/index/KOSPI/price?pageSize=20&page=1";

            try
            {
                using (var response = await _httpClient.GetAsync(url).ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        res.IsSuccess = false;
                        res.ErrorMessage = $"HTTP {(int)response.StatusCode}";
                        return res;
                    }

                    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        res.ErrorMessage = "빈 응답";
                        return res;
                    }

                    var jarr = JArray.Parse(json);
                    if (jarr == null || jarr.Count == 0)
                    {
                        res.ErrorMessage = "데이터 없음";
                        return res;
                    }

                    var closePrices = new List<decimal>();
                    decimal curPrice = 0m;
                    decimal curChangeRate = 0m;
                    decimal curChangePoint = 0m;

                    for (int i = 0; i < jarr.Count; i++)
                    {
                        var row = jarr[i];
                        string closeStr = row["closePrice"]?.ToString() ?? "0";
                        closeStr = Regex.Replace(closeStr, @"[^\d.]", "");
                        if (decimal.TryParse(closeStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal closeVal) && closeVal > 0)
                        {
                            closePrices.Add(closeVal);
                            if (i == 0)
                            {
                                curPrice = closeVal;
                                string ratioStr = row["fluctuationsRatio"]?.ToString() ?? "0";
                                ratioStr = Regex.Replace(ratioStr, @"[^\d.-]", "");
                                decimal.TryParse(ratioStr, NumberStyles.Any, CultureInfo.InvariantCulture, out curChangeRate);

                                string diffStr = row["compareToPreviousClosePrice"]?.ToString() ?? "0";
                                diffStr = Regex.Replace(diffStr, @"[^\d.-]", "");
                                decimal.TryParse(diffStr, NumberStyles.Any, CultureInfo.InvariantCulture, out curChangePoint);
                            }
                        }
                    }

                    if (closePrices.Count == 0)
                    {
                        res.ErrorMessage = "종가 파싱 실패";
                        return res;
                    }

                    // 20일 이동평균선(MA20) 계산
                    decimal sum = 0m;
                    foreach (var p in closePrices) sum += p;
                    decimal ma20 = sum / closePrices.Count;

                    // 이격도 (%) = (현재가 - MA20) / MA20 * 100
                    decimal disparity = ma20 > 0 ? ((curPrice - ma20) / ma20 * 100m) : 0m;

                    // 체온 산출 공식 (정상 36.5℃ 기준, 이격도와 당일 등락 반영)
                    decimal temp = 36.5m + (disparity * 0.65m) + (curChangeRate * 0.25m);
                    temp = Math.Max(31.0m, Math.Min(41.5m, Math.Round(temp, 1)));

                    res.Temperature = temp;
                    res.KospiCurrent = curPrice;
                    res.KospiChangeRate = curChangeRate;
                    res.KospiChangePoint = curChangePoint;
                    res.KospiMa20 = Math.Round(ma20, 2);
                    res.DisparityPercent = Math.Round(disparity, 2);
                    res.UpdatedAt = DateTime.Now;
                    res.IsSuccess = true;

                    // 상태 판정 및 가이드 수립
                    if (temp >= 38.5m)
                    {
                        res.StatusName = "고열 과열";
                        res.BadgeText = $"체온 {temp:F1}℃ [과열]";
                        res.BadgeForeColor = Color.FromArgb(255, 95, 95);
                        res.BadgeBackColor = Color.FromArgb(52, 22, 28);
                        res.MarketPhase = "코스피 20일선 급이격 단기 과열 슈팅 국면";
                        res.CashRatioGuide = "현금 40% 이상 확보 권고";
                        res.ActionGuide = "추격 매수 엄금! 1차 목표가 도달 종목 30~50% 분할 익절하여 안전 현금을 확보하세요.";
                    }
                    else if (temp >= 37.0m)
                    {
                        res.StatusName = "활황 상승";
                        res.BadgeText = $"체온 {temp:F1}℃ [활황]";
                        res.BadgeForeColor = Color.FromArgb(255, 205, 70);
                        res.BadgeBackColor = Color.FromArgb(46, 38, 20);
                        res.MarketPhase = "20일선 우상향 탄력 유지 및 양호한 상승 장세";
                        res.CashRatioGuide = "현금 20% 유지 (주식 80%)";
                        res.ActionGuide = "상승 탄력 양호! 1차 매수 타점 도달 종목 및 외인·기관 쌍끌이 주도주 적극 공략 유효.";
                    }
                    else if (temp >= 36.0m)
                    {
                        res.StatusName = "정상 균형";
                        res.BadgeText = $"체온 {temp:F1}℃ [정상]";
                        res.BadgeForeColor = Color.FromArgb(70, 230, 150);
                        res.BadgeBackColor = Color.FromArgb(22, 40, 36);
                        res.MarketPhase = "20일선 안착 및 지수 적정 안정 균형 장세";
                        res.CashRatioGuide = "현금 30% 유지 (주식 70%)";
                        res.ActionGuide = "시장 안정권. 퀀트 8대 필터 통과 우량주 위주로 원칙 매매를 지속하세요.";
                    }
                    else if (temp >= 34.5m)
                    {
                        res.StatusName = "조정 눌림";
                        res.BadgeText = $"체온 {temp:F1}℃ [조정]";
                        res.BadgeForeColor = Color.FromArgb(100, 210, 255);
                        res.BadgeBackColor = Color.FromArgb(20, 36, 52);
                        res.MarketPhase = "20일선 하회 및 지지선 테스트 단기 조정 국면";
                        res.CashRatioGuide = "현금 40% 유지 (주식 60%)";
                        res.ActionGuide = "조정장 분할 접근! 2차 매수가(눌림목/추매) 부근까지 지지 확인 후 천천히 분할 매수 유효.";
                    }
                    else
                    {
                        res.StatusName = "공포 투매";
                        res.BadgeText = $"체온 {temp:F1}℃ [공포]";
                        res.BadgeForeColor = Color.FromArgb(255, 110, 140);
                        res.BadgeBackColor = Color.FromArgb(50, 20, 36);
                        res.MarketPhase = "20일선 붕괴 및 패닉 투매 침체 국면";
                        res.CashRatioGuide = "현금 60% 이상 절대 방어";
                        res.ActionGuide = "뇌동매매 절대 금지! 손절선 철저히 준수하고, 시장 바닥 반등 지지 확인 전까지 관망.";
                    }

                    lock (_cacheLock)
                    {
                        _cachedResult = res;
                        _lastFetchTime = DateTime.Now;
                    }
                    return res;
                }
            }
            catch (Exception ex)
            {
                res.IsSuccess = false;
                res.ErrorMessage = ex.Message;
                return res;
            }
        }
    }
}
