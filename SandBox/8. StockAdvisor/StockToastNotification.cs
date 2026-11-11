using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace GateHelper.StockAdvisor
{
    public enum ToastAlertType
    {
        TargetHit, // 목표가 도달 (익절 권고)
        StopHit    // 손절 방어선 터치/이탈 (리스크 관리 권고)
    }

    /// <summary>
    /// 보유 종목 목표가 도달 또는 손절가 이탈 시 화면 우측 하단에 부드럽게 표시되는 초경량 다크 네온 토스트 알림
    /// (Zero-Resource: 스마트 자동 종료, 다중 알림 시 위로 자동 스택, 마우스 클릭 시 해당 종목 자동 포커스 이동)
    /// </summary>
    public class StockToastNotification : Form
    {
        private static readonly List<StockToastNotification> _openToasts = new List<StockToastNotification>();
        private readonly Timer _autoCloseTimer;
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
            this.Size = new Size(360, 90);
            this.BackColor = Color.FromArgb(18, 22, 32);
            this.ForeColor = Color.White;
            this.DoubleBuffered = true;
            this.Cursor = Cursors.Hand;

            Color accentColor = alertType == ToastAlertType.TargetHit
                ? Color.FromArgb(0, 230, 118) // 에메랄드 그린
                : Color.FromArgb(255, 75, 85);  // 네온 레드

            string icon = alertType == ToastAlertType.TargetHit ? "🚀" : "⚠️";
            string typeTitle = alertType == ToastAlertType.TargetHit ? "목표가 달성 (분할 익절 검토)" : "손절 방어선 터치 (리스크 관리)";

            // 1. 헤더 (아이콘 + 종목명 + 타이틀)
            var lblTitle = new Label
            {
                Text = $"{icon} [{typeTitle}]\n{stockName} ({stockCode})",
                Font = new Font("Malgun Gothic", 9.2f, FontStyle.Bold),
                ForeColor = accentColor,
                Location = new Point(14, 10),
                Size = new Size(305, 38),
                BackColor = Color.Transparent
            };
            lblTitle.Click += (s, e) => HandleClick();

            // 2. 가격 및 권고 안내 문구
            string priceDetail = alertType == ToastAlertType.TargetHit
                ? $"현재가 {currentPrice:N0}원 | 목표가 {triggerPrice:N0}원 도달 ({changeRate:+0.00;-0.00}%)"
                : $"현재가 {currentPrice:N0}원 | 손절선 {triggerPrice:N0}원 이탈 ({changeRate:+0.00;-0.00}%)";

            var lblBody = new Label
            {
                Text = priceDetail,
                Font = new Font("Malgun Gothic", 8.8f),
                ForeColor = Color.FromArgb(220, 230, 245),
                Location = new Point(14, 52),
                Size = new Size(330, 24),
                BackColor = Color.Transparent
            };
            lblBody.Click += (s, e) => HandleClick();

            // 3. 닫기 버튼
            var btnClose = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(140, 160, 185),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(24, 24),
                Location = new Point(this.Width - 28, 6),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(40, 50, 70);
            btnClose.Click += (s, e) => this.Close();

            this.Controls.Add(btnClose);
            this.Controls.Add(lblTitle);
            this.Controls.Add(lblBody);
            this.Click += (s, e) => HandleClick();

            // 4. 우측 하단 작업표시줄 위 배치 (동시 알림 시 위로 차곡차곡 스택)
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

            // 5. 자동 종료 타이머 (4.5초)
            _autoCloseTimer = new Timer { Interval = 4500 };
            _autoCloseTimer.Tick += (s, e) =>
            {
                _autoCloseTimer.Stop();
                this.Close();
            };

            // 마우스 호버 시 닫힘 일시 정지
            this.MouseEnter += (s, e) => _autoCloseTimer.Stop();
            this.MouseLeave += (s, e) => _autoCloseTimer.Start();
            lblTitle.MouseEnter += (s, e) => _autoCloseTimer.Stop();
            lblTitle.MouseLeave += (s, e) => _autoCloseTimer.Start();
            lblBody.MouseEnter += (s, e) => _autoCloseTimer.Stop();
            lblBody.MouseLeave += (s, e) => _autoCloseTimer.Start();

            this.Shown += (s, e) => _autoCloseTimer.Start();
        }

        private void HandleClick()
        {
            try
            {
                _autoCloseTimer?.Stop();
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
                : Color.FromArgb(255, 75, 85);

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
                _autoCloseTimer?.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// 안전한 토스트 알림 팝업 호출 헬퍼
        /// </summary>
        public static void ShowAlert(
            string stockName,
            string stockCode,
            decimal currentPrice,
            decimal triggerPrice,
            decimal changeRate,
            ToastAlertType alertType,
            Action onClickAction = null)
        {
            try
            {
                var toast = new StockToastNotification(stockName, stockCode, currentPrice, triggerPrice, changeRate, alertType, onClickAction);
                toast.Show();
            }
            catch { }
        }
    }
}
