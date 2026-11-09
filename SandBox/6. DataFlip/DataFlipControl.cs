using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace GateHelper.DataFlip
{
    public partial class DataFlipControl : UserControl
    {
        private CoreFrontGameState _game;
        private Timer _animTimer;
        private bool _showGuide = false;
        private bool _showAchievements = false;

        // 선택된 액션 (건물 또는 유닛 또는 유저 스킬)
        private BuildingType _selectedBuilding = BuildingType.None;
        private UnitType _selectedUnit = UnitType.None;
        private UserSkillType _selectedSkill = UserSkillType.None;

        // 마우스 상호작용
        private Point _hoverTile = new Point(-1, -1);
        private int _hoverButtonId = -1;
        private int _hoverCardIndex = -1;

        // 스타크래프트식 하단 카테고리 탭 (인프라 / 유닛 / 전술스킬 3대 탭)
        private enum BottomCategory
        {
            Buildings, // 🏗️ 건물 인프라
            Units,     // ⚔️ 기동 병력
            Skills     // ⚡ 전술 스킬
        }
        private BottomCategory _currentCategory = BottomCategory.Buildings;

        // 레이아웃 영역 캐시
        private Rectangle _btnBackRect;
        private Rectangle _boardRect;
        private int _tileW = 44;
        private int _tileH = 43;

        // 하단 카테고리 탭 영역 (3개 탭)
        private Rectangle _tabBuildingsRect;
        private Rectangle _tabUnitsRect;
        private Rectangle _tabSkillsRect;

        // 6개 액션 슬롯 영역 (인프라 5개 / 유닛 6개 / 스킬 4개)
        private Rectangle[] _slotRects = new Rectangle[6];

        // 기존 참조 호환용 영역 (슬롯으로 매핑됨)
        private Rectangle _btnBuildPlantRect => _slotRects[0];
        private Rectangle _btnBuildTurretRect => _slotRects[1];
        private Rectangle _btnBuildWallRect => _slotRects[2];
        private Rectangle _btnBuildFactoryRect => _slotRects[3];
        private Rectangle _btnExpandGridRect;

        private Rectangle _btnSpawnVanguardRect => _slotRects[0];
        private Rectangle _btnSpawnRaiderRect => _slotRects[1];
        private Rectangle _btnSpawnTitanRect => _slotRects[2];
        private Rectangle _btnSpawnCheongungRect => _slotRects[3];
        private Rectangle _btnSpawnColossusRect => _slotRects[4];
        private Rectangle _btnSpawnDreadnoughtRect => _slotRects[5];

        private Rectangle _btnEndTurnRect;
        private Rectangle _btnRestartRect;
        private Rectangle _btnGuideRect;
        private Rectangle _btnAchievementsBottomRect;
        private Rectangle _btnAchievementsRect;

        // 로그라이크 카드 3개 영역
        private Rectangle[] _cardRects = new Rectangle[3];

        // 게임오버 재시작 버튼 영역 및 리플레이
        private Rectangle _btnReplayRect;
        private Rectangle _gameOverRestartBtnRect;
        private Rectangle _gameOverBackBtnRect;

        // 리플레이 제어
        private bool _isReplaying = false;
        private bool _isReplayPaused = false;
        private int _replayIndex = 0;
        private int _replaySpeed = 1; // 1x, 2x, 4x
        private int _replayTickCounter = 0;
        private Rectangle _repProgressBarRect;
        private Rectangle _repBtnFirstRect;
        private Rectangle _repBtnPrevRect;
        private Rectangle _repBtnPlayPauseRect;
        private Rectangle _repBtnNextRect;
        private Rectangle _repBtnLastRect;
        private Rectangle _repBtnSpeedRect;
        private Rectangle _repBtnExitRect;
        private int _hoverReplayBtnId = -1; // 1: First, 2: Prev, 3: PlayPause, 4: Next, 5: Last, 6: Speed, 7: Exit, 8: ProgressBar

        // 맵 사이즈 선택 모달 및 토글 영역 (소형 12x6 / 표준 14x8 / 대형 18x8 / 초대형 20x10)
        private bool _isMapSizeSelectActive = true;
        private int _hoverMapSelectIndex = -1;
        private Rectangle _mapSelectModalRect;
        private readonly Rectangle[] _mapSelectCardRects = new Rectangle[4];
        private readonly Rectangle[] _mapSelectBtnRects = new Rectangle[4];
        private Rectangle _btnMapSmallRect;
        private Rectangle _btnMapStdRect;
        private Rectangle _btnMapLargeRect;
        private Rectangle _btnMapHugeRect;

        // 인게임 턴 진행 배속 제어 (1x: 기본 1.5초, 2x: 고속 0.75초, 4x: 초고속 0.38초)
        private int _turnPlaySpeed = 1; // 1, 2, 4
        private Rectangle _btnTurnSpeedRect;

        // 한줄 전투 로그 배너 영역 및 호버 상태
        private Rectangle _combatLogRect;
        private bool _hoverOnCombatLog = false;

        // 이펙트 & 플로팅 텍스트
        private List<CombatFloatingText> _floatingTexts = new List<CombatFloatingText>();
        private List<ShockwaveRing> _shockwaves = new List<ShockwaveRing>();
        private List<CombatLaserBeam> _laserBeams = new List<CombatLaserBeam>();
        private float _pulseAngle = 0f;

        // 턴 시네마틱 애니메이션 제어 (슬로우 이동 & 단계별 교전)
        private enum TurnAnimPhase
        {
            Idle = 0,
            PlayerTurret = 1,
            PlayerMarch = 2,
            AiTurret = 3,
            AiMarch = 4,
            AiDecision = 5
        }

        private TurnAnimPhase _animPhase = TurnAnimPhase.Idle;
        private int _phaseTimer = 0;
        private bool _isTurnRunning = false;

        // 색상 팔레트
        private readonly Color _colorBg = Color.FromArgb(13, 17, 23);
        private readonly Color _colorBoardBg = Color.FromArgb(22, 27, 34);
        private readonly Color _colorGridLine = Color.FromArgb(48, 54, 61);
        private readonly Color _colorCyan = Color.FromArgb(0, 229, 255);
        private readonly Color _colorCrimson = Color.FromArgb(255, 23, 68);
        private readonly Color _colorGold = Color.FromArgb(255, 215, 0);
        private readonly Color _colorEmerald = Color.FromArgb(0, 230, 118);
        private readonly Color _colorPurple = Color.FromArgb(171, 71, 188);

        public DataFlipControl()
        {
            _game = new CoreFrontGameState();
            _game.InitializeMap();

            InitializeComponent();

            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                           ControlStyles.UserPaint |
                           ControlStyles.OptimizedDoubleBuffer |
                           ControlStyles.Selectable |
                           ControlStyles.ResizeRedraw, true);
            this.TabStop = true;

            _animTimer = new Timer();
            _animTimer.Interval = 30; // ~33 FPS
            _animTimer.Tick += AnimTimer_Tick;
            _animTimer.Start();

            UpdateLayoutBounds();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateLayoutBounds();
            Invalidate();
        }

        public void OnTabVisibilityChanged(bool isVisible)
        {
            if (isVisible)
            {
                if (_animTimer != null && !_animTimer.Enabled) _animTimer.Start();
                this.Invalidate();
            }
            else
            {
                if (_animTimer != null && _animTimer.Enabled) _animTimer.Stop();
            }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            bool isVis = this.Visible && (this.ParentForm == null || this.ParentForm.WindowState != FormWindowState.Minimized);
            OnTabVisibilityChanged(isVis);
        }

        private void UpdateLayoutBounds()
        {
            if (_game == null) return;

            int w = this.Width;
            int h = this.Height;

            _btnBackRect = new Rectangle(14, 12, 68, 24);

            // 맵 사이즈 토글 4종 영역 (타이틀 우측)
            int mapBtnY = 12;
            int mapBtnW = 44;
            int mapBtnH = 24;
            _btnMapSmallRect = new Rectangle(210, mapBtnY, mapBtnW, mapBtnH);
            _btnMapStdRect = new Rectangle(_btnMapSmallRect.Right + 4, mapBtnY, mapBtnW, mapBtnH);
            _btnMapLargeRect = new Rectangle(_btnMapStdRect.Right + 4, mapBtnY, mapBtnW, mapBtnH);
            _btnMapHugeRect = new Rectangle(_btnMapLargeRect.Right + 4, mapBtnY, 52, mapBtnH);
            _btnTurnSpeedRect = new Rectangle(_btnMapHugeRect.Right + 8, mapBtnY, 48, mapBtnH);
            _btnAchievementsRect = new Rectangle(_btnTurnSpeedRect.Right + 6, mapBtnY, 82, mapBtnH);

            // 가변 보드 크기 계산 (맵 사이즈 및 창 크기에 맞춰 자동 최적화)
            int boardMaxW = Math.Max(660, w - 60);
            int boardMaxH = Math.Max(340, h - 300);
            _tileW = Math.Min(48, boardMaxW / _game.Cols);
            _tileH = Math.Min(44, boardMaxH / _game.Rows);

            int boardW = _tileW * _game.Cols;
            int boardH = _tileH * _game.Rows;
            int boardX = Math.Max(10, (w - boardW) / 2);
            int boardY = 96;
            _boardRect = new Rectangle(boardX, boardY, boardW, boardH);

            // 한줄 전투 로그 배너 영역 (상단 HUD와 전장 보드 사이)
            _combatLogRect = new Rectangle(boardX, 72, boardW, 20);

            // 하단 조작 영역 (스타크래프트식 탭 & 슬롯 구조)
            int tabY = _boardRect.Bottom + 10;
            int slotY = tabY + 30;

            int tabW = 110;
            int tabH = 26;
            _tabBuildingsRect = new Rectangle(_boardRect.X, tabY, tabW, tabH);
            _tabUnitsRect = new Rectangle(_tabBuildingsRect.Right + 6, tabY, tabW, tabH);
            _tabSkillsRect = new Rectangle(_tabUnitsRect.Right + 6, tabY, tabW, tabH);

            int slotW = 86;
            int slotH = 50;
            int slotGap = 5;
            for (int i = 0; i < 6; i++)
            {
                _slotRects[i] = new Rectangle(_boardRect.X + i * (slotW + slotGap), slotY, slotW, slotH);
            }
            // 발전소 한도 증설 버튼: 발전소(슬롯 0) 바로 아래에 세로로 배치
            _btnExpandGridRect = new Rectangle(_slotRects[0].X, _slotRects[0].Bottom + 4, slotW, 42);

            // 오른쪽 액션 버튼 (턴 종료 및 유틸리티)
            int slotsEndX = _boardRect.X + 6 * (slotW + slotGap) - slotGap;
            int actW = Math.Min(150, Math.Max(120, _boardRect.Right - (slotsEndX + 16)));
            int rightX = Math.Max(slotsEndX + 12, _boardRect.Right - actW);

            _btnEndTurnRect = new Rectangle(rightX, tabY, actW, 46);
            int subBtnW = (actW - 8) / 3;
            _btnRestartRect = new Rectangle(rightX, _btnEndTurnRect.Bottom + 6, subBtnW, 26);
            _btnGuideRect = new Rectangle(_btnRestartRect.Right + 4, _btnEndTurnRect.Bottom + 6, subBtnW, 26);
            _btnAchievementsBottomRect = new Rectangle(_btnGuideRect.Right + 4, _btnEndTurnRect.Bottom + 6, actW - (_btnGuideRect.Right + 4 - rightX), 26);

            // 로그라이크 카드 3개 영역 (카드 폭 및 높이 확장으로 가독성 극대화)
            int cardW = 204;
            int cardH = 280;
            int totalCardW = cardW * 3 + 24 * 2;
            int cardStartX = (w - totalCardW) / 2;
            int cardY = (h - cardH) / 2 - 10;

            for (int i = 0; i < 3; i++)
            {
                _cardRects[i] = new Rectangle(cardStartX + i * (cardW + 24), cardY, cardW, cardH);
            }

            // 게임오버 모달 버튼 3종 (리플레이 / 다시하기 / 로비)
            int goY = h / 2 + 120;
            _btnReplayRect = new Rectangle(w / 2 - 180, goY, 130, 32);
            _gameOverRestartBtnRect = new Rectangle(_btnReplayRect.Right + 10, goY, 105, 32);
            _gameOverBackBtnRect = new Rectangle(_gameOverRestartBtnRect.Right + 10, goY, 75, 32);

            // 맵 사이즈 선택 모달 영역 (4대 전장)
            int mModalW = Math.Min(880, w - 40);
            int mModalH = 390;
            _mapSelectModalRect = new Rectangle((w - mModalW) / 2, (h - mModalH) / 2, mModalW, mModalH);

            int mCardW = (mModalW - 48 - 36) / 4;
            int mCardH = 260;
            int mCardY = _mapSelectModalRect.Y + 85;
            int mCardStartX = _mapSelectModalRect.X + 24;

            for (int i = 0; i < 4; i++)
            {
                _mapSelectCardRects[i] = new Rectangle(mCardStartX + i * (mCardW + 12), mCardY, mCardW, mCardH);
                _mapSelectBtnRects[i] = new Rectangle(_mapSelectCardRects[i].X + 10, _mapSelectCardRects[i].Bottom - 42, _mapSelectCardRects[i].Width - 20, 30);
            }

            // 리플레이 컨트롤러 HUD 영역 (타임라인 바 & 미디어 버튼 7종)
            int repBarY = _boardRect.Bottom + 12;
            int repBarH = 12;
            _repProgressBarRect = new Rectangle(_boardRect.X, repBarY, _boardRect.Width, repBarH);

            int repBtnY = repBarY + repBarH + 12;
            int repBtnH = 34;
            int repGap = 8;
            int totalRepBtnsW = 60 * 4 + 84 + 64 + 94 + repGap * 6; // 570px
            int repBtnStartX = (w - totalRepBtnsW) / 2;

            _repBtnFirstRect = new Rectangle(repBtnStartX, repBtnY, 60, repBtnH);
            _repBtnPrevRect = new Rectangle(_repBtnFirstRect.Right + repGap, repBtnY, 60, repBtnH);
            _repBtnPlayPauseRect = new Rectangle(_repBtnPrevRect.Right + repGap, repBtnY, 84, repBtnH);
            _repBtnNextRect = new Rectangle(_repBtnPlayPauseRect.Right + repGap, repBtnY, 60, repBtnH);
            _repBtnLastRect = new Rectangle(_repBtnNextRect.Right + repGap, repBtnY, 60, repBtnH);
            _repBtnSpeedRect = new Rectangle(_repBtnLastRect.Right + repGap, repBtnY, 64, repBtnH);
            _repBtnExitRect = new Rectangle(_repBtnSpeedRect.Right + repGap, repBtnY, 94, repBtnH);
        }

        private void AnimTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                _pulseAngle += 0.08f;
                if (_pulseAngle > (float)Math.PI * 2) _pulseAngle -= (float)Math.PI * 2;

                // 리플레이 실행 중 턴 자동 재생 (재생/정지 & 배속 제어)
                if (_isReplaying)
                {
                    if (!_isReplayPaused && _game.ReplaySnapshots.Count > 0)
                    {
                        _replayTickCounter++;
                        int ticksNeeded = Math.Max(4, 28 / _replaySpeed); // 1x = ~840ms, 2x = ~420ms, 4x = ~210ms
                        if (_replayTickCounter >= ticksNeeded)
                        {
                            _replayTickCounter = 0;
                            if (_replayIndex < _game.ReplaySnapshots.Count - 1)
                            {
                                _replayIndex++;
                                Invalidate();
                            }
                            else
                            {
                                _isReplayPaused = true;
                                Invalidate();
                            }
                        }
                    }
                    return;
                }

                bool needRedraw = false;
                int spdMult = Math.Max(1, _turnPlaySpeed);

                // 1. 유닛 슬로우 이동 보간 & 타격/피격 이펙트 감쇠 (배속 연동)
                float lerpRate = Math.Min(1.0f, 0.25f * spdMult);
                foreach (var u in _game.Units)
                {
                    if (u.VisualCol < 0) u.VisualCol = u.Col;

                    // 목표 칸으로 스르륵 전진 (Lerp)
                    if (Math.Abs(u.VisualCol - u.TargetCol) > 0.02f)
                    {
                        u.VisualCol += (u.TargetCol - u.VisualCol) * lerpRate;
                        needRedraw = true;
                    }
                    else
                    {
                        u.VisualCol = u.TargetCol;
                        u.Col = (int)Math.Round(u.TargetCol);
                    }

                    if (u.HitFlashAlpha > 0f)
                    {
                        u.HitFlashAlpha = Math.Max(0f, u.HitFlashAlpha - 0.08f * spdMult);
                        u.ShakeOffset *= -0.7f;
                        needRedraw = true;
                    }
                    if (Math.Abs(u.BumpOffset) > 0.1f)
                    {
                        u.BumpOffset *= 0.65f;
                        needRedraw = true;
                    }
                }

                // 건물 피격 플래시 감쇠
                for (int r = 0; r < _game.Rows; r++)
                {
                    for (int c = 0; c < _game.Cols; c++)
                    {
                        var b = _game.Buildings[r, c];
                        if (b != null)
                        {
                            if (b.HitFlashAlpha > 0f)
                            {
                                b.HitFlashAlpha = Math.Max(0f, b.HitFlashAlpha - 0.08f * spdMult);
                                b.ShakeOffset *= -0.7f;
                                needRedraw = true;
                            }
                        }
                    }
                }

                // 2. 포탑 레이저 빔 알파 감쇠
                for (int i = _laserBeams.Count - 1; i >= 0; i--)
                {
                    var beam = _laserBeams[i];
                    beam.Alpha -= 0.08f * spdMult;
                    if (beam.Alpha <= 0f) _laserBeams.RemoveAt(i);
                    needRedraw = true;
                }

                // 3. 플로팅 텍스트 & 충격파
                for (int i = _floatingTexts.Count - 1; i >= 0; i--)
                {
                    var ft = _floatingTexts[i];
                    ft.Y -= 0.9f * spdMult;
                    ft.Alpha -= 0.04f * spdMult;
                    if (ft.Alpha <= 0f) _floatingTexts.RemoveAt(i);
                    needRedraw = true;
                }

                for (int i = _shockwaves.Count - 1; i >= 0; i--)
                {
                    var sw = _shockwaves[i];
                    sw.Radius += 3.5f * spdMult;
                    sw.Alpha -= 0.05f * spdMult;
                    if (sw.Alpha <= 0f) _shockwaves.RemoveAt(i);
                    needRedraw = true;
                }

                // 4. 턴 시네마틱 애니메이션 시퀀스 실행 (배속 반영)
                if (_animPhase != TurnAnimPhase.Idle)
                {
                    _phaseTimer += 30 * spdMult;
                    UpdateTurnSequence();
                    needRedraw = true;
                }

                if (needRedraw || _game.IsCardSelectionActive || _hoverTile.X >= 1 || _game.PendingDropRemainingCount > 0)
                {
                    Invalidate();
                }
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "[CoreFront] AnimTimer_Tick exception");
            }
        }

        #region Turn Sequence State Machine (턴 시네마틱 진행)

        private void StartTurnSequence()
        {
            if (_game.IsInitialBasePlacementActive)
            {
                AddFloatingText("기지 설립을 먼저 완료해야 합니다!", _btnEndTurnRect.X - 50, _btnEndTurnRect.Y - 20, _colorCrimson);
                return;
            }
            if (_game.PendingDropRemainingCount > 0 && _game.PendingDropUnitType != UnitType.None)
            {
                AddFloatingText("공수 배치를 먼저 완료해야 합니다!", _btnEndTurnRect.X - 50, _btnEndTurnRect.Y - 20, _colorCrimson);
                return;
            }

            _game.RecordReplaySnapshot($"턴 {_game.TurnNumber} 출격 명령");
            _isTurnRunning = true;
            _animPhase = TurnAnimPhase.PlayerTurret;
            _phaseTimer = 0;

            // 1. 플레이어 포탑 발사!
            var evts = new List<CombatEvent>();
            var beams = _game.ExecuteTurretAttacks(Owner.Player, evts);
            _laserBeams.AddRange(beams);
            foreach (var ev in evts)
            {
                AddFloatingText(ev.Text, _boardRect.X + ev.Col * _tileW + 10, _boardRect.Y + ev.Row * _tileH, ev.TextColor);
            }
        }

        private void UpdateTurnSequence()
        {
            // Watchdog: 만약 알 수 없는 이유로 애니메이션 시퀀스가 3.5초(3500ms) 이상 지연되면 강제 복구
            if (_phaseTimer > 3500)
            {
                LogManager.LogMessage($"[CoreFront] Watchdog timeout triggered in phase {_animPhase} (timer={_phaseTimer}ms). Forcing EndTurnSequence.", LogManager.Level.Warning);
                EndTurnSequence();
                return;
            }

            switch (_animPhase)
            {
                case TurnAnimPhase.PlayerTurret:
                    if (_phaseTimer >= 280) // 0.28초 포탑 빔 감상 후
                    {
                        _animPhase = TurnAnimPhase.PlayerMarch;
                        _phaseTimer = 0;
                        // 2. 플레이어 유닛 전진 준비 (스르륵 슬라이드 시작)
                        _game.PrepareUnitMarchTargets(Owner.Player);
                    }
                    break;

                case TurnAnimPhase.PlayerMarch:
                    if (_phaseTimer >= 360) // 0.36초 동안 유닛 슬로우 전진 후
                    {
                        // 교전 해결 및 충돌 데미지 팝업
                        var evts = _game.ResolveUnitCombatAndCoreStrikes(Owner.Player);
                        foreach (var ev in evts)
                        {
                            AddFloatingText(ev.Text, _boardRect.X + ev.Col * _tileW + 10, _boardRect.Y + ev.Row * _tileH, ev.TextColor);
                        }

                        if (_game.IsGameOver)
                        {
                            EndTurnSequence();
                            return;
                        }

                        _animPhase = TurnAnimPhase.AiTurret;
                        _phaseTimer = 0;

                        // 3. AI 포탑 발사!
                        var aiEvts = new List<CombatEvent>();
                        var aiBeams = _game.ExecuteTurretAttacks(Owner.Ai, aiEvts);
                        _laserBeams.AddRange(aiBeams);
                        foreach (var ev in aiEvts)
                        {
                            AddFloatingText(ev.Text, _boardRect.X + ev.Col * _tileW + 10, _boardRect.Y + ev.Row * _tileH, ev.TextColor);
                        }
                    }
                    break;

                case TurnAnimPhase.AiTurret:
                    if (_phaseTimer >= 280)
                    {
                        _animPhase = TurnAnimPhase.AiMarch;
                        _phaseTimer = 0;
                        // 4. AI 유닛 전진 준비 (스르륵 슬라이드 시작)
                        _game.PrepareUnitMarchTargets(Owner.Ai);
                    }
                    break;

                case TurnAnimPhase.AiMarch:
                    if (_phaseTimer >= 360)
                    {
                        var aiCombatEvts = _game.ResolveUnitCombatAndCoreStrikes(Owner.Ai);
                        foreach (var ev in aiCombatEvts)
                        {
                            AddFloatingText(ev.Text, _boardRect.X + ev.Col * _tileW + 10, _boardRect.Y + ev.Row * _tileH, ev.TextColor);
                        }

                        if (_game.IsGameOver)
                        {
                            EndTurnSequence();
                            return;
                        }

                        _animPhase = TurnAnimPhase.AiDecision;
                        _phaseTimer = 0;

                        // 5. AI 지능형 건설 & 생산 & 전술 스킬
                        _game.ExecuteAiDecisions();

                        // AI 전술 스킬 및 이벤트 플러시 (시각적 피드백 연동)
                        if (_game.PendingAiLaserBeams.Count > 0)
                        {
                            _laserBeams.AddRange(_game.PendingAiLaserBeams);
                            _game.PendingAiLaserBeams.Clear();
                        }
                        if (_game.PendingAiCombatEvents.Count > 0)
                        {
                            foreach (var ev in _game.PendingAiCombatEvents)
                            {
                                int evX = _boardRect.X + ev.Col * _tileW + _tileW / 2;
                                int evY = _boardRect.Y + ev.Row * _tileH + _tileH / 2;
                                AddFloatingText(ev.Text, evX - 35, evY - 20, ev.TextColor);
                                AddShockwave(evX, evY, ev.TextColor, 40f);
                            }
                            _game.PendingAiCombatEvents.Clear();
                        }
                    }
                    break;

                case TurnAnimPhase.AiDecision:
                    if (_phaseTimer >= 220)
                    {
                        EndTurnSequence();
                    }
                    break;
            }
        }

        private void EndTurnSequence()
        {
            _animPhase = TurnAnimPhase.Idle;
            _isTurnRunning = false;

            if (_game.IsGameOver)
            {
                var newly = _game.EvaluateCoreFrontAchievements(_game.Winner == Owner.Player);
                foreach (var title in newly)
                {
                    ToastNotification.Show($"[도전과제 달성!]\n{title}", "🏆 CORE FRONT", ToastIcon.Success);
                }
                Invalidate();
                return;
            }

            // 실시간 중간 도전과제 달성 확인 (600G 수급, 전진 교두보 등)
            var midAchievements = _game.EvaluateCoreFrontAchievements(false);
            foreach (var title in midAchievements)
            {
                ToastNotification.Show($"[도전과제 달성!]\n{title}", "🏆 CORE FRONT", ToastIcon.Success);
            }

            // 턴 번호 증가 & 플레이어 수입 지급
            _game.TurnNumber++;
            _game.PlayerCredit += _game.CalculateIncome(Owner.Player);
            _game.AiCredit += _game.CalculateIncome(Owner.Ai);

            // 건물 자체 수리 (2턴당 1회복) 및 EMP 무력화 턴수 차감
            var regenEvts = _game.ProcessTurnStartBuildingRegenAndEmp();
            foreach (var rev in regenEvts)
            {
                AddFloatingText(rev.Text, _boardRect.X + rev.Col * _tileW + 6, _boardRect.Y + rev.Row * _tileH - 12, rev.TextColor);
            }

            // 3턴 주기 로그라이크 카드 이벤트 체크
            if (_game.TurnNumber % 3 == 0)
            {
                _game.TriggerRoguelikeCardSelection();
            }
            else
            {
                _game.StatusMessage = $"턴 {_game.TurnNumber} - 자원 수급 (+{_game.CalculateIncome(Owner.Player)}G). 전진 명령을 하달하세요.";
            }

            _game.RecordReplaySnapshot($"턴 {_game.TurnNumber - 1} 교전 완료");

            AddFloatingText("TURN COMPLETED", this.Width / 2 - 50, _boardRect.Y + 140, _colorGold);
            Invalidate();
        }

        public void SkipTurnSequence()
        {
            if (!_isTurnRunning) return;

            // 남은 턴 시네마틱 페이즈들을 즉시 한 번에 완결 처리 (Fast-Forward / Skip)
            int safetyLoop = 0;
            while (_isTurnRunning && _animPhase != TurnAnimPhase.Idle && safetyLoop < 10)
            {
                safetyLoop++;
                _phaseTimer = 5000; // 페이즈 타임아웃 강제 도달로 즉시 다음 페이즈 전진
                UpdateTurnSequence();
                if (_game.IsGameOver) break;
            }

            // 모든 유닛의 표시 좌표를 최종 계산 좌표로 즉시 정렬
            foreach (var u in _game.Units)
            {
                u.VisualCol = u.Col;
            }
            _laserBeams.Clear();
            Invalidate();
        }

        #endregion

        #region Mouse Interaction

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            int prevBtn = _hoverButtonId;
            Point prevTile = _hoverTile;
            int prevCard = _hoverCardIndex;

            _hoverButtonId = -1;
            _hoverTile = new Point(-1, -1);
            _hoverCardIndex = -1;

            if (_isReplaying)
            {
                int prevReplayHover = _hoverReplayBtnId;
                _hoverReplayBtnId = -1;
                if (_repBtnFirstRect.Contains(e.Location)) _hoverReplayBtnId = 1;
                else if (_repBtnPrevRect.Contains(e.Location)) _hoverReplayBtnId = 2;
                else if (_repBtnPlayPauseRect.Contains(e.Location)) _hoverReplayBtnId = 3;
                else if (_repBtnNextRect.Contains(e.Location)) _hoverReplayBtnId = 4;
                else if (_repBtnLastRect.Contains(e.Location)) _hoverReplayBtnId = 5;
                else if (_repBtnSpeedRect.Contains(e.Location)) _hoverReplayBtnId = 6;
                else if (_repBtnExitRect.Contains(e.Location)) _hoverReplayBtnId = 7;
                else if (_repProgressBarRect.Contains(e.Location)) _hoverReplayBtnId = 8;

                Cursor = (_hoverReplayBtnId > 0) ? Cursors.Hand : Cursors.Default;
                if (prevReplayHover != _hoverReplayBtnId) Invalidate();
                return;
            }

            if (_isMapSizeSelectActive)
            {
                int prevMapIdx = _hoverMapSelectIndex;
                _hoverMapSelectIndex = -1;
                for (int i = 0; i < 4; i++)
                {
                    if (_mapSelectCardRects[i].Contains(e.Location))
                    {
                        _hoverMapSelectIndex = i;
                        break;
                    }
                }
                Cursor = (_hoverMapSelectIndex >= 0) ? Cursors.Hand : Cursors.Default;
                if (prevMapIdx != _hoverMapSelectIndex) Invalidate();
                return;
            }

            if (_game.IsGameOver)
            {
                if (_btnReplayRect.Contains(e.Location)) _hoverButtonId = 97;
                else if (_gameOverRestartBtnRect.Contains(e.Location)) _hoverButtonId = 99;
                else if (_gameOverBackBtnRect.Contains(e.Location)) _hoverButtonId = 98;
                Cursor = (_hoverButtonId >= 0) ? Cursors.Hand : Cursors.Default;
                if (prevBtn != _hoverButtonId) Invalidate();
                return;
            }

            if (_showGuide)
            {
                Cursor = Cursors.Hand;
                return;
            }

            if (_game.IsCardSelectionActive)
            {
                for (int i = 0; i < _cardRects.Length; i++)
                {
                    if (_cardRects[i].Contains(e.Location))
                    {
                        _hoverCardIndex = i;
                        break;
                    }
                }
                Cursor = (_hoverCardIndex >= 0) ? Cursors.Hand : Cursors.Default;
                if (prevCard != _hoverCardIndex) Invalidate();
                return;
            }

            // 헤더 뒤로가기 & 맵 사이즈 선택 토글 & 진행 배속 토글
            if (_btnBackRect.Contains(e.Location)) _hoverButtonId = 0;
            else if (_btnMapSmallRect.Contains(e.Location)) _hoverButtonId = 91;
            else if (_btnMapStdRect.Contains(e.Location)) _hoverButtonId = 92;
            else if (_btnMapLargeRect.Contains(e.Location)) _hoverButtonId = 93;
            else if (_btnMapHugeRect.Contains(e.Location)) _hoverButtonId = 94;
            else if (_btnTurnSpeedRect.Contains(e.Location)) _hoverButtonId = 96;
            // 카테고리 탭 (101, 102, 103)
            else if (_tabBuildingsRect.Contains(e.Location)) _hoverButtonId = 101;
            else if (_tabUnitsRect.Contains(e.Location)) _hoverButtonId = 102;
            else if (_tabSkillsRect.Contains(e.Location)) _hoverButtonId = 103;
            // 발전소 한도 증설 전용 버튼 (95)
            else if (_currentCategory == BottomCategory.Buildings && _btnExpandGridRect.Contains(e.Location)) _hoverButtonId = 95;
            // 하단 액션 슬롯 (1~6)
            else if (_slotRects[0].Contains(e.Location)) _hoverButtonId = 1;
            else if (_slotRects[1].Contains(e.Location)) _hoverButtonId = 2;
            else if (_slotRects[2].Contains(e.Location)) _hoverButtonId = 3;
            else if (_slotRects[3].Contains(e.Location)) _hoverButtonId = 4;
            else if (_slotRects[4].Contains(e.Location)) _hoverButtonId = 5;
            else if (_slotRects[5].Contains(e.Location) && _currentCategory != BottomCategory.Skills) _hoverButtonId = 6;
            // 우측 액션 버튼 및 헤더 도전과제
            else if (_btnEndTurnRect.Contains(e.Location)) _hoverButtonId = 8;
            else if (_btnRestartRect.Contains(e.Location)) _hoverButtonId = 9;
            else if (_btnGuideRect.Contains(e.Location)) _hoverButtonId = 10;
            else if (_btnAchievementsRect.Contains(e.Location) || _btnAchievementsBottomRect.Contains(e.Location)) _hoverButtonId = 11;

            // 보드 타일 (Col 1~10만 상호작용 가능)
            if (_boardRect.Contains(e.Location))
            {
                int c = (e.X - _boardRect.X) / _tileW;
                int r = (e.Y - _boardRect.Y) / _tileH;
                if (c >= 1 && c < _game.Cols - 1 && r >= 0 && r < _game.Rows)
                {
                    _hoverTile = new Point(c, r);
                }
            }

            // 전투 로그 배너 호버 여부
            bool prevCombatLogHover = _hoverOnCombatLog;
            _hoverOnCombatLog = _combatLogRect.Contains(e.Location);

            Cursor = (_hoverButtonId >= 0 || _hoverTile.X >= 0 || _hoverOnCombatLog) ? Cursors.Hand : Cursors.Default;

            if (prevBtn != _hoverButtonId || prevTile != _hoverTile || prevCombatLogHover != _hoverOnCombatLog)
            {
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoverButtonId = -1;
            _hoverTile = new Point(-1, -1);
            _hoverOnCombatLog = false;
            Invalidate();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            this.Focus();

            // 우클릭 시 현재 선택된 건설/소환 모드 즉시 취소
            if (e.Button == MouseButtons.Right)
            {
                if (_game.PendingDropRemainingCount > 0 && _game.PendingDropUnitType != UnitType.None)
                {
                    AddFloatingText($"전술 배치를 완료해야 합니다! (남은 유닛: {_game.PendingDropRemainingCount}기)", e.X - 80, e.Y - 15, _colorGold);
                    return;
                }

                if (_selectedBuilding != BuildingType.None || _selectedUnit != UnitType.None || _selectedSkill != UserSkillType.None)
                {
                    _selectedBuilding = BuildingType.None;
                    _selectedUnit = UnitType.None;
                    _selectedSkill = UserSkillType.None;
                    Invalidate();
                }
                return;
            }

            if (e.Button != MouseButtons.Left) return;

            // 0.0 맵 사이즈 선택 모달 클릭 (신규!)
            if (_isMapSizeSelectActive)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (_mapSelectCardRects[i].Contains(e.Location))
                    {
                        MapSizeType chosen = (MapSizeType)i;
                        SelectMapSizeAndStart(chosen);
                        return;
                    }
                }
                return;
            }

            // 0. 리플레이 뷰어 모드 클릭 조작
            if (_isReplaying)
            {
                if (_repBtnFirstRect.Contains(e.Location))
                {
                    _replayIndex = 0;
                    _isReplayPaused = true;
                    Invalidate();
                    return;
                }
                if (_repBtnPrevRect.Contains(e.Location))
                {
                    _replayIndex = Math.Max(0, _replayIndex - 1);
                    _isReplayPaused = true;
                    Invalidate();
                    return;
                }
                if (_repBtnPlayPauseRect.Contains(e.Location))
                {
                    if (_isReplayPaused && _replayIndex >= _game.ReplaySnapshots.Count - 1)
                    {
                        _replayIndex = 0;
                    }
                    _isReplayPaused = !_isReplayPaused;
                    Invalidate();
                    return;
                }
                if (_repBtnNextRect.Contains(e.Location))
                {
                    _replayIndex = Math.Min(_game.ReplaySnapshots.Count - 1, _replayIndex + 1);
                    _isReplayPaused = true;
                    Invalidate();
                    return;
                }
                if (_repBtnLastRect.Contains(e.Location))
                {
                    _replayIndex = _game.ReplaySnapshots.Count - 1;
                    _isReplayPaused = true;
                    Invalidate();
                    return;
                }
                if (_repBtnSpeedRect.Contains(e.Location))
                {
                    _replaySpeed = (_replaySpeed == 1) ? 2 : (_replaySpeed == 2 ? 4 : 1);
                    Invalidate();
                    return;
                }
                if (_repBtnExitRect.Contains(e.Location))
                {
                    _isReplaying = false;
                    Invalidate();
                    return;
                }
                if (_repProgressBarRect.Contains(e.Location) && _game.ReplaySnapshots.Count > 1)
                {
                    float ratio = (float)(e.X - _repProgressBarRect.X) / _repProgressBarRect.Width;
                    ratio = Math.Max(0f, Math.Min(1f, ratio));
                    _replayIndex = (int)Math.Round(ratio * (_game.ReplaySnapshots.Count - 1));
                    _isReplayPaused = true;
                    Invalidate();
                    return;
                }
                return;
            }

            // 1. 게임 종료 상태 클릭
            if (_game.IsGameOver)
            {
                if (_btnReplayRect.Contains(e.Location))
                {
                    StartReplay();
                    return;
                }
                if (_gameOverRestartBtnRect.Contains(e.Location))
                {
                    PromptMapSelection();
                    return;
                }
                if (_gameOverBackBtnRect.Contains(e.Location))
                {
                    if (this.ParentForm is SandBox sb) sb.BackToList();
                    return;
                }
                return;
            }

            // 2. 룰 가이드 및 도전과제 모달 닫기
            if (_showAchievements)
            {
                _showAchievements = false;
                Invalidate();
                return;
            }
            if (_showGuide)
            {
                _showGuide = false;
                Invalidate();
                return;
            }

            // 3. 로그라이크 카드 선택
            if (_game.IsCardSelectionActive)
            {
                for (int i = 0; i < _game.CurrentOfferedCards.Count && i < 3; i++)
                {
                    if (_cardRects[i].Contains(e.Location))
                    {
                        var chosenCard = _game.CurrentOfferedCards[i];
                        _game.SelectProtocolCard(chosenCard);
                        AddShockwave(this.Width / 2, this.Height / 2, chosenCard.AccentColor, 120f);
                        AddFloatingText("PROTOCOL ACTIVATED!", this.Width / 2 - 80, this.Height / 2 - 20, chosenCard.AccentColor);

                        // 💥 궤도 EMP 폭격 카드 선택 시 전장 전체 폭격 쇼크웨이브 및 실시간 타격 피드백!
                        if (chosenCard.Id == "ORBITAL_EMP")
                        {
                            foreach (var enemy in _game.Units.FindAll(u => u.Owner == Owner.Ai))
                            {
                                int ex = _boardRect.X + enemy.Col * _tileW + _tileW / 2;
                                int ey = _boardRect.Y + enemy.Row * _tileH + _tileH / 2;
                                AddShockwave(ex, ey, Color.FromArgb(255, 60, 60), 40f);
                                AddFloatingText("-2 EMP! 💥", ex - 25, ey - 22, Color.FromArgb(255, 60, 60));
                            }
                        }

                        Invalidate();
                        return;
                    }
                }
                return;
            }

            // 턴 애니메이션 진행 중에는 버튼 및 착수 클릭 차단
            if (_isTurnRunning) return;

            // 4. 로비로 뒤로가기 & 맵 사이즈 선택 토글 & 진행 배속 토글
            if (_btnBackRect.Contains(e.Location))
            {
                if (this.ParentForm is SandBox sb) sb.BackToList();
                return;
            }
            if (_btnMapSmallRect.Contains(e.Location))
            {
                RequestMapSizeChange(MapSizeType.Small);
                return;
            }
            if (_btnMapStdRect.Contains(e.Location))
            {
                RequestMapSizeChange(MapSizeType.Standard);
                return;
            }
            if (_btnMapLargeRect.Contains(e.Location))
            {
                RequestMapSizeChange(MapSizeType.Large);
                return;
            }
            if (_btnMapHugeRect.Contains(e.Location))
            {
                RequestMapSizeChange(MapSizeType.Huge);
                return;
            }
            if (_btnTurnSpeedRect.Contains(e.Location))
            {
                _turnPlaySpeed = (_turnPlaySpeed == 1) ? 2 : (_turnPlaySpeed == 2) ? 4 : 1;
                AddFloatingText($"진행 배속: {_turnPlaySpeed}x", _btnTurnSpeedRect.X - 10, _btnTurnSpeedRect.Bottom + 6, (_turnPlaySpeed == 4) ? Color.FromArgb(255, 60, 200) : _colorCyan);
                Invalidate();
                return;
            }
            if (_btnAchievementsRect.Contains(e.Location) || _btnAchievementsBottomRect.Contains(e.Location))
            {
                _showAchievements = true;
                Invalidate();
                return;
            }
            if (_btnGuideRect.Contains(e.Location))
            {
                _showGuide = true;
                Invalidate();
                return;
            }
            if (_btnRestartRect.Contains(e.Location))
            {
                PromptMapSelection();
                return;
            }

            // [최초 1턴 본진 발전소 무료 배치 클릭 최우선 처리]
            if (_game.IsInitialBasePlacementActive)
            {
                if (_boardRect.Contains(e.Location))
                {
                    int c = (e.X - _boardRect.X) / _tileW;
                    int r = (e.Y - _boardRect.Y) / _tileH;
                    if (c >= 1 && c <= 2 && r >= 0 && r < _game.Rows)
                    {
                        if (_game.PlaceInitialPlayerBase(c, r))
                        {
                            int screenX = _boardRect.X + c * _tileW + _tileW / 2;
                            int screenY = _boardRect.Y + r * _tileH + _tileH / 2;
                            AddShockwave(screenX, screenY, _colorGold, 45f);
                            string plantMsg = (_game.InitialBaseTotalCount > 1)
                                ? $"발전소 ({_game.InitialBaseTotalCount - _game.InitialBaseRemainingCount}/{_game.InitialBaseTotalCount}) 설립! ⚡"
                                : "본진 발전소 설립! ⚡";
                            AddFloatingText(plantMsg, screenX - 45, screenY - 20, _colorGold);
                            Invalidate();
                            return;
                        }
                        else
                        {
                            AddFloatingText("배치 불가 타일", e.X - 30, e.Y - 15, _colorCrimson);
                            return;
                        }
                    }
                    else
                    {
                        AddFloatingText("Col 1~2 구역에 기지를 설립하세요!", e.X - 60, e.Y - 15, _colorCrimson);
                        return;
                    }
                }
                return;
            }

            // [전술 투하 대기 상태 시 보드 클릭 최우선 처리]
            if (_game.PendingDropRemainingCount > 0 && _game.PendingDropUnitType != UnitType.None)
            {
                if (_boardRect.Contains(e.Location))
                {
                    int c = (e.X - _boardRect.X) / _tileW;
                    int r = (e.Y - _boardRect.Y) / _tileH;

                    if (c >= _game.PendingDropMinCol && c <= _game.PendingDropMaxCol && r >= 0 && r < _game.Rows)
                    {
                        if (_game.MapTiles[r, c] != TileType.Obstacle &&
                            _game.Buildings[r, c]?.Type != BuildingType.Wall &&
                            !_game.Units.Exists(u => u.Col == c && u.Row == r))
                        {
                            _game.SpawnUnitDirect(c, r, _game.PendingDropUnitType, Owner.Player);
                            int screenX = _boardRect.X + c * _tileW + _tileW / 2;
                            int screenY = _boardRect.Y + r * _tileH + _tileH / 2;
                            AddShockwave(screenX, screenY, _colorCyan, 45f);
                            AddFloatingText("전술 공수 투하! 🚀", screenX - 35, screenY - 20, _colorCyan);

                            _game.PendingDropRemainingCount--;
                            if (_game.PendingDropRemainingCount <= 0)
                            {
                                _game.PendingDropUnitType = UnitType.None;
                                _game.PendingDropCustomHp = 0;
                                _game.PendingDropCustomAtk = 0;
                                _game.PendingDropForceShield = false;
                                _game.StatusMessage = "전술 유닛 배치가 완료되었습니다.";
                            }
                            else
                            {
                                _game.StatusMessage = $"남은 유닛: {_game.PendingDropRemainingCount}기 - 다음 위치를 클릭하세요.";
                            }
                            Invalidate();
                            return;
                        }
                        else
                        {
                            AddFloatingText("배치 불가 타일", e.X - 30, e.Y - 15, _colorCrimson);
                            return;
                        }
                    }
                    else
                    {
                        AddFloatingText($"유효 구역: Col {_game.PendingDropMinCol}~{_game.PendingDropMaxCol}", e.X - 45, e.Y - 15, _colorCrimson);
                        return;
                    }
                }
                else
                {
                    AddFloatingText($"먼저 유닛 배치를 완료하십시오! (남은 유닛: {_game.PendingDropRemainingCount}기)", e.X - 80, e.Y - 15, _colorGold);
                    return;
                }
            }

            // 5. 카테고리 탭 클릭 전환
            if (_tabBuildingsRect.Contains(e.Location))
            {
                _currentCategory = BottomCategory.Buildings;
                _selectedBuilding = BuildingType.None;
                _selectedUnit = UnitType.None;
                _selectedSkill = UserSkillType.None;
                Invalidate();
                return;
            }
            if (_tabUnitsRect.Contains(e.Location))
            {
                _currentCategory = BottomCategory.Units;
                _selectedBuilding = BuildingType.None;
                _selectedUnit = UnitType.None;
                _selectedSkill = UserSkillType.None;
                Invalidate();
                return;
            }
            if (_tabSkillsRect.Contains(e.Location))
            {
                _currentCategory = BottomCategory.Skills;
                _selectedBuilding = BuildingType.None;
                _selectedUnit = UnitType.None;
                _selectedSkill = UserSkillType.None;
                Invalidate();
                return;
            }

            // 인프라 탭: 발전소(슬롯 0) 바로 아래의 한도증설 세로 버튼 클릭
            if (_currentCategory == BottomCategory.Buildings && _btnExpandGridRect.Contains(e.Location))
            {
                ExecuteExpandGrid();
                Invalidate();
                return;
            }

            // 6. 하단 액션 슬롯 클릭
            for (int i = 0; i < 6; i++)
            {
                if (_slotRects[i].Contains(e.Location))
                {
                    if (_currentCategory == BottomCategory.Skills && i == 5) return;
                    ExecuteSlotAction(i);
                    Invalidate();
                    return;
                }
            }

            // 7. 턴 종료 버튼! (슬로우 시네마틱 턴 시작 또는 진행 중 클릭 시 즉시 스킵)
            if (_btnEndTurnRect.Contains(e.Location))
            {
                if (_isTurnRunning)
                {
                    SkipTurnSequence();
                    return;
                }
                _selectedBuilding = BuildingType.None;
                _selectedUnit = UnitType.None;
                StartTurnSequence();
                Invalidate();
                return;
            }

            // 8. 리셋, 룰 및 도전과제
            if (_btnRestartRect.Contains(e.Location))
            {
                PromptMapSelection();
                return;
            }
            if (_btnGuideRect.Contains(e.Location))
            {
                _showGuide = true;
                Invalidate();
                return;
            }
            if (_btnAchievementsRect.Contains(e.Location) || _btnAchievementsBottomRect.Contains(e.Location))
            {
                _showAchievements = true;
                Invalidate();
                return;
            }

            // 9. 보드 타일 클릭 (건설 또는 유닛 스폰)
            if (_boardRect.Contains(e.Location))
            {
                int c = (e.X - _boardRect.X) / _tileW;
                int r = (e.Y - _boardRect.Y) / _tileH;

                if (c >= 1 && c < _game.Cols - 1 && r >= 0 && r < _game.Rows)
                {
                    int screenX = _boardRect.X + c * _tileW + _tileW / 2;
                    int screenY = _boardRect.Y + r * _tileH + _tileH / 2;

                    // 유저 스킬 시전 처리
                    if (_selectedSkill != UserSkillType.None)
                    {
                        if (_game.CastSkill(_selectedSkill, c, r, Owner.Player, out var sEvents, out var sBeam))
                        {
                            if (sBeam != null) _laserBeams.Add(sBeam);
                            foreach (var ev in sEvents)
                            {
                                int evX = _boardRect.X + ev.Col * _tileW + _tileW / 2;
                                int evY = _boardRect.Y + ev.Row * _tileH + _tileH / 2;
                                AddFloatingText(ev.Text, evX - 35, evY - 20, ev.TextColor);
                            }
                            Color waveCol = (_selectedSkill == UserSkillType.HealingBeam) ? Color.FromArgb(0, 230, 118) :
                                            (_selectedSkill == UserSkillType.FortifyBeam) ? Color.FromArgb(0, 229, 255) : _colorCrimson;
                            AddShockwave(screenX, screenY, waveCol, 50f);
                            _selectedSkill = UserSkillType.None;
                        }
                        else
                        {
                            AddFloatingText("스킬 발동 실패 (자금/대상 확인)", screenX - 55, screenY - 15, _colorCrimson);
                        }
                        Invalidate();
                        return;
                    }

                    if (_selectedBuilding != BuildingType.None)
                    {
                        if (_selectedBuilding == BuildingType.PowerPlant && _game.GetBuildingCount(BuildingType.PowerPlant, Owner.Player) >= _game.PlayerPlantLimit)
                        {
                            AddFloatingText($"발전소 한도({_game.PlayerPlantLimit}기) 도달 - [한도 증설] 필요", screenX - 60, screenY - 15, _colorGold);
                            Invalidate();
                            return;
                        }

                        bool built = _game.Build(c, r, _selectedBuilding, Owner.Player);
                        if (built)
                        {
                            AddShockwave(screenX, screenY, _colorCyan, 40f);
                            AddFloatingText("CONSTRUCTED", screenX - 30, screenY - 15, _colorEmerald);
                        }
                        else
                        {
                            AddFloatingText("건설 불가", screenX - 20, screenY - 15, _colorCrimson);
                        }
                        Invalidate();
                        return;
                    }

                    if (_selectedUnit != UnitType.None)
                    {
                        bool spawned = _game.SpawnUnit(c, r, _selectedUnit, Owner.Player);
                        if (spawned)
                        {
                            AddShockwave(screenX, screenY, _colorCyan, 40f);
                            AddFloatingText("DEPLOYED", screenX - 25, screenY - 15, _colorCyan);
                        }
                        else
                        {
                            AddFloatingText("소환 불가", screenX - 20, screenY - 15, _colorCrimson);
                        }
                        Invalidate();
                        return;
                    }
                }
            }
        }

        private void ExecuteSlotAction(int slotIndex)
        {
            if (_currentCategory == BottomCategory.Buildings)
            {
                _selectedSkill = UserSkillType.None;
                switch (slotIndex)
                {
                    case 0: // [1] 발전소 순수 선택 (실수로 돈 차감 원천 차단!)
                        int currentPlantCount = _game.GetBuildingCount(BuildingType.PowerPlant, Owner.Player);
                        if (currentPlantCount >= _game.PlayerPlantLimit)
                        {
                            int cx = _slotRects[0].X + _slotRects[0].Width / 2;
                            int cy = _slotRects[0].Y + _slotRects[0].Height / 2;
                            AddFloatingText($"발전소 한도({_game.PlayerPlantLimit}기) 도달! [5] 한도증설 필요", cx - 75, cy - 20, _colorCrimson);
                            _selectedBuilding = BuildingType.None;
                            _selectedUnit = UnitType.None;
                        }
                        else
                        {
                            _selectedBuilding = (_selectedBuilding == BuildingType.PowerPlant) ? BuildingType.None : BuildingType.PowerPlant;
                            _selectedUnit = UnitType.None;
                        }
                        break;
                    case 1: // [2] 포탑
                        _selectedBuilding = (_selectedBuilding == BuildingType.Turret) ? BuildingType.None : BuildingType.Turret;
                        _selectedUnit = UnitType.None;
                        break;
                    case 2: // [3] 격벽
                        _selectedBuilding = (_selectedBuilding == BuildingType.Wall) ? BuildingType.None : BuildingType.Wall;
                        _selectedUnit = UnitType.None;
                        break;
                    case 3: // [4] 공장
                        _selectedBuilding = (_selectedBuilding == BuildingType.Factory) ? BuildingType.None : BuildingType.Factory;
                        _selectedUnit = UnitType.None;
                        break;
                    case 4: // [5] 수리 격납고 (6G, 턴당 부상 아군 1기 +1 수리)
                        _selectedBuilding = (_selectedBuilding == BuildingType.RepairDepot) ? BuildingType.None : BuildingType.RepairDepot;
                        _selectedUnit = UnitType.None;
                        break;
                    case 5: // [6] 가속 관문 (5G, 통과 아군 이동력 +1)
                        _selectedBuilding = (_selectedBuilding == BuildingType.PhaseGate) ? BuildingType.None : BuildingType.PhaseGate;
                        _selectedUnit = UnitType.None;
                        break;
                }
            }
            else if (_currentCategory == BottomCategory.Units)
            {
                _selectedSkill = UserSkillType.None;
                switch (slotIndex)
                {
                    case 0: // [1] 보병
                        _selectedUnit = (_selectedUnit == UnitType.Vanguard) ? UnitType.None : UnitType.Vanguard;
                        _selectedBuilding = BuildingType.None;
                        break;
                    case 1: // [2] 침투병
                        _selectedUnit = (_selectedUnit == UnitType.Raider) ? UnitType.None : UnitType.Raider;
                        _selectedBuilding = BuildingType.None;
                        break;
                    case 2: // [3] 타이탄
                        _selectedUnit = (_selectedUnit == UnitType.Titan) ? UnitType.None : UnitType.Titan;
                        _selectedBuilding = BuildingType.None;
                        break;
                    case 3: // [4] 천궁 (12G)
                        _selectedUnit = (_selectedUnit == UnitType.Cheongung) ? UnitType.None : UnitType.Cheongung;
                        _selectedBuilding = BuildingType.None;
                        break;
                    case 4: // [5] 콜로서스 (14G)
                        _selectedUnit = (_selectedUnit == UnitType.Colossus) ? UnitType.None : UnitType.Colossus;
                        _selectedBuilding = BuildingType.None;
                        break;
                    case 5: // [6] 드레드노트 (18G)
                        _selectedUnit = (_selectedUnit == UnitType.Dreadnought) ? UnitType.None : UnitType.Dreadnought;
                        _selectedBuilding = BuildingType.None;
                        break;
                }
            }
            else // BottomCategory.Skills
            {
                _selectedBuilding = BuildingType.None;
                _selectedUnit = UnitType.None;
                switch (slotIndex)
                {
                    case 0: // [1] 정밀 궤도 폭격 (타깃 지정)
                        int obCost = _game.GetSkillCost(UserSkillType.OrbitalStrike);
                        if (_game.PlayerCredit < obCost)
                        {
                            int cx = _slotRects[0].X + _slotRects[0].Width / 2;
                            int cy = _slotRects[0].Y + _slotRects[0].Height / 2;
                            AddFloatingText($"크레딧 부족 ({obCost}G 필요)", cx - 40, cy - 20, _colorCrimson);
                            _selectedSkill = UserSkillType.None;
                        }
                        else
                        {
                            _selectedSkill = (_selectedSkill == UserSkillType.OrbitalStrike) ? UserSkillType.None : UserSkillType.OrbitalStrike;
                        }
                        break;
                    case 1: // [2] 전자기 EMP 교란 (타깃 지정)
                        int empCost = _game.GetSkillCost(UserSkillType.EmpJammer);
                        if (_game.PlayerCredit < empCost)
                        {
                            int cx = _slotRects[1].X + _slotRects[1].Width / 2;
                            int cy = _slotRects[1].Y + _slotRects[1].Height / 2;
                            AddFloatingText($"크레딧 부족 ({empCost}G 필요)", cx - 40, cy - 20, _colorCrimson);
                            _selectedSkill = UserSkillType.None;
                        }
                        else
                        {
                            _selectedSkill = (_selectedSkill == UserSkillType.EmpJammer) ? UserSkillType.None : UserSkillType.EmpJammer;
                        }
                        break;
                    case 2: // [3] 전술 치료 광선 (4G, 지정 행 유닛+건물 +6HP 수복)
                        int hlCost = _game.GetSkillCost(UserSkillType.HealingBeam);
                        if (_game.PlayerCredit < hlCost)
                        {
                            int cx = _slotRects[2].X + _slotRects[2].Width / 2;
                            int cy = _slotRects[2].Y + _slotRects[2].Height / 2;
                            AddFloatingText($"크레딧 부족 ({hlCost}G 필요)", cx - 40, cy - 20, _colorCrimson);
                            _selectedSkill = UserSkillType.None;
                        }
                        else
                        {
                            _selectedSkill = (_selectedSkill == UserSkillType.HealingBeam) ? UserSkillType.None : UserSkillType.HealingBeam;
                        }
                        break;
                    case 3: // [4] 전술 구축 광선 (5G, 지정 행 유닛 내구도+2)
                        int ftCost = _game.GetSkillCost(UserSkillType.FortifyBeam);
                        if (_game.PlayerCredit < ftCost)
                        {
                            int cx = _slotRects[3].X + _slotRects[3].Width / 2;
                            int cy = _slotRects[3].Y + _slotRects[3].Height / 2;
                            AddFloatingText($"크레딧 부족 ({ftCost}G 필요)", cx - 40, cy - 20, _colorCrimson);
                            _selectedSkill = UserSkillType.None;
                        }
                        else
                        {
                            _selectedSkill = (_selectedSkill == UserSkillType.FortifyBeam) ? UserSkillType.None : UserSkillType.FortifyBeam;
                        }
                        break;
                    case 4: // [5] 전장 정찰 스캔 (3G, 즉발 은폐 침투병 해제)
                        int scCost = _game.GetSkillCost(UserSkillType.ReconScan);
                        if (_game.CastSkill(UserSkillType.ReconScan, 0, 0))
                        {
                            int cx = _slotRects[4].X + _slotRects[4].Width / 2;
                            int cy = _slotRects[4].Y + _slotRects[4].Height / 2;
                            AddShockwave(this.Width / 2, _boardRect.Y + 140, _colorGold, 100f);
                            AddFloatingText("전장 정찰 스캔 완료! 모든 은폐 해제 📡", this.Width / 2 - 110, _boardRect.Y + 120, _colorGold);
                        }
                        else
                        {
                            int cx = _slotRects[4].X + _slotRects[4].Width / 2;
                            int cy = _slotRects[4].Y + _slotRects[4].Height / 2;
                            AddFloatingText($"크레딧 부족 ({scCost}G 필요)", cx - 40, cy - 20, _colorCrimson);
                        }
                        _selectedSkill = UserSkillType.None;
                        break;
                    case 5: // [6] 미사용
                        break;
                }
            }
        }

        private void ExecuteExpandGrid()
        {
            int cost = _game.GetGridExpandCost(Owner.Player);
            if (_game.UpgradePowerGrid(Owner.Player))
            {
                int cx = _btnExpandGridRect.X + _btnExpandGridRect.Width / 2;
                int cy = _btnExpandGridRect.Y + _btnExpandGridRect.Height / 2;
                AddShockwave(cx, cy, _colorGold, 45f);
                AddFloatingText($"전력망 확장! (한도 {_game.PlayerPlantLimit}기)", cx - 40, cy - 20, _colorGold);
            }
            else
            {
                int cx = _btnExpandGridRect.X + _btnExpandGridRect.Width / 2;
                int cy = _btnExpandGridRect.Y + _btnExpandGridRect.Height / 2;
                AddFloatingText($"크레딧 부족 ({cost}G 필요)", cx - 40, cy - 20, _colorCrimson);
            }
        }

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.B:
                case Keys.U:
                case Keys.S:
                case Keys.D1:
                case Keys.D2:
                case Keys.D3:
                case Keys.D4:
                case Keys.D5:
                case Keys.D6:
                case Keys.NumPad1:
                case Keys.NumPad2:
                case Keys.NumPad3:
                case Keys.NumPad4:
                case Keys.NumPad5:
                case Keys.NumPad6:
                case Keys.Space:
                case Keys.Enter:
                case Keys.Escape:
                case Keys.E:
                case Keys.Oemplus:
                case Keys.Add:
                    return true;
            }
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (_isTurnRunning) return;

            // [리플레이 모드 키보드 단축키]
            if (_isReplaying)
            {
                if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Back)
                {
                    _isReplaying = false;
                    Invalidate();
                }
                else if (e.KeyCode == Keys.Space)
                {
                    if (_isReplayPaused && _replayIndex >= _game.ReplaySnapshots.Count - 1)
                    {
                        _replayIndex = 0;
                    }
                    _isReplayPaused = !_isReplayPaused;
                    Invalidate();
                }
                else if (e.KeyCode == Keys.Left || e.KeyCode == Keys.A)
                {
                    _isReplayPaused = true;
                    _replayIndex = Math.Max(0, _replayIndex - 1);
                    Invalidate();
                }
                else if (e.KeyCode == Keys.Right || e.KeyCode == Keys.D)
                {
                    _isReplayPaused = true;
                    _replayIndex = Math.Min(_game.ReplaySnapshots.Count - 1, _replayIndex + 1);
                    Invalidate();
                }
                else if (e.KeyCode == Keys.Home)
                {
                    _isReplayPaused = true;
                    _replayIndex = 0;
                    Invalidate();
                }
                else if (e.KeyCode == Keys.End)
                {
                    _isReplayPaused = true;
                    _replayIndex = Math.Max(0, _game.ReplaySnapshots.Count - 1);
                    Invalidate();
                }
                else if (e.KeyCode == Keys.D1 || e.KeyCode == Keys.NumPad1)
                {
                    _replaySpeed = 1;
                    Invalidate();
                }
                else if (e.KeyCode == Keys.D2 || e.KeyCode == Keys.NumPad2)
                {
                    _replaySpeed = 2;
                    Invalidate();
                }
                else if (e.KeyCode == Keys.D4 || e.KeyCode == Keys.NumPad4)
                {
                    _replaySpeed = 4;
                    Invalidate();
                }
                return;
            }

            // [맵 사이즈 선택 모달 키보드 단축키]
            if (_isMapSizeSelectActive)
            {
                if (e.KeyCode == Keys.D1 || e.KeyCode == Keys.NumPad1)
                {
                    SelectMapSizeAndStart(MapSizeType.Small);
                }
                else if (e.KeyCode == Keys.D2 || e.KeyCode == Keys.NumPad2 || e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
                {
                    SelectMapSizeAndStart(MapSizeType.Standard);
                }
                else if (e.KeyCode == Keys.D3 || e.KeyCode == Keys.NumPad3)
                {
                    SelectMapSizeAndStart(MapSizeType.Large);
                }
                else if (e.KeyCode == Keys.D4 || e.KeyCode == Keys.NumPad4)
                {
                    SelectMapSizeAndStart(MapSizeType.Huge);
                }
                return;
            }

            if (_game.IsGameOver || _showGuide || _showAchievements || _game.IsCardSelectionActive)
            {
                if (e.KeyCode == Keys.Escape && (_showGuide || _showAchievements))
                {
                    _showGuide = false;
                    _showAchievements = false;
                    Invalidate();
                }
                return;
            }

            // [최초 기지 설립 및 전술 공수 대기 상태]: 배치 완료 전까지 키보드 단축키 조작 잠금
            if (_game.IsInitialBasePlacementActive || (_game.PendingDropRemainingCount > 0 && _game.PendingDropUnitType != UnitType.None))
            {
                return;
            }

            if (e.KeyCode == Keys.B)
            {
                _currentCategory = BottomCategory.Buildings;
                _selectedBuilding = BuildingType.None;
                _selectedUnit = UnitType.None;
                _selectedSkill = UserSkillType.None;
                Invalidate();
            }
            else if (e.KeyCode == Keys.U)
            {
                _currentCategory = BottomCategory.Units;
                _selectedBuilding = BuildingType.None;
                _selectedUnit = UnitType.None;
                _selectedSkill = UserSkillType.None;
                Invalidate();
            }
            else if (e.KeyCode == Keys.S)
            {
                _currentCategory = BottomCategory.Skills;
                _selectedBuilding = BuildingType.None;
                _selectedUnit = UnitType.None;
                _selectedSkill = UserSkillType.None;
                Invalidate();
            }
            else if (e.KeyCode == Keys.D1 || e.KeyCode == Keys.NumPad1)
            {
                ExecuteSlotAction(0);
                Invalidate();
            }
            else if (e.KeyCode == Keys.D2 || e.KeyCode == Keys.NumPad2)
            {
                ExecuteSlotAction(1);
                Invalidate();
            }
            else if (e.KeyCode == Keys.D3 || e.KeyCode == Keys.NumPad3)
            {
                ExecuteSlotAction(2);
                Invalidate();
            }
            else if (e.KeyCode == Keys.D4 || e.KeyCode == Keys.NumPad4)
            {
                ExecuteSlotAction(3);
                Invalidate();
            }
            else if (e.KeyCode == Keys.D5 || e.KeyCode == Keys.NumPad5)
            {
                ExecuteSlotAction(4);
                Invalidate();
            }
            else if (e.KeyCode == Keys.D6 || e.KeyCode == Keys.NumPad6)
            {
                ExecuteSlotAction(5);
                Invalidate();
            }
            else if (e.KeyCode == Keys.E || e.KeyCode == Keys.Oemplus || e.KeyCode == Keys.Add)
            {
                if (_currentCategory == BottomCategory.Buildings)
                {
                    ExecuteExpandGrid();
                    Invalidate();
                }
            }
            else if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
            {
                if (_isTurnRunning)
                {
                    SkipTurnSequence();
                    return;
                }
                _selectedBuilding = BuildingType.None;
                _selectedUnit = UnitType.None;
                _selectedSkill = UserSkillType.None;
                StartTurnSequence();
                Invalidate();
            }
            else if (e.KeyCode == Keys.F || e.KeyCode == Keys.Z)
            {
                _turnPlaySpeed = (_turnPlaySpeed == 1) ? 2 : (_turnPlaySpeed == 2) ? 4 : 1;
                AddFloatingText($"진행 배속: {_turnPlaySpeed}x", _btnTurnSpeedRect.X - 10, _btnTurnSpeedRect.Bottom + 6, (_turnPlaySpeed == 4) ? Color.FromArgb(255, 60, 200) : _colorCyan);
                Invalidate();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                if (_isReplaying)
                {
                    _isReplaying = false;
                    Invalidate();
                    return;
                }

                if (_showGuide)
                {
                    _showGuide = false;
                    Invalidate();
                    return;
                }

                if (_selectedBuilding != BuildingType.None || _selectedUnit != UnitType.None || _selectedSkill != UserSkillType.None)
                {
                    _selectedBuilding = BuildingType.None;
                    _selectedUnit = UnitType.None;
                    _selectedSkill = UserSkillType.None;
                    Invalidate();
                }
            }
        }

        #endregion

        #region Helpers

        private void StartReplay()
        {
            if (_game.ReplaySnapshots.Count == 0) return;

            if (Dashboard.DashboardManager.UnlockCoreFrontAchievement("CF_REPLAY_FIRST", out string rTitle, out int rPts))
            {
                _game.AddCombatLog($"🏆 [도전과제 달성] {rTitle} (+{rPts} AP)");
                ToastNotification.Show($"[도전과제 달성!]\n{rTitle} (+{rPts} AP)", "🏆 CORE FRONT", ToastIcon.Success);
            }

            _isReplaying = true;
            _isReplayPaused = false;
            _replayIndex = 0;
            _replaySpeed = 1;
            _replayTickCounter = 0;
            _hoverReplayBtnId = -1;
            Invalidate();
        }

        public void PromptMapSelection()
        {
            _isMapSizeSelectActive = true;
            _showGuide = false;
            _isReplaying = false;
            _game.PendingDropUnitType = UnitType.None;
            _game.PendingDropRemainingCount = 0;
            Invalidate();
        }

        public void AdjustParentFormSize(MapSizeType mapSize)
        {
            Form parent = this.ParentForm;
            if (parent == null) return;

            int targetW = 820;
            int targetH = 720;
            switch (mapSize)
            {
                case MapSizeType.Small:
                    targetW = 720;
                    targetH = 680;
                    break;
                case MapSizeType.Standard:
                    targetW = 820;
                    targetH = 720;
                    break;
                case MapSizeType.Large:
                    targetW = 1000;
                    targetH = 720;
                    break;
                case MapSizeType.Huge:
                    targetW = 1060;
                    targetH = 800;
                    break;
            }

            if (parent.ClientSize.Width != targetW || parent.ClientSize.Height != targetH)
            {
                Point center = new Point(parent.Left + parent.Width / 2, parent.Top + parent.Height / 2);
                parent.ClientSize = new Size(targetW, targetH);
                var wa = Screen.FromControl(parent).WorkingArea;
                int newLeft = center.X - parent.Width / 2;
                int newTop = center.Y - parent.Height / 2;
                if (newLeft < wa.Left) newLeft = wa.Left;
                if (newTop < wa.Top) newTop = wa.Top;
                if (newLeft + parent.Width > wa.Right) newLeft = wa.Right - parent.Width;
                if (newTop + parent.Height > wa.Bottom) newTop = wa.Bottom - parent.Height;
                parent.Location = new Point(newLeft, newTop);
            }
        }

        private void RequestMapSizeChange(MapSizeType size)
        {
            if (_game == null || _game.CurrentMapSize == size) return;
            bool isGameInProgress = !_game.IsGameOver && (_game.TurnNumber > 1 || !_game.IsInitialBasePlacementActive || _game.InitialBaseRemainingCount < _game.InitialBaseTotalCount || _game.PlayerCredit != 6 || _game.Units.Count > 0);
            if (isGameInProgress)
            {
                var dr = MessageBox.Show(
                    "전장 규모를 변경하면 진행 중인 게임이 초기화되고 새로운 작전이 시작됩니다.\n\n계속 진행하시겠습니까?",
                    "전장 규모 변경 확인",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (dr != DialogResult.Yes) return;
            }
            SelectMapSizeAndStart(size);
        }

        public void SelectMapSizeAndStart(MapSizeType size)
        {
            _isMapSizeSelectActive = false;
            _showGuide = false;
            _isReplaying = false;

            AdjustParentFormSize(size);
            RestartGame(size);
        }

        private void RestartGame(MapSizeType mapSize = MapSizeType.Standard)
        {
            _game.InitializeMap(mapSize);
            _selectedBuilding = BuildingType.None;
            _selectedUnit = UnitType.None;
            _selectedSkill = UserSkillType.None;
            _floatingTexts.Clear();
            _shockwaves.Clear();
            _laserBeams.Clear();
            _isTurnRunning = false;
            _animPhase = TurnAnimPhase.Idle;
            UpdateLayoutBounds();
            string themeAnnounce = (_game.CurrentMapSize == MapSizeType.Huge)
                ? $"작전 개시! [{_game.GetThemeIcon()} {_game.GetThemeName()}]"
                : $"작전 개시! ({mapSize} 전장)";
            Color themeCol = (_game.CurrentMapSize == MapSizeType.Huge) ? _game.GetThemeColor() : _colorCyan;
            AddFloatingText(themeAnnounce, this.Width / 2 - 110, _boardRect.Y + 140, themeCol);
            Invalidate();
        }

        private void AddShockwave(float x, float y, Color color, float maxRadius)
        {
            _shockwaves.Add(new ShockwaveRing
            {
                X = x,
                Y = y,
                Radius = 8f,
                MaxRadius = maxRadius,
                Alpha = 0.9f,
                WaveColor = color
            });
        }

        private void AddFloatingText(string text, float x, float y, Color color)
        {
            _floatingTexts.Add(new CombatFloatingText
            {
                Text = text,
                X = x,
                Y = y,
                Alpha = 1.0f,
                TextColor = color
            });
        }

        #endregion
    }
}
