using MaterialSkin.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GateHelper
{
    public partial class BitFlipControl : UserControl
    {
        // 핵심 게임 엔진
        private BitFlipModel _model;
        private Timer _renderTimer;
        private Timer _gameClockTimer;
        private int _playTimeSeconds = 0;

        // 이펙트 및 파티클
        private readonly List<ShockwaveEffect> _shockwaves = new List<ShockwaveEffect>();
        private readonly List<SparkParticle> _particles = new List<SparkParticle>();
        private readonly List<FloatingNotification> _floatingTexts = new List<FloatingNotification>();

        // 승리 모달 1초 지연 표시 상태
        private bool _showVictoryModal = false;
        private bool _isVictoryDelayPending = false;

        // 호버 및 인터랙션 상태
        private Point? _hoveredGridPos = null;
        private List<BitNode> _hoverAffectedNodes = new List<BitNode>();
        private string _hoverTooltipText = null;
        private Point _mousePos = Point.Empty;

        // 맵 규모 선택 화면 상태 (시작 시 먼저 표시)
        private bool _isInMapSelect = true;
        private Rectangle _mapCardEasy;
        private Rectangle _mapCardNormal;
        private Rectangle _mapCardHard;
        private Rectangle _mapCardExpert;
        private Rectangle _mapBtnEasy;
        private Rectangle _mapBtnNormal;
        private Rectangle _mapBtnHard;
        private Rectangle _mapBtnExpert;
        private Rectangle _btnMapSelectBack;
        private int _hoverMapCardIndex = -1;

        // HUD 가상 버튼 영역 (동적 계산)
        private Rectangle _btnBackRect;
        private Rectangle _btnModeRect;
        private Rectangle _btnEasyRect;
        private Rectangle _btnNormalRect;
        private Rectangle _btnHardRect;
        private Rectangle _btnExpertRect;
        private Rectangle _btnResetRect;
        private Rectangle _btnUndoRect;
        private Rectangle _btnHintRect;

        // 승리 모달 버튼 영역
        private Rectangle _modalNextRect;
        private Rectangle _modalRetryRect;
        private Rectangle _modalModeRect;
        private Rectangle _modalListRect;

        // 보드 렌더링 영역
        private Rectangle _boardBounds;
        private float _cellSize;

        // 애니메이션 틱 카운터
        private float _globalPulse = 0f;

        // ==========================================
        // 고성능 렌더링 리소스 캐시 (GC 압박 & 핸들 생성 0화)
        // ==========================================
        private static readonly Font _fontHudLabel = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        private static readonly Font _fontHudVal = new Font("Segoe UI", 11.5f, FontStyle.Bold);
        private static readonly Font _fontBtn = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        private static readonly Font _fontPill = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        private static readonly Font _fontPillBold = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        private static readonly Font _fontHeaderTitle = new Font("Segoe UI", 16.5f, FontStyle.Bold);
        private static readonly Font _fontHeaderSub = new Font("Segoe UI", 9.8f, FontStyle.Regular);
        private static readonly Font _fontCardTitle = new Font("Segoe UI", 11.5f, FontStyle.Bold);
        private static readonly Font _fontCardBadge = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        private static readonly Font _fontCardBullet = new Font("Segoe UI", 9.0f, FontStyle.Regular);
        private static readonly Font _fontBottomLegend = new Font("Segoe UI", 9.0f, FontStyle.Regular);
        private static readonly Font _fontFloating = new Font("Segoe UI", 10.5f, FontStyle.Bold);
        private static readonly Font _fontModalTitle = new Font("Segoe UI", 18f, FontStyle.Bold);
        private static readonly Font _fontModalSub = new Font("Segoe UI", 10.5f, FontStyle.Regular);
        private static readonly Font _fontModalStar = new Font("Segoe UI", 24f, FontStyle.Bold);
        private static readonly Font _fontModalStatKey = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        private static readonly Font _fontModalStatVal = new Font("Segoe UI", 10.5f, FontStyle.Bold);

        private static readonly StringFormat _sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        private static readonly StringFormat _sfHorizCenter = new StringFormat { Alignment = StringAlignment.Center };
        private static readonly StringFormat _sfFar = new StringFormat { Alignment = StringAlignment.Far };

        // 보드 셀 크기에 따라 동적 캐싱되는 비트 폰트
        private Font _fontBitSymbol = null;
        private Font _fontGimmickBadge = null;

        public BitFlipControl()
        {
            InitializeComponent();

            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.BackColor = Color.FromArgb(11, 15, 25);

            _model = new BitFlipModel();

            // 60 FPS 렌더링 애니메이션 루프
            _renderTimer = new Timer();
            _renderTimer.Interval = 16;
            _renderTimer.Tick += OnRenderTick;
            _renderTimer.Start();

            // 1초 주기의 게임 시계 타이머 (맵 선택 중에는 작동하지 않음)
            _gameClockTimer = new Timer();
            _gameClockTimer.Interval = 1000;
            _gameClockTimer.Tick += (s, e) =>
            {
                if (!_isInMapSelect && _model.IsGameActive && !_model.IsVictory)
                {
                    _playTimeSeconds++;
                }
            };
            _gameClockTimer.Start();

            // 백그라운드 전환 시 타이머 정지 (CPU 점유율 0% 절전)
            this.VisibleChanged += (s, e) =>
            {
                bool isVis = this.Visible && (this.ParentForm == null || this.ParentForm.WindowState != FormWindowState.Minimized);
                OnTabVisibilityChanged(isVis);
            };

            this.Resize += (s, e) => RecalculateLayout();
            RecalculateLayout();
        }

        public void OnTabVisibilityChanged(bool isVisible)
        {
            if (isVisible)
            {
                if (_renderTimer != null && !_renderTimer.Enabled) _renderTimer.Start();
                if (_gameClockTimer != null && !_gameClockTimer.Enabled) _gameClockTimer.Start();
                this.Invalidate();
            }
            else
            {
                if (_renderTimer != null && _renderTimer.Enabled) _renderTimer.Stop();
                if (_gameClockTimer != null && _gameClockTimer.Enabled) _gameClockTimer.Stop();
            }
        }

        private void RecalculateLayout()
        {
            int w = this.ClientSize.Width;
            int h = this.ClientSize.Height;
            if (w <= 0 || h <= 0) return;

            // 1. 상단 헤더 HUD 버튼 레이아웃 (Y: 10 ~ 42)
            _btnBackRect = new Rectangle(12, 10, 56, 32);
            _btnModeRect = new Rectangle(72, 10, 56, 32);

            int diffBtnW = 46;
            int diffStartX = 132;
            int diffGap = 4;
            _btnEasyRect = new Rectangle(diffStartX, 10, diffBtnW, 32);
            _btnNormalRect = new Rectangle(diffStartX + (diffBtnW + diffGap), 10, diffBtnW, 32);
            _btnHardRect = new Rectangle(diffStartX + (diffBtnW + diffGap) * 2, 10, diffBtnW, 32);
            _btnExpertRect = new Rectangle(diffStartX + (diffBtnW + diffGap) * 3, 10, diffBtnW, 32);

            // 우측 컨트롤 버튼들 (언도, 힌트, 재시작)
            int actionBtnW = 74;
            int actionGap = 5;
            _btnResetRect = new Rectangle(w - 12 - actionBtnW, 10, actionBtnW, 32);
            _btnHintRect = new Rectangle(_btnResetRect.Left - actionGap - actionBtnW, 10, actionBtnW, 32);
            _btnUndoRect = new Rectangle(_btnHintRect.Left - actionGap - actionBtnW, 10, actionBtnW, 32);

            // 2. 중앙 보드 영역 (Y: 110 ~ H - 45)
            int boardTop = 110;
            int boardBottom = h - 45;
            int availableH = boardBottom - boardTop;
            int availableW = w - 40;

            int boardSide = Math.Min(availableW, availableH);
            boardSide = Math.Max(240, boardSide);

            int boardX = (w - boardSide) / 2;
            int boardY = boardTop + (availableH - boardSide) / 2;

            _boardBounds = new Rectangle(boardX, boardY, boardSide, boardSide);
            _cellSize = (float)boardSide / _model.GridSize;

            // 비트 폰트 캐시 갱신 (프레임마다 재생성하지 않고 레이아웃 변경 시 1회만 생성)
            float nw = _cellSize - 10f;
            _fontBitSymbol?.Dispose();
            _fontBitSymbol = new Font("Consolas", Math.Max(11f, nw * 0.38f), FontStyle.Bold);
            _fontGimmickBadge?.Dispose();
            _fontGimmickBadge = new Font("Segoe UI", Math.Max(7.5f, nw * 0.22f), FontStyle.Bold);

            // 3. 맵 규모 선택 화면 레이아웃 (2x2 그리드)
            int mapMarginX = 20;
            int cardGapX = 14;
            int mapCardW = (w - mapMarginX * 2 - cardGapX) / 2;
            int mapCardH = 185;

            int row1Y = 82;
            int row2Y = row1Y + mapCardH + 14;

            _mapCardEasy = new Rectangle(mapMarginX, row1Y, mapCardW, mapCardH);
            _mapCardNormal = new Rectangle(mapMarginX + mapCardW + cardGapX, row1Y, mapCardW, mapCardH);
            _mapCardHard = new Rectangle(mapMarginX, row2Y, mapCardW, mapCardH);
            _mapCardExpert = new Rectangle(mapMarginX + mapCardW + cardGapX, row2Y, mapCardW, mapCardH);

            _mapBtnEasy = new Rectangle(_mapCardEasy.Left + 12, _mapCardEasy.Bottom - 36, _mapCardEasy.Width - 24, 26);
            _mapBtnNormal = new Rectangle(_mapCardNormal.Left + 12, _mapCardNormal.Bottom - 36, _mapCardNormal.Width - 24, 26);
            _mapBtnHard = new Rectangle(_mapCardHard.Left + 12, _mapCardHard.Bottom - 36, _mapCardHard.Width - 24, 26);
            _mapBtnExpert = new Rectangle(_mapCardExpert.Left + 12, _mapCardExpert.Bottom - 36, _mapCardExpert.Width - 24, 26);

            int bSelectW = 240;
            _btnMapSelectBack = new Rectangle((w - bSelectW) / 2, row2Y + mapCardH + 16, bSelectW, 36);
        }

        private void OnRenderTick(object sender, EventArgs e)
        {
            if (this.IsDisposed || this.Disposing || !this.Visible) return;

            bool isAnyAnimating = false;

            // 1. 노드 플립 애니메이션 갱신 (기존 0.09f -> 0.25f 초고속 즉각 반응: 70ms 내 완료)
            if (_model.Grid != null)
            {
                for (int r = 0; r < _model.GridSize; r++)
                {
                    for (int c = 0; c < _model.GridSize; c++)
                    {
                        var node = _model.Grid[c, r];
                        if (node.FlipProgress < 1.0f)
                        {
                            node.FlipProgress = Math.Min(1.0f, node.FlipProgress + 0.25f);
                            isAnyAnimating = true;
                        }
                        if (node.PulseScale > 1.0f)
                        {
                            node.PulseScale = Math.Max(1.0f, node.PulseScale - 0.08f);
                            isAnyAnimating = true;
                        }
                    }
                }
            }

            // 2. 쇼크웨이브 갱신
            for (int i = _shockwaves.Count - 1; i >= 0; i--)
            {
                var wave = _shockwaves[i];
                wave.Radius += 8.5f;
                wave.Alpha -= 0.065f;
                if (wave.Alpha <= 0f || wave.Radius >= wave.MaxRadius)
                {
                    _shockwaves.RemoveAt(i);
                }
                else
                {
                    isAnyAnimating = true;
                }
            }

            // 3. 스파크 파티클 갱신
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var p = _particles[i];
                p.Position = new PointF(p.Position.X + p.Velocity.X, p.Position.Y + p.Velocity.Y);
                p.Velocity = new PointF(p.Velocity.X * 0.90f, p.Velocity.Y * 0.90f + 0.22f);
                p.Life -= 0.055f;
                if (p.Life <= 0f)
                {
                    _particles.RemoveAt(i);
                }
                else
                {
                    isAnyAnimating = true;
                }
            }

            // 4. 플로팅 알림 텍스트 갱신
            for (int i = _floatingTexts.Count - 1; i >= 0; i--)
            {
                var ft = _floatingTexts[i];
                ft.Position = new PointF(ft.Position.X, ft.Position.Y - 1.2f);
                ft.Life -= 0.045f;
                ft.Alpha = Math.Max(0f, ft.Life);
                if (ft.Life <= 0f)
                {
                    _floatingTexts.RemoveAt(i);
                }
                else
                {
                    isAnyAnimating = true;
                }
            }

            // 5. 힌트 하이라이트 펄스 애니메이션
            if (_model.CurrentHintNode.HasValue)
            {
                _globalPulse += 0.08f;
                if (_globalPulse > (float)Math.PI * 2) _globalPulse -= (float)Math.PI * 2;
                isAnyAnimating = true;
            }

            // 애니메이션 진행 중일 때만 화면 재갱신 (유휴 상태 시 불필요한 60 FPS 렌더링 낭비 원천 차단 -> CPU 점유율 0% 유지)
            if (isAnyAnimating)
            {
                this.Invalidate();
            }
        }

        // ==========================================
        // GDI+ 메인 렌더링 파이프라인
        // ==========================================
        protected override void OnPaint(PaintEventArgs e)
        {
            try
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                // 1. 미래지향적 사이버 배경 렌더링
                DrawCyberBackground(g);

                // 맵 규모 선택 화면 (게임 시작 전 또는 난이도 선택 시)
                if (_isInMapSelect)
                {
                    DrawMapSelectScreen(g);
                    return;
                }

                // 2. 상단 네온 헤더 바 렌더링
                DrawTopHeaderBar(g);

                // 3. 실시간 정보 HUD (이동 수, Par, 예상 등급)
                DrawStatusHud(g);

                // 4. 중앙 매트릭스 비트 격자 렌더링
                DrawBitMatrix(g);

                // 5. 시각 효과 (쇼크웨이브, 파티클, 플로팅 텍스트)
                DrawVisualEffects(g);

                // 6. 하단 기믹 가이드 툴팁 바
                DrawBottomLegendBar(g);

                // 7. 승리 완료 오버레이 (클리어 완료 감상 1초 후 팝업)
                if (_showVictoryModal)
                {
                    DrawVictoryOverlay(g);
                }
            }
            catch
            {
                // Unhandled GDI+ Exception Fallback: 클린 배경을 채워 폼 전체에 빨간 X(Red X of Death)가 뜨는 현상 원천 차단
                try
                {
                    e.Graphics.Clear(Color.FromArgb(10, 14, 24));
                }
                catch { }
            }
        }

        private void DrawCyberBackground(Graphics g)
        {
            int w = this.ClientSize.Width;
            int h = this.ClientSize.Height;

            // 깊은 우주 매트릭스 그라데이션
            using (var bgBrush = new LinearGradientBrush(new Rectangle(0, 0, w, h),
                Color.FromArgb(10, 14, 24), Color.FromArgb(16, 22, 38), 90f))
            {
                g.FillRectangle(bgBrush, 0, 0, w, h);
            }

            // 은은한 사이버 방사형 백라이트
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(_boardBounds.Left - 40, _boardBounds.Top - 40, _boardBounds.Width + 80, _boardBounds.Height + 80);
                using (var pgb = new PathGradientBrush(path))
                {
                    pgb.CenterColor = Color.FromArgb(20, 0, 240, 210);
                    pgb.SurroundColors = new[] { Color.FromArgb(0, 0, 0, 0) };
                    g.FillPath(pgb, path);
                }
            }

            // 미세한 서킷 그리드 라인
            using (var gridPen = new Pen(Color.FromArgb(18, 28, 48), 1f))
            {
                for (int x = 0; x < w; x += 36) g.DrawLine(gridPen, x, 0, x, h);
                for (int y = 0; y < h; y += 36) g.DrawLine(gridPen, 0, y, w, y);
            }
        }

        // ==========================================
        // 맵 규모 선택 화면 렌더링
        // ==========================================
        private void DrawMapSelectScreen(Graphics g)
        {
            int w = this.ClientSize.Width;
            int h = this.ClientSize.Height;

            // 1. 헤더 타이틀 및 서브타이틀
            using (var titleBrush = new SolidBrush(Color.FromArgb(0, 245, 212)))
            using (var subBrush = new SolidBrush(Color.FromArgb(160, 185, 210)))
            {
                g.DrawString("⚡ BIT FLIP : 침투 매트릭스 규모 선택", _fontHeaderTitle, titleBrush, w / 2f, 18, _sfHorizCenter);
                g.DrawString("작전을 수행할 보안 격자 규모와 침투 난이도를 선택하십시오.", _fontHeaderSub, subBrush, w / 2f, 48, _sfHorizCenter);
            }

            // 2. 4대 난이도 카드 렌더링 (2x2 그리드)
            DrawMapCard(g, _mapCardEasy, _mapBtnEasy, 0, "3×3 입문 매트릭스", "EASY", Color.FromArgb(0, 245, 212),
                new[] { "• 기본 십자(+) 반전 전술 체계", "• 퍼즐 메커니즘 적응 워밍업", "• 최적 권장 이동: 2 ~ 4 Par" });

            DrawMapCard(g, _mapCardNormal, _mapBtnNormal, 1, "4×4 전술 매트릭스", "NORMAL", Color.FromArgb(0, 210, 255),
                new[] { "• 대각선 인버터(✕) 전술 노드", "• 균형 잡힌 전략적 반전 수 싸움", "• 최적 권장 이동: 4 ~ 7 Par" });

            DrawMapCard(g, _mapCardHard, _mapBtnHard, 2, "5×5 방화벽 매트릭스", "HARD", Color.FromArgb(255, 175, 40),
                new[] { "• 절연 방화벽(🛡️) & 라인 레이저(⚡)", "• 고주파 침투 연산 및 깊은 수 읽기", "• 최적 권장 이동: 6 ~ 10 Par" });

            DrawMapCard(g, _mapCardExpert, _mapBtnExpert, 3, "6×6 양자 마스터 매트릭스", "EXPERT", Color.FromArgb(255, 65, 120),
                new[] { "• 양자 얽힘(∞) & 암호 잠금(🔑🔒)", "• 극한의 선형대수학적 연립 해독", "• 최적 권장 이동: 8 ~ 14 Par" });

            // 3. 하단 샌드박스 목록 복귀 버튼
            bool isBackHover = _btnMapSelectBack.Contains(_mousePos);
            DrawCyberButton(g, _btnMapSelectBack, "◀ 샌드박스 메인으로 돌아가기", Color.FromArgb(140, 160, 190), isBackHover);
        }

        private void DrawMapCard(Graphics g, Rectangle cardRect, Rectangle btnRect, int index, string title, string badge, Color themeColor, string[] bullets)
        {
            bool isHovered = _hoverMapCardIndex == index;

            // 카드 배경 & 테두리
            Color bgCol = isHovered ? Color.FromArgb(220, 20, 32, 54) : Color.FromArgb(190, 15, 23, 38);
            Color borderCol = isHovered ? themeColor : Color.FromArgb(40, 60, 95);

            using (var bgBrush = new SolidBrush(bgCol))
            using (var borderPen = new Pen(borderCol, isHovered ? 2.0f : 1.0f))
            {
                FillRoundedRect(g, bgBrush, cardRect, 10);
                DrawRoundedRect(g, borderPen, cardRect, 10);
            }

            // 호버 시 상단 네온 악센트 바
            if (isHovered)
            {
                Rectangle topBar = new Rectangle(cardRect.Left + 2, cardRect.Top + 2, cardRect.Width - 4, 3);
                using (var barBrush = new SolidBrush(themeColor))
                {
                    g.FillRectangle(barBrush, topBar);
                }
            }

            // 난이도 뱃지 (우측 상단)
            Rectangle badgeRect = new Rectangle(cardRect.Right - 76, cardRect.Top + 14, 62, 22);
            using (var badgeBg = new SolidBrush(Color.FromArgb(40, themeColor)))
            using (var badgePen = new Pen(themeColor, 1.2f))
            using (var badgeBrush = new SolidBrush(themeColor))
            {
                FillRoundedRect(g, badgeBg, badgeRect, 4);
                DrawRoundedRect(g, badgePen, badgeRect, 4);
                g.DrawString(badge, _fontCardBadge, badgeBrush, badgeRect, _sfCenter);
            }

            // 카드 타이틀 (좌측 상단)
            using (var titleBrush = new SolidBrush(isHovered ? Color.White : Color.FromArgb(235, 245, 255)))
            {
                g.DrawString(title, _fontCardTitle, titleBrush, cardRect.Left + 14, cardRect.Top + 15);
            }

            // 구분선
            using (var linePen = new Pen(Color.FromArgb(30, 45, 75), 1.0f))
            {
                g.DrawLine(linePen, cardRect.Left + 14, cardRect.Top + 44, cardRect.Right - 14, cardRect.Top + 44);
            }

            // 설명 목록 (불릿)
            using (var bulletBrush = new SolidBrush(Color.FromArgb(160, 180, 205)))
            {
                int bulletY = cardRect.Top + 52;
                foreach (var bullet in bullets)
                {
                    g.DrawString(bullet, _fontCardBullet, bulletBrush, cardRect.Left + 14, bulletY);
                    bulletY += 22;
                }
            }

            // 침투 개시 버튼
            DrawCyberButton(g, btnRect, "침투 개시 ▶", themeColor, isHovered);
        }

        private void SelectMapAndStart(string diff)
        {
            _isInMapSelect = false;
            StartNewGame(diff);

            PointF center = new PointF(_boardBounds.Left + _boardBounds.Width / 2f, _boardBounds.Top + _boardBounds.Height / 2f);
            _shockwaves.Add(new ShockwaveEffect
            {
                Center = center,
                Radius = 15f,
                MaxRadius = _boardBounds.Width * 0.8f,
                WaveColor = Color.FromArgb(0, 245, 212)
            });
            SpawnSparks(center, Color.FromArgb(0, 245, 212), 20);

            _floatingTexts.Add(new FloatingNotification
            {
                Position = new PointF(center.X, _boardBounds.Top - 25),
                Text = $"⚡ 작전 개시! [{diff.ToUpper()} MATRIX]",
                TextColor = Color.FromArgb(0, 245, 212)
            });

            PlayTactileClickSound();
        }

        public void ShowMapSelection()
        {
            _isInMapSelect = true;
            _showVictoryModal = false;
            RecalculateLayout();
            this.Invalidate();
        }

        private void DrawTopHeaderBar(Graphics g)
        {
            // [◀ LIST] 버튼
            DrawCyberButton(g, _btnBackRect, "◀ LIST", Color.FromArgb(120, 140, 170), _btnBackRect.Contains(_mousePos));

            // [🌐 MAP] 버튼 (맵 규모 선택 화면으로 전환)
            DrawCyberButton(g, _btnModeRect, "🌐 MAP", Color.FromArgb(0, 220, 255), _btnModeRect.Contains(_mousePos));

            // 난이도 필 버튼들 (3x3, 4x4, 5x5, 6x6)
            DrawDifficultyPill(g, _btnEasyRect, "3x3", "Easy");
            DrawDifficultyPill(g, _btnNormalRect, "4x4", "Normal");
            DrawDifficultyPill(g, _btnHardRect, "5x5", "Hard");
            DrawDifficultyPill(g, _btnExpertRect, "6x6", "Expert");

            // 컨트롤 버튼 (언도, 힌트, 재시작) - 영어 표기 적용 및 겹침 방지
            bool canUndo = _model.UndoStack.Count > 0 && !_model.IsVictory;
            Color undoColor = canUndo ? Color.FromArgb(255, 170, 0) : Color.FromArgb(70, 75, 85);
            string undoText = canUndo ? $"↶ UNDO ({_model.UndoStack.Count})" : "↶ UNDO";
            DrawCyberButton(g, _btnUndoRect, undoText, undoColor, canUndo && _btnUndoRect.Contains(_mousePos), canUndo);

            Color hintColor = Color.FromArgb(0, 245, 180);
            DrawCyberButton(g, _btnHintRect, "💡 HINT", hintColor, _btnHintRect.Contains(_mousePos));

            DrawCyberButton(g, _btnResetRect, "↺ RESET", Color.FromArgb(0, 200, 255), _btnResetRect.Contains(_mousePos));
        }

        private void DrawDifficultyPill(Graphics g, Rectangle r, string text, string diffKey)
        {
            bool isSelected = _model.Difficulty == diffKey;
            bool isHovered = r.Contains(_mousePos);

            Color baseColor = isSelected ? Color.FromArgb(0, 245, 212) : (isHovered ? Color.FromArgb(160, 190, 220) : Color.FromArgb(80, 95, 115));
            Color bgColor = isSelected ? Color.FromArgb(40, 0, 245, 212) : (isHovered ? Color.FromArgb(25, 255, 255, 255) : Color.FromArgb(18, 25, 40));

            using (var brush = new SolidBrush(bgColor))
            using (var pen = new Pen(baseColor, isSelected ? 1.8f : 1.0f))
            {
                FillRoundedRect(g, brush, r, 6);
                DrawRoundedRect(g, pen, r, 6);
            }

            using (var fontBrush = new SolidBrush(isSelected ? Color.White : (isHovered ? Color.FromArgb(230, 240, 255) : Color.FromArgb(150, 165, 185))))
            {
                g.DrawString(text, isSelected ? _fontPillBold : _fontPill, fontBrush, r, _sfCenter);
            }
        }

        private void DrawCyberButton(Graphics g, Rectangle r, string text, Color themeColor, bool isHover, bool isEnabled = true)
        {
            Color borderCol = isEnabled ? (isHover ? themeColor : Color.FromArgb(140, themeColor)) : Color.FromArgb(50, 60, 75);
            Color bgCol = isEnabled ? (isHover ? Color.FromArgb(50, themeColor) : Color.FromArgb(20, themeColor)) : Color.FromArgb(15, 20, 30);

            using (var brush = new SolidBrush(bgCol))
            using (var pen = new Pen(borderCol, isHover ? 1.5f : 1.0f))
            {
                FillRoundedRect(g, brush, r, 6);
                DrawRoundedRect(g, pen, r, 6);
            }

            using (var textBrush = new SolidBrush(isEnabled ? (isHover ? Color.White : Color.FromArgb(210, 225, 245)) : Color.FromArgb(90, 100, 115)))
            {
                g.DrawString(text, _fontBtn, textBrush, r, _sfCenter);
            }
        }

        private void DrawStatusHud(Graphics g)
        {
            int w = this.ClientSize.Width;
            Rectangle hudRect = new Rectangle(14, 52, w - 28, 48);

            // 글래스모피즘 HUD 컨테이너
            using (var bgBrush = new SolidBrush(Color.FromArgb(180, 16, 23, 37)))
            using (var borderPen = new Pen(Color.FromArgb(40, 60, 95), 1f))
            {
                FillRoundedRect(g, bgBrush, hudRect, 8);
                DrawRoundedRect(g, borderPen, hudRect, 8);
            }

            int sectionW = hudRect.Width / 4;

            // 1. 타이머
            string timeStr = string.Format("{0:D2}:{1:D2}", _playTimeSeconds / 60, _playTimeSeconds % 60);
            DrawHudMetric(g, new Rectangle(hudRect.Left, hudRect.Top, sectionW, hudRect.Height), "⏱️ 경과 시간", timeStr, Color.FromArgb(0, 230, 255));

            // 2. 이동수 vs Par
            string moveStr = $"{_model.MoveCount} / {_model.ParMoves} PAR";
            Color moveCol = _model.MoveCount <= _model.ParMoves ? Color.FromArgb(0, 255, 170) : (_model.MoveCount <= _model.ParMoves + 3 ? Color.FromArgb(255, 200, 50) : Color.FromArgb(255, 90, 110));
            DrawHudMetric(g, new Rectangle(hudRect.Left + sectionW, hudRect.Top, sectionW, hudRect.Height), "🎯 이동 횟수", moveStr, moveCol);

            // 3. 예상 등급
            int stars = _model.GetStarRating();
            string starStr = stars == 3 ? "★★★ (최상급)" : (stars == 2 ? "★★☆ (우수)" : "★☆☆ (달성)");
            Color starCol = stars == 3 ? Color.FromArgb(255, 215, 0) : (stars == 2 ? Color.FromArgb(100, 220, 255) : Color.FromArgb(160, 170, 190));
            DrawHudMetric(g, new Rectangle(hudRect.Left + sectionW * 2, hudRect.Top, sectionW, hudRect.Height), "🌟 해킹 등급", starStr, starCol);

            // 4. 활성 특수 기믹 요약
            var activeGimmicks = new List<string>();
            for (int r = 0; r < _model.GridSize; r++)
                for (int c = 0; c < _model.GridSize; c++)
                {
                    var t = _model.Grid[c, r].Type;
                    if (t != BitNodeType.Normal)
                    {
                        string badge = BitFlipGimmickGuide.GetBadgeSymbol(t);
                        if (!activeGimmicks.Contains(badge)) activeGimmicks.Add(badge);
                    }
                }

            string gimmickStr = activeGimmicks.Count > 0 ? string.Join(" ", activeGimmicks) : "표준 매트릭스";
            DrawHudMetric(g, new Rectangle(hudRect.Left + sectionW * 3, hudRect.Top, sectionW, hudRect.Height), "🛡️ 전술 기믹", gimmickStr, Color.FromArgb(200, 130, 255));
        }

        private void DrawHudMetric(Graphics g, Rectangle r, string label, string value, Color valColor)
        {
            using (var lblBrush = new SolidBrush(Color.FromArgb(140, 155, 175)))
            using (var valBrush = new SolidBrush(valColor))
            {
                g.DrawString(label, _fontHudLabel, lblBrush, r.Left + 12, r.Top + 6);
                g.DrawString(value, _fontHudVal, valBrush, r.Left + 12, r.Top + 24);
            }
        }

        private void DrawBitMatrix(Graphics g)
        {
            if (_model.Grid == null) return;

            // 격자 외곽 사이버 프레임
            using (var framePen = new Pen(Color.FromArgb(30, 45, 75), 1.5f))
            {
                DrawRoundedRect(g, framePen, _boardBounds, 12);
            }

            // 개별 비트 노드 렌더링
            for (int r = 0; r < _model.GridSize; r++)
            {
                for (int c = 0; c < _model.GridSize; c++)
                {
                    DrawBitNode(g, c, r);
                }
            }

            // 양자 얽힘 노드 간 연결 레이저선 렌더링
            DrawQuantumLinkBeams(g);

            // AI 최적 수 힌트 하이라이트 펄스 렌더링
            if (_model.CurrentHintNode.HasValue)
            {
                DrawAiHintHighlight(g, _model.CurrentHintNode.Value);
            }
        }

        private void DrawBitNode(Graphics g, int c, int r)
        {
            var node = _model.Grid[c, r];

            float nodePad = 5f;
            float nx = _boardBounds.Left + c * _cellSize + nodePad;
            float ny = _boardBounds.Top + r * _cellSize + nodePad;
            float nw = _cellSize - nodePad * 2;
            float nh = _cellSize - nodePad * 2;

            PointF center = new PointF(nx + nw / 2, ny + nh / 2);

            // 호버 여부
            bool isHovered = _hoveredGridPos.HasValue && _hoveredGridPos.Value.X == c && _hoveredGridPos.Value.Y == r;
            bool isAffectedByHover = _hoverAffectedNodes.Contains(node);

            // 3D 플립 애니메이션 보간 (Cosine Scale)
            float animProgress = node.FlipProgress;
            float scaleX = Math.Abs((float)Math.Cos(animProgress * Math.PI));
            scaleX = Math.Max(0.05f, scaleX);

            // 현재 표시할 겉면 상태 (0.5 기준으로 전환)
            bool displayState = animProgress >= 0.5f ? node.State : node.PrevState;

            float drawW = nw * scaleX;
            float drawX = center.X - drawW / 2;
            RectangleF nodeRect = new RectangleF(drawX, ny, drawW, nh);

            // 색상 테마 계산
            Color onColor = Color.FromArgb(0, 245, 212);
            Color offColor = Color.FromArgb(20, 28, 44);
            Color borderColor = displayState ? onColor : Color.FromArgb(50, 70, 100);

            if (node.Type != BitNodeType.Normal)
            {
                Color gCol = BitFlipGimmickGuide.GetThemeColor(node.Type);
                if (displayState) onColor = gCol;
                else borderColor = Color.FromArgb(120, gCol);
            }

            // 배경 채우기 (LinearGradient 대신 가벼운 SolidBrush 채우기 사용 -> 초고속 렌더링)
            Color fillCol = displayState ? Color.FromArgb(65, onColor) : offColor;
            using (var fillBrush = new SolidBrush(fillCol))
            {
                FillRoundedRect(g, fillBrush, Rectangle.Round(nodeRect), 8);
            }

            // 테두리 렌더링
            float borderWidth = displayState ? 2.0f : 1.2f;
            if (isHovered) borderWidth = 2.8f;
            using (var borderPen = new Pen(isHovered ? Color.White : borderColor, borderWidth))
            {
                DrawRoundedRect(g, borderPen, Rectangle.Round(nodeRect), 8);
            }

            // 호버 예측 반전 대상 하이라이트 (점선 네온 브라켓)
            if (isAffectedByHover && !isHovered)
            {
                float pulse = 0.5f + 0.5f * (float)Math.Sin(_globalPulse * 2.5f);
                int alpha = Math.Max(0, Math.Min(255, (int)(160 + 90 * pulse)));
                using (var affectPen = new Pen(Color.FromArgb(alpha, 255, 230, 80), 1.6f))
                {
                    affectPen.DashStyle = DashStyle.Dash;
                    DrawRoundedRect(g, affectPen, Rectangle.Round(new RectangleF(nx + 1, ny + 1, nw - 2, nh - 2)), 8);
                }
            }

            // 중앙 비트 상태 기호 (1 vs 0)
            if (scaleX > 0.35f && _fontBitSymbol != null)
            {
                string bitSymbol = displayState ? "1" : "0";
                using (var brush = new SolidBrush(displayState ? Color.White : Color.FromArgb(90, 115, 145)))
                {
                    g.DrawString(bitSymbol, _fontBitSymbol, brush, nodeRect, _sfCenter);
                }

                // 특수 기믹 뱃지 렌더링
                DrawGimmickBadge(g, node, nodeRect, displayState);
            }
        }

        private void DrawGimmickBadge(Graphics g, BitNode node, RectangleF rect, bool displayState)
        {
            if (node.Type == BitNodeType.Normal) return;

            string badge = BitFlipGimmickGuide.GetBadgeSymbol(node.Type);
            Color themeColor = BitFlipGimmickGuide.GetThemeColor(node.Type);

            // 좌측 상단 기믹 아이콘
            if (_fontGimmickBadge != null)
            {
                using (var brush = new SolidBrush(themeColor))
                {
                    g.DrawString(badge, _fontGimmickBadge, brush, rect.Left + 5, rect.Top + 3);
                }
            }

            // 절연 방화벽(Firewall) 보호막 표시
            if (node.Type == BitNodeType.Firewall)
            {
                using (var shieldPen = new Pen(Color.FromArgb(50, 220, 255), 1.8f))
                {
                    g.DrawArc(shieldPen, rect.Right - 16, rect.Top + 4, 11, 11, 0, 360);
                }
            }

            // 라인 레이저(Line) 가로 관통선 표시
            if (node.Type == BitNodeType.Line)
            {
                using (var laserPen = new Pen(Color.FromArgb(160, 255, 230, 0), 1.5f))
                {
                    laserPen.DashStyle = DashStyle.Dot;
                    g.DrawLine(laserPen, rect.Left + 4, rect.Top + rect.Height / 2, rect.Right - 4, rect.Top + rect.Height / 2);
                }
            }

            // 잠금 상태 자물쇠 표시
            if (node.Type == BitNodeType.Locked && node.IsLocked)
            {
                using (var lockBrush = new SolidBrush(Color.FromArgb(160, 255, 30, 50)))
                {
                    g.FillRectangle(lockBrush, rect.Left + 4, rect.Bottom - 10, rect.Width - 8, 6);
                }
            }
        }

        private void DrawQuantumLinkBeams(Graphics g)
        {
            var linkedNodes = new List<BitNode>();
            for (int r = 0; r < _model.GridSize; r++)
                for (int c = 0; c < _model.GridSize; c++)
                    if (_model.Grid[c, r].Type == BitNodeType.Linked)
                        linkedNodes.Add(_model.Grid[c, r]);

            if (linkedNodes.Count >= 2)
            {
                var n1 = linkedNodes[0];
                var n2 = linkedNodes[1];

                PointF p1 = GetNodeCenter(n1.X, n1.Y);
                PointF p2 = GetNodeCenter(n2.X, n2.Y);

                float pulse = 0.5f + 0.5f * (float)Math.Sin(_globalPulse * 2f);
                int alpha = Math.Max(0, Math.Min(255, (int)(120 * pulse)));
                using (var pen = new Pen(Color.FromArgb(alpha, 200, 80, 255), 2f))
                {
                    pen.DashStyle = DashStyle.DashDot;
                    g.DrawLine(pen, p1, p2);
                }
            }
        }

        private void DrawAiHintHighlight(Graphics g, Point hintPt)
        {
            if (hintPt.X < 0 || hintPt.X >= _model.GridSize || hintPt.Y < 0 || hintPt.Y >= _model.GridSize) return;

            PointF center = GetNodeCenter(hintPt.X, hintPt.Y);
            float radius = Math.Max(10f, (_cellSize * 0.52f) + (float)Math.Sin(_globalPulse * 3f) * 4f);

            using (var path = new GraphicsPath())
            {
                path.AddEllipse(center.X - radius, center.Y - radius, radius * 2, radius * 2);
                using (var pen = new Pen(Color.FromArgb(220, 0, 255, 170), 2.5f))
                {
                    g.DrawPath(pen, path);
                }
            }

            // 힌트 텍스트 뱃지
            using (var brush = new SolidBrush(Color.FromArgb(0, 255, 170)))
            {
                g.DrawString("★ AI 추천", _fontBtn, brush, center.X, center.Y - _cellSize * 0.55f - 14, _sfHorizCenter);
            }
        }

        private void DrawVisualEffects(Graphics g)
        {
            // 1. 쇼크웨이브
            foreach (var wave in _shockwaves)
            {
                int alpha = Math.Max(0, Math.Min(255, (int)(wave.Alpha * 255)));
                if (alpha <= 0 || wave.Radius <= 0f) continue;
                using (var pen = new Pen(Color.FromArgb(alpha, wave.WaveColor), 2.2f))
                {
                    g.DrawEllipse(pen, wave.Center.X - wave.Radius, wave.Center.Y - wave.Radius, wave.Radius * 2, wave.Radius * 2);
                }
            }

            // 2. 스파크 파티클
            foreach (var p in _particles)
            {
                int alpha = Math.Max(0, Math.Min(255, (int)(p.Life * 255)));
                if (alpha <= 0 || p.Size <= 0f) continue;
                using (var brush = new SolidBrush(Color.FromArgb(alpha, p.ParticleColor)))
                {
                    g.FillEllipse(brush, p.Position.X - p.Size / 2, p.Position.Y - p.Size / 2, p.Size, p.Size);
                }
            }

            // 3. 플로팅 텍스트
            foreach (var ft in _floatingTexts)
            {
                int alpha = Math.Max(0, Math.Min(255, (int)(ft.Alpha * 255)));
                if (alpha <= 0) continue;
                using (var brush = new SolidBrush(Color.FromArgb(alpha, ft.TextColor)))
                {
                    g.DrawString(ft.Text, _fontFloating, brush, ft.Position.X, ft.Position.Y, _sfHorizCenter);
                }
            }
        }

        private void DrawBottomLegendBar(Graphics g)
        {
            int w = this.ClientSize.Width;
            int h = this.ClientSize.Height;

            string guide = _hoverTooltipText ?? "💡 팁: 노드를 클릭하면 인접 비트가 반전됩니다. 특수 노드에 호버 시 상세 전술이 표출됩니다. (Z: 언도, H: AI 힌트)";

            using (var brush = new SolidBrush(_hoverTooltipText != null ? Color.FromArgb(0, 255, 210) : Color.FromArgb(120, 140, 165)))
            {
                g.DrawString(guide, _fontBottomLegend, brush, new Rectangle(14, h - 38, w - 28, 28), _sfCenter);
            }
        }

        private void DrawVictoryOverlay(Graphics g)
        {
            int w = this.ClientSize.Width;
            int h = this.ClientSize.Height;

            // 1. 딤 레이어
            using (var dimBrush = new SolidBrush(Color.FromArgb(220, 8, 12, 22)))
            {
                g.FillRectangle(dimBrush, 0, 0, w, h);
            }

            // 2. 글래스모피즘 모달 박스
            int modalW = 420;
            int modalH = 430;
            Rectangle modalRect = new Rectangle((w - modalW) / 2, (h - modalH) / 2, modalW, modalH);

            using (var boxBrush = new SolidBrush(Color.FromArgb(240, 16, 24, 40)))
            using (var borderPen = new Pen(Color.FromArgb(0, 245, 212), 2.0f))
            {
                FillRoundedRect(g, boxBrush, modalRect, 14);
                DrawRoundedRect(g, borderPen, modalRect, 14);
            }

            // 3. 타이틀 & 서브타이틀
            using (var titleBrush = new SolidBrush(Color.FromArgb(0, 245, 212)))
            using (var subBrush = new SolidBrush(Color.FromArgb(170, 190, 215)))
            {
                g.DrawString("SYSTEM OVERRIDDEN", _fontModalTitle, titleBrush, modalRect.Left + modalW / 2, modalRect.Top + 24, _sfHorizCenter);
                g.DrawString("보안 매트릭스 해킹 돌파 성공!", _fontModalSub, subBrush, modalRect.Left + modalW / 2, modalRect.Top + 58, _sfHorizCenter);
            }

            // 4. 별점 (★★★)
            int stars = _model.GetStarRating();
            string starGlyph = stars == 3 ? "★ ★ ★" : (stars == 2 ? "★ ★ ☆" : "★ ☆ ☆");
            using (var starBrush = new SolidBrush(Color.FromArgb(255, 215, 0)))
            {
                g.DrawString(starGlyph, _fontModalStar, starBrush, modalRect.Left + modalW / 2, modalRect.Top + 90, _sfHorizCenter);
            }

            // 5. 기록 상세 테이블
            int statsY = modalRect.Top + 145;
            DrawModalStatRow(g, modalRect.Left + 40, statsY, modalW - 80, "클리어 시간", string.Format("{0:D2}:{1:D2}", _playTimeSeconds / 60, _playTimeSeconds % 60));
            DrawModalStatRow(g, modalRect.Left + 40, statsY + 32, modalW - 80, "이동 횟수", $"{_model.MoveCount}회 (최적 Par: {_model.ParMoves}회)");
            DrawModalStatRow(g, modalRect.Left + 40, statsY + 64, modalW - 80, "AI 힌트 사용", $"{_model.HintsUsed}회");
            string rankTitle = stars == 3 ? "S+ MASTER HACKER" : (stars == 2 ? "A+ CYBER EXPERT" : "B CODE BREAKER");
            DrawModalStatRow(g, modalRect.Left + 40, statsY + 96, modalW - 80, "최종 랭크", rankTitle);

            // 6. 결과 모달 버튼 4종 (다음 레벨, 다시하기, 규모 선택, 목록으로)
            int btnW = 86;
            int btnH = 38;
            int btnGap = 8;
            int btnY = modalRect.Bottom - 58;
            int startX = modalRect.Left + (modalW - (btnW * 4 + btnGap * 3)) / 2;

            _modalNextRect = new Rectangle(startX, btnY, btnW, btnH);
            _modalRetryRect = new Rectangle(startX + (btnW + btnGap), btnY, btnW, btnH);
            _modalModeRect = new Rectangle(startX + (btnW + btnGap) * 2, btnY, btnW, btnH);
            _modalListRect = new Rectangle(startX + (btnW + btnGap) * 3, btnY, btnW, btnH);

            DrawCyberButton(g, _modalNextRect, "다음 레벨 ▶", Color.FromArgb(0, 245, 212), _modalNextRect.Contains(_mousePos));
            DrawCyberButton(g, _modalRetryRect, "다시하기 ↺", Color.FromArgb(0, 190, 255), _modalRetryRect.Contains(_mousePos));
            DrawCyberButton(g, _modalModeRect, "규모 선택 🌐", Color.FromArgb(255, 180, 50), _modalModeRect.Contains(_mousePos));
            DrawCyberButton(g, _modalListRect, "목록으로 ◀", Color.FromArgb(140, 160, 185), _modalListRect.Contains(_mousePos));
        }

        private void DrawModalStatRow(Graphics g, int x, int y, int w, string title, string val)
        {
            using (var titleBrush = new SolidBrush(Color.FromArgb(140, 155, 175)))
            using (var valBrush = new SolidBrush(Color.White))
            {
                g.DrawString(title, _fontModalStatKey, titleBrush, x, y);
                g.DrawString(val, _fontModalStatVal, valBrush, x + w, y, _sfFar);
            }
        }

        // ==========================================
        // 마우스 및 키보드 인터랙션
        // ==========================================
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            _mousePos = e.Location;

            // 맵 규모 선택 화면에서의 호버 및 커서 제어
            if (_isInMapSelect)
            {
                int prevHover = _hoverMapCardIndex;
                if (_mapCardEasy.Contains(e.Location) || _mapBtnEasy.Contains(e.Location)) _hoverMapCardIndex = 0;
                else if (_mapCardNormal.Contains(e.Location) || _mapBtnNormal.Contains(e.Location)) _hoverMapCardIndex = 1;
                else if (_mapCardHard.Contains(e.Location) || _mapBtnHard.Contains(e.Location)) _hoverMapCardIndex = 2;
                else if (_mapCardExpert.Contains(e.Location) || _mapBtnExpert.Contains(e.Location)) _hoverMapCardIndex = 3;
                else _hoverMapCardIndex = -1;

                bool isHandOnMapSelect = _hoverMapCardIndex >= 0 || _btnMapSelectBack.Contains(e.Location);
                this.Cursor = isHandOnMapSelect ? Cursors.Hand : Cursors.Default;
                if (prevHover != _hoverMapCardIndex) this.Invalidate();
                return;
            }

            // 보드 노드 호버 탐지
            Point? newHoverPos = GetGridCoordsAt(e.Location);
            if (newHoverPos != _hoveredGridPos)
            {
                _hoveredGridPos = newHoverPos;
                if (_hoveredGridPos.HasValue)
                {
                    _hoverAffectedNodes = _model.GetAffectedNodes(_hoveredGridPos.Value.X, _hoveredGridPos.Value.Y);
                    var node = _model.Grid[_hoveredGridPos.Value.X, _hoveredGridPos.Value.Y];
                    if (node.Type != BitNodeType.Normal)
                    {
                        _hoverTooltipText = $"[{BitFlipGimmickGuide.GetTitle(node.Type)}] {BitFlipGimmickGuide.GetDescription(node.Type)}";
                    }
                    else
                    {
                        _hoverTooltipText = null;
                    }
                }
                else
                {
                    _hoverAffectedNodes.Clear();
                    _hoverTooltipText = null;
                }
                this.Invalidate();
            }

            // 커서 모양 변경
            bool isHand = _boardBounds.Contains(e.Location) ||
                          _btnBackRect.Contains(e.Location) ||
                          _btnModeRect.Contains(e.Location) ||
                          _btnEasyRect.Contains(e.Location) ||
                          _btnNormalRect.Contains(e.Location) ||
                          _btnHardRect.Contains(e.Location) ||
                          _btnExpertRect.Contains(e.Location) ||
                          _btnResetRect.Contains(e.Location) ||
                          _btnUndoRect.Contains(e.Location) ||
                          _btnHintRect.Contains(e.Location) ||
                          (_showVictoryModal && (_modalNextRect.Contains(e.Location) || _modalRetryRect.Contains(e.Location) || _modalModeRect.Contains(e.Location) || _modalListRect.Contains(e.Location)));

            this.Cursor = isHand ? Cursors.Hand : Cursors.Default;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;

            // 0. 맵 규모 선택 화면 클릭 처리
            if (_isInMapSelect)
            {
                if (_mapCardEasy.Contains(e.Location) || _mapBtnEasy.Contains(e.Location)) { SelectMapAndStart("Easy"); return; }
                if (_mapCardNormal.Contains(e.Location) || _mapBtnNormal.Contains(e.Location)) { SelectMapAndStart("Normal"); return; }
                if (_mapCardHard.Contains(e.Location) || _mapBtnHard.Contains(e.Location)) { SelectMapAndStart("Hard"); return; }
                if (_mapCardExpert.Contains(e.Location) || _mapBtnExpert.Contains(e.Location)) { SelectMapAndStart("Expert"); return; }
                if (_btnMapSelectBack.Contains(e.Location)) { GoBackToList(); return; }
                return;
            }

            // 1. 승리 모달 버튼 클릭 처리 (1초 지연 후 모달 활성화 시에만 처리)
            if (_showVictoryModal)
            {
                if (_modalNextRect.Contains(e.Location))
                {
                    AdvanceToNextDifficulty();
                    return;
                }
                if (_modalRetryRect.Contains(e.Location))
                {
                    StartNewGame(_model.Difficulty);
                    return;
                }
                if (_modalModeRect.Contains(e.Location))
                {
                    _showVictoryModal = false;
                    _isInMapSelect = true;
                    this.Invalidate();
                    return;
                }
                if (_modalListRect.Contains(e.Location))
                {
                    GoBackToList();
                    return;
                }
                return;
            }

            // 2. 상단 헤더 HUD 버튼 클릭 처리
            if (_btnBackRect.Contains(e.Location))
            {
                GoBackToList();
                return;
            }

            if (_btnModeRect.Contains(e.Location))
            {
                _isInMapSelect = true;
                this.Invalidate();
                return;
            }

            if (_btnEasyRect.Contains(e.Location)) { StartNewGame("Easy"); return; }
            if (_btnNormalRect.Contains(e.Location)) { StartNewGame("Normal"); return; }
            if (_btnHardRect.Contains(e.Location)) { StartNewGame("Hard"); return; }
            if (_btnExpertRect.Contains(e.Location)) { StartNewGame("Expert"); return; }

            if (_btnResetRect.Contains(e.Location))
            {
                StartNewGame(_model.Difficulty);
                return;
            }

            if (_btnUndoRect.Contains(e.Location))
            {
                TriggerUndo();
                return;
            }

            if (_btnHintRect.Contains(e.Location))
            {
                TriggerAiHint();
                return;
            }

            // 3. 비트 노드 클릭 처리
            Point? pt = GetGridCoordsAt(e.Location);
            if (pt.HasValue)
            {
                TriggerNodeClick(pt.Value.X, pt.Value.Y);
            }
        }

        private void TriggerNodeClick(int x, int y)
        {
            PointF center = GetNodeCenter(x, y);

            if (_model.ClickNode(x, y, out var toggled, out bool unlockedGate))
            {
                // 쇼크웨이브 방출
                _shockwaves.Add(new ShockwaveEffect
                {
                    Center = center,
                    Radius = 10f,
                    MaxRadius = _cellSize * 1.8f,
                    WaveColor = Color.FromArgb(0, 245, 212)
                });

                // 스파크 파티클 생성
                SpawnSparks(center, Color.FromArgb(0, 245, 212), 14);

                // 플로팅 텍스트
                _floatingTexts.Add(new FloatingNotification
                {
                    Position = new PointF(center.X, center.Y - 15),
                    Text = "+1 MOVE",
                    TextColor = Color.FromArgb(0, 245, 212)
                });

                if (unlockedGate)
                {
                    _floatingTexts.Add(new FloatingNotification
                    {
                        Position = new PointF(this.ClientSize.Width / 2f, _boardBounds.Top - 20),
                        Text = "🔓 보안 게이트가 해제되었습니다!",
                        TextColor = Color.FromArgb(255, 215, 0)
                    });
                }

                // 사운드 피드백
                PlayTactileClickSound();

                // 호버 갱신
                _hoverAffectedNodes = _model.GetAffectedNodes(x, y);

                // 승리 시: 완성된 1상태 보드를 1초간 그대로 보여준 뒤 결과 모달창 띄우기
                if (_model.IsVictory && !_showVictoryModal && !_isVictoryDelayPending)
                {
                    _isVictoryDelayPending = true;

                    PointF boardCenter = new PointF(_boardBounds.Left + _boardBounds.Width / 2f, _boardBounds.Top + _boardBounds.Height / 2f);
                    _shockwaves.Add(new ShockwaveEffect
                    {
                        Center = boardCenter,
                        Radius = 20f,
                        MaxRadius = _boardBounds.Width * 0.95f,
                        WaveColor = Color.FromArgb(0, 255, 170)
                    });
                    SpawnSparks(boardCenter, Color.FromArgb(255, 215, 0), 30);

                    _floatingTexts.Add(new FloatingNotification
                    {
                        Position = new PointF(boardCenter.X, _boardBounds.Top - 25),
                        Text = "★ ALL BITS ACTIVE! SYSTEM OVERRIDDEN ★",
                        TextColor = Color.FromArgb(0, 255, 170)
                    });

                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(1000);
                        if (this.IsDisposed || this.Disposing) return;
                        this.BeginInvoke(new Action(() =>
                        {
                            _showVictoryModal = true;
                            this.Invalidate();
                        }));
                    });
                }

                this.Invalidate();
            }
            else
            {
                // 잠긴 노드 클릭 시 거부 이펙트
                SpawnSparks(center, Color.FromArgb(255, 60, 80), 8);
                _floatingTexts.Add(new FloatingNotification
                {
                    Position = new PointF(center.X, center.Y - 15),
                    Text = "🔒 ACCESS DENIED",
                    TextColor = Color.FromArgb(255, 70, 90)
                });
            }
        }

        private void TriggerUndo()
        {
            if (_model.Undo())
            {
                _floatingTexts.Add(new FloatingNotification
                {
                    Position = new PointF(this.ClientSize.Width / 2f, _boardBounds.Top - 20),
                    Text = "↶ 실행 취소 완료",
                    TextColor = Color.FromArgb(255, 180, 50)
                });
                PlayTactileClickSound();
                this.Invalidate();
            }
        }

        private void TriggerAiHint()
        {
            var hintPt = _model.RequestAiHint();
            if (hintPt.HasValue)
            {
                PointF center = GetNodeCenter(hintPt.Value.X, hintPt.Value.Y);
                _shockwaves.Add(new ShockwaveEffect
                {
                    Center = center,
                    Radius = 15f,
                    MaxRadius = _cellSize * 1.5f,
                    WaveColor = Color.FromArgb(0, 255, 170)
                });

                _floatingTexts.Add(new FloatingNotification
                {
                    Position = new PointF(this.ClientSize.Width / 2f, _boardBounds.Top - 20),
                    Text = "💡 AI 딥 스캔: 최적 경로가 감지되었습니다!",
                    TextColor = Color.FromArgb(0, 255, 170)
                });

                PlayTactileClickSound();
                this.Invalidate();
            }
            else
            {
                _floatingTexts.Add(new FloatingNotification
                {
                    Position = new PointF(this.ClientSize.Width / 2f, _boardBounds.Top - 20),
                    Text = "💡 이미 완료되었거나 추가 힌트가 없습니다.",
                    TextColor = Color.FromArgb(170, 185, 205)
                });
            }
        }

        private void AdvanceToNextDifficulty()
        {
            switch (_model.Difficulty)
            {
                case "Easy": StartNewGame("Normal"); break;
                case "Normal": StartNewGame("Hard"); break;
                case "Hard": StartNewGame("Expert"); break;
                default: StartNewGame("Expert"); break;
            }
        }

        public void StartNewGame(string diff)
        {
            _isInMapSelect = false;
            _model.InitializeGame(diff);
            _playTimeSeconds = 0;
            _showVictoryModal = false;
            _isVictoryDelayPending = false;
            _shockwaves.Clear();
            _particles.Clear();
            _floatingTexts.Clear();
            RecalculateLayout();
            this.Invalidate();
        }

        private void GoBackToList()
        {
            if (this.ParentForm is SandBox sb)
            {
                sb.BackToList();
            }
        }

        // ==========================================
        // 키보드 단축키 지원 (Z, H, R, ESC)
        // ==========================================
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.Z) || keyData == Keys.Z)
            {
                if (!_isInMapSelect) TriggerUndo();
                return true;
            }
            if (keyData == Keys.H)
            {
                if (!_isInMapSelect) TriggerAiHint();
                return true;
            }
            if (keyData == Keys.R)
            {
                if (!_isInMapSelect) StartNewGame(_model.Difficulty);
                return true;
            }
            if (keyData == Keys.Escape)
            {
                if (!_isInMapSelect)
                {
                    _isInMapSelect = true;
                    _showVictoryModal = false;
                    this.Invalidate();
                    return true;
                }
                else
                {
                    GoBackToList();
                    return true;
                }
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        // ==========================================
        // 헬퍼 및 비주얼 유틸리티
        // ==========================================
        private Point? GetGridCoordsAt(Point p)
        {
            if (!_boardBounds.Contains(p)) return null;

            int col = (int)((p.X - _boardBounds.Left) / _cellSize);
            int row = (int)((p.Y - _boardBounds.Top) / _cellSize);

            if (col >= 0 && col < _model.GridSize && row >= 0 && row < _model.GridSize)
            {
                return new Point(col, row);
            }
            return null;
        }

        private PointF GetNodeCenter(int c, int r)
        {
            return new PointF(
                _boardBounds.Left + c * _cellSize + _cellSize / 2f,
                _boardBounds.Top + r * _cellSize + _cellSize / 2f
            );
        }

        private void SpawnSparks(PointF pos, Color color, int count)
        {
            var rand = new Random();
            for (int i = 0; i < count; i++)
            {
                double angle = rand.NextDouble() * Math.PI * 2;
                double speed = 1.5 + rand.NextDouble() * 4.0;
                _particles.Add(new SparkParticle
                {
                    Position = pos,
                    Velocity = new PointF((float)(Math.Cos(angle) * speed), (float)(Math.Sin(angle) * speed)),
                    ParticleColor = color,
                    Life = 1.0f,
                    Size = 2.5f + (float)rand.NextDouble() * 2.5f
                });
            }
        }

        private void PlayTactileClickSound()
        {
            try
            {
                System.Media.SystemSounds.Asterisk.Play();
            }
            catch { }
        }

        private static void DrawRoundedRect(Graphics g, Pen pen, Rectangle bounds, int radius)
        {
            using (var path = CreateRoundedRectanglePath(bounds, radius))
            {
                g.DrawPath(pen, path);
            }
        }

        private static void FillRoundedRect(Graphics g, Brush brush, Rectangle bounds, int radius)
        {
            using (var path = CreateRoundedRectanglePath(bounds, radius))
            {
                g.FillPath(brush, path);
            }
        }

        private static GraphicsPath CreateRoundedRectanglePath(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            if (d > bounds.Width) d = bounds.Width;
            if (d > bounds.Height) d = bounds.Height;

            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}