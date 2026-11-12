using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GateHelper.StockAdvisor
{
    /// <summary>
    /// AI 주식 리스크 참모 - 종합 퀀트 점수 산출 기준 및 지표 사전 다이얼로그
    /// (실제 신호등 컬러 RichTextBox 렌더링 지원)
    /// </summary>
    public class StockScoreHelpDialog : Form
    {
        [DllImport("user32.dll")]
        private static extern bool HideCaret(IntPtr hWnd);

        private readonly Font _fontTitle = new Font("Malgun Gothic", 10.2f, FontStyle.Bold);
        private readonly Font _fontBold = new Font("Segoe UI", 9.2f, FontStyle.Bold);
        private readonly Font _fontRegular = new Font("Segoe UI", 9.2f);
        private readonly Font _fontBullet = new Font("Segoe UI", 10.5f, FontStyle.Bold);
        private readonly Color _colorDesc = Color.FromArgb(220, 230, 245);

        public StockScoreHelpDialog()
        {
            InitializeComponent();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fontTitle?.Dispose();
                _fontBold?.Dispose();
                _fontRegular?.Dispose();
                _fontBullet?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            Text = "AI 주식 리스크 참모 - 종합 퀀트 점수 산출 기준 및 지표 사전";
            ClientSize = new Size(800, 850);
            MinimumSize = new Size(800, 750);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(16, 20, 28);
            ForeColor = Color.FromArgb(230, 235, 245);
            Font = new Font("Malgun Gothic", 9.2f);

            var pnlMain = new Panel { Dock = DockStyle.Fill, Padding = new Padding(18) };

            // 1. 헤더 배너
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
                Location = new Point(650, 5),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(25, 38, 55),
                ForeColor = Color.FromArgb(0, 216, 255),
                Cursor = Cursors.Hand,
                Font = new Font("Malgun Gothic", 9.2f, FontStyle.Bold)
            };
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(0, 180, 230);
            btnClose.Click += (s, e) => Close();
            pnlFooter.Controls.Add(btnClose);

            // 3. 중앙 스크롤 본문 (5대 상세 안내 카드)
            var pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(0, 4, 8, 4)
            };

            int curY = 0;

            // =========================================================================
            // 카드 1: 4대 퀀트 진단 점수 산출 공식 (10점 만점)
            // =========================================================================
            var card1 = CreateCardPanel("■ [1. 4대 퀀트 진단 점수 산출 공식] (10점 만점 / 4대 정량 팩터)",
                Color.FromArgb(0, 216, 255), Color.FromArgb(18, 28, 42), Color.FromArgb(0, 160, 220), 220, out RichTextBox rtb1);
            AppendNormalLine(rtb1, "• 최종 산출 공식: [차트 × 30%] + [수급·재무 × 25%] + [뉴스·공시 × 25%] + [손익비 × 20%]", Color.FromArgb(0, 216, 255), true);
            AppendBadgeLine(rtb1, "  ① ", Color.FromArgb(0, 216, 255), "차트 점수 (30%): ", Color.FromArgb(0, 216, 255), "20일 이평선 정배열(+3.0점), 거래량 실린 양봉(+1.5점), RSI 45~65 안정 탄력(+1.0점), 역배열 하락 추세 감점(-2.5점)");
            AppendBadgeLine(rtb1, "  ② ", Color.FromArgb(0, 230, 118), "수급·재무 점수 (25%): ", Color.FromArgb(0, 230, 118), "메이저 매수(+2.0점), 외인/기관 순매수(+1.0점), 주주환원 친화(+2.0점), 부채 100% 이하(+1.0점), 메이저 매도(-2.0점), 부채 250% 초과(-1.0점), 2년 연속 영업적자(-2.0점)");
            AppendBadgeLine(rtb1, "  ③ ", Color.FromArgb(200, 130, 255), "뉴스·공시 점수 (25%): ", Color.FromArgb(200, 130, 255), "DART 전자공시 및 Gemini AI 팩트 검증. 실적 서프라이즈/장기계약(10점) ~ 중립(5점) ~ 유증/CB/제재 악재(1~3점)");
            AppendBadgeLine(rtb1, "  ④ ", Color.FromArgb(100, 200, 255), "손익비 점수 (20%): ", Color.FromArgb(100, 200, 255), "20일 최저가(지지선)까지의 거리. -3% 이내 최상(10점) ~ -4.5%(8~9점) ~ -11% 이상 위험(1~3점)");
            AppendNormalLine(rtb1, "  ※ 종합 등급: [A 등급 (초록)] 8~10점 | [B 등급 (노랑)] 6~7점 | [C 등급 (주황)] 4~5점 | [F 등급 (빨강)] 1~3점", Color.FromArgb(240, 200, 80));
            card1.Location = new Point(0, curY);
            pnlBody.Controls.Add(card1);
            curY += card1.Height + 12;

            // =========================================================================
            // 카드 2: 수급(외인·기관) 5대 지표 및 용어 해설 (실제 신호등 컬러 지원)
            // =========================================================================
            var card2 = CreateCardPanel("■ [2. 수급(외인·기관) 5대 지표 및 용어 해설] (신호등 체계)",
                Color.FromArgb(0, 240, 210), Color.FromArgb(16, 32, 38), Color.FromArgb(0, 190, 170), 180, out RichTextBox rtb2);
            AppendBadgeLine(rtb2, "  ● ", Color.FromArgb(0, 230, 118), "메이저 매수: ", Color.FromArgb(0, 230, 118), "당일 외인과 기관이 동시에 순매수 유입 중이거나, 최근 3일 중 2일 이상 동시 매집한 최고 수급 호재 (+2점)");
            AppendBadgeLine(rtb2, "  ● ", Color.FromArgb(100, 200, 255), "외인매수 / 기관매수 (연속매수 포함): ", Color.FromArgb(100, 200, 255), "외국인 또는 기관 한쪽 주체가 주도하여 순매수 유입 중인 양호한 수급 (+1점)");
            AppendBadgeLine(rtb2, "  ● ", Color.FromArgb(240, 200, 80), "수급공방: ", Color.FromArgb(240, 200, 80), "외인과 기관의 매수/매도가 팽팽하게 맞서거나 뚜렷한 주체가 없는 방향성 탐색 중립 상태 (0점)");
            AppendBadgeLine(rtb2, "  ● ", Color.FromArgb(255, 160, 60), "개미홀로매수: ", Color.FromArgb(255, 160, 60), "외인과 기관은 모두 팔고 개인만 홀로 매수 중인 상태 (상투 물림 or 역배열 리스크 경보, -1점)");
            AppendBadgeLine(rtb2, "  ● ", Color.FromArgb(255, 75, 75), "메이저 매도: ", Color.FromArgb(255, 75, 75), "외국인과 기관이 동시에 대량 투매/이탈 중인 최악의 수급 악재 (-2점)");
            card2.Location = new Point(0, curY);
            pnlBody.Controls.Add(card2);
            curY += card2.Height + 12;

            // =========================================================================
            // 카드 3: 지뢰검증 4단계 뱃지 해설 (실제 신호등 컬러 지원)
            // =========================================================================
            var card3 = CreateCardPanel("■ [3. 지뢰검증 4단계 뱃지 해설] (재무 및 공시 리스크)",
                Color.FromArgb(200, 130, 255), Color.FromArgb(24, 20, 38), Color.FromArgb(160, 90, 230), 180, out RichTextBox rtb3);
            AppendBadgeLine(rtb3, "  ● ", Color.FromArgb(0, 230, 118), "우수: ", Color.FromArgb(0, 230, 118), "자사주 취득/소각, 배당 확대 등 주주환원 친화 공시 발생 종목");
            AppendBadgeLine(rtb3, "  ● ", Color.FromArgb(0, 230, 118), "안전: ", Color.FromArgb(0, 230, 118), "부채비율 250% 미만 건전 + 최근 CB/BW/유상증자/적자 등 악재 지뢰 0건 기본 클린 종목");
            AppendBadgeLine(rtb3, "  ● ", Color.FromArgb(255, 170, 50), "주의: ", Color.FromArgb(255, 170, 50), "부채비율 250% 이상 과다 (재무 건전성 및 이자 부담 점검 필요)");
            AppendBadgeLine(rtb3, "  ● ", Color.FromArgb(255, 75, 75), "위험: ", Color.FromArgb(255, 75, 75), "3년 연속 영업손실, 전환사채(CB)/신주인수권(BW) 발행, 대규모 유상증자, 감사의견거절, 횡령배임 등 치명적 지뢰 발생");
            AppendNormalLine(rtb3, "  ※ '위험' 지뢰 탐지 시 종합 점수 1점 강제 처리 및 신규 매수 금지 발동!", Color.FromArgb(255, 110, 110), true);
            card3.Location = new Point(0, curY);
            pnlBody.Controls.Add(card3);
            curY += card3.Height + 12;

            // =========================================================================
            // 카드 4: 최종 액션 6대 실전 매매 지침 (실제 신호등 컬러 지원)
            // =========================================================================
            var card4 = CreateCardPanel("■ [4. 최종 액션 6대 실전 매매 지침] (AI 참모 결론)",
                Color.FromArgb(0, 230, 130), Color.FromArgb(16, 30, 24), Color.FromArgb(0, 180, 100), 220, out RichTextBox rtb4);
            AppendBadgeLine(rtb4, "  ● ", Color.FromArgb(0, 230, 118), "매수추천: ", Color.FromArgb(0, 230, 118), "20일 눌림목 안착 or 과매도 반등 지지선 매수/추매 적극 권고 (보유자/미보유자 공통)");
            AppendBadgeLine(rtb4, "  ● ", Color.FromArgb(0, 230, 118), "상승추세: ", Color.FromArgb(0, 230, 118), "종합 우량주 주가 상승 랠리 구간 (수익 극대화 홀딩 유지, 추가 추매 자제)");
            AppendBadgeLine(rtb4, "  ● ", Color.FromArgb(70, 190, 255), "과열주의: ", Color.FromArgb(70, 190, 255), "14일 RSI 68+ 단기 급등 (보유자는 30~50% 분할 익절, 미보유자는 추격 매수 금지)");
            AppendBadgeLine(rtb4, "  ● ", Color.FromArgb(240, 200, 80), "관망: ", Color.FromArgb(240, 200, 80), "B등급 중립 지지선 공방 중 (방향성 안착 확인 전까지 섣부른 매매 자제 및 관망 대기)");
            AppendBadgeLine(rtb4, "  ● ", Color.FromArgb(255, 160, 60), "급락주의: ", Color.FromArgb(255, 160, 60), "당일 -4.0% 초과 급락 발생 (떨어지는 칼날 성급한 물타기 절대 금지)");
            AppendBadgeLine(rtb4, "  ● ", Color.FromArgb(255, 75, 75), "비중축소: ", Color.FromArgb(255, 75, 75), "[보유자 전용] C/F등급 또는 지뢰 종목 (반등 시 40~50% 손절 및 비중 축소 탈출)");
            AppendBadgeLine(rtb4, "  ● ", Color.FromArgb(255, 75, 75), "매수금지: ", Color.FromArgb(255, 75, 75), "[미보유자 전용] 리스크/지뢰 종목 신규 진입 절대 금지");
            card4.Location = new Point(0, curY);
            pnlBody.Controls.Add(card4);
            curY += card4.Height + 12;

            // =========================================================================
            // 카드 5: 주요 보조지표 색상 변화 기준 (실제 컬러 지원)
            // =========================================================================
            var card5 = CreateCardPanel("■ [5. 주요 보조지표 색상 변화 기준] (한눈에 보는 직관성)",
                Color.FromArgb(255, 205, 70), Color.FromArgb(32, 26, 18), Color.FromArgb(210, 160, 40), 280, out RichTextBox rtb5);
            AppendNormalLine(rtb5, "• 14일 RSI (상대강도지수)", Color.FromArgb(170, 185, 205), true);
            AppendBadgeLine(rtb5, "    ● ", Color.FromArgb(255, 80, 80), "빨간색 (70 이상): ", Color.FromArgb(255, 80, 80), "단기 과열 슈팅 구간 (상투 물림 주의)");
            AppendBadgeLine(rtb5, "    ● ", Color.FromArgb(0, 230, 118), "연초록 (30~70): ", Color.FromArgb(0, 230, 118), "정상 추세 유효 구간");
            AppendBadgeLine(rtb5, "    ● ", Color.FromArgb(80, 180, 255), "하늘색 (30 이하): ", Color.FromArgb(80, 180, 255), "과매도 바닥 침체권 (역발상 반등 기회)");
            AppendNormalLine(rtb5, "• 5일 거래량 (최근 5일 평균 대비 비율)", Color.FromArgb(170, 185, 205), true);
            AppendBadgeLine(rtb5, "    ● ", Color.FromArgb(0, 230, 200), "청록 네온 (140% 이상): ", Color.FromArgb(0, 230, 200), "최근 5일 평균 대비 거래량 급증 / 수급 유입");
            AppendBadgeLine(rtb5, "    ● ", Color.FromArgb(200, 215, 235), "연회색 (140% 미만): ", Color.FromArgb(200, 215, 235), "통상적인 평이한 거래량");
            AppendNormalLine(rtb5, "• 종합점수 (10점 만점)", Color.FromArgb(170, 185, 205), true);
            AppendBadgeLine(rtb5, "    ● ", Color.FromArgb(255, 215, 0), "황금색 (8~10점): ", Color.FromArgb(255, 215, 0), "차트·수급·재무 완성된 최우량 S/A급 주도주");
            AppendBadgeLine(rtb5, "    ● ", Color.FromArgb(235, 240, 255), "밝은 흰색 (4~7점): ", Color.FromArgb(235, 240, 255), "일반적인 B/C급 평이한 종목");
            AppendBadgeLine(rtb5, "    ● ", Color.FromArgb(255, 110, 110), "빨간색 (1~3점): ", Color.FromArgb(255, 110, 110), "결함/지뢰 많은 부실 위험 F급 종목");
            card5.Location = new Point(0, curY);
            pnlBody.Controls.Add(card5);

            pnlMain.Controls.Add(pnlBody);
            pnlMain.Controls.Add(pnlHeader);
            pnlMain.Controls.Add(pnlFooter);
            Controls.Add(pnlMain);
        }

        private Panel CreateCardPanel(string title, Color titleColor, Color bgColor, Color borderColor, int height, out RichTextBox rtb)
        {
            var pnl = new Panel
            {
                Width = 730,
                Height = height,
                BackColor = bgColor,
                Padding = new Padding(14, 10, 14, 10)
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = _fontTitle,
                ForeColor = titleColor,
                Dock = DockStyle.Top,
                Height = 26
            };

            var box = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = bgColor,
                ScrollBars = RichTextBoxScrollBars.None,
                Cursor = Cursors.Default
            };

            // 커서 깜빡임 제거
            box.GotFocus += (s, e) => HideCaret(box.Handle);
            box.MouseDown += (s, e) => HideCaret(box.Handle);

            pnl.Controls.Add(box);
            pnl.Controls.Add(lblTitle);

            rtb = box;

            // 커스텀 테두리 페인팅
            pnl.Paint += (s, e) =>
            {
                using (var pen = new Pen(borderColor, 1.2f))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, pnl.Width - 1, pnl.Height - 1);
                }
            };

            return pnl;
        }

        private void AppendBadgeLine(RichTextBox rtb, string bullet, Color bulletColor, string badge, Color badgeColor, string description)
        {
            if (!string.IsNullOrEmpty(bullet))
            {
                rtb.SelectionStart = rtb.TextLength;
                rtb.SelectionLength = 0;
                rtb.SelectionColor = bulletColor;
                rtb.SelectionFont = _fontBullet;
                rtb.AppendText(bullet);
            }

            if (!string.IsNullOrEmpty(badge))
            {
                rtb.SelectionStart = rtb.TextLength;
                rtb.SelectionLength = 0;
                rtb.SelectionColor = badgeColor;
                rtb.SelectionFont = _fontBold;
                rtb.AppendText(badge);
            }

            if (!string.IsNullOrEmpty(description))
            {
                rtb.SelectionStart = rtb.TextLength;
                rtb.SelectionLength = 0;
                rtb.SelectionColor = _colorDesc;
                rtb.SelectionFont = _fontRegular;
                rtb.AppendText(description);
            }

            rtb.AppendText("\n");
        }

        private void AppendNormalLine(RichTextBox rtb, string text, Color color, bool isBold = false)
        {
            rtb.SelectionStart = rtb.TextLength;
            rtb.SelectionLength = 0;
            rtb.SelectionColor = color;
            rtb.SelectionFont = isBold ? _fontBold : _fontRegular;
            rtb.AppendText(text + "\n");
        }
    }
}
