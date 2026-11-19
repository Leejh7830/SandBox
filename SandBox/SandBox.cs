using MaterialSkin.Controls;
using System;
using System.Drawing;
using System.Windows.Forms;
using GateHelper.MysteryTime;
using GateHelper.AiTrend;
using GateHelper.ServerTycoon;
using GateHelper.DataFlip;
using GateHelper.PocketFarm;
using GateHelper.EcoSim;

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
        private EcoSimControl _ecoSimControl;

        private NotifyIcon _trayIcon;
        private ContextMenuStrip _trayMenu;
        private Button _btnMinimizeToTray;
        private FormWindowState _previousNonMinimizedState = FormWindowState.Normal;
        private bool _hasShownTrayNotice = false;

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
                _trayMenu.Items.Add(new ToolStripSeparator());
                _trayMenu.Items.Add(mnuExit);

                _trayIcon = new NotifyIcon
                {
                    Text = "SandBox (초절전 대기 중)",
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
                _trayIcon.BalloonTipClicked += (s, e) => ShowSandBoxWindow();
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
                if (!_hasShownTrayNotice)
                {
                    _hasShownTrayNotice = true;
                    MessageBox.Show(
                        this,
                        "SandBox가 시스템 트레이로 이동합니다.\n\n• 화면 렌더링 및 게임 루프는 초절전 모드(CPU 0.0%)로 전환됩니다.\n• 보유/관심 종목의 시세 감시 및 적정가 도달 토스트 알림은 백그라운드에서 계속 정상 작동합니다.\n• 작업 표시줄 우측 하단 트레이 아이콘을 더블클릭하면 언제든 다시 열 수 있습니다.\n\n(이 안내는 프로그램 실행 후 최초 1회만 표시됩니다)",
                        "시스템 트레이 백그라운드 감시 모드",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }

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
                    ShowWindow(this.Handle, SW_RESTORE);
                    this.WindowState = _previousNonMinimizedState;
                }
                this.ShowInTaskbar = true;
                if (_trayIcon != null) _trayIcon.Visible = false;
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
                _ecoSimControl?.OnTabVisibilityChanged(false);
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

            // 9. EcoSim (미니 생태계 진화 시뮬레이터) 컨트롤 추가
            _ecoSimControl = new EcoSimControl();
            _ecoSimControl.Dock = DockStyle.Fill;
            if (SB_tabControl1.TabPages.Contains(tpEcoSim))
            {
                tpEcoSim.Controls.Add(_ecoSimControl);
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

            bool isEcoSim = SB_tabControl1.SelectedTab == tpEcoSim;
            _ecoSimControl?.OnTabVisibilityChanged(isEcoSim);
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
                _ecoSimControl?.OnTabVisibilityChanged(false);
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
                _trayMenu?.Dispose();
                _trayMenu = null;
            }
            catch { }

            _bitFlipGame?.OnTabVisibilityChanged(false);
            _signalLinkGame?.OnTabVisibilityChanged(false);
            _serverTycoonControl?.OnTabVisibilityChanged(false);
            _dataFlipControl?.OnTabVisibilityChanged(false);
            _pocketFarmControl?.OnTabVisibilityChanged(false);
            _ecoSimControl?.OnTabVisibilityChanged(false);
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
            else if (gameName == "EcoSim")
            {
                if (SB_tabControl1.TabPages.Contains(tpEcoSim))
                {
                    if (this.WindowState == FormWindowState.Normal && (this.ClientSize.Width < 1260 || this.ClientSize.Height < 800))
                    {
                        this.ClientSize = new System.Drawing.Size(1280, 820);
                    }
                    SB_tabControl1.SelectedTab = tpEcoSim;
                    _ecoSimControl?.OnTabVisibilityChanged(true);
                }
            }
        }
    }
}