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
                string iconPath = System.IO.Path.Combine(Application.StartupPath, "Resource_TEMP", "sandbox_icon.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    this.Icon = new Icon(iconPath);
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
                if (!string.IsNullOrEmpty(initialGame))
                {
                    SwitchToGame(initialGame);
                }
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