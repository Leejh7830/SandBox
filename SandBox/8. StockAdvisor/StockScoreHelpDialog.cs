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
            ClientSize = new Size(750, 770);
            MinimumSize = new Size(750, 770);
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
                Text = "4대 진단 점수 산출 기준 & 포지션 가이드",
                Font = new Font("Malgun Gothic", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 216, 255),
                AutoSize = true,
                Location = new Point(0, 2)
            };
            var lblSubtitle = new Label
            {
                Text = "차트(40%) · 뉴스(30%) · 손익비(30%) 3대 정량 팩트 기반 종합 점수 및 냉철한 대응 지침",
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
                Location = new Point(600, 5),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(25, 38, 55),
                ForeColor = Color.FromArgb(0, 216, 255),
                Cursor = Cursors.Hand,
                Font = new Font("Malgun Gothic", 9.2f, FontStyle.Bold)
            };
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(0, 180, 230);
            btnClose.Click += (s, e) => Close();
            pnlFooter.Controls.Add(btnClose);

            // 3. 중앙 스크롤 본문 (4대 점수 카드)
            var pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(0, 4, 8, 4)
            };

            int curY = 0;

            // 카드 1: 차트 점수 (40%)
            var cardChart = CreateScoreCard(
                "■ [1. 차트 점수] (1~10점 / 가중치 40%)",
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

            // 카드 2: 뉴스/정책 점수 (30%)
            var cardNews = CreateScoreCard(
                "■ [2. 뉴스/정책 점수] (1~10점 / 가중치 30%)",
                Color.FromArgb(200, 130, 255),
                Color.FromArgb(24, 20, 38),
                Color.FromArgb(160, 90, 230),
                "• Gemini AI(1.5 Pro/Flash)가 글로벌 매크로, 미국 BIS 수출통제, 실적 공시를 팩트 검증합니다.\n" +
                "  - 10점 (최대 호재): 대규모 실적 서프라이즈, 미 규제 전격 해제, 대형 장기 공급 계약\n" +
                "  - 7~8점 (우호 환경): 실적 개선 모멘텀, 업황 개선 기대감, 정책 수혜주 부각\n" +
                "  - 5점 (중립): 뚜렷한 호악재가 없는 일반적 시장 흐름\n" +
                "  - 1~3점 (치명적 악재): 미국 수출통제 제재 직격탄, 대규모 어닝 쇼크, 유상증자 등\n" +
                "• 찌라시/소문 필터링: 단순 포털 뉴스량이 아닌 정부 공시 및 출처 신뢰도(Reliability)를 엄격히 검증"
            );
            cardNews.Location = new Point(0, curY);
            pnlBody.Controls.Add(cardNews);
            curY += cardNews.Height + 10;

            // 카드 3: 손익비/안전 점수 (30%)
            var cardRisk = CreateScoreCard(
                "■ [3. 손익비 점수] (1~10점 / 가중치 30%)",
                Color.FromArgb(0, 230, 130),
                Color.FromArgb(16, 30, 24),
                Color.FromArgb(0, 180, 100),
                "• 최근 20일 최저가(1차 지지선/손절선)까지의 거리(하방 위험률 %)를 측정합니다.\n" +
                "  - 10점 (최상): 지지선까지 하방 위험률 -3.0% 이내 (손절폭이 극도로 좁아 손익비 최상)\n" +
                "  - 9점 (우수): 하방 위험률 -4.5% 이내 (지지선 근접, 눌림목 최적 매수 자리)\n" +
                "  - 7점 (양호): 하방 위험률 -6.0% 이내 (일반적인 손익비 양호 구간)\n" +
                "  - 5점 (보통): 하방 위험률 -8.0% 이내 (손절폭이 다소 넓으므로 분할 접근 필요)\n" +
                "  - 2~3점 (위험): 하방 위험률 -11.0% 이상 (지지선이 너무 멀어 손실 위험 극대화)\n" +
                "• 원칙: 손익비 점수가 높을수록 손절 시 손실은 작고 반등 시 이익이 큰 안전한 구간입니다."
            );
            cardRisk.Location = new Point(0, curY);
            pnlBody.Controls.Add(cardRisk);
            curY += cardRisk.Height + 10;

            // 카드 4: 종합 점수 & 행동 지침 (100%)
            var cardTotal = CreateScoreCard(
                "■ [4. 종합 점수 & AI 4대 등급 체계] (A, B, C, F)",
                Color.FromArgb(255, 205, 70),
                Color.FromArgb(32, 26, 18),
                Color.FromArgb(210, 160, 40),
                "• 최종 산출 공식: [차트 점수 × 40%] + [뉴스 점수 × 30%] + [손익비 점수 × 30%]\n\n" +
                "  [A 등급 (안전/초록)] : 8, 9, 10점 (최우수 / 적극 보유 및 분할 진입)\n" +
                "     - 추세·뉴스·손익비 삼박자 충족. 주도주 눌림목에서 분할 매수 및 수익 극대화 포지션\n" +
                "  [B 등급 (주의/노랑)] : 6, 7점 (양호 / 홀딩 관망 및 지지선 확인)\n" +
                "     - 양호한 상태이나 지지선 이격이 존재하거나 방향성 탐색 중. 추격매수 금지 및 눌림목 대기\n" +
                "  [C 등급 (경고/주황)] : 3, 4, 5점 (모멘텀 약화 / 반등 시 비중 축소 고려)\n" +
                "     - 추세 약세 전환 또는 하방 리스크 누적. 추가 매수 금지 및 반등 시 비중 조절\n" +
                "  [F 등급 (위험/빨강)] : 0, 1, 2점 (위험 극대화 / 기계적 손절 및 긴급 탈출 권고)\n" +
                "     - 역배열 추세 이탈, 돌발 악재 노출, 하방 리스크 과다. 기계적 손절 적극 실행\n\n" +
                "  ※ [과열 안전벨트]: 종합점수가 높아도 14일 RSI가 70 이상이면 '과열 주의: 신규 매수 금지' 강제 발동!"
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