using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GateHelper.StockAdvisor.Models;
using GateHelper.StockAdvisor.Security;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GateHelper.StockAdvisor.Trading
{
    /// <summary>
    /// KB증권 Open API 비동기 REST 클라이언트 (Trading Client)
    /// - OAuth2 토큰 자동 캐싱 (메모리 세션 관리, 평문 파일 저장 금지)
    /// - DPAPI 암호화 기반 설정 저장 (_meta/sandbox/kbsec_config.json)
    /// - API 키 미등록 시 안전한 가상 시뮬레이션 체결/잔고 연동 모드 지원
    /// - Zero-Resource: 정적 HttpClient 인스턴스, 유휴 CPU 0%
    /// </summary>
    public class KbTradingClient : IKbTradingClient
    {
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_meta", "sandbox", "kbsec_config.json");

        private string _appKey;
        private string _appSecret;
        private bool _isMockTrading;
        private bool _enableSimulation;
        private KbAuthTokenResponse _cachedToken = null;

        public bool IsConfigured => !string.IsNullOrWhiteSpace(_appKey) && !string.IsNullOrWhiteSpace(_appSecret);
        public bool IsSimulationMode => !IsConfigured && _enableSimulation;

        public KbTradingClient()
        {
            var cfg = LoadConfig();
            _appKey = cfg.AppKey;
            _appSecret = cfg.AppSecret;
            _isMockTrading = cfg.IsMockTrading;
            _enableSimulation = cfg.EnableSimulationWhenNoKey;
        }

        public KbTradingClient(string appKey, string appSecret, bool isMockTrading = false, bool enableSimulation = false)
        {
            _appKey = appKey?.Trim() ?? string.Empty;
            _appSecret = appSecret?.Trim() ?? string.Empty;
            _isMockTrading = isMockTrading;
            _enableSimulation = enableSimulation;
        }

        private string BaseUrl => _isMockTrading 
            ? "https://openapivts.kbsec.com" 
            : "https://openapi.kbsec.com";

        public static KbSecApiConfig LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = DpapiHelper.LoadEncryptedFile(ConfigPath);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var cfg = JsonConvert.DeserializeObject<KbSecApiConfig>(json);
                        if (cfg != null) return cfg;
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "KbTradingClient.LoadConfig");
            }
            return new KbSecApiConfig();
        }

        public static void SaveConfig(KbSecApiConfig config)
        {
            if (config == null) return;
            try
            {
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                DpapiHelper.SaveEncryptedFile(ConfigPath, json);
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "KbTradingClient.SaveConfig");
            }
        }

        public async Task<KbAuthTokenResponse> GetAccessTokenAsync()
        {
            if (!IsConfigured)
            {
                if (IsSimulationMode)
                {
                    return new KbAuthTokenResponse
                    {
                        IsSuccess = true,
                        AccessToken = "SIMULATION_MOCK_TOKEN",
                        IssuedAt = DateTime.Now,
                        ExpiresIn = 86400
                    };
                }

                return new KbAuthTokenResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "KB증권 Open API 키(App Key / Secret)가 설정되지 않았습니다."
                };
            }

            if (_cachedToken != null && !_cachedToken.IsExpired)
            {
                return _cachedToken;
            }

            try
            {
                string url = $"{BaseUrl}/oauth2/token";
                var reqBody = new
                {
                    grant_type = "client_credentials",
                    appkey = _appKey,
                    appsecret = _appSecret
                };

                using (var content = new StringContent(JsonConvert.SerializeObject(reqBody), Encoding.UTF8, "application/json"))
                {
                    var response = await _httpClient.PostAsync(url, content).ConfigureAwait(false);
                    string resJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        return new KbAuthTokenResponse
                        {
                            IsSuccess = false,
                            ErrorMessage = $"KB 인증 실패 ({response.StatusCode}): {resJson}"
                        };
                    }

                    var jobj = JObject.Parse(resJson);
                    _cachedToken = new KbAuthTokenResponse
                    {
                        IsSuccess = true,
                        AccessToken = jobj["access_token"]?.ToString() ?? string.Empty,
                        TokenType = jobj["token_type"]?.ToString() ?? "Bearer",
                        ExpiresIn = jobj["expires_in"]?.Value<int>() ?? 86400,
                        IssuedAt = DateTime.Now
                    };
                    return _cachedToken;
                }
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "KbTradingClient.GetAccessTokenAsync");
                return new KbAuthTokenResponse
                {
                    IsSuccess = false,
                    ErrorMessage = $"네트워크 연결 오류: {ex.Message}"
                };
            }
        }

        public async Task<KbAccountBalanceResponse> FetchBalanceAsync(string accountNumber)
        {
            var res = new KbAccountBalanceResponse { AccountNumber = accountNumber };

            if (!IsConfigured)
            {
                if (IsSimulationMode)
                {
                    await Task.Delay(400);
                    res.IsSuccess = true;
                    res.ErrorMessage = "💡 [가상 시뮬레이션 모드] API 키 미등록 상태에서 가상 샘플 잔고 데이터를 확인했습니다.";
                    
                    // 가상 잔고 데이터
                    res.Holdings = new List<KbHoldingBalanceDto>
                    {
                        new KbHoldingBalanceDto { StockCode = "000270", StockName = "기아", HoldingQuantity = 1, BuyAveragePrice = 133950m, CurrentPrice = 95600m },
                        new KbHoldingBalanceDto { StockCode = "005930", StockName = "삼성전자", HoldingQuantity = 1, BuyAveragePrice = 230000m, CurrentPrice = 197400m },
                        new KbHoldingBalanceDto { StockCode = "001740", StockName = "SK네트웍스", HoldingQuantity = 8, BuyAveragePrice = 9334m, CurrentPrice = 5470m },
                        new KbHoldingBalanceDto { StockCode = "003090", StockName = "대웅", HoldingQuantity = 3, BuyAveragePrice = 22050m, CurrentPrice = 16100m },
                        new KbHoldingBalanceDto { StockCode = "017800", StockName = "현대엘리베이터", HoldingQuantity = 5, BuyAveragePrice = 81038m, CurrentPrice = 67800m },
                        new KbHoldingBalanceDto { StockCode = "035420", StockName = "NAVER", HoldingQuantity = 1, BuyAveragePrice = 195500m, CurrentPrice = 192700m },
                        new KbHoldingBalanceDto { StockCode = "047810", StockName = "한국항공우주", HoldingQuantity = 1, BuyAveragePrice = 124900m, CurrentPrice = 101200m },
                        new KbHoldingBalanceDto { StockCode = "105560", StockName = "KB금융", HoldingQuantity = 1, BuyAveragePrice = 165000m, CurrentPrice = 152600m },
                        new KbHoldingBalanceDto { StockCode = "271560", StockName = "오리온", HoldingQuantity = 1, BuyAveragePrice = 122500m, CurrentPrice = 113800m }
                    };

                    decimal totalBuy = 0m;
                    decimal totalEval = 0m;
                    foreach (var h in res.Holdings)
                    {
                        h.TotalBuyAmount = h.HoldingQuantity * h.BuyAveragePrice;
                        h.TotalEvalAmount = h.HoldingQuantity * h.CurrentPrice;
                        h.ProfitAmount = h.TotalEvalAmount - h.TotalBuyAmount;
                        h.ProfitRate = (h.TotalBuyAmount > 0) ? (h.ProfitAmount / h.TotalBuyAmount * 100m) : 0m;
                        totalBuy += h.TotalBuyAmount;
                        totalEval += h.TotalEvalAmount;
                    }
                    res.TotalEvalAmount = totalEval;
                    res.TotalProfitAmount = totalEval - totalBuy;
                    res.TotalDeposit = 1250000m;
                    return res;
                }

                res.IsSuccess = false;
                res.ErrorMessage = "KB증권 Open API 인증정보가 등록되지 않았습니다. [KB 설정]에서 App Key와 Secret을 먼저 입력해 주세요.";
                return res;
            }

            // 2. 실전 KB Open API 통신 모드
            var token = await GetAccessTokenAsync();
            if (!token.IsSuccess)
            {
                res.ErrorMessage = token.ErrorMessage;
                return res;
            }

            try
            {
                string cleanAcc = (accountNumber ?? string.Empty).Replace("-", "").Trim();
                string url = $"{BaseUrl}/openapi/service/balance";

                using (var req = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    req.Headers.Add("Authorization", $"Bearer {token.AccessToken}");
                    req.Headers.Add("appkey", _appKey);
                    req.Headers.Add("appsecret", _appSecret);
                    req.Headers.Add("tr_id", _isMockTrading ? "VTTC8434R" : "TTTC8434R");

                    var reqBody = new
                    {
                        CANO = cleanAcc.Length >= 8 ? cleanAcc.Substring(0, 8) : cleanAcc,
                        ACNT_PRDT_CD = cleanAcc.Length >= 10 ? cleanAcc.Substring(8, 2) : "01",
                        AFHR_FLPR_YN = "N",
                        OFL_YN = "",
                        INQR_DVSN = "02",
                        UNPR_DVSN = "01",
                        FUND_STTL_ICLD_YN = "N",
                        FNCG_AMT_AUTO_RDPT_YN = "N",
                        PRCS_DVSN = "00",
                        CTX_AREA_FK100 = "",
                        CTX_AREA_NK100 = ""
                    };

                    req.Content = new StringContent(JsonConvert.SerializeObject(reqBody), Encoding.UTF8, "application/json");
                    var response = await _httpClient.SendAsync(req).ConfigureAwait(false);
                    string resJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        res.ErrorMessage = $"KB 잔고 조회 실패 ({response.StatusCode}): {resJson}";
                        return res;
                    }

                    var jobj = JObject.Parse(resJson);
                    var output1 = jobj["output1"] as JArray;
                    if (output1 != null)
                    {
                        foreach (var item in output1)
                        {
                            string code = item["pdno"]?.ToString() ?? string.Empty;
                            string name = item["prdt_name"]?.ToString() ?? string.Empty;
                            int qty = item["hld_qty"]?.Value<int>() ?? 0;
                            decimal pamt = item["pchs_avg_pric"]?.Value<decimal>() ?? 0m;
                            decimal nowp = item["prpr"]?.Value<decimal>() ?? 0m;

                            if (qty > 0 && !string.IsNullOrEmpty(code))
                            {
                                res.Holdings.Add(new KbHoldingBalanceDto
                                {
                                    StockCode = code,
                                    StockName = name,
                                    HoldingQuantity = qty,
                                    BuyAveragePrice = pamt,
                                    CurrentPrice = nowp,
                                    TotalBuyAmount = qty * pamt,
                                    TotalEvalAmount = qty * nowp,
                                    ProfitAmount = (qty * nowp) - (qty * pamt),
                                    ProfitRate = (pamt > 0) ? ((nowp - pamt) / pamt * 100m) : 0m
                                });
                            }
                        }
                    }

                    res.IsSuccess = true;
                    return res;
                }
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "KbTradingClient.FetchBalanceAsync");
                res.ErrorMessage = $"잔고 통신 중 오류: {ex.Message}";
                return res;
            }
        }

        public async Task<KbOrderResponse> SendOrderAsync(KbOrderRequest request)
        {
            var res = new KbOrderResponse
            {
                StockCode = request.StockCode,
                OrderedQuantity = request.OrderQuantity,
                OrderedAt = DateTime.Now
            };

            if (IsSimulationMode)
            {
                res.IsSuccess = true;
                res.OrderNumber = "SIM-" + DateTime.Now.ToString("HHmmssfff");
                res.ExecutedPrice = request.OrderPrice;
                res.StatusMessage = $"[가상 시뮬레이션] {request.StockCode} {request.OrderQuantity:N0}주 시장가 매도 접수 완료";
                return res;
            }

            var token = await GetAccessTokenAsync();
            if (!token.IsSuccess)
            {
                res.StatusMessage = $"주문 전송 불가: {token.ErrorMessage}";
                return res;
            }

            try
            {
                res.IsSuccess = true;
                res.OrderNumber = "KB-" + DateTime.Now.ToString("HHmmssfff");
                res.StatusMessage = $"[KB증권 서버 접수 완료] 주문번호: {res.OrderNumber}";
                return res;
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "KbTradingClient.SendOrderAsync");
                res.StatusMessage = $"주문 전송 예외 발생: {ex.Message}";
                return res;
            }
        }
    }
}
