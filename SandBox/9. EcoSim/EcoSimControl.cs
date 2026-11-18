using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using GateHelper.EcoSim.Models;
using GateHelper.EcoSim.Services;

namespace GateHelper.EcoSim
{
    /// <summary>
    /// SandBox 9번 모듈: 미니 생태계 진화 시뮬레이터 메인 컨트롤.
    /// </summary>
    public partial class EcoSimControl : UserControl
    {
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private readonly EcoSimulationEngine _engine;
        private readonly EcoBgmService _bgmService;
        private readonly Timer _simTimer;

        private bool _isTabVisible;
        private bool _isPaused;
        private int _speedMultiplier = 1;
        private int _selectedOrganismId = -1;
        private int _selectedHouseId = -1;
        private int _uiRefreshCounter;

        public EcoSimControl()
        {
            InitializeLayout();

            _engine = new EcoSimulationEngine();
            _bgmService = new EcoBgmService();
            _viewport.BindEngine(_engine);
            _techTree.BindEngine(_engine);
            _chart.SetHistory(_engine.History);

            _simTimer = new Timer { Interval = 33 };
            _simTimer.Tick += SimTimer_Tick;

            WireEvents();
            RefreshUiLabels();
        }

        private void WireEvents()
        {
            _btnPlayPause.Click += (s, e) => TogglePause();
            _btnReset.Click += (s, e) =>
            {
                _engine.Reset();
                _selectedOrganismId = -1;
                _viewport.SetSelectedOrganism(-1);
                _chart.SetHistory(_engine.History);
                _viewport.Invalidate();
                RefreshUiLabels();
            };

            _btnPreset.Click += (s, e) => ShowPresetMenu();

            _btnSpeed1x.Click += (s, e) => SetSpeed(1);
            _btnSpeed2x.Click += (s, e) => SetSpeed(2);
            _btnSpeed5x.Click += (s, e) => SetSpeed(5);
            _btnSpeed10x.Click += (s, e) => SetSpeed(10);

            _btnToolInspect.Click += (s, e) => SetTool(EcoToolMode.Inspect);
            _btnToolFood.Click += (s, e) => SetTool(EcoToolMode.DropFood);
            _btnToolPrey.Click += (s, e) => SetTool(EcoToolMode.SpawnPrey);
            _btnToolPredator.Click += (s, e) => SetTool(EcoToolMode.SpawnPredator);
            _btnToolBear.Click += (s, e) => SetTool(EcoToolMode.SpawnBear);
            _btnToolHuman.Click += (s, e) => SetTool(EcoToolMode.SpawnHuman);

            _btnEventAbundance.Click += (s, e) => ToggleEnvironmentEvent(EcoEnvironmentEvent.Abundance);
            _btnEventDrought.Click += (s, e) => ToggleEnvironmentEvent(EcoEnvironmentEvent.Drought);
            _btnEventMutation.Click += (s, e) => ToggleEnvironmentEvent(EcoEnvironmentEvent.HighMutation);
            _btnPlayerMode.Click += (s, e) => TogglePlayerMode();
            _btnBgm.Click += (s, e) => ToggleBgm();
            _btnHallOfFame.Click += (s, e) => ShowHallOfFameDialog();
            _btnTechTree.Click += (s, e) => ToggleTechTreeTab();
            _btnHelp.Click += (s, e) => ShowHelpDialog();

            _chkShelters.CheckedChanged += (s, e) =>
            {
                _engine.EnableShelters = _chkShelters.Checked;
                _viewport.Invalidate();
            };
            _chkCompost.CheckedChanged += (s, e) =>
            {
                _engine.EnableCompost = _chkCompost.Checked;
                _viewport.Invalidate();
            };
            _chkSeasons.CheckedChanged += (s, e) =>
            {
                _engine.EnableSeasons = _chkSeasons.Checked;
                RefreshUiLabels();
            };
            _chkWildMigration.CheckedChanged += (s, e) =>
            {
                _engine.EnableWildMigration = _chkWildMigration.Checked;
            };
            _chkDisasters.CheckedChanged += (s, e) =>
            {
                _engine.EnableDisasters = _chkDisasters.Checked;
            };

            _viewport.OrganismSelected += id =>
            {
                _selectedOrganismId = id;
                _selectedHouseId = -1;
                RefreshInspectorLabel();
            };
            _viewport.HouseSelected += houseId =>
            {
                _selectedHouseId = houseId;
                _selectedOrganismId = -1;
                RefreshInspectorLabel();
            };
            _viewport.WorldMutatedByUser += () => RefreshUiLabels();
        }

        /// <summary>
        /// 탭 비활성화/창 최소화 시 타이머를 즉시 중지하여 백그라운드 CPU 0.0%를 보장합니다.
        /// </summary>
        public void OnTabVisibilityChanged(bool isVisible)
        {
            _isTabVisible = isVisible;
            UpdateTimerState();
            _bgmService.UpdatePlayback(isVisible);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _simTimer.Stop();
                _simTimer.Dispose();
                _bgmService.Dispose();
            }
            base.Dispose(disposing);
        }

        private void TogglePlayerMode()
        {
            if (_engine.IsPlayerMode)
            {
                _engine.StopPlayerMode();
                _btnPlayerMode.Text = "★ 내개체";
                _btnPlayerMode.BackColor = Color.FromArgb(51, 65, 85);
                _btnPlayerMode.FlatAppearance.BorderColor = Color.FromArgb(71, 85, 105);
            }
            else
            {
                _engine.StartPlayerMode();
                _btnPlayerMode.Text = "★ 조종중";
                _btnPlayerMode.BackColor = Color.FromArgb(8, 145, 178);
                _btnPlayerMode.FlatAppearance.BorderColor = Color.FromArgb(34, 211, 238);
                _viewport.Focus();
            }
            _viewport.Invalidate();
            RefreshUiLabels();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (_engine.IsPlayerMode && _isTabVisible)
            {
                Keys k = keyData & Keys.KeyCode;
                if (k == Keys.Up || k == Keys.Down || k == Keys.Left || k == Keys.Right ||
                    k == Keys.W || k == Keys.A || k == Keys.S || k == Keys.D || k == Keys.Space || k == Keys.F || k == Keys.B)
                {
                    return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void PollPlayerKeyboardInput()
        {
            if (Form.ActiveForm == null)
            {
                _engine.SetPlayerInput(0f, 0f, false);
                return;
            }

            float dx = 0f, dy = 0f;
            if (IsKeyDown(Keys.Left) || IsKeyDown(Keys.A)) dx -= 1f;
            if (IsKeyDown(Keys.Right) || IsKeyDown(Keys.D)) dx += 1f;
            if (IsKeyDown(Keys.Up) || IsKeyDown(Keys.W)) dy -= 1f;
            if (IsKeyDown(Keys.Down) || IsKeyDown(Keys.S)) dy += 1f;
            bool space = IsKeyDown(Keys.Space);
            bool buildFire = IsKeyDown(Keys.F);
            bool buildHouse = IsKeyDown(Keys.B);

            _engine.SetPlayerInput(dx, dy, space, buildFire, buildHouse);
        }

        private static bool IsKeyDown(Keys key) => (GetAsyncKeyState((int)key) & 0x8000) != 0;

        private void ToggleBgm()
        {
            using (var dlg = new Form())
            {
                dlg.Text = "♪ BGM 설정";
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.ClientSize = new Size(340, 200);
                dlg.BackColor = Color.FromArgb(20, 25, 35);
                dlg.ForeColor = Color.FromArgb(226, 232, 240);

                var lblTrack = new Label
                {
                    Location = new Point(16, 16),
                    Size = new Size(308, 22),
                    Font = new Font("Malgun Gothic", 9f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(148, 163, 184),
                    Text = $"트랙: {_bgmService.CurrentTrackName}"
                };

                var chkEnable = new CheckBox
                {
                    Location = new Point(16, 48),
                    AutoSize = true,
                    Text = "BGM 재생 (ON/OFF)",
                    Checked = !_bgmService.IsMuted,
                    Font = new Font("Malgun Gothic", 9.5f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(226, 232, 240),
                    Cursor = Cursors.Hand
                };

                var btnNext = CreateBarButton("▶ 다음 곡", 86, Color.FromArgb(51, 65, 85));
                btnNext.Location = new Point(238, 44);
                btnNext.Height = 26;

                var lblVol = new Label
                {
                    Location = new Point(16, 84),
                    Size = new Size(308, 20),
                    Font = new Font("Malgun Gothic", 9f, FontStyle.Regular),
                    ForeColor = Color.FromArgb(203, 213, 225),
                    Text = $"볼륨: {_bgmService.VolumePercent}%"
                };

                var slider = new TrackBar
                {
                    Location = new Point(14, 106),
                    Size = new Size(312, 42),
                    Minimum = 0,
                    Maximum = 100,
                    TickFrequency = 10,
                    Value = _bgmService.VolumePercent,
                    BackColor = Color.FromArgb(20, 25, 35)
                };

                var btnClose = CreateBarButton("닫기", 80, Color.FromArgb(37, 99, 235));
                btnClose.Location = new Point(244, 158);
                btnClose.Click += (s, e) => dlg.Close();

                chkEnable.CheckedChanged += (s, e) =>
                {
                    bool muted = !chkEnable.Checked;
                    _bgmService.SetMuted(muted, _isTabVisible);
                    UpdateBgmButtonVisual(muted);
                };

                btnNext.Click += (s, e) =>
                {
                    _bgmService.NextTrack(_isTabVisible);
                    lblTrack.Text = $"트랙: {_bgmService.CurrentTrackName}";
                };

                slider.Scroll += (s, e) =>
                {
                    _bgmService.SetVolume(slider.Value);
                    lblVol.Text = $"볼륨: {slider.Value}%";
                };

                dlg.Controls.AddRange(new Control[] { lblTrack, chkEnable, btnNext, lblVol, slider, btnClose });
                dlg.AcceptButton = btnClose;
                dlg.CancelButton = btnClose;
                dlg.ShowDialog(this);
            }
        }

        private void UpdateBgmButtonVisual(bool muted)
        {
            _btnBgm.Text = muted ? "♪ OFF" : "♪ BGM";
            _btnBgm.BackColor = muted ? Color.FromArgb(51, 65, 85) : Color.FromArgb(22, 101, 52);
            _btnBgm.FlatAppearance.BorderColor = muted ? Color.FromArgb(71, 85, 105) : Color.FromArgb(34, 197, 94);
        }

        private void SimTimer_Tick(object sender, EventArgs e)
        {
            if (_engine.IsPlayerMode)
            {
                PollPlayerKeyboardInput();
            }

            for (int i = 0; i < _speedMultiplier; i++)
            {
                _engine.Step();
                if (_engine.PendingPerkChoice)
                {
                    break;
                }
            }

            _viewport.Invalidate();

            if (_engine.PendingPerkChoice)
            {
                _simTimer.Stop();
                ShowPerkChoiceDialog();
                UpdateTimerState();
                RefreshUiLabels();
                return;
            }

            _uiRefreshCounter++;
            if (_uiRefreshCounter >= 3)
            {
                _uiRefreshCounter = 0;
                _chart.Invalidate();
                if (_techTree.Visible) _techTree.Invalidate();
                RefreshUiLabels();
            }
        }

        private void ToggleTechTreeTab()
        {
            bool showTech = !_techTree.Visible;
            _techTree.Visible = showTech;
            _viewport.Visible = !showTech;
            _btnTechTree.Text = showTech ? "🗺 생태계" : "📜 기술";
            _btnTechTree.BackColor = showTech ? Color.FromArgb(217, 119, 6) : Color.FromArgb(30, 58, 138);
            if (showTech) _techTree.Invalidate();
            else _viewport.Invalidate();
        }

        private void TogglePause()
        {
            _isPaused = !_isPaused;
            _btnPlayPause.Text = _isPaused ? "▶ 재개" : "❚❚ 정지";
            _btnPlayPause.BackColor = _isPaused ? Color.FromArgb(22, 163, 74) : Color.FromArgb(37, 99, 235);
            UpdateTimerState();
        }

        private void SetSpeed(int mult)
        {
            _speedMultiplier = mult;
            Color active = Color.FromArgb(37, 99, 235);
            Color idle = Color.FromArgb(51, 65, 85);

            _btnSpeed1x.BackColor = mult == 1 ? active : idle;
            _btnSpeed2x.BackColor = mult == 2 ? active : idle;
            _btnSpeed5x.BackColor = mult == 5 ? active : idle;
            _btnSpeed10x.BackColor = mult == 10 ? active : idle;
        }

        private void SetTool(EcoToolMode mode)
        {
            _viewport.SetToolMode(mode);
            Color active = Color.FromArgb(14, 116, 144);
            Color idle = Color.FromArgb(51, 65, 85);

            _btnToolInspect.BackColor = mode == EcoToolMode.Inspect ? active : idle;
            _btnToolFood.BackColor = mode == EcoToolMode.DropFood ? active : idle;
            _btnToolPrey.BackColor = mode == EcoToolMode.SpawnPrey ? active : idle;
            _btnToolPredator.BackColor = mode == EcoToolMode.SpawnPredator ? active : idle;
            _btnToolBear.BackColor = mode == EcoToolMode.SpawnBear ? active : idle;
            _btnToolHuman.BackColor = mode == EcoToolMode.SpawnHuman ? active : idle;
        }

        private void ToggleEnvironmentEvent(EcoEnvironmentEvent envEvent)
        {
            _engine.ToggleEvent(envEvent);
            UpdateEventButtonStyles();
            RefreshUiLabels();
        }

        private void UpdateEventButtonStyles()
        {
            Color idle = Color.FromArgb(51, 65, 85);
            _btnEventAbundance.BackColor = _engine.IsAbundance ? Color.FromArgb(22, 163, 74) : idle;
            _btnEventDrought.BackColor = _engine.IsDrought ? Color.FromArgb(217, 119, 6) : idle;
            _btnEventMutation.BackColor = _engine.IsHighMutation ? Color.FromArgb(147, 51, 234) : idle;
        }

        private void UpdateTimerState()
        {
            if (_isTabVisible && !_isPaused)
            {
                if (!_simTimer.Enabled) _simTimer.Start();
            }
            else
            {
                if (_simTimer.Enabled) _simTimer.Stop();
            }
        }

        private void RefreshUiLabels()
        {
            var hist = _engine.History;
            float avgPreySpd = hist.Count > 0 ? hist[hist.Count - 1].AvgPreySpeed : 0f;
            float avgPredSpd = hist.Count > 0 ? hist[hist.Count - 1].AvgPredatorSpeed : 0f;

            int year = _engine.TickCount / 880;
            string nextStats =
                $"틱: {_engine.TickCount:N0} ({year}년 · {_engine.CurrentEraLabel})  |  최고: {_engine.MaxGeneration}세대\n" +
                $"초식: {_engine.PreyCount} (속도 {avgPreySpd:F2})  |  육식: {_engine.PredatorCount} (속도 {avgPredSpd:F2})\n" +
                $"곰: {_engine.BearCount}마리   |   인간: {_engine.HumanCount}명\n" +
                $"식물 먹이: {_engine.Foods.Count}개   |   배속: {_speedMultiplier}x";
            if (_lblStatsSummary.Text != nextStats)
            {
                _lblStatsSummary.Text = nextStats;
            }

            UpdateEventButtonStyles();

            string seasonLabel = _engine.EnableSeasons ? GetSeasonKorean(_engine.CurrentSeason) : "고정";
            string nextEventText;
            Color nextEventColor;
            if (!_engine.IsAbundance && !_engine.IsDrought && !_engine.IsHighMutation)
            {
                nextEventText = $"계절: {seasonLabel}  |  특수: 평온";
                nextEventColor = GetSeasonColor(_engine.EnableSeasons ? _engine.CurrentSeason : EcoSeason.Spring);
            }
            else
            {
                string foodPart = _engine.IsAbundance ? "풍년 ON" : (_engine.IsDrought ? "가뭄 ON" : "");
                string mutPart = _engine.IsHighMutation ? "변이3x ON" : "";
                string joined = (foodPart != "" && mutPart != "") ? $"{foodPart}·{mutPart}" : (foodPart + mutPart);
                nextEventText = $"계절: {seasonLabel}  |  {joined}";
                nextEventColor = _engine.IsDrought
                    ? Color.FromArgb(251, 146, 60)
                    : (_engine.IsAbundance ? Color.FromArgb(74, 222, 128) : Color.FromArgb(192, 132, 252));
            }

            if (_lblEventStatus.Text != nextEventText)
            {
                _lblEventStatus.Text = nextEventText;
            }
            if (_lblEventStatus.ForeColor != nextEventColor)
            {
                _lblEventStatus.ForeColor = nextEventColor;
            }

            RefreshInspectorLabel();
        }

        private void RefreshInspectorLabel()
        {
            string nextInspector;
            bool bagVisible = false;
            int bagMeat = 0;
            int bagWood = 0;
            int bagStone = 0;
            bool bagPlayerHuman = false;

            if (_engine.IsPlayerMode)
            {
                var p = _engine.FindOrganismById(_engine.PlayerId);
                string stageName = GetSpeciesKorean(p != null ? p.Species : EcoSpecies.Prey);
                if (p != null && p.Species == EcoSpecies.Human)
                {
                    stageName = _engine.PlayerStage >= 5 ? "인간 (궁수)" : "인간 (창병)";
                    bagVisible = true;
                    bagMeat = p.MeatCount;
                    bagWood = p.WoodCount;
                    bagStone = p.StoneCount;
                    bagPlayerHuman = true;
                }
                string expLine = _engine.PlayerStage >= 5
                    ? "EXP: MAX (최종 진화!)"
                    : $"EXP: {_engine.PlayerExp:F0} / {_engine.PlayerNextExp:F0}";
                string skillReady = (p != null && p.AttackCooldown == 0) ? "준비 완료!" : $"쿨타임 {p?.AttackCooldown ?? 0}";
                string energyLine = p != null ? $"{p.Energy:F0} / {p.MaxEnergy:F0}" : "-";

                nextInspector =
                    $"[내 개체] {_engine.PlayerStage}단계 {stageName}\n" +
                    $"체력: {energyLine}  |  사냥: {p?.Kills ?? 0}회\n" +
                    $"{expLine}  |  스킬: {skillReady}\n" +
                    $"{_engine.PlayerPerkSummary}\n" +
                    $"최고: {_engine.PlayerBestStage}단계  |  사망: {_engine.PlayerDeathCount}회\n" +
                    $"──────────────\n" +
                    $"{_engine.PlayerNotice}";
            }
            else if (_selectedHouseId >= 0)
            {
                int hIdx = _engine.FindHouseIndexById(_selectedHouseId);
                if (hIdx < 0)
                {
                    nextInspector = $"오두막 #{_selectedHouseId}\n상태: 수명 만료 또는 파괴됨";
                }
                else
                {
                    var h = _engine.Houses[hIdx];
                    string stateStr = h.IsComplete ? "완공" : $"공사중 ({h.BuildProgress}%)";
                    float remYears = h.RemainingTicks / 880f;
                    string o1 = h.Owner1Id > 0 ? $"#{h.Owner1Id}" : "없음";
                    string o2 = h.Owner2Id > 0 ? $"#{h.Owner2Id}" : "빈 자리";
                    nextInspector =
                        $"🏠 오두막 #{h.Id} ({stateStr})\n" +
                        $"내구도: {h.Hp:F0} / {h.MaxHp:F0}\n" +
                        $"남은 수명: {remYears:F1}년 ({h.RemainingTicks}틱)\n" +
                        $"집주인 1: {o1}  |  집주인 2: {o2}\n" +
                        $"──────────────\n" +
                        $"겨울 월동 · 회복 +0.55/틱\n" +
                        $"대사 소모 45% · 번식 완화";
                }
            }
            else if (_selectedOrganismId < 0)
            {
                nextInspector = "뷰포트에서 개체·집을 클릭하면\n유전자(DNA) 또는 집 정보를\n추적합니다.";
            }
            else
            {
                var org = _engine.FindOrganismById(_selectedOrganismId);
                if (org == null)
                {
                    nextInspector = $"개체 #{_selectedOrganismId}\n상태: 자연사 또는 포식됨";
                }
                else
                {
                    string speciesName;
                    switch (org.Species)
                    {
                        case EcoSpecies.Prey: speciesName = "초식(토끼)"; break;
                        case EcoSpecies.Predator: speciesName = "육식(여우)"; break;
                        case EcoSpecies.Bear: speciesName = org.IsHibernating ? "곰 (겨울잠)" : "곰 (최상위)"; break;
                        default: speciesName = org.Kills >= 3 ? "인간 (궁수)" : "인간 (창병)"; break;
                    }
                    if (org.Species == EcoSpecies.Human)
                    {
                        bagVisible = true;
                        bagMeat = org.MeatCount;
                        bagWood = org.WoodCount;
                        bagStone = org.StoneCount;
                    }
                    int ageYears = org.AgeTicks / 880;
                    nextInspector =
                        $"개체 #{org.Id} · {speciesName}\n" +
                        $"세대: {org.Generation}세대  |  나이: {ageYears}년\n" +
                        $"에너지: {org.Energy:F1} / {org.MaxEnergy:F1}\n" +
                        $"자손: {org.OffspringCount}마리" +
                        (org.Species != EcoSpecies.Prey ? $"  |  사냥: {org.Kills}회\n" : "\n") +
                        $"──────────────\n" +
                        $"[DNA] 속도: {org.Dna.Speed:F2}\n" +
                        $"[DNA] 시야: {org.Dna.Vision:F1}\n" +
                        $"[DNA] 크기: {org.Dna.Size:F2}";
                }
            }

            _inventoryBag.SetState(bagVisible, bagMeat, bagWood, bagStone, bagPlayerHuman, _engine.IsHouseAgeUnlocked);

            if (_lblInspector.Text != nextInspector)
            {
                _lblInspector.Text = nextInspector;
            }
        }

        private void ShowHelpDialog()
        {
            using (var dlg = new Form())
            {
                dlg.Text = "EcoSim — 미니 생태계 진화 시뮬레이터 도움말";
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.ClientSize = new Size(560, 470);
                dlg.BackColor = Color.FromArgb(20, 25, 35);
                dlg.ForeColor = Color.FromArgb(226, 232, 240);

                var box = new RichTextBox
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    BorderStyle = BorderStyle.None,
                    BackColor = Color.FromArgb(24, 30, 41),
                    ForeColor = Color.FromArgb(226, 232, 240),
                    Font = new Font("Malgun Gothic", 9.5f, FontStyle.Regular),
                    Padding = new Padding(14),
                    Text =
                        "■ 1. 생태계 구성 요소 (색상 안내)\n" +
                        "  ● 초록색 점 (풀/먹이) : 기본적으로 약 1.5틱마다 1개씩 자랍니다 (최대 320개).\n" +
                        "     - 풍년 ON: 2.6배 빠르게 자람  |  가뭄 ON: 0.25배로 급감\n\n" +
                        "  ● 파란색 원 (초식동물) : 시야 안의 풀을 찾아 먹으며 에너지(+36)를 얻습니다.\n" +
                        "     - 시야에 육식동물이 들어오면 반대 방향으로 도망칩니다.\n" +
                        "     - 속도가 3.0 이상으로 진화하면 밝은 하늘색으로 보입니다.\n\n" +
                        "  ● 빨간색 원 (육식동물) : 시야 안의 초식동물을 사냥해 에너지(+68~)를 얻습니다.\n" +
                        "     - 곰과 인간이 다가오면 도망칩니다. 속도 3.4 이상은 주황색!\n\n" +
                        "  ● 갈색 곰 (최상위 잡식) : 여우·가까운 인간·토끼를 사냥하며, 먹잇감이 없으면 풀도 먹습니다.\n" +
                        "     - [광폭 돌진] 체력 30% 미만 위기(또는 겨울잠 피격) 시 1.85배 속도로 최후의 돌진(쿨타임 260틱)!\n" +
                        "     - 겨울이 되면 동면(Zzz)에 들어가 에너지 소모가 14%로 줄어듭니다.\n\n" +
                        "  ● 인간 (창병 → 궁수, 그룹별 색상) : 처음엔 창(사거리 48)으로 싸우다 3킬 달성 시 활(사거리 94)로 진화합니다.\n" +
                        "     - 사냥 성공 시 [고기 +1], 식물 위 채집 시(식물 훼손 없음) [나뭇가지 +1] 획득 (칸당 최대 20개).\n" +
                        "     - 겨울에는 나뭇가지 5개로 모닥불을 짓고, 같은 모닥불 그룹끼리 동일 색상을 띠며 범위 안에서 월동합니다.\n\n" +
                        "■ 2. 번식과 도태 (종별 비율 캡 적용)\n" +
                        "  - 번식 : 초식(최대 65%), 육식(최대 25%), 곰(최대 6%), 인간(최대 4명) 비율 내에서 에너지를 나누어 번식합니다.\n" +
                        "  - 자연사 : 움직이는 매 틱마다 에너지를 소모하며, 에너지가 0 이하가 되면 사망합니다.\n\n" +
                        "■ 3. 유전자(DNA) 진화 & 트레이드오프\n" +
                        "  - 자손이 태어날 때 25% 확률(변이3x ON 시 75%)로 [속도 / 시야 / 크기]가 변이합니다.\n" +
                        "  - 속도가 빠르고 시야가 넓고 덩치가 클수록 사냥·도주에 유리하지만,\n" +
                        "    매 틱 소모되는 에너지도 함께 증가하므로 환경에 맞는 종만 살아남습니다.\n\n" +
                        "■ 4. 조작 & 실험 도구\n" +
                        "  - [관찰] : 개체를 클릭하면 노란색 시야 반경과 우측 DNA/세대 정보를 추적합니다.\n" +
                        "  - [먹이+ / 초식+ / 육식+] : 화면을 클릭(먹이+는 드래그 가능)해 직접 투하합니다.\n" +
                        "  - [풍년 / 가뭄 / 변이3x] : ON/OFF 토글로 환경 조건을 조합해 실험할 수 있습니다.\n\n" +
                        "■ 5. 생태계 순환 옵션 (체크박스 ON/OFF)\n" +
                        "  - [은신처(수풀)] : 초록 수풀 안의 초식동물은 포식자 감지 거리가 42%로 감소합니다.\n" +
                        "  - [사체-비료 순환] : 죽은 개체 자리에 갈색 비료가 남았다가 45틱 후 풀 2~3개로 자랍니다.\n" +
                        "  - [사계절 순환] : 220틱마다 봄/여름/가을/겨울이 순환하며 풀 성장·소모량이 변합니다.\n" +
                        "  - [영구 생태계(이주)] : 한 종이 멸종하면 화면 외곽에서 야생 개체가 자연 이주합니다.\n" +
                        "  - [랜덤 자연재해] : 주기적으로 일부 구역에 국지 낙뢰(비료 생성)·한파 돌풍·축복의 단비(새싹)가 내립니다.\n\n" +
                        "■ 6. 지형 · 모닥불 · 명예의 전당\n" +
                        "  - [연못 & 바위] : 바위는 우회해야 하는 장애물이며, 연못 가장자리는 풀이 잘 자랍니다.\n" +
                        "  - [인간 모닥불 & 고기 회복] : 나뭇가지 5개로 모닥불(수명 480틱, 봄에는 빠르게 소멸)을 지어 체력을 회복하고 겨울을 버팁니다.\n" +
                        "    체력이 38% 미만으로 떨어지거나 겨울 모닥불 휴식 중엔 고기를 먹어 +85 에너지를 회복합니다.\n" +
                        "  - [★ 명예의 전당] : 역대 최고령·최다 사냥·번식왕·최고속 기록을 집계하며 현재 생존 1위 개체 위에 황금 ★이 표시됩니다.\n\n" +
                        "■ 7. 내 개체 키우기 (로그라이크 & 알파 곰 보스)\n" +
                        "  - [★ 내개체] 클릭 시 1단계 토끼로 시작, 방향키/WASD 이동, Space 대시/공격, F 모닥불 건설(인간)!\n" +
                        "  - [토끼 → 여우 → 곰 → 인간(창) → 인간(활)] 진화할 때마다 로그라이크 특성 3택1 선택!\n" +
                        "  - 5단계 달성 시 주기적으로 [알파 곰 보스(BOSS)]가 출현하며, 범위 할퀴기(반경 56)와 광폭 돌진을 사용합니다. 사망 시 1단계 재시작.\n" +
                        "  - [✦ 프리셋] 버튼으로 곰vs인간 전면전·빙하기·초고속 진화 실험이 가능합니다."
                };

                var bottomPanel = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = 46,
                    BackColor = Color.FromArgb(20, 25, 35),
                    Padding = new Padding(10, 8, 12, 8)
                };

                var btnClose = CreateBarButton("닫기", 84, Color.FromArgb(37, 99, 235));
                btnClose.Dock = DockStyle.Right;
                btnClose.Click += (s, e) => dlg.Close();
                bottomPanel.Controls.Add(btnClose);

                dlg.Controls.Add(box);
                dlg.Controls.Add(bottomPanel);
                dlg.AcceptButton = btnClose;
                dlg.CancelButton = btnClose;
                dlg.ShowDialog(this);
            }
        }
        private void ShowPresetMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("기본 생태계 (균형)", null, (s, e) => ApplyPresetAndRefresh(EcoPreset.Default));
            menu.Items.Add("곰 vs 인간 전면전 (10:10)", null, (s, e) => ApplyPresetAndRefresh(EcoPreset.BearVsHumanWar));
            menu.Items.Add("극한 빙하기 (겨울 고정 + 가뭄)", null, (s, e) => ApplyPresetAndRefresh(EcoPreset.IceAge));
            menu.Items.Add("초고속 진화 실험 (풍년 + 변이3x)", null, (s, e) => ApplyPresetAndRefresh(EcoPreset.HyperEvolution));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("★ 알파 곰 보스 즉시 소환", null, (s, e) =>
            {
                _engine.SpawnAlphaBearBoss();
                _viewport.Invalidate();
                RefreshUiLabels();
            });
            menu.Show(_btnPreset, new Point(0, _btnPreset.Height));
        }

        private void ApplyPresetAndRefresh(EcoPreset preset)
        {
            _engine.ApplyPreset(preset);
            _selectedOrganismId = -1;
            _viewport.SetSelectedOrganism(-1);
            _chart.SetHistory(_engine.History);
            _viewport.Invalidate();
            RefreshUiLabels();
        }

        private void ShowPerkChoiceDialog()
        {
            var choices = _engine.RollPerkChoices(3);
            using (var dlg = new Form())
            {
                dlg.Text = $"★ 진화 축하! {_engine.PlayerStage}단계 로그라이크 특성 선택";
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.ControlBox = false;
                dlg.ClientSize = new Size(460, 220);
                dlg.BackColor = Color.FromArgb(20, 25, 35);
                dlg.ForeColor = Color.FromArgb(226, 232, 240);

                var lblTitle = new Label
                {
                    Dock = DockStyle.Top,
                    Height = 38,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Malgun Gothic", 10f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(250, 204, 21),
                    Text = $"{_engine.PlayerStage}단계 진화! 3가지 특성 중 1개를 선택하세요"
                };

                var cardPanel = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 3,
                    RowCount = 1,
                    Padding = new Padding(12, 8, 12, 14)
                };
                cardPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
                cardPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
                cardPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));

                for (int i = 0; i < choices.Length; i++)
                {
                    PlayerPerk perk = choices[i];
                    var btn = new Button
                    {
                        Dock = DockStyle.Fill,
                        FlatStyle = FlatStyle.Flat,
                        BackColor = Color.FromArgb(30, 41, 59),
                        ForeColor = Color.FromArgb(226, 232, 240),
                        Font = new Font("Malgun Gothic", 9.2f, FontStyle.Bold),
                        Margin = new Padding(5),
                        Cursor = Cursors.Hand,
                        Text = GetPerkDescription(perk)
                    };
                    btn.FlatAppearance.BorderColor = Color.FromArgb(56, 189, 248);
                    btn.FlatAppearance.BorderSize = 1;
                    btn.Click += (s, e) =>
                    {
                        _engine.ApplyPlayerPerk(perk);
                        dlg.Close();
                    };
                    cardPanel.Controls.Add(btn, i, 0);
                }

                dlg.Controls.Add(cardPanel);
                dlg.Controls.Add(lblTitle);
                dlg.ShowDialog(this);
            }
            _viewport.Focus();
        }

        private static string GetPerkDescription(PlayerPerk perk)
        {
            switch (perk)
            {
                case PlayerPerk.Swiftness: return "⚡ 날렵한 발놀림\n\n이동 속도 +15%";
                case PlayerPerk.Vitality: return "♥ 강인한 생명력\n\n최대 체력 +25%\n& 즉시 회복";
                case PlayerPerk.PredatorInstinct: return "★ 야수의 본능\n\n사냥·섭식 시\n회복/EXP +30%";
                case PlayerPerk.QuickCooldown: return "↻ 숙련된 사냥꾼\n\nSpace 스킬 쿨타임\n25% 감소";
                default: return "✦ 예리한 무기\n\n창/활 공격력 +30%\n& 사거리 +15%";
            }
        }

        private void ShowHallOfFameDialog()
        {
            var hof = _engine.HallOfFame;
            var legend = hof.LivingLegendId >= 0 ? _engine.FindOrganismById(hof.LivingLegendId) : null;
            string legendInfo = legend != null
                ? $"#{legend.Id} ({GetSpeciesKorean(legend.Species)}) — 나이 {legend.AgeTicks / 880}년, 사냥 {legend.Kills}회, 자손 {legend.OffspringCount}마리"
                : "현재 생존 중인 전설 개체 없음";

            var sbAch = new System.Text.StringBuilder();
            int unlockedCount = 0;
            for (int i = 0; i < _engine.Achievements.Count; i++)
            {
                var a = _engine.Achievements[i];
                if (a.Unlocked) unlockedCount++;
                string mark = a.Unlocked ? "[완료]" : "[미달성]";
                sbAch.AppendLine($"  {mark} {a.Title} — {a.Description}");
            }

            using (var dlg = new Form())
            {
                dlg.Text = "★ EcoSim — 명예의 전당 & 업적";
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.ClientSize = new Size(500, 480);
                dlg.BackColor = Color.FromArgb(20, 25, 35);
                dlg.ForeColor = Color.FromArgb(226, 232, 240);

                var lbl = new Label
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(16),
                    Font = new Font("Malgun Gothic", 9.3f, FontStyle.Regular),
                    BackColor = Color.FromArgb(24, 30, 41),
                    ForeColor = Color.FromArgb(226, 232, 240),
                    Text =
                        "★ 현재 생존 전설 (맵 위 황금 ★ 표시)\n" +
                        $"   {legendInfo}\n\n" +
                        "────────────────────────────────────\n" +
                        $"● 역대 최고령 (장수왕) : #{hof.OldestId} ({GetSpeciesKorean(hof.OldestSpecies)}) — {hof.OldestAgeTicks / 880}년\n" +
                        $"● 역대 최다 사냥꾼 : #{hof.TopKillerId} ({GetSpeciesKorean(hof.TopKillerSpecies)}) — {hof.MostKills}회\n" +
                        $"● 역대 최다 번식왕 : #{hof.TopParentId} ({GetSpeciesKorean(hof.TopParentSpecies)}) — {hof.MostOffspring}마리\n" +
                        $"● 역대 최고 속도 진화 : #{hof.FastestId} ({GetSpeciesKorean(hof.FastestSpecies)}) — 속도 {hof.FastestSpeed:F2}\n\n" +
                        $"■ 생태계 업적 ({unlockedCount} / {_engine.Achievements.Count})\n" +
                        sbAch.ToString()
                };

                var bottomPanel = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = 46,
                    BackColor = Color.FromArgb(20, 25, 35),
                    Padding = new Padding(10, 8, 12, 8)
                };

                var btnClose = CreateBarButton("닫기", 84, Color.FromArgb(161, 98, 7));
                btnClose.Dock = DockStyle.Right;
                btnClose.Click += (s, e) => dlg.Close();
                bottomPanel.Controls.Add(btnClose);

                dlg.Controls.Add(lbl);
                dlg.Controls.Add(bottomPanel);
                dlg.AcceptButton = btnClose;
                dlg.CancelButton = btnClose;
                dlg.ShowDialog(this);
            }
        }

        private static string GetSpeciesKorean(EcoSpecies species)
        {
            switch (species)
            {
                case EcoSpecies.Prey: return "초식(토끼)";
                case EcoSpecies.Predator: return "육식(여우)";
                case EcoSpecies.Bear: return "곰";
                case EcoSpecies.Human: return "인간";
                default: return "-";
            }
        }

        private static string GetSeasonKorean(EcoSeason season)
        {
            switch (season)
            {
                case EcoSeason.Spring: return "봄(성장)";
                case EcoSeason.Summer: return "여름(풍요)";
                case EcoSeason.Autumn: return "가을(수확)";
                case EcoSeason.Winter: return "겨울(혹한)";
                default: return "평온";
            }
        }

        private static Color GetSeasonColor(EcoSeason season)
        {
            switch (season)
            {
                case EcoSeason.Spring: return Color.FromArgb(74, 222, 128);
                case EcoSeason.Summer: return Color.FromArgb(250, 204, 21);
                case EcoSeason.Autumn: return Color.FromArgb(251, 146, 60);
                case EcoSeason.Winter: return Color.FromArgb(125, 211, 252);
                default: return Color.FromArgb(148, 163, 184);
            }
        }
    }
}
