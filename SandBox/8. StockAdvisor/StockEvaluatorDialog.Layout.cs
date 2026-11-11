using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using GateHelper.StockAdvisor.Models;

namespace GateHelper.StockAdvisor
{
    public partial class StockEvaluatorDialog : Form
    {
        #region UI Layout Construction

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
                Text = "종목 AI 즉시 진단 & 정통 퀀트 8대 우량주 스캐너",
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

            _searchPopup = new StockSearchPopup();
            _searchPopup.StockSelected += async (item) =>
            {
                _txtSearch.Text = item.Name;
                await ExecuteDiagnosisAsync(item.Code);
            };
            this.FormClosing += (s, e) =>
            {
                _searchPopup?.Close();
                _searchPopup?.Dispose();
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
                    await SearchAndDiagnoseAsync(_txtSearch.Text);
                }
                else if (e.KeyCode == Keys.Down && _searchPopup.Visible)
                {
                    _searchPopup.FocusList();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Escape && _searchPopup.Visible)
                {
                    _searchPopup.Close();
                    e.Handled = true;
                }
            };

            _btnSearch = CreateNeonButton("AI 즉시 진단", Color.FromArgb(14, 46, 68), Color.FromArgb(0, 216, 255), Color.FromArgb(210, 248, 255));
            _btnSearch.Location = new Point(288, 3);
            _btnSearch.Size = new Size(130, 31);
            _btnSearch.Font = new Font("Malgun Gothic", 9.5f, FontStyle.Bold);
            _btnSearch.Click += async (s, e) => await SearchAndDiagnoseAsync(_txtSearch.Text);

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
                    await SearchAndDiagnoseAsync(tag);
                };
                pnlSearchInput.Controls.Add(btnTag);
                tagX += btnTag.Width + 6;
            }

            _lblStatus = new Label
            {
                Text = "종목명(예: 기아) 또는 6자리 코드(000270)를 입력하고 [AI 즉시 진단]을 누르세요.",
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

            _btnOpenNaverWeb = CreateNeonButton("네이버 증권 열기", Color.FromArgb(24, 34, 52), Color.FromArgb(64, 150, 255), Color.FromArgb(215, 235, 255));
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
            return pnl;
        }

        private void BuildCard1Content(Panel card)
        {
            card.Controls.Clear();

            var lblCardTitle = new Label
            {
                Text = "【 1. 기술적 AI 종합 점수 】",
                Font = new Font("Malgun Gothic", 10.2f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 216, 255),
                Dock = DockStyle.Top,
                Height = 28
            };

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
                Text = "종합 점수: -점 / 10점",
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
                Text = "• 14일 RSI: - | 5일 거래량: -\n• 20일 지지선: -원 (하방 -%)\n• 1차 매수가: -원 (방어선: -원)\n• 1차 목표가: -원 (2차: -원)",
                Font = new Font("Malgun Gothic", 8.8f),
                ForeColor = Color.FromArgb(170, 205, 245),
                Dock = DockStyle.Top,
                Height = 84
            };

            _lblTimingGuide = new Label
            {
                Text = "[타이밍 지침]\n진단 대기 중",
                Font = new Font("Malgun Gothic", 9.2f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 220, 100),
                BackColor = Color.FromArgb(36, 32, 24),
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                Margin = new Padding(0, 6, 0, 0),
                TextAlign = ContentAlignment.TopLeft
            };

            // 도킹 역순: 맨 위 타이틀(lblCardTitle)을 가장 마지막에 Add
            card.Controls.Add(_lblTimingGuide);
            card.Controls.Add(_lblTechnicalFacts);
            card.Controls.Add(_lblThreeScores);
            card.Controls.Add(_lblGradeBadge);
            card.Controls.Add(_lblOverallScore);
            card.Controls.Add(_lblPriceAndRate);
            card.Controls.Add(_lblStockHeader);
            card.Controls.Add(lblCardTitle);
        }

        private void BuildCard2Content(Panel card)
        {
            card.Controls.Clear();

            var lblCardTitle = new Label
            {
                Text = "【 2. 메이저 수급 & 재무·지뢰 검증 】",
                Font = new Font("Malgun Gothic", 10.2f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 216, 255),
                Dock = DockStyle.Top,
                Height = 28
            };

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
                Height = 24
            };

            _lblDisclosures = new Label
            {
                Text = "• 공시 데이터 로드 대기 중",
                Font = new Font("Malgun Gothic", 8.5f),
                ForeColor = Color.FromArgb(160, 180, 210),
                BackColor = Color.FromArgb(16, 20, 28),
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                Margin = new Padding(0, 6, 0, 0),
                TextAlign = ContentAlignment.TopLeft
            };

            // 도킹 역순: 맨 위 타이틀(lblCardTitle)을 가장 마지막에 Add
            card.Controls.Add(_lblDisclosures);
            card.Controls.Add(lblDiscTitle);
            card.Controls.Add(_lblLandmineDetails);
            card.Controls.Add(_lblLandmineBadge);
            card.Controls.Add(_lblSupplyDetails);
            card.Controls.Add(_lblSupplyBadge);
            card.Controls.Add(lblCardTitle);
        }

        private void BuildCard3Content(Panel card)
        {
            card.Controls.Clear();

            var lblCardTitle = new Label
            {
                Text = "【 3. 정통 퀀트 8대 황금 필터 】",
                Font = new Font("Malgun Gothic", 10.2f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 216, 255),
                Dock = DockStyle.Top,
                Height = 26
            };

            _lblQuantHeader = new Label
            {
                Text = "통과율: - / 8 통과 (-%)",
                Font = new Font("Malgun Gothic", 10.2f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 215, 64),
                Dock = DockStyle.Top,
                Height = 26
            };

            // 독립 커스텀 헤더 테이블 (어떤 윈도우 OS/테마에서도 100% 선명하게 노출)
            var tblHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 26,
                BackColor = Color.FromArgb(30, 38, 56),
                ColumnCount = 4,
                RowCount = 1,
                Margin = new Padding(0, 4, 0, 0),
                Padding = new Padding(0)
            };
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 94f));
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104f));
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72f));
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70f));
            tblHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var lblH1 = new Label { Text = "검증항목", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Malgun Gothic", 8.8f, FontStyle.Bold), ForeColor = Color.FromArgb(0, 230, 255), Padding = new Padding(6, 0, 0, 0) };
            var lblH2 = new Label { Text = "조건값", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Malgun Gothic", 8.8f, FontStyle.Bold), ForeColor = Color.FromArgb(0, 230, 255), Padding = new Padding(4, 0, 0, 0) };
            var lblH3 = new Label { Text = "현재값", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Font = new Font("Malgun Gothic", 8.8f, FontStyle.Bold), ForeColor = Color.FromArgb(0, 230, 255), Padding = new Padding(0, 0, 4, 0) };
            var lblH4 = new Label { Text = "검증결과", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Malgun Gothic", 8.8f, FontStyle.Bold), ForeColor = Color.FromArgb(0, 230, 255) };

            tblHeader.Controls.Add(lblH1, 0, 0);
            tblHeader.Controls.Add(lblH2, 1, 0);
            tblHeader.Controls.Add(lblH3, 2, 0);
            tblHeader.Controls.Add(lblH4, 3, 0);

            tblHeader.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(55, 70, 100)))
                {
                    e.Graphics.DrawLine(pen, 0, tblHeader.Height - 1, tblHeader.Width, tblHeader.Height - 1);
                    e.Graphics.DrawLine(pen, 94, 3, 94, tblHeader.Height - 4);
                    e.Graphics.DrawLine(pen, 94 + 104, 3, 94 + 104, tblHeader.Height - 4);
                    e.Graphics.DrawLine(pen, 94 + 104 + 72, 3, 94 + 104 + 72, tblHeader.Height - 4);
                }
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
                HeaderStyle = ColumnHeaderStyle.None // ListView 자체 OS 헤더는 숨김!
            };
            _lvQuantFilters.Columns.Add("", 94, HorizontalAlignment.Left);
            _lvQuantFilters.Columns.Add("", 104, HorizontalAlignment.Left);
            _lvQuantFilters.Columns.Add("", 72, HorizontalAlignment.Right);
            _lvQuantFilters.Columns.Add("", 70, HorizontalAlignment.Center);

            // 더블 버퍼링
            typeof(ListView).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
                null, _lvQuantFilters, new object[] { true });

            card.Controls.Add(_lvQuantFilters);
            card.Controls.Add(tblHeader);
            card.Controls.Add(_lblQuantHeader);
            card.Controls.Add(lblCardTitle);
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

        #endregion
    }
}

