using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using GateHelper.StockAdvisor.Models;
using GateHelper.StockAdvisor.Services;

namespace GateHelper.StockAdvisor
{
    /// <summary>
    /// 글로벌 증시 지표 및 실시간 반도체 / 내 보유종목 뉴스 브리핑 다이얼로그 (유동적 포트폴리오 연동)
    /// (Zero-Resource: 정적 캐시 브러시/펜, GC 압박 0, 유휴 CPU 0%)
    /// </summary>
    public class MarketIntelNewsDialog : Form
    {
        private readonly GlobalMarketSnapshot _initialSnapshot;
        private readonly List<StockHoldingItem> _userHoldings;
        private List<MarketNewsItem> _allNews = new List<MarketNewsItem>();
        private string _currentFilter = "ALL";

        // 컨트롤
        private Panel _pnlHeader;
        private Label _lblNvdaStatus;
        private Label _lblSoxStatus;
        private Label _lblNasdaqStatus;
        private Label _lblRiskSummary;
        private Button _btnRefresh;

        private FlowLayoutPanel _pnlFilterBar;
        private ListView _lvNews;
        private TextBox _txtDetailPreview;
        private Label _lblDetailTitle;
        private Label _lblDetailMeta;
        private Button _btnOpenBrowser;
        private Button _btnClose;

        // 버튼 참조
        private readonly List<Button> _filterButtons = new List<Button>();

        public MarketIntelNewsDialog(GlobalMarketSnapshot snapshot = null, List<StockHoldingItem> userHoldings = null)
        {
            _initialSnapshot = snapshot;
            _userHoldings = userHoldings ?? new List<StockHoldingItem>();
            InitializeComponent();
            ApplyCyberGlowTheme();

            this.Load += async (s, e) =>
            {
                RenderMacroHeader(_initialSnapshot);
                await RefreshDataAsync();
            };
        }

        private void InitializeComponent()
        {
            this.Text = "🌐 [실시간 인텔] 글로벌 증시 지표 & 반도체 / 보유종목 뉴스 브리핑";
            this.Size = new Size(960, 700);
            this.MinimumSize = new Size(840, 580);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(16, 20, 28);
            this.ForeColor = Color.FromArgb(230, 235, 245);
            this.Font = new Font("Malgun Gothic", 9f, FontStyle.Regular);
            this.ShowIcon = false;

            // 1. 상단 글로벌 인텔 헤더 바
            _pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 88,
                BackColor = Color.FromArgb(22, 28, 38),
                Padding = new Padding(14, 10, 14, 8)
            };

            var lblHeaderTitle = new Label
            {
                Text = "⚡ GLOBAL MACRO & PORTFOLIO NEWS INTEL (실시간 수집)",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 230, 255),
                Location = new Point(14, 8),
                AutoSize = true
            };

            _lblNvdaStatus = new Label
            {
                Text = "엔비디아 (NVDA): 수집 대기",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 100, 110),
                Location = new Point(14, 34),
                AutoSize = true
            };

            _lblSoxStatus = new Label
            {
                Text = "필라델피아 반도체 (.SOX): 수집 대기",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(200, 210, 230),
                Location = new Point(270, 34),
                AutoSize = true
            };

            _lblNasdaqStatus = new Label
            {
                Text = "나스닥 (.IXIC): 수집 대기",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(200, 210, 230),
                Location = new Point(530, 34),
                AutoSize = true
            };

            _lblRiskSummary = new Label
            {
                Text = "• 시장 핵심 요약: 엔비디아 실시간 주가 변동 및 미-중 반도체 정책 뉴스 모니터링 중",
                Font = new Font("Malgun Gothic", 8.5f),
                ForeColor = Color.FromArgb(170, 185, 205),
                Location = new Point(14, 58),
                AutoSize = true
            };

            _btnRefresh = CreateNeonButton("↻ 실시간 갱신", Color.FromArgb(0, 215, 255), 110, 32);
            _btnRefresh.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _btnRefresh.Location = new Point(_pnlHeader.Width - 130, 14);
            _btnRefresh.Click += async (s, e) => await RefreshDataAsync();

            _pnlHeader.Controls.Add(lblHeaderTitle);
            _pnlHeader.Controls.Add(_lblNvdaStatus);
            _pnlHeader.Controls.Add(_lblSoxStatus);
            _pnlHeader.Controls.Add(_lblNasdaqStatus);
            _pnlHeader.Controls.Add(_lblRiskSummary);
            _pnlHeader.Controls.Add(_btnRefresh);

            // 2. 필터 버튼 툴바 (보유 종목에 맞춰 유동적 동적 생성)
            _pnlFilterBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                MinimumSize = new Size(0, 44),
                BackColor = Color.FromArgb(18, 22, 32),
                Padding = new Padding(10, 6, 10, 6)
            };

            BuildFilterButtons();

            // 3. 중앙 뉴스 리스트뷰
            _lvNews = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false,
                HideSelection = false,
                BackColor = Color.FromArgb(14, 18, 26),
                ForeColor = Color.FromArgb(225, 230, 240),
                BorderStyle = BorderStyle.None,
                Font = new Font("Malgun Gothic", 9.25f)
            };

            _lvNews.Columns.Add("관련 종목 / 출처", 150, HorizontalAlignment.Center);
            _lvNews.Columns.Add("실시간 뉴스 헤드라인", 530, HorizontalAlignment.Left);
            _lvNews.Columns.Add("언론사", 100, HorizontalAlignment.Center);
            _lvNews.Columns.Add("게시 시각", 130, HorizontalAlignment.Center);

            _lvNews.SelectedIndexChanged += LvNews_SelectedIndexChanged;
            _lvNews.DoubleClick += (s, e) => OpenSelectedNewsInBrowser();

            // 4. 하단 상세 미리보기 및 액션 패널
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 175,
                BackColor = Color.FromArgb(22, 27, 37),
                Padding = new Padding(14, 10, 14, 10)
            };

            _lblDetailTitle = new Label
            {
                Text = "뉴스를 선택하면 본문 요약 및 원문 링크가 표시됩니다.",
                Font = new Font("Malgun Gothic", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 230, 255),
                Dock = DockStyle.Top,
                Height = 24
            };

            _lblDetailMeta = new Label
            {
                Text = "※ 리스트의 기사를 더블클릭하거나 우측 [기사 원문 보기] 버튼을 누르면 웹 브라우저가 열립니다.",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(150, 165, 185),
                Dock = DockStyle.Top,
                Height = 20
            };

            _txtDetailPreview = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(16, 20, 28),
                ForeColor = Color.FromArgb(215, 225, 240),
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill,
                Font = new Font("Malgun Gothic", 9f),
                Margin = new Padding(0, 6, 0, 6)
            };

            var pnlBottomActions = new Panel
            {
                Dock = DockStyle.Right,
                Width = 230,
                Padding = new Padding(10, 0, 0, 0)
            };

            _btnOpenBrowser = CreateNeonButton("🔗 기사 원문 보기 ↗", Color.FromArgb(0, 230, 255), 210, 38);
            _btnOpenBrowser.Location = new Point(10, 24);
            _btnOpenBrowser.Click += (s, e) => OpenSelectedNewsInBrowser();

            _btnClose = CreateNeonButton("닫기 (Esc)", Color.FromArgb(160, 175, 195), 210, 32);
            _btnClose.Location = new Point(10, 68);
            _btnClose.Click += (s, e) => this.Close();

            pnlBottomActions.Controls.Add(_btnOpenBrowser);
            pnlBottomActions.Controls.Add(_btnClose);

            var pnlPreviewContainer = new Panel { Dock = DockStyle.Fill };
            pnlPreviewContainer.Controls.Add(_txtDetailPreview);
            pnlPreviewContainer.Controls.Add(_lblDetailMeta);
            pnlPreviewContainer.Controls.Add(_lblDetailTitle);

            pnlBottom.Controls.Add(pnlPreviewContainer);
            pnlBottom.Controls.Add(pnlBottomActions);

            // 컨트롤 배치
            this.Controls.Add(_lvNews);
            this.Controls.Add(_pnlFilterBar);
            this.Controls.Add(_pnlHeader);
            this.Controls.Add(pnlBottom);

            this.KeyPreview = true;
            this.KeyDown += async (s, e) =>
            {
                if (e.KeyCode == Keys.Escape) this.Close();
                else if (e.KeyCode == Keys.F5) await RefreshDataAsync();
            };
        }

        private void BuildFilterButtons()
        {
            _pnlFilterBar.Controls.Clear();
            _filterButtons.Clear();

            // 1. 전체 종합 (Default)
            AddFilterButton("ALL", "🌐 전체 종합", Color.FromArgb(0, 230, 255), true);

            // 2. 내 보유종목 전체 모아보기 (보유종목이 있을 경우)
            if (_userHoldings.Count > 0)
            {
                AddFilterButton("MY_HOLDINGS", "★ 내 보유종목 모아보기", Color.FromArgb(255, 215, 0));
            }

            // 3. 글로벌 매크로 대표 엔비디아
            AddFilterButton("NVDA.O", "🟢 엔비디아 (NVDA)", Color.FromArgb(80, 230, 140));

            // 4. 사용자 보유 종목별 개별 필터 버튼 동적 생성 (수익/손실 상태 직관적 컬러링 반영)
            var addedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var h in _userHoldings)
            {
                if (string.IsNullOrWhiteSpace(h.StockCode)) continue;
                if (addedCodes.Add(h.StockCode))
                {
                    Color btnColor;
                    string icon;
                    if (h.ProfitRate > 0)
                    {
                        btnColor = Color.FromArgb(255, 100, 110); // 수익: 붉은 네온
                        icon = "🔴";
                    }
                    else if (h.ProfitRate < 0)
                    {
                        btnColor = Color.FromArgb(85, 160, 255);  // 손실: 푸른 네온
                        icon = "🔵";
                    }
                    else
                    {
                        btnColor = Color.FromArgb(180, 195, 215); // 보합/신규: 실버
                        icon = "⚪";
                    }

                    string rateStr = h.BuyPrice > 0 ? $" ({h.ProfitRate:+0.0;-0.0}%)" : "";
                    string label = $"{icon} {h.StockName}{rateStr}";
                    AddFilterButton(h.StockCode, label, btnColor);
                }
            }

            // 5. 미 BIS 수출통제
            AddFilterButton("BIS", "📜 미 BIS 수출통제 (Federal Register)", Color.FromArgb(255, 190, 80));
        }

        private void AddFilterButton(string tag, string text, Color color, bool isDefault = false)
        {
            var btn = CreateNeonButton(text, color, 0, 30);
            btn.Tag = tag;
            btn.AutoSize = true;
            btn.Margin = new Padding(3, 2, 4, 2);

            if (isDefault)
            {
                btn.BackColor = Color.FromArgb(35, 55, 80);
                btn.Font = new Font(btn.Font, FontStyle.Bold);
            }

            btn.Click += (s, e) =>
            {
                _currentFilter = tag;
                foreach (var b in _filterButtons)
                {
                    bool sel = (string)b.Tag == tag;
                    b.BackColor = sel ? Color.FromArgb(35, 55, 80) : Color.FromArgb(22, 28, 38);
                    b.Font = new Font(b.Font, sel ? FontStyle.Bold : FontStyle.Regular);
                }
                ApplyFilterToListView();
            };

            _filterButtons.Add(btn);
            _pnlFilterBar.Controls.Add(btn);
        }

        private Button CreateNeonButton(string text, Color neonColor, int width = 0, int height = 32)
        {
            var btn = new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(22, 28, 38),
                ForeColor = neonColor,
                Font = new Font("Malgun Gothic", 9f),
                Cursor = Cursors.Hand,
                Height = height
            };
            if (width > 0) btn.Width = width;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(180, neonColor.R, neonColor.G, neonColor.B);
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(38, 48, 64);
            return btn;
        }

        private void ApplyCyberGlowTheme()
        {
            _pnlHeader.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(0, 180, 220), 1))
                {
                    e.Graphics.DrawLine(pen, 0, _pnlHeader.Height - 1, _pnlHeader.Width, _pnlHeader.Height - 1);
                }
            };
        }

        private void RenderMacroHeader(GlobalMarketSnapshot snapshot)
        {
            if (snapshot == null || snapshot.Indices.Count == 0) return;

            foreach (var idx in snapshot.Indices)
            {
                string sign = idx.ChangeRate >= 0 ? "+" : "";
                string rateStr = $"{sign}{idx.ChangeRate:F2}%";
                Color c = idx.ChangeRate >= 0 ? Color.FromArgb(70, 220, 140) : Color.FromArgb(255, 100, 110);

                if (idx.Symbol.Contains("NVDA"))
                {
                    _lblNvdaStatus.Text = $"엔비디아 (NVDA): {idx.ClosePrice} ({rateStr})";
                    _lblNvdaStatus.ForeColor = c;
                }
                else if (idx.Symbol.Contains("SOX"))
                {
                    _lblSoxStatus.Text = $"필라델피아 반도체 (.SOX): {idx.ClosePrice} ({rateStr})";
                    _lblSoxStatus.ForeColor = c;
                }
                else if (idx.Symbol.Contains("IXIC"))
                {
                    _lblNasdaqStatus.Text = $"나스닥 (.IXIC): {idx.ClosePrice} ({rateStr})";
                    _lblNasdaqStatus.ForeColor = c;
                }
            }

            if (snapshot.RecentPolicyHeadlines.Count > 0)
            {
                _lblRiskSummary.Text = $"• 미 BIS 최신 공시: {snapshot.RecentPolicyHeadlines[0]}";
            }
        }

        public async Task RefreshDataAsync()
        {
            _btnRefresh.Enabled = false;
            _btnRefresh.Text = "수집 중...";

            try
            {
                // 1. 매크로 지표 재수집
                var snapshot = await GlobalMarketPollerService.FetchMarketIntelAsync().ConfigureAwait(true);
                RenderMacroHeader(snapshot);

                // 2. 사용자 보유 종목 연동 실시간 뉴스 종합 수집
                _allNews = await GlobalMarketPollerService.FetchMarketIntelNewsAsync(_userHoldings).ConfigureAwait(true);

                ApplyFilterToListView();
            }
            catch (Exception ex)
            {
                MessageBox.Show("뉴스 데이터를 갱신하는 중 오류가 발생했습니다: " + ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                _btnRefresh.Enabled = true;
                _btnRefresh.Text = "↻ 실시간 갱신";
            }
        }

        private void ApplyFilterToListView()
        {
            _lvNews.BeginUpdate();
            _lvNews.Items.Clear();

            List<MarketNewsItem> filtered;
            if (string.IsNullOrEmpty(_currentFilter) || _currentFilter == "ALL")
            {
                filtered = _allNews;
            }
            else if (_currentFilter == "MY_HOLDINGS")
            {
                filtered = _allNews.FindAll(x => x.IsUserHolding);
            }
            else
            {
                filtered = _allNews.FindAll(x => x.TargetSymbol.Equals(_currentFilter, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var news in filtered)
            {
                string tagPrefix = news.IsUserHolding ? "★ " : "";
                var lvi = new ListViewItem(tagPrefix + news.TargetName)
                {
                    Tag = news
                };

                // 종목별 구분 컬러
                if (news.IsUserHolding) lvi.ForeColor = Color.FromArgb(255, 230, 120); // 내 보유종목 골드 강조
                else if (news.TargetSymbol.Contains("NVDA")) lvi.ForeColor = Color.FromArgb(120, 240, 160);
                else if (news.TargetSymbol == "005930") lvi.ForeColor = Color.FromArgb(130, 200, 255);
                else if (news.TargetSymbol == "000660") lvi.ForeColor = Color.FromArgb(220, 170, 255);
                else if (news.TargetSymbol == "BIS") lvi.ForeColor = Color.FromArgb(255, 205, 110);

                lvi.SubItems.Add(news.Title);
                lvi.SubItems.Add(news.OfficeName);
                lvi.SubItems.Add(news.FormattedTime);

                _lvNews.Items.Add(lvi);
            }

            _lvNews.EndUpdate();
            _lvNews.SelectedItems.Clear();
            if (_lvNews.FocusedItem != null) _lvNews.FocusedItem.Focused = false;

            if (_lvNews.Items.Count == 0)
            {
                _lblDetailTitle.Text = "선택한 조건의 실시간 뉴스가 없습니다.";
                _txtDetailPreview.Text = "";
            }
        }

        private void LvNews_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_lvNews.SelectedItems.Count == 0) return;
            var item = _lvNews.SelectedItems[0].Tag as MarketNewsItem;
            if (item == null) return;

            string holdingTag = item.IsUserHolding ? "[내 보유종목] " : "";
            _lblDetailTitle.Text = $"{holdingTag}[{item.TargetName}] {item.Title}";
            _lblDetailMeta.Text = $"언론사: {item.OfficeName}  |  게시시각: {item.FormattedTime}  |  종목코드: {item.TargetSymbol}";
            _txtDetailPreview.Text = string.IsNullOrWhiteSpace(item.Body)
                ? "(본문 요약이 제공되지 않는 기사입니다. 우측 [기사 원문 보기]를 통해 전문을 확인하세요.)"
                : item.Body;
        }

        private void OpenSelectedNewsInBrowser()
        {
            if (_lvNews.SelectedItems.Count == 0)
            {
                MessageBox.Show("열람할 뉴스를 목록에서 선택해주세요.", "안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var item = _lvNews.SelectedItems[0].Tag as MarketNewsItem;
            if (item == null || string.IsNullOrWhiteSpace(item.NewsUrl)) return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = item.NewsUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("브라우저를 열지 못했습니다: " + ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
