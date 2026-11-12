using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
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
        private Label _lblMarketThermometer;
        private MarketTemperatureResult _marketTemperature;
        private Button _btnDiagnose;
        private Button _btnLeaderRadar; // 📡 주도주 AI 매수 레이더
        private Button _btnEvaluateStock;
        private Button _btnRefreshPrice;
        private Button _btnOption;
        private Button _btnAlertHistory; // 실시간 가격 도달 알림 기록 열기
        private static readonly List<StockAlertLogItem> _alertHistory = new List<StockAlertLogItem>();
        private Button _btnEarningsRadar; // 실적 발표 D-Day 레이더
        private Button _btnMarketCloseReport; // 장마감 결산 & 포트폴리오 리뷰

        private Timer _autoSyncTimer;
        private DateTime _lastAutoPriceSyncTime = DateTime.MinValue;
        private DateTime _lastAutoAiDiagnoseTime = DateTime.MinValue;

        private SplitContainer _splitMain;
        private Panel _pnlListFilter;
        private Button _btnFilterAll;
        private Button _btnFilterHolding;
        private Button _btnFilterWatchlist;
        private Label _lblFilterSummary;
        private StockFilterMode _filterMode = StockFilterMode.All;
        private ListView _lvStocks;
        private Panel _pnlDetail;

        // 상세 패널 요소 (슬림 4단 구조)
        private Label _lblDetailTitle;
        private Label _lblFinalVerdict;          // AI 참모 최종 결론 바 (0.1초 직관 배지)
        private Label _lblDetailPriceTargets;
        private MiniCandleChartControl _miniChart; // 20일 일봉 & AI 타점 미니 차트
        private Label _lblDetailSummary;
        private RichTextBox _rtbDetailReason;
        private Button _btnEditItem;
        private Button _btnDeleteItem;

        // 제로 리소스 폰트 및 브러시 캐시 (루프 및 Paint 내 new 방지, GC 0B)
        internal static readonly Font SubItemBoldFont = new Font("Segoe UI", 9.2f, FontStyle.Bold);
        internal static readonly Font DiagnoseTitleFont = new Font("Malgun Gothic", 8.2f, FontStyle.Bold);
        internal static readonly Font DiagnoseDefaultFont = new Font("Malgun Gothic", 8.5f, FontStyle.Bold);
        internal static readonly Font DiagnoseSubFont = new Font("Segoe UI", 7.2f, FontStyle.Regular);
        internal static readonly Font RtbDividerFont = new Font("Segoe UI", 8.5f);
        internal static readonly Font RtbHeaderBoldFont = new Font("Malgun Gothic", 9.2f, FontStyle.Bold);
        internal static readonly Font RtbSectionFont = new Font("Malgun Gothic", 10.3f, FontStyle.Bold);
        internal static readonly Font RtbBodyFont = new Font("Malgun Gothic", 9.3f);
        internal static readonly Font RtbSubFont = new Font("Malgun Gothic", 8.8f);
        internal static readonly Font RtbSubBoldFont = new Font("Malgun Gothic", 8.8f, FontStyle.Bold);
        internal static readonly Pen HeaderBorderPen = new Pen(Color.FromArgb(42, 50, 70));
        internal static readonly SolidBrush BrushCyanBright = new SolidBrush(Color.FromArgb(0, 245, 255));
        internal static readonly SolidBrush BrushCyanSub = new SolidBrush(Color.FromArgb(160, 235, 255));
        internal static readonly SolidBrush BrushMutedTitle = new SolidBrush(Color.FromArgb(140, 160, 185));
        internal static readonly SolidBrush BrushGoldTitle = new SolidBrush(Color.FromArgb(255, 235, 160));
        internal static readonly SolidBrush BrushGoldSub = new SolidBrush(Color.FromArgb(255, 210, 110));
        internal static readonly StringFormat CenterStringFormat = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.NoWrap
        };

        private DateTime? _lastDiagnoseSuccessTime = null;
        private string _diagnoseCustomStatus = null;
        private DateTime? _lastPriceRefreshSuccessTime = null;
        private string _priceRefreshCustomStatus = null;

        // 하단 상태바
        private Panel _pnlFooter;
        private Label _lblAccountSummary;
        private Label _lblLastUpdated;
        private Timer _kbSyncTimer;

        // 비동기 작업 및 UI 이벤트 플래그
        private bool _isAiDiagnosing = false;
        private bool _isRefreshingPrice = false;
        private bool _hasInitialDiagnosed = false; // 최초 메인 화면 진입 시 1회 자동 진단 실행 가드
        private bool _isUpdatingUi = false;        // 대량 리스트 갱신 중 ItemChecked 이벤트 중복 방지
        private bool _isDoubleClick = false;       // 더블클릭 시 체크박스 오동작 방지
        private readonly HashSet<string> _alertedRadarCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // 주도주 레이더 중복 토스트 알림 방지 캐시

        // 리스트뷰 컬럼 정렬 상태
        private int _sortColumn = -1;
        private SortOrder _sortOrder = SortOrder.None;
        private static readonly string[] OriginalColumnNames = new[]
        {
            "종목명 (앱주문체크)", "현재가", "전일대비", "평단", "보유", "손익금(원)", "수익률", "AI 등급",
            "수급(외인·기관)", "지뢰검증", "14일 RSI", "5일 거래량", "종합점수", "타이밍 안전벨트", "최종 액션"
        };
        private GlobalMarketSnapshot _macroSnapshot;

        public StockAdvisorControl()
        {
            InitializeComponent();
            BuildUiLayout();
            LoadPortfolio();
            UpdateUi();

            SetupKbSyncTimer();
            SetupAutoSyncTimers();
            _ = RefreshMarketThermometerAsync();
            _ = RefreshEarningsScheduleAsync();

            StockToastNotification.AlertLogged += OnAlertLogged;

            this.VisibleChanged += (s, e) =>
            {
                if (!this.Visible)
                {
                    _kbSyncTimer?.Stop();
                    // 실시간 가격 도달 감시 또는 주도주 레이더 자동 알림(초절전 백그라운드 모드)이 활성화되어 있으면 타이머 유지
                    if (_portfolio == null || (!_portfolio.EnablePriceTouchAlert && !_portfolio.EnableLeaderRadarAutoAlert))
                    {
                        _autoSyncTimer?.Stop();
                    }
                }
                else
                {
                    SetupKbSyncTimer();
                    SetupAutoSyncTimers();
                    _ = RefreshMarketThermometerAsync();
                    _ = RefreshEarningsScheduleAsync();
                    TriggerInitialDiagnosisIfNeeded();
                    UpdateUi(); // 백그라운드에서 갱신된 시세/상태 동기화
                    UpdateAlertHistoryButtonText();
                }
            };
        }

        private void TriggerInitialDiagnosisIfNeeded()
        {
            if (_hasInitialDiagnosed) return;
            _hasInitialDiagnosed = true;
            _ = RunAiDiagnosisAsync(isSilent: true);
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
                if (!_hasInitialDiagnosed)
                {
                    TriggerInitialDiagnosisIfNeeded();
                }
                else
                {
                    // 재진입 시에는 가벼운 시세 새로고침만 수행 (유휴 CPU 0% 유지)
                    _ = RefreshPricesAsync();
                }
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
                    foreach (var itm in _portfolio.Items)
                    {
                        itm.MajorSupplyBadge = TextCleanHelper.StripEmoji(itm.MajorSupplyBadge);
                        itm.LandmineBadge = TextCleanHelper.StripEmoji(itm.LandmineBadge);
                        itm.TimingBadge = TextCleanHelper.StripEmoji(itm.TimingBadge);
                        itm.EntryTimingBadge = TextCleanHelper.StripEmoji(itm.EntryTimingBadge);
                        itm.ExitTimingBadge = TextCleanHelper.StripEmoji(itm.ExitTimingBadge);
                        itm.QuantBadge = TextCleanHelper.StripEmoji(itm.QuantBadge);
                    }
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

            UpdateDiagnoseButtonStatus(false);
            UpdatePriceRefreshButtonStatus(false);
        }

        private void UpdatePriceRefreshButtonStatus(bool isSyncSuccess, string customStatus = null, DateTime? syncTime = null)
        {
            if (_btnRefreshPrice == null) return;

            _priceRefreshCustomStatus = customStatus;
            _btnRefreshPrice.Text = ""; // 텍스트를 비워 Paint 이벤트에서 상단 타이틀 + 하단 작은 동기화시간을 선명하게 직접 렌더링

            if (isSyncSuccess)
            {
                _lastPriceRefreshSuccessTime = syncTime ?? DateTime.Now;
                _btnRefreshPrice.BackColor = Color.FromArgb(14, 52, 74);
                _btnRefreshPrice.FlatAppearance.BorderColor = Color.FromArgb(0, 220, 255);
                _btnRefreshPrice.ForeColor = Color.FromArgb(0, 245, 255);
            }
            else if (!string.IsNullOrEmpty(customStatus))
            {
                _btnRefreshPrice.BackColor = Color.FromArgb(40, 36, 18);
                _btnRefreshPrice.FlatAppearance.BorderColor = Color.FromArgb(255, 190, 40);
                _btnRefreshPrice.ForeColor = Color.FromArgb(255, 235, 160);
            }
            else
            {
                if (_lastPriceRefreshSuccessTime.HasValue)
                {
                    _btnRefreshPrice.BackColor = Color.FromArgb(14, 52, 74);
                    _btnRefreshPrice.FlatAppearance.BorderColor = Color.FromArgb(0, 220, 255);
                    _btnRefreshPrice.ForeColor = Color.FromArgb(0, 245, 255);
                }
                else
                {
                    _lastPriceRefreshSuccessTime = null;
                    _btnRefreshPrice.BackColor = Color.FromArgb(20, 34, 56);
                    _btnRefreshPrice.FlatAppearance.BorderColor = Color.FromArgb(64, 150, 255);
                    _btnRefreshPrice.ForeColor = Color.FromArgb(215, 235, 255);
                }
            }

            _btnRefreshPrice.Invalidate();
        }

        private void UpdateDiagnoseButtonStatus(bool isSyncSuccess, string customStatus = null, DateTime? syncTime = null)
        {
            if (_btnDiagnose == null) return;

            _diagnoseCustomStatus = customStatus;
            _btnDiagnose.Text = ""; // 텍스트를 비워 Paint 이벤트에서 상단 타이틀 + 하단 작은 동기화시간을 선명하게 직접 렌더링

            if (isSyncSuccess)
            {
                _lastDiagnoseSuccessTime = syncTime ?? (_portfolio?.LastBriefing?.GeneratedAt ?? DateTime.Now);
                _btnDiagnose.BackColor = Color.FromArgb(14, 52, 74);
                _btnDiagnose.FlatAppearance.BorderColor = Color.FromArgb(0, 220, 255);
                _btnDiagnose.ForeColor = Color.FromArgb(0, 245, 255);
            }
            else if (!string.IsNullOrEmpty(customStatus))
            {
                _btnDiagnose.BackColor = Color.FromArgb(40, 36, 18);
                _btnDiagnose.FlatAppearance.BorderColor = Color.FromArgb(255, 190, 40);
                _btnDiagnose.ForeColor = Color.FromArgb(255, 235, 160);
            }
            else
            {
                if (_portfolio?.LastBriefing != null && _portfolio.LastBriefing.GeneratedAt > DateTime.MinValue)
                {
                    _lastDiagnoseSuccessTime = _portfolio.LastBriefing.GeneratedAt;
                    _btnDiagnose.BackColor = Color.FromArgb(14, 52, 74);
                    _btnDiagnose.FlatAppearance.BorderColor = Color.FromArgb(0, 220, 255);
                    _btnDiagnose.ForeColor = Color.FromArgb(0, 245, 255);
                }
                else
                {
                    _lastDiagnoseSuccessTime = null;
                    _btnDiagnose.BackColor = Color.FromArgb(24, 28, 38);
                    _btnDiagnose.FlatAppearance.BorderColor = Color.FromArgb(50, 60, 80);
                    _btnDiagnose.ForeColor = Color.FromArgb(140, 160, 185);
                }
            }

            _btnDiagnose.Invalidate();
        }

        private void UpdateDiagnoseButtonText(string customStatus = null)
        {
            UpdateDiagnoseButtonStatus(false, customStatus);
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

        #region Dialog Openers & User Actions

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
            _ = RefreshMarketThermometerAsync();
            }
        }

        private void ShowOptionDialog()
        {
            using (var dlg = new StockAdvisorOptionDialog(_portfolio))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK && dlg.OptionsChanged)
                {
                    SavePortfolio();
                    SetupAutoSyncTimers();
                    UpdateUi();
                }
            }
        }

        public void ShowAlertHistoryDialog()
        {
            try
            {
                var existingCodes = new HashSet<string>(_portfolio.Items.Select(x => x.StockCode.TrimStart('0')), StringComparer.OrdinalIgnoreCase);
                using (var dlg = new StockAlertHistoryDialog(
                    _alertHistory,
                    code => SelectAndFocusStock(code),
                    existingCodes,
                    AddStockToWatchlist,
                    (code, owner) => ShowStockEvaluatorDialog(code, owner)))
                {
                    Form parent = this.FindForm();
                    if (parent != null) dlg.ShowDialog(parent);
                    else dlg.ShowDialog(this);
                }
                UpdateAlertHistoryButtonText();
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "StockAdvisorControl.ShowAlertHistoryDialog");
            }
        }

        private void OnAlertLogged(StockAlertLogItem item)
        {
            if (item == null) return;
            lock (_alertHistory)
            {
                _alertHistory.Add(item);
            }
            if (this.IsHandleCreated && !this.IsDisposed)
            {
                this.BeginInvoke(new Action(UpdateAlertHistoryButtonText));
            }
        }

        private void UpdateAlertHistoryButtonText()
        {
            if (_btnAlertHistory == null || _btnAlertHistory.IsDisposed) return;
            int unreadCount = 0;
            int totalCount = 0;
            lock (_alertHistory)
            {
                totalCount = _alertHistory.Count;
                unreadCount = _alertHistory.Count(x => !x.IsRead);
            }
            if (unreadCount > 0)
            {
                _btnAlertHistory.Text = $"알림 기록 ({unreadCount})";
                _btnAlertHistory.BackColor = Color.FromArgb(38, 30, 24);
                _btnAlertHistory.FlatAppearance.BorderColor = Color.FromArgb(255, 170, 0);
                _btnAlertHistory.ForeColor = Color.FromArgb(255, 220, 100);
            }
            else
            {
                _btnAlertHistory.Text = totalCount > 0 ? $"알림 기록 ({totalCount})" : "알림 기록";
                _btnAlertHistory.BackColor = Color.FromArgb(20, 28, 40);
                _btnAlertHistory.FlatAppearance.BorderColor = Color.FromArgb(60, 85, 120);
                _btnAlertHistory.ForeColor = Color.FromArgb(170, 195, 230);
            }
        }

        private void EditSelectedStock()
        {
            if (_lvStocks.SelectedItems.Count == 0)
            {
                ToastNotification.Show("수정할 종목을 목록에서 먼저 선택해주세요.", "선택 필요", ToastIcon.Warning);
                return;
            }
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
            if (_lvStocks.SelectedItems.Count == 0)
            {
                ToastNotification.Show("삭제할 종목을 목록에서 먼저 선택해주세요.", "선택 필요", ToastIcon.Warning);
                return;
            }
            var item = _lvStocks.SelectedItems[0].Tag as StockHoldingItem;
            if (item == null) return;

            var confirm = MessageBox.Show($"[{item.StockName}({item.StockCode})] 종목을 포트폴리오에서 삭제하시겠습니까?", "종목 삭제", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm == DialogResult.Yes)
            {
                _portfolio.Items.Remove(item);
                SavePortfolio();
                UpdateUi();
                _ = RefreshMarketThermometerAsync();
            }
        }

        private void ShowKeyStockSummaryDialog()
        {
            if (_portfolio.Items.Count == 0)
            {
                MessageBox.Show("현재 등록된 종목이 없습니다. 먼저 종목을 추가해주세요.", "중요 종목 브리핑", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new KeyStockSummaryDialog(_portfolio.Items, code => SelectAndFocusStock(code)))
            {
                dlg.ShowDialog(this);
            }
        }

        private async void ShowMarketThermometerDialog()
        {
            if (_marketTemperature == null || (DateTime.Now - _marketTemperature.UpdatedAt).TotalSeconds > 15)
            {
                var fresh = await MarketThermometerService.GetMarketTemperatureAsync(true).ConfigureAwait(true);
                _marketTemperature = fresh;
                if (_lblMarketThermometer != null && fresh.IsSuccess)
                {
                    _lblMarketThermometer.Text = fresh.BadgeText;
                    _lblMarketThermometer.ForeColor = fresh.BadgeForeColor;
                    _lblMarketThermometer.BackColor = fresh.BadgeBackColor;
                }
            }

            using (var dlg = new MarketThermometerDialog(_marketTemperature))
            {
                dlg.ShowDialog(this);
            }
        }

        private bool ShowStockEvaluatorDialog(string customQuery = null, IWin32Window owner = null)
        {
            string initQuery = customQuery ?? "";
            if (string.IsNullOrEmpty(initQuery) && _lvStocks.SelectedItems.Count > 0)
            {
                var sel = _lvStocks.SelectedItems[0].Tag as StockHoldingItem;
                if (sel != null) initQuery = sel.StockName;
            }

            using (var dlg = new StockEvaluatorDialog(initQuery))
            {
                if (dlg.ShowDialog(owner ?? this) == DialogResult.OK && dlg.AddedStockItem != null)
                {
                    var newItem = dlg.AddedStockItem;
                    var existing = _portfolio.Items.FirstOrDefault(x => x.StockCode.TrimStart('0').Equals(newItem.StockCode.TrimStart('0'), StringComparison.OrdinalIgnoreCase));
                    if (existing != null)
                    {
                        if (newItem.Quantity > 0)
                        {
                            existing.BuyPrice = newItem.BuyPrice;
                            existing.Quantity += newItem.Quantity;
                        }
                        existing.CurrentPrice = newItem.CurrentPrice;
                        existing.ChangeRate = newItem.ChangeRate;
                    }
                    else
                    {
                        if (newItem.IsWatchlist && _filterMode == StockFilterMode.HoldingOnly) _filterMode = StockFilterMode.All;
                        else if (!newItem.IsWatchlist && _filterMode == StockFilterMode.WatchlistOnly) _filterMode = StockFilterMode.All;
                        _portfolio.Items.Add(newItem);
                    }

                    SavePortfolio();
                    UpdateUi();
                    string modeStr = newItem.IsWatchlist ? "관심 종목" : "보유 종목";
                    ToastNotification.Show($"[{newItem.StockName}] 종목이 [{modeStr}]에 성공적으로 추가되었습니다.", "종목 추가 완료", ToastIcon.Success);
                    return true;
                }
            }
            return false;
        }

        private void ShowPanicExitDialog()
        {
            if (_portfolio.Items.Count == 0) { MessageBox.Show("현재 포트폴리오에 보유 종목이 없습니다.", "비상 탈출", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            using (var dlg = new PanicExitDialog(_portfolio.Items)) { if (dlg.ShowDialog(this) == DialogResult.OK && dlg.ExecutionCompleted) { SavePortfolio(); UpdateUi(); } }
        }

        private void ShowScoreHelpDialog() { using (var dlg = new StockScoreHelpDialog()) dlg.ShowDialog(this); }

        private void ShowEarningsRadarDialog() { using (var dlg = new EarningsRadarDialog(_portfolio.Items)) dlg.ShowDialog(this); }

        private void ShowMarketCloseReportDialog()
        {
            if (_portfolio == null || _portfolio.Items.Count == 0) { MessageBox.Show("현재 포트폴리오에 등록된 종목이 없습니다.", "장마감 결산", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            using (var dlg = new MarketCloseReportDialog(_portfolio.Items, _macroSnapshot)) dlg.ShowDialog(this);
        }

        private void ShowMarketLeaderRadarDialog(string initialSelectCode = null)
        {
            var existingCodes = new HashSet<string>(_portfolio.Items.Select(x => x.StockCode.TrimStart('0')), StringComparer.OrdinalIgnoreCase);
            using (var dlg = new MarketLeaderRadarDialog(existingCodes, AddStockToWatchlist, initialSelectCode)) dlg.ShowDialog(this);
        }

        public void AddStockToWatchlist(string code, string name, decimal price)
        {
            if (string.IsNullOrWhiteSpace(code) || _portfolio == null) return;
            string cleanCode = code.Trim();

            // 중복 방지
            var existing = _portfolio.Items.FirstOrDefault(x => x.StockCode.TrimStart('0').Equals(cleanCode.TrimStart('0'), StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                SelectAndFocusStock(existing.StockCode);
                return;
            }

            var newItem = new StockHoldingItem
            {
                StockCode = cleanCode,
                StockName = name,
                CurrentPrice = price,
                BuyPrice = 0m,
                Quantity = 0,
                Memo = "주도주 AI 매수 레이더에서 관심 종목으로 발굴 및 등록",
                IsPriceScrapeSuccess = price > 0
            };

            if (_filterMode == StockFilterMode.HoldingOnly)
            {
                _filterMode = StockFilterMode.All;
            }

            _portfolio.Items.Add(newItem);
            SavePortfolio();
            UpdateUi();
            SelectAndFocusStock(cleanCode);

            // 비동기로 즉시 60일 일봉/수급/지뢰 분석 실행하여 타점 보강 및 자동 저장
            _ = Task.Run(async () =>
            {
                try
                {
                    var indTask = TechnicalIndicatorService.AnalyzeIndicatorsAsync(newItem.StockCode, newItem.CurrentPrice);
                    var intelTask = StockIntelligenceService.AnalyzeAsync(newItem.StockCode);
                    await Task.WhenAll(indTask, intelTask);

                    var ind = indTask.Result;
                    var intel = intelTask.Result;

                    if (ind != null)
                    {
                        newItem.Rsi = ind.Rsi;
                        newItem.VolumeRatio = ind.VolumeRatio;
                        newItem.SupportPrice = ind.SupportPrice;
                        newItem.ResistancePrice = ind.ResistancePrice;
                        newItem.DownsideRiskPercent = ind.DownsideRiskPercent;
                        newItem.ChartScore = ind.ChartScore;
                        newItem.RiskScore = ind.RiskScore;
                        newItem.TimingBadge = ind.TimingBadge;
                        newItem.TimingWarning = ind.TimingWarning;
                        newItem.RecentCandles = ind.Candles;
                        newItem.RecommendedBuyPrice = ind.RecommendedBuyPrice;
                        newItem.SecondaryBuyPrice = ind.SecondaryBuyPrice;
                        newItem.TargetStopPrice = ind.TargetStopPrice;
                        newItem.RecommendedTargetPrice = ind.RecommendedTargetPrice;
                        newItem.SecondaryTargetPrice = ind.SecondaryTargetPrice;
                    }
                    if (intel != null)
                    {
                        newItem.MajorSupplyBadge = intel.SupplyBadge;
                        newItem.MajorSupplyDetail = intel.SupplyDetail;
                        newItem.LandmineBadge = intel.LandmineBadge;
                        newItem.LandmineDetail = intel.LandmineDetail;
                        newItem.HasCriticalLandmine = intel.HasCriticalLandmine;
                        newItem.DebtRatio = intel.DebtRatio;
                        newItem.ConsecutiveLossYears = intel.ConsecutiveLossYears;
                    }
                    if (ind != null && intel != null)
                    {
                        var unifiedScore = StockIntelligenceService.CalculateUnifiedScores(ind, intel, 0);
                        newItem.ChartScore = unifiedScore.ChartScore;
                        newItem.SupplyFinanceScore = unifiedScore.SupplyFinanceScore;
                        newItem.NewsScore = unifiedScore.NewsScore;
                        newItem.RiskScore = unifiedScore.RiskScore;
                        newItem.FinalScore = unifiedScore.FinalScore;
                    }

                    SavePortfolio();
                    if (this.IsHandleCreated && !this.IsDisposed)
                    {
                        this.BeginInvoke(new Action(UpdateUi));
                    }
                }
                catch { }
            });
        }

        #endregion
    }
}
