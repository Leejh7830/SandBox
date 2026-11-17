using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using GateHelper.StockAdvisor.Models;

namespace GateHelper.StockAdvisor
{
    /// <summary>
    /// 계좌 현금(예수금) vs 주식 비중 및 업종·섹터별 쏠림 현황을 듀얼 도넛 차트로 시각화하는 다이얼로그
    /// (Zero-Resource: 정적 캐시 GDI+ 리소스, DoubleBuffered, 0B 디스크 I/O)
    /// </summary>
    public class AccountAllocationChartDialog : Form
    {
        private readonly PortfolioData _portfolio;
        private readonly string _thermoBadgeText;
        private readonly decimal _thermoScore;
        private readonly Action _onSavePortfolio;

        private decimal _cashAmount;
        private decimal _stockEvalAmount;
        private decimal _totalAssetAmount;
        private List<SectorSlice> _sectorSlices = new List<SectorSlice>();
        private int _selectedSectorIndex = -1;

        private DonutChartPanel _pnlCashChart;
        private DonutChartPanel _pnlSectorChart;
        private Label _lblTopSummary;
        private Label _lblCashAdvice;
        private Label _lblSectorAlert;
        private ListView _lvSectors;
        private Label _lblSectorDetail;
        private NumericUpDown _nudDepositCash;

        // 정적 캐시 팔레트 (Zero-Resource)
        private static readonly Color[] SectorPalette = new[]
        {
            Color.FromArgb(0, 216, 255),   // 시안 (1위 섹터)
            Color.FromArgb(255, 190, 40),  // 골드
            Color.FromArgb(0, 230, 118),   // 에메랄드
            Color.FromArgb(165, 130, 255), // 바이올렛
            Color.FromArgb(255, 110, 130), // 코랄
            Color.FromArgb(90, 175, 255),  // 스카이블루
            Color.FromArgb(255, 145, 70),  // 오렌지
            Color.FromArgb(170, 210, 120), // 라임
            Color.FromArgb(160, 175, 195)  // 실버 (기타)
        };

        public AccountAllocationChartDialog(PortfolioData portfolio, string thermoBadgeText = null, decimal thermoScore = 50m, Action onSavePortfolio = null)
        {
            _portfolio = portfolio ?? new PortfolioData();
            _thermoBadgeText = string.IsNullOrWhiteSpace(thermoBadgeText) ? "중립 (50°C)" : thermoBadgeText;
            _thermoScore = thermoScore;
            _onSavePortfolio = onSavePortfolio;

            this.Text = "📊 계좌 현금 비중 & 섹터 쏠림 분석 차트 (Account Allocation)";
            this.Size = new Size(1020, 650);
            this.MinimumSize = new Size(900, 560);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(16, 20, 28);
            this.ForeColor = Color.White;
            this.Font = new Font("Malgun Gothic", 9f);
            this.ShowIcon = false;

            ComputeAllocationData();
            InitializeCustomUi();
            BindDataToUi();
        }

        private void ComputeAllocationData()
        {
            _cashAmount = Math.Max(0m, _portfolio.DepositCash);
            var holdings = _portfolio.Items.Where(x => !x.IsWatchlist && x.Quantity > 0 && x.TotalEvalAmount > 0).ToList();
            _stockEvalAmount = holdings.Sum(x => x.TotalEvalAmount);
            _totalAssetAmount = _cashAmount + _stockEvalAmount;

            var sectorMap = new Dictionary<string, SectorSlice>(StringComparer.OrdinalIgnoreCase);
            foreach (var h in holdings)
            {
                string sector = ClassifySector(h.StockName, h.StockCode);
                if (!sectorMap.TryGetValue(sector, out var slice))
                {
                    slice = new SectorSlice { SectorName = sector };
                    sectorMap[sector] = slice;
                }
                slice.EvalAmount += h.TotalEvalAmount;
                slice.BuyAmount += h.TotalBuyAmount;
                slice.Items.Add(h);
            }

            _sectorSlices = sectorMap.Values
                .OrderByDescending(x => x.EvalAmount)
                .ToList();

            for (int i = 0; i < _sectorSlices.Count; i++)
            {
                _sectorSlices[i].Color = SectorPalette[i % SectorPalette.Length];
                _sectorSlices[i].SharePercent = _stockEvalAmount > 0
                    ? Math.Round(_sectorSlices[i].EvalAmount / _stockEvalAmount * 100m, 1)
                    : 0m;
            }
        }

        private static string ClassifySector(string stockName, string stockCode)
        {
            string n = (stockName ?? string.Empty).ToUpperInvariant();

            if (n.Contains("나스닥") || n.Contains("S&P") || n.Contains("미국배당") || n.Contains("다우존스") ||
                n.Contains("ACE ") || n.Contains("KODEX") || n.Contains("TIGER") || n.Contains("SOL ") || n.Contains("KOACT"))
            {
                if (n.Contains("반도체") || n.Contains("AI") || n.Contains("테크")) return "반도체·AI 테마 ETF";
                return "글로벌·배당 ETF";
            }
            if (n.Contains("삼성전자") || n.Contains("하이닉스") || n.Contains("반도체") || n.Contains("미크론") ||
                n.Contains("이수페타") || n.Contains("원익") || n.Contains("주성") || n.Contains("심텍") || n.Contains("리노"))
                return "반도체·소부장";
            if (n.Contains("현대차") || n.Contains("기아") || n.Contains("모비스") || n.Contains("만도"))
                return "자동차·모빌리티";
            if (n.Contains("NAVER") || n.Contains("카카오") || n.Contains("크래프톤") || n.Contains("엔씨"))
                return "플랫폼·IT·SW";
            if (n.Contains("LG전자") || n.Contains("삼성전기") || n.Contains("SDI") || n.Contains("에너지솔루션") ||
                n.Contains("이노베이션") || n.Contains("화학") || n.Contains("에코프로"))
                return "전자·2차전지·에너지";
            if (n.Contains("바이오") || n.Contains("셀트리온") || n.Contains("대웅") || n.Contains("유한") ||
                n.Contains("실리콘투") || n.Contains("에이피알") || n.Contains("아모레"))
                return "바이오·헬스·뷰티";
            if (n.Contains("중공업") || n.Contains("항공우주") || n.Contains("에어로") || n.Contains("현대로템") ||
                n.Contains("LIG") || n.Contains("두산") || n.Contains("전력") || n.Contains("삼성E&A") || n.Contains("삼성물산"))
                return "조선·방산·인프라";
            if (n.Contains("금융") || n.Contains("은행") || n.Contains("지주") || n.Contains("화재") ||
                n.Contains("생명") || n.Contains("증권") || n.Contains("KT&G") || n.Contains("오리온") || n.Contains("SK텔레콤") || n.Contains("네트웍스"))
                return "금융·통신·소비재";

            return "기타 일반 업종";
        }

        private void InitializeCustomUi()
        {
            // 1. 상단 종합 요약 배너
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 68,
                BackColor = Color.FromArgb(22, 27, 38),
                Padding = new Padding(16, 8, 16, 8)
            };

            _lblTopSummary = new Label
            {
                Font = new Font("Malgun Gothic", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 230, 255),
                Dock = DockStyle.Top,
                Height = 24
            };

            _lblCashAdvice = new Label
            {
                Font = new Font("Malgun Gothic", 9f),
                ForeColor = Color.FromArgb(200, 220, 240),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var pnlCashInput = new Panel
            {
                Dock = DockStyle.Right,
                Width = 285,
                Padding = new Padding(6, 12, 0, 12)
            };

            var lblCashEdit = new Label
            {
                Text = "예수금(원):",
                ForeColor = Color.FromArgb(180, 200, 225),
                Location = new Point(4, 18),
                AutoSize = true
            };

            _nudDepositCash = new NumericUpDown
            {
                Location = new Point(78, 15),
                Width = 135,
                Maximum = 10000000000m,
                ThousandsSeparator = true,
                Value = Math.Max(0m, _portfolio.DepositCash),
                BackColor = Color.FromArgb(28, 34, 48),
                ForeColor = Color.White
            };

            var btnApplyCash = new Button
            {
                Text = "적용",
                Location = new Point(218, 14),
                Width = 60,
                Height = 25,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(14, 58, 78),
                ForeColor = Color.FromArgb(0, 230, 255),
                Cursor = Cursors.Hand
            };
            btnApplyCash.FlatAppearance.BorderColor = Color.FromArgb(0, 180, 220);
            btnApplyCash.Click += (s, e) =>
            {
                _portfolio.DepositCash = _nudDepositCash.Value;
                _onSavePortfolio?.Invoke();
                ComputeAllocationData();
                BindDataToUi();
            };

            pnlCashInput.Controls.Add(lblCashEdit);
            pnlCashInput.Controls.Add(_nudDepositCash);
            pnlCashInput.Controls.Add(btnApplyCash);

            var pnlTopLeft = new Panel { Dock = DockStyle.Fill };
            pnlTopLeft.Controls.Add(_lblCashAdvice);
            pnlTopLeft.Controls.Add(_lblTopSummary);

            pnlTop.Controls.Add(pnlTopLeft);
            pnlTop.Controls.Add(pnlCashInput);

            // 2. 중앙 듀얼 도넛 차트 영역
            var tblCharts = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 285,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.FromArgb(18, 22, 32),
                Padding = new Padding(12, 8, 12, 8)
            };
            tblCharts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44f));
            tblCharts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56f));

            _pnlCashChart = new DonutChartPanel("현금(예수금) vs 주식 투자 비중");
            _pnlSectorChart = new DonutChartPanel("업종·섹터별 투자금 쏠림 분포");

            tblCharts.Controls.Add(_pnlCashChart, 0, 0);
            tblCharts.Controls.Add(_pnlSectorChart, 1, 0);

            // 3. 섹터 쏠림 경보 바
            var pnlAlertBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 32,
                BackColor = Color.FromArgb(24, 30, 42),
                Padding = new Padding(16, 4, 16, 4)
            };

            _lblSectorAlert = new Label
            {
                Font = new Font("Malgun Gothic", 9f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlAlertBar.Controls.Add(_lblSectorAlert);

            // 4. 하단 섹터별 상세 내역 테이블 + 선택 섹터 종목 상세
            var splitBottom = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 580,
                BackColor = Color.FromArgb(16, 20, 28)
            };

            _lvSectors = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false,
                BackColor = Color.FromArgb(14, 18, 26),
                ForeColor = Color.FromArgb(230, 235, 245),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Malgun Gothic", 9f)
            };
            _lvSectors.Columns.Add("업종 / 섹터", 160);
            _lvSectors.Columns.Add("비중(주식내)", 85, HorizontalAlignment.Right);
            _lvSectors.Columns.Add("평가금액", 110, HorizontalAlignment.Right);
            _lvSectors.Columns.Add("평가손익", 110, HorizontalAlignment.Right);
            _lvSectors.Columns.Add("종목수", 65, HorizontalAlignment.Center);

            _lvSectors.SelectedIndexChanged += (s, e) =>
            {
                if (_lvSectors.SelectedIndices.Count > 0)
                {
                    _selectedSectorIndex = _lvSectors.SelectedIndices[0];
                    UpdateSectorDetailPanel();
                }
            };

            var pnlDetailBox = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 24, 36),
                Padding = new Padding(12, 8, 12, 8)
            };

            _lblSectorDetail = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Malgun Gothic", 9f),
                ForeColor = Color.FromArgb(210, 225, 245),
                Text = "좌측 섹터 행을 클릭하면 해당 섹터에 속한 보유 종목 목록이 표시됩니다."
            };
            pnlDetailBox.Controls.Add(_lblSectorDetail);

            splitBottom.Panel1.Controls.Add(_lvSectors);
            splitBottom.Panel2.Controls.Add(pnlDetailBox);

            // 5. 하단 닫기 푸터
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                BackColor = Color.FromArgb(22, 27, 38),
                Padding = new Padding(14, 6, 14, 6)
            };

            var btnClose = new Button
            {
                Text = "닫기 (Esc)",
                Dock = DockStyle.Right,
                Width = 100,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(38, 46, 62),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(70, 90, 120);
            btnClose.Click += (s, e) => this.Close();
            pnlFooter.Controls.Add(btnClose);

            this.Controls.Add(splitBottom);
            this.Controls.Add(pnlAlertBar);
            this.Controls.Add(tblCharts);
            this.Controls.Add(pnlTop);
            this.Controls.Add(pnlFooter);

            this.KeyPreview = true;
            this.KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) this.Close(); };
        }

        private void BindDataToUi()
        {
            decimal cashPct = _totalAssetAmount > 0 ? Math.Round(_cashAmount / _totalAssetAmount * 100m, 1) : 0m;
            decimal stockPct = _totalAssetAmount > 0 ? Math.Round(_stockEvalAmount / _totalAssetAmount * 100m, 1) : 0m;

            _lblTopSummary.Text = $"총 자산: {_totalAssetAmount:N0}원  |  주식 평가금: {_stockEvalAmount:N0}원 ({stockPct:F1}%)  |  보유 현금(예수금): {_cashAmount:N0}원 ({cashPct:F1}%)";

            // 시장 체온 연동 현금 방어력 진단
            string advice;
            Color adviceColor;
            if (_thermoScore <= 34.0m)
            {
                if (cashPct >= 30m)
                {
                    advice = $"🛡️ [시장 체온: {_thermoBadgeText}] 공포장 권장 현금 비중(30%↑)을 충족 중입니다! 현재 현금 {cashPct:F1}%로 저점 분할 매수 여력이 매우 안전합니다.";
                    adviceColor = Color.FromArgb(0, 230, 118);
                }
                else
                {
                    advice = $"⚠ [시장 체온: {_thermoBadgeText}] 공포 구간에서는 현금 30% 이상 확보를 권장합니다. (현재 현금 {cashPct:F1}% — 무리한 추격 매수 자제)";
                    adviceColor = Color.FromArgb(255, 190, 60);
                }
            }
            else
            {
                advice = $"💡 [시장 체온: {_thermoBadgeText}] 현재 현금 비중 {cashPct:F1}% ({_cashAmount:N0}원) · 주식 비중 {stockPct:F1}% ({_stockEvalAmount:N0}원) 운용 중";
                adviceColor = Color.FromArgb(180, 215, 245);
            }
            _lblCashAdvice.Text = advice;
            _lblCashAdvice.ForeColor = adviceColor;

            // 1) 좌측 도넛: 현금 vs 주식
            var cashSlices = new List<DonutSlice>();
            if (_cashAmount > 0)
            {
                cashSlices.Add(new DonutSlice
                {
                    Label = "보유 현금(예수금)",
                    Value = _cashAmount,
                    Percent = cashPct,
                    Color = Color.FromArgb(0, 230, 118)
                });
            }
            if (_stockEvalAmount > 0)
            {
                cashSlices.Add(new DonutSlice
                {
                    Label = "주식 투자금",
                    Value = _stockEvalAmount,
                    Percent = stockPct,
                    Color = Color.FromArgb(0, 185, 255)
                });
            }
            _pnlCashChart.SetData(cashSlices, $"현금 {cashPct:F1}%", $"{_cashAmount / 10000m:N0}만원");

            // 2) 우측 도넛: 업종·섹터
            var sectorDonutSlices = _sectorSlices.Select(s => new DonutSlice
            {
                Label = s.SectorName,
                Value = s.EvalAmount,
                Percent = s.SharePercent,
                Color = s.Color
            }).ToList();

            string topSectorName = _sectorSlices.Count > 0 ? _sectorSlices[0].SectorName : "보유없음";
            decimal topSectorPct = _sectorSlices.Count > 0 ? _sectorSlices[0].SharePercent : 0m;
            _pnlSectorChart.SetData(sectorDonutSlices, $"1위 {topSectorPct:F1}%", topSectorName);

            // 3) 섹터 쏠림 경보
            if (_sectorSlices.Count > 0 && topSectorPct >= 40m)
            {
                _lblSectorAlert.Text = $"⚠ [섹터 집중 경보] '{topSectorName}' 비중이 {topSectorPct:F1}%로 높습니다! 신규 진입 시 타 섹터 분산 투자를 권장합니다.";
                _lblSectorAlert.ForeColor = Color.FromArgb(255, 175, 75);
            }
            else if (_sectorSlices.Count > 0)
            {
                _lblSectorAlert.Text = $"✔ [섹터 분산 양호] 최대 비중 섹터 '{topSectorName}'({topSectorPct:F1}%)로 특정 업종 과도한 쏠림 없이 양호하게 분산되어 있습니다.";
                _lblSectorAlert.ForeColor = Color.FromArgb(0, 230, 118);
            }
            else
            {
                _lblSectorAlert.Text = "• 현재 보유 중인 주식이 없습니다.";
                _lblSectorAlert.ForeColor = Color.FromArgb(160, 175, 195);
            }

            // 4) 하단 리스트뷰 채우기
            _lvSectors.BeginUpdate();
            _lvSectors.Items.Clear();
            foreach (var s in _sectorSlices)
            {
                decimal pnl = s.EvalAmount - s.BuyAmount;
                var lvi = new ListViewItem("■ " + s.SectorName);
                lvi.ForeColor = s.Color;
                lvi.SubItems.Add($"{s.SharePercent:F1}%");
                lvi.SubItems.Add($"{s.EvalAmount:N0}원");
                lvi.SubItems.Add($"{pnl:+N0;-N0;0}원");
                lvi.SubItems.Add($"{s.Items.Count}종목");
                lvi.Tag = s;
                _lvSectors.Items.Add(lvi);
            }
            _lvSectors.EndUpdate();

            if (_lvSectors.Items.Count > 0)
            {
                _lvSectors.Items[0].Selected = true;
            }
        }

        private void UpdateSectorDetailPanel()
        {
            if (_selectedSectorIndex < 0 || _selectedSectorIndex >= _sectorSlices.Count) return;
            var s = _sectorSlices[_selectedSectorIndex];

            decimal pnl = s.EvalAmount - s.BuyAmount;
            decimal pnlRate = s.BuyAmount > 0 ? Math.Round(pnl / s.BuyAmount * 100m, 2) : 0m;
            var lines = new List<string>
            {
                $"【 {s.SectorName} 】  비중: {s.SharePercent:F1}%  |  평가금: {s.EvalAmount:N0}원  |  합산 손익: {pnl:+N0;-N0;0}원 ({pnlRate:+0.00;-0.00}%)",
                "────────────────────────────────────────────"
            };

            foreach (var itm in s.Items.OrderByDescending(x => x.TotalEvalAmount))
            {
                lines.Add($"• {itm.StockName} ({itm.Quantity}주)  |  평가: {itm.TotalEvalAmount:N0}원  |  수익률: {itm.ProfitRate:+0.00;-0.00}%");
            }
            _lblSectorDetail.Text = string.Join("\n", lines);
        }

        private class SectorSlice
        {
            public string SectorName { get; set; }
            public decimal EvalAmount { get; set; }
            public decimal BuyAmount { get; set; }
            public decimal SharePercent { get; set; }
            public Color Color { get; set; }
            public List<StockHoldingItem> Items { get; } = new List<StockHoldingItem>();
        }

        private class DonutSlice
        {
            public string Label { get; set; }
            public decimal Value { get; set; }
            public decimal Percent { get; set; }
            public Color Color { get; set; }
        }

        /// <summary>
        /// Zero-Resource GDI+ 도넛 차트 커스텀 컨트롤
        /// </summary>
        private class DonutChartPanel : Panel
        {
            private readonly string _title;
            private List<DonutSlice> _slices = new List<DonutSlice>();
            private string _centerBig = "-";
            private string _centerSub = string.Empty;

            private static readonly Font TitleFont = new Font("Malgun Gothic", 9.5f, FontStyle.Bold);
            private static readonly Font CenterBigFont = new Font("Malgun Gothic", 11.5f, FontStyle.Bold);
            private static readonly Font CenterSubFont = new Font("Malgun Gothic", 8.5f);
            private static readonly Font LegendFont = new Font("Malgun Gothic", 8.5f);
            private static readonly Brush CenterBgBrush = new SolidBrush(Color.FromArgb(18, 22, 32));
            private static readonly Brush EmptyRingBrush = new SolidBrush(Color.FromArgb(35, 42, 58));
            private static readonly Dictionary<int, SolidBrush> BrushCache = new Dictionary<int, SolidBrush>();

            private static SolidBrush GetCachedBrush(Color c)
            {
                int argb = c.ToArgb();
                if (!BrushCache.TryGetValue(argb, out var br))
                {
                    br = new SolidBrush(c);
                    BrushCache[argb] = br;
                }
                return br;
            }

            public DonutChartPanel(string title)
            {
                _title = title;
                this.DoubleBuffered = true;
                this.Dock = DockStyle.Fill;
                this.BackColor = Color.FromArgb(20, 25, 36);
                this.Margin = new Padding(6);
            }

            public void SetData(List<DonutSlice> slices, string centerBig, string centerSub)
            {
                _slices = slices ?? new List<DonutSlice>();
                _centerBig = centerBig ?? "-";
                _centerSub = centerSub ?? string.Empty;
                this.Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // 1. 상단 타이틀
                TextRenderer.DrawText(g, _title, TitleFont, new Point(12, 8), Color.FromArgb(0, 216, 255));

                int diameter = Math.Min(this.Height - 52, 185);
                if (diameter < 80) return;

                var rectOuter = new Rectangle(20, 38, diameter, diameter);
                decimal total = _slices.Sum(x => x.Value);

                if (total <= 0)
                {
                    g.FillEllipse(EmptyRingBrush, rectOuter);
                }
                else
                {
                    float startAngle = -90f;
                    foreach (var s in _slices)
                    {
                        float sweep = (float)(s.Value / total * 360m);
                        if (sweep < 0.5f) sweep = 0.5f;
                        g.FillPie(GetCachedBrush(s.Color), rectOuter, startAngle, sweep);
                        startAngle += sweep;
                    }
                }

                // 도넛 내부 구멍
                int innerOffset = (int)(diameter * 0.28);
                var rectInner = new Rectangle(
                    rectOuter.X + innerOffset,
                    rectOuter.Y + innerOffset,
                    diameter - innerOffset * 2,
                    diameter - innerOffset * 2);
                g.FillEllipse(CenterBgBrush, rectInner);

                // 중앙 텍스트
                var fmtFlags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
                var rectCenterBig = new Rectangle(rectInner.X, rectInner.Y + rectInner.Height / 2 - 18, rectInner.Width, 22);
                var rectCenterSub = new Rectangle(rectInner.X, rectInner.Y + rectInner.Height / 2 + 4, rectInner.Width, 18);
                TextRenderer.DrawText(g, _centerBig, CenterBigFont, rectCenterBig, Color.White, fmtFlags);
                TextRenderer.DrawText(g, _centerSub, CenterSubFont, rectCenterSub, Color.FromArgb(160, 180, 210), fmtFlags);

                // 우측 범례 (최대 8개)
                int legX = rectOuter.Right + 16;
                int legY = 42;
                int maxLeg = Math.Min(_slices.Count, 8);
                for (int i = 0; i < maxLeg; i++)
                {
                    var s = _slices[i];
                    g.FillRectangle(GetCachedBrush(s.Color), legX, legY + 3, 10, 10);
                    string text = $"{s.Label} ({s.Percent:F1}%)";
                    TextRenderer.DrawText(g, text, LegendFont, new Rectangle(legX + 15, legY, this.Width - legX - 20, 18),
                        Color.FromArgb(220, 230, 245), TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
                    legY += 23;
                }
            }
        }
    }
}
