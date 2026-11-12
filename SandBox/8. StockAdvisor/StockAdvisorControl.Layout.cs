using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GateHelper.StockAdvisor.Models;

namespace GateHelper.StockAdvisor
{
    public partial class StockAdvisorControl
    {
        #region UI Layout Construction

        private void BuildUiLayout()
        {
            this.SuspendLayout();

            // 1. 상단 슬림 툴바 헤더 (Height = 54, 버튼 테두리 잘림 100% 방지)
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 54,
                BackColor = Color.FromArgb(20, 25, 36),
                Padding = new Padding(12, 8, 12, 8)
            };

            // 좌측: 타이틀 및 시장 상태 신호등 뱃지
            var pnlLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = 374,
                BackColor = Color.Transparent
            };

            var lblHeaderTitle = new Label
            {
                Text = "AI 주식 리스크 참모",
                Font = new Font("Malgun Gothic", 10.2f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 230, 255),
                AutoSize = true,
                Location = new Point(0, 8)
            };

            // 신호등 뱃지 (가로 1행 단정 레이아웃)
            _lblTrafficLight = new Label
            {
                Text = "● 시장안전 (뉴스)",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Malgun Gothic", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 220, 140),
                BackColor = Color.FromArgb(28, 36, 50),
                BorderStyle = BorderStyle.FixedSingle,
                Size = new Size(108, 34),
                Location = new Point(140, 2),
                Cursor = Cursors.Hand
            };
            _lblTrafficLight.MouseEnter += (s, e) => _lblTrafficLight.BackColor = Color.FromArgb(42, 54, 74);
            _lblTrafficLight.MouseLeave += (s, e) => _lblTrafficLight.BackColor = Color.FromArgb(28, 36, 50);
            _lblTrafficLight.Click += (s, e) => ShowMarketIntelNewsDialog();

            var ttTraffic = new ToolTip();
            ttTraffic.SetToolTip(_lblTrafficLight, "클릭 시 엔비디아(NVDA)/삼성전자/SK하이닉스 실시간 뉴스 및 글로벌 지표 브리핑이 열립니다.");

            // 시장 체온계 뱃지 (코스피 20일선 이격도 기반 실시간 과열/공포 측정기)
            _lblMarketThermometer = new Label
            {
                Text = "체온 36.5℃ [정상]",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Malgun Gothic", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 230, 150),
                BackColor = Color.FromArgb(22, 40, 36),
                BorderStyle = BorderStyle.FixedSingle,
                Size = new Size(118, 34),
                Location = new Point(252, 2),
                Cursor = Cursors.Hand
            };
            _lblMarketThermometer.MouseEnter += (s, e) => _lblMarketThermometer.BackColor = Color.FromArgb(32, 54, 48);
            _lblMarketThermometer.MouseLeave += (s, e) => _lblMarketThermometer.BackColor = _marketTemperature?.BadgeBackColor ?? Color.FromArgb(22, 40, 36);
            _lblMarketThermometer.Click += (s, e) => ShowMarketThermometerDialog();

            var ttThermo = new ToolTip();
            ttThermo.SetToolTip(_lblMarketThermometer, "클릭 시 코스피 20일선 이격도 및 시장 체온계 상세 대응 가이드가 열립니다.");

            pnlLeft.Controls.Add(lblHeaderTitle);
            pnlLeft.Controls.Add(_lblTrafficLight);
            pnlLeft.Controls.Add(_lblMarketThermometer);

            // 우측: 핵심 액션 버튼 (가로 1행 나열 FlowLayoutPanel)
            var flpButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            _btnDiagnose = CreateNeonButton("", Color.FromArgb(24, 28, 38), Color.FromArgb(50, 60, 80), Color.FromArgb(140, 160, 185));
            _btnDiagnose.Size = new Size(116, 34);
            _btnDiagnose.Margin = new Padding(2, 1, 2, 1);
            _btnDiagnose.Click += async (s, e) => await RunAiDiagnosisAsync();

            // 사용자 요청: AI 참모진단 성공 시 버튼 안 텍스트 아래에 동기화 시간을 작게 표시
            _btnDiagnose.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                if (!string.IsNullOrEmpty(_diagnoseCustomStatus))
                {
                    g.DrawString("AI 참모 진단", DiagnoseTitleFont, BrushGoldTitle, new RectangleF(0, 2, _btnDiagnose.Width, 15), CenterStringFormat);
                    g.DrawString(_diagnoseCustomStatus, DiagnoseSubFont, BrushGoldSub, new RectangleF(0, 17, _btnDiagnose.Width, 14), CenterStringFormat);
                }
                else if (_lastDiagnoseSuccessTime.HasValue)
                {
                    g.DrawString("AI 참모 진단", DiagnoseTitleFont, BrushCyanBright, new RectangleF(0, 2, _btnDiagnose.Width, 15), CenterStringFormat);
                    string timeText = $"{_lastDiagnoseSuccessTime.Value:HH:mm:ss} 동기화";
                    g.DrawString(timeText, DiagnoseSubFont, BrushCyanSub, new RectangleF(0, 17, _btnDiagnose.Width, 14), CenterStringFormat);
                }
                else
                {
                    g.DrawString("AI 참모 진단", DiagnoseDefaultFont, BrushMutedTitle, new RectangleF(0, 0, _btnDiagnose.Width, _btnDiagnose.Height), CenterStringFormat);
                }
            };

            _btnLeaderRadar = CreateNeonButton("주도주 레이더", Color.FromArgb(52, 20, 50), Color.FromArgb(255, 90, 215), Color.FromArgb(255, 220, 248));
            _btnLeaderRadar.Font = new Font("Malgun Gothic", 8.8f, FontStyle.Bold);
            _btnLeaderRadar.Size = new Size(95, 34);
            _btnLeaderRadar.Margin = new Padding(2, 1, 2, 1);
            _btnLeaderRadar.Click += (s, e) => ShowMarketLeaderRadarDialog();

            _btnEvaluateStock = CreateNeonButton("종목 평가", Color.FromArgb(16, 44, 58), Color.FromArgb(0, 220, 245), Color.FromArgb(215, 250, 255));
            _btnEvaluateStock.Font = new Font("Malgun Gothic", 8.8f, FontStyle.Bold);
            _btnEvaluateStock.Size = new Size(76, 34);
            _btnEvaluateStock.Margin = new Padding(2, 1, 2, 1);
            _btnEvaluateStock.Click += (s, e) => ShowStockEvaluatorDialog();

            _btnRefreshPrice = CreateNeonButton("", Color.FromArgb(20, 34, 56), Color.FromArgb(64, 150, 255), Color.FromArgb(215, 235, 255));
            _btnRefreshPrice.Size = new Size(95, 34);
            _btnRefreshPrice.Margin = new Padding(2, 1, 2, 1);
            _btnRefreshPrice.Click += async (s, e) => await RefreshPricesAsync();

            // 사용자 요청: 시세 갱신도 버튼 안 텍스트 아래에 동기화 시간을 작게 표시
            _btnRefreshPrice.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                if (!string.IsNullOrEmpty(_priceRefreshCustomStatus))
                {
                    g.DrawString("시세 갱신", DiagnoseTitleFont, BrushGoldTitle, new RectangleF(0, 2, _btnRefreshPrice.Width, 15), CenterStringFormat);
                    g.DrawString(_priceRefreshCustomStatus, DiagnoseSubFont, BrushGoldSub, new RectangleF(0, 17, _btnRefreshPrice.Width, 14), CenterStringFormat);
                }
                else if (_lastPriceRefreshSuccessTime.HasValue)
                {
                    g.DrawString("시세 갱신", DiagnoseTitleFont, BrushCyanBright, new RectangleF(0, 2, _btnRefreshPrice.Width, 15), CenterStringFormat);
                    string timeText = $"{_lastPriceRefreshSuccessTime.Value:HH:mm:ss} 동기화";
                    g.DrawString(timeText, DiagnoseSubFont, BrushCyanSub, new RectangleF(0, 17, _btnRefreshPrice.Width, 14), CenterStringFormat);
                }
                else
                {
                    g.DrawString("시세 갱신", DiagnoseDefaultFont, BrushMutedTitle, new RectangleF(0, 0, _btnRefreshPrice.Width, _btnRefreshPrice.Height), CenterStringFormat);
                }
            };

            _btnEarningsRadar = CreateNeonButton("실적 D-Day", Color.FromArgb(42, 24, 46), Color.FromArgb(210, 110, 255), Color.FromArgb(245, 225, 255));
            _btnEarningsRadar.Font = new Font("Malgun Gothic", 8.5f, FontStyle.Bold);
            _btnEarningsRadar.Size = new Size(80, 34);
            _btnEarningsRadar.Margin = new Padding(2, 1, 2, 1);
            _btnEarningsRadar.Click += (s, e) => ShowEarningsRadarDialog();

            _btnMarketCloseReport = CreateNeonButton("장마감 결산", Color.FromArgb(48, 34, 18), Color.FromArgb(255, 175, 45), Color.FromArgb(255, 235, 180));
            _btnMarketCloseReport.Font = new Font("Malgun Gothic", 8.5f, FontStyle.Bold);
            _btnMarketCloseReport.Size = new Size(82, 34);
            _btnMarketCloseReport.Margin = new Padding(2, 1, 2, 1);
            _btnMarketCloseReport.Click += (s, e) => ShowMarketCloseReportDialog();

            // 부가 도구 드롭다운 메뉴 (공간 절약 및 사용자 맞춤 메뉴화 - 불필요한 이모지 및 중복 메뉴 제거)
            var cmsMoreTools = new ContextMenuStrip();
            var mnuKeyStock = new ToolStripMenuItem("중요 종목 요약 브리핑");
            mnuKeyStock.Click += (s, e) => ShowKeyStockSummaryDialog();
            var mnuPanicExit = new ToolStripMenuItem("비상 탈출 (패닉셀 방어) 시뮬레이터");
            mnuPanicExit.Click += (s, e) => ShowPanicExitDialog();
            var mnuImport = new ToolStripMenuItem("증권사 계좌 잔고 가져오기 (엑셀/CSV)");
            mnuImport.Click += (s, e) => ShowImportPortfolioDialog();
            var mnuKbSec = new ToolStripMenuItem("KB증권 Open API 실시간 잔고 연동 설정");
            mnuKbSec.Click += (s, e) => ShowKbSettingDialog();
            var mnuHelpScore = new ToolStripMenuItem("AI 종목 종합 평가 점수 산출 기준 안내");
            mnuHelpScore.Click += (s, e) => ShowScoreHelpDialog();

            cmsMoreTools.Items.Add(mnuKeyStock);
            cmsMoreTools.Items.Add(mnuPanicExit);
            cmsMoreTools.Items.Add(new ToolStripSeparator());
            cmsMoreTools.Items.Add(mnuImport);
            cmsMoreTools.Items.Add(mnuKbSec);
            cmsMoreTools.Items.Add(new ToolStripSeparator());
            cmsMoreTools.Items.Add(mnuHelpScore);

            var btnMoreTools = CreateNeonButton("부가 도구 ▾", Color.FromArgb(34, 28, 48), Color.FromArgb(180, 130, 250), Color.FromArgb(235, 220, 255));
            btnMoreTools.Font = new Font("Malgun Gothic", 8.8f, FontStyle.Bold);
            btnMoreTools.Padding = new Padding(0, 3, 0, 0); // ▾ 특수문자로 인한 텍스트 상단 치우침 보정
            btnMoreTools.Size = new Size(86, 34);
            btnMoreTools.Margin = new Padding(2, 1, 2, 1);
            btnMoreTools.Click += (s, e) =>
            {
                cmsMoreTools.Show(btnMoreTools, new Point(0, btnMoreTools.Height));
            };

            flpButtons.Controls.Add(_btnDiagnose);
            flpButtons.Controls.Add(_btnLeaderRadar);
            flpButtons.Controls.Add(_btnEvaluateStock);
            flpButtons.Controls.Add(_btnRefreshPrice);
            flpButtons.Controls.Add(_btnEarningsRadar);
            flpButtons.Controls.Add(_btnMarketCloseReport);
            flpButtons.Controls.Add(btnMoreTools);

            pnlHeader.Controls.Add(flpButtons);
            pnlHeader.Controls.Add(pnlLeft);

            pnlHeader.Paint += (s, e) =>
            {
                e.Graphics.DrawLine(HeaderBorderPen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
            };

            _pnlHeader = pnlHeader;

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
                Text = "동기화: 대기 중",
                Font = new Font("Malgun Gothic", 8.8f),
                ForeColor = Color.FromArgb(130, 160, 195),
                Dock = DockStyle.Right,
                TextAlign = ContentAlignment.MiddleRight,
                AutoSize = true
            };

            // 도킹 순서: Right 도킹 먼저 추가 후 Left 추가
            _pnlFooter.Controls.Add(_lblLastUpdated);
            _pnlFooter.Controls.Add(_lblAccountSummary);

            // 3. 중앙 분할 컨테이너 (좌측 리스트뷰 최적폭 880px, 우측 상세 카드 380px 쾌적 확보)
            _splitMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 880,
                BackColor = Color.FromArgb(30, 34, 46)
            };

            // 좌측: 종목 리스트뷰 (15대 컬럼 구성: 종목명에 증권앱 주문 체크박스 탑재, 불필요한 코드 컬럼 삭제)
            _lvStocks = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                CheckBoxes = true,
                GridLines = true,
                BackColor = Color.FromArgb(18, 20, 26),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.2f),
                BorderStyle = BorderStyle.None,
                ShowItemToolTips = true
            };
            _lvStocks.Columns.Add("종목명 (앱주문체크)", 140);
            _lvStocks.Columns.Add("현재가", 75, HorizontalAlignment.Right);
            _lvStocks.Columns.Add("전일대비", 68, HorizontalAlignment.Right);
            _lvStocks.Columns.Add("평단", 75, HorizontalAlignment.Right);
            _lvStocks.Columns.Add("보유", 50, HorizontalAlignment.Right);
            _lvStocks.Columns.Add("손익금(원)", 85, HorizontalAlignment.Right);
            _lvStocks.Columns.Add("수익률", 68, HorizontalAlignment.Right);
            _lvStocks.Columns.Add("AI 등급", 55, HorizontalAlignment.Center);
            _lvStocks.Columns.Add("수급(외인·기관)", 108, HorizontalAlignment.Center);
            _lvStocks.Columns.Add("지뢰검증", 88, HorizontalAlignment.Center);
            _lvStocks.Columns.Add("14일 RSI", 62, HorizontalAlignment.Right);
            _lvStocks.Columns.Add("5일 거래량", 80, HorizontalAlignment.Right);
            _lvStocks.Columns.Add("종합점수", 75, HorizontalAlignment.Center);
            _lvStocks.Columns.Add("타이밍 안전벨트", 115, HorizontalAlignment.Center);
            _lvStocks.Columns.Add("최종 액션", 140, HorizontalAlignment.Center);

            // 표시 순서 최적화:
            // 1. 가장 중요한 [최종 액션](데이터 인덱스 14)을 [AI 등급](표시 7) 바로 뒤인 [표시 위치 8]로 전진 배치! (0.1초 직관성 확보)
            // 2. 보조지표인 [타이밍 안전벨트](데이터 인덱스 13)는 종합점수 뒤인 [표시 위치 14]로 이동
            _lvStocks.Columns[14].DisplayIndex = 8;
            _lvStocks.Columns[13].DisplayIndex = 14;

            _lvStocks.Resize += (s, e) => AutoFitLastColumn();
            _lvStocks.SelectedIndexChanged += LvStocks_SelectedIndexChanged;
            _lvStocks.ColumnClick += LvStocks_ColumnClick;
            _lvStocks.DoubleClick += (s, e) =>
            {
                if (_lvStocks.SelectedItems.Count > 0 && _lvStocks.SelectedItems[0].Tag is StockHoldingItem sel)
                {
                    ShowStockEvaluatorDialog(sel.StockCode);
                }
            };
            _lvStocks.MouseDown += (s, e) =>
            {
                if (e.Clicks > 1) _isDoubleClick = true;
            };
            _lvStocks.ItemCheck += (s, e) =>
            {
                if (_isDoubleClick)
                {
                    e.NewValue = e.CurrentValue; // 더블클릭 시 체크박스 토글 오동작 방지
                    _isDoubleClick = false;
                }
            };
            _lvStocks.ItemChecked += (s, e) =>
            {
                if (_isUpdatingUi) return;
                if (e.Item?.Tag is StockHoldingItem itm)
                {
                    itm.IsBrokerOrderSet = e.Item.Checked;
                    SavePortfolio();
                    if (_lblFilterSummary != null)
                    {
                        int hCount = _portfolio.Items.Count(x => !x.IsWatchlist);
                        int wCount = _portfolio.Items.Count(x => x.IsWatchlist);
                        int oCount = _portfolio.Items.Count(x => x.IsBrokerOrderSet);
                        _lblFilterSummary.Text = $"보유 {hCount}종목 | 관심 {wCount}종목 | 앱 주문설정 {oCount}건";
                    }
                    if (_lvStocks.SelectedItems.Count > 0 && _lvStocks.SelectedItems[0] == e.Item)
                    {
                        UpdateDetailPanel(itm);
                    }
                }
            };
            _lvStocks.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Insert)
                {
                    ShowAddStockDialog();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Delete)
                {
                    DeleteSelectedStock();
                    e.Handled = true;
                }
            };

            // 우클릭 컨텍스트 메뉴 설정
            var ctx = new ContextMenuStrip();
            var mnuToggleOrder = new ToolStripMenuItem("증권앱 주문 설정 (체크박스 토글)");
            mnuToggleOrder.Click += (s, e) =>
            {
                if (_lvStocks.SelectedItems.Count > 0)
                {
                    _lvStocks.SelectedItems[0].Checked = !_lvStocks.SelectedItems[0].Checked;
                }
            };

            var mnuAdd = new ToolStripMenuItem("새 종목 추가 (Insert)");
            mnuAdd.Click += (s, e) => ShowAddStockDialog();
            var mnuEdit = new ToolStripMenuItem("종목 정보 수정 ([수정])");
            mnuEdit.Click += (s, e) => EditSelectedStock();
            var mnuDelete = new ToolStripMenuItem("종목 삭제 ([삭제])");
            mnuDelete.Click += (s, e) => DeleteSelectedStock();
            var mnuNaver = new ToolStripMenuItem("네이버 증권 웹페이지 열기");
            mnuNaver.Click += (s, e) => OpenNaverFinanceWeb();

            ctx.Items.Add(mnuToggleOrder);
            ctx.Items.Add(new ToolStripSeparator());
            ctx.Items.Add(mnuAdd);
            ctx.Items.Add(new ToolStripSeparator());
            ctx.Items.Add(mnuEdit);
            ctx.Items.Add(mnuDelete);
            ctx.Items.Add(new ToolStripSeparator());
            ctx.Items.Add(mnuNaver);

            ctx.Opening += (s, e) =>
            {
                Point pt = _lvStocks.PointToClient(Cursor.Position);
                var hit = _lvStocks.HitTest(pt);
                if (hit.Item != null && !hit.Item.Selected)
                {
                    _lvStocks.SelectedItems.Clear();
                    hit.Item.Selected = true;
                }
                else if (hit.Item == null)
                {
                    _lvStocks.SelectedItems.Clear();
                }

                bool hasSelection = _lvStocks.SelectedItems.Count > 0;
                mnuToggleOrder.Enabled = hasSelection;
                if (hasSelection && _lvStocks.SelectedItems[0].Tag is StockHoldingItem selItm)
                {
                    mnuToggleOrder.Text = selItm.IsBrokerOrderSet
                        ? "✓ 증권앱 주문 설정됨 (체크 해제)"
                        : (selItm.IsWatchlist ? "증권앱 예약매수 걸림으로 체크" : "증권앱 자동청산 걸림으로 체크");
                }
                else
                {
                    mnuToggleOrder.Text = "증권앱 주문 설정 (체크박스 토글)";
                }

                mnuEdit.Enabled = hasSelection;
                mnuDelete.Enabled = hasSelection;
                mnuNaver.Enabled = hasSelection;
            };

            _lvStocks.ContextMenuStrip = ctx;

            // 리스트 더블 버퍼링 적용 (깜빡임 0)
            typeof(ListView).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
                null, _lvStocks, new object[] { true });

            // 좌측 상단: 관심/보유 필터 바 (34px)
            _pnlListFilter = new Panel
            {
                Dock = DockStyle.Top,
                Height = 34,
                BackColor = Color.FromArgb(16, 20, 28),
                Padding = new Padding(6, 4, 8, 4)
            };

            var flpFilter = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };

            _btnFilterAll = CreateFilterTabButton("전체 (0)", StockFilterMode.All);
            _btnFilterHolding = CreateFilterTabButton("보유 (0)", StockFilterMode.HoldingOnly);
            _btnFilterWatchlist = CreateFilterTabButton("★ 관심 (0)", StockFilterMode.WatchlistOnly);

            flpFilter.Controls.Add(_btnFilterAll);
            flpFilter.Controls.Add(_btnFilterHolding);
            flpFilter.Controls.Add(_btnFilterWatchlist);

            _lblFilterSummary = new Label
            {
                Text = "보유 0종목 | 관심 0종목",
                Font = new Font("Malgun Gothic", 8.8f),
                ForeColor = Color.FromArgb(130, 160, 195),
                Dock = DockStyle.Right,
                TextAlign = ContentAlignment.MiddleRight,
                AutoSize = true
            };

            _pnlListFilter.Controls.Add(_lblFilterSummary);
            _pnlListFilter.Controls.Add(flpFilter);

            _splitMain.Panel1.Controls.Add(_lvStocks);
            _splitMain.Panel1.Controls.Add(_pnlListFilter);

            // 우측: 선택 종목 AI 상세 진단 카드 (440px 와이드 레이아웃)
            _pnlDetail = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(24, 28, 38),
                Padding = new Padding(12)
            };

            // 1. 종목명 및 현재가 타이틀
            _lblDetailTitle = new Label
            {
                Text = "선택된 종목이 없습니다",
                Font = new Font("Malgun Gothic", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(140, 200, 255),
                Dock = DockStyle.Top,
                Height = 28,
                AutoEllipsis = true
            };

            // 2. AI 참모 최종 결론 바 (0.1초 직관 배지 + 1줄 행동 지침)
            _lblFinalVerdict = new Label
            {
                Text = "[ AI 참모 최종 결론 : 좌측 목록에서 종목을 선택하세요 ]\n종목을 선택하시면 단일화된 최종 행동 결론이 표시됩니다.",
                Font = new Font("Malgun Gothic", 9.2f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 220, 90),
                BackColor = Color.FromArgb(36, 32, 20),
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Top,
                Height = 48,
                Padding = new Padding(8, 4, 8, 4),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // 3. 핵심 가격 타점 바 (목표가 / 손절가 / 인터락 매수가 단일 표시)
            _lblDetailPriceTargets = new Label
            {
                Text = "• 목표가: - 원   |   손절가: - 원\n• 매수가: 1차 - 원   |   2차(추매): - 원",
                Font = new Font("Malgun Gothic", 9.2f, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 240, 255),
                BackColor = Color.FromArgb(18, 26, 38),
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Top,
                Height = 44,
                Padding = new Padding(8, 4, 8, 4),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // 4. 일봉 캔들 & AI 타점 미니 차트 (60일/20일 기간 토글 인라인 뷰어)
            _miniChart = new MiniCandleChartControl
            {
                Dock = DockStyle.Top,
                Height = 168
            };

            // 5. 진단 스펙 & 수급 지뢰 요약 바 (권고 액션 중복 제거 후 2줄 압축)
            _lblDetailSummary = new Label
            {
                Text = "• AI 등급: -   |   종합 점수: -점\n• 수급: -   |   지뢰: -",
                Font = new Font("Malgun Gothic", 8.8f),
                ForeColor = Color.FromArgb(190, 210, 235),
                Dock = DockStyle.Top,
                Height = 44,
                Padding = new Padding(2, 4, 2, 2)
            };

            var lblReasonTitle = new Label
            {
                Text = "[AI 참모 심층 진단 사유 및 대응 논리]",
                Font = new Font("Malgun Gothic", 9.2f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 215, 255),
                Dock = DockStyle.Top,
                Height = 26,
                Padding = new Padding(0, 4, 0, 0)
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

            var pnlDetailBtns = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 42,
                Padding = new Padding(0, 4, 0, 0)
            };

            var pnlLeftBtns = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0)
            };

            _btnOption = CreateNeonButton("Option", Color.FromArgb(20, 32, 46), Color.FromArgb(0, 216, 255), Color.FromArgb(200, 240, 255), 84);
            _btnOption.Font = new Font("Malgun Gothic", 9f, FontStyle.Bold);
            _btnOption.Margin = new Padding(0, 2, 6, 2);
            _btnOption.Cursor = Cursors.Hand;
            _btnOption.Click += (s, e) => ShowOptionDialog();

            var ttOpt = new ToolTip();
            ttOpt.SetToolTip(_btnOption, "시세·AI 진단 자동 동기화 주기 설정 및 점수 산출 가이드 열기");

            _btnAlertHistory = CreateNeonButton("알림 기록", Color.FromArgb(20, 28, 40), Color.FromArgb(60, 85, 120), Color.FromArgb(170, 195, 230), 100);
            _btnAlertHistory.Font = new Font("Malgun Gothic", 9f, FontStyle.Bold);
            _btnAlertHistory.Margin = new Padding(0, 2, 6, 2); // Option 및 우측 버튼과 동일한 Top/Bottom 마진 2px로 수평 완벽 정렬
            _btnAlertHistory.Padding = new Padding(0, 1, 0, 0); // 텍스트 상하 수직 정렬 최적화
            _btnAlertHistory.Cursor = Cursors.Hand;
            _btnAlertHistory.Click += (s, e) => ShowAlertHistoryDialog();

            var ttHist = new ToolTip();
            ttHist.SetToolTip(_btnAlertHistory, "실시간 가격 도달(목표가/손절가/적정매수가) 토스트 알림 누적 기록 열기");

            pnlLeftBtns.Controls.Add(_btnOption);
            pnlLeftBtns.Controls.Add(_btnAlertHistory);

            var pnlRightBtns = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Margin = new Padding(0)
            };

            _btnDeleteItem = CreateNeonButton("[삭제]", Color.FromArgb(48, 18, 24), Color.FromArgb(255, 65, 85), Color.FromArgb(255, 220, 225), 80);
            _btnDeleteItem.Click += (s, e) => DeleteSelectedStock();

            _btnEditItem = CreateNeonButton("[수정]", Color.FromArgb(18, 36, 56), Color.FromArgb(64, 150, 255), Color.FromArgb(215, 235, 255), 80);
            _btnEditItem.Click += (s, e) => EditSelectedStock();

            pnlRightBtns.Controls.Add(_btnDeleteItem);
            pnlRightBtns.Controls.Add(_btnEditItem);

            pnlDetailBtns.Controls.Add(pnlLeftBtns);
            pnlDetailBtns.Controls.Add(pnlRightBtns);

            // 도킹 역순: 마지막에 맨 위 컨트롤이 오도록 추가 (Fill -> Top 5 -> Top 4 -> Top 3 -> Top 2 -> Top 1 -> Bottom)
            _pnlDetail.Controls.Add(_rtbDetailReason);
            _pnlDetail.Controls.Add(lblReasonTitle);
            _pnlDetail.Controls.Add(_lblDetailSummary);
            _pnlDetail.Controls.Add(_miniChart);
            _pnlDetail.Controls.Add(_lblDetailPriceTargets);
            _pnlDetail.Controls.Add(_lblFinalVerdict);
            _pnlDetail.Controls.Add(_lblDetailTitle);
            _pnlDetail.Controls.Add(pnlDetailBtns);

            _splitMain.Panel2.Controls.Add(_pnlDetail);

            // 메인 컨트롤 배치 (도킹 역순)
            this.Controls.Add(_splitMain);
            this.Controls.Add(_pnlFooter);
            this.Controls.Add(_pnlHeader);

            this.ResumeLayout(false);
        }

        private void AutoFitLastColumn()
        {
            if (_lvStocks == null || _lvStocks.Columns.Count < 15) return;
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

        private Button CreateFilterTabButton(string text, StockFilterMode mode)
        {
            var btn = new Button
            {
                Text = text,
                Tag = mode,
                Font = new Font("Malgun Gothic", 8.5f, FontStyle.Bold),
                Size = new Size(95, 26),
                FlatStyle = FlatStyle.Flat,
                BackColor = (mode == _filterMode) ? Color.FromArgb(18, 52, 74) : Color.FromArgb(24, 28, 38),
                ForeColor = (mode == _filterMode) ? Color.FromArgb(0, 216, 255) : Color.FromArgb(140, 160, 185),
                Margin = new Padding(2, 0, 3, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = (mode == _filterMode) ? Color.FromArgb(0, 216, 255) : Color.FromArgb(42, 54, 72);
            btn.Click += (s, e) =>
            {
                SetFilterMode(mode);
            };
            return btn;
        }

        #endregion
    }
}

