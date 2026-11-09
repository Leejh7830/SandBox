using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GateHelper.StockAdvisor.Models;
using GateHelper.StockAdvisor.Security;
using GateHelper.StockAdvisor.Services;
using Newtonsoft.Json;

namespace GateHelper.StockAdvisor
{
    public partial class StockAdvisorControl : UserControl
    {
        private PortfolioData _portfolio = new PortfolioData();
        private static readonly string SaveFilePath = Path.Combine(Util.MetaFolder, "stock_portfolio.dat");

        // UI 컨트롤 요소
        private Panel _pnlHeader;
        private Label _lblTrafficLight;
        private Label _lblBriefing1;
        private Label _lblBriefing2;
        private Label _lblBriefing3;
        private Button _btnDiagnose;
        private Button _btnRefreshPrice;
        private Button _btnAddStock;
        private Button _btnEmergencyGuide;

        // 글로벌 매크로 티커 바
        private Panel _pnlMacroTicker;
        private Label _lblMacroTicker;

        private SplitContainer _splitMain;
        private ListView _lvStocks;
        private Panel _pnlDetail;

        // 상세 패널 요소
        private Label _lblDetailTitle;
        private Label _lblDetailGroupBadge;
        private Label _lblDetailAction;
        private Label _lblDetailReliability;
        private Label _lblDetailImpact;
        private Label _lblDetailTargetCut;
        private TextBox _txtDetailReason;
        private Button _btnEditItem;
        private Button _btnDeleteItem;

        // 하단 상태바
        private Panel _pnlFooter;
        private Label _lblAccountSummary;
        private Label _lblLastUpdated;

        private bool _isBusy = false;
        private GlobalMarketSnapshot _macroSnapshot;

        public StockAdvisorControl()
        {
            InitializeComponent();
            BuildUiLayout();
            LoadPortfolio();
            UpdateUi();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            AdjustSplitter();
        }

        private void AdjustSplitter()
        {
            if (_splitMain == null || this.Width < 600) return;
            try
            {
                // 우측 상세 패널에 420px를 보장하여 가독성 리포트가 잘리지 않도록 동적 분할
                int targetDist = this.Width - 420;
                if (targetDist >= 400 && targetDist < this.Width - 150)
                {
                    _splitMain.SplitterDistance = targetDist;
                }
            }
            catch { }
        }

        public void OnTabVisibilityChanged(bool isVisible)
        {
            if (isVisible)
            {
                // 탭 진입 시 시세 자동 새로고침 (유휴 CPU 0% 유지)
                _ = RefreshPricesAsync();
            }
        }

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
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280f));  // 우측 버튼 (고정)
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

            var lblHeaderTitle = new Label
            {
                Text = "◆ [AI 주식 리스크 참모] 오늘의 모닝 브리핑 & 거시 동향",
                Font = new Font("Malgun Gothic", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(120, 210, 255),
                Dock = DockStyle.Top,
                Height = 20
            };

            _lblBriefing1 = new Label { Text = "• 1행: 글로벌 매크로 및 미 수출통제(BIS) 동향 점검 중", Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(220, 225, 235), Dock = DockStyle.Top, Height = 20 };
            _lblBriefing2 = new Label { Text = "• 2행: 국내 증시 및 주력 반도체/IT 섹터 리스크 모니터링", Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(200, 205, 215), Dock = DockStyle.Top, Height = 20 };
            _lblBriefing3 = new Label { Text = "• 3행: 뇌동매매 금지! 추천 포지션 지침을 확인하세요.", Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(160, 210, 160), Dock = DockStyle.Top, Height = 20 };

            pnlBriefingText.Controls.Add(_lblBriefing3);
            pnlBriefingText.Controls.Add(_lblBriefing2);
            pnlBriefingText.Controls.Add(_lblBriefing1);
            pnlBriefingText.Controls.Add(lblHeaderTitle);

            // 우측 상단 액션 버튼 영역 (네온 사이버 스타일 2행 2열 그리드)
            var tblButtons = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Margin = new Padding(0)
            };
            tblButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblButtons.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            tblButtons.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            _btnDiagnose = CreateNeonButton("★ AI 참모 진단", Color.FromArgb(14, 38, 56), Color.FromArgb(0, 216, 255), Color.FromArgb(210, 248, 255));
            _btnDiagnose.Dock = DockStyle.Fill;
            _btnDiagnose.Click += async (s, e) => await RunAiDiagnosisAsync();

            _btnRefreshPrice = CreateNeonButton("↻ 시세 갱신", Color.FromArgb(20, 34, 56), Color.FromArgb(64, 150, 255), Color.FromArgb(215, 235, 255));
            _btnRefreshPrice.Dock = DockStyle.Fill;
            _btnRefreshPrice.Click += async (s, e) => await RefreshPricesAsync();

            _btnAddStock = CreateNeonButton("+ 종목 추가", Color.FromArgb(16, 42, 32), Color.FromArgb(0, 230, 118), Color.FromArgb(215, 255, 230));
            _btnAddStock.Dock = DockStyle.Fill;
            _btnAddStock.Click += (s, e) => ShowAddStockDialog();

            _btnEmergencyGuide = CreateNeonButton("▲ 비상 탈출", Color.FromArgb(48, 16, 24), Color.FromArgb(255, 50, 85), Color.FromArgb(255, 220, 230));
            _btnEmergencyGuide.Dock = DockStyle.Fill;
            _btnEmergencyGuide.Click += (s, e) => ShowPanicExitDialog();

            tblButtons.Controls.Add(_btnDiagnose, 0, 0);
            tblButtons.Controls.Add(_btnRefreshPrice, 1, 0);
            tblButtons.Controls.Add(_btnAddStock, 0, 1);
            tblButtons.Controls.Add(_btnEmergencyGuide, 1, 1);

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
                Text = "시세 갱신: 대기 중",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(140, 150, 165),
                Dock = DockStyle.Right,
                AutoSize = true
            };

            _pnlFooter.Controls.Add(_lblAccountSummary);
            _pnlFooter.Controls.Add(_lblLastUpdated);

            // 3. 중앙 분할 컨테이너 (좌측 리스트뷰 9개 컬럼 최적폭 850px, 우측 상세 카드 410px 쾌적 확보)
            _splitMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 850,
                BackColor = Color.FromArgb(30, 34, 46)
            };

            // 좌측: 종목 리스트뷰
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
            _lvStocks.Columns.Add("종목명 (코드)", 140);
            _lvStocks.Columns.Add("수량", 65, HorizontalAlignment.Right);
            _lvStocks.Columns.Add("매수가", 80, HorizontalAlignment.Right);
            _lvStocks.Columns.Add("현재가", 85, HorizontalAlignment.Right);
            _lvStocks.Columns.Add("수익률", 75, HorizontalAlignment.Right);
            _lvStocks.Columns.Add("평가손익", 85, HorizontalAlignment.Right);
            _lvStocks.Columns.Add("비중", 50, HorizontalAlignment.Right);
            _lvStocks.Columns.Add("AI 포지션", 95, HorizontalAlignment.Center);
            _lvStocks.Columns.Add("권고 지침", 160);

            _lvStocks.SelectedIndexChanged += LvStocks_SelectedIndexChanged;
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

            // 우측: 선택 종목 AI 상세 진단 카드
            _pnlDetail = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(24, 28, 38),
                Padding = new Padding(14)
            };

            _lblDetailTitle = new Label
            {
                Text = "선택된 종목이 없습니다",
                Font = new Font("Malgun Gothic", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(140, 200, 255),
                Dock = DockStyle.Top,
                Height = 30
            };

            _lblDetailGroupBadge = new Label
            {
                Text = "포지션 판정: -",
                Font = new Font("Malgun Gothic", 9.5f, FontStyle.Bold),
                ForeColor = Color.Silver,
                Dock = DockStyle.Top,
                Height = 24,
                AutoEllipsis = true
            };

            _lblDetailAction = new Label
            {
                Text = "권고 액션: -",
                Font = new Font("Malgun Gothic", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 215, 100),
                Dock = DockStyle.Top,
                Height = 24,
                AutoEllipsis = true
            };

            _lblDetailReliability = new Label
            {
                Text = "■ 출처 신뢰도: ★★★☆☆ (3/5)",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(220, 220, 230),
                Dock = DockStyle.Top,
                Height = 22
            };

            _lblDetailImpact = new Label
            {
                Text = "⚡ 정책/실적 직결도: 0 (-5 ~ +5)",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(220, 220, 230),
                Dock = DockStyle.Top,
                Height = 22
            };

            _lblDetailTargetCut = new Label
            {
                Text = "◆ 권고 손절/방어선: - 원",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 140, 140),
                Dock = DockStyle.Top,
                Height = 24
            };

            var lblReasonTitle = new Label
            {
                Text = "[분석] AI 참모 진단 사유 및 대응 논리:",
                Font = new Font("Malgun Gothic", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(170, 190, 210),
                Dock = DockStyle.Top,
                Height = 22
            };

            _txtDetailReason = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                BackColor = Color.FromArgb(14, 18, 26),
                ForeColor = Color.FromArgb(235, 240, 252),
                Font = new Font("Malgun Gothic", 9.5f),
                BorderStyle = BorderStyle.FixedSingle,
                ScrollBars = ScrollBars.Vertical
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

            _pnlDetail.Controls.Add(_txtDetailReason);
            _pnlDetail.Controls.Add(lblReasonTitle);
            _pnlDetail.Controls.Add(_lblDetailTargetCut);
            _pnlDetail.Controls.Add(_lblDetailImpact);
            _pnlDetail.Controls.Add(_lblDetailReliability);
            _pnlDetail.Controls.Add(_lblDetailAction);
            _pnlDetail.Controls.Add(_lblDetailGroupBadge);
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

        private Button CreateNeonButton(string text, Color bg, Color borderColor, Color foreColor, int width = 0)
        {
            var btn = new Button
            {
                Text = text,
                BackColor = bg,
                ForeColor = foreColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Malgun Gothic", 9.2f, FontStyle.Bold),
                Width = width,
                Height = 34,
                Margin = new Padding(3),
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

        #region Business Logic & Data Binding

        private void LoadPortfolio()
        {
            try
            {
                string json = DpapiHelper.LoadEncryptedFile(SaveFilePath);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    _portfolio = JsonConvert.DeserializeObject<PortfolioData>(json) ?? new PortfolioData();
                }
                else
                {
                    // 최초 실행 시 기본 예시 종목 (삼성전자, SK하이닉스) 탑재
                    _portfolio.Items.Add(new StockHoldingItem
                    {
                        StockCode = "005930",
                        StockName = "삼성전자",
                        Quantity = 50,
                        BuyPrice = 75000,
                        Memo = "반도체 HBM 공급망 모멘텀 및 정책 동향 주목"
                    });
                    _portfolio.Items.Add(new StockHoldingItem
                    {
                        StockCode = "000660",
                        StockName = "SK하이닉스",
                        Quantity = 20,
                        BuyPrice = 195000,
                        Memo = "HBM3E 독점 납품 및 AI 서버 수요"
                    });
                    SavePortfolio();
                }
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "StockAdvisorControl.LoadPortfolio");
                _portfolio = new PortfolioData();
            }
        }

        private void SavePortfolio()
        {
            try
            {
                string json = JsonConvert.SerializeObject(_portfolio, Formatting.Indented);
                DpapiHelper.SaveEncryptedFile(SaveFilePath, json);
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "StockAdvisorControl.SavePortfolio");
            }
        }

        private void UpdateUi()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(UpdateUi));
                return;
            }

            // 1. 상단 브리핑 및 신호등 갱신
            var briefing = _portfolio.LastBriefing ?? new MarketBriefing();
            _lblBriefing1.Text = "• " + briefing.Line1;
            _lblBriefing2.Text = "• " + briefing.Line2;
            _lblBriefing3.Text = "• " + briefing.Line3;

            switch (briefing.RiskLevel)
            {
                case OverallRiskLevel.Safe:
                    _lblTrafficLight.Text = "●\n안전\n(뉴스📰)";
                    _lblTrafficLight.ForeColor = Color.FromArgb(70, 220, 140);
                    break;
                case OverallRiskLevel.Caution:
                    _lblTrafficLight.Text = "▲\n주의\n(뉴스📰)";
                    _lblTrafficLight.ForeColor = Color.FromArgb(240, 200, 60);
                    break;
                case OverallRiskLevel.Panic:
                    _lblTrafficLight.Text = "■\n비상\n(뉴스📰)";
                    _lblTrafficLight.ForeColor = Color.FromArgb(255, 80, 80);
                    break;
            }

            // 2. 비중 계산
            decimal totalEval = 0m;
            decimal totalBuy = 0m;
            foreach (var item in _portfolio.Items)
            {
                totalEval += item.TotalEvalAmount;
                totalBuy += item.TotalBuyAmount;
            }
            foreach (var item in _portfolio.Items)
            {
                item.WeightPercent = (totalEval > 0) ? (item.TotalEvalAmount / totalEval * 100m) : 0m;
            }

            // 3. 리스트뷰 갱신
            _lvStocks.BeginUpdate();
            _lvStocks.Items.Clear();

            foreach (var item in _portfolio.Items)
            {
                var lvi = new ListViewItem(new[]
                {
                    $"{item.StockName} ({item.StockCode})",
                    $"{item.Quantity:N0}",
                    $"{item.BuyPrice:N0}",
                    $"{item.CurrentPrice:N0}",
                    $"{item.ProfitRate:+0.00;-0.00}%",
                    $"{item.ProfitAmount:+0,0;-0,0;0}",
                    $"{item.WeightPercent:F1}%",
                    GetGroupDisplay(item.LastDiagnosis?.Group ?? PositionGroup.GroupB),
                    item.LastDiagnosis?.RecommendedAction ?? "진단 대기"
                });

                lvi.Tag = item;

                // 손익 색상 (수익: 빨강, 손실: 파랑)
                if (item.ProfitRate > 0)
                {
                    lvi.SubItems[4].ForeColor = Color.FromArgb(255, 95, 95);
                    lvi.SubItems[5].ForeColor = Color.FromArgb(255, 95, 95);
                }
                else if (item.ProfitRate < 0)
                {
                    lvi.SubItems[4].ForeColor = Color.FromArgb(85, 160, 255);
                    lvi.SubItems[5].ForeColor = Color.FromArgb(85, 160, 255);
                }

                // AI 포지션 색상
                if (item.LastDiagnosis != null)
                {
                    if (item.LastDiagnosis.Group == PositionGroup.GroupA)
                        lvi.SubItems[7].ForeColor = Color.FromArgb(255, 110, 110);
                    else if (item.LastDiagnosis.Group == PositionGroup.GroupC)
                        lvi.SubItems[7].ForeColor = Color.FromArgb(100, 220, 150);
                    else
                        lvi.SubItems[7].ForeColor = Color.FromArgb(240, 205, 90);
                }

                lvi.UseItemStyleForSubItems = false;
                _lvStocks.Items.Add(lvi);
            }
            _lvStocks.EndUpdate();

            // 4. 하단 요약바 갱신
            decimal totalProfit = totalEval - totalBuy;
            decimal totalRate = (totalBuy > 0) ? (totalProfit / totalBuy * 100m) : 0m;
            string rateColor = totalProfit >= 0 ? "text-red-400" : "text-blue-400";
            _lblAccountSummary.Text = $"총 매수: {totalBuy:N0}원  |  총 평가: {totalEval:N0}원  |  손익: {totalProfit:+0,0;-0,0;0}원 ({totalRate:+0.00;-0.00}%)";
            _lblAccountSummary.ForeColor = totalProfit >= 0 ? Color.FromArgb(255, 110, 110) : Color.FromArgb(100, 175, 255);

            // 첫 번째 종목 자동 선택
            if (_lvStocks.Items.Count > 0 && _lvStocks.SelectedItems.Count == 0)
            {
                _lvStocks.Items[0].Selected = true;
            }
            else
            {
                UpdateDetailPanel(null);
            }
        }

        private string GetGroupDisplay(PositionGroup group)
        {
            switch (group)
            {
                case PositionGroup.GroupA: return "[A:손절권고]";
                case PositionGroup.GroupC: return "[C:분할매수]";
                default: return "[B:관망유지]";
            }
        }

        private void LvStocks_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_lvStocks.SelectedItems.Count > 0)
            {
                var item = _lvStocks.SelectedItems[0].Tag as StockHoldingItem;
                UpdateDetailPanel(item);
            }
            else
            {
                UpdateDetailPanel(null);
            }
        }

        private void UpdateDetailPanel(StockHoldingItem item)
        {
            if (item == null)
            {
                _lblDetailTitle.Text = "선택된 종목이 없습니다";
                _lblDetailGroupBadge.Text = "포지션: -";
                _lblDetailAction.Text = "권고 액션: -";
                _lblDetailReliability.Text = "■ 출처 신뢰도: -";
                _lblDetailImpact.Text = "⚡ 정책/실적 직결도: -";
                _lblDetailTargetCut.Text = "◆ 권고 손절/방어선: - 원";
                _txtDetailReason.Text = "좌측 목록에서 종목을 선택하시면 AI 수석 참모의 리스크 진단 사유와 대응 지침을 열람할 수 있습니다.";
                return;
            }

            _lblDetailTitle.Text = $"◆ {item.StockName} ({item.StockCode})";
            var diag = item.LastDiagnosis;
            if (diag != null)
            {
                _lblDetailGroupBadge.Text = $"포지션 판정: {GetGroupDisplay(diag.Group)}";
                _lblDetailGroupBadge.ForeColor = diag.Group == PositionGroup.GroupA 
                    ? Color.FromArgb(255, 80, 100) 
                    : (diag.Group == PositionGroup.GroupC ? Color.FromArgb(0, 230, 118) : Color.FromArgb(255, 205, 70));

                _lblDetailAction.Text = $"▶ 권고 액션: {diag.RecommendedAction}";

                string stars = new string('★', diag.SourceReliability) + new string('☆', 5 - diag.SourceReliability);
                _lblDetailReliability.Text = $"■ 출처 신뢰도: {stars} ({diag.SourceReliability}/5점)";

                string impactShort = diag.PolicyImpact > 0 ? "호재 수혜" : (diag.PolicyImpact < 0 ? "악재 규제" : "중립");
                _lblDetailImpact.Text = $"⚡ 정책/실적 직결도: {(diag.PolicyImpact > 0 ? "+" : "")}{diag.PolicyImpact}점 ({impactShort})";
                _lblDetailTargetCut.Text = diag.TargetCutPrice > 0 ? $"◆ 권고 손절/방어선: {diag.TargetCutPrice:N0}원" : "◆ 권고 손절/방어선: 시장 상황 주시";

                _txtDetailReason.Text = FormatDetailReasonReport(item, diag);
            }
            else
            {
                _lblDetailGroupBadge.Text = "포지션: AI 진단 대기 중";
                _lblDetailGroupBadge.ForeColor = Color.Silver;
                _lblDetailAction.Text = "권고 액션: [★ AI 참모 진단] 필요";
                _lblDetailReliability.Text = "■ 출처 신뢰도: 대기 중";
                _lblDetailImpact.Text = "⚡ 정책/실적 직결도: 대기 중";
                _lblDetailTargetCut.Text = $"◆ 현재 매수가: {item.BuyPrice:N0}원";
                _txtDetailReason.Text = "상단의 [★ AI 참모 진단] 버튼을 클릭하시면 최신 글로벌 매크로 및 섹터 동향을 바탕으로 심층 리스크 진단을 제공합니다.";
            }
        }

        private string FormatDetailReasonReport(StockHoldingItem item, StockRiskDiagnosis diag)
        {
            var sb = new StringBuilder();
            sb.AppendLine("----------------------------------------");
            sb.AppendLine($"  [AI 주식 참모 진단 리포트]  {diag.DiagnosedAt:yyyy-MM-dd HH:mm}");
            sb.AppendLine("----------------------------------------");
            sb.AppendLine();

            sb.AppendLine("【 1. 핵심 권고 행동 지침 】");
            sb.AppendLine($"  ▶ 권고 액션 : {diag.RecommendedAction}");
            if (diag.TargetCutPrice > 0)
            {
                sb.AppendLine($"  ▶ 권고 손절/방어선 : {diag.TargetCutPrice:N0}원");
            }
            sb.AppendLine();

            sb.AppendLine("【 2. 거시 지표 & 정책 영향 】");
            string stars = new string('★', diag.SourceReliability) + new string('☆', 5 - diag.SourceReliability);
            sb.AppendLine($"  • 정보 출처 신뢰도 : {stars} ({diag.SourceReliability} / 5점)");
            string impactDesc = diag.PolicyImpact > 0 ? "호재성 정책/실적 수혜" : (diag.PolicyImpact < 0 ? "악재성 규제/비용 충격" : "중립 영향");
            sb.AppendLine($"  • 정책/실적 직결도 : {(diag.PolicyImpact > 0 ? "+" : "")}{diag.PolicyImpact}점 ({impactDesc})");
            sb.AppendLine();

            sb.AppendLine("【 3. 심층 대응 논리 및 판단 근거 】");
            string reasonText = diag.ReasonSummary ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(reasonText))
            {
                // 문장 단위로 분리하여 불릿 포인트로 줄바꿈 가독성 극대화
                var sentences = reasonText.Split(new[] { ". ", ".\n", ".\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var s in sentences)
                {
                    string trimmed = s.Trim().TrimEnd('.');
                    if (!string.IsNullOrWhiteSpace(trimmed))
                    {
                        sb.AppendLine($"  • {trimmed}.");
                        sb.AppendLine();
                    }
                }
            }
            else
            {
                sb.AppendLine("  • 상세 대응 논리가 기록되지 않았습니다.");
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(item.Memo))
            {
                sb.AppendLine("【 4. 사용자 투자 메모 】");
                sb.AppendLine($"  • {item.Memo}");
            }

            return sb.ToString().TrimEnd();
        }

        #endregion

        #region Actions (Async & Dialogs)

        public async Task RefreshPricesAsync()
        {
            if (_isBusy) return;
            _isBusy = true;
            _btnRefreshPrice.Enabled = false;
            _btnRefreshPrice.Text = "조회 중...";

            try
            {
                var priceTask = RefreshPricesInternalAsync();
                var intelTask = GlobalMarketPollerService.FetchMarketIntelAsync();
                await Task.WhenAll(priceTask, intelTask);

                UpdateMacroTicker(intelTask.Result);

                _lblLastUpdated.Text = $"시세 갱신: {DateTime.Now:HH:mm:ss}";
                SavePortfolio();
                UpdateUi();
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "StockAdvisorControl.RefreshPricesAsync");
            }
            finally
            {
                _isBusy = false;
                _btnRefreshPrice.Enabled = true;
                _btnRefreshPrice.Text = "↻ 시세 갱신";
            }
        }

        private void UpdateMacroTicker(GlobalMarketSnapshot intel)
        {
            if (intel == null) return;
            _macroSnapshot = intel;
            if (_lblMacroTicker == null) return;
            try
            {
                string tickerText = intel.GetTickerText();
                if (intel.RecentPolicyHeadlines.Count > 0)
                {
                    tickerText += "   |   미 연방관보(BIS): " + intel.RecentPolicyHeadlines[0];
                }
                _lblMacroTicker.Text = "⚡ [글로벌 매크로 인텔]  " + tickerText;
            }
            catch { }
        }

        private void ShowMarketIntelNewsDialog()
        {
            try
            {
                using (var dlg = new MarketIntelNewsDialog(_macroSnapshot, _portfolio?.Items))
                {
                    var parentForm = this.FindForm();
                    if (parentForm != null)
                    {
                        dlg.ShowDialog(parentForm);
                    }
                    else
                    {
                        dlg.ShowDialog(this);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("실시간 뉴스 브리핑 창을 여는 중 오류가 발생했습니다: " + ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OpenNaverFinanceWeb()
        {
            if (_lvStocks.SelectedItems.Count == 0) return;
            var item = _lvStocks.SelectedItems[0].Tag as StockHoldingItem;
            if (item != null && !string.IsNullOrEmpty(item.StockCode))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = $"https://finance.naver.com/item/main.naver?code={item.StockCode}",
                        UseShellExecute = true
                    });
                }
                catch { }
            }
        }

        private async Task RefreshPricesInternalAsync()
        {
            foreach (var item in _portfolio.Items)
            {
                var priceDto = await NaverStockPriceService.FetchPriceAsync(item.StockCode);
                if (priceDto.IsSuccess)
                {
                    item.CurrentPrice = priceDto.CurrentPrice;
                    item.ChangeRate = priceDto.ChangeRate;
                    item.ChangePrice = priceDto.ChangePrice;
                    if (!string.IsNullOrWhiteSpace(priceDto.StockName))
                    {
                        item.StockName = priceDto.StockName;
                    }
                }
            }
        }

        private async Task RunAiDiagnosisAsync()
        {
            if (_isBusy) return;
            if (_portfolio.Items.Count == 0)
            {
                MessageBox.Show("진단할 종목이 없습니다. [+ 종목 추가] 버튼을 눌러 먼저 종목을 등록하세요.", "종목 없음", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _isBusy = true;
            _btnDiagnose.Enabled = false;
            _btnDiagnose.Text = "AI 분석 중...";

            try
            {
                // 1. 최신 시세 갱신 및 글로벌 지표 병렬 수집
                var priceTask = RefreshPricesInternalAsync();
                var intelTask = GlobalMarketPollerService.FetchMarketIntelAsync();
                await Task.WhenAll(priceTask, intelTask);

                var marketIntel = intelTask.Result;
                UpdateMacroTicker(marketIntel);

                // 2. AI 리스크 진단 실행 (글로벌 매크로 스냅샷 주입)
                var diagResult = await GeminiRiskAdvisorService.DiagnosePortfolioAsync(_portfolio.Items, marketIntel);
                if (diagResult.IsSuccess)
                {
                    _portfolio.LastBriefing = diagResult.Briefing;

                    // 종목별 결과 매핑
                    foreach (var itemDiag in diagResult.ItemDiagnoses)
                    {
                        var matched = _portfolio.Items.Find(x => x.StockCode.TrimStart('0') == itemDiag.StockCode.TrimStart('0'));
                        if (matched != null)
                        {
                            matched.LastDiagnosis = itemDiag;
                        }
                    }

                    _lblLastUpdated.Text = $"시세 갱신: {DateTime.Now:HH:mm:ss}";
                    SavePortfolio();
                    UpdateUi();

                    if (!string.IsNullOrEmpty(diagResult.ErrorMessage))
                    {
                        MessageBox.Show(diagResult.ErrorMessage, "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    MessageBox.Show(diagResult.ErrorMessage, "진단 실패", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"진단 중 오류 발생: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isBusy = false;
                _btnDiagnose.Enabled = true;
                _btnDiagnose.Text = "★ AI 참모 진단";
            }
        }

        private void ShowAddStockDialog()
        {
            using (var dlg = new StockItemDialog())
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    // 중복 체크
                    var existing = _portfolio.Items.Find(x => x.StockCode == dlg.Item.StockCode);
                    if (existing != null)
                    {
                        existing.Quantity = dlg.Item.Quantity;
                        existing.BuyPrice = dlg.Item.BuyPrice;
                        existing.Memo = dlg.Item.Memo;
                    }
                    else
                    {
                        _portfolio.Items.Add(dlg.Item);
                    }

                    SavePortfolio();
                    _ = RefreshPricesAsync();
                }
            }
        }

        private void EditSelectedStock()
        {
            if (_lvStocks.SelectedItems.Count == 0) return;
            var item = _lvStocks.SelectedItems[0].Tag as StockHoldingItem;
            if (item == null) return;

            using (var dlg = new StockItemDialog(item))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    SavePortfolio();
                    _ = RefreshPricesAsync();
                }
            }
        }

        private void DeleteSelectedStock()
        {
            if (_lvStocks.SelectedItems.Count == 0) return;
            var item = _lvStocks.SelectedItems[0].Tag as StockHoldingItem;
            if (item == null) return;

            var confirm = MessageBox.Show($"[{item.StockName}({item.StockCode})] 종목을 포트폴리오에서 삭제하시겠습니까?", "종목 삭제", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm == DialogResult.Yes)
            {
                _portfolio.Items.Remove(item);
                SavePortfolio();
                UpdateUi();
            }
        }

        private void ShowPanicExitDialog()
        {
            if (_portfolio.Items.Count == 0)
            {
                MessageBox.Show("현재 포트폴리오에 보유 종목이 없습니다.", "비상 탈출", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new PanicExitDialog(_portfolio.Items))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK && dlg.ExecutionCompleted)
                {
                    SavePortfolio();
                    UpdateUi();
                }
            }
        }

        #endregion
    }
}
