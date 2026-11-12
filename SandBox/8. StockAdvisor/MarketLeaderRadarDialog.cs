using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GateHelper.StockAdvisor.Models;
using GateHelper.StockAdvisor.Services;

namespace GateHelper.StockAdvisor
{
    public class MarketLeaderRadarDialog : Form
    {
        private readonly HashSet<string> _existingCodes;
        private readonly Action<string, string, decimal> _onAddWatchlist;
        private readonly Func<string, Form, bool> _onEvaluateStock;
        private readonly string _initialSelectCode;

        private List<MarketLeaderItem> _allLeaders = new List<MarketLeaderItem>();
        private RadarStrategyType _currentTab = RadarStrategyType.GoldenPullback;

        // UI 요소
        private Label _lblStatus;
        private ProgressBar _pbScan;
        private Button _btnTabGolden;
        private Button _btnTabOversold;
        private Button _btnTabVolume;
        private Button _btnTabAll;
        private CheckBox _chkIncludeEtf;
        private ListView _lvRadar;
        private Label _lblGuideBanner;
        private Button _btnRefresh;
        private Button _btnClose;

        private static readonly Font BoldFont = new Font("Segoe UI", 9.2f, FontStyle.Bold);

        public MarketLeaderRadarDialog(
            HashSet<string> existingCodes,
            Action<string, string, decimal> onAddWatchlist,
            string initialSelectCode = null,
            Func<string, Form, bool> onEvaluateStock = null)
        {
            _existingCodes = existingCodes != null
                ? new HashSet<string>(existingCodes.Select(x => x.TrimStart('0')), StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _onAddWatchlist = onAddWatchlist;
            _onEvaluateStock = onEvaluateStock;
            _initialSelectCode = initialSelectCode;

            this.Text = "주도주 AI 매수 레이더 (코스피 100 + 코스닥 50)";
            this.Size = new Size(1100, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(18, 22, 32);
            this.ForeColor = Color.White;
            this.Font = new Font("Malgun Gothic", 9f);
            this.MinimumSize = new Size(950, 600);

            InitializeCustomUi();
            this.Shown += async (s, e) =>
            {
                this.ActiveControl = _btnClose;
                await ScanRadarAsync(false);
            };
        }

        private void InitializeCustomUi()
        {
            // 1. 상단 타이틀 헤더 바 (84px, 서브타이틀 및 스캔 상태 겹침 100% 방지)
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 84,
                BackColor = Color.FromArgb(24, 28, 40),
                Padding = new Padding(16, 8, 16, 6)
            };

            var lblTitle = new Label
            {
                Text = "★ 주도주 AI 매수 레이더 (Market Leader Radar)",
                Font = new Font("Malgun Gothic", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 110, 210),
                Dock = DockStyle.Top,
                Height = 26
            };

            var lblSubtitle = new Label
            {
                Text = "시장 거래대금 최상위 150대 주도주 중에서 AI 퀀트 3대 매수 급소(골든 눌림목·과매도 반등·거래대금 폭발)를 실시간 포착합니다.",
                Font = new Font("Malgun Gothic", 8.8f),
                ForeColor = Color.FromArgb(170, 190, 220),
                Dock = DockStyle.Top,
                Height = 22
            };

            _lblStatus = new Label
            {
                Text = "주도주 풀 스캔 준비 중...",
                Font = new Font("Malgun Gothic", 8.5f),
                ForeColor = Color.FromArgb(0, 216, 255),
                Dock = DockStyle.Bottom,
                Height = 20
            };

            _pbScan = new ProgressBar
            {
                Dock = DockStyle.Bottom,
                Height = 3,
                Style = ProgressBarStyle.Continuous,
                Visible = false
            };

            pnlHeader.Controls.Add(_lblStatus);
            pnlHeader.Controls.Add(_pbScan);
            pnlHeader.Controls.Add(lblSubtitle);
            pnlHeader.Controls.Add(lblTitle);

            // 2. 전략 선택 탭 바 (36px)
            var pnlTabBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 38,
                BackColor = Color.FromArgb(16, 20, 28),
                Padding = new Padding(12, 5, 12, 5)
            };

            var flpTabs = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };

            _btnTabGolden = CreateTabButton("★ 골든 눌림목 (0)", RadarStrategyType.GoldenPullback);
            _btnTabOversold = CreateTabButton("★ 과매도 반등 (0)", RadarStrategyType.OversoldBounce);
            _btnTabVolume = CreateTabButton("★ 거래 폭발 (0)", RadarStrategyType.VolumeBreakout);
            _btnTabAll = CreateTabButton("전체 주도주 TOP 100", RadarStrategyType.AllLeaders);

            _chkIncludeEtf = new CheckBox
            {
                Text = "ETF/ETN 포함",
                Font = new Font("Malgun Gothic", 9f),
                ForeColor = Color.FromArgb(200, 220, 245),
                Dock = DockStyle.Right,
                Width = 115,
                Checked = false,
                Cursor = Cursors.Hand
            };
            _chkIncludeEtf.CheckedChanged += (s, e) =>
            {
                UpdateTabCounters();
                RenderCurrentTabItems();
            };

            flpTabs.Controls.Add(_btnTabGolden);
            flpTabs.Controls.Add(_btnTabOversold);
            flpTabs.Controls.Add(_btnTabVolume);
            flpTabs.Controls.Add(_btnTabAll);
            pnlTabBar.Controls.Add(_chkIncludeEtf);
            pnlTabBar.Controls.Add(flpTabs);

            // 3. 메인 레이더 리스트뷰
            _lvRadar = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BackColor = Color.FromArgb(18, 20, 26),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.2f),
                BorderStyle = BorderStyle.None,
                ShowItemToolTips = true
            };

            _lvRadar.Columns.Add("순위", 45, HorizontalAlignment.Center);
            _lvRadar.Columns.Add("시장", 58, HorizontalAlignment.Center);
            _lvRadar.Columns.Add("코드", 65, HorizontalAlignment.Center);
            _lvRadar.Columns.Add("종목명", 130);
            _lvRadar.Columns.Add("현재가", 85, HorizontalAlignment.Right);
            _lvRadar.Columns.Add("등락률", 75, HorizontalAlignment.Right);
            _lvRadar.Columns.Add("거래대금", 100, HorizontalAlignment.Right);
            _lvRadar.Columns.Add("14일 RSI", 65, HorizontalAlignment.Right);
            _lvRadar.Columns.Add("20일선 이격", 80, HorizontalAlignment.Right);
            _lvRadar.Columns.Add("AI 전략 타점", 115, HorizontalAlignment.Center);
            _lvRadar.Columns.Add("AI 권고 매수가", 100, HorizontalAlignment.Right);
            _lvRadar.Columns.Add("관심 등록", 90, HorizontalAlignment.Center);

            // 리스트 더블 버퍼링
            typeof(ListView).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
                null, _lvRadar, new object[] { true });

            _lvRadar.SelectedIndexChanged += LvRadar_SelectedIndexChanged;

            // 종목 행 더블클릭 시 종목 평가 다이얼로그로 즉시 이동
            _lvRadar.DoubleClick += (s, e) =>
            {
                if (_lvRadar.SelectedItems.Count > 0 && _lvRadar.SelectedItems[0].Tag is MarketLeaderItem sel && !string.IsNullOrEmpty(sel.StockCode))
                {
                    _onEvaluateStock?.Invoke(sel.StockCode, this);
                }
            };

            // 테이블 내 [관심 등록] 컬럼의 [+ 담기] 셀 클릭 시에만 등록 (행 더블클릭 자동 등록 제거)
            _lvRadar.MouseClick += (s, e) =>
            {
                var hit = _lvRadar.HitTest(e.Location);
                if (hit.Item != null && hit.SubItem != null)
                {
                    int colIndex = hit.Item.SubItems.IndexOf(hit.SubItem);
                    if (colIndex == 11 || hit.SubItem.Text == "+ 담기")
                    {
                        AddLeaderItemToWatchlist(hit.Item);
                    }
                }
            };

            // 마우스 호버 시 [+ 담기] 셀 위에서 손가락(Hand) 커서 피드백
            _lvRadar.MouseMove += (s, e) =>
            {
                var hit = _lvRadar.HitTest(e.Location);
                if (hit.Item != null && hit.SubItem != null && hit.SubItem.Text == "+ 담기")
                {
                    _lvRadar.Cursor = Cursors.Hand;
                }
                else
                {
                    _lvRadar.Cursor = Cursors.Default;
                }
            };

            // 4. 하단 슬림 가이드 및 버튼 바 (46px, 좌측 안내문구 + 우측 새로고침/닫기 단정 배치)
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 46,
                BackColor = Color.FromArgb(24, 28, 40),
                Padding = new Padding(16, 7, 16, 7)
            };

            _lblGuideBanner = new Label
            {
                Text = "[안내] 테이블 우측의 [+ 담기] 셀을 클릭하면 내 관심종목에 즉시 등록됩니다.",
                Font = new Font("Malgun Gothic", 9f),
                ForeColor = Color.FromArgb(215, 235, 255),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };

            _btnClose = new Button
            {
                Text = "닫기",
                Font = new Font("Malgun Gothic", 9f),
                Size = new Size(76, 32),
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(32, 38, 52),
                ForeColor = Color.FromArgb(200, 215, 230),
                Cursor = Cursors.Hand
            };
            _btnClose.FlatAppearance.BorderColor = Color.FromArgb(60, 75, 100);
            _btnClose.Click += (s, e) => this.Close();

            _btnRefresh = new Button
            {
                Text = "새로고침",
                Font = new Font("Malgun Gothic", 9f, FontStyle.Bold),
                Size = new Size(88, 32),
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(28, 42, 64),
                ForeColor = Color.FromArgb(100, 210, 255),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 8, 0)
            };
            _btnRefresh.FlatAppearance.BorderColor = Color.FromArgb(50, 130, 200);
            _btnRefresh.Click += async (s, e) => await ScanRadarAsync(true);

            // WinForms 도킹 순서: Right 버튼들 먼저 추가 후 Fill 레이블 추가
            pnlFooter.Controls.Add(_lblGuideBanner);
            pnlFooter.Controls.Add(_btnRefresh);
            pnlFooter.Controls.Add(_btnClose);

            // 도킹 추가
            this.Controls.Add(_lvRadar);
            this.Controls.Add(pnlTabBar);
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlFooter);
        }

        private Button CreateTabButton(string text, RadarStrategyType tab)
        {
            var btn = new Button
            {
                Text = text,
                Tag = tab,
                Font = new Font("Malgun Gothic", 8.8f, FontStyle.Bold),
                Size = new Size(160, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = (_currentTab == tab) ? Color.FromArgb(48, 20, 52) : Color.FromArgb(22, 26, 36),
                ForeColor = (_currentTab == tab) ? Color.FromArgb(255, 110, 210) : Color.FromArgb(140, 160, 185),
                Margin = new Padding(2, 0, 4, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = (_currentTab == tab) ? Color.FromArgb(255, 90, 200) : Color.FromArgb(40, 52, 70);
            btn.Click += (s, e) =>
            {
                _currentTab = tab;
                UpdateTabButtonStyles();
                RenderCurrentTabItems();
            };
            return btn;
        }

        private void UpdateTabButtonStyles()
        {
            UpdateButtonTab(_btnTabGolden, RadarStrategyType.GoldenPullback);
            UpdateButtonTab(_btnTabOversold, RadarStrategyType.OversoldBounce);
            UpdateButtonTab(_btnTabVolume, RadarStrategyType.VolumeBreakout);
            UpdateButtonTab(_btnTabAll, RadarStrategyType.AllLeaders);
        }

        private void UpdateButtonTab(Button btn, RadarStrategyType tab)
        {
            if (btn == null) return;
            bool active = _currentTab == tab;
            btn.BackColor = active ? Color.FromArgb(48, 20, 52) : Color.FromArgb(22, 26, 36);
            btn.ForeColor = active ? Color.FromArgb(255, 110, 210) : Color.FromArgb(140, 160, 185);
            btn.FlatAppearance.BorderColor = active ? Color.FromArgb(255, 90, 200) : Color.FromArgb(40, 52, 70);
        }

        private async Task ScanRadarAsync(bool forceRefresh)
        {
            _btnRefresh.Enabled = false;
            _pbScan.Visible = true;
            _pbScan.Value = 10;
            _lblStatus.Text = "코스피 100 + 코스닥 50 주도주 시세 및 기술적 퀀트 스캔 중...";

            var prog = new Progress<int>(pct =>
            {
                if (this.IsHandleCreated && !this.IsDisposed)
                {
                    this.BeginInvoke(new Action(() => _pbScan.Value = Math.Min(100, Math.Max(10, pct))));
                }
            });

            try
            {
                var leaders = await MarketLeaderRadarService.ScanMarketLeadersAsync(forceRefresh, prog);
                _allLeaders = leaders ?? new List<MarketLeaderItem>();

                // 이미 포트폴리오에 존재하는지 체크
                foreach (var item in _allLeaders)
                {
                    item.IsAlreadyInPortfolio = _existingCodes.Contains(item.StockCode.TrimStart('0'));
                }

                if (!string.IsNullOrEmpty(_initialSelectCode))
                {
                    var match = _allLeaders.FirstOrDefault(x => x.StockCode.TrimStart('0').Equals(_initialSelectCode.TrimStart('0'), StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                    {
                        if (match.IsGoldenPullback) _currentTab = RadarStrategyType.GoldenPullback;
                        else if (match.IsOversoldBounce) _currentTab = RadarStrategyType.OversoldBounce;
                        else if (match.IsVolumeBreakout) _currentTab = RadarStrategyType.VolumeBreakout;
                        else _currentTab = RadarStrategyType.AllLeaders;
                        UpdateTabButtonStyles();
                    }
                }

                UpdateTabCounters();
                RenderCurrentTabItems(_initialSelectCode);
            }
            catch (Exception ex)
            {
                _lblStatus.Text = $"스캔 실패: {ex.Message}";
            }
            finally
            {
                _btnRefresh.Enabled = true;
                _pbScan.Visible = false;
            }
        }

        private void UpdateTabCounters()
        {
            if (_allLeaders == null) return;
            bool includeEtf = _chkIncludeEtf != null && _chkIncludeEtf.Checked;
            var pool = includeEtf ? _allLeaders : _allLeaders.Where(x => !x.IsEtf).ToList();

            int goldenCount = pool.Count(x => x.IsGoldenPullback);
            int oversoldCount = pool.Count(x => x.IsOversoldBounce);
            int volumeCount = pool.Count(x => x.IsVolumeBreakout);

            _btnTabGolden.Text = $"★ 골든 눌림목 ({goldenCount})";
            _btnTabOversold.Text = $"★ 과매도 반등 ({oversoldCount})";
            _btnTabVolume.Text = $"★ 거래 폭발 ({volumeCount})";
            _btnTabAll.Text = $"전체 주도주 ({pool.Count})";

            string etfGuide = includeEtf ? " [ETF 포함]" : " [개별주만]";
            _lblStatus.Text = $"스캔 완료: 총 {pool.Count}종목{etfGuide} | 골든 눌림목 {goldenCount}종목 | 과매도 반등 {oversoldCount}종목 | 거래대금 폭발 {volumeCount}종목";
        }

        private void RenderCurrentTabItems(string selectCode = null)
        {
            _lvRadar.BeginUpdate();
            _lvRadar.Items.Clear();

            bool includeEtf = _chkIncludeEtf != null && _chkIncludeEtf.Checked;
            IEnumerable<MarketLeaderItem> basePool = _allLeaders ?? Enumerable.Empty<MarketLeaderItem>();
            if (!includeEtf)
            {
                basePool = basePool.Where(x => !x.IsEtf);
            }

            IEnumerable<MarketLeaderItem> targets = basePool;
            switch (_currentTab)
            {
                case RadarStrategyType.GoldenPullback:
                    targets = basePool.Where(x => x.IsGoldenPullback);
                    break;
                case RadarStrategyType.OversoldBounce:
                    targets = basePool.Where(x => x.IsOversoldBounce);
                    break;
                case RadarStrategyType.VolumeBreakout:
                    targets = basePool.Where(x => x.IsVolumeBreakout);
                    break;
                case RadarStrategyType.AllLeaders:
                default:
                    targets = basePool.Take(100);
                    break;
            }

            int rank = 1;
            foreach (var itm in targets)
            {
                string rsiStr = itm.Rsi > 0 ? $"{itm.Rsi:F1}" : "-";
                string dispStr = itm.Ma20 > 0 ? $"{itm.Ma20DisparityRate:+0.0;-0.0}%" : "-";
                string buyStr = itm.RecommendedBuyPrice > 0 ? $"{itm.RecommendedBuyPrice:N0}" : "-";
                string regStr = itm.IsAlreadyInPortfolio ? "★ 등록됨" : "+ 담기";

                var lvi = new ListViewItem(new[]
                {
                    $"{rank++}",
                    itm.MarketType,
                    itm.StockCode,
                    itm.StockName,
                    $"{itm.CurrentPrice:N0}",
                    $"{itm.ChangeRate:+0.00;-0.00;0.00}%",
                    itm.TradingValueText,
                    rsiStr,
                    dispStr,
                    itm.PrimaryStrategyBadge,
                    buyStr,
                    regStr
                });

                lvi.Tag = itm;

                // 스타일링
                lvi.SubItems[0].ForeColor = Color.FromArgb(140, 160, 190);
                lvi.SubItems[1].ForeColor = itm.MarketType == "KOSPI" ? Color.FromArgb(80, 200, 255) : Color.FromArgb(255, 170, 70);
                lvi.SubItems[2].ForeColor = Color.FromArgb(150, 170, 200);
                lvi.SubItems[3].ForeColor = Color.FromArgb(235, 245, 255);
                lvi.SubItems[3].Font = BoldFont;

                // 등락률
                if (itm.ChangeRate > 0) lvi.SubItems[5].ForeColor = Color.FromArgb(255, 95, 95);
                else if (itm.ChangeRate < 0) lvi.SubItems[5].ForeColor = Color.FromArgb(85, 160, 255);
                else lvi.SubItems[5].ForeColor = Color.FromArgb(180, 190, 205);

                // 거래대금 (1,000억 이상 골드)
                if (itm.TradingValueRaw >= 100000000000L) lvi.SubItems[6].ForeColor = Color.FromArgb(255, 215, 60);
                else lvi.SubItems[6].ForeColor = Color.FromArgb(210, 225, 240);

                // RSI
                if (itm.Rsi <= 36.5m && itm.Rsi > 0) lvi.SubItems[7].ForeColor = Color.FromArgb(80, 200, 255);
                else if (itm.Rsi >= 68m) lvi.SubItems[7].ForeColor = Color.FromArgb(255, 90, 90);
                else lvi.SubItems[7].ForeColor = Color.FromArgb(130, 225, 160);

                // 전략 타점 뱃지
                if (itm.IsGoldenPullback) lvi.SubItems[9].ForeColor = Color.FromArgb(0, 230, 118);
                else if (itm.IsOversoldBounce) lvi.SubItems[9].ForeColor = Color.FromArgb(0, 220, 255);
                else if (itm.IsVolumeBreakout) lvi.SubItems[9].ForeColor = Color.FromArgb(255, 190, 40);
                else if (itm.PrimaryStrategyBadge != null && itm.PrimaryStrategyBadge.Contains("급락주의")) lvi.SubItems[9].ForeColor = Color.FromArgb(255, 160, 60);
                else if (itm.PrimaryStrategyBadge != null && itm.PrimaryStrategyBadge.Contains("매수금지")) lvi.SubItems[9].ForeColor = Color.FromArgb(255, 95, 110);
                else if (itm.PrimaryStrategyBadge != null && itm.PrimaryStrategyBadge.Contains("분할매수")) lvi.SubItems[9].ForeColor = Color.FromArgb(100, 240, 160);
                else lvi.SubItems[9].ForeColor = Color.FromArgb(160, 175, 200);
                lvi.SubItems[9].Font = BoldFont;

                // 권고 매수가
                lvi.SubItems[10].ForeColor = Color.FromArgb(0, 216, 255);

                // 관심 등록 상태
                lvi.SubItems[11].ForeColor = itm.IsAlreadyInPortfolio ? Color.FromArgb(0, 216, 255) : Color.FromArgb(255, 140, 210);
                lvi.SubItems[11].Font = BoldFont;

                lvi.UseItemStyleForSubItems = false;
                _lvRadar.Items.Add(lvi);
            }

            _lvRadar.EndUpdate();

            if (_lvRadar.Items.Count > 0)
            {
                ListViewItem targetLvi = null;
                if (!string.IsNullOrEmpty(selectCode))
                {
                    string norm = selectCode.TrimStart('0');
                    foreach (ListViewItem lvi in _lvRadar.Items)
                    {
                        if (lvi.Tag is MarketLeaderItem m && m.StockCode.TrimStart('0').Equals(norm, StringComparison.OrdinalIgnoreCase))
                        {
                            targetLvi = lvi;
                            break;
                        }
                    }
                }
                if (targetLvi != null)
                {
                    targetLvi.Selected = true;
                    targetLvi.EnsureVisible();
                    _lvRadar.Focus();
                }
                else
                {
                    _lvRadar.SelectedItems.Clear();
                    if (_lvRadar.FocusedItem != null) _lvRadar.FocusedItem.Focused = false;
                }
            }
            else
            {
                _lblGuideBanner.Text = "[안내] 현재 선택된 전략 조건에 부합하는 종목이 없습니다. 상단 다른 전략 탭을 눌러보세요.";
            }
        }

        private void LvRadar_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_lvRadar.SelectedItems.Count > 0 && _lvRadar.SelectedItems[0].Tag is MarketLeaderItem itm)
            {
                string statusTxt = itm.IsAlreadyInPortfolio ? "★ 이미 등록됨" : "우측 [+ 담기] 클릭 시 관심 등록";
                _lblGuideBanner.Text = $"[{itm.StockName}] {itm.PrimaryStrategyGuide} | 권고 매수가: {itm.RecommendedBuyPrice:N0}원 ({statusTxt})";
            }
        }

        private void AddLeaderItemToWatchlist(ListViewItem lvi)
        {
            if (lvi == null) return;
            var itm = lvi.Tag as MarketLeaderItem;
            if (itm == null) return;

            if (itm.IsAlreadyInPortfolio)
            {
                ToastNotification.Show($"[{itm.StockName}] 종목은 이미 포트폴리오/관심종목에 등록되어 있습니다.", "알림", ToastIcon.Info);
                return;
            }

            try
            {
                _onAddWatchlist?.Invoke(itm.StockCode, itm.StockName, itm.CurrentPrice);
                itm.IsAlreadyInPortfolio = true;
                _existingCodes.Add(itm.StockCode);

                lvi.SubItems[11].Text = "★ 등록됨";
                lvi.SubItems[11].ForeColor = Color.FromArgb(0, 216, 255);

                _lblGuideBanner.Text = $"[완료] [{itm.StockName}] 관심종목 추가 완료! (AI 권고 매수가: {itm.RecommendedBuyPrice:N0}원)";

                string toastMsg = string.Format("[{0}]\n관심 종목에 등록되었습니다.", itm.StockName);
                if (itm.RecommendedBuyPrice > 0)
                {
                    toastMsg += string.Format(" (매수가: {0:N0}원)", itm.RecommendedBuyPrice);
                }
                ToastNotification.Show(toastMsg, "관심 종목 등록 완료", ToastIcon.Success);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"등록 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
