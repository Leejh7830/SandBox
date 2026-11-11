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
    public class EarningsRadarDialog : Form
    {
        private readonly List<StockHoldingItem> _portfolioItems;
        private List<EarningsScheduleItem> _allSchedules = new List<EarningsScheduleItem>();

        private Label _lblSummaryAlert;
        private TabControl _tabMain;
        private ListView _lvHeld;
        private ListView _lvBellwether;
        private Button _btnRefresh;
        private Button _btnClose;

        public EarningsRadarDialog(List<StockHoldingItem> portfolio)
        {
            _portfolioItems = portfolio ?? new List<StockHoldingItem>();
            InitializeCustomComponent();
            _ = LoadDataAsync();
        }

        private void InitializeCustomComponent()
        {
            Text = "실시간 실적 발표 D-Day 레이더 & 어닝 리스크 가이드";
            Size = new Size(920, 640);
            MinimumSize = new Size(860, 560);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(16, 20, 28);
            ForeColor = Color.FromArgb(230, 235, 245);
            Font = new Font("Malgun Gothic", 9.2f);
            KeyPreview = true;
            KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) Close(); };

            var pnlMain = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 12, 16, 12) };

            // 1. 상단 타이틀 및 D-Day 요약 배너
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 74,
                BackColor = Color.FromArgb(24, 30, 44),
                Padding = new Padding(16, 10, 16, 10)
            };

            var lblTitle = new Label
            {
                Text = "📅 실적 발표 D-Day 레이더 (Earnings Radar)",
                Font = new Font("Malgun Gothic", 12.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 216, 255),
                Dock = DockStyle.Top,
                Height = 26
            };

            _lblSummaryAlert = new Label
            {
                Text = "⏳ 실시간 실적 발표 캘린더 및 DART 공시 동기화 중...",
                Font = new Font("Malgun Gothic", 9.2f),
                ForeColor = Color.FromArgb(200, 225, 250),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            pnlHeader.Controls.Add(_lblSummaryAlert);
            pnlHeader.Controls.Add(lblTitle);

            // 2. 하단 버튼 및 가이드 바
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                Padding = new Padding(0, 8, 0, 0)
            };

            _btnRefresh = new Button
            {
                Text = "🔄 새로고침",
                Size = new Size(110, 36),
                Dock = DockStyle.Left,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(24, 38, 56),
                ForeColor = Color.FromArgb(100, 210, 255),
                Cursor = Cursors.Hand,
                Font = new Font("Malgun Gothic", 9f, FontStyle.Bold)
            };
            _btnRefresh.FlatAppearance.BorderColor = Color.FromArgb(0, 180, 230);
            _btnRefresh.Click += async (s, e) => await LoadDataAsync();

            _btnClose = new Button
            {
                Text = "닫기 (Esc)",
                Size = new Size(100, 36),
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(32, 40, 56),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Font = new Font("Malgun Gothic", 9f)
            };
            _btnClose.FlatAppearance.BorderColor = Color.FromArgb(70, 90, 130);
            _btnClose.Click += (s, e) => Close();

            pnlFooter.Controls.Add(_btnRefresh);
            pnlFooter.Controls.Add(_btnClose);

            // 3. 하단 실적 시즌 3대 행동 원칙 가이드 카드
            var pnlGuide = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 72,
                BackColor = Color.FromArgb(20, 26, 38),
                Padding = new Padding(12, 6, 12, 6),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblGuide = new Label
            {
                Text = "【 💡 AI 참모 실적 시즌 3대 행동 수칙 】\n" +
                       "• [D-7 ~ D-3 기대감 급등 시]: 실적 발표 전 주가가 단기 급등하면 '재료 소멸(Sell the Fact)'에 대비해 20~30% 선제적 분할 익절 권고.\n" +
                       "• [D-3 ~ D-Day 불확실성 구간]: 20일 지지선 이탈 우려 종목은 비중 축소하여 장 마감 후 '어닝 쇼크 갭하락' 리스크 원천 회피.\n" +
                       "• [실적 발표 당일]: 호실적이라도 외인·기관 대량 차익 매물 출회 여부 및 지지선 안착을 확인한 후 차분히 대응.",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(170, 210, 245),
                Dock = DockStyle.Fill
            };
            pnlGuide.Controls.Add(lblGuide);

            // 4. 중앙 탭 컨트롤 (내 보유 종목 vs 주요 대형주)
            _tabMain = new TabControl
            {
                Dock = DockStyle.Fill,
                Padding = new Point(16, 6),
                Font = new Font("Malgun Gothic", 9.2f, FontStyle.Bold)
            };

            var tabHeld = new TabPage("내 보유 종목 실적 D-Day")
            {
                BackColor = Color.FromArgb(16, 20, 28)
            };
            _lvHeld = CreateScheduleListView();
            tabHeld.Controls.Add(_lvHeld);

            var tabBellwether = new TabPage("국내 주요 대형주 어닝 캘린더")
            {
                BackColor = Color.FromArgb(16, 20, 28)
            };
            _lvBellwether = CreateScheduleListView();
            tabBellwether.Controls.Add(_lvBellwether);

            _tabMain.TabPages.Add(tabHeld);
            _tabMain.TabPages.Add(tabBellwether);

            pnlMain.Controls.Add(_tabMain);
            pnlMain.Controls.Add(pnlGuide);
            pnlMain.Controls.Add(pnlFooter);
            pnlMain.Controls.Add(pnlHeader);

            Controls.Add(pnlMain);
        }

        private ListView CreateScheduleListView()
        {
            var lv = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BackColor = Color.FromArgb(14, 18, 26),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f),
                BorderStyle = BorderStyle.None
            };

            lv.Columns.Add("D-Day", 95, HorizontalAlignment.Center);
            lv.Columns.Add("종목명", 125);
            lv.Columns.Add("코드", 65, HorizontalAlignment.Center);
            lv.Columns.Add("발표일", 90, HorizontalAlignment.Center);
            lv.Columns.Add("실적 구분", 125);
            lv.Columns.Add("컨센서스(영업익/전망)", 175);
            lv.Columns.Add("AI 참모 실전 대응 지침", 280);

            // 더블 버퍼링
            typeof(ListView).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
                null, lv, new object[] { true });

            return lv;
        }

        private async Task LoadDataAsync()
        {
            _btnRefresh.Enabled = false;
            _btnRefresh.Text = "조회 중...";

            try
            {
                _allSchedules = await EarningsRadarService.GetEarningsScheduleAsync(_portfolioItems);
                RenderSchedules();
            }
            catch (Exception ex)
            {
                _lblSummaryAlert.Text = $"[오류] 실적 캘린더 수집 실패: {ex.Message}";
                _lblSummaryAlert.ForeColor = Color.FromArgb(255, 100, 100);
            }
            finally
            {
                _btnRefresh.Text = "🔄 새로고침";
                _btnRefresh.Enabled = true;
            }
        }

        private void RenderSchedules()
        {
            _lvHeld.BeginUpdate();
            _lvBellwether.BeginUpdate();
            _lvHeld.Items.Clear();
            _lvBellwether.Items.Clear();

            var heldList = _allSchedules.Where(x => x.IsHeld).OrderBy(x => x.DaysRemaining < 0 ? 1 : 0).ThenBy(x => Math.Abs(x.DaysRemaining)).ToList();
            var bellwetherList = _allSchedules.Where(x => x.IsImportantBellwether).OrderBy(x => x.DaysRemaining < 0 ? 1 : 0).ThenBy(x => Math.Abs(x.DaysRemaining)).ToList();

            int urgentHeldCount = heldList.Count(x => x.IsWithin7Days);
            int todayHeldCount = heldList.Count(x => x.DaysRemaining == 0);

            // 상단 배너 텍스트 갱신
            if (urgentHeldCount > 0)
            {
                _lblSummaryAlert.Text = $"🚨 [어닝 경보] 내 보유 종목 중 D-7일 이내 실적 발표 임박 종목: {urgentHeldCount}건! (오늘 발표: {todayHeldCount}건)  대응 지침을 확인하세요.";
                _lblSummaryAlert.ForeColor = Color.FromArgb(255, 175, 60);
            }
            else
            {
                _lblSummaryAlert.Text = $"✔ 내 보유 종목 중 7일 이내 임박한 실적 발표는 없습니다. (총 {heldList.Count}개 보유 종목 모니터링 중)";
                _lblSummaryAlert.ForeColor = Color.FromArgb(100, 240, 160);
            }

            _tabMain.TabPages[0].Text = $"내 보유 종목 실적 D-Day ({heldList.Count})";
            _tabMain.TabPages[1].Text = $"국내 주요 대형주 어닝 캘린더 ({bellwetherList.Count})";

            PopulateListItems(_lvHeld, heldList);
            PopulateListItems(_lvBellwether, bellwetherList);

            _lvHeld.EndUpdate();
            _lvBellwether.EndUpdate();
        }

        private void PopulateListItems(ListView lv, List<EarningsScheduleItem> list)
        {
            foreach (var item in list)
            {
                var lvi = new ListViewItem(new[]
                {
                    item.DDayBadge,
                    item.StockName,
                    item.StockCode,
                    item.EarningsDate.ToString("yyyy-MM-dd"),
                    item.PeriodTitle,
                    item.EstimatedConsensus,
                    item.ActionAdvice
                });

                lvi.Tag = item;

                int d = item.DaysRemaining;
                if (d == 0)
                {
                    lvi.BackColor = Color.FromArgb(52, 20, 28);
                    lvi.SubItems[0].ForeColor = Color.FromArgb(255, 80, 80);
                    lvi.SubItems[0].Font = new Font("Segoe UI", 9.2f, FontStyle.Bold);
                }
                else if (d >= 1 && d <= 3)
                {
                    lvi.BackColor = Color.FromArgb(42, 28, 16);
                    lvi.SubItems[0].ForeColor = Color.FromArgb(255, 160, 50);
                    lvi.SubItems[0].Font = new Font("Segoe UI", 9.2f, FontStyle.Bold);
                }
                else if (d >= 4 && d <= 7)
                {
                    lvi.SubItems[0].ForeColor = Color.FromArgb(255, 215, 64);
                    lvi.SubItems[0].Font = new Font("Segoe UI", 9.2f, FontStyle.Bold);
                }
                else if (d < 0)
                {
                    lvi.SubItems[0].ForeColor = Color.FromArgb(150, 165, 185);
                }
                else
                {
                    lvi.SubItems[0].ForeColor = Color.FromArgb(100, 215, 245);
                }

                lvi.SubItems[1].ForeColor = Color.FromArgb(235, 240, 255);
                lvi.SubItems[2].ForeColor = Color.FromArgb(145, 165, 195);
                lvi.SubItems[3].ForeColor = Color.FromArgb(180, 200, 220);
                lvi.SubItems[4].ForeColor = Color.FromArgb(200, 220, 240);
                lvi.SubItems[5].ForeColor = Color.FromArgb(255, 220, 140);
                lvi.SubItems[6].ForeColor = Color.FromArgb(220, 245, 255);

                lvi.UseItemStyleForSubItems = false;
                lv.Items.Add(lvi);
            }
        }
    }
}
