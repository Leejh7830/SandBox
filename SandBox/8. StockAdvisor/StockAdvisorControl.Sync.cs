using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GateHelper.StockAdvisor.Models;
using GateHelper.StockAdvisor.Services;
using GateHelper.StockAdvisor.Trading;

namespace GateHelper.StockAdvisor
{
    public partial class StockAdvisorControl
    {
        #region [Price & Indicator Synchronization]

        public async Task RefreshPricesAsync()
        {
            if (_isRefreshingPrice) return;
            _isRefreshingPrice = true;
            _btnRefreshPrice.Enabled = false;
            _btnRefreshPrice.Text = "조회 중...";

            try
            {
                var priceTask = RefreshPricesInternalAsync();
                var intelTask = GlobalMarketPollerService.FetchMarketIntelAsync();
                await Task.WhenAll(priceTask, intelTask);

                UpdateMacroTicker(intelTask.Result);

                int failCount = 0;
                foreach (var itm in _portfolio.Items)
                {
                    if (!itm.IsPriceScrapeSuccess) failCount++;
                }

                if (_lblLastUpdated != null)
                {
                    if (failCount > 0)
                    {
                        _lblLastUpdated.Text = $"동기화: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ({failCount}건 수집 실패)";
                        _lblLastUpdated.ForeColor = Color.FromArgb(255, 120, 100);
                    }
                    else
                    {
                        _lblLastUpdated.Text = $"동기화: {DateTime.Now:yyyy-MM-dd HH:mm:ss} (전 종목 정상)";
                        _lblLastUpdated.ForeColor = Color.FromArgb(100, 255, 180);
                    }
                }

                SavePortfolio();
                UpdateUi();
                _ = RefreshMarketThermometerAsync();
                CheckAndDispatchPriceTouchAlerts();
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "StockAdvisorControl.RefreshPricesAsync");
            }
            finally
            {
                _isRefreshingPrice = false;
                _btnRefreshPrice.Enabled = true;
                _btnRefreshPrice.Text = "↻ 시세 갱신";
            }
        }

        private async Task RefreshPricesInternalAsync()
        {
            var tasks = new List<Task>();
            foreach (var item in _portfolio.Items)
            {
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var priceDto = await NaverStockPriceService.FetchPriceAsync(item.StockCode);
                        if (priceDto.IsSuccess)
                        {
                            item.IsPriceScrapeSuccess = true;
                            item.ScrapeErrorMessage = string.Empty;
                            item.LastScrapedTime = priceDto.ScrapedAt;
                            item.CurrentPrice = priceDto.CurrentPrice;
                            item.ChangeRate = priceDto.ChangeRate;
                            item.ChangePrice = priceDto.ChangePrice;
                            if (!string.IsNullOrWhiteSpace(priceDto.StockName))
                            {
                                item.StockName = priceDto.StockName;
                            }
                        }
                        else
                        {
                            item.IsPriceScrapeSuccess = false;
                            item.ScrapeErrorMessage = priceDto.ErrorMessage;
                        }

                        // 팩트 기술 지표, 수급 및 지뢰 탐지 병렬 비동기 수집
                        var indTask = TechnicalIndicatorService.AnalyzeIndicatorsAsync(item.StockCode, item.CurrentPrice);
                        var intelTask = StockIntelligenceService.AnalyzeAsync(item.StockCode);
                        await Task.WhenAll(indTask, intelTask);

                        var ind = indTask.Result;
                        var intel = intelTask.Result;

                        item.Rsi = ind.Rsi;
                        item.VolumeRatio = ind.VolumeRatio;
                        item.SupportPrice = ind.SupportPrice;
                        item.ResistancePrice = ind.ResistancePrice;
                        item.DownsideRiskPercent = ind.DownsideRiskPercent;
                        item.ChartScore = ind.ChartScore;
                        item.RiskScore = ind.RiskScore;
                        item.TimingBadge = ind.TimingBadge;
                        item.TimingWarning = ind.TimingWarning;
                        item.RecentCandles = ind.Candles;
                        item.RecommendedBuyPrice = ind.RecommendedBuyPrice;
                        item.SecondaryBuyPrice = ind.SecondaryBuyPrice;
                        item.TargetStopPrice = ind.TargetStopPrice;
                        item.EntryTimingBadge = ind.EntryTimingBadge;
                        item.EntryTimingGuide = ind.EntryTimingGuide;
                        item.RecommendedTargetPrice = ind.RecommendedTargetPrice;
                        item.SecondaryTargetPrice = ind.SecondaryTargetPrice;
                        item.ExitTimingBadge = ind.ExitTimingBadge;
                        item.ExitTimingGuide = ind.ExitTimingGuide;

                        // 수급 및 지뢰 탐지 지표 반영
                        item.MajorSupplyBadge = intel.SupplyBadge;
                        item.MajorSupplyDetail = intel.SupplyDetail;
                        item.LandmineBadge = intel.LandmineBadge;
                        item.LandmineDetail = intel.LandmineDetail;
                        item.HasCriticalLandmine = intel.HasCriticalLandmine;
                        item.DebtRatio = intel.DebtRatio;
                        item.ConsecutiveLossYears = intel.ConsecutiveLossYears;

                        // 4대 정통 퀀트 팩터(차트 30%, 수급·재무 25%, 뉴스·공시 25%, 손익비 20%) 단일 산출기 적용 (종목평가와 100% 동일한 단일 기준)
                        var unifiedScore = StockIntelligenceService.CalculateUnifiedScores(
                            ind,
                            intel,
                            0);

                        item.ChartScore = unifiedScore.ChartScore;
                        item.SupplyFinanceScore = unifiedScore.SupplyFinanceScore;
                        item.NewsScore = unifiedScore.NewsScore;
                        item.RiskScore = unifiedScore.RiskScore;
                        item.FinalScore = unifiedScore.FinalScore;
                    }
                    catch (Exception ex)
                    {
                        LogManager.LogException(ex, LogManager.Level.Warning, "StockAdvisorControl.RefreshPricesInternalAsync item loop");
                    }
                }));
            }
            await Task.WhenAll(tasks);
            await RefreshEarningsScheduleAsync();
        }

        #endregion

        #region [AI Risk Diagnosis Execution]

        private async Task RunAiDiagnosisAsync(bool isSilent = false)
        {
            if (_isAiDiagnosing) return;
            if (_portfolio.Items.Count == 0)
            {
                if (!isSilent)
                {
                    MessageBox.Show("진단할 종목이 없습니다. [+ 종목 추가] 버튼을 눌러 먼저 종목을 등록하세요.", "종목 없음", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                return;
            }

            _isAiDiagnosing = true;
            _btnDiagnose.Enabled = false;
            UpdateDiagnoseButtonText("⏳ 실시간 분석 중...");

            try
            {
                // 1. 최신 시세 갱신 및 글로벌 지표 병렬 수집
                var priceTask = RefreshPricesInternalAsync();
                var intelTask = GlobalMarketPollerService.FetchMarketIntelAsync();
                await Task.WhenAll(priceTask, intelTask);

                var marketIntel = intelTask.Result;
                UpdateMacroTicker(marketIntel);

                // 2. AI 리스크 진단 실행 (글로벌 매크로 스냅샷 주입)
                var diagResult = await GeminiRiskAdvisorService.DiagnosePortfolioAsync(_portfolio.Items, marketIntel);
                if (diagResult.IsSuccess)
                {
                    _portfolio.LastBriefing = diagResult.Briefing;

                    // 종목별 결과 매핑
                    foreach (var itemDiag in diagResult.ItemDiagnoses)
                    {
                        var matched = _portfolio.Items.Find(x => x.StockCode.TrimStart('0') == itemDiag.StockCode.TrimStart('0'));
                        if (matched != null)
                        {
                            matched.LastDiagnosis = itemDiag;
                            matched.NewsScore = itemDiag.NewsScore;
                            matched.FinalScore = itemDiag.FinalScore;
                        }
                    }

                    SavePortfolio();
                    UpdateUi();
                    CheckAndDispatchPriceTouchAlerts();

                    // 1. 하단 상태바에 작은 글씨로 정밀 동기화 일시 표시
                    if (_lblLastUpdated != null)
                    {
                        _lblLastUpdated.Text = $"동기화: {DateTime.Now:yyyy-MM-dd HH:mm:ss} (전 종목 정상)";
                        _lblLastUpdated.ForeColor = Color.FromArgb(100, 255, 180);
                    }

                    // 2. 상단 버튼 텍스트에 2줄로 동기화 상태 및 시간 즉시 반영
                    string timeStr = DateTime.Now.ToString("HH:mm:ss");
                    string finishNote = !string.IsNullOrEmpty(diagResult.ErrorMessage)
                        ? $"✓ {timeStr} 동기화"
                        : $"✓ {timeStr} 동기화";
                    UpdateDiagnoseButtonText(finishNote);
                }
                else
                {
                    UpdateDiagnoseButtonText("[실패] 재시도");
                    if (!isSilent)
                    {
                        MessageBox.Show(diagResult.ErrorMessage, "진단 실패", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateDiagnoseButtonText("[오류] 통신 실패");
                if (!isSilent)
                {
                    MessageBox.Show($"진단 중 오류 발생: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                _isAiDiagnosing = false;
                _btnDiagnose.Enabled = true;
            }
        }

        #endregion

        #region [KB OpenAPI Background Sync]

        private void SetupKbSyncTimer()
        {
            try
            {
                var config = KbTradingClient.LoadConfig();
                if (config.AutoSyncIntervalMinutes > 0 && (config.IsConfigured || config.EnableSimulationWhenNoKey))
                {
                    if (_kbSyncTimer == null)
                    {
                        _kbSyncTimer = new Timer();
                        _kbSyncTimer.Tick += async (s, e) => await SyncFromKbOpenApiBackgroundAsync();
                    }
                    _kbSyncTimer.Interval = config.AutoSyncIntervalMinutes * 60 * 1000;
                    _kbSyncTimer.Start();
                }
                else
                {
                    _kbSyncTimer?.Stop();
                }
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "StockAdvisorControl.SetupKbSyncTimer");
            }
        }

        private async Task SyncFromKbOpenApiBackgroundAsync()
        {
            if (_isAiDiagnosing || _isRefreshingPrice) return;
            try
            {
                var config = KbTradingClient.LoadConfig();
                var client = new KbTradingClient(config.AppKey, config.AppSecret, config.IsMockTrading);
                var balance = await client.FetchBalanceAsync(config.AccountNumber);

                if (balance.IsSuccess && balance.Holdings != null && balance.Holdings.Count > 0)
                {
                    bool changed = false;
                    foreach (var h in balance.Holdings)
                    {
                        var existing = _portfolio.Items.Find(x => x.StockCode.TrimStart('0').Equals(h.StockCode.TrimStart('0'), StringComparison.OrdinalIgnoreCase));
                        if (existing != null)
                        {
                            if (existing.Quantity != h.HoldingQuantity || existing.BuyPrice != h.BuyAveragePrice)
                            {
                                existing.Quantity = h.HoldingQuantity;
                                existing.BuyPrice = h.BuyAveragePrice;
                                changed = true;
                            }
                        }
                    }

                    if (changed)
                    {
                        SavePortfolio();
                        UpdateUi();
                    }
                }
            }
            catch { }
        }

        #endregion

        #region [Auto-Sync Timers]

        private void SetupAutoSyncTimers()
        {
            try
            {
                bool needTimer = (_portfolio != null) && (_portfolio.AutoPriceSyncEnabled || _portfolio.AutoAiDiagnosisEnabled);
                if (needTimer)
                {
                    if (_autoSyncTimer == null)
                    {
                        _autoSyncTimer = new Timer();
                        _autoSyncTimer.Interval = 10000; // 10초마다 인터벌 경과 체크
                        _autoSyncTimer.Tick += async (s, e) => await HandleAutoSyncTickAsync();
                    }
                    _autoSyncTimer.Start();
                }
                else
                {
                    _autoSyncTimer?.Stop();
                }
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "StockAdvisorControl.SetupAutoSyncTimers");
            }
        }

        private async Task HandleAutoSyncTickAsync()
        {
            if (!this.Visible || _portfolio == null || _portfolio.Items.Count == 0) return;
            if (_isAiDiagnosing || _isRefreshingPrice) return;

            DateTime now = DateTime.Now;

            // 1. AI 참모 진단 주기 체크 (AI 진단 내부에 시세 갱신이 포함되므로 우선 처리)
            if (_portfolio.AutoAiDiagnosisEnabled && _portfolio.AiDiagnosisIntervalMinutes > 0)
            {
                if (_lastAutoAiDiagnoseTime == DateTime.MinValue)
                {
                    _lastAutoAiDiagnoseTime = now;
                }
                else if (now - _lastAutoAiDiagnoseTime >= TimeSpan.FromMinutes(_portfolio.AiDiagnosisIntervalMinutes))
                {
                    _lastAutoAiDiagnoseTime = now;
                    _lastAutoPriceSyncTime = now;
                    await RunAiDiagnosisAsync(isSilent: true);
                    return;
                }
            }

            // 2. 실시간 시세 동기화 주기 체크
            if (_portfolio.AutoPriceSyncEnabled && _portfolio.PriceSyncIntervalMinutes > 0)
            {
                if (_lastAutoPriceSyncTime == DateTime.MinValue)
                {
                    _lastAutoPriceSyncTime = now;
                }
                else if (now - _lastAutoPriceSyncTime >= TimeSpan.FromMinutes(_portfolio.PriceSyncIntervalMinutes))
                {
                    _lastAutoPriceSyncTime = now;
                    await RefreshPricesAsync();
                }
            }
        }

        #endregion

        #region [Market Thermometer & Earnings Schedule]

        private async Task RefreshMarketThermometerAsync()
        {
            try
            {
                var res = await MarketThermometerService.GetMarketTemperatureAsync().ConfigureAwait(false);
                _marketTemperature = res;

                if (this.IsHandleCreated && !this.IsDisposed)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        if (_lblMarketThermometer == null) return;
                        if (res.IsSuccess)
                        {
                            _lblMarketThermometer.Text = res.BadgeText;
                            _lblMarketThermometer.ForeColor = res.BadgeForeColor;
                            _lblMarketThermometer.BackColor = res.BadgeBackColor;
                        }
                        else
                        {
                            _lblMarketThermometer.Text = "체온 대기";
                            _lblMarketThermometer.ForeColor = Color.FromArgb(180, 190, 210);
                            _lblMarketThermometer.BackColor = Color.FromArgb(28, 36, 50);
                        }
                    }));
                }
            }
            catch { }
        }

        private async Task RefreshEarningsScheduleAsync()
        {
            try
            {
                var schedules = await EarningsRadarService.GetEarningsScheduleAsync(_portfolio.Items);
                foreach (var item in _portfolio.Items)
                {
                    var match = schedules.FirstOrDefault(x => x.StockCode.TrimStart('0') == item.StockCode.TrimStart('0'));
                    if (match != null)
                    {
                        item.EarningsDaysRemaining = match.DaysRemaining;
                        item.EarningsPeriodTitle = match.PeriodTitle;
                        item.EarningsAnnouncementDate = match.EarningsDate;
                    }
                }
                UpdateEarningsRadarButtonBadge(schedules);
            }
            catch { }
        }

        private void UpdateEarningsRadarButtonBadge(List<EarningsScheduleItem> schedules = null)
        {
            if (_btnEarningsRadar == null) return;

            int urgentCount = _portfolio.Items.Count(x => x.EarningsDaysRemaining.HasValue && x.EarningsDaysRemaining.Value >= 0 && x.EarningsDaysRemaining.Value <= 7);
            int minDays = _portfolio.Items
                .Where(x => x.EarningsDaysRemaining.HasValue && x.EarningsDaysRemaining.Value >= 0 && x.EarningsDaysRemaining.Value <= 7)
                .Select(x => x.EarningsDaysRemaining.Value)
                .DefaultIfEmpty(999)
                .Min();

            if (urgentCount > 0)
            {
                string badge = minDays == 0 ? "오늘!" : $"D-{minDays}";
                _btnEarningsRadar.Text = $"실적 ({badge})";
                if (minDays == 0)
                {
                    _btnEarningsRadar.BackColor = Color.FromArgb(58, 20, 28);
                    _btnEarningsRadar.FlatAppearance.BorderColor = Color.FromArgb(255, 80, 80);
                    _btnEarningsRadar.ForeColor = Color.FromArgb(255, 120, 120);
                }
                else if (minDays <= 3)
                {
                    _btnEarningsRadar.BackColor = Color.FromArgb(48, 28, 16);
                    _btnEarningsRadar.FlatAppearance.BorderColor = Color.FromArgb(255, 160, 50);
                    _btnEarningsRadar.ForeColor = Color.FromArgb(255, 200, 100);
                }
                else
                {
                    _btnEarningsRadar.BackColor = Color.FromArgb(42, 34, 18);
                    _btnEarningsRadar.FlatAppearance.BorderColor = Color.FromArgb(255, 215, 64);
                    _btnEarningsRadar.ForeColor = Color.FromArgb(255, 235, 150);
                }
            }
            else
            {
                _btnEarningsRadar.Text = "실적 D-Day";
                _btnEarningsRadar.BackColor = Color.FromArgb(42, 24, 46);
                _btnEarningsRadar.FlatAppearance.BorderColor = Color.FromArgb(210, 110, 255);
                _btnEarningsRadar.ForeColor = Color.FromArgb(245, 225, 255);
            }
        }

        #endregion

        #region [Price Touch Alert Notifications]

        private void CheckAndDispatchPriceTouchAlerts()
        {
            if (_portfolio == null || !_portfolio.EnablePriceTouchAlert || _portfolio.Items.Count == 0) return;

            foreach (var item in _portfolio.Items)
            {
                if (item.CurrentPrice <= 0 || !item.IsPriceScrapeSuccess) continue;

                var verdict = StockVerdictHelper.Evaluate(item);

                // ==========================================
                // 1. 보유 종목 (Quantity > 0): 목표가/손절가/2차추매가 감지
                // ==========================================
                if (!item.IsWatchlist)
                {
                    // (1) 목표가 계산 (1차 권고 목표가 우선 -> 20일 저항선)
                    decimal targetPrice = item.RecommendedTargetPrice > 0 ? item.RecommendedTargetPrice : item.ResistancePrice;
                    if (targetPrice > 0)
                    {
                        if (item.CurrentPrice >= targetPrice)
                        {
                            if (!item.WasTargetAlerted)
                            {
                                item.WasTargetAlerted = true;
                                StockHoldingItem targetItem = item;
                                decimal hitPrice = targetPrice;
                                if (this.IsHandleCreated && !this.IsDisposed)
                                {
                                    this.BeginInvoke(new Action(() =>
                                    {
                                        StockToastNotification.ShowAlert(
                                            targetItem.StockName,
                                            targetItem.StockCode,
                                            targetItem.CurrentPrice,
                                            hitPrice,
                                            targetItem.ChangeRate,
                                            ToastAlertType.TargetHit,
                                            onClickAction: () => SelectAndFocusStock(targetItem.StockCode)
                                        );
                                    }));
                                }
                            }
                        }
                        else if (item.CurrentPrice < targetPrice * 0.985m)
                        {
                            item.WasTargetAlerted = false;
                        }
                    }

                    // (2) 손절가 계산 (2차 방어선 권고 손절가 우선 -> 20일 지지선 -3% -> 평단 -7%)
                    decimal stopPrice = item.TargetStopPrice > 0
                        ? item.TargetStopPrice
                        : (item.SupportPrice > 0 ? item.SupportPrice * 0.97m : (item.BuyPrice > 0 ? item.BuyPrice * 0.93m : 0m));
                    stopPrice = Math.Round(stopPrice, 0);

                    if (stopPrice > 0)
                    {
                        if (item.CurrentPrice <= stopPrice)
                        {
                            if (!item.WasStopAlerted)
                            {
                                item.WasStopAlerted = true;
                                StockHoldingItem stopItem = item;
                                decimal hitPrice = stopPrice;
                                if (this.IsHandleCreated && !this.IsDisposed)
                                {
                                    this.BeginInvoke(new Action(() =>
                                    {
                                        StockToastNotification.ShowAlert(
                                            stopItem.StockName,
                                            stopItem.StockCode,
                                            stopItem.CurrentPrice,
                                            hitPrice,
                                            stopItem.ChangeRate,
                                            ToastAlertType.StopHit,
                                            onClickAction: () => SelectAndFocusStock(stopItem.StockCode)
                                        );
                                    }));
                                }
                            }
                        }
                        else if (item.CurrentPrice > stopPrice * 1.015m)
                        {
                            item.WasStopAlerted = false;
                        }
                    }

                    // (3) 보유 종목 2차 추가 매수가(눌림목 지지선) 도달 알림 (AI 매수 승인 시에만)
                    if (verdict.CanBuy && item.SecondaryBuyPrice > 0)
                    {
                        if (item.CurrentPrice <= item.SecondaryBuyPrice)
                        {
                            if (!item.WasSecondaryBuyAlerted)
                            {
                                item.WasSecondaryBuyAlerted = true;
                                StockHoldingItem buyItem = item;
                                decimal hitBuyPrice = item.SecondaryBuyPrice;
                                if (this.IsHandleCreated && !this.IsDisposed)
                                {
                                    this.BeginInvoke(new Action(() =>
                                    {
                                        StockToastNotification.ShowAlert(
                                            buyItem.StockName,
                                            buyItem.StockCode,
                                            buyItem.CurrentPrice,
                                            hitBuyPrice,
                                            buyItem.ChangeRate,
                                            ToastAlertType.BuyHit,
                                            onClickAction: () => SelectAndFocusStock(buyItem.StockCode)
                                        );
                                    }));
                                }
                            }
                        }
                        else if (item.CurrentPrice > item.SecondaryBuyPrice * 1.015m)
                        {
                            item.WasSecondaryBuyAlerted = false;
                        }
                    }
                }
                // ==========================================
                // 2. 관심 종목 (IsWatchlist: 수량 0주): AI 1차 권고 매수가 도달 감지
                // ==========================================
                else
                {
                    // AI 인터락: C/F 등급이나 지뢰 종목은 가격이 떨어져도 매수 알림 원천 차단
                    if (verdict.CanBuy)
                    {
                        decimal buyTarget = item.RecommendedBuyPrice > 0 ? item.RecommendedBuyPrice : item.SupportPrice;
                        if (buyTarget > 0)
                        {
                            if (item.CurrentPrice <= buyTarget)
                            {
                                if (!item.WasBuyAlerted)
                                {
                                    item.WasBuyAlerted = true;
                                    StockHoldingItem watchItem = item;
                                    decimal hitBuy = buyTarget;
                                    if (this.IsHandleCreated && !this.IsDisposed)
                                    {
                                        this.BeginInvoke(new Action(() =>
                                        {
                                            StockToastNotification.ShowAlert(
                                                watchItem.StockName,
                                                watchItem.StockCode,
                                                watchItem.CurrentPrice,
                                                hitBuy,
                                                watchItem.ChangeRate,
                                                ToastAlertType.BuyHit,
                                                onClickAction: () => SelectAndFocusStock(watchItem.StockCode)
                                            );
                                        }));
                                    }
                                }
                            }
                            else if (item.CurrentPrice > buyTarget * 1.015m)
                            {
                                item.WasBuyAlerted = false;
                            }
                        }
                    }
                }
            }
        }

        private void SelectAndFocusStock(string code)
        {
            if (_lvStocks == null || string.IsNullOrEmpty(code) || _lvStocks.IsDisposed) return;
            try
            {
                foreach (ListViewItem lvi in _lvStocks.Items)
                {
                    if (lvi.Tag is StockHoldingItem itm &&
                        itm.StockCode.TrimStart('0').Equals(code.TrimStart('0'), StringComparison.OrdinalIgnoreCase))
                    {
                        _lvStocks.SelectedItems.Clear();
                        lvi.Selected = true;
                        lvi.EnsureVisible();
                        _lvStocks.Focus();
                        UpdateDetailPanel(itm);
                        break;
                    }
                }
            }
            catch { }
        }

        #endregion
    }
}

