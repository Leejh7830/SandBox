using System;
using System.Drawing;
using System.Windows.Forms;

namespace GateHelper.PocketFarm
{
    public partial class PocketFarmControl : UserControl
    {
        private PocketFarmModel _model;
        private Timer _simTimer;
        private DateTime _lastTick;
        private FarmTabType _currentTab = FarmTabType.Farm;
        private ModalViewType _activeModal = ModalViewType.None;

        // 마우스 호버 좌표
        private Point _hoverPoint = Point.Empty;

        // 탭 버튼 영역 캐시
        private Rectangle _rcTabFarm;
        private Rectangle _rcTabFishing;
        private Rectangle _rcTabLivestock;
        private Rectangle _rcTabRestaurant;
        private Rectangle _rcBtnBack;
        private Rectangle _rcBtnAdmin;
        private Rectangle _rcBtnReset;
        private Rectangle _rcBtnAchievements;
        private Rectangle _rcBtnContracts;

        // 하단 액션 버튼 영역 캐시
        private Rectangle _rcBtnDispatch;
        private Rectangle _rcBtnAutoToggle;
        private Rectangle _rcBtnSellAll;
        private Rectangle _rcBtnUpgrade;
        private Rectangle _rcBtnTech;
        private Rectangle _rcBtnBreeding;
        private Rectangle _rcBtnRecipes;

        public PocketFarmControl()
        {
            InitializeComponent();

            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            _model = PocketFarmModel.LoadOrCreate();
            InitializeGdiResources();

            _simTimer = new Timer();
            _simTimer.Interval = 50; // 20 FPS 초경량 렌더링
            _simTimer.Tick += SimTimer_Tick;

            _lastTick = DateTime.Now;
            _simTimer.Start();
        }

        private void SimTimer_Tick(object sender, EventArgs e)
        {
            if (!this.Visible || this.ParentForm == null || this.ParentForm.WindowState == FormWindowState.Minimized)
            {
                return;
            }

            DateTime now = DateTime.Now;
            float dt = (float)(now - _lastTick).TotalSeconds;
            _lastTick = now;

            // 지나치게 큰 dt 보정 (예: 절전 모드 복귀)
            if (dt > 0.5f) dt = 0.5f;

            _model.Tick(dt);
            _model.LastSaveTime = now;
            this.Invalidate();
        }

        public void OnTabVisibilityChanged(bool isVisible)
        {
            if (isVisible)
            {
                // 복귀 시 오프라인 시간 보정
                TimeSpan elapsed = DateTime.Now - _model.LastSaveTime;
                if (elapsed.TotalSeconds > 5.0)
                {
                    _model.CalculateOfflineProgress(elapsed.TotalSeconds);
                }
                _lastTick = DateTime.Now;
                _model.LastSaveTime = DateTime.Now;
                _simTimer.Start();
                this.Invalidate();
            }
            else
            {
                // 백그라운드 전환 시 CPU 0.0% 절전 및 데이터 자동 저장
                _simTimer.Stop();
                _model.LastSaveTime = DateTime.Now;
                _model.Save();
            }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            OnTabVisibilityChanged(this.Visible);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            _hoverPoint = e.Location;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            // 1. 모달이 열려 있는 경우 모달 내부 클릭 우선 처리
            if (_activeModal != ModalViewType.None)
            {
                HandleModalClick(e.Location);
                this.Invalidate();
                return;
            }

            // 2. 상단 헤더 버튼 처리
            if (_rcBtnBack.Contains(e.Location))
            {
                _model.Save();
                var p = this.ParentForm;
                if (p != null)
                {
                    var m = p.GetType().GetMethod("BackToList");
                    if (m != null) m.Invoke(p, null);
                }
                return;
            }

            if (_rcTabFarm.Contains(e.Location)) { _currentTab = FarmTabType.Farm; this.Invalidate(); return; }
            if (_rcTabFishing.Contains(e.Location)) { _currentTab = FarmTabType.Fishing; this.Invalidate(); return; }
            if (_rcTabLivestock.Contains(e.Location)) { _currentTab = FarmTabType.Livestock; this.Invalidate(); return; }
            if (_rcTabRestaurant.Contains(e.Location)) { _currentTab = FarmTabType.Restaurant; this.Invalidate(); return; }

            if (_rcBtnAchievements.Contains(e.Location))
            {
                if (_model.AchievementSystem != null) _model.AchievementSystem.CheckAllMilestones(_model);
                _activeModal = ModalViewType.Achievements;
                this.Invalidate();
                return;
            }
            if (_rcBtnContracts.Contains(e.Location))
            {
                _activeModal = ModalViewType.Contracts;
                this.Invalidate();
                return;
            }

            // 임시/테스트용 관리자 치트 & 초기화 버튼
            if (_rcBtnAdmin.Contains(e.Location))
            {
                _model.ToggleAdminMode();
                this.Invalidate();
                return;
            }
            if (_rcBtnReset.Contains(e.Location))
            {
                if (MessageBox.Show("게임을 초기 상태로 리셋하시겠습니까?\n(모든 시설, 일꾼, 연구가 초기화됩니다)", "게임 초기화", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    _model.ResetAllData();
                    this.Invalidate();
                }
                return;
            }

            // 3. 하단 액션 버튼 처리
            if (_rcBtnDispatch.Contains(e.Location))
            {
                _model.DispatchAllWorkers(_currentTab);
                this.Invalidate();
                return;
            }
            if (_rcBtnAutoToggle.Contains(e.Location))
            {
                if (!_model.IsTechUnlocked("tech_auto_dispatch"))
                {
                    _model.AddFloatingText("[잠김] 기술 연구 [스마트 자율 파견] 개발 필요!", 325, 520, Color.FromArgb(255, 180, 80));
                    return;
                }
                _model.AutoDispatch = !_model.AutoDispatch;
                _model.AddFloatingText(_model.AutoDispatch ? "[자동] 자율 파견 활성화" : "[정지] 자율 파견 중지", 325, 520, Color.FromArgb(120, 255, 180));
                _model.Save();
                this.Invalidate();
                return;
            }
            if (_rcBtnSellAll.Contains(e.Location))
            {
                _model.SellAllProduce();
                this.Invalidate();
                return;
            }
            if (_rcBtnUpgrade.Contains(e.Location))
            {
                _activeModal = ModalViewType.Upgrades;
                this.Invalidate();
                return;
            }
            if (_rcBtnTech.Contains(e.Location))
            {
                _activeModal = ModalViewType.TechTree;
                this.Invalidate();
                return;
            }
            if (_rcBtnBreeding.Contains(e.Location))
            {
                _activeModal = ModalViewType.Breeding;
                this.Invalidate();
                return;
            }
            if (_rcBtnRecipes.Contains(e.Location))
            {
                _activeModal = ModalViewType.Recipes;
                this.Invalidate();
                return;
            }

            // 4. 메인 뷰 내부 인터랙션 (밭 해금, 낚시터 해금, 사료 급여 등)
            HandleMainViewClick(e.Location);
            this.Invalidate();
        }

        private void HandleMainViewClick(Point pt)
        {
            if (_currentTab == FarmTabType.Farm)
            {
                foreach (var plot in _model.Plots)
                {
                    if (plot.Bounds.Contains(pt))
                    {
                        if (!plot.IsUnlocked)
                        {
                            if (_model.Gold >= plot.UnlockCost)
                            {
                                _model.Gold -= plot.UnlockCost;
                                plot.IsUnlocked = true;
                                plot.State = TaskPlotState.Growing;
                                if (_model.AchievementSystem != null) _model.AchievementSystem.CheckAllMilestones(_model);
                                _model.AddFloatingText("[개간] 밭 개간 완료!", pt.X, pt.Y, Color.FromArgb(100, 255, 120));
                                _model.Save();
                            }
                            else
                            {
                                _model.AddFloatingText("골드가 부족합니다!", pt.X, pt.Y, Color.FromArgb(255, 100, 100));
                            }
                        }
                        else
                        {
                            // 개간된 밭 클릭 시: 개량된 작물 순환 전환 (밀 ➔ 황금밀 ➔ 당근 ➔ 감자)
                            _model.CyclePlotCrop(plot.PlotId);
                        }
                        break;
                    }
                }
            }
            else if (_currentTab == FarmTabType.Fishing)
            {
                foreach (var spot in _model.FishingSpots)
                {
                    if (spot.Bounds.Contains(pt))
                    {
                        if (!spot.IsUnlocked)
                        {
                            if (_model.Gold >= spot.UnlockCost)
                            {
                                _model.Gold -= spot.UnlockCost;
                                spot.IsUnlocked = true;
                                spot.State = TaskPlotState.Growing;
                                if (_model.AchievementSystem != null) _model.AchievementSystem.CheckAllMilestones(_model);
                                _model.AddFloatingText("[개방] 낚시 포인트 개방!", pt.X, pt.Y, Color.FromArgb(100, 220, 255));
                                _model.Save();
                            }
                            else
                            {
                                _model.AddFloatingText("골드가 부족합니다!", pt.X, pt.Y, Color.FromArgb(255, 100, 100));
                            }
                        }
                        break;
                    }
                }
            }
            else if (_currentTab == FarmTabType.Livestock)
            {
                foreach (var pen in _model.LivestockPens)
                {
                    if (pen.Bounds.Contains(pt))
                    {
                        if (!pen.IsUnlocked)
                        {
                            if (_model.Gold >= pen.UnlockCost)
                            {
                                _model.Gold -= pen.UnlockCost;
                                pen.IsUnlocked = true;
                                pen.State = TaskPlotState.Growing;
                                if (_model.AchievementSystem != null) _model.AchievementSystem.CheckAllMilestones(_model);
                                _model.AddFloatingText("[증축] 축사 증축 완료!", pt.X, pt.Y, Color.FromArgb(255, 200, 100));
                                _model.Save();
                            }
                            else
                            {
                                _model.AddFloatingText("골드가 부족합니다!", pt.X, pt.Y, Color.FromArgb(255, 100, 100));
                            }
                        }
                        else if (!pen.HasFeed)
                        {
                            // 사료 주기 (밀 1개 소비하여 2배 가속)
                            if (_model.Inventory.ContainsKey("crop_wheat") && _model.Inventory["crop_wheat"] > 0)
                            {
                                _model.Inventory["crop_wheat"]--;
                                pen.HasFeed = true;
                                _model.AddFloatingText("[급여] 사료 급여 완료 (생산 2배)!", pt.X, pt.Y, Color.FromArgb(255, 230, 80));
                                _model.Save();
                            }
                            else
                            {
                                _model.AddFloatingText("사료용 밀이 없습니다!", pt.X, pt.Y, Color.FromArgb(255, 150, 100));
                            }
                        }
                        break;
                    }
                }
            }
            else if (_currentTab == FarmTabType.Restaurant)
            {
                // 영업 개시 토글 버튼
                Rectangle rcToggle = new Rectangle(235, 350, 180, 42);
                if (rcToggle.Contains(pt))
                {
                    _model.RestaurantOpen = !_model.RestaurantOpen;
                    _model.AddFloatingText(_model.RestaurantOpen ? "[영업] 다이닝 오픈!" : "[마감] 다이닝 영업 마감", 325, 330, Color.FromArgb(255, 215, 0));
                    _model.Save();
                }
            }
        }
    }
}
