using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GateHelper.StockAdvisor.Models;

namespace GateHelper.StockAdvisor
{
    /// <summary>
    /// 포트폴리오 내 요주의/핵심 주도주/최대 비중 종목을 1클릭으로 브리핑하는 중요 종목 인텔 다이얼로그
    /// (S, A, B, C, D, F 6대 등급 및 4색 신호등 체계 적용)
    /// </summary>
    public class KeyStockSummaryDialog : Form
    {
        private readonly List<StockHoldingItem> _items;
        private readonly Action<string> _onSelectStock;
        private TabControl _tabControl;
        private static readonly Font BoldFont = new Font("Segoe UI", 9f, FontStyle.Bold);

        public KeyStockSummaryDialog(List<StockHoldingItem> items, Action<string> onSelectStock = null)
        {
            _items = items ?? new List<StockHoldingItem>();
            _onSelectStock = onSelectStock;
            InitializeCustomComponent();
            PopulateData();
            this.Shown += (s, e) =>
            {
                foreach (TabPage tp in _tabControl.TabPages)
                {
                    foreach (Control c in tp.Controls)
                    {
                        if (c is ListView lv)
                        {
                            lv.SelectedItems.Clear();
                            if (lv.FocusedItem != null) lv.FocusedItem.Focused = false;
                        }
                    }
                }
            };
        }

        private void InitializeCustomComponent()
        {
            this.Text = "[핵심 포커스] 내 포트폴리오 중요 종목 종합 브리핑";
            this.Size = new Size(840, 580);
            this.MinimumSize = new Size(780, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(18, 22, 32);
            this.ForeColor = Color.White;
            this.Font = new Font("Malgun Gothic", 9f);
            this.ShowIcon = false;

            // 1. 상단 요약 배너 패널
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 84,
                BackColor = Color.FromArgb(24, 30, 44),
                Padding = new Padding(16, 12, 16, 12)
            };

            var lblTitle = new Label
            {
                Text = "포트폴리오 핵심/요주의 종목 종합 브리핑 (Key Stock Focus)",
                Font = new Font("Malgun Gothic", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 215, 64),
                Dock = DockStyle.Top,
                Height = 24
            };

            int countA = _items.Count(x => x.Grade == PositionGrade.A);
            int countB = _items.Count(x => x.Grade == PositionGrade.B);
            int countC = _items.Count(x => x.Grade == PositionGrade.C);
            int countF = _items.Count(x => x.Grade == PositionGrade.F);
            int countHold = _items.Count(x => !x.IsWatchlist);
            int countWatch = _items.Count(x => x.IsWatchlist);

            var lblSummary = new Label
            {
                Text = $"• 보유: {countHold}개 | 관심: {countWatch}개  |  " +
                       $"[초록:안전] A: {countA}개  |  " +
                       $"[노랑:주의] B: {countB}개  |  " +
                       $"[주황:경고] C: {countC}개  |  " +
                       $"[빨강:위험] F: {countF}개",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(210, 225, 245),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };

            pnlTop.Controls.Add(lblSummary);
            pnlTop.Controls.Add(lblTitle);

            // 2. 하단 닫기 버튼 바
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                BackColor = Color.FromArgb(20, 24, 34),
                Padding = new Padding(16, 6, 16, 6)
            };

            var btnClose = new Button
            {
                Text = "닫기",
                Size = new Size(95, 34),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(38, 46, 62),
                ForeColor = Color.FromArgb(220, 230, 245),
                Cursor = Cursors.Hand,
                Font = new Font("Malgun Gothic", 9.2f)
            };
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(70, 85, 110);
            btnClose.FlatAppearance.BorderSize = 1;
            btnClose.Click += (s, e) => this.Close();

            void LayoutBottom()
            {
                btnClose.Location = new Point(pnlBottom.Width - btnClose.Width - 16, 7);
            }
            pnlBottom.Resize += (s, e) => LayoutBottom();
            LayoutBottom();
            pnlBottom.Controls.Add(btnClose);
            this.CancelButton = btnClose;

            // 3. 중앙 탭 컨트롤
            _tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Padding = new Point(14, 6),
                Font = new Font("Malgun Gothic", 9.2f, FontStyle.Bold)
            };

            this.Controls.Add(_tabControl);
            this.Controls.Add(pnlBottom);
            this.Controls.Add(pnlTop);
        }

        private void PopulateData()
        {
            // 탭 1: 즉각 주의 / 리스크 경계 종목 (C, F 등급 또는 과열 RSI >= 70% 또는 손실률 <= -7%)
            var alertItems = _items
                .Where(x => x.Grade == PositionGrade.C || x.Grade == PositionGrade.F || x.Rsi >= 70m || x.ProfitRate <= -7m)
                .OrderBy(x => x.Grade)
                .ThenBy(x => x.ProfitRate)
                .ToList();

            var tabAlert = new TabPage($"[경계/주의 종목] ({alertItems.Count})");
            tabAlert.BackColor = Color.FromArgb(18, 22, 32);
            tabAlert.Controls.Add(CreateListView(alertItems, true));
            _tabControl.TabPages.Add(tabAlert);

            // 탭 2: 핵심 유망 / 주도주 (A 등급 또는 종합점수 >= 8)
            var leaderItems = _items
                .Where(x => x.Grade == PositionGrade.A || x.FinalScore >= 8)
                .OrderByDescending(x => x.FinalScore)
                .ToList();

            var tabLeader = new TabPage($"[핵심 유망/주도주] ({leaderItems.Count})");
            tabLeader.BackColor = Color.FromArgb(18, 22, 32);
            tabLeader.Controls.Add(CreateListView(leaderItems, false));
            _tabControl.TabPages.Add(tabLeader);

            // 탭 3: 포트폴리오 비중 Top 5 종목 (보유 종목 대상)
            var topItems = _items
                .Where(x => !x.IsWatchlist)
                .OrderByDescending(x => x.TotalEvalAmount)
                .Take(5)
                .ToList();

            var tabTop = new TabPage($"[자산 비중 Top 종목] ({topItems.Count})");
            tabTop.BackColor = Color.FromArgb(18, 22, 32);
            tabTop.Controls.Add(CreateListView(topItems, false));
            _tabControl.TabPages.Add(tabTop);
        }

        private ListView CreateListView(List<StockHoldingItem> list, bool isAlertMode)
        {
            var lv = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BackColor = Color.FromArgb(16, 20, 28),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f),
                BorderStyle = BorderStyle.None
            };

            lv.Columns.Add("종목명", 125);
            lv.Columns.Add("코드", 62, HorizontalAlignment.Center);
            lv.Columns.Add("현재가", 75, HorizontalAlignment.Right);
            lv.Columns.Add("수익률", 68, HorizontalAlignment.Right);
            lv.Columns.Add("AI 등급", 55, HorizontalAlignment.Center);
            lv.Columns.Add("수급", 85, HorizontalAlignment.Center);
            lv.Columns.Add("지뢰검증", 85, HorizontalAlignment.Center);
            lv.Columns.Add("14일 RSI", 60, HorizontalAlignment.Right);
            lv.Columns.Add("종합점수", 70, HorizontalAlignment.Center);
            lv.Columns.Add("타이밍 안전벨트", 110, HorizontalAlignment.Center);
            lv.Columns.Add("권고 지침 및 핵심 사유", 260);

            typeof(ListView).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
                null, lv, new object[] { true });

            lv.DoubleClick += (s, e) =>
            {
                if (lv.SelectedItems.Count > 0 && lv.SelectedItems[0].Tag is StockHoldingItem sel && !string.IsNullOrEmpty(sel.StockCode))
                {
                    this.Close();
                    _onSelectStock?.Invoke(sel.StockCode);
                }
            };

            foreach (var item in list)
            {
                string rsiStr = item.Rsi > 0 ? $"{item.Rsi:F1}" : "-";
                string scoreStr = item.FinalScore > 0 ? $"{item.FinalScore}점" : "-";
                string supplyStr = !string.IsNullOrEmpty(item.MajorSupplyBadge) ? item.MajorSupplyBadge : "수급대기";
                var landmineInfo = LandmineHelper.GetSimplifiedBadge(item);
                string actionStr = item.LastDiagnosis?.RecommendedAction;
                if (string.IsNullOrEmpty(actionStr))
                {
                    actionStr = item.Rsi >= 70m ? "[과열주의] 추격매수 금지" : (item.FinalScore >= 8 ? "[골든] 보유 및 분할 진입" : "진단 대기");
                }

                string rateStr = item.IsWatchlist ? "[관심]" : $"{item.ProfitRate:+0.00;-0.00}%";

                var lvi = new ListViewItem(new[]
                {
                    item.StockName,
                    item.StockCode,
                    $"{item.CurrentPrice:N0}원",
                    rateStr,
                    item.Grade.ToString(),
                    supplyStr,
                    landmineInfo.Text,
                    rsiStr,
                    scoreStr,
                    item.TimingBadge,
                    actionStr
                });

                lvi.Tag = item;

                // 수익률 색상 (SubItem 3)
                if (item.IsWatchlist) lvi.SubItems[3].ForeColor = Color.FromArgb(0, 216, 255);
                else if (item.ProfitRate > 0) lvi.SubItems[3].ForeColor = Color.FromArgb(255, 95, 95);
                else if (item.ProfitRate < 0) lvi.SubItems[3].ForeColor = Color.FromArgb(85, 160, 255);

                // AI 등급 (SubItem 4)
                lvi.SubItems[4].ForeColor = GradeHelper.GetGradeColor(item.Grade);
                lvi.SubItems[4].Font = BoldFont;

                // 수급 (SubItem 5)
                if (supplyStr.Contains("쌍끌이")) lvi.SubItems[5].ForeColor = Color.FromArgb(0, 240, 210);
                else if (supplyStr.Contains("외인")) lvi.SubItems[5].ForeColor = Color.FromArgb(120, 210, 255);
                else if (supplyStr.Contains("기관")) lvi.SubItems[5].ForeColor = Color.FromArgb(255, 215, 100);
                else if (supplyStr.Contains("개미")) lvi.SubItems[5].ForeColor = Color.FromArgb(210, 170, 255);
                else lvi.SubItems[5].ForeColor = Color.FromArgb(180, 195, 215);

                // 지뢰검증 (SubItem 6: 우수/안전 -> 연초록, 주의 -> 주황, 위험 -> 빨강)
                lvi.SubItems[6].ForeColor = landmineInfo.Color;
                if (landmineInfo.IsBold) lvi.SubItems[6].Font = BoldFont;

                // RSI 색상 (SubItem 7)
                if (item.Rsi >= 70m) lvi.SubItems[7].ForeColor = Color.FromArgb(255, 80, 80);
                else if (item.Rsi <= 30m && item.Rsi > 0) lvi.SubItems[7].ForeColor = Color.FromArgb(80, 180, 255);

                // 종합점수 (SubItem 8)
                if (item.FinalScore >= 8) lvi.SubItems[8].ForeColor = Color.FromArgb(255, 215, 0);
                else if (item.FinalScore <= 3) lvi.SubItems[8].ForeColor = Color.FromArgb(255, 110, 110);

                // 타이밍 뱃지 색상 (SubItem 9)
                string tb = item.TimingBadge ?? string.Empty;
                if (tb.Contains("과열")) lvi.SubItems[9].ForeColor = Color.FromArgb(255, 75, 95);
                else if (tb.Contains("골든")) lvi.SubItems[9].ForeColor = Color.FromArgb(0, 230, 118);
                else if (tb.Contains("눌림목")) lvi.SubItems[9].ForeColor = Color.FromArgb(255, 210, 60);
                else if (tb.Contains("과매도")) lvi.SubItems[9].ForeColor = Color.FromArgb(0, 230, 118);
                else if (tb.Contains("보유불필요")) lvi.SubItems[9].ForeColor = Color.FromArgb(255, 90, 90);

                lvi.UseItemStyleForSubItems = false;
                lv.Items.Add(lvi);
            }

            return lv;
        }
    }
}
