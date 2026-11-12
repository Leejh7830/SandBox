using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using GateHelper.StockAdvisor.Models;

namespace GateHelper.StockAdvisor
{
    /// <summary>
    /// 보유 종목 목표가/손절가 또는 관심 종목 적정 매수가 도달 시 화면 우측 하단에 표시되는 다크 네온 토스트 알림
    /// (Sticky 모드: 사용자가 직접 클릭 또는 닫기[✕]할 때까지 화면에 영구 유지되어 중요한 타점을 놓치지 않음)
    /// </summary>
    public class StockToastNotification : Form
    {
        private static readonly List<StockToastNotification> _openToasts = new List<StockToastNotification>();
        public static event Action<StockAlertLogItem> AlertLogged;

        private readonly Action _onClickAction;
        private readonly ToastAlertType _alertType;

        public StockToastNotification(
            string stockName,
            string stockCode,
            decimal currentPrice,
            decimal triggerPrice,
            decimal changeRate,
            ToastAlertType alertType,
            Action onClickAction = null)
        {
            _alertType = alertType;
            _onClickAction = onClickAction;

            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.StartPosition = FormStartPosition.Manual;
            this.Size = new Size(390, 78);
            this.BackColor = Color.FromArgb(16, 20, 30);
            this.ForeColor = Color.White;
            this.DoubleBuffered = true;
            this.Cursor = Cursors.Hand;

            Color accentColor = alertType == ToastAlertType.TargetHit
                ? Color.FromArgb(0, 230, 118) // 에메랄드 그린 (목표가 매도)
                : (alertType == ToastAlertType.BuyHit
                    ? Color.FromArgb(0, 216, 255) // 네온 사이언 (매수 진입)
                    : Color.FromArgb(255, 75, 85));  // 네온 레드 (손절 방어)

            string actionStatus = alertType == ToastAlertType.BuyHit
                ? "매수가 도달"
                : (alertType == ToastAlertType.TargetHit ? "매도가 도달" : "손절가 도달");

            // 사용자 맞춤 포맷: [종목명 + 매수(매도)가격 + 도달 여부] 만 심플하게 노출하여 긴 종목명도 글자 잘림 원천 차단
            string headline = $"[{stockName} {triggerPrice:N0}원 {actionStatus}]";

            // 1. 헤더 (종목명 + 가격 + 도달여부)
            var lblTitle = new Label
            {
                Text = headline,
                Font = new Font("Malgun Gothic", 9.8f, FontStyle.Bold),
                ForeColor = accentColor,
                Location = new Point(14, 12),
                Size = new Size(344, 24),
                BackColor = Color.Transparent,
                AutoEllipsis = true
            };
            lblTitle.Click += (s, e) => HandleClick();

            // 2. 서브 안내: 현재가 및 클릭 차트 이동 가이드
            var lblBody = new Label
            {
                Text = $"현재가: {currentPrice:N0}원 ({changeRate:+0.00;-0.00}%) · 클릭 시 차트 이동",
                Font = new Font("Malgun Gothic", 8.4f),
                ForeColor = Color.FromArgb(170, 190, 215),
                Location = new Point(14, 43),
                Size = new Size(305, 20),
                BackColor = Color.Transparent
            };
            lblBody.Click += (s, e) => HandleClick();

            // 3. 확인 버튼 (영구 대기 확인 후 닫기)
            var btnConfirm = new Button
            {
                Text = "확인",
                Font = new Font("Malgun Gothic", 8.2f, FontStyle.Bold),
                ForeColor = accentColor,
                BackColor = Color.FromArgb(24, 32, 46),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(50, 24),
                Location = new Point(this.Width - 60, 42),
                Cursor = Cursors.Hand
            };
            btnConfirm.FlatAppearance.BorderColor = accentColor;
            btnConfirm.Click += (s, e) => HandleClick();

            // 4. 닫기 버튼 (X)
            var btnClose = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(140, 160, 185),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(22, 22),
                Location = new Point(this.Width - 26, 6),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(40, 50, 70);
            btnClose.Click += (s, e) => this.Close();

            this.Controls.Add(btnConfirm);
            this.Controls.Add(btnClose);
            this.Controls.Add(lblTitle);
            this.Controls.Add(lblBody);
            this.Click += (s, e) => HandleClick();

            // 5. 우측 하단 작업표시줄 위 배치 (동시 알림 시 위로 차곡차곡 스택, 확인 전까지 유지)
            Rectangle workArea = Screen.PrimaryScreen.WorkingArea;
            int offsetIndex = 0;
            lock (_openToasts)
            {
                _openToasts.RemoveAll(t => t.IsDisposed || !t.Visible);
                offsetIndex = Math.Min(_openToasts.Count, 4);
                _openToasts.Add(this);
            }

            int stackY = workArea.Bottom - this.Height - 16 - (offsetIndex * (this.Height + 8));
            int stackX = workArea.Right - this.Width - 16;
            this.Location = new Point(stackX, Math.Max(workArea.Top + 16, stackY));

            this.FormClosed += (s, e) =>
            {
                lock (_openToasts)
                {
                    _openToasts.Remove(this);
                }
            };
        }

        private void HandleClick()
        {
            try
            {
                this.Close();
                _onClickAction?.Invoke();
            }
            catch { }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Color accentColor = _alertType == ToastAlertType.TargetHit
                ? Color.FromArgb(0, 230, 118)
                : (_alertType == ToastAlertType.BuyHit
                    ? Color.FromArgb(0, 216, 255)
                    : Color.FromArgb(255, 75, 85));

            // 네온 테두리
            using (var pen = new Pen(accentColor, 1.5f))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
            }

            // 좌측 인디케이터 바
            using (var brush = new SolidBrush(accentColor))
            {
                e.Graphics.FillRectangle(brush, 0, 0, 4, this.Height);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_openToasts)
                {
                    _openToasts.Remove(this);
                }
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// 안전한 토스트 알림 팝업 호출 헬퍼 (확인 전까지 영구 유지 & 알림 기록 자동 누적)
        /// </summary>
        public static void ShowAlert(
            string stockName,
            string stockCode,
            decimal currentPrice,
            decimal triggerPrice,
            decimal changeRate,
            ToastAlertType alertType,
            Action onClickAction = null,
            string message = null)
        {
            try
            {
                var toast = new StockToastNotification(stockName, stockCode, currentPrice, triggerPrice, changeRate, alertType, onClickAction);
                toast.Show();

                // 히스토리 이벤트 브로드캐스트
                AlertLogged?.Invoke(new StockAlertLogItem
                {
                    AlertTime = DateTime.Now,
                    StockCode = stockCode,
                    StockName = stockName,
                    AlertType = alertType,
                    CurrentPrice = currentPrice,
                    TriggerPrice = triggerPrice,
                    ChangeRate = changeRate,
                    Message = message ?? (alertType == ToastAlertType.BuyHit
                        ? $"[{stockName} {triggerPrice:N0}원 매수가 도달]"
                        : (alertType == ToastAlertType.TargetHit
                            ? $"[{stockName} {triggerPrice:N0}원 매도가 도달]"
                            : $"[{stockName} {triggerPrice:N0}원 손절가 도달]")),
                    IsRead = false
                });
            }
            catch { }
        }
    }
}
