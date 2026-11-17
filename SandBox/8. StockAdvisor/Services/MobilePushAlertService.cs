using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GateHelper.StockAdvisor.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GateHelper.StockAdvisor.Services
{
    /// <summary>
    /// 텔레그램 봇 및 디스코드 웹훅을 통해 PC 주식 알림을 스마트폰으로 즉시 푸시 전송하는 비동기 서비스
    /// (Zero-Resource: 정적 HttpClient 재사용, UI 블로킹 0ms, 0B 디스크 I/O)
    /// </summary>
    public static class MobilePushAlertService
    {
        private static readonly HttpClient _httpClient;
        private static long _lastUpdateId = 0;
        private static bool _isOffsetInitialized = false;
        private static bool _isPolling = false;

        static MobilePushAlertService()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(6)
            };
        }

        /// <summary>
        /// 텔레그램 봇의 최근 대화(getUpdates)에서 사용자의 Chat ID를 자동으로 탐지합니다.
        /// </summary>
        public static async Task<(bool Ok, string ChatId, string DisplayName, string Error)> DetectTelegramChatIdAsync(string botToken)
        {
            if (string.IsNullOrWhiteSpace(botToken))
            {
                return (false, string.Empty, string.Empty, "텔레그램 API 토큰을 먼저 입력하세요.");
            }

            try
            {
                string url = $"https://api.telegram.org/bot{botToken.Trim()}/getUpdates";
                var resp = await _httpClient.GetAsync(url).ConfigureAwait(false);
                string body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!resp.IsSuccessStatusCode)
                {
                    return (false, string.Empty, string.Empty, $"토큰 인증 실패 (HTTP {(int)resp.StatusCode}) — API 토큰을 다시 확인하세요.");
                }

                var root = JObject.Parse(body);
                if (root["ok"]?.Value<bool>() != true)
                {
                    return (false, string.Empty, string.Empty, "텔레그램 API 응답 오류");
                }

                var results = root["result"] as JArray;
                if (results == null || results.Count == 0)
                {
                    return (false, string.Empty, string.Empty, "대화 내역이 없습니다. 텔레그램에서 내 봇에게 '안녕'이라고 1번 말을 걸어준 뒤 다시 눌러주세요.");
                }

                // 가장 최근 메시지부터 역순으로 chat.id 탐색
                for (int i = results.Count - 1; i >= 0; i--)
                {
                    var item = results[i];
                    var chat = item["message"]?["chat"] ?? item["edited_message"]?["chat"] ?? item["my_chat_member"]?["chat"];
                    if (chat != null && chat["id"] != null)
                    {
                        string chatId = chat["id"].ToString();
                        string name = chat["first_name"]?.ToString() ?? chat["username"]?.ToString() ?? "내 텔레그램";
                        return (true, chatId, name, string.Empty);
                    }
                }

                return (false, string.Empty, string.Empty, "유효한 채팅방 ID를 찾지 못했습니다. 봇에게 메시지를 1번 보내주세요.");
            }
            catch (Exception ex)
            {
                return (false, string.Empty, string.Empty, $"통신 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 텔레그램으로 메시지를 발송합니다.
        /// </summary>
        public static async Task<(bool Ok, string Error)> SendTelegramAsync(string botToken, string chatId, string text)
        {
            if (string.IsNullOrWhiteSpace(botToken) || string.IsNullOrWhiteSpace(chatId))
            {
                return (false, "텔레그램 토큰 또는 Chat ID가 비어 있습니다.");
            }

            try
            {
                string url = $"https://api.telegram.org/bot{botToken.Trim()}/sendMessage";
                var payload = new
                {
                    chat_id = chatId.Trim(),
                    text = text,
                    disable_web_page_preview = true
                };
                string json = JsonConvert.SerializeObject(payload);
                using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                {
                    var resp = await _httpClient.PostAsync(url, content).ConfigureAwait(false);
                    if (!resp.IsSuccessStatusCode)
                    {
                        string errBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                        return (false, $"HTTP {(int)resp.StatusCode}: {errBody}");
                    }
                }
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public static string BuildNaverMobileUrl(string stockCode)
        {
            if (string.IsNullOrWhiteSpace(stockCode)) return string.Empty;
            string clean = stockCode.Trim().PadLeft(6, '0');
            return $"https://m.stock.naver.com/domestic/stock/{clean}/total";
        }

        /// <summary>
        /// 실시간 주식 알림을 설정된 텔레그램 봇으로 비동기 전송합니다. (모바일 네이버 증권 바로가기 링크 포함)
        /// </summary>
        public static async Task DispatchStockAlertAsync(PortfolioData portfolio, StockAlertLogItem alertItem)
        {
            if (portfolio == null || !portfolio.EnableMobilePushAlert || alertItem == null) return;

            try
            {
                string badge = GetAlertLabel(alertItem.AlertType);
                string sign = alertItem.ChangeRate >= 0 ? "+" : "";
                string link = BuildNaverMobileUrl(alertItem.StockCode);
                string linkLine = !string.IsNullOrEmpty(link) ? $"\n👉 네이버 증권: {link}" : "";

                string msg = string.IsNullOrWhiteSpace(alertItem.Message)
                    ? $"{badge}\n" +
                      $"• 종목: {alertItem.StockName} ({alertItem.StockCode})\n" +
                      $"• 현재가: {alertItem.CurrentPrice:N0}원 ({sign}{alertItem.ChangeRate:F2}%)\n" +
                      $"• 기준가: {alertItem.TriggerPrice:N0}원\n" +
                      $"• 시각: {alertItem.AlertTime:HH:mm:ss}{linkLine}"
                    : $"{badge}\n" +
                      $"• 종목: {alertItem.StockName} ({alertItem.StockCode})\n" +
                      $"• 현재가: {alertItem.CurrentPrice:N0}원 ({sign}{alertItem.ChangeRate:F2}%)\n" +
                      $"• 내용: {alertItem.Message}{linkLine}";

                if (!string.IsNullOrWhiteSpace(portfolio.TelegramBotToken) && !string.IsNullOrWhiteSpace(portfolio.TelegramChatId))
                {
                    await SendTelegramAsync(portfolio.TelegramBotToken, portfolio.TelegramChatId, msg).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "MobilePushAlertService.DispatchStockAlertAsync");
            }
        }

        /// <summary>
        /// 텔레그램 봇으로 들어온 원격 조회 명령어(/잔고, /추천, /체온, 종목명)를 경량 폴링하여 응답합니다.
        /// (인가된 본인 Chat ID의 메시지에만 응답하여 계좌 정보 보안 100% 유지)
        /// </summary>
        public static async Task PollAndHandleTelegramCommandsAsync(PortfolioData portfolio, Func<string, string> commandHandler)
        {
            if (_isPolling || portfolio == null || !portfolio.EnableMobilePushAlert || commandHandler == null) return;
            if (string.IsNullOrWhiteSpace(portfolio.TelegramBotToken) || string.IsNullOrWhiteSpace(portfolio.TelegramChatId)) return;

            _isPolling = true;
            try
            {
                string token = portfolio.TelegramBotToken.Trim();
                string authorizedChatId = portfolio.TelegramChatId.Trim();
                string url = _isOffsetInitialized
                    ? $"https://api.telegram.org/bot{token}/getUpdates?offset={_lastUpdateId + 1}&timeout=0"
                    : $"https://api.telegram.org/bot{token}/getUpdates?timeout=0";

                var resp = await _httpClient.GetAsync(url).ConfigureAwait(false);
                if (!resp.IsSuccessStatusCode) return;

                string body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                var root = JObject.Parse(body);
                if (root["ok"]?.Value<bool>() != true) return;

                var results = root["result"] as JArray;
                if (results == null || results.Count == 0)
                {
                    _isOffsetInitialized = true;
                    return;
                }

                // 최초 1회 실행 시 기존에 쌓여있던 과거 메시지는 재응답하지 않고 최신 offset만 동기화
                if (!_isOffsetInitialized)
                {
                    long maxId = 0;
                    foreach (var itm in results)
                    {
                        long uid = itm["update_id"]?.Value<long>() ?? 0;
                        if (uid > maxId) maxId = uid;
                    }
                    _lastUpdateId = maxId;
                    _isOffsetInitialized = true;
                    return;
                }

                foreach (var itm in results)
                {
                    long uid = itm["update_id"]?.Value<long>() ?? 0;
                    if (uid > _lastUpdateId) _lastUpdateId = uid;

                    var msg = itm["message"];
                    if (msg == null) continue;

                    string chatId = msg["chat"]?["id"]?.ToString()?.Trim();
                    string text = msg["text"]?.ToString()?.Trim();
                    if (string.IsNullOrEmpty(text) || !string.Equals(chatId, authorizedChatId, StringComparison.Ordinal))
                    {
                        continue; // 인가된 내 채팅방이 아니면 보안상 무시
                    }

                    string reply = commandHandler(text);
                    if (!string.IsNullOrWhiteSpace(reply))
                    {
                        await SendTelegramAsync(token, authorizedChatId, reply).ConfigureAwait(false);
                    }
                }
            }
            catch
            {
                // 폴링 네트워크 일시 오류는 조용히 무시하여 백그라운드 무부하 유지
            }
            finally
            {
                _isPolling = false;
            }
        }

        private static string GetAlertLabel(ToastAlertType type)
        {
            switch (type)
            {
                case ToastAlertType.TargetHit: return "🎯 [목표가 도달 / 익절 권고]";
                case ToastAlertType.StopHit: return "🚨 [손절가 이탈 / 리스크 방어]";
                case ToastAlertType.BuyHit: return "⚡ [적정 매수가 도달]";
                case ToastAlertType.TargetNear: return "📈 [목표가 1.5% 도달 임박]";
                case ToastAlertType.StopNear: return "⚠️ [손절가 1.5% 접근 경고]";
                case ToastAlertType.BuyNear: return "👀 [매수가 1.5% 접근 임박]";
                case ToastAlertType.TargetChanged: return "🔄 [AI 익절가 변경 — MTS 정정 권고]";
                case ToastAlertType.StopChanged: return "🔄 [AI 손절가 변경 — MTS 정정 권고]";
                default: return "🔔 [주식 실시간 알림]";
            }
        }
    }
}
