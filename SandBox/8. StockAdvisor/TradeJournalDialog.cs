using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GateHelper.StockAdvisor.Models;

namespace GateHelper.StockAdvisor
{
    /// <summary>
    /// 매매 복기 및 전략별 승률·누적 손익 통계 일지 다이얼로그
    /// (AI 진단평가와 100% 독립된 별도 데이터 저장소 사용)
    /// </summary>
    public class TradeJournalDialog : Form
    {
        private readonly List<StockHoldingItem> _portfolioItems;
        private List<TradeJournalEntry> _entries;

        private Label _lblStatsMain;
        private Label _lblStatsStrategy;
        private ComboBox _cboFilterStrategy;
        private ListView _lvJournal;
        private Button _btnAdd;
        private Button _btnDelete;
        private Button _btnClose;

        private static readonly Font BoldFont = new Font("Malgun Gothic", 9f, FontStyle.Bold);

        public TradeJournalDialog(List<StockHoldingItem> portfolioItems = null)
        {
            _portfolioItems = portfolioItems ?? new List<StockHoldingItem>();
            _entries = TradeJournalStorage.LoadEntries();

            this.Text = "📓 매매 복기 & 전략 승률 일지 (Trade Journal)";
            this.Size = new Size(1100, 620);
            this.MinimumSize = new Size(920, 480);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(16, 20, 28);
            this.ForeColor = Color.White;
            this.Font = new Font("Malgun Gothic", 9f);
            this.ShowIcon = false;

            InitializeCustomUi();
            RefreshJournalView();
        }

        private void InitializeCustomUi()
        {
            // 1. 상단 통계 대시보드
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 82,
                BackColor = Color.FromArgb(22, 27, 38),
                Padding = new Padding(16, 10, 16, 8)
            };

            var lblTitle = new Label
            {
                Text = "📓 나만의 매매 복기 & 전략별 승률 통계 (AI 진단과 100% 독립 누적)",
                Font = new Font("Malgun Gothic", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 230, 255),
                Dock = DockStyle.Top,
                Height = 24
            };

            _lblStatsMain = new Label
            {
                Font = new Font("Malgun Gothic", 9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 22
            };

            _lblStatsStrategy = new Label
            {
                Font = new Font("Malgun Gothic", 8.8f),
                ForeColor = Color.FromArgb(170, 195, 225),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            pnlHeader.Controls.Add(_lblStatsStrategy);
            pnlHeader.Controls.Add(_lblStatsMain);
            pnlHeader.Controls.Add(lblTitle);

            // 2. 툴바 (전략 필터 + 추가/삭제 버튼)
            var pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 42,
                BackColor = Color.FromArgb(18, 22, 32),
                Padding = new Padding(16, 6, 16, 6)
            };

            var lblFilter = new Label
            {
                Text = "진입 전략 필터:",
                ForeColor = Color.FromArgb(170, 185, 205),
                Location = new Point(16, 11),
                AutoSize = true
            };

            _cboFilterStrategy = new ComboBox
            {
                Location = new Point(112, 8),
                Width = 170,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(24, 30, 42),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _cboFilterStrategy.Items.AddRange(new object[]
            {
                "전체 전략 보기", "과매도 반등", "골든 눌림목", "거래대금 폭발", "분할매수", "수동/기타"
            });
            _cboFilterStrategy.SelectedIndex = 0;
            _cboFilterStrategy.SelectedIndexChanged += (s, e) => RefreshJournalView();

            _btnAdd = new Button
            {
                Text = "+ 새 복기 기록 추가",
                Width = 140,
                Height = 28,
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(16, 48, 32),
                ForeColor = Color.FromArgb(90, 255, 170),
                Font = BoldFont,
                Cursor = Cursors.Hand
            };
            _btnAdd.FlatAppearance.BorderColor = Color.FromArgb(40, 170, 90);
            _btnAdd.Click += (s, e) => AddNewEntry();

            _btnDelete = new Button
            {
                Text = "선택 기록 삭제",
                Width = 110,
                Height = 28,
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(44, 24, 28),
                ForeColor = Color.FromArgb(255, 130, 140),
                Cursor = Cursors.Hand
            };
            _btnDelete.FlatAppearance.BorderColor = Color.FromArgb(150, 50, 60);
            _btnDelete.Click += (s, e) => DeleteSelectedEntry();

            var pnlSpacer = new Panel { Dock = DockStyle.Right, Width = 8 };

            pnlToolbar.Controls.Add(lblFilter);
            pnlToolbar.Controls.Add(_cboFilterStrategy);
            pnlToolbar.Controls.Add(_btnDelete);
            pnlToolbar.Controls.Add(pnlSpacer);
            pnlToolbar.Controls.Add(_btnAdd);

            // 3. 중앙 리스트뷰
            _lvJournal = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false,
                BackColor = Color.FromArgb(14, 18, 26),
                ForeColor = Color.FromArgb(225, 230, 240),
                BorderStyle = BorderStyle.None,
                Font = new Font("Malgun Gothic", 9f)
            };
            _lvJournal.Columns.Add("청산일", 90, HorizontalAlignment.Center);
            _lvJournal.Columns.Add("종목명", 120);
            _lvJournal.Columns.Add("코드", 65, HorizontalAlignment.Center);
            _lvJournal.Columns.Add("진입 전략", 105, HorizontalAlignment.Center);
            _lvJournal.Columns.Add("청산 사유", 105, HorizontalAlignment.Center);
            _lvJournal.Columns.Add("수량", 55, HorizontalAlignment.Right);
            _lvJournal.Columns.Add("매수가", 85, HorizontalAlignment.Right);
            _lvJournal.Columns.Add("매도가", 85, HorizontalAlignment.Right);
            _lvJournal.Columns.Add("실현손익", 100, HorizontalAlignment.Right);
            _lvJournal.Columns.Add("수익률", 75, HorizontalAlignment.Right);
            _lvJournal.Columns.Add("복기 메모", 185);

            _lvJournal.DoubleClick += (s, e) => EditSelectedEntry();

            // 4. 하단 푸터
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                BackColor = Color.FromArgb(22, 27, 38),
                Padding = new Padding(16, 6, 16, 6)
            };

            var lblGuide = new Label
            {
                Text = "[안내] [+ 새 복기 기록 추가]에서 내 종목 정보를 1초 만에 불러와 기록할 수 있습니다. (항목 더블클릭 시 수정)",
                ForeColor = Color.FromArgb(165, 185, 210),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _btnClose = new Button
            {
                Text = "닫기 (Esc)",
                Dock = DockStyle.Right,
                Width = 100,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(38, 46, 62),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            _btnClose.FlatAppearance.BorderColor = Color.FromArgb(70, 90, 120);
            _btnClose.Click += (s, e) => this.Close();

            pnlFooter.Controls.Add(lblGuide);
            pnlFooter.Controls.Add(_btnClose);

            this.Controls.Add(_lvJournal);
            this.Controls.Add(pnlToolbar);
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlFooter);

            this.KeyPreview = true;
            this.KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) this.Close(); };
        }

        private void RefreshJournalView()
        {
            string filter = _cboFilterStrategy.SelectedIndex <= 0 ? null : _cboFilterStrategy.SelectedItem.ToString();

            var filtered = string.IsNullOrEmpty(filter)
                ? _entries.OrderByDescending(x => x.TradeDate).ToList()
                : _entries.Where(x => string.Equals(x.EntryStrategy, filter, StringComparison.OrdinalIgnoreCase))
                          .OrderByDescending(x => x.TradeDate).ToList();

            // 전체 통계 계산
            int totalCount = _entries.Count;
            int winCount = _entries.Count(x => x.IsWin);
            decimal winRate = totalCount > 0 ? Math.Round((decimal)winCount / totalCount * 100m, 1) : 0m;
            decimal totalProfit = _entries.Sum(x => x.RealizedProfit);
            decimal avgRate = totalCount > 0 ? Math.Round(_entries.Average(x => x.ProfitRate), 2) : 0m;

            _lblStatsMain.Text = $"누적 매매: {totalCount}건 ({winCount}승 {totalCount - winCount}패)  |  전체 승률: {winRate:F1}%  |  누적 실현손익: {totalProfit:+#,##0;-#,##0;0}원  |  평균 수익률: {avgRate:+0.00;-0.00;0.00}%";
            _lblStatsMain.ForeColor = totalProfit >= 0 ? Color.FromArgb(0, 230, 118) : Color.FromArgb(255, 110, 120);

            // 전략별 승률 요약
            var stratGroups = _entries
                .GroupBy(x => string.IsNullOrWhiteSpace(x.EntryStrategy) ? "수동/기타" : x.EntryStrategy)
                .OrderByDescending(g => g.Count())
                .Select(g =>
                {
                    int cnt = g.Count();
                    int w = g.Count(x => x.IsWin);
                    decimal wr = cnt > 0 ? Math.Round((decimal)w / cnt * 100m, 0) : 0m;
                    return $"{g.Key}({w}승/{cnt}건, {wr:F0}%)";
                });
            _lblStatsStrategy.Text = totalCount > 0
                ? "• 전략별 승률: " + string.Join("  |  ", stratGroups)
                : "• 아직 기록된 매매 복기 내역이 없습니다. 우측 [+ 새 복기 기록 추가] 버튼으로 첫 매매를 기록해보세요!";

            _lvJournal.BeginUpdate();
            _lvJournal.Items.Clear();
            foreach (var item in filtered)
            {
                var lvi = new ListViewItem(item.TradeDate.ToString("yyyy-MM-dd"));
                lvi.Tag = item;
                lvi.UseItemStyleForSubItems = false;

                lvi.SubItems.Add(item.StockName);
                lvi.SubItems.Add(item.StockCode);
                lvi.SubItems.Add(item.EntryStrategy);
                lvi.SubItems.Add(item.ExitReason);
                lvi.SubItems.Add($"{item.Quantity:N0}");
                lvi.SubItems.Add($"{item.BuyPrice:N0}");
                lvi.SubItems.Add($"{item.SellPrice:N0}");

                var subProfit = lvi.SubItems.Add($"{item.RealizedProfit:+#,##0;-#,##0;0}원");
                var subRate = lvi.SubItems.Add($"{item.ProfitRate:+0.00;-0.00;0.00}%");
                Color pnlColor = item.RealizedProfit > 0
                    ? Color.FromArgb(255, 100, 110)
                    : (item.RealizedProfit < 0 ? Color.FromArgb(95, 175, 255) : Color.LightGray);
                subProfit.ForeColor = pnlColor;
                subRate.ForeColor = pnlColor;
                subProfit.Font = BoldFont;
                subRate.Font = BoldFont;

                lvi.SubItems.Add(item.Memo);
                _lvJournal.Items.Add(lvi);
            }
            _lvJournal.EndUpdate();
        }

        private void AddNewEntry()
        {
            using (var dlg = new TradeJournalEditDialog(null, _portfolioItems))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK && dlg.ResultEntry != null)
                {
                    _entries.Add(dlg.ResultEntry);
                    TradeJournalStorage.SaveEntries(_entries);
                    RefreshJournalView();
                }
            }
        }

        private void EditSelectedEntry()
        {
            if (_lvJournal.SelectedItems.Count == 0) return;
            var entry = _lvJournal.SelectedItems[0].Tag as TradeJournalEntry;
            if (entry == null) return;

            using (var dlg = new TradeJournalEditDialog(entry, _portfolioItems))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK && dlg.ResultEntry != null)
                {
                    int idx = _entries.IndexOf(entry);
                    if (idx >= 0) _entries[idx] = dlg.ResultEntry;
                    TradeJournalStorage.SaveEntries(_entries);
                    RefreshJournalView();
                }
            }
        }

        private void DeleteSelectedEntry()
        {
            if (_lvJournal.SelectedItems.Count == 0) return;
            var entry = _lvJournal.SelectedItems[0].Tag as TradeJournalEntry;
            if (entry == null) return;

            var dr = MessageBox.Show($"[{entry.StockName}] 매매 복기 기록을 삭제하시겠습니까?", "복기 기록 삭제",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dr != DialogResult.Yes) return;

            _entries.Remove(entry);
            TradeJournalStorage.SaveEntries(_entries);
            RefreshJournalView();
        }

        /// <summary>
        /// 매매 복기 기록 입력/수정 모달
        /// </summary>
        private class TradeJournalEditDialog : Form
        {
            public TradeJournalEntry ResultEntry { get; private set; }
            private readonly TradeJournalEntry _origin;
            private readonly List<StockHoldingItem> _portfolioItems;

            private ComboBox _cboLoadStock;
            private TextBox _txtName;
            private TextBox _txtCode;
            private ComboBox _cboStrategy;
            private ComboBox _cboReason;
            private NumericUpDown _nudQty;
            private NumericUpDown _nudBuy;
            private NumericUpDown _nudSell;
            private DateTimePicker _dtpDate;
            private TextBox _txtMemo;

            public TradeJournalEditDialog(TradeJournalEntry origin, List<StockHoldingItem> portfolioItems)
            {
                _origin = origin;
                _portfolioItems = portfolioItems ?? new List<StockHoldingItem>();

                this.Text = origin == null ? "새 매매 복기 기록 추가" : "매매 복기 기록 수정";
                this.Size = new Size(460, 450);
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.StartPosition = FormStartPosition.CenterParent;
                this.BackColor = Color.FromArgb(20, 24, 34);
                this.ForeColor = Color.White;
                this.Font = new Font("Malgun Gothic", 9f);

                BuildUi();
                LoadOriginData();
            }

            private void BuildUi()
            {
                int y = 14;
                var lblLoad = new Label { Text = "내 종목 불러오기:", Location = new Point(16, y + 3), AutoSize = true, ForeColor = Color.FromArgb(0, 216, 255) };
                _cboLoadStock = new ComboBox
                {
                    Location = new Point(135, y),
                    Width = 290,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    BackColor = Color.FromArgb(28, 34, 48),
                    ForeColor = Color.White
                };
                _cboLoadStock.Items.Add("-- 직접 입력 또는 보유/관심 종목 선택 --");
                foreach (var itm in _portfolioItems)
                {
                    _cboLoadStock.Items.Add(new StockPickWrapper(itm));
                }
                _cboLoadStock.SelectedIndex = 0;
                _cboLoadStock.SelectedIndexChanged += (s, e) =>
                {
                    if (_cboLoadStock.SelectedItem is StockPickWrapper w && w.Item != null)
                    {
                        _txtName.Text = w.Item.StockName;
                        _txtCode.Text = w.Item.StockCode;
                        if (w.Item.Quantity > 0) _nudQty.Value = Math.Max(1, w.Item.Quantity);
                        if (w.Item.BuyPrice > 0) _nudBuy.Value = Math.Max(0, w.Item.BuyPrice);
                        if (w.Item.CurrentPrice > 0) _nudSell.Value = Math.Max(0, w.Item.CurrentPrice);
                        if (!string.IsNullOrEmpty(w.Item.TimingBadge))
                        {
                            if (w.Item.TimingBadge.Contains("과매도")) _cboStrategy.SelectedItem = "과매도 반등";
                            else if (w.Item.TimingBadge.Contains("눌림목")) _cboStrategy.SelectedItem = "골든 눌림목";
                        }
                    }
                };
                y += 34;

                var lblName = new Label { Text = "종목명 / 코드:", Location = new Point(16, y + 3), AutoSize = true };
                _txtName = new TextBox { Location = new Point(135, y), Width = 190, BackColor = Color.FromArgb(28, 34, 48), ForeColor = Color.White };
                _txtCode = new TextBox { Location = new Point(335, y), Width = 90, BackColor = Color.FromArgb(28, 34, 48), ForeColor = Color.White };
                y += 32;

                var lblStrat = new Label { Text = "진입 전략:", Location = new Point(16, y + 3), AutoSize = true };
                _cboStrategy = new ComboBox
                {
                    Location = new Point(135, y), Width = 140, DropDownStyle = ComboBoxStyle.DropDownList,
                    BackColor = Color.FromArgb(28, 34, 48), ForeColor = Color.White
                };
                _cboStrategy.Items.AddRange(new object[] { "과매도 반등", "골든 눌림목", "거래대금 폭발", "분할매수", "수동/기타" });
                _cboStrategy.SelectedIndex = 0;

                _cboReason = new ComboBox
                {
                    Location = new Point(285, y), Width = 140, DropDownStyle = ComboBoxStyle.DropDownList,
                    BackColor = Color.FromArgb(28, 34, 48), ForeColor = Color.White
                };
                _cboReason.Items.AddRange(new object[] { "목표가 익절", "분할 익절", "손절선 준수", "비중 축소/교체", "수동 청산" });
                _cboReason.SelectedIndex = 0;
                y += 32;

                var lblQty = new Label { Text = "매매 수량(주):", Location = new Point(16, y + 3), AutoSize = true };
                _nudQty = new NumericUpDown { Location = new Point(135, y), Width = 120, Maximum = 1000000, Minimum = 1, Value = 1, BackColor = Color.FromArgb(28, 34, 48), ForeColor = Color.White };
                _dtpDate = new DateTimePicker { Location = new Point(265, y), Width = 160, Format = DateTimePickerFormat.Short };
                y += 32;

                var lblBuy = new Label { Text = "매수 단가(원):", Location = new Point(16, y + 3), AutoSize = true };
                _nudBuy = new NumericUpDown { Location = new Point(135, y), Width = 135, Maximum = 100000000, ThousandsSeparator = true, BackColor = Color.FromArgb(28, 34, 48), ForeColor = Color.White };
                y += 32;

                var lblSell = new Label { Text = "매도 단가(원):", Location = new Point(16, y + 3), AutoSize = true };
                _nudSell = new NumericUpDown { Location = new Point(135, y), Width = 135, Maximum = 100000000, ThousandsSeparator = true, BackColor = Color.FromArgb(28, 34, 48), ForeColor = Color.White };
                y += 34;

                var lblMemo = new Label { Text = "한줄 복기 메모:", Location = new Point(16, y + 3), AutoSize = true };
                _txtMemo = new TextBox
                {
                    Location = new Point(135, y), Width = 290, Height = 80, Multiline = true,
                    BackColor = Color.FromArgb(28, 34, 48), ForeColor = Color.White
                };
                y += 96;

                var btnSave = new Button
                {
                    Text = "저장",
                    Location = new Point(240, y), Width = 90, Height = 32,
                    FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(16, 68, 42), ForeColor = Color.White
                };
                btnSave.Click += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(_txtName.Text))
                    {
                        MessageBox.Show("종목명을 입력해주세요.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    ResultEntry = new TradeJournalEntry
                    {
                        Id = _origin?.Id ?? Guid.NewGuid().ToString("N"),
                        TradeDate = _dtpDate.Value,
                        StockName = _txtName.Text.Trim(),
                        StockCode = _txtCode.Text.Trim(),
                        EntryStrategy = _cboStrategy.SelectedItem?.ToString() ?? "수동/기타",
                        ExitReason = _cboReason.SelectedItem?.ToString() ?? "수동 청산",
                        Quantity = (int)_nudQty.Value,
                        BuyPrice = _nudBuy.Value,
                        SellPrice = _nudSell.Value,
                        Memo = _txtMemo.Text.Trim()
                    };
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                };

                var btnCancel = new Button
                {
                    Text = "취소",
                    Location = new Point(338, y), Width = 87, Height = 32,
                    FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(40, 46, 58), ForeColor = Color.White
                };
                btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

                this.Controls.AddRange(new Control[]
                {
                    lblLoad, _cboLoadStock, lblName, _txtName, _txtCode,
                    lblStrat, _cboStrategy, _cboReason,
                    lblQty, _nudQty, _dtpDate, lblBuy, _nudBuy, lblSell, _nudSell,
                    lblMemo, _txtMemo, btnSave, btnCancel
                });
            }

            private void LoadOriginData()
            {
                if (_origin == null) return;
                _txtName.Text = _origin.StockName;
                _txtCode.Text = _origin.StockCode;
                _cboStrategy.SelectedItem = _origin.EntryStrategy;
                _cboReason.SelectedItem = _origin.ExitReason;
                _nudQty.Value = Math.Max(1, _origin.Quantity);
                _nudBuy.Value = Math.Max(0, _origin.BuyPrice);
                _nudSell.Value = Math.Max(0, _origin.SellPrice);
                _dtpDate.Value = _origin.TradeDate;
                _txtMemo.Text = _origin.Memo;
            }

            private class StockPickWrapper
            {
                public StockHoldingItem Item { get; }
                public StockPickWrapper(StockHoldingItem item) { Item = item; }
                public override string ToString() => $"{Item.StockName} ({Item.StockCode}) - {(Item.Quantity > 0 ? $"보유 {Item.Quantity}주" : "관심종목")}";
            }
        }
    }
}
