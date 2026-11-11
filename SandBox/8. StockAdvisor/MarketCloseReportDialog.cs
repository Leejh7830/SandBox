using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using GateHelper.StockAdvisor.Models;
using GateHelper.StockAdvisor.Services;

namespace GateHelper.StockAdvisor
{
    /// <summary>
    /// 장마감 후 보유 종목의 당일 가치 변동, 시장 지수 대비 성과,
    /// 외인·기관 수급 결산 및 내일 시초가 대응 체크포인트를 종합 제공하는 결산 다이얼로그
    /// (실현손익 대신 현재 보유 종목의 오늘 하루 팩트 변동 및 수급 분석에 집중)
    /// </summary>
    public class MarketCloseReportDialog : Form
    {
        private readonly List<StockHoldingItem> _heldItems;
        private readonly GlobalMarketSnapshot _macroSnapshot;

        // UI 요소
        private Label _lblHeaderTitle;
        private Label _lblHeaderSummary;
        private RichTextBox _rtbReport;
        private Button _btnCopyClipboard;
        private Button _btnClose;

        public MarketCloseReportDialog(List<StockHoldingItem> items, GlobalMarketSnapshot macroSnapshot)
        {
            // 현재 보유 중인 종목에 한정 (Quantity > 0 및 현재가 유효)
            _heldItems = (items ?? new List<StockHoldingItem>())
                .Where(x => x.Quantity > 0 && x.CurrentPrice > 0)
                .ToList();
            _macroSnapshot = macroSnapshot;

            InitializeCustomComponent();
            GenerateAndRenderReport();
        }

        private void InitializeCustomComponent()
        {
            this.Text = "📊 [장마감 결산] 오늘의 포트폴리오 리뷰 & 내일 대응 리포트";
            this.Size = new Size(840, 720);
            this.MinimumSize = new Size(760, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(18, 22, 32);
            this.ForeColor = Color.White;
            this.Font = new Font("Malgun Gothic", 9f);
            this.ShowIcon = false;

            // 1. 상단 타이틀 배너
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 82,
                BackColor = Color.FromArgb(24, 30, 44),
                Padding = new Padding(18, 12, 18, 10)
            };

            _lblHeaderTitle = new Label
            {
                Text = $"📊 {DateTime.Now:yyyy년 MM월 dd일} 정규장 마감 결산 리포트",
                Font = new Font("Malgun Gothic", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 185, 50), // 선셋 골드
                Dock = DockStyle.Top,
                Height = 28
            };

            _lblHeaderSummary = new Label
            {
                Text = "보유 종목 당일 가치 변동 및 시장 지수 비교 집계 중...",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(200, 220, 245),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };

            pnlTop.Controls.Add(_lblHeaderSummary);
            pnlTop.Controls.Add(_lblHeaderTitle);

            // 2. 하단 액션 버튼 바
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 54,
                BackColor = Color.FromArgb(22, 26, 36),
                Padding = new Padding(16, 10, 16, 10)
            };

            _btnCopyClipboard = new Button
            {
                Text = "📋 리포트 클립보드 복사",
                Font = new Font("Malgun Gothic", 9.2f, FontStyle.Bold),
                BackColor = Color.FromArgb(16, 44, 38),
                ForeColor = Color.FromArgb(0, 230, 140),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Width = 200,
                Dock = DockStyle.Left
            };
            _btnCopyClipboard.FlatAppearance.BorderColor = Color.FromArgb(0, 200, 120);
            _btnCopyClipboard.Click += (s, e) => CopyReportToClipboard();

            _btnClose = new Button
            {
                Text = "닫기",
                Font = new Font("Malgun Gothic", 9f),
                BackColor = Color.FromArgb(32, 36, 48),
                ForeColor = Color.FromArgb(200, 215, 235),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Width = 100,
                Dock = DockStyle.Right
            };
            _btnClose.FlatAppearance.BorderColor = Color.FromArgb(70, 85, 110);
            _btnClose.Click += (s, e) => this.Close();

            pnlBottom.Controls.Add(_btnCopyClipboard);
            pnlBottom.Controls.Add(_btnClose);

            // 3. 중앙 리포트 뷰어 (다크 서식 지원 RichTextBox)
            _rtbReport = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(14, 18, 26),
                ForeColor = Color.FromArgb(235, 240, 255),
                Font = new Font("Malgun Gothic", 9.5f),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Padding = new Padding(16)
            };

            this.Controls.Add(_rtbReport);
            this.Controls.Add(pnlBottom);
            this.Controls.Add(pnlTop);
        }

        private void GenerateAndRenderReport()
        {
            if (_heldItems.Count == 0)
            {
                _lblHeaderSummary.Text = "현재 보유 수량이 있는 종목이 없습니다 (미보유 상태).";
                _rtbReport.Clear();
                AppendText("【 보유 종목 없음 】\n\n", Color.FromArgb(255, 180, 60), FontStyle.Bold, 12f);
                AppendText("현재 등록된 포트폴리오 중 보유 수량(Quantity > 0)이 등록된 종목이 없습니다.\n" +
                           "매도 후 종목을 삭제하셨거나, 관심 종목만 등록되어 있는 경우 장마감 결산이 집계되지 않습니다.\n\n" +
                           "💡 [+ 종목 추가] 또는 보유 수량을 입력하시면 당일 종가 기준의 실시간 결산 리포트가 생성됩니다.\n", Color.FromArgb(180, 195, 215), FontStyle.Regular, 9.5f);
                return;
            }

            // 1. 핵심 수치 집계
            decimal totalEval = _heldItems.Sum(x => x.TotalEvalAmount);
            decimal totalBuy = _heldItems.Sum(x => x.TotalBuyAmount);
            decimal cumulativeProfit = totalEval - totalBuy;
            decimal cumulativeRate = totalBuy > 0 ? (cumulativeProfit / totalBuy * 100m) : 0m;

            // 오늘 하루 평가 변동 금액 및 등락률 (전일 종가 기준)
            decimal todayChangeAmount = _heldItems.Sum(x => x.ChangePrice * x.Quantity);
            decimal prevDayEval = totalEval - todayChangeAmount;
            decimal todayChangeRate = prevDayEval > 0 ? (todayChangeAmount / prevDayEval * 100m) : 0m;

            // 시장 지수 (코스피 / 코스닥) 등락률 추출
            decimal kospiRate = 0m;
            decimal kosdaqRate = 0m;
            if (_macroSnapshot?.Indices != null)
            {
                var k1 = _macroSnapshot.Indices.Find(x => x.Name != null && x.Name.Contains("코스피"));
                if (k1 != null) kospiRate = k1.ChangeRate;
                var k2 = _macroSnapshot.Indices.Find(x => x.Name != null && x.Name.Contains("코스닥"));
                if (k2 != null) kosdaqRate = k2.ChangeRate;
            }
            decimal alphaKospi = todayChangeRate - kospiRate;

            // 상단 요약 배너 문구 설정
            string signToday = todayChangeAmount >= 0 ? "+" : "";
            string signAlpha = alphaKospi >= 0 ? "+" : "";
            _lblHeaderSummary.Text = $"오늘 하루 보유 자산 변동: {signToday}{todayChangeAmount:N0}원 ({signToday}{todayChangeRate:F2}%)  |  코스피({kospiRate:+0.00;-0.00}%) 대비 {signAlpha}{alphaKospi:F2}% 초과 성과";
            _lblHeaderSummary.ForeColor = todayChangeAmount >= 0 ? Color.FromArgb(100, 240, 160) : Color.FromArgb(100, 180, 255);

            // 2. 오늘의 MVP(최고 상승) vs 워스트(최대 하락) 종목
            var mvp = _heldItems.OrderByDescending(x => x.ChangeRate).FirstOrDefault();
            var worst = _heldItems.OrderBy(x => x.ChangeRate).FirstOrDefault();

            // 3. 메이저 수급 주체 분류
            var ssangkuriBuy = new List<StockHoldingItem>();
            var foreignBuy = new List<StockHoldingItem>();
            var instBuy = new List<StockHoldingItem>();
            var antOnly = new List<StockHoldingItem>();
            var majorSell = new List<StockHoldingItem>();

            foreach (var item in _heldItems)
            {
                string sup = item.MajorSupplyBadge ?? "";
                if (sup.Contains("쌍끌이매수") || sup.Contains("메이저쌍끌이")) ssangkuriBuy.Add(item);
                else if (sup.Contains("외인매수") || sup.Contains("외인")) foreignBuy.Add(item);
                else if (sup.Contains("기관매수") || sup.Contains("기관")) instBuy.Add(item);
                else if (sup.Contains("개미")) antOnly.Add(item);
                else if (sup.Contains("매도")) majorSell.Add(item);
            }

            // 4. 내일 시초가 대응 3대 체크포인트
            var targetNearItems = new List<Tuple<StockHoldingItem, decimal>>();
            var stopNearItems = new List<Tuple<StockHoldingItem, decimal>>();
            var earningsNearItems = new List<StockHoldingItem>();

            foreach (var item in _heldItems)
            {
                // 목표가 근접 체크 (목표가의 98% 이상 도달 시)
                decimal targetPrice = item.RecommendedTargetPrice > 0 ? item.RecommendedTargetPrice : item.ResistancePrice;
                if (targetPrice > 0 && item.CurrentPrice >= targetPrice * 0.98m)
                {
                    targetNearItems.Add(Tuple.Create(item, targetPrice));
                }

                // 손절가/지지선 위협 체크 (손절가 +2% 이내이거나 이미 이탈 시)
                decimal stopPrice = item.TargetStopPrice > 0 ? item.TargetStopPrice : (item.SupportPrice > 0 ? item.SupportPrice * 0.97m : (item.BuyPrice > 0 ? item.BuyPrice * 0.93m : 0m));
                stopPrice = Math.Round(stopPrice, 0);
                if (stopPrice > 0 && item.CurrentPrice <= stopPrice * 1.02m)
                {
                    stopNearItems.Add(Tuple.Create(item, stopPrice));
                }

                // 어닝 D-Day 경보 (3일 이내)
                if (item.EarningsDaysRemaining.HasValue && item.EarningsDaysRemaining.Value >= 0 && item.EarningsDaysRemaining.Value <= 3)
                {
                    earningsNearItems.Add(item);
                }
            }

            // 5. RichTextBox 렌더링 시작
            _rtbReport.Clear();

            // 섹션 1: 오늘의 내 계좌 성적표
            AppendText("[ 1. 오늘의 보유 자산 가치 변동 & 계좌 성적표 ]\n", Color.FromArgb(0, 230, 255), FontStyle.Bold, 11f);
            AppendText($"• 총 보유 평가액 : {totalEval:N0}원  (총 매입원금: {totalBuy:N0}원)\n", Color.White, FontStyle.Regular, 9.5f);
            AppendText($"• 총 누적 평가손익 : {cumulativeProfit:+0,0;-0,0;0}원 ({cumulativeRate:+0.00;-0.00}%)\n", cumulativeProfit >= 0 ? Color.FromArgb(255, 100, 100) : Color.FromArgb(100, 180, 255), FontStyle.Bold, 9.5f);
            AppendText($"• 오늘 하루 가치 변동 : {signToday}{todayChangeAmount:N0}원 ({signToday}{todayChangeRate:F2}%)\n", todayChangeAmount >= 0 ? Color.FromArgb(0, 230, 140) : Color.FromArgb(120, 180, 255), FontStyle.Bold, 10f);
            AppendText($"• 시장 지수 대비 비교 : 코스피 {kospiRate:+0.00;-0.00}% | 코스닥 {kosdaqRate:+0.00;-0.00}% 대비 ", Color.FromArgb(200, 215, 235), FontStyle.Regular, 9.5f);
            AppendText($"{signAlpha}{alphaKospi:F2}% {(alphaKospi >= 0 ? "초과 선방 (아웃퍼폼)" : "시장 대비 부진")}\n\n", alphaKospi >= 0 ? Color.FromArgb(0, 230, 140) : Color.FromArgb(255, 120, 120), FontStyle.Bold, 9.5f);

            if (mvp != null)
            {
                decimal mvpDayVal = mvp.ChangePrice * mvp.Quantity;
                AppendText($"• 오늘의 계좌 MVP : [{mvp.StockName}] ({mvp.ChangeRate:+0.00;-0.00}%, 당일 평가액 {mvpDayVal:+0,0;-0,0;0}원 기여)\n", Color.FromArgb(255, 215, 64), FontStyle.Bold, 9.5f);
            }
            if (worst != null && worst.ChangeRate < 0)
            {
                decimal worstDayVal = worst.ChangePrice * worst.Quantity;
                AppendText($"• 오늘의 부진 종목 : [{worst.StockName}] ({worst.ChangeRate:+0.00;-0.00}%, 당일 평가액 {worstDayVal:+0,0;-0,0;0}원)\n", Color.FromArgb(255, 130, 130), FontStyle.Regular, 9.5f);
            }
            AppendText("\n", Color.White, FontStyle.Regular, 9f);

            // 섹션 2: 보유 종목 메이저 수급 결산
            AppendText("[ 2. 보유 종목 메이저 수급 (외인·기관) 당일 결산 ]\n", Color.FromArgb(0, 230, 255), FontStyle.Bold, 11f);
            if (ssangkuriBuy.Count > 0)
            {
                string names = string.Join(", ", ssangkuriBuy.Select(x => $"{x.StockName}({x.ChangeRate:+0.0;-0.0}%)"));
                AppendText($"• 외인+기관 쌍끌이 순매수 : {names}\n", Color.FromArgb(0, 240, 210), FontStyle.Bold, 9.5f);
            }
            if (foreignBuy.Count > 0)
            {
                string names = string.Join(", ", foreignBuy.Select(x => $"{x.StockName}({x.ChangeRate:+0.0;-0.0}%)"));
                AppendText($"• 외국인 집중 순매수 : {names}\n", Color.FromArgb(120, 210, 255), FontStyle.Regular, 9.5f);
            }
            if (instBuy.Count > 0)
            {
                string names = string.Join(", ", instBuy.Select(x => $"{x.StockName}({x.ChangeRate:+0.0;-0.0}%)"));
                AppendText($"• 기관 집중 순매수 : {names}\n", Color.FromArgb(255, 215, 110), FontStyle.Regular, 9.5f);
            }
            if (majorSell.Count > 0)
            {
                string names = string.Join(", ", majorSell.Select(x => $"{x.StockName}({x.ChangeRate:+0.0;-0.0}%)"));
                AppendText($"• 메이저 동반 매도 출회 : {names}\n", Color.FromArgb(255, 110, 110), FontStyle.Regular, 9.5f);
            }
            if (antOnly.Count > 0)
            {
                string names = string.Join(", ", antOnly.Select(x => $"{x.StockName}({x.ChangeRate:+0.0;-0.0}%)"));
                AppendText($"• 개인(개미) 위주 매수 : {names} (기관/외인 수급 공백 주의)\n", Color.FromArgb(200, 180, 230), FontStyle.Regular, 9.5f);
            }
            if (ssangkuriBuy.Count == 0 && foreignBuy.Count == 0 && instBuy.Count == 0 && majorSell.Count == 0 && antOnly.Count == 0)
            {
                AppendText("• 당일 특이 수급 집중 종목 없음 (수급 관망세 유지)\n", Color.FromArgb(180, 190, 205), FontStyle.Regular, 9.5f);
            }
            AppendText("\n", Color.White, FontStyle.Regular, 9f);

            // 섹션 3: 내일 시초가 대응 3대 체크포인트
            AppendText("[ 3. 내일 시초가 대응 3대 체크포인트 ]\n", Color.FromArgb(0, 230, 255), FontStyle.Bold, 11f);

            // 1. 익절 대기
            if (targetNearItems.Count > 0)
            {
                AppendText("• 목표가 근접 종목 (분할 익절 검토)\n", Color.FromArgb(0, 230, 140), FontStyle.Bold, 9.5f);
                foreach (var t in targetNearItems)
                {
                    decimal diff = ((t.Item1.CurrentPrice - t.Item2) / t.Item2) * 100m;
                    AppendText($"   - {t.Item1.StockName} : 현재가 {t.Item1.CurrentPrice:N0}원 / 목표가 {t.Item2:N0}원 ({diff:+0.0;-0.0}%)\n" +
                               $"     → 내일 시초가 추가 상승 시 1/3~1/2 분할 익절 권고\n", Color.FromArgb(220, 245, 230), FontStyle.Regular, 9f);
                }
            }
            else
            {
                AppendText("• 목표가 근접 종목 : 현재 목표가 임박 종목 없음 (추세 홀딩 유지)\n", Color.FromArgb(180, 195, 210), FontStyle.Regular, 9f);
            }

            // 2. 손절 방어선 위협
            if (stopNearItems.Count > 0)
            {
                AppendText("• 손절 방어선 위협 종목 (리스크 관리)\n", Color.FromArgb(255, 95, 95), FontStyle.Bold, 9.5f);
                foreach (var s in stopNearItems)
                {
                    AppendText($"   - {s.Item1.StockName} : 현재가 {s.Item1.CurrentPrice:N0}원 / 손절선 {s.Item2:N0}원 (이탈 위험)\n" +
                               $"     → 내일 시초가 20일 지지선 반등 실패 시 비중 축소 또는 원칙 손절 대비\n", Color.FromArgb(255, 210, 210), FontStyle.Regular, 9f);
                }
            }
            else
            {
                AppendText("• 손절 방어선 : 전 보유 종목이 주요 지지선 위에서 안정적으로 마감\n", Color.FromArgb(180, 235, 190), FontStyle.Regular, 9f);
            }

            // 3. 어닝 레이더
            if (earningsNearItems.Count > 0)
            {
                AppendText("• 실적 발표 임박 종목 (어닝 변동성 주의)\n", Color.FromArgb(220, 130, 255), FontStyle.Bold, 9.5f);
                foreach (var e in earningsNearItems)
                {
                    string ddayStr = e.EarningsDaysRemaining == 0 ? "오늘 발표" : $"D-{e.EarningsDaysRemaining}";
                    AppendText($"   - {e.StockName} : {e.EarningsPeriodTitle} 실적 발표 [{ddayStr}] ({e.EarningsAnnouncementDate:yyyy-MM-dd})\n" +
                               $"     → 어닝 쇼크/서프라이즈에 따른 변동성에 유의하세요.\n", Color.FromArgb(240, 225, 255), FontStyle.Regular, 9f);
                }
            }
            else
            {
                AppendText("• 실적 레이더 : 향후 3일 이내 예정된 보유 종목 실적 발표 없음\n", Color.FromArgb(180, 195, 210), FontStyle.Regular, 9f);
            }

            // 4. AI 참모 1줄 종합 총평
            AppendText("\n[ 💡 AI 참모 종합 한 줄 총평 ]\n", Color.FromArgb(255, 215, 64), FontStyle.Bold, 10.5f);
            string aiSummary;
            if (todayChangeRate >= 1.0m)
            {
                aiSummary = $"오늘 포트폴리오는 시장 대비 강한 반등(+{todayChangeRate:F2}%)을 보이며 우수한 성과로 마감했습니다. " +
                            $"상승세를 탄 주도주는 익절선을 높여가며 이익을 극대화(Trailing Stop)하시고, 내일 시초가 갭상승 시 추격매수는 지양하세요.";
            }
            else if (todayChangeRate <= -1.0m)
            {
                aiSummary = $"오늘 시장 조정과 함께 포트폴리오 자산 가치가 {todayChangeRate:F2}% 하락 마감했습니다. " +
                            $"손절선 부근 종목의 20일선 지지력을 반드시 재확인하시고, 지지 이탈 시 감정에 치우치지 말고 기계적 비중 축소로 현금을 확보하세요.";
            }
            else
            {
                aiSummary = $"오늘 포트폴리오는 {todayChangeRate:+0.00;-0.00}%로 차분한 관망세 속에 마감되었습니다. " +
                            $"외인·기관의 수급이 유입되는 핵심 종목 중심으로 비중을 유지하며, 내일 시초가 지수 방향성을 확인한 후 대응하시기 바랍니다.";
            }
            AppendText(aiSummary + "\n", Color.FromArgb(245, 240, 220), FontStyle.Regular, 9.5f);

            _rtbReport.SelectionStart = 0;
            _rtbReport.ScrollToCaret();
        }

        private void AppendText(string text, Color color, FontStyle style, float size)
        {
            _rtbReport.SelectionStart = _rtbReport.TextLength;
            _rtbReport.SelectionLength = 0;
            _rtbReport.SelectionColor = color;
            using (var font = new Font("Malgun Gothic", size, style))
            {
                _rtbReport.SelectionFont = font;
            }
            _rtbReport.AppendText(text);
        }

        private void CopyReportToClipboard()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_rtbReport.Text)) return;
                Clipboard.SetText(_rtbReport.Text);
                ToastNotification.Show("장마감 결산 리포트가 클립보드에 복사되었습니다.\n(카카오톡, 텔레그램, 메모장에 바로 붙여넣을 수 있습니다)", "복사 완료", ToastIcon.Success);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "클립보드 복사 중 오류: " + ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}

