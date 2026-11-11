using System;
using System.Drawing;
using System.Windows.Forms;

namespace GateHelper.StockAdvisor
{
    public partial class SmartPortfolioImportDialog : Form
    {
        #region UI Layout Construction

        private void BuildUi()
        {
            this.SuspendLayout();

            // 상단 툴바 패널
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.FromArgb(24, 28, 40),
                Padding = new Padding(12, 10, 12, 10)
            };

            _btnKbSync = CreateButton("⚡ KB 실시간 연동", Color.FromArgb(28, 48, 75), Color.FromArgb(0, 220, 255));
            _btnKbSync.Width = 150;
            _btnKbSync.Dock = DockStyle.Left;
            _btnKbSync.Click += async (s, e) => await ShowKbSettingDialogAndSyncAsync();

            _btnOpenFile = CreateButton("CSV 파일 열기", Color.FromArgb(20, 48, 70), Color.FromArgb(100, 210, 240));
            _btnOpenFile.Width = 120;
            _btnOpenFile.Dock = DockStyle.Left;
            _btnOpenFile.Margin = new Padding(6, 0, 0, 0);
            _btnOpenFile.Click += (s, e) => OpenDataFile();

            _btnPasteClipboard = CreateButton("클립보드 붙여넣기", Color.FromArgb(20, 52, 40), Color.FromArgb(0, 230, 118));
            _btnPasteClipboard.Width = 145;
            _btnPasteClipboard.Dock = DockStyle.Left;
            _btnPasteClipboard.Margin = new Padding(6, 0, 0, 0);
            _btnPasteClipboard.Click += (s, e) => PasteFromClipboard();

            _btnClear = CreateButton("초기화", Color.FromArgb(40, 44, 56), Color.FromArgb(180, 190, 210));
            _btnClear.Width = 65;
            _btnClear.Dock = DockStyle.Left;
            _btnClear.Margin = new Padding(6, 0, 0, 0);
            _btnClear.Click += (s, e) => ClearData();

            _lblStatus = new Label
            {
                Text = "• [⚡ KB 실시간 연동]을 눌러 설정 후 잔고를 불러오거나, HTS 복사 후 [클립보드 붙여넣기]를 누르세요.",
                ForeColor = Color.FromArgb(170, 185, 210),
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                Padding = new Padding(12, 0, 0, 0),
                AutoEllipsis = true
            };

            pnlTop.Controls.Add(_lblStatus);
            pnlTop.Controls.Add(_btnClear);
            pnlTop.Controls.Add(_btnPasteClipboard);
            pnlTop.Controls.Add(_btnOpenFile);
            pnlTop.Controls.Add(_btnKbSync);

            // 지능형 컬럼 매핑 바 (헤더 감지 및 사용자 매핑 지정)
            var pnlMapping = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Color.FromArgb(28, 34, 48),
                Padding = new Padding(12, 8, 12, 8)
            };

            var tblMapping = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 2,
                Margin = new Padding(0)
            };
            tblMapping.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblMapping.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblMapping.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblMapping.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblMapping.RowStyles.Add(new RowStyle(SizeType.Absolute, 20f));
            tblMapping.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var lblColName = new Label { Text = "① 종목명 컬럼 (필수)", ForeColor = Color.FromArgb(0, 230, 255), Font = new Font("Malgun Gothic", 8.5f, FontStyle.Bold), Dock = DockStyle.Fill };
            var lblColCode = new Label { Text = "② 종목코드 컬럼 (선택)", ForeColor = Color.FromArgb(200, 210, 225), Font = new Font("Malgun Gothic", 8.5f, FontStyle.Bold), Dock = DockStyle.Fill };
            var lblColQty = new Label { Text = "③ 보유수량 컬럼 (필수)", ForeColor = Color.FromArgb(0, 230, 118), Font = new Font("Malgun Gothic", 8.5f, FontStyle.Bold), Dock = DockStyle.Fill };
            var lblColPrice = new Label { Text = "④ 매입단가 컬럼 (필수)", ForeColor = Color.FromArgb(255, 190, 60), Font = new Font("Malgun Gothic", 8.5f, FontStyle.Bold), Dock = DockStyle.Fill };

            _cboColName = CreateMappingComboBox();
            _cboColCode = CreateMappingComboBox();
            _cboColQty = CreateMappingComboBox();
            _cboColPrice = CreateMappingComboBox();

            _cboColName.SelectedIndexChanged += (s, e) => RebuildPreviewList();
            _cboColCode.SelectedIndexChanged += (s, e) => RebuildPreviewList();
            _cboColQty.SelectedIndexChanged += (s, e) => RebuildPreviewList();
            _cboColPrice.SelectedIndexChanged += (s, e) => RebuildPreviewList();

            tblMapping.Controls.Add(lblColName, 0, 0);
            tblMapping.Controls.Add(lblColCode, 1, 0);
            tblMapping.Controls.Add(lblColQty, 2, 0);
            tblMapping.Controls.Add(lblColPrice, 3, 0);

            tblMapping.Controls.Add(_cboColName, 0, 1);
            tblMapping.Controls.Add(_cboColCode, 1, 1);
            tblMapping.Controls.Add(_cboColQty, 2, 1);
            tblMapping.Controls.Add(_cboColPrice, 3, 1);

            pnlMapping.Controls.Add(tblMapping);

            // 중간 리스트뷰 영역 (체크박스 목록)
            var pnlCenter = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12, 8, 12, 8)
            };

            var pnlListHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 30,
                Padding = new Padding(2, 4, 2, 4)
            };

            _chkSelectAll = new CheckBox
            {
                Text = "전체 선택 / 해제",
                Checked = true,
                AutoSize = true,
                Dock = DockStyle.Left,
                ForeColor = Color.FromArgb(210, 220, 240),
                Cursor = Cursors.Hand
            };
            _chkSelectAll.CheckedChanged += (s, e) =>
            {
                bool chk = _chkSelectAll.Checked;
                foreach (ListViewItem item in _lvPreview.Items)
                {
                    item.Checked = chk;
                }
                UpdateSelectionSummary();
            };

            _lblSelectionSummary = new Label
            {
                Text = "선택: 0개 종목 | 매입총액: 0원",
                ForeColor = Color.FromArgb(0, 216, 255),
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Right,
                AutoSize = true
            };

            _progResolve = new ProgressBar
            {
                Visible = false,
                Height = 16,
                Width = 160,
                Dock = DockStyle.Right,
                Style = ProgressBarStyle.Continuous
            };

            pnlListHeader.Controls.Add(_progResolve);
            pnlListHeader.Controls.Add(_lblSelectionSummary);
            pnlListHeader.Controls.Add(_chkSelectAll);

            _lvPreview = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                CheckBoxes = true,
                GridLines = true,
                BackColor = Color.FromArgb(14, 18, 26),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f)
            };
            _lvPreview.Columns.Add("선택/상태", 100);
            _lvPreview.Columns.Add("종목명", 180);
            _lvPreview.Columns.Add("종목코드", 100);
            _lvPreview.Columns.Add("보유수량", 90, HorizontalAlignment.Right);
            _lvPreview.Columns.Add("평균매입단가", 120, HorizontalAlignment.Right);
            _lvPreview.Columns.Add("매입총액", 140, HorizontalAlignment.Right);
            _lvPreview.Columns.Add("자동 매핑 안내", 170);

            _lvPreview.ItemChecked += (s, e) => UpdateSelectionSummary();

            pnlCenter.Controls.Add(_lvPreview);
            pnlCenter.Controls.Add(pnlListHeader);

            // 하단 액션 패널
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 74,
                BackColor = Color.FromArgb(24, 28, 40),
                Padding = new Padding(14, 8, 14, 8)
            };

            var tblRadio = new TableLayoutPanel
            {
                Dock = DockStyle.Left,
                Width = 480,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0)
            };
            tblRadio.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));
            tblRadio.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));

            _rbAppendMerge = new RadioButton
            {
                Text = "기존 포트폴리오에 추가 (중복 시 수량/단가 갱신)",
                Checked = true,
                AutoSize = true,
                ForeColor = Color.FromArgb(220, 230, 245),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 0, 2),
                Font = new Font("Malgun Gothic", 9f)
            };

            _rbReplaceAll = new RadioButton
            {
                Text = "기존 포트폴리오 비우고 새로 덮어쓰기 (전체 교체)",
                Checked = false,
                AutoSize = true,
                ForeColor = Color.FromArgb(255, 170, 170),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 0, 2),
                Font = new Font("Malgun Gothic", 9f)
            };

            tblRadio.Controls.Add(_rbAppendMerge, 0, 0);
            tblRadio.Controls.Add(_rbReplaceAll, 0, 1);

            var pnlButtons = new Panel
            {
                Dock = DockStyle.Right,
                Width = 320
            };

            _btnApply = CreateButton("다음: 최종 표 미리보기 ▶ (0개)", Color.FromArgb(14, 52, 42), Color.FromArgb(0, 230, 118));
            _btnApply.Font = new Font("Malgun Gothic", 9.5f, FontStyle.Bold);
            _btnApply.Dock = DockStyle.Fill;
            _btnApply.Click += (s, e) => ApplySelectedStocks();

            _btnCancel = CreateButton("취소", Color.FromArgb(40, 44, 56), Color.FromArgb(190, 200, 215));
            _btnCancel.Width = 80;
            _btnCancel.Dock = DockStyle.Right;
            _btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            pnlButtons.Controls.Add(_btnApply);
            pnlButtons.Controls.Add(_btnCancel);

            pnlBottom.Controls.Add(pnlButtons);
            pnlBottom.Controls.Add(tblRadio);

            // 전체 폼에 추가
            this.Controls.Add(pnlCenter);
            this.Controls.Add(pnlMapping);
            this.Controls.Add(pnlTop);
            this.Controls.Add(pnlBottom);

            this.ResumeLayout(false);
        }

        private static ComboBox CreateMappingComboBox()
        {
            return new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(36, 44, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f)
            };
        }

        private static Button CreateButton(string text, Color backColor, Color foreColor)
        {
            var btn = new Button
            {
                Text = text,
                BackColor = backColor,
                ForeColor = foreColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Malgun Gothic", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(4, 0, 4, 0)
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(Math.Min(255, backColor.R + 40), Math.Min(255, backColor.G + 40), Math.Min(255, backColor.B + 40));
            btn.FlatAppearance.BorderSize = 1;
            return btn;
        }

        #endregion
    }
}
