using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace GateHelper.StockAdvisor
{
    /// <summary>
    /// AI 주식 리스크 참모 - 종합 퀀트 점수 산출 기준 및 지표 사전 다이얼로그
    /// (100% GDI+ ClearType 네온 신호등 렌더링 지원 - RichTextBox 흑백 버그 원천 차단)
    /// </summary>
    public class StockScoreHelpDialog : Form
    {
        private FlowLayoutPanel _flpBody;

        public StockScoreHelpDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "AI 주식 리스크 참모 - 종합 퀀트 점수 산출 기준 및 지표 사전";
            ClientSize = new Size(820, 860);
            MinimumSize = new Size(800, 750);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(16, 20, 28);
            ForeColor = Color.FromArgb(230, 235, 245);
            Font = new Font("Malgun Gothic", 9.2f);

            var pnlMain = new Panel { Dock = DockStyle.Fill, Padding = new Padding(18) };

            // 1. 상단 헤더 배너
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 58 };
            var lblTitle = new Label
            {
                Text = "4대 퀀트 진단 점수 산출 기준 & 지표 사전",
                Font = new Font("Malgun Gothic", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 216, 255),
                AutoSize = true,
                Location = new Point(0, 2)
            };
            var lblSubtitle = new Label
            {
                Text = "차트(30%) · 수급·재무(25%) · 뉴스·공시(25%) · 손익비(20%) 4대 팩터 공식 및 수급·지뢰·최종액션 용어 해설",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(150, 165, 185),
                Location = new Point(2, 30),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);

            // 2. 하단 닫기 버튼 바
            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 46, Padding = new Padding(0, 8, 0, 0) };
            var btnClose = new Button
            {
                Text = "확인 (닫기)",
                Size = new Size(110, 36),
                Location = new Point(670, 5),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(25, 38, 55),
                ForeColor = Color.FromArgb(0, 216, 255),
                Cursor = Cursors.Hand,
                Font = new Font("Malgun Gothic", 9.2f, FontStyle.Bold)
            };
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(0, 180, 230);
            btnClose.Click += (s, e) => Close();
            pnlFooter.Controls.Add(btnClose);

            // 3. 중앙 스크롤 본문 (FlowLayoutPanel 자동 배치)
            _flpBody = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(0, 4, 12, 4)
            };

            BuildHelpCards();

            pnlMain.Controls.Add(_flpBody);
            pnlMain.Controls.Add(pnlHeader);
            pnlMain.Controls.Add(pnlFooter);
            Controls.Add(pnlMain);
        }

        private void BuildHelpCards()
        {
            const int cardWidth = 745;

            // [카드 1] 4대 퀀트 진단 점수 산출 공식
            var card1 = new HelpCardPanel(
                "■ [1. 4대 퀀트 진단 점수 산출 공식] (10점 만점 / 4대 정량 팩터)",
                Color.FromArgb(0, 216, 255), Color.FromArgb(18, 28, 42), Color.FromArgb(0, 160, 220), cardWidth);
            card1.AddFullLine("• 최종 산출 공식: [차트 × 30%] + [수급·재무 × 25%] + [뉴스·공시 × 25%] + [손익비 × 20%]", Color.FromArgb(0, 216, 255), true);
            card1.AddItem("①", Color.FromArgb(0, 216, 255), "차트 점수 (30%): ", Color.FromArgb(0, 216, 255), "20일 이평선 정배열(+3.0점), 거래량 실린 양봉(+1.5점), RSI 45~65 안정 탄력(+1.0점), 역배열 하락 추세 감점(-2.5점)");
            card1.AddItem("②", Color.FromArgb(0, 230, 118), "수급·재무 점수 (25%): ", Color.FromArgb(0, 230, 118), "메이저 매수(+2.0점), 외인/기관 순매수(+1.0점), 주주환원 친화(+2.0점), 부채 100% 이하(+1.0점), 메이저 매도(-2.0점), 부채 250% 초과(-1.0점), 2년 연속 영업적자(-2.0점)");
            card1.AddItem("③", Color.FromArgb(200, 130, 255), "뉴스·공시 점수 (25%): ", Color.FromArgb(200, 130, 255), "DART 전자공시 및 Gemini AI 팩트 검증. 실적 서프라이즈/장기계약(10점) ~ 중립(5점) ~ 유증/CB/제재 악재(1~3점)");
            card1.AddItem("④", Color.FromArgb(100, 200, 255), "손익비 점수 (20%): ", Color.FromArgb(100, 200, 255), "20일 최저가(지지선)까지의 거리. -3% 이내 최상(10점) ~ -4.5%(8~9점) ~ -11% 이상 위험(1~3점)");
            card1.AddFullLine("  ※ 종합 등급: [A 등급 (초록)] 8~10점 | [B 등급 (노랑)] 6~7점 | [C 등급 (주황)] 4~5점 | [F 등급 (빨강)] 1~3점", Color.FromArgb(240, 200, 80));
            card1.FinishLayout();

            // [카드 2] 수급(외인·기관) 5대 지표 및 용어 해설
            var card2 = new HelpCardPanel(
                "■ [2. 수급(외인·기관) 5대 지표 및 용어 해설] (신호등 체계)",
                Color.FromArgb(0, 240, 210), Color.FromArgb(16, 32, 38), Color.FromArgb(0, 190, 170), cardWidth);
            card2.AddItem("●", Color.FromArgb(0, 230, 118), "메이저 매수: ", Color.FromArgb(0, 230, 118), "당일 외인과 기관이 동시에 순매수 유입 중이거나, 최근 3일 중 2일 이상 동시 매집한 최고 수급 호재 (+2점)");
            card2.AddItem("●", Color.FromArgb(100, 200, 255), "외인매수 / 기관매수: ", Color.FromArgb(100, 200, 255), "외국인 또는 기관 한쪽 주체가 주도하여 순매수 유입 중인 양호한 수급 (+1점, 연속매수 포함)");
            card2.AddItem("●", Color.FromArgb(240, 200, 80), "수급공방: ", Color.FromArgb(240, 200, 80), "외인과 기관의 매수/매도가 팽팽하게 맞서거나 뚜렷한 주체가 없는 방향성 탐색 중립 상태 (0점)");
            card2.AddItem("●", Color.FromArgb(255, 160, 60), "개미홀로매수: ", Color.FromArgb(255, 160, 60), "외인과 기관은 모두 팔고 개인만 홀로 매수 중인 상태 (상투 물림 or 역배열 리스크 경보, -1점)");
            card2.AddItem("●", Color.FromArgb(255, 75, 75), "메이저 매도: ", Color.FromArgb(255, 75, 75), "외국인과 기관이 동시에 대량 투매/이탈 중인 최악의 수급 악재 (-2점)");
            card2.FinishLayout();

            // [카드 3] 지뢰검증 4단계 뱃지 해설
            var card3 = new HelpCardPanel(
                "■ [3. 지뢰검증 4단계 뱃지 해설] (재무 및 공시 리스크)",
                Color.FromArgb(200, 130, 255), Color.FromArgb(24, 20, 38), Color.FromArgb(160, 90, 230), cardWidth);
            card3.AddItem("●", Color.FromArgb(0, 230, 118), "우수: ", Color.FromArgb(0, 230, 118), "자사주 취득/소각, 배당 확대 등 주주환원 친화 공시 발생 종목");
            card3.AddItem("●", Color.FromArgb(0, 230, 118), "안전: ", Color.FromArgb(0, 230, 118), "부채비율 250% 미만 건전 + 최근 CB/BW/유상증자/적자 등 악재 지뢰 0건 기본 클린 종목");
            card3.AddItem("●", Color.FromArgb(255, 170, 50), "주의: ", Color.FromArgb(255, 170, 50), "부채비율 250% 이상 과다 (재무 건전성 및 이자 부담 점검 필요)");
            card3.AddItem("●", Color.FromArgb(255, 75, 75), "위험: ", Color.FromArgb(255, 75, 75), "3년 연속 영업손실, 전환사채(CB)/신주인수권(BW) 발행, 대규모 유상증자, 감사의견거절, 횡령배임 등 치명적 지뢰 발생");
            card3.AddFullLine("  ※ '위험' 지뢰 탐지 시 종합 점수 1점 강제 처리 및 신규 매수 금지 발동!", Color.FromArgb(255, 110, 110), true);
            card3.FinishLayout();

            // [카드 4] 최종 액션 6대 실전 매매 지침
            var card4 = new HelpCardPanel(
                "■ [4. 최종 액션 6대 실전 매매 지침] (AI 참모 결론)",
                Color.FromArgb(0, 230, 130), Color.FromArgb(16, 30, 24), Color.FromArgb(0, 180, 100), cardWidth);
            card4.AddItem("●", Color.FromArgb(0, 230, 118), "매수추천: ", Color.FromArgb(0, 230, 118), "20일 눌림목 안착 or 과매도 반등 지지선 매수/추매 적극 권고 (보유자/미보유자 공통)");
            card4.AddItem("●", Color.FromArgb(0, 230, 118), "상승추세: ", Color.FromArgb(0, 230, 118), "종합 우량주 주가 상승 랠리 구간 (수익 극대화 홀딩 유지, 추가 추매 자제)");
            card4.AddItem("●", Color.FromArgb(70, 190, 255), "과열주의: ", Color.FromArgb(70, 190, 255), "14일 RSI 68+ 단기 급등 (보유자는 30~50% 분할 익절, 미보유자는 추격 매수 금지)");
            card4.AddItem("●", Color.FromArgb(240, 200, 80), "관망: ", Color.FromArgb(240, 200, 80), "B등급 중립 지지선 공방 중 (방향성 안착 확인 전까지 섣부른 매매 자제 및 관망 대기)");
            card4.AddItem("●", Color.FromArgb(255, 160, 60), "급락주의: ", Color.FromArgb(255, 160, 60), "당일 -4.0% 초과 급락 발생 (떨어지는 칼날 성급한 물타기 절대 금지)");
            card4.AddItem("●", Color.FromArgb(255, 75, 75), "비중축소: ", Color.FromArgb(255, 75, 75), "[보유자 전용] C/F등급 또는 지뢰 종목 (반등 시 40~50% 손절 및 비중 축소 탈출)");
            card4.AddItem("●", Color.FromArgb(255, 75, 75), "매수금지: ", Color.FromArgb(255, 75, 75), "[미보유자 전용] 리스크/지뢰 종목 신규 진입 절대 금지");
            card4.FinishLayout();

            // [카드 5] 주요 보조지표 색상 변화 기준
            var card5 = new HelpCardPanel(
                "■ [5. 주요 보조지표 색상 변화 기준] (한눈에 보는 직관성)",
                Color.FromArgb(255, 205, 70), Color.FromArgb(32, 26, 18), Color.FromArgb(210, 160, 40), cardWidth);
            card5.AddFullLine("• 14일 RSI (상대강도지수)", Color.FromArgb(170, 185, 205), true);
            card5.AddItem("  ●", Color.FromArgb(255, 80, 80), "70 이상: ", Color.FromArgb(255, 80, 80), "단기 과열 슈팅 구간 (상투 물림 주의)");
            card5.AddItem("  ●", Color.FromArgb(0, 230, 118), "30~70: ", Color.FromArgb(0, 230, 118), "정상 추세 유효 구간");
            card5.AddItem("  ●", Color.FromArgb(80, 180, 255), "30 이하: ", Color.FromArgb(80, 180, 255), "과매도 바닥 침체권 (역발상 반등 기회)");
            card5.AddFullLine("• 5일 거래량 (최근 5일 평균 대비 비율)", Color.FromArgb(170, 185, 205), true);
            card5.AddItem("  ●", Color.FromArgb(0, 230, 200), "140% 이상: ", Color.FromArgb(0, 230, 200), "최근 5일 평균 대비 거래량 급증 / 수급 유입");
            card5.AddItem("  ●", Color.FromArgb(200, 215, 235), "140% 미만: ", Color.FromArgb(200, 215, 235), "통상적인 평이한 거래량");
            card5.AddFullLine("• 종합점수 (10점 만점)", Color.FromArgb(170, 185, 205), true);
            card5.AddItem("  ●", Color.FromArgb(255, 215, 0), "8~10점: ", Color.FromArgb(255, 215, 0), "차트·수급·재무 완성된 최우량 S/A급 주도주");
            card5.AddItem("  ●", Color.FromArgb(235, 240, 255), "4~7점: ", Color.FromArgb(235, 240, 255), "일반적인 B/C급 평이한 종목");
            card5.AddItem("  ●", Color.FromArgb(255, 110, 110), "1~3점: ", Color.FromArgb(255, 110, 110), "결함/지뢰 많은 부실 위험 F급 종목");
            card5.FinishLayout();

            _flpBody.Controls.Add(card1);
            _flpBody.Controls.Add(card2);
            _flpBody.Controls.Add(card3);
            _flpBody.Controls.Add(card4);
            _flpBody.Controls.Add(card5);
        }
    }

    /// <summary>
    /// 단일 도움말 카드 GDI+ 렌더링 패널 (ClearType 고화질 + 정확한 RGB 컬러 보장)
    /// </summary>
    public class HelpCardPanel : Panel
    {
        private class HelpLineItem
        {
            public string Bullet { get; set; } = "";
            public Color BulletColor { get; set; } = Color.White;
            public string Badge { get; set; } = "";
            public Color BadgeColor { get; set; } = Color.White;
            public string Description { get; set; } = "";
            public Color DescColor { get; set; } = Color.FromArgb(220, 230, 245);
            public bool IsFullLine { get; set; } = false;
            public bool IsBold { get; set; } = false;

            // 계산된 레이아웃 좌표
            public int Y { get; set; }
            public int Height { get; set; }
            public int BulletWidth { get; set; }
            public int BadgeWidth { get; set; }
            public Rectangle DescRect { get; set; }
        }

        private static readonly Font _fontTitle = new Font("Malgun Gothic", 10.2f, FontStyle.Bold);
        private static readonly Font _fontBold = new Font("Segoe UI", 9.2f, FontStyle.Bold);
        private static readonly Font _fontRegular = new Font("Segoe UI", 9.2f);
        private static readonly Font _fontBullet = new Font("Segoe UI", 10f, FontStyle.Bold);

        private readonly string _title;
        private readonly Color _titleColor;
        private readonly Color _bgColor;
        private readonly Color _borderColor;
        private readonly List<HelpLineItem> _items = new List<HelpLineItem>();

        private SolidBrush _brushBg;
        private Pen _penBorder;
        private Pen _penSep;

        public HelpCardPanel(string title, Color titleColor, Color bgColor, Color borderColor, int width)
        {
            _title = title;
            _titleColor = titleColor;
            _bgColor = bgColor;
            _borderColor = borderColor;

            Width = width;
            BackColor = bgColor;
            Margin = new Padding(0, 0, 0, 14);
            DoubleBuffered = true;

            _brushBg = new SolidBrush(_bgColor);
            _penBorder = new Pen(_borderColor, 1.2f);
            _penSep = new Pen(Color.FromArgb(40, _titleColor.R, _titleColor.G, _titleColor.B), 1.0f);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _brushBg?.Dispose();
                _penBorder?.Dispose();
                _penSep?.Dispose();
            }
            base.Dispose(disposing);
        }

        public void AddItem(string bullet, Color bulletColor, string badge, Color badgeColor, string description)
        {
            _items.Add(new HelpLineItem
            {
                Bullet = bullet,
                BulletColor = bulletColor,
                Badge = badge,
                BadgeColor = badgeColor,
                Description = description,
                DescColor = Color.FromArgb(220, 230, 245),
                IsFullLine = false
            });
        }

        public void AddFullLine(string text, Color color, bool isBold = false)
        {
            _items.Add(new HelpLineItem
            {
                Description = text,
                DescColor = color,
                IsFullLine = true,
                IsBold = isBold
            });
        }

        public void FinishLayout()
        {
            // 화면 픽셀 계산 및 패널 전체 높이 결정
            int currentY = 38; // 타이틀 + 상단 여백
            const int leftPad = 16;
            const int rightPad = 16;
            int availWidth = Width - leftPad - rightPad;

            using (var g = CreateGraphics())
            {
                foreach (var item in _items)
                {
                    item.Y = currentY;

                    if (item.IsFullLine)
                    {
                        var font = item.IsBold ? _fontBold : _fontRegular;
                        var sz = TextRenderer.MeasureText(g, item.Description, font, new Size(availWidth, 0),
                            TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
                        int lineH = Math.Max(20, sz.Height);
                        item.Height = lineH + 5;
                        item.DescRect = new Rectangle(leftPad, currentY, availWidth, lineH);
                        currentY += item.Height;
                    }
                    else
                    {
                        // 1. 불릿 너비 측정
                        var szBullet = TextRenderer.MeasureText(g, item.Bullet, _fontBullet, Size.Empty, TextFormatFlags.NoPadding);
                        item.BulletWidth = szBullet.Width + 6;

                        // 2. 뱃지 너비 측정
                        var szBadge = TextRenderer.MeasureText(g, item.Badge, _fontBold, Size.Empty, TextFormatFlags.NoPadding);
                        item.BadgeWidth = szBadge.Width + 4;

                        // 3. 설명 가용 너비 및 줄바꿈 영역 계산
                        int descStartX = leftPad + item.BulletWidth + item.BadgeWidth;
                        int descAvailWidth = availWidth - (item.BulletWidth + item.BadgeWidth);

                        var szDesc = TextRenderer.MeasureText(g, item.Description, _fontRegular, new Size(descAvailWidth, 0),
                            TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);

                        int lineH = Math.Max(22, szDesc.Height);
                        item.Height = lineH + 4;
                        item.DescRect = new Rectangle(descStartX, currentY, descAvailWidth, lineH);
                        currentY += item.Height;
                    }
                }
            }

            Height = currentY + 12; // 하단 여백 포함
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // 1. 배경 채우기
            g.FillRectangle(_brushBg, ClientRectangle);

            // 2. 외곽 테두리
            g.DrawRectangle(_penBorder, 0, 0, Width - 1, Height - 1);

            // 3. 카드 타이틀
            TextRenderer.DrawText(g, _title, _fontTitle, new Point(14, 10), _titleColor, TextFormatFlags.NoPadding);

            // 4. 은은한 타이틀 구분선
            g.DrawLine(_penSep, 14, 30, Width - 14, 30);

            // 5. 각 라인 아이템 렌더링
            const int leftPad = 16;
            foreach (var item in _items)
            {
                if (item.IsFullLine)
                {
                    var font = item.IsBold ? _fontBold : _fontRegular;
                    TextRenderer.DrawText(g, item.Description, font, item.DescRect, item.DescColor,
                        TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
                }
                else
                {
                    int x = leftPad;

                    // 불릿
                    if (!string.IsNullOrEmpty(item.Bullet))
                    {
                        TextRenderer.DrawText(g, item.Bullet, _fontBullet, new Point(x, item.Y - 1), item.BulletColor, TextFormatFlags.NoPadding);
                        x += item.BulletWidth;
                    }

                    // 뱃지
                    if (!string.IsNullOrEmpty(item.Badge))
                    {
                        TextRenderer.DrawText(g, item.Badge, _fontBold, new Point(x, item.Y), item.BadgeColor, TextFormatFlags.NoPadding);
                    }

                    // 설명 본문 (단어 줄바꿈 완벽 지원)
                    if (!string.IsNullOrEmpty(item.Description))
                    {
                        TextRenderer.DrawText(g, item.Description, _fontRegular, item.DescRect, item.DescColor,
                            TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
                    }
                }
            }
        }
    }
}
