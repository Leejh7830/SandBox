using System;
using System.Drawing;
using System.Windows.Forms;

namespace GateHelper.StockAdvisor
{
    /// <summary>
    /// AI 주식 리스크 참모 - 종합 퀀트 점수 산출 기준 및 지표 사전 다이얼로그
    /// </summary>
    public class StockScoreHelpDialog : Form
    {
        public StockScoreHelpDialog()
        {
            InitializeComponent();
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

            // 카드 1: 4대 퀀트 진단 점수 산출 공식 (10점 만점)
            var cardScore = CreateScoreCard(
                "■ [1. 4대 퀀트 진단 점수 산출 공식] (10점 만점 / 4대 정량 팩터)",
                Color.FromArgb(0, 216, 255),
                Color.FromArgb(18, 28, 42),
                Color.FromArgb(0, 160, 220),
                "• 최종 산출 공식: [차트 × 30%] + [수급·재무 × 25%] + [뉴스·공시 × 25%] + [손익비 × 20%]\n" +
                "  ① 차트 점수 (30%): 20일 일봉 기준 이평선 정배열(+3.0점), 거래량 실린 양봉(+1.5점), RSI 45~65 안정 탄력(+1.0점), 역배열 하락 추세 감점(-2.5점)\n" +
                "  ② 수급·재무 점수 (25%): 메이저 매수(+2.0점), 외인/기관 순매수(+1.0점), 주주환원 친화(+2.0점), 부채 100% 이하(+1.0점), 메이저 매도(-2.0점), 부채 250% 초과(-1.0점), 2년 연속 영업적자(-2.0점)\n" +
                "  ③ 뉴스·공시 점수 (25%): DART 전자공시 및 Gemini AI 팩트 검증. 실적 서프라이즈/장기계약(10점) ~ 중립(5점) ~ 유증/CB/제재 악재(1~3점)\n" +
                "  ④ 손익비 점수 (20%): 20일 최저가(지지선)까지의 거리. -3% 이내 최상(10점) ~ -4.5%(8~9점) ~ -11% 이상 위험(1~3점)\n" +
                "  ※ 종합 등급: [A 등급 (초록)] 8~10점 (최우량 주도주) | [B 등급 (노랑)] 6~7점 (양호) | [C 등급 (주황)] 4~5점 (주의) | [F 등급 (빨강)] 1~3점 (위험)"
            );
            cardScore.Location = new Point(0, curY);
            pnlBody.Controls.Add(cardScore);
            curY += cardScore.Height + 10;

            // 카드 2: 수급(외인·기관) 5대 지표 및 용어 해설
            var cardSupply = CreateScoreCard(
                "■ [2. 수급(외인·기관) 5대 지표 및 용어 해설] (신호등 체계)",
                Color.FromArgb(0, 240, 210),
                Color.FromArgb(16, 32, 38),
                Color.FromArgb(0, 190, 170),
                "• 🟢 메이저 매수: 당일 외인과 기관이 동시에 순매수 유입 중이거나, 최근 3일 중 2일 이상 동시 매집한 최고 수급 호재 (+2점)\n" +
                "• 🔵 외인매수 / 기관매수 (연속매수 포함): 외국인 또는 기관 한쪽 주체가 주도하여 순매수 유입 중인 양호한 수급 (+1점)\n" +
                "• 🟡 수급공방: 외인과 기관의 매수/매도가 팽팽하게 맞서거나 뚜렷한 주체가 없는 방향성 탐색 중립 상태 (0점)\n" +
                "• 🟠 개미홀로매수: 외인과 기관은 모두 팔고 개인만 홀로 매수 중인 상태 (상투 물림 or 역배열 리스크 경보, -1점)\n" +
                "• 🔴 메이저 매도: 외국인과 기관이 동시에 대량 투매/이탈 중인 최악의 수급 악재 (-2점)"
            );
            cardSupply.Location = new Point(0, curY);
            pnlBody.Controls.Add(cardSupply);
            curY += cardSupply.Height + 10;

            // 카드 3: 지뢰검증 4단계 뱃지 해설
            var cardLandmine = CreateScoreCard(
                "■ [3. 지뢰검증 4단계 뱃지 해설] (재무 및 공시 리스크)",
                Color.FromArgb(200, 130, 255),
                Color.FromArgb(24, 20, 38),
                Color.FromArgb(160, 90, 230),
                "• 🟢 우수: 자사주 취득/소각, 배당 확대 등 주주환원 친화 공시 발생 종목\n" +
                "• 🟢 안전: 부채비율 250% 미만 건전 + 최근 CB/BW/유상증자/적자 등 악재 지뢰 0건 기본 클린 종목\n" +
                "• 🟠 주의: 부채비율 250% 이상 과다 (재무 건전성 및 이자 부담 점검 필요)\n" +
                "• 🔴 위험: 3년 연속 영업손실, 전환사채(CB)/신주인수권(BW) 발행, 대규모 유상증자, 감사의견거절, 횡령배임 등 치명적 지뢰 발생\n" +
                "  ※ '위험' 지뢰 탐지 시 종합 점수 1점 강제 처리 및 신규 매수 금지 발동!"
            );
            cardLandmine.Location = new Point(0, curY);
            pnlBody.Controls.Add(cardLandmine);
            curY += cardLandmine.Height + 10;

            // 카드 4: 최종 액션 6대 실전 매매 지침
            var cardAction = CreateScoreCard(
                "■ [4. 최종 액션 6대 실전 매매 지침] (AI 참모 결론)",
                Color.FromArgb(0, 230, 130),
                Color.FromArgb(16, 30, 24),
                Color.FromArgb(0, 180, 100),
                "• 🟢 매수추천: 20일 눌림목 안착 or 과매도 반등 지지선 매수/추매 적극 권고 (보유자/미보유자 공통)\n" +
                "• 🟢 상승추세: 종합 우량주 주가 상승 랠리 구간 (수익 극대화 홀딩 유지, 추가 추매 자제)\n" +
                "• 🔵 과열주의: 14일 RSI 68+ 단기 급등 (보유자는 30~50% 분할 익절, 미보유자는 추격 매수 금지)\n" +
                "• 🟡 관망: B등급 중립 지지선 공방 중 (방향성 안착 확인 전까지 섣부른 매매 자제 및 관망 대기)\n" +
                "• 🟠 급락주의: 당일 -4.0% 초과 급락 발생 (떨어지는 칼날 성급한 물타기 절대 금지)\n" +
                "• 🔴 비중축소: [보유자 전용] C/F등급 또는 지뢰 종목 (반등 시 40~50% 손절 및 비중 축소 탈출)\n" +
                "• 🔴 매수금지: [미보유자 전용] 리스크/지뢰 종목 신규 진입 절대 금지"
            );
            cardAction.Location = new Point(0, curY);
            pnlBody.Controls.Add(cardAction);
            curY += cardAction.Height + 10;

            // 카드 5: 주요 보조지표 색상 변화 기준
            var cardIndicators = CreateScoreCard(
                "■ [5. 주요 보조지표 색상 변화 기준] (한눈에 보는 직관성)",
                Color.FromArgb(255, 205, 70),
                Color.FromArgb(32, 26, 18),
                Color.FromArgb(210, 160, 40),
                "• 14일 RSI: 🔴 빨간색(70 이상 단기 과열 슈팅) | 🟢 연초록(30~70 정상 추세) | 🔵 하늘색(30 이하 과매도 바닥 반등)\n" +
                "• 5일 거래량: 🟢 청록 네온(140% 이상 최근 5일 평균 대비 거래량 급증/수급 유입) | ⚪ 연회색(140% 미만 평이한 거래량)\n" +
                "• 종합점수: 🟡 황금색(8~10점 최우량 S/A급) | ⚪ 밝은 흰색(4~7점 일반 B/C급) | 🔴 빨간색(1~3점 부실/위험 F급)"
            );
            cardIndicators.Location = new Point(0, curY);
            pnlBody.Controls.Add(cardIndicators);

            pnlMain.Controls.Add(pnlBody);
            pnlMain.Controls.Add(pnlHeader);
            pnlMain.Controls.Add(pnlFooter);
            Controls.Add(pnlMain);
        }

        private Panel CreateScoreCard(string title, Color titleColor, Color bgColor, Color borderColor, string content)
        {
            var pnl = new Panel
            {
                Width = 730,
                BackColor = bgColor,
                Padding = new Padding(14, 10, 14, 10)
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Malgun Gothic", 10.2f, FontStyle.Bold),
                ForeColor = titleColor,
                Dock = DockStyle.Top,
                Height = 24
            };

            var lblContent = new Label
            {
                Text = content,
                Font = new Font("Segoe UI", 9.2f),
                ForeColor = Color.FromArgb(220, 230, 245),
                Dock = DockStyle.Fill,
                AutoSize = false
            };

            pnl.Controls.Add(lblContent);
            pnl.Controls.Add(lblTitle);

            // 줄 수에 따른 패널 높이 자동 계산 (가독성을 위한 넉넉한 줄간격 확보)
            int lineCount = content.Split('\n').Length;
            pnl.Height = 34 + (lineCount * 22) + 12;

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
    }
}
