using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using GateHelper.StockAdvisor.Models;
using GateHelper.StockAdvisor.Services;

namespace GateHelper.StockAdvisor
{
    /// <summary>
    /// 보유 여부와 무관하게 모든 종목을 즉시 검색하여
    /// 팩트 시세 + 14일 RSI + 외인/기관 수급 + 재무·공시 지뢰 + 정통 퀀트 8대 우량주 필터를 1초 만에 원스톱 진단하는 다이얼로그
    /// </summary>
    public partial class StockEvaluatorDialog : Form
    {
        // 결과 반환 (내 포트폴리오에 추가할 경우)
        public StockHoldingItem AddedStockItem { get; private set; }

        private TextBox _txtSearch;
        private Button _btnSearch;
        private Label _lblStatus;
        private StockSearchPopup _searchPopup;

        // 좌측: 기술적 AI 종합 점수 카드
        private Panel _pnlCard1;
        private Label _lblStockHeader;
        private Label _lblPriceAndRate;
        private Label _lblOverallScore;
        private Label _lblGradeBadge;
        private Label _lblThreeScores;
        private Label _lblTechnicalFacts;
        private Label _lblTimingGuide;

        // 중앙: 외인·기관 수급 & 재무·공시 지뢰 카드
        private Panel _pnlCard2;
        private Label _lblSupplyBadge;
        private Label _lblSupplyDetails;
        private Label _lblLandmineBadge;
        private Label _lblLandmineDetails;
        private Label _lblDisclosures;

        // 우측: 정통 퀀트 8대 황금 필터 체크리스트 카드
        private Panel _pnlCard3;
        private Label _lblQuantHeader;
        private ListView _lvQuantFilters;

        // 하단 버튼
        private Button _btnAddToPortfolio;
        private Button _btnOpenNaverWeb;
        private Button _btnClose;

        // 현재 진단 완료된 임시 데이터
        private StockHoldingItem _currentEvaluatedItem;

        public StockEvaluatorDialog(string initialQuery = "")
        {
            InitializeCustomComponent();
            if (!string.IsNullOrWhiteSpace(initialQuery))
            {
                _txtSearch.Text = initialQuery.Trim();
                this.Shown += async (s, e) => await ExecuteDiagnosisAsync(_txtSearch.Text);
            }
        }

        #region Search & Diagnosis Execution

        private async Task SearchAndDiagnoseAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return;
            string cleanQuery = query.Trim();

            // 1. 순수 6자리 숫자인 경우: 국내 종목코드로 즉시 직통 진단 실행
            if (cleanQuery.Length <= 6 && System.Text.RegularExpressions.Regex.IsMatch(cleanQuery, @"^\d+$"))
            {
                _searchPopup?.Close();
                await ExecuteDiagnosisAsync(cleanQuery);
                return;
            }

            _lblStatus.Text = $"🔍 [{cleanQuery}] 종목 후보 검색 중...";
            _lblStatus.ForeColor = Color.FromArgb(0, 230, 255);
            _btnSearch.Enabled = false;

            try
            {
                var searchResults = await NaverStockPriceService.SearchStockAsync(cleanQuery);
                if (searchResults == null || searchResults.Count == 0)
                {
                    _searchPopup?.Close();
                    _lblStatus.Text = $"[안내] '{cleanQuery}' 종목을 네이버 증권에서 찾을 수 없습니다.";
                    _lblStatus.ForeColor = Color.FromArgb(255, 160, 60);
                    return;
                }

                if (searchResults.Count == 1)
                {
                    _searchPopup?.Close();
                    _txtSearch.Text = searchResults[0].Name;
                    await ExecuteDiagnosisAsync(searchResults[0].Code);
                }
                else
                {
                    // 2건 이상인 경우: 검색 텍스트박스 바로 아래에 다크 테마 후보 리스트 팝업 표시
                    _lblStatus.Text = $"🔍 '{cleanQuery}' 검색 결과 {searchResults.Count}건. 아래 목록에서 원하는 종목을 선택하세요 (클릭 또는 Enter).";
                    _lblStatus.ForeColor = Color.FromArgb(120, 220, 255);
                    _searchPopup.ShowResults(_txtSearch, searchResults);
                }
            }
            catch (Exception ex)
            {
                _lblStatus.Text = $"검색 중 오류 발생: {ex.Message}";
                _lblStatus.ForeColor = Color.FromArgb(255, 110, 110);
            }
            finally
            {
                _btnSearch.Enabled = true;
            }
        }

        private async Task ExecuteDiagnosisAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return;
            _searchPopup?.Close();

            string cleanQuery = query.Trim();
            _lblStatus.Text = $"⏳ [{cleanQuery}] 실시간 시세, 외인·기관 수급, DART 공시, 정통 퀀트 8대 팩트 수집 중...";
            _lblStatus.ForeColor = Color.FromArgb(0, 230, 255);
            _btnSearch.Enabled = false;

            try
            {
                // 1. 네이버 시세 및 종목 정보 조회
                var priceInfo = await NaverStockPriceService.FetchPriceAsync(cleanQuery);
                if (!priceInfo.IsSuccess)
                {
                    _lblStatus.Text = $"[오류] 네이버 증권 시세 수집 실패: {priceInfo.ErrorMessage ?? "종목명 또는 6자리 코드를 확인해주세요."}";
                    _lblStatus.ForeColor = Color.FromArgb(255, 90, 90);
                    _btnAddToPortfolio.Enabled = false;
                    _btnOpenNaverWeb.Enabled = false;
                    return;
                }

                string stockCode = priceInfo.StockCode;
                string stockName = priceInfo.StockName;
                decimal currentPrice = priceInfo.CurrentPrice;
                decimal changeRate = priceInfo.ChangeRate;

                if (!string.IsNullOrWhiteSpace(stockName))
                {
                    _txtSearch.Text = stockName;
                }

                // 2. 기술적 지표, 수급·지뢰 인텔, 정통 퀀트 병렬 비동기 수집
                var techTask = TechnicalIndicatorService.AnalyzeIndicatorsAsync(stockCode, currentPrice);
                var intelTask = StockIntelligenceService.AnalyzeAsync(stockCode);

                await Task.WhenAll(techTask, intelTask);

                var tech = techTask.Result;
                var intel = intelTask.Result;

                // 3. 정통 퀀트 8대 필터 분석 (14일 RSI 연동)
                var quant = await StockIntelligenceService.AnalyzeQuantAsync(stockCode, tech.Rsi);

                // 4. 단일 기준(Single Source of Truth) 통합 4대 팩터 점수 산출
                var scoreRes = StockIntelligenceService.CalculateUnifiedScores(tech, intel, 0);

                // AI 등급 (보유 여부와 무관한 순수 시장 팩터 객관적 등급)
                PositionGrade grade = scoreRes.Grade;
                int finalScore = scoreRes.FinalScore;

                // 임시 종목 아이템 구성 (미보유 상태 Quantity = 0)
                _currentEvaluatedItem = new StockHoldingItem
                {
                    StockCode = stockCode,
                    StockName = stockName,
                    CurrentPrice = currentPrice,
                    ChangeRate = changeRate,
                    BuyPrice = currentPrice,
                    Quantity = 0,
                    ChartScore = scoreRes.ChartScore,
                    SupplyFinanceScore = scoreRes.SupplyFinanceScore,
                    NewsScore = scoreRes.NewsScore,
                    RiskScore = scoreRes.RiskScore,
                    FinalScore = scoreRes.FinalScore,
                    Rsi = tech.Rsi,
                    VolumeRatio = tech.VolumeRatio,
                    SupportPrice = tech.SupportPrice,
                    RecentCandles = tech.Candles,
                    ResistancePrice = tech.ResistancePrice,
                    DownsideRiskPercent = tech.DownsideRiskPercent,
                    RecommendedBuyPrice = tech.RecommendedBuyPrice,
                    SecondaryBuyPrice = tech.SecondaryBuyPrice,
                    TargetStopPrice = tech.TargetStopPrice,
                    EntryTimingBadge = tech.EntryTimingBadge,
                    EntryTimingGuide = tech.EntryTimingGuide,
                    RecommendedTargetPrice = tech.RecommendedTargetPrice,
                    SecondaryTargetPrice = tech.SecondaryTargetPrice,
                    ExitTimingBadge = tech.ExitTimingBadge,
                    ExitTimingGuide = tech.ExitTimingGuide,
                    IsPriceScrapeSuccess = priceInfo.IsSuccess,
                    ScrapeErrorMessage = priceInfo.ErrorMessage,
                    LastScrapedTime = priceInfo.ScrapedAt,
                    TimingBadge = tech.TimingBadge,
                    TimingWarning = tech.TimingWarning,
                    MajorSupplyBadge = intel.SupplyBadge,
                    MajorSupplyDetail = intel.SupplyDetail,
                    LandmineBadge = intel.LandmineBadge,
                    LandmineDetail = intel.LandmineDetail,
                    HasCriticalLandmine = intel.HasCriticalLandmine,
                    DebtRatio = intel.DebtRatio,
                    ConsecutiveLossYears = intel.ConsecutiveLossYears,
                    QuantBadge = quant.QuantGradeBadge,
                    QuantSummary = $"정통 퀀트 {quant.PassCount}/{quant.TotalCount} PASS"
                };

                // UI 3개 카드에 렌더링
                RenderResults(priceInfo, tech, intel, quant, finalScore, grade);

                _lblStatus.Text = $"✔ [{stockName} ({stockCode})] 종합 진단 완료 (시세·수급·지뢰·정통 퀀트 전수 검증)";
                _lblStatus.ForeColor = Color.FromArgb(100, 255, 180);
                _btnAddToPortfolio.Enabled = true;
                _btnOpenNaverWeb.Enabled = true;
            }
            catch (Exception ex)
            {
                _lblStatus.Text = $"[오류] 진단 중 예외 발생: {ex.Message}";
                _lblStatus.ForeColor = Color.FromArgb(255, 100, 100);
            }
            finally
            {
                _btnSearch.Enabled = true;
            }
        }

        private void RenderResults(StockPriceDto price, TechnicalIndicatorService.IndicatorAnalysisResult tech, SupplyAndLandmineDto intel, QuantAnalysisResult quant, int finalScore, PositionGrade grade)
        {
            // 카드 1 갱신
            _lblStockHeader.Text = $"{price.StockName} ({price.StockCode})";
            string chgPrefix = price.ChangeRate > 0 ? "+" : "";
            _lblPriceAndRate.Text = $"현재가: {price.CurrentPrice:N0}원 ({chgPrefix}{price.ChangeRate:F2}%)";
            _lblPriceAndRate.ForeColor = price.ChangeRate > 0 ? Color.FromArgb(255, 95, 95) : (price.ChangeRate < 0 ? Color.FromArgb(85, 160, 255) : Color.White);

            _lblOverallScore.Text = $"최종 종합 점수 : {finalScore}점 / 10점";
            _lblOverallScore.ForeColor = finalScore >= 8 ? Color.FromArgb(255, 215, 0) : (finalScore <= 2 ? Color.FromArgb(255, 95, 95) : Color.FromArgb(255, 225, 120));

            _lblGradeBadge.Text = $"AI 등급: {GradeHelper.GetGradeDescription(grade)}";
            _lblGradeBadge.ForeColor = GradeHelper.GetGradeColor(grade);

            int sfScore = _currentEvaluatedItem != null ? _currentEvaluatedItem.SupplyFinanceScore : 5;
            int newsScore = _currentEvaluatedItem != null ? _currentEvaluatedItem.NewsScore : 5;
            int riskScore = _currentEvaluatedItem != null ? _currentEvaluatedItem.RiskScore : tech.RiskScore;
            _lblThreeScores.Text = $"• 차트: {tech.ChartScore}점 | 수급·재무: {sfScore}점 | 뉴스: {newsScore}점 | 손익비: {riskScore}점";
            
            string buyPriceStr = tech.RecommendedBuyPrice > 0 ? $"{tech.RecommendedBuyPrice:N0}원" : "시장가 분할";
            string secBuyStr = tech.SecondaryBuyPrice > 0 ? $" | 2차: {tech.SecondaryBuyPrice:N0}원" : "";
            string stopPriceStr = tech.TargetStopPrice > 0 ? $"{tech.TargetStopPrice:N0}원" : "20일선 이탈";
            decimal upside1 = (price.CurrentPrice > 0 && tech.RecommendedTargetPrice > price.CurrentPrice)
                ? ((tech.RecommendedTargetPrice - price.CurrentPrice) / price.CurrentPrice * 100m) : 0m;

            _lblTechnicalFacts.Text = $"• 14일 RSI: {tech.Rsi:F1} | 5일 거래량: {tech.VolumeRatio:F0}%\n" +
                                       $"• 20일 지지선: {(tech.SupportPrice > 0 ? $"{tech.SupportPrice:N0}원" : "-")} (하방 -{tech.DownsideRiskPercent:F1}%)\n" +
                                       $"• 1차 매수가: {buyPriceStr}{secBuyStr} (방어선: {stopPriceStr})\n" +
                                       $"• 1차 목표가: {tech.RecommendedTargetPrice:N0}원 (+{upside1:F1}%) [2차: {tech.SecondaryTargetPrice:N0}원]";

            // AI 참모 최종 결론 & 행동 판정 (미보유 종목 전용 객관적 진입/관망 가이드)
            StockVerdictInfo verdict = StockVerdictHelper.Evaluate(_currentEvaluatedItem);
            string exitGuide = !string.IsNullOrEmpty(tech.ExitTimingGuide) ? tech.ExitTimingGuide : tech.ExitTimingBadge;
            _lblTimingGuide.Text = $"{verdict.TitleText}\n{verdict.GuideText}\n\n【 청산/방어 지침 : {tech.ExitTimingBadge} 】\n{exitGuide}";
            _lblTimingGuide.BackColor = verdict.BackColor;
            _lblTimingGuide.ForeColor = verdict.ForeColor;

            // 카드 2 갱신
            _lblSupplyBadge.Text = $"수급 주체: {intel.SupplyBadge}";
            _lblSupplyBadge.ForeColor = intel.SupplyBadge.Contains("쌍끌이") ? Color.FromArgb(0, 240, 210) : Color.FromArgb(180, 210, 240);

            _lblSupplyDetails.Text = $"• {intel.SupplyDetail}\n• 최근 순매수 : 외인 {intel.ForeignNetQuantity:+0;-0;0} | 기관 {intel.OrganNetQuantity:+0;-0;0}";

            _lblLandmineBadge.Text = $"지뢰 검증: {intel.LandmineBadge}";
            _lblLandmineBadge.ForeColor = intel.HasCriticalLandmine ? Color.FromArgb(255, 95, 95) : Color.FromArgb(100, 240, 140);

            _lblLandmineDetails.Text = $"• {intel.LandmineDetail}\n• 부채비율 {intel.DebtRatio:F1}% | 연속적자 {intel.ConsecutiveLossYears}개년";

            _lblDisclosures.Text = string.IsNullOrEmpty(intel.RecentDisclosuresSummary)
                ? "• 최근 20건 DART 공시 중 특이 지뢰 사항 없음 (정상)"
                : $"• 최신 공시: {intel.RecentDisclosuresSummary}";

            // 카드 3 갱신 (정통 퀀트 8대 필터 리스트뷰)
            _lblQuantHeader.Text = $"통과율: {quant.PassCount} / {quant.TotalCount} 통과 ({quant.PassPercent:F0}%)";
            _lblQuantHeader.ForeColor = quant.PassCount >= 7 ? Color.FromArgb(255, 215, 0) : (quant.PassCount >= 4 ? Color.FromArgb(100, 240, 160) : Color.FromArgb(255, 120, 120));

            _lvQuantFilters.Items.Clear();
            foreach (var itm in quant.Items)
            {
                string passMark = itm.IsPassed ? "PASS" : "FAIL";
                var lvi = new ListViewItem(new[]
                {
                    itm.Name,                   // 1열: 검증항목 (앞에 E. C. 등 알파벳 완전 제거)
                    itm.Criteria,               // 2열: 조건값
                    itm.ActualValue,            // 3열: 현재값
                    passMark                    // 4열: 검증결과
                });
                lvi.UseItemStyleForSubItems = false;
                lvi.SubItems[0].ForeColor = Color.FromArgb(220, 235, 255);
                lvi.SubItems[1].ForeColor = Color.FromArgb(170, 195, 225);
                lvi.SubItems[2].ForeColor = itm.IsPassed ? Color.FromArgb(120, 240, 255) : Color.FromArgb(255, 170, 170);
                lvi.SubItems[3].ForeColor = itm.IsPassed ? Color.FromArgb(100, 255, 160) : Color.FromArgb(255, 95, 95);
                _lvQuantFilters.Items.Add(lvi);
            }
            _lvQuantFilters.SelectedItems.Clear();
            if (_lvQuantFilters.FocusedItem != null) _lvQuantFilters.FocusedItem.Focused = false;
        }

        private void AddCurrentStockToPortfolio()
        {
            if (_currentEvaluatedItem == null) return;

            // 보유종목 또는 관심종목을 선택하여 추가할 수 있는 입력 다이얼로그
            using (var prompt = new Form())
            {
                prompt.Width = 400;
                prompt.Height = 265;
                prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
                prompt.Text = $"내 포트폴리오에 추가: {_currentEvaluatedItem.StockName}";
                prompt.StartPosition = FormStartPosition.CenterParent;
                prompt.BackColor = Color.FromArgb(24, 28, 40);
                prompt.ForeColor = Color.White;
                prompt.Font = new Font("Malgun Gothic", 9f);
                prompt.MaximizeBox = false;
                prompt.MinimizeBox = false;

                var lblType = new Label { Left = 20, Top = 20, Text = "등록 구분:", AutoSize = true, Font = new Font("Malgun Gothic", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(0, 230, 255) };
                var rdoHolding = new RadioButton { Left = 130, Top = 18, Text = "보유 종목", AutoSize = true, Checked = true, ForeColor = Color.White, Cursor = Cursors.Hand };
                var rdoWatchlist = new RadioButton { Left = 225, Top = 18, Text = "관심 종목 (0주)", AutoSize = true, Checked = false, ForeColor = Color.FromArgb(0, 220, 255), Cursor = Cursors.Hand };

                var lbl1 = new Label { Left = 20, Top = 60, Text = "매수 평단가(원):", AutoSize = true, ForeColor = Color.FromArgb(200, 220, 245) };
                var txtBuyPrice = new TextBox { Left = 150, Top = 56, Width = 200, BackColor = Color.FromArgb(14, 18, 26), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Text = _currentEvaluatedItem.CurrentPrice.ToString("N0") };

                var lbl2 = new Label { Left = 20, Top = 98, Text = "보유 수량(주):", AutoSize = true, ForeColor = Color.FromArgb(200, 220, 245) };
                var txtQty = new TextBox { Left = 150, Top = 94, Width = 200, BackColor = Color.FromArgb(14, 18, 26), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Text = "1" };

                var lblGuide = new Label { Left = 20, Top = 132, Width = 345, Height = 22, Text = "※ 보유 종목으로 등록되어 실시간 수익률 및 목표/손절가가 추적됩니다.", Font = new Font("Malgun Gothic", 8.2f), ForeColor = Color.FromArgb(160, 195, 230) };

                var btnConfirm = CreateNeonButton("포트폴리오에 추가", Color.FromArgb(16, 48, 36), Color.FromArgb(0, 230, 118), Color.FromArgb(215, 255, 230));
                btnConfirm.Left = 65;
                btnConfirm.Top = 166;
                btnConfirm.Width = 165;
                btnConfirm.Height = 36;
                btnConfirm.Font = new Font("Malgun Gothic", 9.2f, FontStyle.Bold);

                var btnCancel = CreateNeonButton("취소", Color.FromArgb(32, 38, 52), Color.FromArgb(90, 110, 140), Color.FromArgb(200, 215, 230));
                btnCancel.Left = 240;
                btnCancel.Top = 166;
                btnCancel.Width = 90;
                btnCancel.Height = 36;
                btnCancel.Click += (s, e) => { prompt.DialogResult = DialogResult.Cancel; prompt.Close(); };

                EventHandler updateMode = (s, e) =>
                {
                    if (rdoWatchlist.Checked)
                    {
                        txtBuyPrice.Text = "0";
                        txtBuyPrice.Enabled = false;
                        txtQty.Text = "0";
                        txtQty.Enabled = false;
                        lblGuide.Text = "※ 관심 종목(0주)으로 등록되어 AI 적정 매수가 도달 시 알림이 울립니다.";
                        lblGuide.ForeColor = Color.FromArgb(0, 220, 255);
                    }
                    else
                    {
                        txtBuyPrice.Enabled = true;
                        txtQty.Enabled = true;
                        if (txtBuyPrice.Text == "0") txtBuyPrice.Text = _currentEvaluatedItem.CurrentPrice.ToString("N0");
                        if (txtQty.Text == "0") txtQty.Text = "1";
                        lblGuide.Text = "※ 보유 종목으로 등록되어 실시간 수익률 및 목표/손절가가 추적됩니다.";
                        lblGuide.ForeColor = Color.FromArgb(160, 195, 230);
                    }
                };
                rdoHolding.CheckedChanged += updateMode;
                rdoWatchlist.CheckedChanged += updateMode;

                btnConfirm.Click += (s, e) =>
                {
                    if (rdoWatchlist.Checked)
                    {
                        _currentEvaluatedItem.BuyPrice = 0m;
                        _currentEvaluatedItem.Quantity = 0;
                        if (string.IsNullOrEmpty(_currentEvaluatedItem.Memo))
                            _currentEvaluatedItem.Memo = "종목 평가에서 관심 종목으로 등록";
                    }
                    else
                    {
                        string pRaw = txtBuyPrice.Text.Replace(",", "").Trim();
                        string qRaw = txtQty.Text.Replace(",", "").Trim();
                        decimal.TryParse(pRaw, out decimal buyPrice);
                        int.TryParse(qRaw, out int qty);

                        if (qty <= 0)
                        {
                            // 수량에 0을 입력한 경우에도 관심 종목으로 자연스럽게 처리
                            _currentEvaluatedItem.BuyPrice = 0m;
                            _currentEvaluatedItem.Quantity = 0;
                            if (string.IsNullOrEmpty(_currentEvaluatedItem.Memo))
                                _currentEvaluatedItem.Memo = "종목 평가에서 관심 종목으로 등록";
                        }
                        else
                        {
                            if (buyPrice <= 0) buyPrice = _currentEvaluatedItem.CurrentPrice;
                            _currentEvaluatedItem.BuyPrice = buyPrice;
                            _currentEvaluatedItem.Quantity = qty;
                        }
                    }

                    this.AddedStockItem = _currentEvaluatedItem;
                    prompt.DialogResult = DialogResult.OK;
                    prompt.Close();
                };

                prompt.Controls.Add(lblType);
                prompt.Controls.Add(rdoHolding);
                prompt.Controls.Add(rdoWatchlist);
                prompt.Controls.Add(lbl1);
                prompt.Controls.Add(txtBuyPrice);
                prompt.Controls.Add(lbl2);
                prompt.Controls.Add(txtQty);
                prompt.Controls.Add(lblGuide);
                prompt.Controls.Add(btnConfirm);
                prompt.Controls.Add(btnCancel);

                if (prompt.ShowDialog(this) == DialogResult.OK)
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
        }

        private void OpenNaverWeb()
        {
            if (_currentEvaluatedItem == null || string.IsNullOrWhiteSpace(_currentEvaluatedItem.StockCode)) return;
            try
            {
                string url = $"https://m.stock.naver.com/domestic/stock/{_currentEvaluatedItem.StockCode}";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"웹 브라우저를 열 수 없습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        #endregion
    }
}
