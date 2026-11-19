using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using GateHelper.StockAdvisor.Models;
using GateHelper.StockAdvisor.Services;

namespace GateHelper.StockAdvisor
{
    public partial class StockAdvisorControl
    {
        #region [Telegram Two-Way Remote Query Bot]

        private void SetupTelegramPollTimer()
        {
            _telegramPollTimer = new Timer { Interval = 5000 }; // 기본 5초 경량 폴링 (심야 자동 20초 절전)
            _telegramPollTimer.Tick += async (s, e) =>
            {
                if (_portfolio == null || !_portfolio.EnableMobilePushAlert) return;
                if (string.IsNullOrWhiteSpace(_portfolio.TelegramBotToken) || string.IsNullOrWhiteSpace(_portfolio.TelegramChatId)) return;

                // 심야 시간대(22:00 ~ 06:59)에는 20초 주기로 완화하여 유휴 네트워크 호출 75% 절감
                int hour = DateTime.Now.Hour;
                int targetInterval = (hour >= 7 && hour < 22) ? 5000 : 20000;
                if (_telegramPollTimer != null && _telegramPollTimer.Interval != targetInterval)
                {
                    _telegramPollTimer.Interval = targetInterval;
                }

                await MobilePushAlertService.PollAndHandleTelegramCommandsAsync(_portfolio, HandleTelegramCommand);
            };
            _telegramPollTimer.Start();
        }

        private (List<StockHoldingItem> Items, decimal DepositCash, string MarketThermo, string SyncTimeText) CapturePortfolioSnapshotSafe()
        {
            Func<(List<StockHoldingItem>, decimal, string, string)> capture = () =>
            {
                var items = _portfolio?.Items?.ToList() ?? new List<StockHoldingItem>();
                decimal cash = _portfolio?.DepositCash ?? 0m;
                string thermo = _marketTemperature?.BadgeText ?? "체온 정보 수집 대기 중";
                string syncText = !_lastPriceRefreshSuccessTime.HasValue
                    ? "미동기화"
                    : _lastPriceRefreshSuccessTime.Value.ToString("MM/dd HH:mm");
                return (items, cash, thermo, syncText);
            };

            try
            {
                if (this.IsHandleCreated && !this.IsDisposed && this.InvokeRequired)
                {
                    return ((List<StockHoldingItem>, decimal, string, string))this.Invoke(capture);
                }
                return capture();
            }
            catch
            {
                return (new List<StockHoldingItem>(), 0m, "조회 오류", "미동기화");
            }
        }

        private string HandleTelegramCommand(string rawText)
        {
            string cmd = (rawText ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(cmd)) return string.Empty;

            if (cmd.Equals("/help", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("/start", StringComparison.OrdinalIgnoreCase) ||
                cmd == "/도움" || cmd == "도움" || cmd == "?")
            {
                return "🤖 [SandBox 주식 참모 원격 명령어 안내]\n" +
                       "• /잔고 : 내 보유 계좌 평가손익 및 종목별 현황\n" +
                       "• /추천 : 지금 [매수추천] 뜬 보유·관심 종목 조회\n" +
                       "• /체온 : 현재 시장 체온 및 증시 온도계\n" +
                       "• 종목명 (예: LG전자) : 해당 종목 실시간 시세·AI타점·링크 조회";
            }

            var snapshot = CapturePortfolioSnapshotSafe();
            var itemsSnapshot = snapshot.Items;
            string syncStamp = snapshot.SyncTimeText;

            if (cmd == "/잔고" || cmd == "잔고")
            {
                var holdings = itemsSnapshot.Where(x => !x.IsWatchlist && x.Quantity > 0).ToList();
                decimal totalEval = holdings.Sum(x => x.TotalEvalAmount);
                decimal totalBuy = holdings.Sum(x => x.TotalBuyAmount);
                decimal totalPnl = totalEval - totalBuy;
                decimal totalRate = totalBuy > 0 ? Math.Round(totalPnl / totalBuy * 100m, 2) : 0m;
                decimal cash = snapshot.DepositCash;

                var lines = new List<string>
                {
                    $"💼 [내 계좌 실시간 잔고 브리핑] (기준: {syncStamp})",
                    $"• 총 자산: {(totalEval + cash):N0}원 (예수금: {cash:N0}원)",
                    $"• 주식 평가: {totalEval:N0}원",
                    $"• 평가 손익: {totalPnl:+#,##0;-#,##0;0}원 ({totalRate:+0.00;-0.00}%)",
                    "────────────────────"
                };

                foreach (var h in holdings.OrderByDescending(x => x.TotalEvalAmount))
                {
                    string sign = h.ProfitRate >= 0 ? "+" : "";
                    lines.Add($"• {h.StockName} ({h.Quantity}주): {h.CurrentPrice:N0}원 ({sign}{h.ProfitRate:F2}%)");
                }
                if (holdings.Count == 0) lines.Add("• 현재 보유 중인 주식이 없습니다.");
                return string.Join("\n", lines);
            }

            if (cmd == "/추천" || cmd == "추천")
            {
                var picks = itemsSnapshot
                    .Select(x => new { Item = x, Verdict = StockVerdictHelper.Evaluate(x) })
                    .Where(x => x.Verdict.CanBuy)
                    .OrderByDescending(x => x.Item.FinalScore)
                    .ToList();

                if (picks.Count == 0)
                {
                    return $"🟢 [매수추천 종목 현황] (기준: {syncStamp})\n현재 등록된 종목 중 즉시 매수추천(눌림목·과매도) 타점에 진입한 종목이 없습니다.";
                }

                var lines = new List<string> { $"🟢 [현재 매수추천 종목 ({picks.Count}건)] (기준: {syncStamp})" };
                foreach (var p in picks.Take(10))
                {
                    var itm = p.Item;
                    decimal target = itm.RecommendedTargetPrice > 0 ? itm.RecommendedTargetPrice : itm.ResistancePrice;
                    lines.Add($"• {itm.StockName} ({itm.FinalScore}점/{itm.Grade}등급) | {itm.CurrentPrice:N0}원 | RSI {itm.Rsi:F1} | 목표 {target:N0}원");
                }
                return string.Join("\n", lines);
            }

            if (cmd == "/체온" || cmd == "체온" || cmd == "/시장" || cmd == "시장")
            {
                return $"🌡️ [실시간 시장 체온] (기준: {syncStamp})\n• 현재 상태: {snapshot.MarketThermo}\n• 등록 종목: 보유 {itemsSnapshot.Count(x => !x.IsWatchlist)}개 / 관심 {itemsSnapshot.Count(x => x.IsWatchlist)}개";
            }

            // 종목명 또는 종목코드 검색
            string query = cmd.TrimStart('/');
            var matched = itemsSnapshot.FirstOrDefault(x =>
                x.StockName.Equals(query, StringComparison.OrdinalIgnoreCase) ||
                x.StockCode.TrimStart('0').Equals(query.TrimStart('0'), StringComparison.OrdinalIgnoreCase))
                ?? itemsSnapshot.FirstOrDefault(x => x.StockName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);

            if (matched == null)
            {
                return $"❓ 등록된 보유/관심 종목에서 '{query}'을(를) 찾지 못했습니다.\n(명령어: /잔고, /추천, /체온, /도움)";
            }

            var verdict = StockVerdictHelper.Evaluate(matched);
            string chgSign = matched.ChangeRate >= 0 ? "+" : "";
            decimal tgt = matched.RecommendedTargetPrice > 0 ? matched.RecommendedTargetPrice : matched.ResistancePrice;
            decimal stp = matched.TargetStopPrice > 0 ? matched.TargetStopPrice : (matched.SupportPrice > 0 ? matched.SupportPrice * 0.97m : 0m);
            string link = MobilePushAlertService.BuildNaverMobileUrl(matched.StockCode);

            return $"📊 [{matched.StockName} ({matched.StockCode})] (기준: {syncStamp})\n" +
                   $"• 현재가: {matched.CurrentPrice:N0}원 ({chgSign}{matched.ChangeRate:F2}%)\n" +
                   $"• AI 등급: {matched.Grade}등급 ({matched.FinalScore}점) | 최종액션: [{verdict.BadgeText}]\n" +
                   $"• 권고 매수가: {matched.RecommendedBuyPrice:N0}원\n" +
                   $"• 목표가: {tgt:N0}원 | 손절가: {stp:N0}원\n" +
                   $"• 14일 RSI: {matched.Rsi:F1} | 수급: {matched.MajorSupplyBadge}\n" +
                   $"👉 네이버 증권: {link}";
        }

        #endregion
    }
}
