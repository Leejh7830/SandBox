using System;
using System.Drawing;
using System.Windows.Forms;

namespace GateHelper.StockAdvisor
{
    /// <summary>
    /// AI 주식 리스크 참모 - 4대 진단 점수(차트/뉴스/손익비/종합) 산출 가이드 다이얼로그
    /// </summary>
    public class StockScoreHelpDialog : Form
    {
        public StockScoreHelpDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "AI 주식 리스크 참모 - 4대 진단 점수 산출 가이드";
            ClientSize = new Size(760, 800);
            MinimumSize = new Size(760, 750);
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
                Text = "4대 퀀트 진단 점수 산출 기준 & 포지션 가이드",
                Font = new Font("Malgun Gothic", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 216, 255),
                AutoSize = true,
                Location = new Point(0, 2)
            };
            var lblSubtitle = new Label
            {
                Text = "차트(30%) · 수급·재무(25%) · 뉴스·공시(25%) · 손익비(20%) 4대 정량 팩트 기반 종합 점수 및 대응 지침",
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
                Location = new Point(610, 5),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(25, 38, 55),
                ForeColor = Color.FromArgb(0, 216, 255),
                Cursor = Cursors.Hand,
                Font = new Font("Malgun Gothic", 9.2f, FontStyle.Bold)
            };
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(0, 180, 230);
            btnClose.Click += (s, e) => Close();
            pnlFooter.Controls.Add(btnClose);

            // 3. 중앙 스크롤 본문 (4대 점수 카드 + 종합 체계 카드)
            var pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(0, 4, 8, 4)
            };

            int curY = 0;

            // 카드 1: 차트 점수 (30%)
            var cardChart = CreateScoreCard(
                "■ [1. 차트 점수] (1~10점 / 가중치 30%)",
                Color.FromArgb(0, 216, 255),
                Color.FromArgb(18, 28, 42),
                Color.FromArgb(0, 160, 220),
                "• 20일 일봉 데이터를 기반으로 이평선 정배열, 거래량 수급, RSI 모멘텀을 분석합니다.\n" +
                "  - 정배열 가산점: 현재가 > 5일선 (+1.5점) 및 5일선 > 20일선 (+1.5점)\n" +
                "  - 거래량 수급 가산점: 직전 5영업일 평균 대비 당일 거래량 130% 이상 실린 양봉 (+1.5점)\n" +
                "  - RSI 안정 가산점: 14일 RSI 45~65 구간의 안정적 상승 탄력 (+1.0점)\n" +
                "  - 역배열 하락 감점: 주가가 5일선/20일선 아래로 무너진 역배열 하락 추세 (-2.5점)\n" +
                "• 점수 판정: [8~10점] 강력 상승 추세 | [5~7점] 횡보/중립 | [1~4점] 역배열 하락 추세"
            );
            cardChart.Location = new Point(0, curY);
            pnlBody.Controls.Add(cardChart);
            curY += cardChart.Height + 10;

            // 카드 2: 수급·재무 점수 (25%)
            var cardSupply = CreateScoreCard(
                "■ [2. 수급·재무 점수] (1~10점 / 가중치 25%)",
                Color.FromArgb(0, 240, 210),
                Color.FromArgb(16, 32, 38),
                Color.FromArgb(0, 190, 170),
                "• 외인/기관 순매수 수급과 부채비율·적자 여부·주주환원 건전성을 결합 검증합니다.\n" +
                "  - 메이저 쌍끌이 매수 가산점: 외인·기관 동시 순매수 유입 (+2.0점)\n" +
                "  - 단독 매수 가산점: 외인 또는 기관 순매수 주도 (+1.0점)\n" +
                "  - 주주 친화 공시 가산점: 자사주 취득/소각 또는 배당 확대 (+2.0점)\n" +
                "  - 부채비율 건전 가산점: 부채비율 100% 이하 초우량 (+1.0점) / 250% 초과 (-1.0점)\n" +
                "  - 쌍끌이 매도/적자 감점: 외인·기관 동시 매도 (-2.0점), 2년 연속 영업적자 (-2.0점)\n" +
                "  ※ 치명적 지뢰(3년 연속 적자, 감사의견거절, 횡령배임 등) 감지 시 점수 1점 강제 처리"
            );
            cardSupply.Location = new Point(0, curY);
            pnlBody.Controls.Add(cardSupply);
            curY += cardSupply.Height + 10;

            // 카드 3: 뉴스·공시 점수 (25%)
            var cardNews = CreateScoreCard(
                "■ [3. 뉴스·공시 점수] (1~10점 / 가중치 25%)",
                Color.FromArgb(200, 130, 255),
                Color.FromArgb(24, 20, 38),
                Color.FromArgb(160, 90, 230),
                "• DART 실시간 전자공시 및 Gemini AI(1.5 Pro) 기반 글로벌 팩트 검증을 수행합니다.\n" +
                "  - 10점 (초대형 호재): 대규모 실적 서프라이즈, 수출규제 해제, 대형 장기 공급 계약\n" +
                "  - 7~8점 (우호 환경): 실적 개선 모멘텀, 공시 호재, 업황 정책 수혜주 부각\n" +
                "  - 5점 (중립 기본값): 뚜렷한 돌발 호악재가 없는 정상적인 시장 흐름\n" +
                "  - 1~3점 (치명적 악재): 미국 수출통제 제재 직격탄, 대규모 어닝 쇼크, 유상증자/CB발행 등\n" +
                "• 원칙: 포털의 자극적인 찌라시/소문을 배제하고 공식 DART 공시와 신뢰도 높은 팩트만 반영"
            );
            cardNews.Location = new Point(0, curY);
            pnlBody.Controls.Add(cardNews);
            curY += cardNews.Height + 10;

            // 카드 4: 손익비 점수 (20%)
            var cardRisk = CreateScoreCard(
                "■ [4. 손익비 점수] (1~10점 / 가중치 20%)",
                Color.FromArgb(0, 230, 130),
                Color.FromArgb(16, 30, 24),
                Color.FromArgb(0, 180, 100),
                "• 최근 20일 최저가(1차 지지선/손절선)까지의 거리(하방 위험률 %)를 정량 측정합니다.\n" +
                "  - 10점 (최상): 지지선까지 하방 위험률 -3.0% 이내 (손절폭이 극도로 좁아 손익비 최상)\n" +
                "  - 8~9점 (우수): 하방 위험률 -4.5% 이내 (지지선 근접, 눌림목 최적 매수 타점)\n" +
                "  - 6~7점 (양호): 하방 위험률 -6.0% 이내 (일반적인 손익비 양호 구간)\n" +
                "  - 4~5점 (보통): 하방 위험률 -8.0% 이내 (손절폭이 다소 넓으므로 분할 접근 필요)\n" +
                "  - 1~3점 (위험): 하방 위험률 -11.0% 이상 (지지선이 너무 멀어 하락 시 손실 위험 극대화)\n" +
                "• 원칙: 손익비 점수가 높을수록 손절 시 손실은 작고 반등 시 이익이 큰 안전한 구간입니다."
            );
            cardRisk.Location = new Point(0, curY);
            pnlBody.Controls.Add(cardRisk);
            curY += cardRisk.Height + 10;

            // 카드 5: 종합 점수 & AI 4대 등급 체계 (100%)
            var cardTotal = CreateScoreCard(
                "■ [5. 종합 점수 & AI 4대 등급 체계] (A, B, C, F)",
                Color.FromArgb(255, 205, 70),
                Color.FromArgb(32, 26, 18),
                Color.FromArgb(210, 160, 40),
                "• 최종 산출 공식: [차트 × 30%] + [수급·재무 × 25%] + [뉴스 × 25%] + [손익비 × 20%]\n\n" +
                "  [A 등급 (안전/초록)] : 8, 9, 10점 (최우수 / 적극 보유 및 눌림목 분할 진입)\n" +
                "  [B 등급 (주의/노랑)] : 6, 7점 (양호 / 홀딩 관망 및 지지선 확인)\n" +
                "  [C 등급 (경고/주황)] : 3, 4, 5점 (모멘텀 약화 / 반등 시 비중 축소 고려)\n" +
                "  [F 등급 (위험/빨강)] : 0, 1, 2점 (위험 극대화 / 기계적 손절 및 신규 매수 금지)\n\n" +
                "• 참모 행동(Action) 분리 원칙:\n" +
                "  - 보유자: 점수 및 손실 구간에 따라 [비중 축소] · [분할 익절] · [보유 관망] · [추매 유효]\n" +
                "  - 미보유자: 시장 가치 점수에 따라 [매수 금지] · [추격매수 금지] · [분할 매수 진입] · [관망 대기]\n" +
                "  ※ [과열 안전벨트]: 종합점수가 높아도 14일 RSI가 70 이상이면 '과열 주의: 추격매수 금지' 강제 발동!"
            );
            cardTotal.Location = new Point(0, curY);
            pnlBody.Controls.Add(cardTotal);

            pnlMain.Controls.Add(pnlBody);
            pnlMain.Controls.Add(pnlHeader);
            pnlMain.Controls.Add(pnlFooter);
            Controls.Add(pnlMain);
        }

        private Panel CreateScoreCard(string title, Color titleColor, Color bgColor, Color borderColor, string content)
        {
            var pnl = new Panel
            {
                Width = 690,
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
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(220, 230, 245),
                Dock = DockStyle.Fill,
                AutoSize = false
            };

            pnl.Controls.Add(lblContent);
            pnl.Controls.Add(lblTitle);

            // 줄 수에 따른 패널 높이 자동 계산
            int lineCount = content.Split('\n').Length;
            pnl.Height = 32 + (lineCount * 19) + 10;

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