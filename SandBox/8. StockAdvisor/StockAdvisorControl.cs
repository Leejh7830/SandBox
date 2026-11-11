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
        private Button _btnEvaluateStock;
        private Button _btnRefreshPrice;
        private Button _btnOption;
        private Button _btnAddStock;
        private Button _btnEarningsRadar; // 실적 발표 D-Day 레이더
        private Button _btnMarketCloseReport; // 장마감 결산 & 포트폴리오 리뷰
        private Button _btnImportPortfolio;
        private Button _btnEmergencyGuide;

        private Timer _autoSyncTimer;
        private DateTime _lastAutoPriceSyncTime = DateTime.MinValue;
        private DateTime _lastAutoAiDiagnoseTime = DateTime.MinValue;

        private SplitContainer _splitMain;
        private ListView _lvStocks;
        private Panel _pnlDetail;

        // 상세 패널 요소 (슬림 4단 구조)
        private Label _lblDetailTitle;
        private Label _lblFinalVerdict;          // AI 참모 최종 결론 바 (0.1초 직관 배지)
        private Label _lblDetailPriceTargets;
        private Label _lblDetailSummary;
        private RichTextBox _rtbDetailReason;
        private Button _btnEditItem;
        private Button _btnDeleteItem;

        // 제로 리소스 폰트 캐시 (루프 내 new Font 방지, GC 0B)
        internal static readonly Font SubItemBoldFont = new Font("Segoe UI", 9.2f, FontStyle.Bold);

        // 하단 상태바
        private Panel _pnlFooter;
        private Label _lblAccountSummary;
        private Label _lblLastUpdated;
        private Timer _kbSyncTimer;

        // 비동기 작업 상태 플래그 (멀티태스킹 분리)
        private bool _isAiDiagnosing = false;
        private bool _isRefreshingPrice = false;
        private bool _hasInitialDiagnosed = false; // 최초 메인 화면 진입 시 1회 자동 진단 실행 가드

        // 리스트뷰 컬럼 정렬 상태
        private int _sortColumn = -1;
        private SortOrder _sortOrder = SortOrder.None;
        private static readonly string[] OriginalColumnNames = new[]
        {
            "코드", "종목명", "현재가", "전일대비", "평단", "보유", "손익금(원)", "수익률", "AI 등급",
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
            this.VisibleChanged += (s, e) =>
            {
                if (!this.Visible)
                {
                    _kbSyncTimer?.Stop();
                    _autoSyncTimer?.Stop();
                }
                else
                {
                    SetupKbSyncTimer();
                    SetupAutoSyncTimers();
                    _ = RefreshMarketThermometerAsync();
                    _ = RefreshEarningsScheduleAsync();
                    TriggerInitialDiagnosisIfNeeded();
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

            UpdateDiagnoseButtonText();
        }

        private void UpdateDiagnoseButtonText(string customStatus = null)
        {
            if (_btnDiagnose == null) return;

            if (!string.IsNullOrEmpty(customStatus))
            {
                _btnDiagnose.Text = $"AI 참모 진단\n{customStatus}";
                return;
            }

            if (_portfolio?.LastBriefing != null && _portfolio.LastBriefing.GeneratedAt != default && _portfolio.LastBriefing.GeneratedAt.Year > 2000)
            {
                _btnDiagnose.Text = $"AI 참모 진단\n✓ {_portfolio.LastBriefing.GeneratedAt:HH:mm:ss} 동기화";
            }
            else
            {
                _btnDiagnose.Text = "AI 참모 진단\n(진단 대기)";
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

            using (var dlg = new KeyStockSummaryDialog(_portfolio.Items))
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

        private void ShowStockEvaluatorDialog()
        {
            string initQuery = "";
            if (_lvStocks.SelectedItems.Count > 0)
            {
                var sel = _lvStocks.SelectedItems[0].Tag as StockHoldingItem;
                if (sel != null) initQuery = sel.StockName;
            }

            using (var dlg = new StockEvaluatorDialog(initQuery))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK && dlg.AddedStockItem != null)
                {
                    var newItem = dlg.AddedStockItem;
                    var existing = _portfolio.Items.FirstOrDefault(x => x.StockCode == newItem.StockCode);
                    if (existing != null)
                    {
                        // 기존 종목 업데이트
                        existing.BuyPrice = newItem.BuyPrice;
                        existing.Quantity += newItem.Quantity;
                        existing.CurrentPrice = newItem.CurrentPrice;
                        existing.ChangeRate = newItem.ChangeRate;
                    }
                    else
                    {
                        _portfolio.Items.Add(newItem);
                    }

                    SavePortfolio();
                    UpdateUi();
                    ToastNotification.Show($"[{newItem.StockName}] 종목이 포트폴리오에 성공적으로 추가되었습니다.", "종목 추가 완료", ToastIcon.Success);
                }
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

        private void ShowEarningsRadarDialog()
        {
            using (var dlg = new EarningsRadarDialog(_portfolio.Items))
            {
                dlg.ShowDialog(this);
            }
        }

        private void ShowMarketCloseReportDialog()
        {
            if (_portfolio == null || _portfolio.Items.Count == 0)
            {
                MessageBox.Show("현재 포트폴리오에 등록된 종목이 없습니다.", "장마감 결산", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new MarketCloseReportDialog(_portfolio.Items, _macroSnapshot))
            {
                dlg.ShowDialog(this);
            }
        }

        #endregion
    }
}
