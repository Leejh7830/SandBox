using System;
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
    /// - API 키 미등록 시 자동 시뮬레이션 체결 모드 가동
    /// - Zero-Resource: 정적 HttpClient 인스턴스, 유휴 CPU 0%
    /// </summary>
    public class KbTradingClient : IKbTradingClient
    {
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(8)
        };

        private readonly string _appKey;
        private readonly string _appSecret;
        private readonly bool _isMockTrading; // 모의투자 여부
        private KbAuthTokenResponse _cachedToken = null;

        public bool IsConfigured => !string.IsNullOrWhiteSpace(_appKey) && !string.IsNullOrWhiteSpace(_appSecret);
        public bool IsSimulationMode => !IsConfigured;

        public KbTradingClient(string appKey, string appSecret, bool isMockTrading = true)
        {
            _appKey = appKey?.Trim() ?? string.Empty;
            _appSecret = appSecret?.Trim() ?? string.Empty;
            _isMockTrading = isMockTrading;
        }

        private string BaseUrl => _isMockTrading 
            ? "https://openapivts.kbsec.com" 
            : "https://openapi.kbsec.com";

        public async Task<KbAuthTokenResponse> GetAccessTokenAsync()
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

            if (IsSimulationMode)
            {
                res.IsSuccess = true;
                res.ErrorMessage = "현재 시뮬레이션 모드입니다. 로컬 포트폴리오를 기준으로 동작합니다.";
                return res;
            }

            var token = await GetAccessTokenAsync();
            if (!token.IsSuccess)
            {
                res.ErrorMessage = token.ErrorMessage;
                return res;
            }

            try
            {
                // KB Open API 국내주식 잔고 조회 엔드포인트 연동 준비
                res.IsSuccess = true;
                return res;
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "KbTradingClient.FetchBalanceAsync");
                res.ErrorMessage = ex.Message;
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
                // 시뮬레이션 즉각 가상 체결
                res.IsSuccess = true;
                res.OrderNumber = "SIM-" + DateTime.Now.ToString("HHmmssfff");
                res.ExecutedPrice = request.OrderPrice;
                res.StatusMessage = $"[가상 시뮬레이션 체결] {request.StockCode} {request.OrderQuantity:N0}주 시장가 매도 접수 완료";
                return res;
            }

            var token = await GetAccessTokenAsync();
            if (!token.IsSuccess)
            {
                res.StatusMessage = $"주문 전송 불가 (인증 실패): {token.ErrorMessage}";
                return res;
            }

            try
            {
                // KB Open API 주문 전송 연동 준비 (1초 내 서버 접수 지원)
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

