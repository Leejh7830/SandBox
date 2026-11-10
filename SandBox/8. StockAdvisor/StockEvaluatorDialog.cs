using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using GateHelper.StockAdvisor.Models;
using GateHelper.StockAdvisor.Services;

namespace GateHelper.StockAdvisor
{
    /// <summary>
    /// 보유 여부와 무관하게 모든 종목을 즉시 검색하여
    /// 팩트 시세 + 14일 RSI + 외인/기관 수급 + 재무·공시 지뢰 + 정통 퀀트 8대 우량주 필터를 1초 만에 원스톱 진단하는 다이얼로그
    /// </summary>
    public class StockEvaluatorDialog : Form
    {
        // 결과 반환 (내 포트폴리오에 추가할 경우)
        public StockHoldingItem AddedStockItem { get; private set; }

        private TextBox _txtSearch;
        private Button _btnSearch;
        private Label _lblStatus;

        // 좌측: 기술적 AI 종합 점수 카드
        private Panel _pnlCard1;
        private Label _lblStockHeader;
        private Label _lblPriceAndRate;
        private Label _lblOverallScore;
        private Label _lblGradeBadge;
        private Label _lblThreeScores;
        private Label _lblTechnicalFacts;
        private Label _lblTimingGuide;

        // 중앙: 외인·기관 수급 & 재무·공시 지뢰 카드
        private Panel _pnlCard2;
        private Label _lblSupplyBadge;
        private Label _lblSupplyDetails;
        private Label _lblLandmineBadge;
        private Label _lblLandmineDetails;
        private Label _lblDisclosures;

        // 우측: 정통 퀀트 8대 황금 필터 체크리스트 카드
        private Panel _pnlCard3;
        private Label _lblQuantHeader;
        private ListView _lvQuantFilters;

        // 하단 버튼
        private Button _btnAddToPortfolio;
        private Button _btnOpenNaverWeb;
        private Button _btnClose;

        // 현재 진단 완료된 임시 데이터
        private StockHoldingItem _currentEvaluatedItem;

        public StockEvaluatorDialog(string initialQuery = "")
        {
            InitializeCustomComponent();
            if (!string.IsNullOrWhiteSpace(initialQuery))
            {
                _txtSearch.Text = initialQuery.Trim();
                this.Shown += async (s, e) => await ExecuteDiagnosisAsync(_txtSearch.Text);
            }
        }

        private void InitializeCustomComponent()
        {
            this.Text = "종목 AI 즉시 진단 & 정통 퀀트 8대 우량주 스캐너";
            this.Size = new Size(1120, 740);
            this.MinimumSize = new Size(1000, 660);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(18, 22, 32);
            this.ForeColor = Color.White;
            this.Font = new Font("Malgun Gothic", 9f);
            this.ShowIcon = false;

            // ================= 1. 상단 검색 바 영역 =================
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 126,
                BackColor = Color.FromArgb(24, 30, 44),
                Padding = new Padding(16, 10, 16, 8)
            };

            var lblTitle = new Label
            {
                Text = "🔍 종목 AI 즉시 진단 & 정통 퀀트 8대 우량주 스캐너",
                Font = new Font("Malgun Gothic", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 230, 255),
                Dock = DockStyle.Top,
                Height = 24
            };

            var lblSubtitle = new Label
            {
                Text = "보유 종목 외에도 관심 있는 어떤 종목이든 검색만 하면 실시간 팩트 시세, 수급, 지뢰, 8대 황금 필터를 1초 만에 종합 진단합니다.",
                Font = new Font("Malgun Gothic", 8.8f),
                ForeColor = Color.FromArgb(170, 190, 220),
                Dock = DockStyle.Top,
                Height = 18
            };

            var pnlSearchInput = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 2, 0, 0)
            };

            _txtSearch = new TextBox
            {
                Font = new Font("Malgun Gothic", 11f, FontStyle.Bold),
                BackColor = Color.FromArgb(14, 18, 26),
                ForeColor = Color.FromArgb(240, 245, 255),
                BorderStyle = BorderStyle.FixedSingle,
                Width = 280,
                Height = 30,
                Location = new Point(2, 4)
            };
            _txtSearch.KeyDown += async (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    await ExecuteDiagnosisAsync(_txtSearch.Text);
                }
            };

            _btnSearch = CreateNeonButton("🔍 AI 즉시 진단", Color.FromArgb(14, 46, 68), Color.FromArgb(0, 216, 255), Color.FromArgb(210, 248, 255));
            _btnSearch.Location = new Point(288, 3);
            _btnSearch.Size = new Size(130, 31);
            _btnSearch.Font = new Font("Malgun Gothic", 9.5f, FontStyle.Bold);
            _btnSearch.Click += async (s, e) => await ExecuteDiagnosisAsync(_txtSearch.Text);

            // 빠른 추천 태그
            int tagX = 428;
            string[] quickTags = { "기아", "SK하이닉스", "현대차", "삼성전자", "NAVER" };
            foreach (var tag in quickTags)
            {
                var btnTag = CreateTagButton(tag);
                btnTag.Location = new Point(tagX, 4);
                btnTag.Click += async (s, e) =>
                {
                    _txtSearch.Text = tag;
                    await ExecuteDiagnosisAsync(tag);
                };
                pnlSearchInput.Controls.Add(btnTag);
                tagX += btnTag.Width + 6;
            }

            _lblStatus = new Label
            {
                Text = "💡 종목명(예: 기아) 또는 6자리 코드(000270)를 입력하고 [AI 즉시 진단]을 누르세요.",
                Font = new Font("Malgun Gothic", 8.8f),
                ForeColor = Color.FromArgb(140, 215, 255),
                Location = new Point(2, 40),
                AutoSize = true
            };

            pnlSearchInput.Controls.Add(_txtSearch);
            pnlSearchInput.Controls.Add(_btnSearch);
            pnlSearchInput.Controls.Add(_lblStatus);

            pnlTop.Controls.Add(pnlSearchInput);
            pnlTop.Controls.Add(lblSubtitle);
            pnlTop.Controls.Add(lblTitle);

            // ================= 2. 중앙 3대 카드 레이아웃 =================
            var tblCards = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(12),
                BackColor = Color.FromArgb(18, 22, 32)
            };
            tblCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32f));
            tblCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34f));
            tblCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34f));

            _pnlCard1 = CreateCardPanel("【 1. 기술적 AI 종합 점수 】");
            _pnlCard2 = CreateCardPanel("【 2. 메이저 수급 & 재무·지뢰 검증 】");
            _pnlCard3 = CreateCardPanel("【 3. 정통 퀀트 8대 황금 필터 】");

            BuildCard1Content(_pnlCard1);
            BuildCard2Content(_pnlCard2);
            BuildCard3Content(_pnlCard3);

            tblCards.Controls.Add(_pnlCard1, 0, 0);
            tblCards.Controls.Add(_pnlCard2, 1, 0);
            tblCards.Controls.Add(_pnlCard3, 2, 0);

            // ================= 3. 하단 액션 버튼 영역 =================
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                BackColor = Color.FromArgb(22, 26, 36),
                Padding = new Padding(16, 10, 16, 10)
            };

            _btnAddToPortfolio = CreateNeonButton("+ 내 포트폴리오에 담기", Color.FromArgb(16, 48, 36), Color.FromArgb(0, 230, 118), Color.FromArgb(215, 255, 230));
            _btnAddToPortfolio.Font = new Font("Malgun Gothic", 9.5f, FontStyle.Bold);
            _btnAddToPortfolio.Dock = DockStyle.Left;
            _btnAddToPortfolio.Width = 200;
            _btnAddToPortfolio.Enabled = false;
            _btnAddToPortfolio.Click += (s, e) => AddCurrentStockToPortfolio();

            _btnOpenNaverWeb = CreateNeonButton("🌐 네이버 증권 열기", Color.FromArgb(24, 34, 52), Color.FromArgb(64, 150, 255), Color.FromArgb(215, 235, 255));
            _btnOpenNaverWeb.Font = new Font("Malgun Gothic", 9f);
            _btnOpenNaverWeb.Dock = DockStyle.Left;
            _btnOpenNaverWeb.Width = 160;
            _btnOpenNaverWeb.Margin = new Padding(10, 0, 0, 0);
            _btnOpenNaverWeb.Enabled = false;
            _btnOpenNaverWeb.Click += (s, e) => OpenNaverWeb();

            _btnClose = CreateNeonButton("닫기", Color.FromArgb(32, 36, 46), Color.FromArgb(180, 190, 210), Color.White);
            _btnClose.Font = new Font("Malgun Gothic", 9f);
            _btnClose.Dock = DockStyle.Right;
            _btnClose.Width = 100;
            _btnClose.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            pnlBottom.Controls.Add(_btnClose);
            pnlBottom.Controls.Add(_btnOpenNaverWeb);
            pnlBottom.Controls.Add(_btnAddToPortfolio);

            this.Controls.Add(tblCards);
            this.Controls.Add(pnlBottom);
            this.Controls.Add(pnlTop);
        }

        private Panel CreateCardPanel(string headerText)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(24, 28, 40),
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(6),
                Padding = new Padding(12)
            };

            var lblHeader = new Label
            {
                Text = headerText,
                Font = new Font("Malgun Gothic", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 216, 255),
                Dock = DockStyle.Top,
                Height = 28
            };
            pnl.Controls.Add(lblHeader);
            return pnl;
        }

        private void BuildCard1Content(Panel card)
        {
            _lblStockHeader = new Label
            {
                Text = "종목을 검색해주세요",
                Font = new Font("Malgun Gothic", 11.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 30
            };

            _lblPriceAndRate = new Label
            {
                Text = "현재가: - 원 (-%)",
                Font = new Font("Malgun Gothic", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 215, 235),
                Dock = DockStyle.Top,
                Height = 28
            };

            _lblOverallScore = new Label
            {
                Text = "★ 종합 점수: -점 / 10점",
                Font = new Font("Malgun Gothic", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 215, 64),
                Dock = DockStyle.Top,
                Height = 32
            };

            _lblGradeBadge = new Label
            {
                Text = "AI 등급: -",
                Font = new Font("Malgun Gothic", 10f, FontStyle.Bold),
                ForeColor = Color.Silver,
                Dock = DockStyle.Top,
                Height = 26
            };

            _lblThreeScores = new Label
            {
                Text = "• 차트 점수: -점 | 뉴스: -점 | 손익비: -점",
                Font = new Font("Malgun Gothic", 9f),
                ForeColor = Color.FromArgb(190, 210, 235),
                Dock = DockStyle.Top,
                Height = 26
            };

            _lblTechnicalFacts = new Label
            {
                Text = "• 14일 RSI: - | 5일 거래량: -\n• 20일 지지선: -원 (하방 -%)\n• 🎯 1차 매수가: -원 (방어선: -원)\n• 🚀 1차 목표가: -원 (2차 슈팅: -원)",
                Font = new Font("Malgun Gothic", 8.8f),
                ForeColor = Color.FromArgb(170, 205, 245),
                Dock = DockStyle.Top,
                Height = 84
            };

            _lblTimingGuide = new Label
            {
                Text = "🚨 [타이밍 지침]\n진단 대기 중",
                Font = new Font("Malgun Gothic", 9.2f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 220, 100),
                BackColor = Color.FromArgb(36, 32, 24),
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill,
                Padding = new Padding(8),
                TextAlign = ContentAlignment.MiddleLeft
            };

            card.Controls.Add(_lblTimingGuide);
            card.Controls.Add(_lblTechnicalFacts);
            card.Controls.Add(_lblThreeScores);
            card.Controls.Add(_lblGradeBadge);
            card.Controls.Add(_lblOverallScore);
            card.Controls.Add(_lblPriceAndRate);
            card.Controls.Add(_lblStockHeader);
        }

        private void BuildCard2Content(Panel card)
        {
            _lblSupplyBadge = new Label
            {
                Text = "수급 주체: 대기 중",
                Font = new Font("Malgun Gothic", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 240, 210),
                Dock = DockStyle.Top,
                Height = 28
            };

            _lblSupplyDetails = new Label
            {
                Text = "• 최근 3일 외인: -주 | 기관: -주 | 개인: -주",
                Font = new Font("Malgun Gothic", 8.8f),
                ForeColor = Color.FromArgb(180, 220, 235),
                Dock = DockStyle.Top,
                Height = 44
            };

            _lblLandmineBadge = new Label
            {
                Text = "재무·공시 지뢰: 검증 대기 중",
                Font = new Font("Malgun Gothic", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 240, 140),
                Dock = DockStyle.Top,
                Height = 28
            };

            _lblLandmineDetails = new Label
            {
                Text = "• 부채비율: - | 3년 연속적자: -\n• DART 지뢰 공시 스캔 결과 대기",
                Font = new Font("Malgun Gothic", 8.8f),
                ForeColor = Color.FromArgb(200, 215, 230),
                Dock = DockStyle.Top,
                Height = 44
            };

            var lblDiscTitle = new Label
            {
                Text = "최근 DART 공시 모니터링:",
                Font = new Font("Malgun Gothic", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(170, 200, 235),
                Dock = DockStyle.Top,
                Height = 22
            };

            _lblDisclosures = new Label
            {
                Text = "• 공시 데이터 로드 대기 중",
                Font = new Font("Malgun Gothic", 8.5f),
                ForeColor = Color.FromArgb(160, 180, 210),
                BackColor = Color.FromArgb(16, 20, 28),
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill,
                Padding = new Padding(8)
            };

            card.Controls.Add(_lblDisclosures);
            card.Controls.Add(lblDiscTitle);
            card.Controls.Add(_lblLandmineDetails);
            card.Controls.Add(_lblLandmineBadge);
            card.Controls.Add(_lblSupplyDetails);
            card.Controls.Add(_lblSupplyBadge);
        }

        private void BuildCard3Content(Panel card)
        {
            _lblQuantHeader = new Label
            {
                Text = "통과율: - / 8 PASS (-)  |  -",
                Font = new Font("Malgun Gothic", 9.8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 215, 64),
                Dock = DockStyle.Top,
                Height = 36
            };

            _lvQuantFilters = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BackColor = Color.FromArgb(14, 18, 26),
                ForeColor = Color.White,
                Font = new Font("Malgun Gothic", 8.8f),
                BorderStyle = BorderStyle.None,
                HeaderStyle = ColumnHeaderStyle.Nonclickable
            };
            _lvQuantFilters.Columns.Add("검증 항목", 96, HorizontalAlignment.Left);
            _lvQuantFilters.Columns.Add("합격 기준", 94, HorizontalAlignment.Left);
            _lvQuantFilters.Columns.Add("현재값", 78, HorizontalAlignment.Right);
            _lvQuantFilters.Columns.Add("판정", 68, HorizontalAlignment.Center);

            // 더블 버퍼링
            typeof(ListView).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
                null, _lvQuantFilters, new object[] { true });

            card.Controls.Add(_lvQuantFilters);
            card.Controls.Add(_lblQuantHeader);
        }

        private Button CreateNeonButton(string text, Color back, Color border, Color fore)
        {
            var btn = new Button
            {
                Text = text,
                BackColor = back,
                ForeColor = fore,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = border;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(Math.Min(255, back.R + 25), Math.Min(255, back.G + 25), Math.Min(255, back.B + 30));
            return btn;
        }

        private Button CreateTagButton(string text)
        {
            var btn = new Button
            {
                Text = text,
                AutoSize = true,
                Height = 28,
                BackColor = Color.FromArgb(24, 34, 48),
                ForeColor = Color.FromArgb(170, 215, 255),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Malgun Gothic", 8.5f),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(64, 120, 180);
            btn.FlatAppearance.BorderSize = 1;
            return btn;
        }

        private async Task ExecuteDiagnosisAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return;

            string cleanQuery = query.Trim();
            _lblStatus.Text = $"⏳ [{cleanQuery}] 실시간 시세, 외인·기관 수급, DART 공시, 정통 퀀트 8대 팩트 수집 중...";
            _lblStatus.ForeColor = Color.FromArgb(0, 230, 255);
            _btnSearch.Enabled = false;

            try
            {
                // 1. 네이버 시세 및 종목 정보 조회
                var priceInfo = await NaverStockPriceService.FetchPriceAsync(cleanQuery);
                if (!priceInfo.IsSuccess)
                {
                    _lblStatus.Text = $"❌ 네이버 증권 시세 수집 실패: {priceInfo.ErrorMessage ?? "종목명 또는 6자리 코드를 확인해주세요."}";
                    _lblStatus.ForeColor = Color.FromArgb(255, 90, 90);
                    _btnAddToPortfolio.Enabled = false;
                    _btnOpenNaverWeb.Enabled = false;
                    return;
                }

                string stockCode = priceInfo.StockCode;
                string stockName = priceInfo.StockName;
                decimal currentPrice = priceInfo.CurrentPrice;
                decimal changeRate = priceInfo.ChangeRate;

                // 2. 기술적 지표, 수급·지뢰 인텔, 정통 퀀트 병렬 비동기 수집
                var techTask = TechnicalIndicatorService.AnalyzeIndicatorsAsync(stockCode, currentPrice);
                var intelTask = StockIntelligenceService.AnalyzeAsync(stockCode);

                await Task.WhenAll(techTask, intelTask);

                var tech = techTask.Result;
                var intel = intelTask.Result;

                // 3. 정통 퀀트 8대 필터 분석 (14일 RSI 연동)
                var quant = await StockIntelligenceService.AnalyzeQuantAsync(stockCode, tech.Rsi);

                // 4. 점수 및 등급 산출
                int chartScore = tech.ChartScore;
                int riskScore = tech.RiskScore;

                // 뉴스 점수 가중치 (쌍끌이 호재, 주주환원 호재 반영)
                int newsScore = 5;
                if (intel.SupplyBadge.Contains("쌍끌이매수") || intel.SupplyBadge.Contains("메이저쌍끌이")) newsScore += 2;
                if (intel.LandmineBadge.Contains("주주환원")) newsScore += 2;
                if (intel.HasCriticalLandmine) newsScore = 1;
                newsScore = Math.Max(1, Math.Min(10, newsScore));

                // 종합 점수 = 차트(40%) + 뉴스(30%) + 손익비(30%)
                int finalScore = (int)Math.Round((chartScore * 0.40) + (newsScore * 0.30) + (riskScore * 0.30), MidpointRounding.AwayFromZero);
                if (finalScore < 1) finalScore = 1;
                if (finalScore > 10) finalScore = 10;

                // 치명적 지뢰 감지 시 강제 F등급 강등 룰
                if (intel.HasCriticalLandmine)
                {
                    finalScore = Math.Min(finalScore, 2);
                }

                // AI 등급 계산
                PositionGrade grade = GradeHelper.CalculateGrade(finalScore, tech.Rsi, changeRate);

                // 임시 종목 아이템 구성
                _currentEvaluatedItem = new StockHoldingItem
                {
                    StockCode = stockCode,
                    StockName = stockName,
                    CurrentPrice = currentPrice,
                    ChangeRate = changeRate,
                    BuyPrice = currentPrice,
                    Quantity = 0,
                    ChartScore = chartScore,
                    NewsScore = newsScore,
                    RiskScore = riskScore,
                    FinalScore = finalScore,
                    Rsi = tech.Rsi,
                    VolumeRatio = tech.VolumeRatio,
                    SupportPrice = tech.SupportPrice,
                    ResistancePrice = tech.ResistancePrice,
                    DownsideRiskPercent = tech.DownsideRiskPercent,
                    RecommendedBuyPrice = tech.RecommendedBuyPrice,
                    TargetStopPrice = tech.TargetStopPrice,
                    EntryTimingBadge = tech.EntryTimingBadge,
                    EntryTimingGuide = tech.EntryTimingGuide,
                    RecommendedTargetPrice = tech.RecommendedTargetPrice,
                    SecondaryTargetPrice = tech.SecondaryTargetPrice,
                    ExitTimingBadge = tech.ExitTimingBadge,
                    ExitTimingGuide = tech.ExitTimingGuide,
                    IsPriceScrapeSuccess = priceInfo.IsSuccess,
                    ScrapeErrorMessage = priceInfo.ErrorMessage,
                    LastScrapedTime = priceInfo.ScrapedAt,
                    TimingBadge = tech.TimingBadge,
                    TimingWarning = tech.TimingWarning,
                    MajorSupplyBadge = intel.SupplyBadge,
                    MajorSupplyDetail = intel.SupplyDetail,
                    LandmineBadge = intel.LandmineBadge,
                    LandmineDetail = intel.LandmineDetail,
                    HasCriticalLandmine = intel.HasCriticalLandmine,
                    DebtRatio = intel.DebtRatio,
                    ConsecutiveLossYears = intel.ConsecutiveLossYears,
                    QuantBadge = quant.QuantGradeBadge,
                    QuantSummary = $"정통 퀀트 {quant.PassCount}/{quant.TotalCount} PASS"
                };

                // UI 3개 카드에 렌더링
                RenderResults(priceInfo, tech, intel, quant, finalScore, grade);

                _lblStatus.Text = $"✔ [{stockName} ({stockCode})] 종합 진단 완료 (시세·수급·지뢰·정통 퀀트 전수 검증)";
                _lblStatus.ForeColor = Color.FromArgb(100, 255, 180);
                _btnAddToPortfolio.Enabled = true;
                _btnOpenNaverWeb.Enabled = true;
            }
            catch (Exception ex)
            {
                _lblStatus.Text = $"❌ 진단 중 예외 발생: {ex.Message}";
                _lblStatus.ForeColor = Color.FromArgb(255, 100, 100);
            }
            finally
            {
                _btnSearch.Enabled = true;
            }
        }

        private void RenderResults(StockPriceDto price, TechnicalIndicatorService.IndicatorAnalysisResult tech, SupplyAndLandmineDto intel, QuantAnalysisResult quant, int finalScore, PositionGrade grade)
        {
            // 카드 1 갱신
            _lblStockHeader.Text = $"◆ {price.StockName} ({price.StockCode})";
            string chgPrefix = price.ChangeRate > 0 ? "+" : "";
            _lblPriceAndRate.Text = $"현재가: {price.CurrentPrice:N0}원 ({chgPrefix}{price.ChangeRate:F2}%)";
            _lblPriceAndRate.ForeColor = price.ChangeRate > 0 ? Color.FromArgb(255, 95, 95) : (price.ChangeRate < 0 ? Color.FromArgb(85, 160, 255) : Color.White);

            _lblOverallScore.Text = $"★ 최종 종합 점수 : ★ {finalScore}점 / 10점";
            _lblOverallScore.ForeColor = finalScore >= 8 ? Color.FromArgb(255, 215, 0) : (finalScore <= 2 ? Color.FromArgb(255, 95, 95) : Color.FromArgb(255, 225, 120));

            _lblGradeBadge.Text = $"AI 등급: {GradeHelper.GetGradeDescription(grade)}";
            _lblGradeBadge.ForeColor = GradeHelper.GetGradeColor(grade);

            _lblThreeScores.Text = $"• 차트 점수: {tech.ChartScore}점 | 뉴스: {_currentEvaluatedItem.NewsScore}점 | 손익비: {tech.RiskScore}점";
            
            string buyPriceStr = tech.RecommendedBuyPrice > 0 ? $"{tech.RecommendedBuyPrice:N0}원" : "시장가 분할";
            string stopPriceStr = tech.TargetStopPrice > 0 ? $"{tech.TargetStopPrice:N0}원" : "20일선 이탈";
            decimal upside1 = (price.CurrentPrice > 0 && tech.RecommendedTargetPrice > price.CurrentPrice)
                ? ((tech.RecommendedTargetPrice - price.CurrentPrice) / price.CurrentPrice * 100m) : 0m;

            _lblTechnicalFacts.Text = $"• 14일 RSI: {tech.Rsi:F1} | 5일 거래량: {tech.VolumeRatio:F0}%\n" +
                                       $"• 20일 지지선: {(tech.SupportPrice > 0 ? $"{tech.SupportPrice:N0}원" : "-")} (하방 -{tech.DownsideRiskPercent:F1}%)\n" +
                                       $"• 🎯 1차 매수가: {buyPriceStr} (방어선: {stopPriceStr})\n" +
                                       $"• 🚀 1차 목표가: {tech.RecommendedTargetPrice:N0}원 (+{upside1:F1}%) [2차: {tech.SecondaryTargetPrice:N0}원]";

            string tb = tech.EntryTimingBadge ?? tech.TimingBadge ?? "⚪ 관망유지";
            string tg = !string.IsNullOrEmpty(tech.EntryTimingGuide) ? tech.EntryTimingGuide : tech.TimingWarning;
            string exitGuide = !string.IsNullOrEmpty(tech.ExitTimingGuide) ? tech.ExitTimingGuide : tech.ExitTimingBadge;
            _lblTimingGuide.Text = $"🚨 【 🎯 진입 지침: {tb} 】\n{tg}\n\n【 🚀 청산 지침: {tech.ExitTimingBadge} 】\n{exitGuide}";
            if (tb.Contains("과열"))
            {
                _lblTimingGuide.BackColor = Color.FromArgb(58, 20, 28);
                _lblTimingGuide.ForeColor = Color.FromArgb(255, 120, 130);
            }
            else if (tb.Contains("골든") || tb.Contains("눌림목") || tb.Contains("매수권"))
            {
                _lblTimingGuide.BackColor = Color.FromArgb(16, 46, 32);
                _lblTimingGuide.ForeColor = Color.FromArgb(100, 255, 180);
            }
            else if (tb.Contains("과매도"))
            {
                _lblTimingGuide.BackColor = Color.FromArgb(20, 36, 56);
                _lblTimingGuide.ForeColor = Color.FromArgb(120, 210, 255);
            }
            else
            {
                _lblTimingGuide.BackColor = Color.FromArgb(36, 32, 24);
                _lblTimingGuide.ForeColor = Color.FromArgb(255, 220, 100);
            }

            // 카드 2 갱신
            _lblSupplyBadge.Text = $"수급 주체: {intel.SupplyBadge}";
            _lblSupplyBadge.ForeColor = intel.SupplyBadge.Contains("쌍끌이") ? Color.FromArgb(0, 240, 210) : Color.FromArgb(180, 210, 240);

            _lblSupplyDetails.Text = $"• {intel.SupplyDetail}\n• 최근 순매수 : 외인 {intel.ForeignNetQuantity:+0;-0;0} | 기관 {intel.OrganNetQuantity:+0;-0;0}";

            _lblLandmineBadge.Text = $"지뢰 검증: {intel.LandmineBadge}";
            _lblLandmineBadge.ForeColor = intel.HasCriticalLandmine ? Color.FromArgb(255, 95, 95) : Color.FromArgb(100, 240, 140);

            _lblLandmineDetails.Text = $"• {intel.LandmineDetail}\n• 부채비율 {intel.DebtRatio:F1}% | 연속적자 {intel.ConsecutiveLossYears}개년";

            _lblDisclosures.Text = string.IsNullOrEmpty(intel.RecentDisclosuresSummary)
                ? "• 최근 20건 DART 공시 중 특이 지뢰 사항 없음 (정상)"
                : $"• 최신 공시: {intel.RecentDisclosuresSummary}";

            // 카드 3 갱신 (정통 퀀트 8대 필터 리스트뷰)
            _lblQuantHeader.Text = $"통과율: {quant.PassCount} / {quant.TotalCount} PASS ({quant.PassPercent:F1}%)  |  {quant.QuantGradeBadge}";
            _lblQuantHeader.ForeColor = quant.PassCount >= 7 ? Color.FromArgb(255, 215, 0) : (quant.PassCount >= 4 ? Color.FromArgb(100, 240, 160) : Color.FromArgb(255, 120, 120));

            _lvQuantFilters.Items.Clear();
            foreach (var itm in quant.Items)
            {
                string passMark = itm.IsPassed ? "PASS ✅" : "FAIL ❌";
                var lvi = new ListViewItem(new[]
                {
                    $"{itm.Id}. {itm.Name}",
                    itm.Criteria,
                    itm.ActualValue,
                    passMark
                });
                lvi.UseItemStyleForSubItems = false;
                lvi.SubItems[0].ForeColor = Color.FromArgb(220, 235, 255);
                lvi.SubItems[1].ForeColor = Color.FromArgb(170, 195, 225);
                lvi.SubItems[2].ForeColor = itm.IsPassed ? Color.FromArgb(120, 240, 255) : Color.FromArgb(255, 170, 170);
                lvi.SubItems[3].ForeColor = itm.IsPassed ? Color.FromArgb(100, 255, 160) : Color.FromArgb(255, 95, 95);
                _lvQuantFilters.Items.Add(lvi);
            }
        }

        private void AddCurrentStockToPortfolio()
        {
            if (_currentEvaluatedItem == null) return;

            // 평단가와 수량을 입력받는 간단한 입력 다이얼로그
            using (var prompt = new Form())
            {
                prompt.Width = 360;
                prompt.Height = 220;
                prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
                prompt.Text = $"내 포트폴리오에 추가: {_currentEvaluatedItem.StockName}";
                prompt.StartPosition = FormStartPosition.CenterParent;
                prompt.BackColor = Color.FromArgb(24, 28, 40);
                prompt.ForeColor = Color.White;
                prompt.Font = new Font("Malgun Gothic", 9f);
                prompt.MaximizeBox = false;
                prompt.MinimizeBox = false;

                var lbl1 = new Label { Left = 20, Top = 20, Text = "매수 평단가(원):", AutoSize = true, ForeColor = Color.FromArgb(200, 220, 245) };
                var txtBuyPrice = new TextBox { Left = 150, Top = 16, Width = 160, BackColor = Color.FromArgb(14, 18, 26), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Text = _currentEvaluatedItem.CurrentPrice.ToString("N0") };

                var lbl2 = new Label { Left = 20, Top = 60, Text = "보유 수량(주):", AutoSize = true, ForeColor = Color.FromArgb(200, 220, 245) };
                var txtQty = new TextBox { Left = 150, Top = 56, Width = 160, BackColor = Color.FromArgb(14, 18, 26), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Text = "1" };

                var btnConfirm = CreateNeonButton("포트폴리오에 추가", Color.FromArgb(16, 48, 36), Color.FromArgb(0, 230, 118), Color.FromArgb(215, 255, 230));
                btnConfirm.Left = 80;
                btnConfirm.Top = 110;
                btnConfirm.Width = 180;
                btnConfirm.Height = 36;
                btnConfirm.Font = new Font("Malgun Gothic", 9.5f, FontStyle.Bold);

                btnConfirm.Click += (s, e) =>
                {
                    string pRaw = txtBuyPrice.Text.Replace(",", "").Trim();
                    string qRaw = txtQty.Text.Replace(",", "").Trim();
                    decimal.TryParse(pRaw, out decimal buyPrice);
                    int.TryParse(qRaw, out int qty);

                    if (buyPrice <= 0) buyPrice = _currentEvaluatedItem.CurrentPrice;
                    if (qty <= 0) qty = 1;

                    _currentEvaluatedItem.BuyPrice = buyPrice;
                    _currentEvaluatedItem.Quantity = qty;

                    this.AddedStockItem = _currentEvaluatedItem;
                    prompt.DialogResult = DialogResult.OK;
                    prompt.Close();
                };

                prompt.Controls.Add(lbl1);
                prompt.Controls.Add(txtBuyPrice);
                prompt.Controls.Add(lbl2);
                prompt.Controls.Add(txtQty);
                prompt.Controls.Add(btnConfirm);

                if (prompt.ShowDialog(this) == DialogResult.OK)
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
        }

        private void OpenNaverWeb()
        {
            if (_currentEvaluatedItem == null || string.IsNullOrWhiteSpace(_currentEvaluatedItem.StockCode)) return;
            try
            {
                string url = $"https://m.stock.naver.com/domestic/stock/{_currentEvaluatedItem.StockCode}";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"웹 브라우저를 열 수 없습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
