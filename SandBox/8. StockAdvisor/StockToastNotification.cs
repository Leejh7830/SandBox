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

        // 제로 리소스 정적 폰트 및 GDI+ 브러시/펜 캐시 (토스트 다중 생성 및 OnPaint 시 GC 0B, 핸들 누수 0)
        private static readonly Font TitleFont = new Font("Malgun Gothic", 9.8f, FontStyle.Bold);
        private static readonly Font BodyFont = new Font("Malgun Gothic", 8.4f);
        private static readonly Font ConfirmFont = new Font("Malgun Gothic", 8.2f, FontStyle.Bold);
        private static readonly Font CloseFont = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        private static readonly Pen PenTarget = new Pen(Color.FromArgb(0, 230, 118), 1.5f);
        private static readonly Pen PenBuy = new Pen(Color.FromArgb(0, 216, 255), 1.5f);
        private static readonly Pen PenStop = new Pen(Color.FromArgb(255, 75, 85), 1.5f);
        private static readonly Pen PenNear = new Pen(Color.FromArgb(255, 193, 7), 1.5f);
        private static readonly Pen PenChanged = new Pen(Color.FromArgb(180, 140, 255), 1.5f);

        private static readonly SolidBrush BrushTarget = new SolidBrush(Color.FromArgb(0, 230, 118));
        private static readonly SolidBrush BrushBuy = new SolidBrush(Color.FromArgb(0, 216, 255));
        private static readonly SolidBrush BrushStop = new SolidBrush(Color.FromArgb(255, 75, 85));
        private static readonly SolidBrush BrushNear = new SolidBrush(Color.FromArgb(255, 193, 7));
        private static readonly SolidBrush BrushChanged = new SolidBrush(Color.FromArgb(180, 140, 255));

        private readonly Action _onClickAction;
        private readonly ToastAlertType _alertType;

        public StockToastNotification(
            string stockName,
            string stockCode,
            decimal currentPrice,
            decimal triggerPrice,
            decimal changeRate,
            ToastAlertType alertType,
            Action onClickAction = null,
            string customMessage = null)
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

            Color accentColor;
            string actionStatus;

            switch (alertType)
            {
                case ToastAlertType.TargetHit:
                    accentColor = Color.FromArgb(0, 230, 118);
                    actionStatus = "매도가 도달";
                    break;
                case ToastAlertType.BuyHit:
                    accentColor = Color.FromArgb(0, 216, 255);
                    actionStatus = "매수가 도달";
                    break;
                case ToastAlertType.StopHit:
                    accentColor = Color.FromArgb(255, 75, 85);
                    actionStatus = "손절가 도달";
                    break;
                case ToastAlertType.TargetNear:
                    accentColor = Color.FromArgb(255, 193, 7); // 골드 옐로우
                    actionStatus = "익절가 1.5% 도달임박";
                    break;
                case ToastAlertType.StopNear:
                    accentColor = Color.FromArgb(255, 140, 0); // 오렌지 경보
                    actionStatus = "손절가 1.5% 접근경고";
                    break;
                case ToastAlertType.TargetChanged:
                    accentColor = Color.FromArgb(180, 140, 255); // 네온 퍼플
                    actionStatus = "AI 익절가 변동 알림";
                    break;
                case ToastAlertType.StopChanged:
                    accentColor = Color.FromArgb(255, 110, 160); // 핑크 로즈
                    actionStatus = "AI 손절가 변동 알림";
                    break;
                default:
                    accentColor = Color.FromArgb(0, 216, 255);
                    actionStatus = "가격 알림";
                    break;
            }

            string headline = !string.IsNullOrEmpty(customMessage)
                ? customMessage
                : $"[{stockName} {triggerPrice:N0}원 {actionStatus}]";

            // 1. 헤더 (종목명 + 가격 + 도달여부)
            var lblTitle = new Label
            {
                Text = headline,
                Font = TitleFont,
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
                Font = BodyFont,
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
                Font = ConfirmFont,
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
                Font = CloseFont,
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

            Pen borderPen;
            SolidBrush accentBrush;

            switch (_alertType)
            {
                case ToastAlertType.TargetHit:
                    borderPen = PenTarget;
                    accentBrush = BrushTarget;
                    break;
                case ToastAlertType.BuyHit:
                    borderPen = PenBuy;
                    accentBrush = BrushBuy;
                    break;
                case ToastAlertType.StopHit:
                    borderPen = PenStop;
                    accentBrush = BrushStop;
                    break;
                case ToastAlertType.TargetNear:
                case ToastAlertType.StopNear:
                    borderPen = PenNear;
                    accentBrush = BrushNear;
                    break;
                case ToastAlertType.TargetChanged:
                case ToastAlertType.StopChanged:
                default:
                    borderPen = PenChanged;
                    accentBrush = BrushChanged;
                    break;
            }

            e.Graphics.DrawRectangle(borderPen, 0, 0, this.Width - 1, this.Height - 1);
            e.Graphics.FillRectangle(accentBrush, 0, 0, 4, this.Height);
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
                var toast = new StockToastNotification(stockName, stockCode, currentPrice, triggerPrice, changeRate, alertType, onClickAction, message);
                toast.Show();

                string defaultMsg;
                switch (alertType)
                {
                    case ToastAlertType.BuyHit:
                        defaultMsg = $"[{stockName} {triggerPrice:N0}원 매수가 도달]";
                        break;
                    case ToastAlertType.TargetHit:
                        defaultMsg = $"[{stockName} {triggerPrice:N0}원 매도가 도달]";
                        break;
                    case ToastAlertType.StopHit:
                        defaultMsg = $"[{stockName} {triggerPrice:N0}원 손절가 도달]";
                        break;
                    case ToastAlertType.TargetNear:
                        defaultMsg = $"[{stockName} 익절가 {triggerPrice:N0}원 도달임박(1.5%내)]";
                        break;
                    case ToastAlertType.StopNear:
                        defaultMsg = $"[{stockName} 손절가 {triggerPrice:N0}원 접근경고(1.5%내)]";
                        break;
                    case ToastAlertType.TargetChanged:
                        defaultMsg = $"[{stockName} AI 권고 익절가 {triggerPrice:N0}원으로 변동]";
                        break;
                    case ToastAlertType.StopChanged:
                        defaultMsg = $"[{stockName} AI 권고 손절가 {triggerPrice:N0}원으로 변동]";
                        break;
                    default:
                        defaultMsg = $"[{stockName} 가격 알림]";
                        break;
                }

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
                    Message = message ?? defaultMsg,
                    IsRead = false
                });
            }
            catch { }
        }
    }
}
