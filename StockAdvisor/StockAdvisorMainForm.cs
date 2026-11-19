using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace GateHelper.StockAdvisor.Standalone
{
    public class StockAdvisorMainForm : Form
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        private const int SW_RESTORE = 9;

        private StockAdvisorControl _stockAdvisorControl;
        private NotifyIcon _trayIcon;
        private ContextMenuStrip _trayMenu;
        private EventWaitHandle _wakeEvent;
        private RegisteredWaitHandle _registeredWakeHandle;
        private FormWindowState _previousNonMinimizedState = FormWindowState.Normal;
        private bool _hasShownTrayNotice = false;
        private bool _isExplicitExit = false;

        public StockAdvisorMainForm()
        {
            InitializeWindow();
            InitializeBody();
            InitializeSystemTray();
            StartSingleInstanceWakeListener();
        }

        private void InitializeWindow()
        {
            this.Text = "StockAdvisor — AI 주식 리스크 참모 (독립 실행 모드)";
            this.Size = new Size(1280, 820);
            this.MinimumSize = new Size(1080, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(14, 18, 26);
            this.ForeColor = Color.White;
            this.DoubleBuffered = true;

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
        }

        private void InitializeBody()
        {
            // 상단 슬림 독립 실행 바 (트레이 최소화 버튼 포함)
            var topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 32,
                BackColor = Color.FromArgb(18, 24, 36),
                Padding = new Padding(10, 0, 10, 0)
            };

            var lblStatus = new Label
            {
                Text = "🟢 독립 실행 프로세스 (StockAdvisor.exe) — SandBox 개발/빌드와 무관하게 상시 가동",
                AutoSize = true,
                ForeColor = Color.FromArgb(130, 220, 175),
                Font = new Font("Malgun Gothic", 8.5f, FontStyle.Bold),
                Location = new Point(10, 8)
            };

            var btnTray = new Button
            {
                Text = "📥 트레이 최소화",
                Dock = DockStyle.Right,
                Width = 118,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(28, 42, 66),
                ForeColor = Color.FromArgb(190, 225, 255),
                Font = new Font("Malgun Gothic", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TabStop = false
            };
            btnTray.FlatAppearance.BorderColor = Color.FromArgb(55, 95, 145);
            btnTray.FlatAppearance.BorderSize = 1;
            btnTray.Click += (s, e) => MinimizeToTray();

            topBar.Controls.Add(lblStatus);
            topBar.Controls.Add(btnTray);

            _stockAdvisorControl = new StockAdvisorControl
            {
                Dock = DockStyle.Fill,
                RequestHostActivate = ShowMainWindow
            };

            this.Controls.Add(_stockAdvisorControl);
            this.Controls.Add(topBar);
        }

        private void InitializeSystemTray()
        {
            try
            {
                _trayMenu = new ContextMenuStrip();

                var mnuOpen = new ToolStripMenuItem("★ StockAdvisor 열기");
                mnuOpen.Font = new Font("Malgun Gothic", 9f, FontStyle.Bold);
                mnuOpen.Click += (s, e) => ShowMainWindow();

                var mnuAlertHistory = new ToolStripMenuItem("가격 도달 알림 기록");
                mnuAlertHistory.Click += (s, e) =>
                {
                    ShowMainWindow();
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

                var mnuExit = new ToolStripMenuItem("StockAdvisor 종료");
                mnuExit.Click += (s, e) =>
                {
                    _isExplicitExit = true;
                    if (_trayIcon != null) _trayIcon.Visible = false;
                    Application.Exit();
                };

                _trayMenu.Items.Add(mnuOpen);
                _trayMenu.Items.Add(mnuAlertHistory);
                _trayMenu.Items.Add(new ToolStripSeparator());
                _trayMenu.Items.Add(mnuRefresh);
                _trayMenu.Items.Add(new ToolStripSeparator());
                _trayMenu.Items.Add(mnuExit);

                _trayIcon = new NotifyIcon
                {
                    Text = "StockAdvisor - AI 주식 리스크 참모 (독립 상시 감시 중)",
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

                _trayIcon.DoubleClick += (s, e) => ShowMainWindow();
                _trayIcon.BalloonTipClicked += (s, e) => ShowMainWindow();

                StockToastNotification.AlertLogged += OnStockAlertLogged;
            }
            catch { }
        }

        private void OnStockAlertLogged(Models.StockAlertLogItem item)
        {
            if (item == null || _trayIcon == null) return;
            try
            {
                if (this.WindowState == FormWindowState.Minimized || !this.Visible)
                {
                    string title = item.Message;
                    string body = $"현재가: {item.CurrentPrice:N0}원 ({item.ChangeRate:+0.00;-0.00}%) · 클릭 시 이동";
                    _trayIcon.ShowBalloonTip(3000, title, body, ToolTipIcon.Info);
                }
            }
            catch { }
        }

        private void StartSingleInstanceWakeListener()
        {
            try
            {
                _wakeEvent = new EventWaitHandle(false, EventResetMode.AutoReset, StockAdvisorProgram.SingleInstanceWakeEventName);
                _registeredWakeHandle = ThreadPool.RegisterWaitForSingleObject(
                    _wakeEvent,
                    (state, timedOut) =>
                    {
                        if (this.IsDisposed) return;
                        try
                        {
                            this.BeginInvoke(new Action(() => ShowMainWindow()));
                        }
                        catch { }
                    },
                    null,
                    -1,
                    false
                );
            }
            catch { }
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
                        "StockAdvisor가 시스템 트레이로 이동합니다.\n\n• 실시간 시세 감시·가격 도달 알림·텔레그램 양방향 봇은 백그라운드에서 계속 정상 작동합니다.\n• SandBox(9번 모듈 등)를 새로 빌드하거나 종료해도 StockAdvisor는 영향을 받지 않습니다.\n• 트레이 아이콘을 더블클릭하면 언제든 다시 열 수 있습니다.",
                        "StockAdvisor 트레이 백그라운드 감시",
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
                    _trayIcon.ShowBalloonTip(2000, "StockAdvisor 트레이 가동", "독립 프로세스로 백그라운드 초절전 감시 중입니다.", ToolTipIcon.Info);
                }
            }
            catch { }
        }

        public void ShowMainWindow()
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

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _stockAdvisorControl?.OnTabVisibilityChanged(true);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (this.WindowState == FormWindowState.Minimized)
            {
                _stockAdvisorControl?.OnTabVisibilityChanged(false);
            }
            else if (this.Visible)
            {
                _previousNonMinimizedState = this.WindowState;
                _stockAdvisorControl?.OnTabVisibilityChanged(true);
            }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            bool isVis = this.Visible && this.WindowState != FormWindowState.Minimized;
            _stockAdvisorControl?.OnTabVisibilityChanged(isVis);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_isExplicitExit && e.CloseReason == CloseReason.UserClosing)
            {
                // 창 X 버튼 클릭 시 즉시 종료
            }

            base.OnFormClosing(e);
            StockToastNotification.AlertLogged -= OnStockAlertLogged;

            try
            {
                _registeredWakeHandle?.Unregister(null);
                _wakeEvent?.Dispose();
                _wakeEvent = null;
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
        }
    }
}
