using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using GateHelper.StockAdvisor.Models;
using GateHelper.StockAdvisor.Security;
using GateHelper.StockAdvisor.Services;
using GateHelper.StockAdvisor.Trading;
using Newtonsoft.Json;

namespace GateHelper.StockAdvisor
{
    public partial class StockAdvisorControl : UserControl
    {
        private PortfolioData _portfolio = new PortfolioData();
        private static readonly string SaveFilePath = Path.Combine(Util.MetaFolder, "stock_portfolio.dat");

        // UI 컨트롤 요소
        private Panel _pnlHeader;
        private Label _lblTrafficLight;
        private Label _lblBriefing1;
        private Label _lblBriefing2;
        private Label _lblBriefing3;
        private ComboBox _cboAiModel;
        private Button _btnDiagnose;
        private Button _btnRefreshPrice;
        private Button _btnScoreHelp;
        private Button _btnAddStock;
        private Button _btnImportPortfolio;
        private Button _btnEmergencyGuide;

        // 글로벌 매크로 티커 바
        private Panel _pnlMacroTicker;
        private Label _lblMacroTicker;

        private SplitContainer _splitMain;
        private ListView _lvStocks;
        private Panel _pnlDetail;

        // 상세 패널 요소
        private Label _lblDetailTitle;
        private Label _lblDetailTimingWarning;
        private Label _lblDetailScores;
        private Label _lblDetailTechnicalFacts;
        private Label _lblDetailIntelligence;
        private Label _lblDetailGroupBadge;
        private Label _lblDetailAction;
        private Label _lblDetailReliability;
        private Label _lblDetailImpact;
        private Label _lblDetailTargetCut;
        private RichTextBox _rtbDetailReason;
        private Button _btnEditItem;
        private Button _btnDeleteItem;

        // 하단 상태바
        private Panel _pnlFooter;
        private Label _lblAccountSummary;
        private Label _lblLastUpdated;
        private Timer _kbSyncTimer;

        // 비동기 작업 상태 플래그 (멀티태스킹 분리)
        private bool _isAiDiagnosing = false;
        private bool _isRefreshingPrice = false;

        // 리스트뷰 컬럼 정렬 상태
        private int _sortColumn = -1;
        private SortOrder _sortOrder = SortOrder.None;
        private static readonly string[] OriginalColumnNames = new[]
        {
            "코드", "종목명", "현재가", "전일대비", "평단", "보유수량", "손익금(원)", "수익률", "AI 등급",
            "수급(외인·기관)", "지뢰검증", "14일 RSI", "5일 거래량", "차트", "뉴스", "손익비", "★종합점수", "타이밍 안전벨트", "권고 지침"
        };
        private GlobalMarketSnapshot _macroSnapshot;

        public StockAdvisorControl()
        {
            InitializeComponent();
            BuildUiLayout();
            LoadPortfolio();
            UpdateUi();

            SetupKbSyncTimer();
            this.VisibleChanged += (s, e) =>
            {
                if (!this.Visible)
                {
                    _kbSyncTimer?.Stop();
                }
                else
                {
                    SetupKbSyncTimer();
                }
            };
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            AdjustSplitter();
        }

        private void AdjustSplitter()
        {
            if (_splitMain == null || this.Width < 600) return;
            try
            {
                // 화면 전체 너비에 따른 최적 반응형 분할:
                // 넓은 화면(FHD 1920px 등 최대화)에서는 우측 리포트 패널에 36% (최대 720px)를 할당하여 줄바꿈 최소화 및 시원한 가독성 확보
                // 좁은 화면에서도 최소 460px를 보장
                int rightWidth = (int)(this.Width * 0.36);
                rightWidth = Math.Max(460, Math.Min(720, rightWidth));
                int targetDist = this.Width - rightWidth;

                if (targetDist >= 500 && targetDist < this.Width - 200)
                {
                    _splitMain.SplitterDistance = targetDist;
                }
            }
            catch { }
        }

        public void OnTabVisibilityChanged(bool isVisible)
        {
            if (isVisible)
            {
                // 탭 진입 시 시세 자동 새로고침 (유휴 CPU 0% 유지)
                _ = RefreshPricesAsync();
            }
        }

        #region Business Logic & Data Binding

        private void LoadPortfolio()
        {
            try
            {
                string json = DpapiHelper.LoadEncryptedFile(SaveFilePath);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    _portfolio = JsonConvert.DeserializeObject<PortfolioData>(json) ?? new PortfolioData();
                }
                else
                {
                    // 최초 실행 시 기본 예시 종목 (삼성전자, SK하이닉스) 탑재
                    _portfolio.Items.Add(new StockHoldingItem
                    {
                        StockCode = "005930",
                        StockName = "삼성전자",
                        Quantity = 50,
                        BuyPrice = 75000,
                        Memo = "반도체 HBM 공급망 모멘텀 및 정책 동향 주목"
                    });
                    _portfolio.Items.Add(new StockHoldingItem
                    {
                        StockCode = "000660",
                        StockName = "SK하이닉스",
                        Quantity = 20,
                        BuyPrice = 195000,
                        Memo = "HBM3E 독점 납품 및 AI 서버 수요"
                    });
                    SavePortfolio();
                }
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "StockAdvisorControl.LoadPortfolio");
                _portfolio = new PortfolioData();
            }

            UpdateDiagnoseButtonText();
        }

        private void UpdateDiagnoseButtonText(string customStatus = null)
        {
            if (_btnDiagnose == null) return;

            if (!string.IsNullOrEmpty(customStatus))
            {
                _btnDiagnose.Text = $"★ AI 참모 진단\r\n{customStatus}";
                return;
            }

            if (_portfolio?.LastBriefing != null && _portfolio.LastBriefing.GeneratedAt != default && _portfolio.LastBriefing.GeneratedAt.Year > 2000)
            {
                _btnDiagnose.Text = $"★ AI 참모 진단\r\n✓ 진단 완료 ({_portfolio.LastBriefing.GeneratedAt:HH:mm:ss})";
            }
            else
            {
                _btnDiagnose.Text = "★ AI 참모 진단\r\n(진단 대기)";
            }
        }

        private void SavePortfolio()
        {
            try
            {
                string json = JsonConvert.SerializeObject(_portfolio, Formatting.Indented);
                DpapiHelper.SaveEncryptedFile(SaveFilePath, json);
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "StockAdvisorControl.SavePortfolio");
            }
        }

        #endregion

        #region Actions (Async & Dialogs)

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

                _lblLastUpdated.Text = $"시세 갱신: {DateTime.Now:HH:mm:ss}";
                SavePortfolio();
                UpdateUi();
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
                            item.CurrentPrice = priceDto.CurrentPrice;
                            item.ChangeRate = priceDto.ChangeRate;
                            item.ChangePrice = priceDto.ChangePrice;
                            if (!string.IsNullOrWhiteSpace(priceDto.StockName))
                            {
                                item.StockName = priceDto.StockName;
                            }
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

                        // 수급 및 지뢰 탐지 지표 반영
                        item.MajorSupplyBadge = intel.SupplyBadge;
                        item.MajorSupplyDetail = intel.SupplyDetail;
                        item.LandmineBadge = intel.LandmineBadge;
                        item.LandmineDetail = intel.LandmineDetail;
                        item.HasCriticalLandmine = intel.HasCriticalLandmine;
                        item.DebtRatio = intel.DebtRatio;
                        item.ConsecutiveLossYears = intel.ConsecutiveLossYears;

                        // 최종 점수: 차트 40% + 뉴스 30% + 손익비 30%
                        int news = item.NewsScore > 0 ? item.NewsScore : 5;
                        item.FinalScore = (int)Math.Round(item.ChartScore * 0.4 + news * 0.3 + item.RiskScore * 0.3);

                        // 치명적 지뢰(CB/유증/3년적자) 감지 시 F등급 강제 강등 (최대 2점)
                        if (item.HasCriticalLandmine)
                        {
                            item.FinalScore = Math.Min(item.FinalScore, 2);
                        }
                        item.FinalScore = Math.Max(1, Math.Min(10, item.FinalScore));
                    }
                    catch (Exception ex)
                    {
                        LogManager.LogException(ex, LogManager.Level.Warning, "StockAdvisorControl.RefreshPricesInternalAsync item loop");
                    }
                }));
            }
            await Task.WhenAll(tasks);
        }

        private async Task RunAiDiagnosisAsync()
        {
            if (_isAiDiagnosing) return;
            if (_portfolio.Items.Count == 0)
            {
                MessageBox.Show("진단할 종목이 없습니다. [+ 종목 추가] 버튼을 눌러 먼저 종목을 등록하세요.", "종목 없음", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

                    // 버튼 텍스트에 완료 상태와 시간 즉시 반영 (우하단 시선 분산 방지)
                    string timeStr = DateTime.Now.ToString("HH:mm:ss");
                    string finishNote = !string.IsNullOrEmpty(diagResult.ErrorMessage)
                        ? $"✓ 룰엔진 완료 ({timeStr})"
                        : $"✓ 진단 완료 ({timeStr})";
                    UpdateDiagnoseButtonText(finishNote);
                }
                else
                {
                    UpdateDiagnoseButtonText("❌ 진단 실패 (재시도)");
                    MessageBox.Show(diagResult.ErrorMessage, "진단 실패", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                UpdateDiagnoseButtonText("❌ 통신 오류");
                MessageBox.Show($"진단 중 오류 발생: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isAiDiagnosing = false;
                _btnDiagnose.Enabled = true;
            }
        }

        private void ShowMarketIntelNewsDialog()
        {
            try
            {
                using (var dlg = new MarketIntelNewsDialog(_macroSnapshot, _portfolio?.Items))
                {
                    var parentForm = this.FindForm();
                    if (parentForm != null)
                    {
                        dlg.ShowDialog(parentForm);
                    }
                    else
                    {
                        dlg.ShowDialog(this);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("실시간 뉴스 브리핑 창을 여는 중 오류가 발생했습니다: " + ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OpenNaverFinanceWeb()
        {
            if (_lvStocks.SelectedItems.Count == 0) return;
            var item = _lvStocks.SelectedItems[0].Tag as StockHoldingItem;
            if (item != null && !string.IsNullOrEmpty(item.StockCode))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = $"https://finance.naver.com/item/main.naver?code={item.StockCode}",
                        UseShellExecute = true
                    });
                }
                catch { }
            }
        }

        private void ShowAddStockDialog()
        {
            using (var dlg = new StockItemDialog())
            {
                var parentForm = this.FindForm();
                var result = parentForm != null ? dlg.ShowDialog(parentForm) : dlg.ShowDialog(this);
                if (result == DialogResult.OK && dlg.Item != null)
                {
                    var existing = _portfolio.Items.Find(x => x.StockCode.TrimStart('0') == dlg.Item.StockCode.TrimStart('0'));
                    if (existing != null)
                    {
                        existing.Quantity += dlg.Item.Quantity;
                        existing.BuyPrice = dlg.Item.BuyPrice;
                        existing.Memo = dlg.Item.Memo;
                    }
                    else
                    {
                        _portfolio.Items.Add(dlg.Item);
                    }

                    SavePortfolio();
                    UpdateUi(); // AI 진단 중이더라도 등록 즉시 화면에 0.01초 만에 표시!
                    _ = RefreshPricesAsync();
                }
            }
        }

        private void ShowImportPortfolioDialog()
        {
            using (var dlg = new SmartPortfolioImportDialog(_portfolio.Items))
            {
                var parentForm = this.FindForm();
                var result = parentForm != null ? dlg.ShowDialog(parentForm) : dlg.ShowDialog(this);
                if (result == DialogResult.OK && dlg.ResultItems != null && dlg.ResultItems.Count > 0)
                {
                    if (dlg.IsReplaceMode)
                    {
                        _portfolio.Items.Clear();
                    }

                    int addedCount = 0;
                    int updatedCount = 0;

                    foreach (var imported in dlg.ResultItems)
                    {
                        var existing = _portfolio.Items.Find(x =>
                            x.StockCode.TrimStart('0').Equals(imported.StockCode.TrimStart('0'), StringComparison.OrdinalIgnoreCase));

                        if (existing != null)
                        {
                            existing.Quantity = imported.Quantity;
                            existing.BuyPrice = imported.BuyPrice;
                            if (!string.IsNullOrEmpty(imported.Memo)) existing.Memo = imported.Memo;
                            updatedCount++;
                        }
                        else
                        {
                            _portfolio.Items.Add(imported);
                            addedCount++;
                        }
                    }

                    SavePortfolio();
                    UpdateUi();
                    _ = RefreshPricesAsync();

                    MessageBox.Show(parentForm ?? (IWin32Window)this,
                        $"총 {dlg.ResultItems.Count}개 종목이 성공적으로 반영되었습니다.\n(신규 추가: {addedCount}건, 기존 갱신: {updatedCount}건)\n실시간 시세 조회가 백그라운드에서 진행됩니다.",
                        "계좌 가져오기 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void ShowKbSettingDialog()
        {
            using (var dlg = new KbSecSettingDialog())
            {
                var parentForm = this.FindForm();
                if (parentForm != null) dlg.ShowDialog(parentForm);
                else dlg.ShowDialog(this);

                SetupKbSyncTimer();
            }
        }

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

        private void EditSelectedStock()
        {
            if (_lvStocks.SelectedItems.Count == 0) return;
            var item = _lvStocks.SelectedItems[0].Tag as StockHoldingItem;
            if (item == null) return;

            using (var dlg = new StockItemDialog(item))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    SavePortfolio();
                    UpdateUi(); // 수정 즉시 화면에 반영!
                    _ = RefreshPricesAsync();
                }
            }
        }

        private void DeleteSelectedStock()
        {
            if (_lvStocks.SelectedItems.Count == 0) return;
            var item = _lvStocks.SelectedItems[0].Tag as StockHoldingItem;
            if (item == null) return;

            var confirm = MessageBox.Show($"[{item.StockName}({item.StockCode})] 종목을 포트폴리오에서 삭제하시겠습니까?", "종목 삭제", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm == DialogResult.Yes)
            {
                _portfolio.Items.Remove(item);
                SavePortfolio();
                UpdateUi();
            }
        }

        private void ShowKeyStockSummaryDialog()
        {
            if (_portfolio.Items.Count == 0)
            {
                MessageBox.Show("현재 등록된 종목이 없습니다. 먼저 종목을 추가해주세요.", "중요 종목 브리핑", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new KeyStockSummaryDialog(_portfolio.Items))
            {
                dlg.ShowDialog(this);
            }
        }

        private void ShowPanicExitDialog()
        {
            if (_portfolio.Items.Count == 0)
            {
                MessageBox.Show("현재 포트폴리오에 보유 종목이 없습니다.", "비상 탈출", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new PanicExitDialog(_portfolio.Items))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK && dlg.ExecutionCompleted)
                {
                    SavePortfolio();
                    UpdateUi();
                }
            }
        }

        private void ShowScoreHelpDialog()
        {
            using (var dlg = new StockScoreHelpDialog())
            {
                dlg.ShowDialog(this);
            }
        }

        #endregion
    }
}
