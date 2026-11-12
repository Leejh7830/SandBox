using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using GateHelper.StockAdvisor.Models;

namespace GateHelper.StockAdvisor
{
    /// <summary>
    /// 목표가 도달, 손절선 터치, AI 적정 매수가 진입 알림 기록을 한눈에 조회하고
    /// 선택 종목으로 바로 이동할 수 있는 알림 기록 다이얼로그 (Zero-Resource, 0B 디스크 I/O)
    /// </summary>
    public class StockAlertHistoryDialog : Form
    {
        private readonly List<StockAlertLogItem> _history;
        private readonly Action<string> _onSelectStock;
        private ListView _lvHistory;
        private Label _lblSummary;
        private Button _btnNavigate;
        private Button _btnClear;
        private Button _btnClose;

        private static readonly Font BoldFont = new Font("Segoe UI", 9.2f, FontStyle.Bold);

        public StockAlertHistoryDialog(List<StockAlertLogItem> history, Action<string> onSelectStock)
        {
            _history = history ?? new List<StockAlertLogItem>();
            _onSelectStock = onSelectStock;

            this.Text = "가격 도달 알림 기록 내역 (Price Alert History)";
            this.Size = new Size(860, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(18, 22, 32);
            this.ForeColor = Color.White;
            this.Font = new Font("Malgun Gothic", 9f);
            this.MinimumSize = new Size(750, 420);

            InitializeCustomUi();
            LoadHistoryItems();
        }

        private void InitializeCustomUi()
        {
            // 1. 상단 헤더
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Color.FromArgb(24, 28, 40),
                Padding = new Padding(16, 8, 16, 6)
            };

            var lblTitle = new Label
            {
                Text = "★ 가격 도달 알림 기록 내역 (Price Alert History)",
                Font = new Font("Malgun Gothic", 11.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 190, 40),
                Dock = DockStyle.Top,
                Height = 24
            };

            _lblSummary = new Label
            {
                Text = "보유종목의 목표가/손절가 및 관심종목의 AI 매수가 도달 알림 히스토리입니다. 항목을 더블클릭하면 해당 종목으로 즉시 이동합니다.",
                Font = new Font("Malgun Gothic", 8.8f),
                ForeColor = Color.FromArgb(170, 190, 220),
                Dock = DockStyle.Top,
                Height = 20
            };

            pnlHeader.Controls.Add(_lblSummary);
            pnlHeader.Controls.Add(lblTitle);

            // 2. 리스트뷰
            _lvHistory = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BackColor = Color.FromArgb(18, 20, 26),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.2f),
                BorderStyle = BorderStyle.None,
                ShowItemToolTips = true
            };

            _lvHistory.Columns.Add("시간", 75, HorizontalAlignment.Center);
            _lvHistory.Columns.Add("알림 구분", 125, HorizontalAlignment.Center);
            _lvHistory.Columns.Add("코드", 65, HorizontalAlignment.Center);
            _lvHistory.Columns.Add("종목명", 120);
            _lvHistory.Columns.Add("현재가", 85, HorizontalAlignment.Right);
            _lvHistory.Columns.Add("감지가격", 85, HorizontalAlignment.Right);
            _lvHistory.Columns.Add("등락률", 75, HorizontalAlignment.Right);
            _lvHistory.Columns.Add("AI 대응 권고 및 메시지", 200);

            // 더블 버퍼링
            typeof(ListView).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
                null, _lvHistory, new object[] { true });

            _lvHistory.DoubleClick += (s, e) => NavigateToSelectedStock();

            // 3. 하단 푸터 바
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 46,
                BackColor = Color.FromArgb(24, 28, 40),
                Padding = new Padding(16, 7, 16, 7)
            };

            _btnNavigate = new Button
            {
                Text = "선택 종목 차트 이동",
                Font = new Font("Malgun Gothic", 9f, FontStyle.Bold),
                Size = new Size(135, 32),
                Dock = DockStyle.Left,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(18, 56, 74),
                ForeColor = Color.FromArgb(0, 220, 255),
                Cursor = Cursors.Hand
            };
            _btnNavigate.FlatAppearance.BorderColor = Color.FromArgb(0, 180, 220);
            _btnNavigate.Click += (s, e) => NavigateToSelectedStock();

            _btnClose = new Button
            {
                Text = "닫기",
                Font = new Font("Malgun Gothic", 9f),
                Size = new Size(76, 32),
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(32, 38, 52),
                ForeColor = Color.FromArgb(200, 215, 230),
                Cursor = Cursors.Hand
            };
            _btnClose.FlatAppearance.BorderColor = Color.FromArgb(60, 75, 100);
            _btnClose.Click += (s, e) => this.Close();

            _btnClear = new Button
            {
                Text = "기록 비우기",
                Font = new Font("Malgun Gothic", 8.8f),
                Size = new Size(88, 32),
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(42, 24, 28),
                ForeColor = Color.FromArgb(255, 140, 150),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 8, 0)
            };
            _btnClear.FlatAppearance.BorderColor = Color.FromArgb(140, 50, 60);
            _btnClear.Click += (s, e) =>
            {
                _history.Clear();
                LoadHistoryItems();
            };

            pnlFooter.Controls.Add(_btnNavigate);
            pnlFooter.Controls.Add(_btnClear);
            pnlFooter.Controls.Add(_btnClose);

            this.Controls.Add(_lvHistory);
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlFooter);
        }

        private void LoadHistoryItems()
        {
            _lvHistory.BeginUpdate();
            _lvHistory.Items.Clear();

            int targetCount = 0;
            int stopCount = 0;
            int buyCount = 0;

            // 최신 알림이 맨 위에 오도록 역순 나열
            for (int i = _history.Count - 1; i >= 0; i--)
            {
                var item = _history[i];
                item.IsRead = true;

                string typeStr = "목표가 달성";
                Color typeColor = Color.FromArgb(0, 230, 118);

                if (item.AlertType == ToastAlertType.TargetHit)
                {
                    typeStr = "★ 목표가 달성 (익절)";
                    typeColor = Color.FromArgb(0, 230, 118);
                    targetCount++;
                }
                else if (item.AlertType == ToastAlertType.StopHit)
                {
                    typeStr = "★ 손절선 터치 (방어)";
                    typeColor = Color.FromArgb(255, 75, 85);
                    stopCount++;
                }
                else if (item.AlertType == ToastAlertType.BuyHit)
                {
                    typeStr = "★ AI 매수가 도달 (진입)";
                    typeColor = Color.FromArgb(0, 216, 255);
                    buyCount++;
                }

                var lvi = new ListViewItem(new[]
                {
                    item.AlertTime.ToString("HH:mm:ss"),
                    typeStr,
                    item.StockCode,
                    item.StockName,
                    $"{item.CurrentPrice:N0}원",
                    $"{item.TriggerPrice:N0}원",
                    $"{item.ChangeRate:+0.00;-0.00;0.00}%",
                    item.Message
                });

                lvi.Tag = item.StockCode;
                lvi.SubItems[0].ForeColor = Color.FromArgb(140, 160, 190);
                lvi.SubItems[1].ForeColor = typeColor;
                lvi.SubItems[1].Font = BoldFont;
                lvi.SubItems[2].ForeColor = Color.FromArgb(150, 170, 200);
                lvi.SubItems[3].ForeColor = Color.FromArgb(235, 245, 255);
                lvi.SubItems[3].Font = BoldFont;

                if (item.ChangeRate > 0) lvi.SubItems[6].ForeColor = Color.FromArgb(255, 95, 95);
                else if (item.ChangeRate < 0) lvi.SubItems[6].ForeColor = Color.FromArgb(85, 160, 255);
                else lvi.SubItems[6].ForeColor = Color.FromArgb(180, 190, 205);

                lvi.SubItems[7].ForeColor = Color.FromArgb(200, 220, 240);

                lvi.UseItemStyleForSubItems = false;
                _lvHistory.Items.Add(lvi);
            }

            _lvHistory.EndUpdate();

            _lblSummary.Text = $"총 {_history.Count}건의 알림 기록 (목표가 {targetCount}건 | 손절가 {stopCount}건 | 적정매수가 {buyCount}건)";
            _btnNavigate.Enabled = _lvHistory.Items.Count > 0;
            _btnClear.Enabled = _lvHistory.Items.Count > 0;

            if (_lvHistory.Items.Count > 0)
            {
                _lvHistory.Items[0].Selected = true;
            }
        }

        private void NavigateToSelectedStock()
        {
            if (_lvHistory.SelectedItems.Count == 0) return;
            string code = _lvHistory.SelectedItems[0].Tag as string;
            if (!string.IsNullOrEmpty(code))
            {
                this.Close();
                _onSelectStock?.Invoke(code);
            }
        }
    }
}
