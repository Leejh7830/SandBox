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

            // 1. 상단 시장 상태 신호등 갱신
            var briefing = _portfolio.LastBriefing ?? new MarketBriefing();

            switch (briefing.RiskLevel)
            {
                case OverallRiskLevel.Safe:
                    _lblTrafficLight.Text = "● 시장안전 (뉴스)";
                    _lblTrafficLight.ForeColor = Color.FromArgb(70, 220, 140);
                    break;
                case OverallRiskLevel.Caution:
                    _lblTrafficLight.Text = "▲ 시장주의 (뉴스)";
                    _lblTrafficLight.ForeColor = Color.FromArgb(240, 200, 60);
                    break;
                case OverallRiskLevel.Panic:
                    _lblTrafficLight.Text = "■ 시장비상 (뉴스)";
                    _lblTrafficLight.ForeColor = Color.FromArgb(255, 80, 80);
                    break;
            }

            // 2. 비중 계산 및 관심/보유 종목 집계
            int totalCount = _portfolio.Items.Count;
            int holdingCount = 0;
            int watchlistCount = 0;
            decimal totalEval = 0m;
            decimal totalBuy = 0m;

            int orderSetCount = 0;
            foreach (var item in _portfolio.Items)
            {
                if (item.IsBrokerOrderSet) orderSetCount++;
                if (item.IsWatchlist) watchlistCount++;
                else
                {
                    holdingCount++;
                    totalEval += item.TotalEvalAmount;
                    totalBuy += item.TotalBuyAmount;
                }
            }
            foreach (var item in _portfolio.Items)
            {
                item.WeightPercent = (!item.IsWatchlist && totalEval > 0) ? (item.TotalEvalAmount / totalEval * 100m) : 0m;
            }

            // 필터 버튼 텍스트 및 요약 갱신
            if (_btnFilterAll != null) _btnFilterAll.Text = $"전체 ({totalCount})";
            if (_btnFilterHolding != null) _btnFilterHolding.Text = $"보유 ({holdingCount})";
            if (_btnFilterWatchlist != null) _btnFilterWatchlist.Text = $"★ 관심 ({watchlistCount})";
            if (_lblFilterSummary != null) _lblFilterSummary.Text = $"보유 {holdingCount}종목 | 관심 {watchlistCount}종목 | 앱 주문설정 {orderSetCount}건";
            UpdateFilterButtonStyles();

            // 3. 리스트뷰 갱신 (기존 사용자가 직접 선택한 종목이 있을 때만 선택 복원, 최초 열림 시에는 미선택 상태 유지)
            string prevSelectedCode = null;
            if (_lvStocks.SelectedItems.Count > 0 && _lvStocks.SelectedItems[0].Tag is StockHoldingItem prevItem)
            {
                prevSelectedCode = prevItem.StockCode;
            }

            _isUpdatingUi = true;
            try
            {
                _lvStocks.BeginUpdate();
                _lvStocks.ListViewItemSorter = null; // 대량 추가 시 정렬기 간섭 방지
                _lvStocks.Items.Clear();

                foreach (var item in _portfolio.Items)
                {
                    // 필터 모드 적용
                    if (_filterMode == StockFilterMode.HoldingOnly && item.IsWatchlist) continue;
                    if (_filterMode == StockFilterMode.WatchlistOnly && !item.IsWatchlist) continue;

                    string rsiText = item.Rsi > 0 ? $"{item.Rsi:F1}" : "-";
                    string volText = item.VolumeRatio > 0 ? $"{item.VolumeRatio:F0}%" : "-";
                    string finalScoreText = item.FinalScore > 0 ? $"{item.FinalScore}점" : "-";

                    // AI 참모 최종 결론 및 행동 판정 평가
                    StockVerdictInfo verdict = StockVerdictHelper.Evaluate(item);

                    string gradeText = item.Grade.ToString();
                    var supplyInfo = SupplyHelper.GetSupplyInfo(item);
                    var landmineInfo = LandmineHelper.GetSimplifiedBadge(item);

                    string priceDisplay = item.IsPriceScrapeSuccess ? $"{item.CurrentPrice:N0}" : $"{item.CurrentPrice:N0} [오류]";
                    string changeDisplay = item.IsPriceScrapeSuccess ? $"{item.ChangeRate:+0.00;-0.00;0.00}%" : "[수집실패]";

                    string buyPriceDisplay = item.IsWatchlist ? "- " : $"{item.BuyPrice:N0}";
                    string qtyDisplay = item.IsWatchlist ? "[관심]" : $"{item.Quantity:N0}";
                    string profitAmtDisplay = item.IsWatchlist ? "- " : $"{item.ProfitAmount:+0,0;-0,0;0}";
                    string profitRateDisplay = item.IsWatchlist ? "- " : $"{item.ProfitRate:+0.00;-0.00;0.00}%";

                    var lvi = new ListViewItem(new[]
                    {
                        item.StockName,                                      // 0: 종목명 (체크박스 탑재)
                        priceDisplay,                                        // 1: 현재가
                        changeDisplay,                                       // 2: 전일대비
                        buyPriceDisplay,                                     // 3: 평단
                        qtyDisplay,                                          // 4: 보유수량
                        profitAmtDisplay,                                    // 5: 손익금(원)
                        profitRateDisplay,                                   // 6: 수익률
                        gradeText,                                           // 7: AI 등급
                        supplyInfo.Text,                                     // 8: 수급(외인·기관)
                        landmineInfo.Text,                                   // 9: 지뢰검증 (우수/안전/주의/위험)
                        rsiText,                                             // 10: 14일 RSI
                        volText,                                             // 11: 5일 거래량
                        finalScoreText,                                      // 12: ★종합점수
                        item.TimingBadge,                                    // 13: 타이밍 안전벨트
                        verdict.BadgeText                                    // 14: 최종 액션 (신호등 뱃지)
                    });

                    lvi.Tag = item;
                    lvi.Checked = item.IsBrokerOrderSet;

                    string orderTip = item.IsBrokerOrderSet
                        ? (item.IsWatchlist ? "[증권앱 지정가 매수주문 설정 완료]" : "[증권앱 손절/익절 자동청산 설정 완료]")
                        : (item.IsWatchlist ? "[증권앱 매수주문 미설정 - 체크박스로 관리]" : "[증권앱 자동청산 미설정 - 체크박스로 관리]");

                    // 급한 종목 하이라이트 (당일 -5% 급락 또는 손절가 터치/이탈 등)
                    if (verdict.IsEmergency)
                    {
                        lvi.BackColor = Color.FromArgb(44, 18, 24); // 부드러운 다크 와인 틴트
                        lvi.SubItems[0].Text = $"{item.StockName} ⚠️";
                        lvi.SubItems[0].ForeColor = Color.FromArgb(255, 140, 150);
                        lvi.ToolTipText = $"⚠️ [위급 경보 종목] {verdict.EmergencyReason}\n{item.StockName} ({item.StockCode}) · {orderTip}";
                    }
                    else
                    {
                        lvi.SubItems[0].Text = item.StockName;
                        lvi.SubItems[0].ForeColor = Color.FromArgb(235, 240, 255);
                        lvi.ToolTipText = $"{item.StockName} ({item.StockCode}) · {orderTip}";
                    }

                    // 1: 현재가 (화이트 또는 수집실패 주황색 경고)
                    lvi.SubItems[1].ForeColor = item.IsPriceScrapeSuccess ? Color.White : Color.FromArgb(255, 150, 80);

                    // 2: 전일대비 상승퍼센트 (또는 수집실패 적색)
                    if (!item.IsPriceScrapeSuccess) lvi.SubItems[2].ForeColor = Color.FromArgb(255, 110, 110);
                    else if (item.ChangeRate > 0) lvi.SubItems[2].ForeColor = Color.FromArgb(255, 95, 95);
                    else if (item.ChangeRate < 0) lvi.SubItems[2].ForeColor = Color.FromArgb(85, 160, 255);
                    else lvi.SubItems[2].ForeColor = Color.FromArgb(180, 190, 205);

                    // 3: 평단 (부드러운 실버)
                    lvi.SubItems[3].ForeColor = Color.FromArgb(215, 220, 230);

                    // 4: 보유수량 (청록 실버 또는 관심종목 네온 사이언)
                    lvi.SubItems[4].ForeColor = item.IsWatchlist ? Color.FromArgb(0, 220, 255) : Color.FromArgb(190, 215, 235);
                    if (item.IsWatchlist) lvi.SubItems[4].Font = SubItemBoldFont;

                    // 5: 손익금(원) (수익 빨강 / 손실 파랑)
                    if (item.ProfitAmount > 0) lvi.SubItems[5].ForeColor = Color.FromArgb(255, 95, 95);
                    else if (item.ProfitAmount < 0) lvi.SubItems[5].ForeColor = Color.FromArgb(85, 160, 255);
                    else lvi.SubItems[5].ForeColor = Color.FromArgb(180, 190, 205);

                    // 6: 수익률 (수익 빨강 / 손실 파랑)
                    if (item.ProfitRate > 0) lvi.SubItems[6].ForeColor = Color.FromArgb(255, 95, 95);
                    else if (item.ProfitRate < 0) lvi.SubItems[6].ForeColor = Color.FromArgb(85, 160, 255);
                    else lvi.SubItems[6].ForeColor = Color.FromArgb(180, 190, 205);

                    // 7: AI 등급 (A, B, C, F 4색, 정적 폰트 캐시 사용으로 GC 0B)
                    lvi.SubItems[7].ForeColor = GradeHelper.GetGradeColor(item.Grade);
                    lvi.SubItems[7].Font = SubItemBoldFont;

                    bool isDangerAction = verdict.BadgeText.Contains("급락") || verdict.BadgeText.Contains("매수금지") || verdict.BadgeText.Contains("비중축소");

                    // 8: 수급 (외인·기관) - 메이저 매수(연초록), 외인/기관(하늘색), 수급공방(노란색), 개미홀로(주황색), 메이저 매도(빨간색)
                    // 만약 최종 액션이 위험(급락/매수금지/비중축소)이면 수급이 양호해도 톤다운(뮤트)하여 뇌동매매 유인 차단!
                    if (isDangerAction && (supplyInfo.Text.Contains("매수") || supplyInfo.Text.Contains("메이저")))
                    {
                        lvi.SubItems[8].ForeColor = Color.FromArgb(120, 150, 175);
                    }
                    else
                    {
                        lvi.SubItems[8].ForeColor = supplyInfo.Color;
                    }
                    if (supplyInfo.IsBold && !isDangerAction) lvi.SubItems[8].Font = SubItemBoldFont;

                    // 9: 지뢰검증 (간결 4단계: 우수/안전 -> 연초록, 주의 -> 주황, 위험 -> 빨강)
                    lvi.SubItems[9].ForeColor = landmineInfo.Color;
                    if (landmineInfo.IsBold) lvi.SubItems[9].Font = SubItemBoldFont;

                    // 10: 14일 RSI (위험 상태일 경우 과매도 반등 톤다운)
                    if (item.Rsi >= 70m) lvi.SubItems[10].ForeColor = Color.FromArgb(255, 80, 80);
                    else if (item.Rsi <= 30m && item.Rsi > 0)
                    {
                        lvi.SubItems[10].ForeColor = isDangerAction ? Color.FromArgb(120, 160, 190) : Color.FromArgb(80, 180, 255);
                    }
                    else lvi.SubItems[10].ForeColor = isDangerAction ? Color.FromArgb(140, 160, 170) : Color.FromArgb(120, 230, 150);

                    // 11: 5일 거래량
                    if (item.VolumeRatio >= 140m) lvi.SubItems[11].ForeColor = Color.FromArgb(0, 230, 200);
                    else lvi.SubItems[11].ForeColor = Color.FromArgb(200, 215, 235);

                    // 12: ★ 최종점수
                    if (item.FinalScore >= 8) lvi.SubItems[12].ForeColor = Color.FromArgb(255, 215, 0); // 골드
                    else if (item.FinalScore <= 3) lvi.SubItems[12].ForeColor = Color.FromArgb(255, 110, 110);
                    else lvi.SubItems[12].ForeColor = Color.FromArgb(235, 240, 255);

                    // 13: 타이밍 안전벨트 (위험 상태일 때는 골든/과매도 배지가 밝은 초록색으로 튀지 않게 뮤트 처리)
                    string tb = item.TimingBadge ?? string.Empty;
                    if (tb.Contains("과열")) lvi.SubItems[13].ForeColor = Color.FromArgb(255, 75, 95);
                    else if (tb.Contains("골든")) lvi.SubItems[13].ForeColor = isDangerAction ? Color.FromArgb(130, 165, 175) : Color.FromArgb(0, 230, 118);
                    else if (tb.Contains("눌림목")) lvi.SubItems[13].ForeColor = isDangerAction ? Color.FromArgb(160, 155, 130) : Color.FromArgb(255, 210, 60);
                    else if (tb.Contains("과매도")) lvi.SubItems[13].ForeColor = isDangerAction ? Color.FromArgb(130, 165, 175) : Color.FromArgb(0, 230, 118);
                    else if (tb.Contains("보유불필요")) lvi.SubItems[13].ForeColor = Color.FromArgb(255, 90, 90);
                    else lvi.SubItems[13].ForeColor = Color.FromArgb(190, 200, 215);

                    // 14: 최종 액션 (신호등 뱃지)
                    lvi.SubItems[14].ForeColor = verdict.ListBadgeColor;
                    lvi.SubItems[14].Font = SubItemBoldFont;

                    lvi.UseItemStyleForSubItems = false;
                    _lvStocks.Items.Add(lvi);
                }
                _lvStocks.EndUpdate();
            }
            finally
            {
                _isUpdatingUi = false;
            }

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

            // 사용자가 기존에 선택했던 종목이 있으면 선택 유지, 없으면 첫 행 자동 클릭/포커스 없이 깔끔한 미선택 상태 유지
            ListViewItem restoredLvi = null;
            if (!string.IsNullOrEmpty(prevSelectedCode))
            {
                string normPrev = prevSelectedCode.TrimStart('0');
                foreach (ListViewItem lvi in _lvStocks.Items)
                {
                    if (lvi.Tag is StockHoldingItem itm && itm.StockCode.TrimStart('0').Equals(normPrev, StringComparison.OrdinalIgnoreCase))
                    {
                        restoredLvi = lvi;
                        break;
                    }
                }
            }

            if (restoredLvi != null)
            {
                restoredLvi.Selected = true;
                UpdateDetailPanel(restoredLvi.Tag as StockHoldingItem);
            }
            else
            {
                _lvStocks.SelectedItems.Clear();
                if (_lvStocks.FocusedItem != null) _lvStocks.FocusedItem.Focused = false;
                UpdateDetailPanel(null);
            }

            // 5. 오늘의 AI 알짜 추천주 (0~3개) 배너 실시간 갱신
            UpdateTopPickBanner();
        }

        public void SetFilterMode(StockFilterMode mode)
        {
            _filterMode = mode;
            UpdateFilterButtonStyles();
            UpdateUi();
        }

        private void UpdateFilterButtonStyles()
        {
            UpdateButtonTabStyle(_btnFilterAll, StockFilterMode.All);
            UpdateButtonTabStyle(_btnFilterHolding, StockFilterMode.HoldingOnly);
            UpdateButtonTabStyle(_btnFilterWatchlist, StockFilterMode.WatchlistOnly);
        }

        private void UpdateButtonTabStyle(Button btn, StockFilterMode mode)
        {
            if (btn == null) return;
            bool active = _filterMode == mode;
            btn.BackColor = active ? Color.FromArgb(18, 52, 74) : Color.FromArgb(24, 28, 38);
            btn.ForeColor = active ? Color.FromArgb(0, 216, 255) : Color.FromArgb(140, 160, 185);
            btn.FlatAppearance.BorderColor = active ? Color.FromArgb(0, 216, 255) : Color.FromArgb(42, 54, 72);
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
                _lblDetailTitle.Text = "📊 포트폴리오 리스크 & 시장 요약 브리핑";

                // 매수추천 종목 및 리스크 통계 탐색
                StockHoldingItem topPick = null;
                int dangerCount = 0;
                int buyCount = 0;
                int holdCount = 0;
                int watchCount = 0;

                if (_portfolio?.Items != null)
                {
                    foreach (var p in _portfolio.Items)
                    {
                        if (p.Quantity > 0) holdCount++;
                        else watchCount++;

                        var v = StockVerdictHelper.Evaluate(p);
                        if (v.BadgeText.Contains("매수추천"))
                        {
                            buyCount++;
                            if (topPick == null) topPick = p;
                        }
                        else if (v.BadgeText.Contains("급락") || v.BadgeText.Contains("비중축소") || v.BadgeText.Contains("매수금지") || p.ChangeRate <= -3.0m)
                        {
                            dangerCount++;
                        }
                    }
                }

                if (_lblFinalVerdict != null)
                {
                    if (topPick != null)
                    {
                        _lblFinalVerdict.Text = $"【 오늘의 알짜 탑픽 추천 】 : {topPick.StockName} ({topPick.CurrentPrice:N0}원)\n20일 눌림목 및 퀀트 통과 우량주 - 분할 진입 가이드 확인 권고";
                        _lblFinalVerdict.BackColor = Color.FromArgb(18, 48, 32);
                        _lblFinalVerdict.ForeColor = Color.FromArgb(100, 255, 180);
                    }
                    else
                    {
                        string thermoLabel = _marketTemperature?.BadgeText ?? "체온 31.0°C [공포]";
                        _lblFinalVerdict.Text = $"【 시장 리스크 관리 모드 】 : {thermoLabel}\n폭락장 뇌동매매 엄금 - 현금 비중 유지 및 손절 방어선 준수";
                        _lblFinalVerdict.BackColor = Color.FromArgb(44, 32, 20);
                        _lblFinalVerdict.ForeColor = Color.FromArgb(255, 215, 100);
                    }
                }

                string thermoStr = _marketTemperature?.BadgeText ?? "체온 31.0°C [공포]";
                _lblDetailPriceTargets.Text = $"• 계좌 현황: 보유 {holdCount}종목 | 관심 {watchCount}종목\n• 시장 진단: {thermoStr} (보수적 대응 권고)";
                _lblDetailSummary.Text = $"• 매수 추천: {buyCount}종목  |  급락·위험 종목: {dangerCount}종목 감지\n• 항목 클릭 시 1초 만에 14대 지표 상세 리포트가 열람됩니다.";
                _miniChart?.SetData(null);
                RenderEmptyReport(topPick, holdCount, watchCount, dangerCount, buyCount);
                return;
            }

            // AI 참모 최종 투자 결론 및 행동 판정 산출
            StockVerdictInfo verdict = StockVerdictHelper.Evaluate(item);

            // 1. 종목명 및 현재가 (전일대비 등락률 표시 및 증권앱 주문설정 뱃지 연동)
            string chgSign = item.ChangeRate > 0 ? "+" : "";
            string orderBadge = item.IsBrokerOrderSet
                ? (item.IsWatchlist ? "  [✓예약매수]" : "  [✓자동청산]")
                : "  [앱주문 미설정]";
            _lblDetailTitle.Text = $"{item.StockName} ({item.StockCode}) | {item.CurrentPrice:N0}원 ({chgSign}{item.ChangeRate:F2}%){orderBadge}";

            // 2. AI 참모 최종 결론 바 (0.1초 직관 배지 + 1줄 행동 지침)
            if (_lblFinalVerdict != null)
            {
                _lblFinalVerdict.Text = $"{verdict.TitleText}\n{verdict.GuideText}";
                _lblFinalVerdict.BackColor = verdict.BackColor;
                _lblFinalVerdict.ForeColor = verdict.ForeColor;
            }

            // 3. 핵심 가격 타점 (목표가 / 손절가 / 권고 매수가 단일 3단 표시)
            decimal targetPrice = item.RecommendedTargetPrice > 0 ? item.RecommendedTargetPrice : item.ResistancePrice;
            decimal targetUp = (item.CurrentPrice > 0 && targetPrice > item.CurrentPrice)
                ? ((targetPrice - item.CurrentPrice) / item.CurrentPrice * 100m) : 0m;
            string targetStr = targetPrice > 0 ? $"{targetPrice:N0}원 (+{targetUp:F1}%)" : "-";

            // 손절가 단일화 (TechnicalIndicatorService에서 산출된 TargetStopPrice 기준 단일가)
            decimal stopPrice = item.TargetStopPrice > 0 ? item.TargetStopPrice : (item.SupportPrice > 0 ? item.SupportPrice * 0.97m : item.CurrentPrice * 0.93m);
            stopPrice = Math.Round(stopPrice, 0);
            decimal stopDown = (item.CurrentPrice > 0 && stopPrice > 0)
                ? ((stopPrice - item.CurrentPrice) / item.CurrentPrice * 100m) : 0m;
            string stopStr = stopPrice > 0 ? $"{stopPrice:N0}원 ({stopDown:F1}%)" : "-";

            _lblDetailPriceTargets.Text =
                $"{verdict.BuyPriceText}\n" +
                $"• 목표가: {targetStr}   |   손절가: {stopStr}";

            // 4. 진단 스펙 & 수급 지뢰 요약 (권고 액션은 상단 결론 바가 전담하므로 중복 제거)
            var supplyInfo = SupplyHelper.GetSupplyInfo(item);
            string supBadge = supplyInfo.Text;
            string rawMineBadge = TextCleanHelper.StripEmoji(!string.IsNullOrEmpty(item.LandmineBadge) ? item.LandmineBadge : "클린안전");
            var landmineInfo = LandmineHelper.GetSimplifiedBadge(item);
            string mineBadgeDisplay = $"[{landmineInfo.Text}] {rawMineBadge}";
            var diag = item.LastDiagnosis;

            _lblDetailSummary.Text =
                $"• AI 등급: {GradeHelper.GetGradeDescription(item.Grade)}  |  종합 점수: {item.FinalScore}점 / 10점\n" +
                $"• 수급: {supBadge}  |  지뢰: {mineBadgeDisplay}  |  세부: 차트{item.ChartScore}·수급{item.SupplyFinanceScore}·뉴스{item.NewsScore}·손익{item.RiskScore}";

            // 5. 20일 일봉 & AI 타점 미니 차트 갱신
            _miniChart?.SetData(item);

            // 하단 심층 리포트 렌더링
            if (diag != null)
            {
                RenderRichDetailReport(item, diag);
            }
            else
            {
                RenderRichPreDiagnosisReport(item);
            }
        }

        private void RenderEmptyReport(StockHoldingItem topPick, int holdCount, int watchCount, int dangerCount, int buyCount)
        {
            if (_rtbDetailReason == null) return;
            _rtbDetailReason.SuspendLayout();
            _rtbDetailReason.Clear();

            AppendText("【 AI 주식 리스크 참모 - 통합 종합 브리핑 】\n\n", RtbSectionFont, Color.FromArgb(0, 230, 255));

            AppendText("1. 시장 환경 및 대응 원칙\n", RtbSectionFont, Color.FromArgb(255, 215, 64));
            string tempLabel = _marketTemperature?.BadgeText ?? "체온 31.0°C [공포]";
            AppendText($"• 현재 시장 상태: {tempLabel}\n", RtbBodyFont, Color.FromArgb(220, 230, 245));
            AppendText("• 하락/공포장에서는 지표 1~2개만 보고 섣불리 물타기하면 위험합니다. 20일선 지지선과 퀀트 8대 필터를 엄격히 통과한 종목에만 분할 진입하십시오.\n\n", RtbBodyFont, Color.FromArgb(180, 195, 215));

            AppendText("2. 내 계좌 및 관심종목 상태\n", RtbSectionFont, Color.FromArgb(255, 215, 64));
            AppendText($"• 총 보유 {holdCount}종목 중 손절선 위협 및 급락주의 종목이 {dangerCount}건 감지되었습니다.\n", RtbBodyFont, dangerCount > 0 ? Color.FromArgb(255, 120, 120) : Color.FromArgb(100, 240, 160));
            AppendText($"• 엄격한 안전벨트 기준을 통과한 매수추천 종목은 총 {buyCount}건입니다.\n\n", RtbBodyFont, buyCount > 0 ? Color.FromArgb(100, 255, 180) : Color.FromArgb(180, 195, 215));

            if (topPick != null)
            {
                AppendText("3. 오늘의 핵심 관심 종목 (Top Pick)\n", RtbSectionFont, Color.FromArgb(0, 230, 255));
                AppendText($"• 종목명: {topPick.StockName} ({topPick.CurrentPrice:N0}원, {topPick.ChangeRate:+#0.00;-#0.00;0.00}%)\n", RtbBodyFont, Color.FromArgb(255, 230, 100));
                AppendText($"• AI 판정: {topPick.TimingBadge} | 종합점수 {topPick.FinalScore}점\n\n", RtbBodyFont, Color.FromArgb(180, 220, 245));
            }

            AppendText("💡 [조작 안내]\n", RtbSectionFont, Color.FromArgb(140, 200, 255));
            AppendText("좌측 종목 목록에서 원하는 종목을 클릭하시면, 해당 종목의 일봉 차트, 목표가/손절가 타점, 외인·기관 수급, DART 지뢰 공시 심층 분석 리포트가 즉시 표시됩니다.\n더블클릭 시 8대 정통 퀀트 전수 검증창이 열립니다.", RtbBodyFont, Color.FromArgb(160, 180, 205));

            _rtbDetailReason.ResumeLayout();
        }

        private void RenderRichPreDiagnosisReport(StockHoldingItem item)
        {
            if (_rtbDetailReason == null) return;
            _rtbDetailReason.SuspendLayout();
            _rtbDetailReason.Clear();

            // 네이버 시세 수집 실패 경고 박스
            if (!item.IsPriceScrapeSuccess)
            {
                AppendText("--------------------------------------------------------------------------------\n", RtbDividerFont, Color.FromArgb(255, 75, 75));
                AppendText("  [네이버 시세 실시간 수집 실패 경고]\n", RtbHeaderBoldFont, Color.FromArgb(255, 95, 95));
                AppendText($"  • 실패 사유 : {item.ScrapeErrorMessage}\n", RtbSubFont, Color.FromArgb(255, 170, 170));
                AppendText($"  • 주의 : 표시된 시세({item.CurrentPrice:N0}원)는 실시간이 아닌 이전 캐시 데이터입니다.\n", RtbSubBoldFont, Color.FromArgb(255, 215, 100));
                AppendText("--------------------------------------------------------------------------------\n\n", RtbDividerFont, Color.FromArgb(255, 75, 75));
            }

            AppendText("--------------------------------------------------------------------------------\n", RtbDividerFont, Color.FromArgb(60, 75, 100));
            AppendText($"  [정량 기술 지표 진단 완료]  {DateTime.Now:yyyy-MM-dd HH:mm}\n", RtbHeaderBoldFont, Color.FromArgb(140, 190, 240));
            AppendText("--------------------------------------------------------------------------------\n\n", RtbDividerFont, Color.FromArgb(60, 75, 100));

            AppendText("【 정량 지표 분석 요약 】\n", RtbSectionFont, Color.FromArgb(100, 215, 255));
            string suppStr = item.SupportPrice > 0 ? $"{item.SupportPrice:N0}원" : "-";
            string resisStr = item.ResistancePrice > 0 ? $"{item.ResistancePrice:N0}원" : "-";
            AppendText($"  • 14일 RSI {item.Rsi:F1}, 5일 거래량 {item.VolumeRatio:F0}%, 20일 지지선 {suppStr}, 저항선 {resisStr}을 분석하여 상단 목표가 및 손절가가 도출되었습니다.\n\n", RtbBodyFont, Color.FromArgb(220, 225, 240));

            AppendText("【 안내 】\n", RtbSectionFont, Color.FromArgb(255, 215, 100));
            AppendText("  • 상단의 [AI 참모 진단] 버튼을 클릭하시면 실시간 외신 뉴스 및 공시를 결합한 심층 대응 리포트가 완성됩니다.\n\n", RtbBodyFont, Color.FromArgb(200, 225, 255));

            if (!string.IsNullOrWhiteSpace(item.Memo))
            {
                AppendText("【 사용자 투자 메모 】\n", RtbSectionFont, Color.FromArgb(240, 200, 140));
                AppendText($"  • {item.Memo}\n", RtbBodyFont, Color.FromArgb(225, 230, 240));
            }

            _rtbDetailReason.Select(0, 0);
            _rtbDetailReason.ScrollToCaret();
            _rtbDetailReason.ResumeLayout();
        }

        private void RenderRichDetailReport(StockHoldingItem item, StockRiskDiagnosis diag)
        {
            if (_rtbDetailReason == null) return;
            _rtbDetailReason.SuspendLayout();
            _rtbDetailReason.Clear();

            // 네이버 시세 수집 실패 경고 박스
            if (!item.IsPriceScrapeSuccess)
            {
                AppendText("--------------------------------------------------------------------------------\n", RtbDividerFont, Color.FromArgb(255, 75, 75));
                AppendText("  [네이버 시세 실시간 수집 실패 경고]\n", RtbHeaderBoldFont, Color.FromArgb(255, 95, 95));
                AppendText($"  • 실패 사유 : {item.ScrapeErrorMessage}\n", RtbSubFont, Color.FromArgb(255, 170, 170));
                AppendText($"  • 주의 : 표시된 시세({item.CurrentPrice:N0}원)는 실시간이 아닌 이전 캐시 데이터입니다.\n", RtbSubBoldFont, Color.FromArgb(255, 215, 100));
                AppendText("--------------------------------------------------------------------------------\n\n", RtbDividerFont, Color.FromArgb(255, 75, 75));
            }

            AppendText("--------------------------------------------------------------------------------\n", RtbDividerFont, Color.FromArgb(60, 75, 100));
            AppendText($"  [AI 주식 참모 심층 진단 리포트]  {diag.DiagnosedAt:yyyy-MM-dd HH:mm}\n", RtbHeaderBoldFont, Color.FromArgb(140, 190, 240));
            AppendText("--------------------------------------------------------------------------------\n\n", RtbDividerFont, Color.FromArgb(60, 75, 100));

            // 1. 거시 지표 및 정보 신뢰도 (1~2줄 압축 요약)
            string stars = new string('★', diag.SourceReliability) + new string('☆', 5 - diag.SourceReliability);
            string impactDesc = diag.PolicyImpact > 0 ? "호재성 정책/실적 수혜" : (diag.PolicyImpact < 0 ? "악재성 규제/비용 충격" : "중립 영향");
            AppendText($"• 정보 신뢰도 : {stars} ({diag.SourceReliability} / 5점)  |  정책 영향 : {(diag.PolicyImpact > 0 ? "+" : "")}{diag.PolicyImpact}점 ({impactDesc})\n\n", RtbHeaderBoldFont, Color.FromArgb(180, 215, 245));

            // 2. 심층 대응 논리 및 판단 근거 (본문 중심)
            AppendText("【 AI 참모 심층 대응 논리 및 판단 근거 】\n", RtbSectionFont, Color.FromArgb(0, 230, 255));
            string reasonText = diag.ReasonSummary ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(reasonText))
            {
                var sentences = reasonText.Split(new[] { ". ", ".\n", ".\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var s in sentences)
                {
                    string trimmed = s.Trim().TrimEnd('.');
                    if (!string.IsNullOrWhiteSpace(trimmed))
                    {
                        AppendText($"  • {trimmed}.\n\n", RtbBodyFont, Color.FromArgb(235, 240, 252));
                    }
                }
            }
            else
            {
                AppendText("  • 상세 대응 논리가 기록되지 않았습니다.\n\n", RtbBodyFont, Color.FromArgb(180, 190, 205));
            }

            // 3. 사용자 투자 메모
            if (!string.IsNullOrWhiteSpace(item.Memo))
            {
                AppendText("【 사용자 투자 메모 】\n", RtbSectionFont, Color.FromArgb(240, 200, 140));
                AppendText($"  • {item.Memo}\n\n", RtbBodyFont, Color.FromArgb(225, 230, 240));
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
                // 금액/수익률/지표/점수 컬럼(1~6번, 10~12번)은 높은 값(내림차순) 우선
                // 7번(AI 등급: A->B->C->F) 및 텍스트 컬럼(0:종목명, 8:수급, 9:지뢰, 13:타이밍, 14:권고)은 오름차순 우선
                _sortOrder = ((e.Column >= 1 && e.Column <= 6) || (e.Column >= 10 && e.Column <= 12)) ? SortOrder.Descending : SortOrder.Ascending;
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
                case 0: // 종목명 (한글 가나다순)
                    result = string.Compare(itemX.StockName, itemY.StockName, StringComparison.CurrentCultureIgnoreCase);
                    break;
                case 1: // 현재가 (수치 정렬)
                    result = itemX.CurrentPrice.CompareTo(itemY.CurrentPrice);
                    break;
                case 2: // 전일대비 상승률 (수치 정렬)
                    result = itemX.ChangeRate.CompareTo(itemY.ChangeRate);
                    break;
                case 3: // 평단 (수치 정렬)
                    result = itemX.BuyPrice.CompareTo(itemY.BuyPrice);
                    break;
                case 4: // 보유수량 (수치 정렬)
                    result = itemX.Quantity.CompareTo(itemY.Quantity);
                    break;
                case 5: // 손익금(원) (수치 정렬)
                    result = itemX.ProfitAmount.CompareTo(itemY.ProfitAmount);
                    break;
                case 6: // 수익률 (수치 정렬)
                    result = itemX.ProfitRate.CompareTo(itemY.ProfitRate);
                    break;
                case 7: // AI 등급 (A, B, C, F)
                    result = itemX.Grade.CompareTo(itemY.Grade);
                    break;
                case 8: // 수급(외인·기관) (우선순위 정렬: 메이저 매수 > 외인/기관 연속 > 외인/기관 일반 > 수급공방 > 개미홀로 > 메이저 매도)
                    result = SupplyHelper.GetSupplyPriority(itemX).CompareTo(SupplyHelper.GetSupplyPriority(itemY));
                    break;
                case 9: // 지뢰검증 (간결 4단계 우선순위 정렬: 우수 > 안전 > 주의 > 위험)
                    result = LandmineHelper.GetLandminePriority(itemX).CompareTo(LandmineHelper.GetLandminePriority(itemY));
                    break;
                case 10: // 14일 RSI (수치 정렬)
                    result = itemX.Rsi.CompareTo(itemY.Rsi);
                    break;
                case 11: // 5일 거래량 (수치 정렬)
                    result = itemX.VolumeRatio.CompareTo(itemY.VolumeRatio);
                    break;
                case 12: // ★종합점수 (수치 정렬)
                    result = itemX.FinalScore.CompareTo(itemY.FinalScore);
                    break;
                case 13: // 타이밍 안전벨트
                    result = string.Compare(itemX.TimingBadge, itemY.TimingBadge, StringComparison.CurrentCultureIgnoreCase);
                    break;
                case 14: // 권고 지침 / 최종 액션 (우선순위 정렬: 매수추천 > 상승추세 > 과열주의 > 관망 > 급락주의 > 비중축소/매수금지)
                    string actX = lviX.SubItems.Count > 14 ? lviX.SubItems[14].Text : string.Empty;
                    string actY = lviY.SubItems.Count > 14 ? lviY.SubItems[14].Text : string.Empty;
                    int prioX = StockVerdictHelper.GetActionPriority(actX);
                    int prioY = StockVerdictHelper.GetActionPriority(actY);
                    result = prioX.CompareTo(prioY);
                    if (result == 0)
                    {
                        result = string.Compare(actX, actY, StringComparison.CurrentCultureIgnoreCase);
                    }
                    break;
                default:
                    result = 0;
                    break;
            }

            if (result == 0 && ColumnIndex != 0)
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
