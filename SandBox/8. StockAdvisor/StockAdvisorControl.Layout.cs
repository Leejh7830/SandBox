using System;
using System.Drawing;
using System.Windows.Forms;

namespace GateHelper.StockAdvisor
{
    public partial class StockAdvisorControl
    {
        #region UI Layout Construction

        private void BuildUiLayout()
        {
            this.SuspendLayout();

            // 1. 상단 브리핑 카드 패널 (1행 3열 반응형 그리드)
            var tblHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 110,
                BackColor = Color.FromArgb(24, 28, 38),
                Padding = new Padding(10, 8, 10, 8),
                ColumnCount = 3,
                RowCount = 1
            };
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 85f));   // 신호등
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));   // 3줄 브리핑 (가변)
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 450f));  // 우측 버튼 (150px*3열 여유 너비 확보)
            tblHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // 신호등 뱃지 (클릭 시 글로벌 인텔 & 실시간 반도체 뉴스 브리핑 팝업)
            _lblTrafficLight = new Label
            {
                Text = "●\n안전\n(뉴스📰)",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Malgun Gothic", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 220, 140),
                BackColor = Color.FromArgb(32, 40, 54),
                Dock = DockStyle.Fill,
                Margin = new Padding(2),
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand
            };
            _lblTrafficLight.MouseEnter += (s, e) => _lblTrafficLight.BackColor = Color.FromArgb(46, 58, 78);
            _lblTrafficLight.MouseLeave += (s, e) => _lblTrafficLight.BackColor = Color.FromArgb(32, 40, 54);
            _lblTrafficLight.Click += (s, e) => ShowMarketIntelNewsDialog();

            var ttTraffic = new ToolTip();
            ttTraffic.SetToolTip(_lblTrafficLight, "클릭 시 엔비디아(NVDA)/삼성전자/SK하이닉스 실시간 뉴스 및 글로벌 지표 브리핑이 열립니다.");

            // 3줄 브리핑 영역
            var pnlBriefingText = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(8, 2, 8, 2)
            };

            var pnlTitleRow = new Panel
            {
                Dock = DockStyle.Top,
                Height = 24,
                Margin = new Padding(0)
            };

            var lblHeaderTitle = new Label
            {
                Text = "◆ [AI 주식 리스크 참모] 오늘의 모닝 브리핑 & 거시 동향",
                Font = new Font("Malgun Gothic", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(120, 210, 255),
                Dock = DockStyle.Left,
                AutoSize = true
            };

            _cboAiModel = new ComboBox
            {
                Dock = DockStyle.Right,
                Width = 230,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(28, 36, 50),
                ForeColor = Color.FromArgb(0, 230, 255),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _cboAiModel.Items.Add("🧠 Gemini 1.5 Pro (심층 추론)");
            _cboAiModel.Items.Add("⚡ Gemini 1.5 Flash (초고속)");
            _cboAiModel.Items.Add("🚀 Gemini 2.0 Flash (차세대)");

            // 저장된 설정 로드
            string curModel = GateHelper.StockAdvisor.Services.GeminiRiskAdvisorService.UserPreferredModel;
            if (curModel.Contains("2.0")) _cboAiModel.SelectedIndex = 2;
            else if (curModel.Contains("flash")) _cboAiModel.SelectedIndex = 1;
            else _cboAiModel.SelectedIndex = 0; // Pro 기본 선택

            _cboAiModel.SelectedIndexChanged += (s, e) =>
            {
                if (_cboAiModel.SelectedIndex == 0) GateHelper.StockAdvisor.Services.GeminiRiskAdvisorService.UserPreferredModel = "gemini-1.5-pro";
                else if (_cboAiModel.SelectedIndex == 1) GateHelper.StockAdvisor.Services.GeminiRiskAdvisorService.UserPreferredModel = "gemini-1.5-flash";
                else if (_cboAiModel.SelectedIndex == 2) GateHelper.StockAdvisor.Services.GeminiRiskAdvisorService.UserPreferredModel = "gemini-2.0-flash";
            };

            pnlTitleRow.Controls.Add(_cboAiModel);
            pnlTitleRow.Controls.Add(lblHeaderTitle);

            _lblBriefing1 = new Label { Text = "• 1행: 글로벌 매크로 및 미 수출통제(BIS) 동향 점검 중", Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(220, 225, 235), Dock = DockStyle.Top, Height = 20 };
            _lblBriefing2 = new Label { Text = "• 2행: 국내 증시 및 주력 반도체/IT 섹터 리스크 모니터링", Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(200, 205, 215), Dock = DockStyle.Top, Height = 20 };
            _lblBriefing3 = new Label { Text = "• 3행: 뇌동매매 금지! 추천 포지션 지침을 확인하세요.", Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(160, 210, 160), Dock = DockStyle.Top, Height = 20 };

            pnlBriefingText.Controls.Add(_lblBriefing3);
            pnlBriefingText.Controls.Add(_lblBriefing2);
            pnlBriefingText.Controls.Add(_lblBriefing1);
            pnlBriefingText.Controls.Add(pnlTitleRow);

            // 우측 상단 액션 버튼 영역 (네온 사이버 스타일 2행 3열 반응형 그리드)
            var tblButtons = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2,
                Margin = new Padding(0)
            };
            tblButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            tblButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            tblButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
            tblButtons.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            tblButtons.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            _btnDiagnose = CreateNeonButton("★ AI 참모 진단\r\n(진단 대기)", Color.FromArgb(14, 38, 56), Color.FromArgb(0, 216, 255), Color.FromArgb(210, 248, 255));
            _btnDiagnose.Font = new Font("Malgun Gothic", 8.8f, FontStyle.Bold);
            _btnDiagnose.Dock = DockStyle.Fill;
            _btnDiagnose.Click += async (s, e) => await RunAiDiagnosisAsync();

            _btnRefreshPrice = CreateNeonButton("↻ 시세 갱신", Color.FromArgb(20, 34, 56), Color.FromArgb(64, 150, 255), Color.FromArgb(215, 235, 255));
            _btnRefreshPrice.Dock = DockStyle.Fill;
            _btnRefreshPrice.Margin = new Padding(1);
            _btnRefreshPrice.Click += async (s, e) => await RefreshPricesAsync();

            _btnScoreHelp = CreateNeonButton("?", Color.FromArgb(24, 38, 55), Color.FromArgb(0, 216, 255), Color.FromArgb(210, 245, 255));
            _btnScoreHelp.Dock = DockStyle.Fill;
            _btnScoreHelp.Margin = new Padding(1);
            _btnScoreHelp.Font = new Font("Malgun Gothic", 10.5f, FontStyle.Bold);
            _btnScoreHelp.Cursor = Cursors.Hand;
            _btnScoreHelp.Click += (s, e) => ShowScoreHelpDialog();

            var ttHelp = new ToolTip();
            ttHelp.SetToolTip(_btnScoreHelp, "4대 진단 점수(차트/뉴스/손익비/종합) 산출 기준 및 대응 가이드 열기");

            var tblRefreshAndHelp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                ColumnCount = 2,
                RowCount = 1
            };
            tblRefreshAndHelp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblRefreshAndHelp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 38f));
            tblRefreshAndHelp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblRefreshAndHelp.Controls.Add(_btnRefreshPrice, 0, 0);
            tblRefreshAndHelp.Controls.Add(_btnScoreHelp, 1, 0);

            _btnAddStock = CreateNeonButton("+ 종목 추가", Color.FromArgb(16, 42, 32), Color.FromArgb(0, 230, 118), Color.FromArgb(215, 255, 230));
            _btnAddStock.Dock = DockStyle.Fill;
            _btnAddStock.Click += (s, e) => ShowAddStockDialog();

            _btnImportPortfolio = CreateNeonButton("계좌 가져오기", Color.FromArgb(36, 26, 54), Color.FromArgb(190, 130, 255), Color.FromArgb(240, 225, 255));
            _btnImportPortfolio.Dock = DockStyle.Fill;
            _btnImportPortfolio.Click += (s, e) => ShowImportPortfolioDialog();

            _btnEmergencyGuide = CreateNeonButton("★ 중요 종목", Color.FromArgb(45, 36, 20), Color.FromArgb(255, 190, 40), Color.FromArgb(255, 245, 210));
            _btnEmergencyGuide.Dock = DockStyle.Fill;
            _btnEmergencyGuide.Click += (s, e) => ShowKeyStockSummaryDialog();

            // 1행: AI 진단(가로 2칸) + [시세 갱신 + ? 도움말](1칸)
            tblButtons.Controls.Add(_btnDiagnose, 0, 0);
            tblButtons.SetColumnSpan(_btnDiagnose, 2);
            tblButtons.Controls.Add(tblRefreshAndHelp, 2, 0);

            // 2행: 종목 추가(1칸) + 계좌 가져오기(1칸) + 비상 탈출(1칸)
            tblButtons.Controls.Add(_btnAddStock, 0, 1);
            tblButtons.Controls.Add(_btnImportPortfolio, 1, 1);
            tblButtons.Controls.Add(_btnEmergencyGuide, 2, 1);

            tblHeader.Controls.Add(_lblTrafficLight, 0, 0);
            tblHeader.Controls.Add(pnlBriefingText, 1, 0);
            tblHeader.Controls.Add(tblButtons, 2, 0);
            _pnlHeader = tblHeader;

            // 1-2. 글로벌 매크로 지표 슬림 티커 바
            _pnlMacroTicker = new Panel
            {
                Dock = DockStyle.Top,
                Height = 26,
                BackColor = Color.FromArgb(20, 24, 34),
                Padding = new Padding(12, 4, 12, 4)
            };
            _lblMacroTicker = new Label
            {
                Text = "⚡ [글로벌 매크로 인텔] NVDA: 대기 | SOX(반도체): 대기 | 나스닥: 대기 | 미 연방관보: BIS 반도체 정책 모니터링",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(170, 205, 245),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            _pnlMacroTicker.Controls.Add(_lblMacroTicker);

            // 2. 하단 계좌 요약 상태바
            _pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 36,
                BackColor = Color.FromArgb(20, 24, 32),
                Padding = new Padding(12, 6, 12, 6)
            };

            _lblAccountSummary = new Label
            {
                Text = "총 매수: 0원  |  총 평가: 0원  |  손익: 0원 (0.00%)",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 220, 245),
                Dock = DockStyle.Left,
                AutoSize = true
            };

            _lblLastUpdated = new Label
            {
                Visible = false
            };

            _pnlFooter.Controls.Add(_lblAccountSummary);

            // 3. 중앙 분할 컨테이너 (좌측 리스트뷰 최적폭 880px, 우측 상세 카드 380px 쾌적 확보)
            _splitMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 880,
                BackColor = Color.FromArgb(30, 34, 46)
            };

            // 좌측: 종목 리스트뷰 (17대 컬럼 구성: 코드 맨앞, 평단/손익금/보유수량/전일대비 탑재)
            _lvStocks = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BackColor = Color.FromArgb(18, 20, 26),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.2f),
                BorderStyle = BorderStyle.None
            };
            _lvStocks.Columns.Add("코드", 62, HorizontalAlignment.Center);
            _lvStocks.Columns.Add("종목명", 120);
            _lvStocks.Columns.Add("현재가", 75, HorizontalAlignment.Right);
            _lvStocks.Columns.Add("전일대비", 68, HorizontalAlignment.Right);
            _lvStocks.Columns.Add("평단", 75, HorizontalAlignment.Right);
            _lvStocks.Columns.Add("보유수량", 60, HorizontalAlignment.Right);
            _lvStocks.Columns.Add("손익금(원)", 85, HorizontalAlignment.Right);
            _lvStocks.Columns.Add("수익률", 68, HorizontalAlignment.Right);
            _lvStocks.Columns.Add("AI 등급", 55, HorizontalAlignment.Center);
            _lvStocks.Columns.Add("14일 RSI", 68, HorizontalAlignment.Right);
            _lvStocks.Columns.Add("5일 거래량", 72, HorizontalAlignment.Right);
            _lvStocks.Columns.Add("차트", 48, HorizontalAlignment.Center);
            _lvStocks.Columns.Add("뉴스", 48, HorizontalAlignment.Center);
            _lvStocks.Columns.Add("손익비", 52, HorizontalAlignment.Center);
            _lvStocks.Columns.Add("★종합점수", 75, HorizontalAlignment.Center);
            _lvStocks.Columns.Add("타이밍 안전벨트", 115, HorizontalAlignment.Center);
            _lvStocks.Columns.Add("권고 지침", 220);

            _lvStocks.Resize += (s, e) => AutoFitLastColumn();
            _lvStocks.SelectedIndexChanged += LvStocks_SelectedIndexChanged;
            _lvStocks.ColumnClick += LvStocks_ColumnClick;
            _lvStocks.DoubleClick += (s, e) => EditSelectedStock();
            _lvStocks.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Delete)
                {
                    DeleteSelectedStock();
                    e.Handled = true;
                }
            };

            // 우클릭 컨텍스트 메뉴 설정
            var ctx = new ContextMenuStrip();
            var mnuEdit = new ToolStripMenuItem("종목 정보 수정 ([수정])");
            mnuEdit.Click += (s, e) => EditSelectedStock();
            var mnuDelete = new ToolStripMenuItem("종목 삭제 ([삭제])");
            mnuDelete.Click += (s, e) => DeleteSelectedStock();
            var mnuNaver = new ToolStripMenuItem("네이버 증권 웹페이지 열기");
            mnuNaver.Click += (s, e) => OpenNaverFinanceWeb();

            ctx.Items.Add(mnuEdit);
            ctx.Items.Add(mnuDelete);
            ctx.Items.Add(new ToolStripSeparator());
            ctx.Items.Add(mnuNaver);
            _lvStocks.ContextMenuStrip = ctx;

            // 리스트 더블 버퍼링 적용 (깜빡임 0)
            typeof(ListView).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
                null, _lvStocks, new object[] { true });

            _splitMain.Panel1.Controls.Add(_lvStocks);

            // 우측: 선택 종목 AI 상세 진단 카드 (440px 와이드 레이아웃)
            _pnlDetail = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(24, 28, 38),
                Padding = new Padding(12)
            };

            _lblDetailTitle = new Label
            {
                Text = "선택된 종목이 없습니다",
                Font = new Font("Malgun Gothic", 11.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(140, 200, 255),
                Dock = DockStyle.Top,
                Height = 28
            };

            _lblDetailTimingWarning = new Label
            {
                Text = "🚨 [타이밍 가이드] 지표 분석 대기 중",
                Font = new Font("Malgun Gothic", 8.8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 230, 100),
                BackColor = Color.FromArgb(36, 32, 24),
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Top,
                Height = 38,
                Padding = new Padding(6, 4, 6, 4),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _lblDetailScores = new Label
            {
                Text = "📊 차트: -점 | 뉴스: -점 | 손익비: -점\n★ 최종 종합 점수 : ★ -점 / 10점",
                Font = new Font("Segoe UI", 9.2f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 225, 120),
                Dock = DockStyle.Top,
                Height = 36
            };

            _lblDetailTechnicalFacts = new Label
            {
                Text = "🔍 14일 RSI: - | 거래량: -%\n• 20일 지지선: -원 (하방: -%)",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(170, 210, 255),
                Dock = DockStyle.Top,
                Height = 34
            };

            _lblDetailGroupBadge = new Label
            {
                Text = "포지션 판정: -",
                Font = new Font("Malgun Gothic", 9.5f, FontStyle.Bold),
                ForeColor = Color.Silver,
                Dock = DockStyle.Top,
                Height = 22,
                AutoEllipsis = true
            };

            _lblDetailAction = new Label
            {
                Text = "권고 액션: -",
                Font = new Font("Malgun Gothic", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 215, 100),
                Dock = DockStyle.Top,
                Height = 22,
                AutoEllipsis = true
            };

            _lblDetailReliability = new Label
            {
                Text = "■ 출처 신뢰도: ★★★☆☆ (3/5)",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(220, 220, 230),
                Dock = DockStyle.Top,
                Height = 20
            };

            _lblDetailImpact = new Label
            {
                Text = "⚡ 정책/실적 직결도: 0 (-5 ~ +5)",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(220, 220, 230),
                Dock = DockStyle.Top,
                Height = 20
            };

            _lblDetailTargetCut = new Label
            {
                Text = "◆ 권고 손절/방어선: - 원",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 140, 140),
                Dock = DockStyle.Top,
                Height = 22
            };

            var lblReasonTitle = new Label
            {
                Text = "[분석] AI 참모 진단 사유 및 대응 논리:",
                Font = new Font("Malgun Gothic", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(170, 190, 210),
                Dock = DockStyle.Top,
                Height = 22
            };

            _rtbDetailReason = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.FromArgb(14, 18, 26),
                ForeColor = Color.FromArgb(235, 240, 252),
                Font = new Font("Malgun Gothic", 9.5f),
                BorderStyle = BorderStyle.FixedSingle,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                DetectUrls = false
            };

            var pnlDetailBtns = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 42,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(0, 4, 0, 0)
            };

            _btnDeleteItem = CreateNeonButton("[삭제]", Color.FromArgb(48, 18, 24), Color.FromArgb(255, 65, 85), Color.FromArgb(255, 220, 225), 80);
            _btnDeleteItem.Click += (s, e) => DeleteSelectedStock();

            _btnEditItem = CreateNeonButton("[수정]", Color.FromArgb(18, 36, 56), Color.FromArgb(64, 150, 255), Color.FromArgb(215, 235, 255), 80);
            _btnEditItem.Click += (s, e) => EditSelectedStock();

            pnlDetailBtns.Controls.Add(_btnDeleteItem);
            pnlDetailBtns.Controls.Add(_btnEditItem);

            _pnlDetail.Controls.Add(_rtbDetailReason);
            _pnlDetail.Controls.Add(lblReasonTitle);
            _pnlDetail.Controls.Add(_lblDetailTargetCut);
            _pnlDetail.Controls.Add(_lblDetailImpact);
            _pnlDetail.Controls.Add(_lblDetailReliability);
            _pnlDetail.Controls.Add(_lblDetailAction);
            _pnlDetail.Controls.Add(_lblDetailGroupBadge);
            _pnlDetail.Controls.Add(_lblDetailTechnicalFacts);
            _pnlDetail.Controls.Add(_lblDetailScores);
            _pnlDetail.Controls.Add(_lblDetailTimingWarning);
            _pnlDetail.Controls.Add(_lblDetailTitle);
            _pnlDetail.Controls.Add(pnlDetailBtns);

            _splitMain.Panel2.Controls.Add(_pnlDetail);

            // 메인 컨트롤 배치 (도킹 역순)
            this.Controls.Add(_splitMain);
            this.Controls.Add(_pnlFooter);
            this.Controls.Add(_pnlMacroTicker);
            this.Controls.Add(_pnlHeader);

            this.ResumeLayout(false);
        }

        private void AutoFitLastColumn()
        {
            if (_lvStocks == null || _lvStocks.Columns.Count < 17) return;
            try
            {
                int fixedWidth = 0;
                int lastIdx = _lvStocks.Columns.Count - 1;
                for (int i = 0; i < lastIdx; i++)
                {
                    fixedWidth += _lvStocks.Columns[i].Width;
                }
                int remain = _lvStocks.ClientSize.Width - fixedWidth - 4;
                _lvStocks.Columns[lastIdx].Width = Math.Max(220, remain);
            }
            catch { }
        }

        private Button CreateNeonButton(string text, Color bg, Color borderColor, Color foreColor, int width = 0)
        {
            var btn = new Button
            {
                Text = text,
                BackColor = bg,
                ForeColor = foreColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Malgun Gothic", 9.0f, FontStyle.Bold),
                Width = width,
                Height = 34,
                Margin = new Padding(2),
                Padding = new Padding(0),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = borderColor;

            int hr = Math.Min(255, bg.R + 25);
            int hg = Math.Min(255, bg.G + 25);
            int hb = Math.Min(255, bg.B + 25);
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(hr, hg, hb);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(Math.Max(0, bg.R - 10), Math.Max(0, bg.G - 10), Math.Max(0, bg.B - 10));

            return btn;
        }

        private Button CreateHeaderButton(string text, Color bg, int width)
        {
            Color borderColor = Color.FromArgb(Math.Min(255, bg.R + 60), Math.Min(255, bg.G + 60), Math.Min(255, bg.B + 60));
            return CreateNeonButton(text, bg, borderColor, Color.White, width);
        }

        #endregion
    }
}

