using System;
using System.Collections;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using GateHelper.StockAdvisor.Models;
using GateHelper.StockAdvisor.Services;

namespace GateHelper.StockAdvisor
{
    public partial class StockAdvisorControl
    {
        private void UpdateUi()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(UpdateUi));
                return;
            }

            // 1. 상단 브리핑 및 신호등 갱신
            var briefing = _portfolio.LastBriefing ?? new MarketBriefing();
            _lblBriefing1.Text = "• " + briefing.Line1;
            _lblBriefing2.Text = "• " + briefing.Line2;
            _lblBriefing3.Text = "• " + briefing.Line3;

            switch (briefing.RiskLevel)
            {
                case OverallRiskLevel.Safe:
                    _lblTrafficLight.Text = "●\n안전\n(뉴스📰)";
                    _lblTrafficLight.ForeColor = Color.FromArgb(70, 220, 140);
                    break;
                case OverallRiskLevel.Caution:
                    _lblTrafficLight.Text = "▲\n주의\n(뉴스📰)";
                    _lblTrafficLight.ForeColor = Color.FromArgb(240, 200, 60);
                    break;
                case OverallRiskLevel.Panic:
                    _lblTrafficLight.Text = "■\n비상\n(뉴스📰)";
                    _lblTrafficLight.ForeColor = Color.FromArgb(255, 80, 80);
                    break;
            }

            // 2. 비중 계산
            decimal totalEval = 0m;
            decimal totalBuy = 0m;
            foreach (var item in _portfolio.Items)
            {
                totalEval += item.TotalEvalAmount;
                totalBuy += item.TotalBuyAmount;
            }
            foreach (var item in _portfolio.Items)
            {
                item.WeightPercent = (totalEval > 0) ? (item.TotalEvalAmount / totalEval * 100m) : 0m;
            }

            // 3. 리스트뷰 갱신
            _lvStocks.BeginUpdate();
            _lvStocks.ListViewItemSorter = null; // 대량 추가 시 정렬기 간섭 방지
            _lvStocks.Items.Clear();

            foreach (var item in _portfolio.Items)
            {
                string rsiText = item.Rsi > 0 ? $"{item.Rsi:F1}%" : "-";
                string volText = item.VolumeRatio > 0 ? $"{item.VolumeRatio:F0}%" : "-";
                string chartText = item.ChartScore > 0 ? $"{item.ChartScore}점" : "-";
                string newsText = item.NewsScore > 0 ? $"{item.NewsScore}점" : "-";
                string riskText = item.RiskScore > 0 ? $"{item.RiskScore}점" : "-";
                string finalScoreText = item.FinalScore > 0 ? $"★ {item.FinalScore}점" : "-";

                string actionText = item.LastDiagnosis?.RecommendedAction;
                if (string.IsNullOrEmpty(actionText))
                {
                    actionText = item.Rsi >= 70m ? "[과열] 추격매수 금지" : (item.FinalScore >= 7 ? "[골든] 분할 진입 검토" : "진단 대기");
                }

                string gradeText = item.Grade.ToString();

                var lvi = new ListViewItem(new[]
                {
                    item.StockCode,                                      // 0: 코드
                    item.StockName,                                      // 1: 종목명
                    $"{item.CurrentPrice:N0}",                          // 2: 현재가
                    $"{item.ChangeRate:+0.00;-0.00;0.00}%",              // 3: 전일대비
                    $"{item.BuyPrice:N0}",                              // 4: 평단
                    $"{item.Quantity:N0}",                              // 5: 보유수량
                    $"{item.ProfitAmount:+0,0;-0,0;0}",                 // 6: 손익금(원)
                    $"{item.ProfitRate:+0.00;-0.00;0.00}%",             // 7: 수익률
                    gradeText,                                           // 8: AI 등급
                    rsiText,                                             // 9: 14일 RSI
                    volText,                                             // 10: 5일 거래량
                    chartText,                                           // 11: 차트
                    newsText,                                            // 12: 뉴스
                    riskText,                                            // 13: 손익비
                    finalScoreText,                                      // 14: ★종합점수
                    item.TimingBadge,                                    // 15: 타이밍 안전벨트
                    actionText                                           // 16: 권고 지침
                });

                lvi.Tag = item;

                // 0: 종목코드 (차분한 청회색)
                lvi.SubItems[0].ForeColor = Color.FromArgb(145, 165, 195);

                // 1: 종목명 (밝은 텍스트)
                lvi.SubItems[1].ForeColor = Color.FromArgb(235, 240, 255);

                // 2: 현재가 (화이트)
                lvi.SubItems[2].ForeColor = Color.White;

                // 3: 전일대비 상승퍼센트
                if (item.ChangeRate > 0) lvi.SubItems[3].ForeColor = Color.FromArgb(255, 95, 95);
                else if (item.ChangeRate < 0) lvi.SubItems[3].ForeColor = Color.FromArgb(85, 160, 255);
                else lvi.SubItems[3].ForeColor = Color.FromArgb(180, 190, 205);

                // 4: 평단 (부드러운 실버)
                lvi.SubItems[4].ForeColor = Color.FromArgb(215, 220, 230);

                // 5: 보유수량 (청록 실버)
                lvi.SubItems[5].ForeColor = Color.FromArgb(190, 215, 235);

                // 6: 손익금(원) (수익 빨강 / 손실 파랑)
                if (item.ProfitAmount > 0) lvi.SubItems[6].ForeColor = Color.FromArgb(255, 95, 95);
                else if (item.ProfitAmount < 0) lvi.SubItems[6].ForeColor = Color.FromArgb(85, 160, 255);
                else lvi.SubItems[6].ForeColor = Color.FromArgb(180, 190, 205);

                // 7: 수익률 (수익 빨강 / 손실 파랑)
                if (item.ProfitRate > 0) lvi.SubItems[7].ForeColor = Color.FromArgb(255, 95, 95);
                else if (item.ProfitRate < 0) lvi.SubItems[7].ForeColor = Color.FromArgb(85, 160, 255);
                else lvi.SubItems[7].ForeColor = Color.FromArgb(180, 190, 205);

                // 8: AI 등급 (A, B, C, F 4색)
                lvi.SubItems[8].ForeColor = GradeHelper.GetGradeColor(item.Grade);
                lvi.SubItems[8].Font = new Font(_lvStocks.Font, FontStyle.Bold);

                // 9: 14일 RSI
                if (item.Rsi >= 70m) lvi.SubItems[9].ForeColor = Color.FromArgb(255, 80, 80);
                else if (item.Rsi <= 30m && item.Rsi > 0) lvi.SubItems[9].ForeColor = Color.FromArgb(80, 180, 255);
                else lvi.SubItems[9].ForeColor = Color.FromArgb(120, 230, 150);

                // 10: 5일 거래량
                if (item.VolumeRatio >= 140m) lvi.SubItems[10].ForeColor = Color.FromArgb(0, 230, 200);
                else lvi.SubItems[10].ForeColor = Color.FromArgb(200, 215, 235);

                // 11: 차트 점수
                if (item.ChartScore >= 7) lvi.SubItems[11].ForeColor = Color.FromArgb(120, 215, 255);
                else if (item.ChartScore <= 3) lvi.SubItems[11].ForeColor = Color.FromArgb(255, 110, 110);

                // 12: 뉴스 점수
                if (item.NewsScore >= 7) lvi.SubItems[12].ForeColor = Color.FromArgb(120, 230, 150);
                else if (item.NewsScore <= 3) lvi.SubItems[12].ForeColor = Color.FromArgb(255, 110, 110);

                // 13: 손익비 점수
                if (item.RiskScore >= 8) lvi.SubItems[13].ForeColor = Color.FromArgb(120, 230, 150);
                else if (item.RiskScore <= 3) lvi.SubItems[13].ForeColor = Color.FromArgb(255, 170, 70);

                // 14: ★ 최종점수
                if (item.FinalScore >= 8) lvi.SubItems[14].ForeColor = Color.FromArgb(255, 215, 0); // 골드
                else if (item.FinalScore <= 3) lvi.SubItems[14].ForeColor = Color.FromArgb(255, 110, 110);
                else lvi.SubItems[14].ForeColor = Color.FromArgb(235, 240, 255);

                // 15: 타이밍 안전벨트
                string tb = item.TimingBadge ?? string.Empty;
                if (tb.Contains("과열")) lvi.SubItems[15].ForeColor = Color.FromArgb(255, 75, 95);
                else if (tb.Contains("골든")) lvi.SubItems[15].ForeColor = Color.FromArgb(0, 230, 118);
                else if (tb.Contains("눌림목")) lvi.SubItems[15].ForeColor = Color.FromArgb(255, 210, 60);
                else if (tb.Contains("과매도")) lvi.SubItems[15].ForeColor = Color.FromArgb(70, 180, 255);
                else if (tb.Contains("보유불필요")) lvi.SubItems[15].ForeColor = Color.FromArgb(255, 90, 90);
                else lvi.SubItems[15].ForeColor = Color.FromArgb(190, 200, 215);

                // 16: 권고 지침
                if (tb.Contains("과열")) lvi.SubItems[16].ForeColor = Color.FromArgb(255, 130, 130);
                else lvi.SubItems[16].ForeColor = Color.FromArgb(220, 225, 235);

                lvi.UseItemStyleForSubItems = false;
                _lvStocks.Items.Add(lvi);
            }
            _lvStocks.EndUpdate();

            // 기존 정렬 상태 유지
            if (_sortColumn >= 0 && _sortOrder != SortOrder.None)
            {
                _lvStocks.ListViewItemSorter = new StockListViewComparer(_sortColumn, _sortOrder);
                _lvStocks.Sort();
            }
            UpdateColumnHeaderSortArrows();

            // 4. 하단 요약바 갱신
            decimal totalProfit = totalEval - totalBuy;
            decimal totalRate = (totalBuy > 0) ? (totalProfit / totalBuy * 100m) : 0m;
            _lblAccountSummary.Text = $"총 매수: {totalBuy:N0}원  |  총 평가: {totalEval:N0}원  |  손익: {totalProfit:+0,0;-0,0;0}원 ({totalRate:+0.00;-0.00}%)";
            _lblAccountSummary.ForeColor = totalProfit >= 0 ? Color.FromArgb(255, 110, 110) : Color.FromArgb(100, 175, 255);

            // 첫 번째 종목 자동 선택
            if (_lvStocks.Items.Count > 0 && _lvStocks.SelectedItems.Count == 0)
            {
                _lvStocks.Items[0].Selected = true;
            }
            else
            {
                UpdateDetailPanel(null);
            }
        }

        private string GetGroupDisplay(PositionGroup group)
        {
            switch (group)
            {
                case PositionGroup.GroupA: return "[A:손절권고]";
                case PositionGroup.GroupC: return "[C:분할매수]";
                default: return "[B:관망유지]";
            }
        }

        private void LvStocks_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_lvStocks.SelectedItems.Count > 0)
            {
                var item = _lvStocks.SelectedItems[0].Tag as StockHoldingItem;
                UpdateDetailPanel(item);
            }
            else
            {
                UpdateDetailPanel(null);
            }
        }

        private void UpdateDetailPanel(StockHoldingItem item)
        {
            if (item == null)
            {
                _lblDetailTitle.Text = "선택된 종목이 없습니다";
                _lblDetailTimingWarning.Text = "🚨 [타이밍 가이드] 종목을 선택하시면 안전벨트 지침이 표시됩니다.";
                _lblDetailTimingWarning.BackColor = Color.FromArgb(28, 30, 38);
                _lblDetailTimingWarning.ForeColor = Color.Silver;
                _lblDetailScores.Text = "📊 [3대 정량점수] 차트: -점 | 뉴스: -점 | 손익비: -점\n★ 최종 종합점수: -점 / 10점";
                _lblDetailTechnicalFacts.Text = "🔍 [팩트] 14일 RSI: -% | 거래량: -%\n• 20일 지지선: -원 (하방위험: -%)";
                _lblDetailGroupBadge.Text = "포지션: -";
                _lblDetailAction.Text = "권고 액션: -";
                _lblDetailReliability.Text = "■ 출처 신뢰도: -";
                _lblDetailImpact.Text = "⚡ 정책/실적 직결도: -";
                _lblDetailTargetCut.Text = "◆ 권고 손절/방어선: - 원";
                RenderEmptyReport();
                return;
            }

            _lblDetailTitle.Text = $"◆ {item.StockName} ({item.StockCode})  |  현재가: {item.CurrentPrice:N0}원 ({item.ProfitRate:+0.00;-0.00}%)";

            // 타이밍 안전벨트 가이드 라벨
            string tb = item.TimingBadge ?? "⚪ 관망유지";
            string warnText = !string.IsNullOrEmpty(item.TimingWarning) ? item.TimingWarning : "추세 및 지지선 모니터링 중";
            if (tb.Contains("과열"))
            {
                _lblDetailTimingWarning.Text = $"🚨 {warnText}";
                _lblDetailTimingWarning.BackColor = Color.FromArgb(58, 20, 28);
                _lblDetailTimingWarning.ForeColor = Color.FromArgb(255, 120, 130);
            }
            else if (tb.Contains("골든"))
            {
                _lblDetailTimingWarning.Text = $"🟢 {warnText}";
                _lblDetailTimingWarning.BackColor = Color.FromArgb(16, 46, 32);
                _lblDetailTimingWarning.ForeColor = Color.FromArgb(100, 255, 180);
            }
            else if (tb.Contains("눌림목"))
            {
                _lblDetailTimingWarning.Text = $"🟡 {warnText}";
                _lblDetailTimingWarning.BackColor = Color.FromArgb(46, 40, 20);
                _lblDetailTimingWarning.ForeColor = Color.FromArgb(255, 230, 110);
            }
            else if (tb.Contains("과매도"))
            {
                _lblDetailTimingWarning.Text = $"🔵 {warnText}";
                _lblDetailTimingWarning.BackColor = Color.FromArgb(20, 36, 56);
                _lblDetailTimingWarning.ForeColor = Color.FromArgb(120, 210, 255);
            }
            else
            {
                _lblDetailTimingWarning.Text = $"⚪ {warnText}";
                _lblDetailTimingWarning.BackColor = Color.FromArgb(26, 30, 42);
                _lblDetailTimingWarning.ForeColor = Color.FromArgb(200, 215, 235);
            }

            // 3대 정량 점수 라벨 (2줄 배치로 텍스트 잘림 0%)
            _lblDetailScores.Text = $"📊 차트: {item.ChartScore}점  |  뉴스: {item.NewsScore}점  |  손익비: {item.RiskScore}점\n★ 최종 종합 점수 : ★ {item.FinalScore}점 / 10점";
            _lblDetailScores.ForeColor = item.FinalScore >= 8 ? Color.FromArgb(255, 215, 0) : Color.FromArgb(255, 225, 130);

            // 팩트 지표 라벨 (2줄 배치로 텍스트 잘림 0%)
            string suppStr = item.SupportPrice > 0 ? $"{item.SupportPrice:N0}원" : "-";
            _lblDetailTechnicalFacts.Text = $"🔍 14일 RSI: {item.Rsi:F1}%  |  5일대비 거래량: {item.VolumeRatio:F0}%\n• 20일 지지선: {suppStr} (하방위험: -{item.DownsideRiskPercent:F1}%)";

            // AI 등급 표시 (사용자 지정 4색: S,A(초록) / B,C(노랑) / D(주황) / F(빨강))
            _lblDetailGroupBadge.Text = $"AI 등급: {GradeHelper.GetGradeDescription(item.Grade)}";
            _lblDetailGroupBadge.ForeColor = GradeHelper.GetGradeColor(item.Grade);

            var diag = item.LastDiagnosis;
            if (diag != null)
            {
                _lblDetailAction.Text = $"▶ 권고 액션: {diag.RecommendedAction}";

                string stars = new string('★', diag.SourceReliability) + new string('☆', 5 - diag.SourceReliability);
                _lblDetailReliability.Text = $"■ 출처 신뢰도: {stars} ({diag.SourceReliability}/5점)";

                string impactShort = diag.PolicyImpact > 0 ? "호재 수혜" : (diag.PolicyImpact < 0 ? "악재 규제" : "중립");
                _lblDetailImpact.Text = $"⚡ 정책/실적 직결도: {(diag.PolicyImpact > 0 ? "+" : "")}{diag.PolicyImpact}점 ({impactShort})";
                _lblDetailTargetCut.Text = diag.TargetCutPrice > 0 ? $"◆ 권고 손절/방어선: {diag.TargetCutPrice:N0}원" : $"◆ 20일 지지 기준선: {suppStr}";

                RenderRichDetailReport(item, diag);
            }
            else
            {
                _lblDetailAction.Text = item.Rsi >= 70m ? "▶ 권고 액션: [과열주의] 추격매수 금지" : "권고 액션: [★ AI 참모 진단] 필요";
                _lblDetailReliability.Text = "■ 출처 신뢰도: 대기 중";
                _lblDetailImpact.Text = "⚡ 정책/실적 직결도: 대기 중";
                _lblDetailTargetCut.Text = $"◆ 20일 지지 기준선: {suppStr}";
                RenderRichPreDiagnosisReport(item);
            }
        }

        private void RenderEmptyReport()
        {
            if (_rtbDetailReason == null) return;
            _rtbDetailReason.Clear();
            AppendText("좌측 종목 목록에서 항목을 선택하시면 객관적 3대 지표 분석과 AI 참모 심층 리포트가 열람됩니다.", new Font("Malgun Gothic", 9.5f), Color.FromArgb(170, 185, 205));
        }

        private void RenderRichPreDiagnosisReport(StockHoldingItem item)
        {
            if (_rtbDetailReason == null) return;
            _rtbDetailReason.SuspendLayout();
            _rtbDetailReason.Clear();

            AppendText($"--------------------------------------------------------------------------------\n", new Font("Segoe UI", 8.5f), Color.FromArgb(60, 75, 100));
            AppendText($"  [정량 기술 지표 진단 리포트]  {DateTime.Now:yyyy-MM-dd HH:mm}\n", new Font("Malgun Gothic", 9.2f, FontStyle.Bold), Color.FromArgb(140, 190, 240));
            AppendText($"--------------------------------------------------------------------------------\n\n", new Font("Segoe UI", 8.5f), Color.FromArgb(60, 75, 100));

            // 섹션 1: 3대 점수 (굵은 글씨 헤더)
            AppendText("【 1. 객관적 3대 정량 점수 】\n", new Font("Malgun Gothic", 10.5f, FontStyle.Bold), Color.FromArgb(100, 215, 255));
            AppendText($"  ▶ ★ 최종 종목 점수 : ", new Font("Malgun Gothic", 9.5f), Color.FromArgb(200, 215, 235));
            AppendText($"★ {item.FinalScore} / 10점", new Font("Malgun Gothic", 10f, FontStyle.Bold), item.FinalScore >= 8 ? Color.FromArgb(255, 215, 0) : Color.FromArgb(255, 225, 120));
            AppendText($"  (차트 40% + 뉴스 30% + 손익비 30%)\n", new Font("Malgun Gothic", 8.8f), Color.FromArgb(150, 170, 195));
            AppendText($"  • 📈 차트 기술 점수 : {item.ChartScore} / 10점  (5일/20일선 정배열 및 거래량 수급)\n", new Font("Malgun Gothic", 9.2f), Color.FromArgb(220, 225, 240));
            AppendText($"  • 📰 뉴스 감성 점수 : {item.NewsScore} / 10점  (AI 심층 진단 시 정밀 분석 반영)\n", new Font("Malgun Gothic", 9.2f), Color.FromArgb(220, 225, 240));
            AppendText($"  • 🛡️ 손익비 안전 점수 : {item.RiskScore} / 10점  (지지선 대비 하방 안전마진)\n\n", new Font("Malgun Gothic", 9.2f), Color.FromArgb(220, 225, 240));

            // 섹션 2: 팩트 데이터 및 타이밍 (굵은 글씨 헤더)
            AppendText("【 2. 팩트 데이터 및 타이밍 안전벨트 】\n", new Font("Malgun Gothic", 10.5f, FontStyle.Bold), Color.FromArgb(255, 215, 100));
            AppendText($"  ▶ 타이밍 안전 판정 : ", new Font("Malgun Gothic", 9.5f), Color.FromArgb(200, 215, 235));
            AppendText($"{item.TimingBadge}\n", new Font("Malgun Gothic", 9.8f, FontStyle.Bold), item.TimingBadge.Contains("과열") ? Color.FromArgb(255, 95, 95) : Color.FromArgb(100, 255, 180));
            if (!string.IsNullOrEmpty(item.TimingWarning))
            {
                AppendText($"  ▶ 안전 가이드 : {item.TimingWarning}\n", new Font("Malgun Gothic", 9.2f, FontStyle.Bold), item.TimingBadge.Contains("과열") ? Color.FromArgb(255, 130, 130) : Color.FromArgb(255, 230, 110));
            }
            AppendText($"  • 14일 RSI : {item.Rsi:F1}% {(item.Rsi >= 70 ? "(과열 구간 - 추격매수 금지!)" : (item.Rsi <= 30 ? "(과매도 침체 구간)" : "(안정 추세)"))}\n", new Font("Malgun Gothic", 9.2f), Color.FromArgb(220, 225, 240));
            AppendText($"  • 5일 평균대비 거래량 : {item.VolumeRatio:F0}% {(item.VolumeRatio >= 140 ? "(대량 수급 유입)" : "")}\n", new Font("Malgun Gothic", 9.2f), Color.FromArgb(220, 225, 240));
            AppendText($"  • 20일 지지선 (손절 기준선) : {item.SupportPrice:N0}원 (현재가 대비 -{item.DownsideRiskPercent:F1}%)\n", new Font("Malgun Gothic", 9.2f), Color.FromArgb(220, 225, 240));
            AppendText($"  • 20일 저항선 (단기 목표선) : {item.ResistancePrice:N0}원\n\n", new Font("Malgun Gothic", 9.2f), Color.FromArgb(220, 225, 240));

            AppendText("💡 상단의 [★ AI 참모 진단] 버튼을 클릭하시면 글로벌 외신 및 실시간 정책 공시를 결합한 최고급 전략 리포트가 완성됩니다.\n", new Font("Malgun Gothic", 9f), Color.FromArgb(130, 190, 255));

            _rtbDetailReason.Select(0, 0);
            _rtbDetailReason.ScrollToCaret();
            _rtbDetailReason.ResumeLayout();
        }

        private void RenderRichDetailReport(StockHoldingItem item, StockRiskDiagnosis diag)
        {
            if (_rtbDetailReason == null) return;
            _rtbDetailReason.SuspendLayout();
            _rtbDetailReason.Clear();

            AppendText($"--------------------------------------------------------------------------------\n", new Font("Segoe UI", 8.5f), Color.FromArgb(60, 75, 100));
            AppendText($"  [AI 주식 참모 심층 진단 리포트]  {diag.DiagnosedAt:yyyy-MM-dd HH:mm}\n", new Font("Malgun Gothic", 9.2f, FontStyle.Bold), Color.FromArgb(140, 190, 240));
            AppendText($"--------------------------------------------------------------------------------\n\n", new Font("Segoe UI", 8.5f), Color.FromArgb(60, 75, 100));

            // 섹션 1: 3대 점수 (굵은 글씨 헤더)
            AppendText("【 1. 객관적 3대 정량 점수 및 타이밍 】\n", new Font("Malgun Gothic", 10.5f, FontStyle.Bold), Color.FromArgb(100, 215, 255));
            AppendText($"  ▶ ★ 최종 종합 점수 : ", new Font("Malgun Gothic", 9.5f), Color.FromArgb(200, 215, 235));
            AppendText($"★ {item.FinalScore} / 10점", new Font("Malgun Gothic", 10.5f, FontStyle.Bold), item.FinalScore >= 8 ? Color.FromArgb(255, 215, 0) : (item.FinalScore <= 3 ? Color.FromArgb(255, 100, 100) : Color.FromArgb(255, 225, 120)));
            AppendText($"  (가중치: 차트 40% + 뉴스 30% + 손익비 30%)\n", new Font("Malgun Gothic", 8.8f), Color.FromArgb(150, 170, 195));
            AppendText($"  • 📈 차트 점수 : {item.ChartScore} / 10점   |   📰 뉴스 점수 : {item.NewsScore} / 10점   |   🛡️ 손익비 점수 : {item.RiskScore} / 10점\n", new Font("Malgun Gothic", 9.2f), Color.FromArgb(220, 225, 240));
            AppendText($"  • 타이밍 안전벨트 : ", new Font("Malgun Gothic", 9.2f), Color.FromArgb(200, 215, 235));
            AppendText($"{item.TimingBadge}\n", new Font("Malgun Gothic", 9.5f, FontStyle.Bold), item.TimingBadge.Contains("과열") ? Color.FromArgb(255, 95, 95) : Color.FromArgb(100, 255, 180));
            if (!string.IsNullOrEmpty(item.TimingWarning))
            {
                AppendText($"  • 타이밍 가이드 : {item.TimingWarning}\n", new Font("Malgun Gothic", 9.2f, FontStyle.Bold), item.TimingBadge.Contains("과열") ? Color.FromArgb(255, 130, 130) : Color.FromArgb(255, 230, 110));
            }
            AppendText($"  • 14일 RSI : {item.Rsi:F1}%   |   5일대비 거래량 : {item.VolumeRatio:F0}%\n", new Font("Malgun Gothic", 9.2f), Color.FromArgb(220, 225, 240));
            AppendText($"  • 20일 지지선 : {item.SupportPrice:N0}원 (하방 위험 -{item.DownsideRiskPercent:F1}%)   |   저항선 : {item.ResistancePrice:N0}원\n\n", new Font("Malgun Gothic", 9.2f), Color.FromArgb(220, 225, 240));

            // 섹션 2: 행동 지침 (굵은 글씨 헤더)
            AppendText("【 2. 핵심 권고 행동 지침 】\n", new Font("Malgun Gothic", 10.5f, FontStyle.Bold), Color.FromArgb(255, 215, 100));
            AppendText($"  ▶ 권고 액션 : ", new Font("Malgun Gothic", 9.5f), Color.FromArgb(200, 215, 235));
            Color actColor = diag.Group == PositionGroup.GroupA ? Color.FromArgb(255, 95, 95) : (diag.Group == PositionGroup.GroupC ? Color.FromArgb(100, 255, 180) : Color.FromArgb(255, 220, 100));
            AppendText($"{diag.RecommendedAction}\n", new Font("Malgun Gothic", 10f, FontStyle.Bold), actColor);
            if (diag.TargetCutPrice > 0)
            {
                AppendText($"  ▶ 권고 손절/방어선 : {diag.TargetCutPrice:N0}원\n", new Font("Malgun Gothic", 9.5f, FontStyle.Bold), Color.FromArgb(255, 140, 140));
            }
            AppendText("\n", new Font("Segoe UI", 6f), Color.White);

            // 섹션 3: 거시 지표 (굵은 글씨 헤더)
            AppendText("【 3. 거시 지표 & 정책 영향 】\n", new Font("Malgun Gothic", 10.5f, FontStyle.Bold), Color.FromArgb(120, 230, 160));
            string stars = new string('★', diag.SourceReliability) + new string('☆', 5 - diag.SourceReliability);
            AppendText($"  • 정보 출처 신뢰도 : {stars} ({diag.SourceReliability} / 5점)\n", new Font("Malgun Gothic", 9.2f), Color.FromArgb(220, 225, 240));
            string impactDesc = diag.PolicyImpact > 0 ? "호재성 정책/실적 수혜" : (diag.PolicyImpact < 0 ? "악재성 규제/비용 충격" : "중립 영향");
            AppendText($"  • 정책/실적 직결도 : {(diag.PolicyImpact > 0 ? "+" : "")}{diag.PolicyImpact}점 ({impactDesc})\n\n", new Font("Malgun Gothic", 9.2f), Color.FromArgb(220, 225, 240));

            // 섹션 4: 심층 대응 논리 (굵은 글씨 헤더)
            AppendText("【 4. 심층 대응 논리 및 판단 근거 】\n", new Font("Malgun Gothic", 10.5f, FontStyle.Bold), Color.FromArgb(180, 210, 245));
            string reasonText = diag.ReasonSummary ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(reasonText))
            {
                var sentences = reasonText.Split(new[] { ". ", ".\n", ".\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var s in sentences)
                {
                    string trimmed = s.Trim().TrimEnd('.');
                    if (!string.IsNullOrWhiteSpace(trimmed))
                    {
                        AppendText($"  • {trimmed}.\n\n", new Font("Malgun Gothic", 9.3f), Color.FromArgb(235, 240, 252));
                    }
                }
            }
            else
            {
                AppendText("  • 상세 대응 논리가 기록되지 않았습니다.\n\n", new Font("Malgun Gothic", 9.2f), Color.FromArgb(180, 190, 205));
            }

            // 섹션 5: 사용자 메모 (굵은 글씨 헤더)
            if (!string.IsNullOrWhiteSpace(item.Memo))
            {
                AppendText("【 5. 사용자 투자 메모 】\n", new Font("Malgun Gothic", 10.5f, FontStyle.Bold), Color.FromArgb(240, 200, 140));
                AppendText($"  • {item.Memo}\n", new Font("Malgun Gothic", 9.2f), Color.FromArgb(225, 230, 240));
            }

            _rtbDetailReason.Select(0, 0);
            _rtbDetailReason.ScrollToCaret();
            _rtbDetailReason.ResumeLayout();
        }

        private void AppendText(string text, Font font, Color color)
        {
            if (_rtbDetailReason == null) return;
            _rtbDetailReason.SelectionFont = font;
            _rtbDetailReason.SelectionColor = color;
            _rtbDetailReason.AppendText(text);
        }

        private void UpdateMacroTicker(GlobalMarketSnapshot intel)
        {
            if (intel == null) return;
            _macroSnapshot = intel;
            if (_lblMacroTicker == null) return;
            try
            {
                string tickerText = intel.GetTickerText();
                if (intel.RecentPolicyHeadlines.Count > 0)
                {
                    tickerText += "   |   미 연방관보(BIS): " + intel.RecentPolicyHeadlines[0];
                }
                _lblMacroTicker.Text = "⚡ [글로벌 매크로 인텔]  " + tickerText;
            }
            catch { }
        }

        #region Column Sorting Logic

        private void LvStocks_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == _sortColumn)
            {
                // 동일 컬럼 재클릭 시 오름차순 <-> 내림차순 토글
                _sortOrder = (_sortOrder == SortOrder.Ascending) ? SortOrder.Descending : SortOrder.Ascending;
            }
            else
            {
                _sortColumn = e.Column;
                // 금액/수익률/지표/점수 컬럼(2~7번, 9~14번)은 높은 값(내림차순) 우선
                // 8번(AI 등급: A->B->C->F) 및 텍스트 컬럼(0:코드, 1:종목명, 15:타이밍, 16:권고)은 오름차순 우선
                _sortOrder = (e.Column >= 2 && e.Column <= 14 && e.Column != 8) ? SortOrder.Descending : SortOrder.Ascending;
            }

            UpdateColumnHeaderSortArrows();

            _lvStocks.ListViewItemSorter = new StockListViewComparer(_sortColumn, _sortOrder);
            _lvStocks.Sort();
        }

        private void UpdateColumnHeaderSortArrows()
        {
            if (_lvStocks == null) return;

            for (int i = 0; i < _lvStocks.Columns.Count; i++)
            {
                string baseName = (i < OriginalColumnNames.Length) ? OriginalColumnNames[i] : _lvStocks.Columns[i].Text;
                if (i == _sortColumn && _sortOrder != SortOrder.None)
                {
                    _lvStocks.Columns[i].Text = baseName + (_sortOrder == SortOrder.Ascending ? " ▲" : " ▼");
                }
                else
                {
                    _lvStocks.Columns[i].Text = baseName;
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// 주식 포트폴리오 리스트뷰 전용 수치/텍스트 정렬기
    /// </summary>
    public class StockListViewComparer : IComparer
    {
        public int ColumnIndex { get; set; } = -1;
        public SortOrder Order { get; set; } = SortOrder.None;

        public StockListViewComparer(int column, SortOrder order)
        {
            ColumnIndex = column;
            Order = order;
        }

        public int Compare(object x, object y)
        {
            if (Order == SortOrder.None || ColumnIndex < 0) return 0;

            var lviX = x as ListViewItem;
            var lviY = y as ListViewItem;
            if (lviX == null || lviY == null) return 0;

            var itemX = lviX.Tag as StockHoldingItem;
            var itemY = lviY.Tag as StockHoldingItem;
            if (itemX == null || itemY == null) return 0;

            int result = 0;
            switch (ColumnIndex)
            {
                case 0: // 코드 (문자열 정렬)
                    result = string.Compare(itemX.StockCode, itemY.StockCode, StringComparison.OrdinalIgnoreCase);
                    break;
                case 1: // 종목명 (한글 가나다순)
                    result = string.Compare(itemX.StockName, itemY.StockName, StringComparison.CurrentCultureIgnoreCase);
                    break;
                case 2: // 현재가 (수치 정렬)
                    result = itemX.CurrentPrice.CompareTo(itemY.CurrentPrice);
                    break;
                case 3: // 전일대비 상승률 (수치 정렬)
                    result = itemX.ChangeRate.CompareTo(itemY.ChangeRate);
                    break;
                case 4: // 평단 (수치 정렬)
                    result = itemX.BuyPrice.CompareTo(itemY.BuyPrice);
                    break;
                case 5: // 보유수량 (수치 정렬)
                    result = itemX.Quantity.CompareTo(itemY.Quantity);
                    break;
                case 6: // 손익금(원) (수치 정렬)
                    result = itemX.ProfitAmount.CompareTo(itemY.ProfitAmount);
                    break;
                case 7: // 수익률 (수치 정렬)
                    result = itemX.ProfitRate.CompareTo(itemY.ProfitRate);
                    break;
                case 8: // AI 등급 (A, B, C, F)
                    result = itemX.Grade.CompareTo(itemY.Grade);
                    break;
                case 9: // 14일 RSI (수치 정렬)
                    result = itemX.Rsi.CompareTo(itemY.Rsi);
                    break;
                case 10: // 5일 거래량 (수치 정렬)
                    result = itemX.VolumeRatio.CompareTo(itemY.VolumeRatio);
                    break;
                case 11: // 차트 점수 (수치 정렬)
                    result = itemX.ChartScore.CompareTo(itemY.ChartScore);
                    break;
                case 12: // 뉴스 점수 (수치 정렬)
                    result = itemX.NewsScore.CompareTo(itemY.NewsScore);
                    break;
                case 13: // 손익비 점수 (수치 정렬)
                    result = itemX.RiskScore.CompareTo(itemY.RiskScore);
                    break;
                case 14: // ★종합점수 (수치 정렬)
                    result = itemX.FinalScore.CompareTo(itemY.FinalScore);
                    break;
                case 15: // 타이밍 안전벨트
                    result = string.Compare(itemX.TimingBadge, itemY.TimingBadge, StringComparison.CurrentCultureIgnoreCase);
                    break;
                case 16: // 권고 지침
                    string actX = itemX.LastDiagnosis?.RecommendedAction ?? string.Empty;
                    string actY = itemY.LastDiagnosis?.RecommendedAction ?? string.Empty;
                    result = string.Compare(actX, actY, StringComparison.CurrentCultureIgnoreCase);
                    break;
                default:
                    result = 0;
                    break;
            }

            if (result == 0 && ColumnIndex != 1)
            {
                // 동률 시 2차 정렬: 종합점수 내림차순 -> 종목명 가나다순 안정 정렬
                result = itemY.FinalScore.CompareTo(itemX.FinalScore);
                if (result == 0)
                {
                    result = string.Compare(itemX.StockName, itemY.StockName, StringComparison.CurrentCultureIgnoreCase);
                }
                return result;
            }

            return Order == SortOrder.Descending ? -result : result;
        }
    }
}
