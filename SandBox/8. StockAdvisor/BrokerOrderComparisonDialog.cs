using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using GateHelper.StockAdvisor.Models;

namespace GateHelper.StockAdvisor
{
    /// <summary>
    /// MTS/HTS에 설정된 보유주식청산(스탑로스 손실제한 / 이익목표)과 AI 참모의 권고 타점을 비교·대조하는 다이얼로그
    /// </summary>
    public class BrokerOrderComparisonDialog : Form
    {
        #region Data Models & Enums

        public enum OrderDiffType
        {
            AllDifferent,     // ⚠️ 둘 다 상이 (손절가/목표가 모두 다름)
            StopLossDiffOnly, // ⚠️ 손절가 상이 (손절선 재설정 필요)
            TargetDiffOnly,   // ⚠️ 목표가 상이 (익절선 재설정 필요)
            Matched,          // ✔ 일치 (HTS 설정 == AI 권고)
            NotHeld,          // ❓ 미보유 (포트폴리오에 없는 종목)
            AiTargetPending   // ⚪ AI타점 대기 (지지선/타점 분석 대기)
        }

        public class ComparedOrderItem
        {
            public string StockCode { get; set; } = string.Empty;
            public string StockName { get; set; } = string.Empty;
            public int HtsQty { get; set; } = 0;
            public decimal HtsBuyPrice { get; set; } = 0m;
            public decimal HtsCurrentPrice { get; set; } = 0m;
            public decimal HtsStopLossPrice { get; set; } = 0m;  // HTS 손실제한
            public decimal HtsTargetPrice { get; set; } = 0m;    // HTS 이익목표
            public string HtsStatus { get; set; } = "감시중";
            public string HtsValidDate { get; set; } = string.Empty;
            public string HtsSetDate { get; set; } = string.Empty;

            // SandBox AI 참모 진단 데이터
            public bool IsHeldInPortfolio { get; set; } = false;
            public StockHoldingItem MatchedHoldingItem { get; set; }
            public decimal AiStopPrice { get; set; } = 0m;
            public decimal AiTargetPrice { get; set; } = 0m;
            public decimal AiSecondaryTargetPrice { get; set; } = 0m;

            // 대조 결과
            public OrderDiffType DiffType { get; set; } = OrderDiffType.Matched;
            public decimal StopPriceDiff => (AiStopPrice > 0 && HtsStopLossPrice > 0) ? (AiStopPrice - HtsStopLossPrice) : 0m;
            public decimal TargetPriceDiff => (AiTargetPrice > 0 && HtsTargetPrice > 0) ? (AiTargetPrice - HtsTargetPrice) : 0m;

            public string DiffStatusBadgeText
            {
                get
                {
                    switch (DiffType)
                    {
                        case OrderDiffType.AllDifferent: return "⚠️ 둘다상이";
                        case OrderDiffType.StopLossDiffOnly: return "⚠️ 손절가상이";
                        case OrderDiffType.TargetDiffOnly: return "⚠️ 목표가상이";
                        case OrderDiffType.Matched: return "✔ 일치";
                        case OrderDiffType.NotHeld: return "❓ 미보유종목";
                        case OrderDiffType.AiTargetPending: return "⚪ AI타점대기";
                        default: return "대조대기";
                    }
                }
            }

            public Color DiffBadgeColor
            {
                get
                {
                    switch (DiffType)
                    {
                        case OrderDiffType.AllDifferent: return Color.FromArgb(255, 80, 80);
                        case OrderDiffType.StopLossDiffOnly: return Color.FromArgb(255, 140, 50);
                        case OrderDiffType.TargetDiffOnly: return Color.FromArgb(0, 216, 255);
                        case OrderDiffType.Matched: return Color.FromArgb(0, 230, 118);
                        case OrderDiffType.NotHeld: return Color.FromArgb(170, 180, 200);
                        default: return Color.FromArgb(140, 150, 170);
                    }
                }
            }

            public string ActionGuide
            {
                get
                {
                    if (!IsHeldInPortfolio)
                    {
                        return $"【미보유 종목】 [{StockName}] 종목은 현재 내 SandBox 포트폴리오에 등록되어 있지 않습니다. 관심 종목으로 추가하시거나 외부 계좌 설정인지 확인하세요.";
                    }

                    if (DiffType == OrderDiffType.Matched)
                    {
                        return $"【✔ 완벽 일치】 [{StockName}] HTS에 설정된 손실제한({HtsStopLossPrice:N0}원)과 이익목표({HtsTargetPrice:N0}원)가 AI 참모의 최신 권고 타점과 정확히 일치합니다. 현재 설정을 유지하세요.";
                    }

                    var sb = new StringBuilder();
                    sb.Append($"【⚠️ 타점 재설정 권고 : {StockName}】\n");

                    if (HtsStopLossPrice > 0 && AiStopPrice > 0)
                    {
                        if (AiStopPrice > HtsStopLossPrice)
                        {
                            decimal diff = AiStopPrice - HtsStopLossPrice;
                            sb.Append($"• [손절가 상향 권고]: AI 최근 지지선이 {AiStopPrice:N0}원으로 상향되었습니다. HTS의 손실제한을 {diff:N0}원 올려 {AiStopPrice:N0}원으로 변경하시면 수익/원금 방어에 유리합니다.\n");
                        }
                        else if (AiStopPrice < HtsStopLossPrice)
                        {
                            decimal diff = HtsStopLossPrice - AiStopPrice;
                            sb.Append($"• [손절가 하향 검토]: HTS 손실제한({HtsStopLossPrice:N0}원)이 AI 지지선({AiStopPrice:N0}원)보다 {diff:N0}원 높게 설정되어 있어 장중 일시적 노이즈에 조기 손절될 수 있습니다.\n");
                        }
                    }
                    else if (HtsStopLossPrice == 0 && AiStopPrice > 0)
                    {
                        sb.Append($"• [손절가 신규 설정]: 현재 HTS에 손실제한이 미설정 상태입니다. 20일 지지선 방어선인 {AiStopPrice:N0}원을 설정하세요.\n");
                    }

                    if (HtsTargetPrice > 0 && AiTargetPrice > 0)
                    {
                        if (AiTargetPrice > HtsTargetPrice)
                        {
                            decimal diff = AiTargetPrice - HtsTargetPrice;
                            sb.Append($"• [목표가 상향 가능]: AI 권고 1차 목표가가 {AiTargetPrice:N0}원으로 현재 HTS({HtsTargetPrice:N0}원)보다 {diff:N0}원 높습니다. 익절선을 높여 수익을 극대화할 수 있습니다.");
                        }
                        else if (AiTargetPrice < HtsTargetPrice)
                        {
                            decimal diff = HtsTargetPrice - AiTargetPrice;
                            sb.Append($"• [목표가 조정 권고]: HTS 이익목표({HtsTargetPrice:N0}원)가 AI 단기 저항선({AiTargetPrice:N0}원)보다 {diff:N0}원 높아 체결되지 않고 꺾일 위험이 있습니다.");
                        }
                    }

                    return sb.ToString().TrimEnd();
                }
            }
        }

        #endregion

        #region Zero-Resource Static Cache

        private static readonly Font HeaderTitleFont = new Font("Malgun Gothic", 11.5f, FontStyle.Bold);
        private static readonly Font HeaderSubFont = new Font("Malgun Gothic", 8.8f);
        private static readonly Font ListFont = new Font("Segoe UI", 9.0f);
        private static readonly Font ListBoldFont = new Font("Segoe UI", 9.0f, FontStyle.Bold);
        private static readonly Font GuideFont = new Font("Malgun Gothic", 9.2f);
        private static readonly Font BadgeFont = new Font("Malgun Gothic", 8.2f, FontStyle.Bold);

        private static readonly SolidBrush TextWhiteBrush = new SolidBrush(Color.White);
        private static readonly SolidBrush TextMutedBrush = new SolidBrush(Color.FromArgb(170, 185, 210));
        private static readonly SolidBrush TextCyanBrush = new SolidBrush(Color.FromArgb(0, 230, 255));
        private static readonly SolidBrush TextGoldBrush = new SolidBrush(Color.FromArgb(255, 215, 64));
        private static readonly SolidBrush TextGreenBrush = new SolidBrush(Color.FromArgb(0, 230, 118));
        private static readonly SolidBrush TextRedBrush = new SolidBrush(Color.FromArgb(255, 95, 110));

        private static readonly Pen PenBorder = new Pen(Color.FromArgb(42, 54, 74), 1f);
        private static readonly Pen PenActiveCard = new Pen(Color.FromArgb(0, 216, 255), 1.2f);

        #endregion

        #region UI Fields

        private readonly PortfolioData _portfolio;
        private readonly List<ComparedOrderItem> _allComparedItems = new List<ComparedOrderItem>();
        private string _activeFilter = "전체";

        private ListView _lvOrders;
        private Label _lblSummaryTotal;
        private Label _lblSummaryDifferent;
        private Label _lblSummaryMatched;
        private Label _lblSummaryNotHeld;

        private Button _btnFilterAll;
        private Button _btnFilterDiff;
        private Button _btnFilterMatched;
        private Button _btnFilterNotHeld;

        private RichTextBox _rtbGuide;
        private Button _btnCopyAiStop;
        private Button _btnCopyAiTarget;
        private Button _btnPasteClipboard;

        #endregion

        public BrokerOrderComparisonDialog(PortfolioData portfolio)
        {
            _portfolio = portfolio ?? new PortfolioData();

            InitializeComponent();

            // 열릴 때 클립보드에 데이터가 있으면 자동으로 즉시 감지하여 붙여넣기 시도
            this.Shown += (s, e) =>
            {
                if (Clipboard.ContainsText())
                {
                    string clip = Clipboard.GetText();
                    if (clip.Contains("손실제한") || clip.Contains("이익목표") || clip.Contains("감시중") || clip.Contains("종목코드"))
                    {
                        ParseAndCompareClipboardData(clip);
                    }
                }
            };
        }

        #region UI Construction

        private void InitializeComponent()
        {
            this.Text = "MTS/HTS 자동청산(익절·손절) 설정 대조 & AI 동기화 검사기";
            this.Size = new Size(1160, 720);
            this.MinimumSize = new Size(1020, 620);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(16, 20, 28);
            this.ForeColor = Color.White;
            this.KeyPreview = true;
            this.ShowIcon = false;

            this.KeyDown += (s, e) =>
            {
                if (e.Control && e.KeyCode == Keys.V)
                {
                    PasteFromClipboard();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    this.Close();
                }
            };

            // 1. 상단 타이틀 & 툴바 패널
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 118,
                BackColor = Color.FromArgb(22, 28, 40),
                Padding = new Padding(16, 10, 16, 8)
            };

            var lblTitle = new Label
            {
                Text = "⚖️ MTS/HTS 자동청산(익절·손절) 설정 대조기",
                Font = HeaderTitleFont,
                ForeColor = Color.FromArgb(0, 230, 255),
                Dock = DockStyle.Top,
                Height = 24
            };

            var lblSubtitle = new Label
            {
                Text = "증권사 MTS/HTS의 '보유주식청산(스탑로스)' 화면에서 표를 복사(Ctrl+C)한 후 [클립보드 붙여넣기]를 누르시면, AI 참모의 최신 권고 타점과 대조하여 불일치 종목을 즉시 찾아냅니다.",
                Font = HeaderSubFont,
                ForeColor = Color.FromArgb(160, 180, 210),
                Dock = DockStyle.Top,
                Height = 20
            };

            // 버튼 및 요약 카드 행 (가로 1행)
            var pnlActionBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                BackColor = Color.Transparent
            };

            _btnPasteClipboard = CreateNeonButton("📋 클립보드 붙여넣기 (Ctrl+V)", Color.FromArgb(16, 52, 38), Color.FromArgb(0, 230, 118), Color.FromArgb(220, 255, 235), 230);
            _btnPasteClipboard.Click += (s, e) => PasteFromClipboard();

            var btnClear = CreateNeonButton("초기화", Color.FromArgb(32, 38, 52), Color.FromArgb(90, 110, 140), Color.FromArgb(200, 215, 235), 75);
            btnClear.Click += (s, e) => ClearData();

            var btnSample = CreateNeonButton("샘플 데이터 체험", Color.FromArgb(28, 36, 56), Color.FromArgb(64, 150, 255), Color.FromArgb(215, 235, 255), 130);
            btnSample.Click += (s, e) => LoadSampleData();

            _btnPasteClipboard.Location = new Point(0, 5);
            btnClear.Location = new Point(236, 5);
            btnSample.Location = new Point(317, 5);

            // 우측 요약 상태 카드
            var flpSummary = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 6, 0, 0)
            };

            _lblSummaryTotal = CreateSummaryBadge("대조: 0건", Color.FromArgb(28, 36, 50), Color.FromArgb(180, 195, 220));
            _lblSummaryDifferent = CreateSummaryBadge("⚠️ 재설정필요: 0건", Color.FromArgb(48, 20, 26), Color.FromArgb(255, 110, 120));
            _lblSummaryMatched = CreateSummaryBadge("✔ 일치: 0건", Color.FromArgb(16, 44, 30), Color.FromArgb(0, 230, 118));
            _lblSummaryNotHeld = CreateSummaryBadge("❓ 미보유: 0건", Color.FromArgb(36, 36, 48), Color.FromArgb(180, 190, 215));

            flpSummary.Controls.Add(_lblSummaryTotal);
            flpSummary.Controls.Add(_lblSummaryDifferent);
            flpSummary.Controls.Add(_lblSummaryMatched);
            flpSummary.Controls.Add(_lblSummaryNotHeld);

            pnlActionBar.Controls.Add(_btnPasteClipboard);
            pnlActionBar.Controls.Add(btnClear);
            pnlActionBar.Controls.Add(btnSample);
            pnlActionBar.Controls.Add(flpSummary);

            pnlTop.Controls.Add(pnlActionBar);
            pnlTop.Controls.Add(lblSubtitle);
            pnlTop.Controls.Add(lblTitle);

            // 2. 필터 탭 바 (전체 / ⚠️ 재설정 필요 / ✔ 일치 / ❓ 미보유)
            var pnlFilterBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.FromArgb(18, 24, 34),
                Padding = new Padding(16, 3, 16, 3)
            };

            _btnFilterAll = CreateFilterTabButton("전체 (0)", "전체");
            _btnFilterDiff = CreateFilterTabButton("⚠️ 재설정 필요 (0)", "재설정필요");
            _btnFilterMatched = CreateFilterTabButton("✔ 일치 (0)", "일치");
            _btnFilterNotHeld = CreateFilterTabButton("❓ 미보유 종목 (0)", "미보유");

            _btnFilterAll.Click += (s, e) => SetFilter("전체");
            _btnFilterDiff.Click += (s, e) => SetFilter("재설정필요");
            _btnFilterMatched.Click += (s, e) => SetFilter("일치");
            _btnFilterNotHeld.Click += (s, e) => SetFilter("미보유");

            var flpFilters = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                WrapContents = false,
                BackColor = Color.Transparent
            };
            flpFilters.Controls.Add(_btnFilterAll);
            flpFilters.Controls.Add(_btnFilterDiff);
            flpFilters.Controls.Add(_btnFilterMatched);
            flpFilters.Controls.Add(_btnFilterNotHeld);

            pnlFilterBar.Controls.Add(flpFilters);

            // 3. 하단 AI 참모 조치 가이드 & 복사 액션 패널
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 135,
                BackColor = Color.FromArgb(20, 26, 38),
                Padding = new Padding(16, 8, 16, 8)
            };

            var pnlBottomActions = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 34,
                BackColor = Color.Transparent
            };

            _btnCopyAiStop = CreateNeonButton("📋 선택 종목 AI 손절가 복사", Color.FromArgb(46, 24, 28), Color.FromArgb(255, 120, 130), Color.FromArgb(255, 215, 220), 200);
            _btnCopyAiStop.Click += (s, e) => CopySelectedPrice(isStopLoss: true);

            _btnCopyAiTarget = CreateNeonButton("📋 선택 종목 AI 목표가 복사", Color.FromArgb(16, 44, 48), Color.FromArgb(0, 216, 255), Color.FromArgb(215, 250, 255), 200);
            _btnCopyAiTarget.Click += (s, e) => CopySelectedPrice(isStopLoss: false);

            var btnClose = CreateNeonButton("닫기 (Esc)", Color.FromArgb(32, 38, 52), Color.FromArgb(90, 110, 140), Color.White, 95);
            btnClose.Click += (s, e) => this.Close();
            btnClose.Dock = DockStyle.Right;

            _btnCopyAiStop.Dock = DockStyle.Left;
            _btnCopyAiTarget.Dock = DockStyle.Left;
            _btnCopyAiTarget.Margin = new Padding(8, 0, 0, 0);

            pnlBottomActions.Controls.Add(btnClose);
            pnlBottomActions.Controls.Add(_btnCopyAiTarget);
            pnlBottomActions.Controls.Add(_btnCopyAiStop);

            _rtbGuide = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(14, 18, 26),
                ForeColor = Color.FromArgb(220, 235, 255),
                BorderStyle = BorderStyle.None,
                Font = GuideFont,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
            _rtbGuide.Text = "💡 위 목록에서 종목을 클릭하시면 해당 종목의 HTS 설정값과 AI 권고 타점 간의 구체적 차이점 및 HTS 재설정 가이드가 표시됩니다.";

            pnlBottom.Controls.Add(_rtbGuide);
            pnlBottom.Controls.Add(pnlBottomActions);

            // 4. 메인 리스트뷰 (테이블 컬럼 배치 - 앞쪽 열에 AI 진단 결과 집중 배치)
            _lvOrders = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false,
                BackColor = Color.FromArgb(14, 17, 24),
                ForeColor = Color.White,
                Font = ListFont,
                BorderStyle = BorderStyle.None,
                OwnerDraw = false
            };

            // 사용자 요청 반영: 테이블 앞쪽 열에 AI 진단평가의 익절가/손절가가 어떤 건지도 표시
            _lvOrders.Columns.Add("대조 상태", 115, HorizontalAlignment.Center);
            _lvOrders.Columns.Add("종목명", 110, HorizontalAlignment.Left);
            _lvOrders.Columns.Add("종목코드", 75, HorizontalAlignment.Center);
            _lvOrders.Columns.Add("보유 구분", 95, HorizontalAlignment.Center);
            _lvOrders.Columns.Add("현재가", 85, HorizontalAlignment.Right);
            _lvOrders.Columns.Add("[AI] 권고 손절가", 105, HorizontalAlignment.Right);
            _lvOrders.Columns.Add("[AI] 권고 1차목표가", 115, HorizontalAlignment.Right);
            _lvOrders.Columns.Add("[HTS] 손실제한", 100, HorizontalAlignment.Right);
            _lvOrders.Columns.Add("[HTS] 이익목표", 100, HorizontalAlignment.Right);
            _lvOrders.Columns.Add("손절가 차이 (AI - HTS)", 135, HorizontalAlignment.Right);
            _lvOrders.Columns.Add("목표가 차이 (AI - HTS)", 135, HorizontalAlignment.Right);
            _lvOrders.Columns.Add("HTS 감시상태", 85, HorizontalAlignment.Center);
            _lvOrders.Columns.Add("HTS 설정일자", 95, HorizontalAlignment.Center);

            _lvOrders.SelectedIndexChanged += (s, e) => UpdateSelectedDetailGuide();

            this.Controls.Add(_lvOrders);
            this.Controls.Add(pnlFilterBar);
            this.Controls.Add(pnlBottom);
            this.Controls.Add(pnlTop);
        }

        #endregion

        #region Clipboard Parsing & Order Comparison Engine

        private void PasteFromClipboard()
        {
            try
            {
                if (!Clipboard.ContainsText())
                {
                    MessageBox.Show(this, "클립보드에 복사된 텍스트 데이터가 없습니다.\nMTS나 HTS의 '보유주식청산(스탑로스)' 화면에서 설정 목록을 블록 지정하여 복사(Ctrl+C)한 후 다시 시도해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string clipText = Clipboard.GetText();
                ParseAndCompareClipboardData(clipText);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"클립보드 데이터 읽기 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void ParseAndCompareClipboardData(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText)) return;

            string[] lines = rawText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0) return;

            _allComparedItems.Clear();

            // 헤더 인덱스 매핑 (헤더 행이 존재할 경우 자동 탐색, 없으면 기본 포맷 인덱스 사용)
            int colCode = 2;
            int colName = 3;
            int colBuyPrice = 6;
            int colCurrentPrice = 7;
            int colStatus = 10;
            int colQty = 11;
            int colStopLoss = 12;
            int colTarget = 13;
            int colValidDate = 17;
            int colSetDate = 19;

            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                // 탭 단위 분리
                string[] parts = line.Split('\t').Select(p => p.Trim()).ToArray();
                if (parts.Length < 6) continue;

                // 헤더 라인 감지
                if (line.Contains("종목코드") || line.Contains("손실제한") || line.Contains("이익목표") || line.Contains("합꼐"))
                {
                    // 동적 헤더 인덱스 매핑
                    for (int i = 0; i < parts.Length; i++)
                    {
                        string header = parts[i].Replace(" ", "");
                        if (header.Contains("종목코드")) colCode = i;
                        else if (header.Contains("종목명")) colName = i;
                        else if (header.Contains("매입평균가") || header.Contains("평단")) colBuyPrice = i;
                        else if (header.Contains("현재가")) colCurrentPrice = i;
                        else if (header.Contains("처리상태") || header.Contains("상태")) colStatus = i;
                        else if (header.Contains("수량") && !header.Contains("가능") && !header.Contains("보유")) colQty = i;
                        else if (header.Contains("손실제한") || header.Contains("손절")) colStopLoss = i;
                        else if ((header.Contains("이익목표") || header.Contains("익절") || header.Contains("목표가") || header == "목표") && !header.Contains("TS")) colTarget = i;
                        else if (header.Contains("유효기간")) colValidDate = i;
                        else if (header.Contains("설정일자") || header.Contains("설정일")) colSetDate = i;
                    }
                    continue;
                }

                // 데이터 행 파싱
                string rawCode = (colCode < parts.Length) ? parts[colCode] : string.Empty;
                string cleanCode = CleanStockCode(rawCode);

                // 만약 2번 열에서 6자리 코드를 못 찾으면 다른 열에서 6자리 숫자 탐색
                if (string.IsNullOrEmpty(cleanCode))
                {
                    for (int c = 0; c < Math.Min(parts.Length, 5); c++)
                    {
                        string candidate = CleanStockCode(parts[c]);
                        if (!string.IsNullOrEmpty(candidate))
                        {
                            cleanCode = candidate;
                            colCode = c;
                            if (c + 1 < parts.Length) colName = c + 1;
                            break;
                        }
                    }
                }

                if (string.IsNullOrEmpty(cleanCode)) continue;

                string name = (colName < parts.Length) ? parts[colName] : cleanCode;
                decimal buyPrice = (colBuyPrice < parts.Length) ? ParsePrice(parts[colBuyPrice]) : 0m;
                decimal currentPrice = (colCurrentPrice < parts.Length) ? ParsePrice(parts[colCurrentPrice]) : 0m;
                int qty = (colQty < parts.Length) ? (int)ParsePrice(parts[colQty]) : 0;
                decimal stopLoss = (colStopLoss < parts.Length) ? ParsePrice(parts[colStopLoss]) : 0m;
                decimal targetPrice = (colTarget < parts.Length) ? ParsePrice(parts[colTarget]) : 0m;
                string status = (colStatus < parts.Length) ? parts[colStatus] : "감시중";
                string validDate = (colValidDate < parts.Length) ? parts[colValidDate] : string.Empty;
                string setDate = (colSetDate < parts.Length) ? parts[colSetDate] : string.Empty;

                var item = new ComparedOrderItem
                {
                    StockCode = cleanCode,
                    StockName = name,
                    HtsQty = qty,
                    HtsBuyPrice = buyPrice,
                    HtsCurrentPrice = currentPrice,
                    HtsStopLossPrice = stopLoss,
                    HtsTargetPrice = targetPrice,
                    HtsStatus = status,
                    HtsValidDate = validDate,
                    HtsSetDate = setDate
                };

                // SandBox 포트폴리오와 대조
                MatchWithAiPortfolio(item);

                _allComparedItems.Add(item);
            }

            UpdateSummaryAndListView();
        }

        private void MatchWithAiPortfolio(ComparedOrderItem item)
        {
            if (_portfolio == null || _portfolio.Items == null)
            {
                item.IsHeldInPortfolio = false;
                item.DiffType = OrderDiffType.NotHeld;
                return;
            }

            // 코드 또는 이름으로 매칭
            var held = _portfolio.Items.FirstOrDefault(x =>
                x.StockCode.Equals(item.StockCode, StringComparison.OrdinalIgnoreCase) ||
                x.StockName.Equals(item.StockName, StringComparison.OrdinalIgnoreCase));

            if (held != null)
            {
                item.IsHeldInPortfolio = (held.Quantity > 0);
                item.MatchedHoldingItem = held;
                item.StockName = held.StockName; // 포트폴리오 표준 이름으로 동기화

                // AI 손절가 추출 (TargetStopPrice > 20일 지지선 3% 하방)
                decimal aiStop = held.TargetStopPrice;
                if (aiStop <= 0 && held.SupportPrice > 0)
                {
                    aiStop = Math.Round(held.SupportPrice * 0.97m, 0);
                }
                item.AiStopPrice = aiStop;

                // AI 목표가 추출 (RecommendedTargetPrice > 20일 저항선)
                decimal aiTarget = held.RecommendedTargetPrice;
                if (aiTarget <= 0 && held.ResistancePrice > 0)
                {
                    aiTarget = held.ResistancePrice;
                }
                item.AiTargetPrice = aiTarget;
                item.AiSecondaryTargetPrice = held.SecondaryTargetPrice;

                // 대조 불일치 판정 (오차 0원 기준 엄격 대조)
                bool stopMatches = (item.HtsStopLossPrice > 0 && item.AiStopPrice > 0 && item.HtsStopLossPrice == item.AiStopPrice);
                bool targetMatches = (item.HtsTargetPrice > 0 && item.AiTargetPrice > 0 && item.HtsTargetPrice == item.AiTargetPrice);

                if (!item.IsHeldInPortfolio)
                {
                    item.DiffType = OrderDiffType.NotHeld;
                }
                else if (item.AiStopPrice <= 0 && item.AiTargetPrice <= 0)
                {
                    item.DiffType = OrderDiffType.AiTargetPending;
                }
                else if (stopMatches && targetMatches)
                {
                    item.DiffType = OrderDiffType.Matched;
                }
                else if (!stopMatches && !targetMatches)
                {
                    item.DiffType = OrderDiffType.AllDifferent;
                }
                else if (!stopMatches)
                {
                    item.DiffType = OrderDiffType.StopLossDiffOnly;
                }
                else
                {
                    item.DiffType = OrderDiffType.TargetDiffOnly;
                }
            }
            else
            {
                item.IsHeldInPortfolio = false;
                item.DiffType = OrderDiffType.NotHeld;
            }
        }

        private static string CleanStockCode(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            string clean = raw.Replace("'", "").Replace("\"", "").Trim();
            var match = Regex.Match(clean, @"\d{6}");
            return match.Success ? match.Value : string.Empty;
        }

        private static decimal ParsePrice(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return 0m;
            // 공백 제거 ("85 200" -> "85200"), 원/%/쉼표 제거
            string clean = raw.Replace(" ", "").Replace(",", "").Replace("원", "").Replace("%", "").Trim();
            if (decimal.TryParse(clean, out decimal val))
            {
                return val;
            }
            return 0m;
        }

        #endregion

        #region ListView Rendering & Summary

        private void UpdateSummaryAndListView()
        {
            int total = _allComparedItems.Count;
            int diffCount = _allComparedItems.Count(x => x.DiffType == OrderDiffType.AllDifferent || x.DiffType == OrderDiffType.StopLossDiffOnly || x.DiffType == OrderDiffType.TargetDiffOnly);
            int matchedCount = _allComparedItems.Count(x => x.DiffType == OrderDiffType.Matched);
            int notHeldCount = _allComparedItems.Count(x => !x.IsHeldInPortfolio);

            _lblSummaryTotal.Text = $"대조: {total}건";
            _lblSummaryDifferent.Text = $"⚠️ 재설정필요: {diffCount}건";
            _lblSummaryMatched.Text = $"✔ 일치: {matchedCount}건";
            _lblSummaryNotHeld.Text = $"❓ 미보유: {notHeldCount}건";

            _btnFilterAll.Text = $"전체 ({total})";
            _btnFilterDiff.Text = $"⚠️ 재설정 필요 ({diffCount})";
            _btnFilterMatched.Text = $"✔ 일치 ({matchedCount})";
            _btnFilterNotHeld.Text = $"❓ 미보유 종목 ({notHeldCount})";

            ApplyCurrentFilter();
        }

        private void SetFilter(string filter)
        {
            _activeFilter = filter;
            _btnFilterAll.ForeColor = (_activeFilter == "전체") ? Color.FromArgb(0, 230, 255) : Color.FromArgb(160, 180, 205);
            _btnFilterDiff.ForeColor = (_activeFilter == "재설정필요") ? Color.FromArgb(255, 120, 120) : Color.FromArgb(160, 180, 205);
            _btnFilterMatched.ForeColor = (_activeFilter == "일치") ? Color.FromArgb(0, 230, 118) : Color.FromArgb(160, 180, 205);
            _btnFilterNotHeld.ForeColor = (_activeFilter == "미보유") ? Color.FromArgb(200, 210, 230) : Color.FromArgb(160, 180, 205);

            ApplyCurrentFilter();
        }

        private void ApplyCurrentFilter()
        {
            _lvOrders.BeginUpdate();
            _lvOrders.Items.Clear();

            IEnumerable<ComparedOrderItem> filtered = _allComparedItems;

            if (_activeFilter == "재설정필요")
            {
                filtered = _allComparedItems.Where(x => x.DiffType == OrderDiffType.AllDifferent || x.DiffType == OrderDiffType.StopLossDiffOnly || x.DiffType == OrderDiffType.TargetDiffOnly);
            }
            else if (_activeFilter == "일치")
            {
                filtered = _allComparedItems.Where(x => x.DiffType == OrderDiffType.Matched);
            }
            else if (_activeFilter == "미보유")
            {
                filtered = _allComparedItems.Where(x => !x.IsHeldInPortfolio);
            }

            foreach (var item in filtered)
            {
                var lvi = new ListViewItem(item.DiffStatusBadgeText);
                lvi.UseItemStyleForSubItems = false;
                lvi.Tag = item;

                // 1. 대조 상태 열 색상
                lvi.ForeColor = item.DiffBadgeColor;
                lvi.Font = ListBoldFont;

                // 2. 종목명
                var subName = lvi.SubItems.Add(item.StockName);
                subName.ForeColor = Color.White;
                subName.Font = ListBoldFont;

                // 3. 종목코드
                var subCode = lvi.SubItems.Add(item.StockCode);
                subCode.ForeColor = Color.FromArgb(140, 160, 190);

                // 4. 보유 구분
                var subHeld = lvi.SubItems.Add(item.IsHeldInPortfolio ? "보유중" : "미보유");
                subHeld.ForeColor = item.IsHeldInPortfolio ? Color.FromArgb(0, 230, 255) : Color.FromArgb(150, 160, 180);

                // 5. 현재가
                decimal curPrice = item.HtsCurrentPrice > 0 ? item.HtsCurrentPrice : (item.MatchedHoldingItem?.CurrentPrice ?? 0m);
                var subCur = lvi.SubItems.Add(curPrice > 0 ? $"{curPrice:N0}원" : "-");
                subCur.ForeColor = Color.FromArgb(220, 235, 255);

                // 6. [AI 권고] 손절가
                var subAiStop = lvi.SubItems.Add(item.AiStopPrice > 0 ? $"{item.AiStopPrice:N0}원" : "-");
                subAiStop.ForeColor = Color.FromArgb(255, 140, 150);
                subAiStop.Font = ListBoldFont;

                // 7. [AI 권고] 1차목표가
                var subAiTarget = lvi.SubItems.Add(item.AiTargetPrice > 0 ? $"{item.AiTargetPrice:N0}원" : "-");
                subAiTarget.ForeColor = Color.FromArgb(130, 255, 170);
                subAiTarget.Font = ListBoldFont;

                // 8. [HTS 설정] 손실제한
                var subHtsStop = lvi.SubItems.Add(item.HtsStopLossPrice > 0 ? $"{item.HtsStopLossPrice:N0}원" : "미설정");
                subHtsStop.ForeColor = Color.FromArgb(255, 185, 120);

                // 9. [HTS 설정] 이익목표
                var subHtsTarget = lvi.SubItems.Add(item.HtsTargetPrice > 0 ? $"{item.HtsTargetPrice:N0}원" : "미설정");
                subHtsTarget.ForeColor = Color.FromArgb(140, 225, 255);

                // 10. 손절가 차이 (AI - HTS)
                string stopDiffStr = "-";
                Color stopDiffColor = Color.FromArgb(160, 175, 195);
                if (item.IsHeldInPortfolio && item.AiStopPrice > 0 && item.HtsStopLossPrice > 0)
                {
                    decimal diff = item.StopPriceDiff;
                    if (diff == 0)
                    {
                        stopDiffStr = "0원 (일치)";
                        stopDiffColor = Color.FromArgb(0, 230, 118);
                    }
                    else if (diff > 0)
                    {
                        stopDiffStr = $"▲ +{diff:N0}원 (상향필요)";
                        stopDiffColor = Color.FromArgb(255, 100, 100);
                    }
                    else
                    {
                        stopDiffStr = $"▼ {diff:N0}원 (하향검토)";
                        stopDiffColor = Color.FromArgb(255, 185, 64);
                    }
                }
                var subDiffStop = lvi.SubItems.Add(stopDiffStr);
                subDiffStop.ForeColor = stopDiffColor;
                subDiffStop.Font = ListBoldFont;

                // 11. 목표가 차이 (AI - HTS)
                string targetDiffStr = "-";
                Color targetDiffColor = Color.FromArgb(160, 175, 195);
                if (item.IsHeldInPortfolio && item.AiTargetPrice > 0 && item.HtsTargetPrice > 0)
                {
                    decimal diff = item.TargetPriceDiff;
                    if (diff == 0)
                    {
                        targetDiffStr = "0원 (일치)";
                        targetDiffColor = Color.FromArgb(0, 230, 118);
                    }
                    else if (diff > 0)
                    {
                        targetDiffStr = $"▲ +{diff:N0}원 (상향가능)";
                        targetDiffColor = Color.FromArgb(0, 216, 255);
                    }
                    else
                    {
                        targetDiffStr = $"▼ {diff:N0}원 (하향조정)";
                        targetDiffColor = Color.FromArgb(255, 160, 80);
                    }
                }
                var subDiffTarget = lvi.SubItems.Add(targetDiffStr);
                subDiffTarget.ForeColor = targetDiffColor;
                subDiffTarget.Font = ListBoldFont;

                // 12. HTS 감시상태
                var subStatus = lvi.SubItems.Add(item.HtsStatus);
                subStatus.ForeColor = Color.FromArgb(170, 185, 210);

                // 13. HTS 설정일자
                var subDate = lvi.SubItems.Add(item.HtsSetDate);
                subDate.ForeColor = Color.FromArgb(140, 155, 175);

                _lvOrders.Items.Add(lvi);
            }

            _lvOrders.EndUpdate();

            if (_lvOrders.Items.Count > 0)
            {
                _lvOrders.Items[0].Selected = true;
            }
            else
            {
                _rtbGuide.Text = "💡 표시할 대조 종목 데이터가 없습니다. MTS/HTS에서 표를 복사 후 [클립보드 붙여넣기]를 눌러주세요.";
            }
        }

        private void UpdateSelectedDetailGuide()
        {
            if (_lvOrders.SelectedItems.Count == 0)
            {
                _rtbGuide.Text = "💡 위 목록에서 종목을 클릭하시면 해당 종목의 상세 AI 조치 권고안이 표시됩니다.";
                _btnCopyAiStop.Enabled = false;
                _btnCopyAiTarget.Enabled = false;
                return;
            }

            var item = _lvOrders.SelectedItems[0].Tag as ComparedOrderItem;
            if (item == null) return;

            _rtbGuide.Clear();

            // 상단 타이틀
            Color titleColor = item.DiffBadgeColor;
            AppendGuideText($"【 {item.StockName} ({item.StockCode}) 청산 설정 대조 리포트 】  상태: {item.DiffStatusBadgeText}\n", titleColor, FontStyle.Bold);

            // 본문 안내
            AppendGuideText($"{item.ActionGuide}\n\n", Color.FromArgb(220, 235, 255), FontStyle.Regular);

            // 핵심 수치 박스
            string htsStopStr = item.HtsStopLossPrice > 0 ? $"{item.HtsStopLossPrice:N0}원" : "미설정";
            string htsTargetStr = item.HtsTargetPrice > 0 ? $"{item.HtsTargetPrice:N0}원" : "미설정";
            string aiStopStr = item.AiStopPrice > 0 ? $"{item.AiStopPrice:N0}원" : "분석대기";
            string aiTargetStr = item.AiTargetPrice > 0 ? $"{item.AiTargetPrice:N0}원" : "분석대기";

            AppendGuideText($"• [HTS 현재 설정]: 손실제한={htsStopStr}  |  이익목표={htsTargetStr}  (설정일: {item.HtsSetDate}, 상태: {item.HtsStatus})\n", Color.FromArgb(200, 215, 235), FontStyle.Regular);
            AppendGuideText($"• [AI 참모 권고]: 권고손절={aiStopStr}  |  권고목표={aiTargetStr} (2차목표: {item.AiSecondaryTargetPrice:N0}원)\n", Color.FromArgb(0, 230, 255), FontStyle.Bold);

            _btnCopyAiStop.Enabled = item.AiStopPrice > 0;
            _btnCopyAiStop.Text = item.AiStopPrice > 0 ? $"📋 AI 손절가 ({item.AiStopPrice:N0}원) 복사" : "AI 손절가 없음";

            _btnCopyAiTarget.Enabled = item.AiTargetPrice > 0;
            _btnCopyAiTarget.Text = item.AiTargetPrice > 0 ? $"📋 AI 목표가 ({item.AiTargetPrice:N0}원) 복사" : "AI 목표가 없음";
        }

        private void AppendGuideText(string text, Color color, FontStyle style)
        {
            int start = _rtbGuide.TextLength;
            _rtbGuide.AppendText(text);
            _rtbGuide.Select(start, text.Length);
            _rtbGuide.SelectionColor = color;
            _rtbGuide.SelectionFont = new Font(GuideFont, style);
            _rtbGuide.Select(_rtbGuide.TextLength, 0);
        }

        private void CopySelectedPrice(bool isStopLoss)
        {
            if (_lvOrders.SelectedItems.Count == 0) return;
            var item = _lvOrders.SelectedItems[0].Tag as ComparedOrderItem;
            if (item == null) return;

            decimal price = isStopLoss ? item.AiStopPrice : item.AiTargetPrice;
            if (price <= 0) return;

            string priceStr = $"{price:F0}";
            Clipboard.SetText(priceStr);

            string typeStr = isStopLoss ? "AI 권고 손절가" : "AI 권고 목표가";
            MessageBox.Show(this, $"{item.StockName}의 {typeStr} [{price:N0}원]이 클립보드에 복사되었습니다.\nMTS/HTS의 해당 입력창에 붙여넣기(Ctrl+V)하세요.", "복사 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ClearData()
        {
            _allComparedItems.Clear();
            _lvOrders.Items.Clear();
            _rtbGuide.Text = "💡 데이터가 초기화되었습니다. MTS/HTS에서 표를 복사 후 [클립보드 붙여넣기]를 눌러주세요.";
            UpdateSummaryAndListView();
        }

        private void LoadSampleData()
        {
            string sample =
                "실행\t삭제\t종목코드\t종목명\t가능수량\t보유수량\t매입평균가\t현재가\t평가손익금액\t평가손익률\t처리상태\t수량\t손실제한\t이익목표\tTS목표\t자동정정\t주문가\t유효기간\t자동/수동\t설정일자\t설정\r\n" +
                "\t\t합꼐\t가능수량\t보유수량\t매입평균가\t현재가\t평가손익금액\t평가손익률\t처리상태\t수량\t손실제한\t이익목표\tTS목표\t자동정정\t주문가\t유효기간\t자동/수동\t설정일자\t설정\r\n" +
                "1\t\t'000270\t기아\t0000000001\t0000000001\t0134035.00\t0000092700\t-0000000000000041335\t-00030.83\t감시중\t100%\t85 200\t114 300\t미설정\t미설정\t현재가\t2026/12/31\t자동\t2026/09/23\t직접설정\r\n" +
                "1\t\t'001740\tSK네트웍스\t0000000008\t0000000008\t0009341.25\t0000005270\t-0000000000000032570\t-00043.58\t감시중\t100%\t4 995\t5 940\t미설정\t미설정\t현재가\t2026/12/31\t자동\t2026/11/13\t직접설정\r\n" +
                "1\t\t'005930\t삼성전자\t0000000001\t0000000001\t0230010.00\t0000192300\t-0000000000000037710\t-00016.39\t감시중\t100%\t173 800\t244 500\t미설정\t미설정\t현재가\t2026/12/31\t자동\t2026/11/12\t직접설정\r\n" +
                "1\t\t'010140\t삼성중공업\t0000000008\t0000000008\t0027229.75\t0000016140\t-0000000000000088718\t-00040.72\t감시중\t100%\t14 650\t17 810\t미설정\t미설정\t현재가\t2026/12/31\t자동\t2026/07/14\t직접설정\r\n" +
                "1\t\t'033780\tKT&G\t0000000001\t0000000001\t0174000.00\t0000180500\t00000000000000006500\t000003.73\t감시중\t100%\t165 300\t197 300\t미설정\t미설정\t현재가\t2026/12/31\t자동\t2026/11/13\t직접설정\r\n" +
                "1\t\t'035420\tNAVER\t0000000001\t0000000001\t0195500.00\t0000171700\t-0000000000000023800\t-00012.17\t감시중\t100%\t161 000\t194 300\t미설정\t미설정\t현재가\t2026/12/31\t자동\t2026/09/23\t직접설정\r\n" +
                "1\t\t'047810\t한국항공우주\t0000000001\t0000000001\t0124900.00\t0000104000\t-0000000000000020900\t-00016.73\t감시중\t1주\t92 100\t114 500\t미설정\t미설정\t현재가\t2026/12/31\t자동\t2026/09/02\t직접설정\r\n" +
                "1\t\t257720\t실리콘투\t0000000012\t0000000012\t0024466.67\t0000023700\t-0000000000000009200\t-00003.13\t감시중\t100%\t22 850\t33 600\t미설정\t미설정\t현재가\t2026/12/31\t자동\t2026/11/12\t직접설정\r\n" +
                "1\t\t271560\t오리온\t0000000001\t0000000001\t0122500.00\t0000114200\t-0000000000000008300\t-00006.77\t감시중\t100%\t104 500\t120 500\t미설정\t미설정\t현재가\t2026/12/31\t자동\t2026/09/07\t직접설정\r\n" +
                "0\t\t367380\tACE 미국나스닥100\t0000000049\t0000000049\t0033441.63\t0000027690\t-0000000000000281830\t-00017.19\t미설정\t0%\t0\t0\t미설정\t미설정\t현재가\t2026/12/31\t수동\t2026/01/01\t미설정\r\n" +
                "1\t\t396500\tTIGER 반도체TOP10\t0000000002\t0000000002\t0050817.00\t0000024970\t-0000000000000051694\t-00050.86\t감시중\t100%\t23 000\t33 050\t미설정\t미설정\t현재가\t2026/12/31\t자동\t2026/10/02\t직접설정\r\n" +
                "0\t\t402970\tACE 미국배당다우존스\t0000000043\t0000000043\t0015807.91\t0000015415\t-0000000000000016895\t-00002.48\t미설정\t0%\t0\t0\t미설정\t미설정\t현재가\t2026/12/31\t수동\t2026/01/01\t미설정\r\n" +
                "1\t\t487130\tKoAct AI인프라액티브\t0000000004\t0000000004\t0038904.00\t0000020295\t-0000000000000074436\t-00047.83\t감시중\t100%\t18 170\t26 250\t미설정\t미설정\t현재가\t2026/12/31\t자동\t2026/11/13\t직접설정\r\n";

            ParseAndCompareClipboardData(sample);
        }

        #endregion

        #region Helper Controls

        private Button CreateNeonButton(string text, Color bg, Color border, Color fore, int width = 0)
        {
            var btn = new Button
            {
                Text = text,
                BackColor = bg,
                ForeColor = fore,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Malgun Gothic", 8.8f, FontStyle.Bold),
                Height = 32,
                Margin = new Padding(2),
                Padding = new Padding(0),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            if (width > 0) btn.Width = width;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = border;

            int hr = Math.Min(255, bg.R + 25);
            int hg = Math.Min(255, bg.G + 25);
            int hb = Math.Min(255, bg.B + 25);
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(hr, hg, hb);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(Math.Max(0, bg.R - 10), Math.Max(0, bg.G - 10), Math.Max(0, bg.B - 10));

            return btn;
        }

        private Button CreateFilterTabButton(string text, string filterTag)
        {
            var btn = new Button
            {
                Text = text,
                Tag = filterTag,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = (filterTag == "전체") ? Color.FromArgb(0, 230, 255) : Color.FromArgb(160, 180, 205),
                Font = new Font("Malgun Gothic", 8.5f, FontStyle.Bold),
                Height = 28,
                AutoSize = true,
                Margin = new Padding(0, 0, 8, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 42, 60);
            return btn;
        }

        private Label CreateSummaryBadge(string text, Color bg, Color fore)
        {
            return new Label
            {
                Text = text,
                BackColor = bg,
                ForeColor = fore,
                Font = BadgeFont,
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 28,
                AutoSize = true,
                Padding = new Padding(8, 4, 8, 4),
                Margin = new Padding(4, 0, 0, 0),
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        #endregion
    }
}
