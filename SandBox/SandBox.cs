using MaterialSkin.Controls;
using System;
using System.Drawing;
using System.Windows.Forms;
using GateHelper.MysteryTime;
using GateHelper.AiTrend;
using GateHelper.ServerTycoon;
using GateHelper.DataFlip;
using GateHelper.PocketFarm;
using GateHelper.StockAdvisor;

namespace GateHelper
{
    public partial class SandBox : MaterialForm
    {
        private GameListControl _gameList;
        private BitFlipControl _bitFlipGame;
        private SignalLinkControl _signalLinkGame;
        private MysteryTimeControl _mysteryTimeGame;
        private AiTrendControl _aiTrendControl;
        private ServerTycoonControl _serverTycoonControl;
        private DataFlipControl _dataFlipControl;
        private PocketFarmControl _pocketFarmControl;
        private StockAdvisorControl _stockAdvisorControl;

        private NotifyIcon _trayIcon;
        private ContextMenuStrip _trayMenu;
        private Button _btnMinimizeToTray;
        private FormWindowState _previousNonMinimizedState = FormWindowState.Normal;

        private System.Threading.EventWaitHandle _wakeEvent;
        private System.Threading.RegisteredWaitHandle _registeredWakeHandle;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        private const int SW_RESTORE = 9;

        public SandBox() : this(null)
        {
        }

        public SandBox(string initialGame)
        {
            InitializeComponent();

            var skinManager = MaterialSkin.MaterialSkinManager.Instance;
            skinManager.AddFormToManage(this);
            skinManager.Theme = MaterialSkin.MaterialSkinManager.Themes.DARK;

            // SandBox 전용 크리스탈 큐브 아이콘 적용
            try
            {
                var resIcon = GateHelper.Properties.Resources.sandbox_icon;
                if (resIcon != null)
                {
                    this.Icon = (Icon)resIcon.Clone();
                }
                else
                {
                    this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                }
            }
            catch { }

            if (!DesignMode)
            {
                InitializeCustomControls();
                InitializeSystemTray();
                InitializeTrayButton();
                StartSingleInstanceWakeListener();
                if (!string.IsNullOrEmpty(initialGame))
                {
                    SwitchToGame(initialGame);
                }
            }
        }

        private void InitializeSystemTray()
        {
            try
            {
                _trayMenu = new ContextMenuStrip();

                var mnuOpen = new ToolStripMenuItem("★ SandBox 열기");
                mnuOpen.Font = new Font("Malgun Gothic", 9f, FontStyle.Bold);
                mnuOpen.Click += (s, e) => ShowSandBoxWindow();

                var mnuStock = new ToolStripMenuItem("AI 주식 리스크 참모 열기");
                mnuStock.Click += (s, e) =>
                {
                    ShowSandBoxWindow();
                    SwitchToGame("StockAdvisor");
                };

                var mnuAlertHistory = new ToolStripMenuItem("가격 도달 알림 기록");
                mnuAlertHistory.Click += (s, e) =>
                {
                    ShowSandBoxWindow();
                    SwitchToGame("StockAdvisor");
                    _stockAdvisorControl?.ShowAlertHistoryDialog();
                };

                var mnuRefresh = new ToolStripMenuItem("실시간 시세 즉시 갱신");
                mnuRefresh.Click += async (s, e) =>
                {
                    if (_stockAdvisorControl != null)
                    {
                        await _stockAdvisorControl.RefreshPricesAsync();
                    }
                };

                var mnuExit = new ToolStripMenuItem("프로그램 종료");
                mnuExit.Click += (s, e) =>
                {
                    if (_trayIcon != null)
                    {
                        _trayIcon.Visible = false;
                    }
                    Application.Exit();
                };

                _trayMenu.Items.Add(mnuOpen);
                _trayMenu.Items.Add(mnuStock);
                _trayMenu.Items.Add(mnuAlertHistory);
                _trayMenu.Items.Add(new ToolStripSeparator());
                _trayMenu.Items.Add(mnuRefresh);
                _trayMenu.Items.Add(new ToolStripSeparator());
                _trayMenu.Items.Add(mnuExit);

                _trayIcon = new NotifyIcon
                {
                    Text = "SandBox - 주식 AI 리스크 참모 (초절전 가동 중)",
                    ContextMenuStrip = _trayMenu,
                    Visible = false
                };

                try
                {
                    var resIcon = GateHelper.Properties.Resources.sandbox_icon;
                    _trayIcon.Icon = resIcon != null ? (Icon)resIcon.Clone() : this.Icon;
                }
                catch
                {
                    _trayIcon.Icon = this.Icon;
                }

                _trayIcon.DoubleClick += (s, e) => ShowSandBoxWindow();
                _trayIcon.BalloonTipClicked += (s, e) =>
                {
                    ShowSandBoxWindow();
                    SwitchToGame("StockAdvisor");
                };

                // 알림 발생 시 트레이 풍선 알림 연동
                GateHelper.StockAdvisor.StockToastNotification.AlertLogged += (item) =>
                {
                    if (item == null || _trayIcon == null) return;
                    if (this.WindowState == FormWindowState.Minimized || !this.Visible)
                    {
                        string title = item.Message;
                        string body = $"현재가: {item.CurrentPrice:N0}원 ({item.ChangeRate:+0.00;-0.00}%) · 클릭 시 이동";
                        _trayIcon.ShowBalloonTip(3000, title, body, ToolTipIcon.Info);
                    }
                };
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "SandBox.InitializeSystemTray");
            }
        }

        private void StartSingleInstanceWakeListener()
        {
            try
            {
                _wakeEvent = new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.AutoReset, Program.SingleInstanceWakeEventName);
                _registeredWakeHandle = System.Threading.ThreadPool.RegisterWaitForSingleObject(
                    _wakeEvent,
                    (state, timedOut) =>
                    {
                        if (this.IsDisposed) return;
                        try
                        {
                            this.BeginInvoke(new Action(() => ShowSandBoxWindow()));
                        }
                        catch { }
                    },
                    null,
                    -1,
                    false
                );
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "SandBox.StartSingleInstanceWakeListener");
            }
        }

        private void InitializeTrayButton()
        {
            try
            {
                var skinManager = MaterialSkin.MaterialSkinManager.Instance;
                Color statusBg = skinManager?.ColorScheme?.DarkPrimaryColor ?? Color.FromArgb(48, 63, 159);

                _btnMinimizeToTray = new Button
                {
                    Size = new Size(26, 24),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = statusBg,
                    ForeColor = Color.FromArgb(220, 235, 255),
                    Font = new Font("Segoe UI Emoji", 8.5f),
                    Text = "📥",
                    Cursor = Cursors.Hand,
                    Margin = Padding.Empty,
                    Padding = Padding.Empty,
                    TabStop = false
                };
                _btnMinimizeToTray.FlatAppearance.BorderSize = 0;
                _btnMinimizeToTray.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 160, 230);
                _btnMinimizeToTray.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 120, 180);
                _btnMinimizeToTray.Click += (s, e) => MinimizeToTray();

                var tt = new ToolTip();
                tt.SetToolTip(_btnMinimizeToTray, "시스템 트레이로 최소화 (작업표시줄에서 숨기고 백그라운드 초절전 감시)");

                this.Controls.Add(_btnMinimizeToTray);
                _btnMinimizeToTray.BringToFront();
                UpdateTrayButtonPosition();
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "SandBox.InitializeTrayButton");
            }
        }

        private void UpdateTrayButtonPosition()
        {
            if (_btnMinimizeToTray != null && !_btnMinimizeToTray.IsDisposed)
            {
                int btnWidth = 26;
                int btnHeight = 24;
                int x = this.Width - 72 - btnWidth;
                _btnMinimizeToTray.Size = new Size(btnWidth, btnHeight);
                _btnMinimizeToTray.Location = new Point(x, 0);
                _btnMinimizeToTray.BringToFront();
            }
        }

        public void MinimizeToTray()
        {
            try
            {
                if (this.WindowState != FormWindowState.Minimized)
                {
                    _previousNonMinimizedState = this.WindowState;
                }
                this.WindowState = FormWindowState.Minimized;
                this.Hide();
                if (_trayIcon != null)
                {
                    _trayIcon.Visible = true;
                    _trayIcon.ShowBalloonTip(2000, "SandBox 시스템 트레이 가동", "작업표시줄에서 숨겨지고 시스템 트레이에서 초절전 모드(0.0%)로 가동 중입니다.", ToolTipIcon.Info);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "SandBox.MinimizeToTray");
            }
        }

        public void ShowSandBoxWindow()
        {
            try
            {
                if (!this.Visible) this.Show();
                if (this.WindowState == FormWindowState.Minimized)
                {
                    this.WindowState = _previousNonMinimizedState;
                }
                this.ShowInTaskbar = true;
                ShowWindow(this.Handle, SW_RESTORE);
                this.BringToFront();
                this.Activate();
                SetForegroundWindow(this.Handle);
            }
            catch { }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateTrayButtonPosition();

            if (this.WindowState != FormWindowState.Minimized)
            {
                _previousNonMinimizedState = this.WindowState;
                SB_tabControl1_SelectedIndexChanged(this, EventArgs.Empty);
            }
            else
            {
                // 표준 최소화 버튼(_) 클릭 시: 작업표시줄로만 이동 (this.Hide() 안 함)
                // 작업표시줄 최소화 상태에서도 백그라운드 초절전 절전 모드(CPU 0.0%) 적용
                _bitFlipGame?.OnTabVisibilityChanged(false);
                _signalLinkGame?.OnTabVisibilityChanged(false);
                _serverTycoonControl?.OnTabVisibilityChanged(false);
                _dataFlipControl?.OnTabVisibilityChanged(false);
                _pocketFarmControl?.OnTabVisibilityChanged(false);
                _stockAdvisorControl?.OnTabVisibilityChanged(false);
            }
        }

        private void InitializeCustomControls()
        {
            // 1. 게임 리스트 추가
            _gameList = new GameListControl();
            _gameList.Dock = DockStyle.Fill;

            if (SB_tabControl1.TabPages.Contains(tpList))
            {
                tpList.Controls.Add(_gameList);
            }

            // 2. Bit Flip 컨트롤 추가
            _bitFlipGame = new BitFlipControl();
            _bitFlipGame.Dock = DockStyle.Fill;
            if (SB_tabControl1.TabPages.Contains(tpBitFlip))
            {
                tpBitFlip.Controls.Add(_bitFlipGame);
            }

            // 3. Signal Link 컨트롤 추가
            _signalLinkGame = new SignalLinkControl();
            _signalLinkGame.Dock = DockStyle.Fill;
            if (SB_tabControl1.TabPages.Contains(tpSignalLink))
            {
                tpSignalLink.Controls.Add(_signalLinkGame);
            }

            // 4. Mystery Time 컨트롤 추가
            _mysteryTimeGame = new MysteryTimeControl();
            _mysteryTimeGame.Dock = DockStyle.Fill;
            if (SB_tabControl1.TabPages.Contains(tpMysteryTime))
            {
                tpMysteryTime.Controls.Add(_mysteryTimeGame);
            }

            // 5. AI Trend 컨트롤 추가
            _aiTrendControl = new AiTrendControl();
            _aiTrendControl.Dock = DockStyle.Fill;
            if (SB_tabControl1.TabPages.Contains(tpAiTrend))
            {
                tpAiTrend.Controls.Add(_aiTrendControl);
            }

            // 6. Server Tycoon 컨트롤 추가
            _serverTycoonControl = new ServerTycoonControl();
            _serverTycoonControl.Dock = DockStyle.Fill;
            if (SB_tabControl1.TabPages.Contains(tpServerTycoon))
            {
                tpServerTycoon.Controls.Add(_serverTycoonControl);
            }

            // 7. Data Flip 컨트롤 추가
            _dataFlipControl = new DataFlipControl();
            _dataFlipControl.Dock = DockStyle.Fill;
            if (SB_tabControl1.TabPages.Contains(tpDataFlip))
            {
                tpDataFlip.Controls.Add(_dataFlipControl);
            }

            // 8. Pocket Farm 컨트롤 추가
            _pocketFarmControl = new PocketFarmControl();
            _pocketFarmControl.Dock = DockStyle.Fill;
            if (SB_tabControl1.TabPages.Contains(tpPocketFarm))
            {
                tpPocketFarm.Controls.Add(_pocketFarmControl);
            }

            // 9. Stock Advisor 컨트롤 추가
            _stockAdvisorControl = new StockAdvisorControl();
            _stockAdvisorControl.Dock = DockStyle.Fill;
            if (SB_tabControl1.TabPages.Contains(tpStockAdvisor))
            {
                tpStockAdvisor.Controls.Add(_stockAdvisorControl);
            }

            // 탭 전환 이벤트: 활성 탭에 따른 스마트 렌더링/절전 제어
            SB_tabControl1.SelectedIndexChanged += SB_tabControl1_SelectedIndexChanged;

            // 초기 탭 (리스트 화면)
            SB_tabControl1.SelectedTab = tpList;
        }

        private void SB_tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool isBitFlip = SB_tabControl1.SelectedTab == tpBitFlip;
            _bitFlipGame?.OnTabVisibilityChanged(isBitFlip);

            bool isSignalLink = SB_tabControl1.SelectedTab == tpSignalLink;
            _signalLinkGame?.OnTabVisibilityChanged(isSignalLink);

            bool isTycoon = SB_tabControl1.SelectedTab == tpServerTycoon;
            _serverTycoonControl?.OnTabVisibilityChanged(isTycoon);

            bool isDataFlip = SB_tabControl1.SelectedTab == tpDataFlip;
            _dataFlipControl?.OnTabVisibilityChanged(isDataFlip);

            bool isPocketFarm = SB_tabControl1.SelectedTab == tpPocketFarm;
            _pocketFarmControl?.OnTabVisibilityChanged(isPocketFarm);

            bool isStockAdvisor = SB_tabControl1.SelectedTab == tpStockAdvisor;
            _stockAdvisorControl?.OnTabVisibilityChanged(isStockAdvisor);
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            bool isVis = this.Visible && this.WindowState != FormWindowState.Minimized;
            if (!isVis)
            {
                _bitFlipGame?.OnTabVisibilityChanged(false);
                _signalLinkGame?.OnTabVisibilityChanged(false);
                _serverTycoonControl?.OnTabVisibilityChanged(false);
                _dataFlipControl?.OnTabVisibilityChanged(false);
                _pocketFarmControl?.OnTabVisibilityChanged(false);
                _stockAdvisorControl?.OnTabVisibilityChanged(false);
            }
            else
            {
                SB_tabControl1_SelectedIndexChanged(this, EventArgs.Empty);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            try
            {
                _registeredWakeHandle?.Unregister(null);
                _wakeEvent?.Dispose();
                _wakeEvent = null;
            }
            catch { }

            try
            {
                if (_btnMinimizeToTray != null)
                {
                    _btnMinimizeToTray.Dispose();
                    _btnMinimizeToTray = null;
                }
            }
            catch { }

            try
            {
                if (_trayIcon != null)
                {
                    _trayIcon.Visible = false;
                    _trayIcon.Dispose();
                    _trayIcon = null;
                }
            }
            catch { }

            _bitFlipGame?.OnTabVisibilityChanged(false);
            _signalLinkGame?.OnTabVisibilityChanged(false);
            _serverTycoonControl?.OnTabVisibilityChanged(false);
            _dataFlipControl?.OnTabVisibilityChanged(false);
            _pocketFarmControl?.OnTabVisibilityChanged(false);
            _stockAdvisorControl?.OnTabVisibilityChanged(false);
        }

        public void BackToList()
        {
            SB_tabControl1.SelectedTab = tpList;
            ResetToDefaultSize();
        }

        public void ShowGameList()
        {
            SB_tabControl1.SelectedTab = tpList;
            ResetToDefaultSize();
        }

        public void ResetToDefaultSize()
        {
            if (this.ClientSize.Width != 650 || this.ClientSize.Height != 700)
            {
                Point center = new Point(this.Left + this.Width / 2, this.Top + this.Height / 2);
                this.ClientSize = new System.Drawing.Size(650, 700);
                this.Left = Math.Max(0, center.X - this.Width / 2);
                this.Top = Math.Max(0, center.Y - this.Height / 2);
            }
        }

        public void SwitchToGame(string gameName)
        {
            if (gameName == "BitFlip")
            {
                _bitFlipGame?.ShowMapSelection();
                SB_tabControl1.SelectedTab = tpBitFlip;
            }
            else if (gameName == "SignalLink")
            {
                _signalLinkGame?.ShowMapSelection();
                SB_tabControl1.SelectedTab = tpSignalLink; 
            }
            else if (gameName == "MysteryTime")
            {
                if (SB_tabControl1.TabPages.Contains(tpMysteryTime))
                {
                    SB_tabControl1.SelectedTab = tpMysteryTime;
                }
            }
            else if (gameName == "AiTrend")
            {
                if (SB_tabControl1.TabPages.Contains(tpAiTrend))
                {
                    _aiTrendControl?.ApplyTheme(MaterialSkin.MaterialSkinManager.Instance.Theme == MaterialSkin.MaterialSkinManager.Themes.DARK);
                    SB_tabControl1.SelectedTab = tpAiTrend;
                }
            }
            else if (gameName == "ServerTycoon")
            {
                if (SB_tabControl1.TabPages.Contains(tpServerTycoon))
                {
                    SB_tabControl1.SelectedTab = tpServerTycoon;
                }
            }
            else if (gameName == "DataFlip")
            {
                if (SB_tabControl1.TabPages.Contains(tpDataFlip))
                {
                    SB_tabControl1.SelectedTab = tpDataFlip;
                    _dataFlipControl?.PromptMapSelection();
                }
            }
            else if (gameName == "PocketFarm")
            {
                if (SB_tabControl1.TabPages.Contains(tpPocketFarm))
                {
                    SB_tabControl1.SelectedTab = tpPocketFarm;
                    _pocketFarmControl?.OnTabVisibilityChanged(true);
                }
            }
            else if (gameName == "StockAdvisor")
            {
                if (SB_tabControl1.TabPages.Contains(tpStockAdvisor))
                {
                    if (this.ClientSize.Width < 1000 || this.ClientSize.Height < 700)
                    {
                        this.ClientSize = new System.Drawing.Size(1000, 700);
                    }
                    SB_tabControl1.SelectedTab = tpStockAdvisor;
                    _stockAdvisorControl?.OnTabVisibilityChanged(true);
                }
            }
        }
    }
}