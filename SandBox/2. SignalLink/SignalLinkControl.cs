using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace GateHelper
{
    /// <summary>
    /// 시그널 링크 (SignalLink) - 네온 사이버 회로 보드 퍼즐 컨트롤
    /// 제로 리소스(Zero-Resource) 원칙: 100% GDI+ 정적 캐싱, 0% 유휴 CPU, 스내피 궤적 보간
    /// </summary>
    public partial class SignalLinkControl : UserControl
    {
        // 핵심 게임 엔진
        private SignalManager _model;
        private Timer _renderTimer;
        private Timer _gameClockTimer;
        private int _playTimeSeconds = 0;

        // 드래그 상태
        private bool _isDragging = false;
        private int _activeColor = 0;
        private List<Point> _currentPath = new List<Point>();
        private Point? _hoveredGridPos = null;

        // 힌트 상태
        private bool _isHintActive = false;
        private int _hintColorId = 0;
        private List<Point> _hintPath = null;
        private float _hintRemainingSeconds = 0f;

        // 맵 규모 선택 화면 상태
        private bool _isInMapSelect = true;

        // 승리 모달 1초 지연 표시 상태
        private bool _showVictoryModal = false;
        private bool _isVictoryDelayPending = false;
        private float _victoryDelayTimer = 0f;

        // 이펙트 및 파티클
        private readonly List<ShockwaveEffect> _shockwaves = new List<ShockwaveEffect>();
        private readonly List<SparkParticle> _particles = new List<SparkParticle>();
        private readonly List<FloatingNotification> _floatingTexts = new List<FloatingNotification>();

        // 레이아웃 경계
        private Rectangle _boardBounds;
        private float _cellSize;
        private Point _mousePos;
        private string _hoverTooltipText = null;
        private float _globalPulse = 0f;

        // 헤더 버튼
        private Rectangle _btnBackRect;
        private Rectangle _btnModeRect;
        private Rectangle _btnUndoRect;
        private Rectangle _btnHintRect;
        private Rectangle _btnResetRect;

        // 맵 규모 선택 카드 (2x2 그리드)
        private Rectangle _mapCardEasy;
        private Rectangle _mapCardNormal;
        private Rectangle _mapCardHard;
        private Rectangle _mapCardExpert;
        private Rectangle _mapBtnEasy;
        private Rectangle _mapBtnNormal;
        private Rectangle _mapBtnHard;
        private Rectangle _mapBtnExpert;
        private Rectangle _btnMapSelectBack;

        // 승리 모달 버튼
        private Rectangle _modalNextRect;
        private Rectangle _modalRetryRect;
        private Rectangle _modalModeRect;
        private Rectangle _modalListRect;

        // ==========================================
        // 제로 리소스 GDI+ 정적 캐시 (GC 압박 0화)
        // ==========================================
        private static readonly Font _fontHeaderTitle = new Font("Segoe UI", 13f, FontStyle.Bold);
        private static readonly Font _fontHeaderSub = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        private static readonly Font _fontBtn = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        private static readonly Font _fontHudKey = new Font("Segoe UI", 8f, FontStyle.Regular);
        private static readonly Font _fontHudVal = new Font("Consolas", 11.5f, FontStyle.Bold);
        private static readonly Font _fontCardTitle = new Font("Segoe UI", 12.5f, FontStyle.Bold);
        private static readonly Font _fontCardBadge = new Font("Segoe UI", 8f, FontStyle.Bold);
        private static readonly Font _fontCardDesc = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        private static readonly Font _fontCardBullet = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        private static readonly Font _fontModalTitle = new Font("Segoe UI", 17f, FontStyle.Bold);
        private static readonly Font _fontModalSub = new Font("Segoe UI", 10f, FontStyle.Regular);
        private static readonly Font _fontModalStar = new Font("Segoe UI", 26f, FontStyle.Bold);
        private static readonly Font _fontModalStatKey = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        private static readonly Font _fontModalStatVal = new Font("Consolas", 10.5f, FontStyle.Bold);
        private static readonly Font _fontFloating = new Font("Segoe UI", 10.5f, FontStyle.Bold);
        private static readonly Font _fontBottomLegend = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        private static readonly Font _fontTerminalGlyph = new Font("Consolas", 9f, FontStyle.Bold);

        private static readonly StringFormat _sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        private static readonly StringFormat _sfHorizCenter = new StringFormat { Alignment = StringAlignment.Center };
        private static readonly StringFormat _sfFar = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
        private static readonly StringFormat _sfNear = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };

        // 12종 고대비 네온 신호 색상 팔레트
        private static readonly Color[] _signalColors = new Color[]
        {
            Color.FromArgb(120, 130, 145), // 0: None / Default
            Color.FromArgb(255, 75, 95),   // 1: Neon Crimson
            Color.FromArgb(0, 215, 255),   // 2: Electric Cyan
            Color.FromArgb(0, 255, 170),   // 3: Matrix Emerald
            Color.FromArgb(255, 215, 0),   // 4: Radiant Gold
            Color.FromArgb(195, 80, 255),  // 5: Quantum Purple
            Color.FromArgb(255, 135, 40),  // 6: Plasma Orange
            Color.FromArgb(255, 90, 200),  // 7: Cyber Pink
            Color.FromArgb(80, 255, 230),  // 8: Arctic Mint
            Color.FromArgb(170, 255, 50),  // 9: Acid Lime
            Color.FromArgb(240, 245, 255), // 10: Ghost White
            Color.FromArgb(255, 185, 90),  // 11: Amber Glow
            Color.FromArgb(140, 190, 255)  // 12: Deep Sky
        };

        public SignalLinkControl()
        {
            InitializeComponent();

            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            this.BackColor = Color.FromArgb(11, 15, 25);

            _model = new SignalManager();

            // 60 FPS 렌더링 타이머
            _renderTimer = new Timer();
            _renderTimer.Interval = 16;
            _renderTimer.Tick += OnRenderTick;
            _renderTimer.Start();

            // 게임 시계 타이머
            _gameClockTimer = new Timer();
            _gameClockTimer.Interval = 1000;
            _gameClockTimer.Tick += (s, e) =>
            {
                if (!_isInMapSelect && _model.IsGameActive && !_model.IsVictory)
                {
                    _playTimeSeconds++;
                    this.Invalidate();
                }
            };
            _gameClockTimer.Start();

            // 백그라운드 유휴 절전
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

        public void ShowMapSelection()
        {
            _isInMapSelect = true;
            _showVictoryModal = false;
            _isVictoryDelayPending = false;
            this.Invalidate();
        }

        private void RecalculateLayout()
        {
            int w = Math.Max(400, this.ClientSize.Width);
            int h = Math.Max(400, this.ClientSize.Height);

            // 1. 상단 컨트롤 버튼 (헤더)
            _btnBackRect = new Rectangle(14, 12, 70, 32);
            _btnModeRect = new Rectangle(_btnBackRect.Right + 8, 12, 74, 32);

            int actionBtnW = 74;
            int actionGap = 5;
            _btnResetRect = new Rectangle(w - 12 - actionBtnW, 10, actionBtnW, 32);
            _btnHintRect = new Rectangle(_btnResetRect.Left - actionGap - actionBtnW, 10, actionBtnW, 32);
            _btnUndoRect = new Rectangle(_btnHintRect.Left - actionGap - actionBtnW, 10, actionBtnW, 32);

            // 2. 중앙 보드 영역
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

            int btnH = 34;
            int btnPad = 12;
            _mapBtnEasy = new Rectangle(_mapCardEasy.Left + btnPad, _mapCardEasy.Bottom - btnH - btnPad, mapCardW - btnPad * 2, btnH);
            _mapBtnNormal = new Rectangle(_mapCardNormal.Left + btnPad, _mapCardNormal.Bottom - btnH - btnPad, mapCardW - btnPad * 2, btnH);
            _mapBtnHard = new Rectangle(_mapCardHard.Left + btnPad, _mapCardHard.Bottom - btnH - btnPad, mapCardW - btnPad * 2, btnH);
            _mapBtnExpert = new Rectangle(_mapCardExpert.Left + btnPad, _mapCardExpert.Bottom - btnH - btnPad, mapCardW - btnPad * 2, btnH);

            _btnMapSelectBack = new Rectangle(mapMarginX, row2Y + mapCardH + 18, w - mapMarginX * 2, 38);
        }

        // ==========================================
        // 렌더링 프레임 갱신 (Smart Idle Sleep)
        // ==========================================
        private void OnRenderTick(object sender, EventArgs e)
        {
            if (_isInMapSelect) return;

            bool isAnyAnimating = false;

            // 1. 승리 모달 1초 딜레이 타이머
            if (_isVictoryDelayPending && !_showVictoryModal)
            {
                _victoryDelayTimer += 0.016f;
                if (_victoryDelayTimer >= 1.0f)
                {
                    _showVictoryModal = true;
                    _isVictoryDelayPending = false;
                    PlayTactileClickSound();
                }
                isAnyAnimating = true;
            }

            // 2. 힌트 카운트다운 타이머
            if (_isHintActive)
            {
                _hintRemainingSeconds -= 0.016f;
                if (_hintRemainingSeconds <= 0f)
                {
                    _isHintActive = false;
                    _hintPath = null;
                }
                isAnyAnimating = true;
            }

            // 3. 노드 연결 펄스 감쇄
            for (int r = 0; r < _model.GridSize; r++)
            {
                for (int c = 0; c < _model.GridSize; c++)
                {
                    var node = _model.Grid[c, r];
                    if (node.ConnectPulse > 0f)
                    {
                        node.ConnectPulse -= 0.06f;
                        if (node.ConnectPulse < 0f) node.ConnectPulse = 0f;
                        isAnyAnimating = true;
                    }
                }
            }

            // 4. 쇼크웨이브
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

            // 5. 파티클
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

            // 6. 플로팅 알림 텍스트
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

            // 7. 글로벌 펄스 (전류 흐름 애니메이션)
            _globalPulse += 0.08f;
            if (_globalPulse > (float)Math.PI * 2) _globalPulse -= (float)Math.PI * 2;
            isAnyAnimating = true;

            if (_isDragging || isAnyAnimating)
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

                // 1. 미래지향적 사이버 배경
                DrawCyberBackground(g);

                // 맵 규모 선택 화면
                if (_isInMapSelect)
                {
                    DrawMapSelectScreen(g);
                    return;
                }

                // 2. 상단 네온 헤더 바
                DrawTopHeaderBar(g);

                // 3. 실시간 정보 HUD (시간, 동기화 쌍, 충진율)
                DrawStatusHud(g);

                // 4. 중앙 회로 격자판
                DrawCircuitBoard(g);

                // 5. 시각 효과 (쇼크웨이브, 파티클, 플로팅 텍스트)
                DrawVisualEffects(g);

                // 6. 하단 가이드 바
                DrawBottomLegendBar(g);

                // 7. 승리 완료 오버레이
                if (_showVictoryModal)
                {
                    DrawVictoryOverlay(g);
                }
            }
            catch
            {
                try { e.Graphics.Clear(Color.FromArgb(10, 14, 24)); } catch { }
            }
        }

        private void DrawCyberBackground(Graphics g)
        {
            int w = Math.Max(10, this.ClientSize.Width);
            int h = Math.Max(10, this.ClientSize.Height);

            using (var bgBrush = new LinearGradientBrush(new Rectangle(0, 0, w, h),
                Color.FromArgb(10, 14, 24), Color.FromArgb(16, 22, 38), 90f))
            {
                g.FillRectangle(bgBrush, 0, 0, w, h);
            }

            if (_boardBounds.Width > 0 && _boardBounds.Height > 0)
            {
                using (var path = new GraphicsPath())
                {
                    path.AddEllipse(_boardBounds.Left - 40, _boardBounds.Top - 40, _boardBounds.Width + 80, _boardBounds.Height + 80);
                    using (var pgb = new PathGradientBrush(path))
                    {
                        pgb.CenterColor = Color.FromArgb(20, 0, 215, 255);
                        pgb.SurroundColors = new[] { Color.FromArgb(0, 0, 0, 0) };
                        g.FillPath(pgb, path);
                    }
                }
            }
        }

        // ==========================================
        // 맵 규모 선택 화면 (2x2 그리드 글래스모피즘)
        // ==========================================
        private void DrawMapSelectScreen(Graphics g)
        {
            int w = this.ClientSize.Width;

            using (var titleBrush = new SolidBrush(Color.FromArgb(0, 245, 212)))
            using (var subBrush = new SolidBrush(Color.FromArgb(160, 185, 210)))
            {
                g.DrawString("⚡ SIGNAL LINK : 전술 회로망 규모 선택", _fontHeaderTitle, titleBrush, w / 2f, 18, _sfHorizCenter);
                g.DrawString("신호를 복호화하고 연결할 네트워크 격자 규모와 보안 프로토콜을 선택하십시오.", _fontHeaderSub, subBrush, w / 2f, 48, _sfHorizCenter);
            }

            DrawMapCard(g, _mapCardEasy, _mapBtnEasy, 0, "5×5 입문 회로망", "EASY", Color.FromArgb(0, 245, 212),
                "기본 신호선 3쌍", "초급 보안 버스", "빠른 흐름 파악", "★☆☆ 입문");

            DrawMapCard(g, _mapCardNormal, _mapBtnNormal, 1, "6×6 전술 버스", "NORMAL", Color.FromArgb(0, 210, 255),
                "고밀도 신호선 4쌍", "다중 라우팅 필요", "표준 작전 규격", "★★☆ 추천");

            DrawMapCard(g, _mapCardHard, _mapBtnHard, 2, "8×8 고밀도 코어", "HARD", Color.FromArgb(255, 175, 40),
                "복합 신호선 6쌍", "정밀 공간 분할", "상급 보안 매트릭스", "★★★ 도전");

            DrawMapCard(g, _mapCardExpert, _mapBtnExpert, 3, "10×10 스텔스 암기", "EXPERT", Color.FromArgb(255, 65, 120),
                "대형 신호선 7쌍", "⚡ 스텔스 암기 모드", "호버 시만 색상 노출", "★★★ 마스터");

            bool isBackHover = _btnMapSelectBack.Contains(_mousePos);
            DrawCyberButton(g, _btnMapSelectBack, "◀ 샌드박스 메인으로 돌아가기", Color.FromArgb(140, 160, 190), isBackHover);
        }

        private void DrawMapCard(Graphics g, Rectangle cardRect, Rectangle btnRect, int index, string title, string badge, Color themeColor, string b1, string b2, string b3, string stars)
        {
            bool isHovered = cardRect.Contains(_mousePos);
            Color bgCol = isHovered ? Color.FromArgb(220, 20, 32, 54) : Color.FromArgb(190, 15, 23, 38);
            Color borderCol = isHovered ? themeColor : Color.FromArgb(40, 60, 95);

            using (var brush = new SolidBrush(bgCol))
            using (var pen = new Pen(borderCol, isHovered ? 2.0f : 1.0f))
            {
                FillRoundedRect(g, brush, cardRect, 10);
                DrawRoundedRect(g, pen, cardRect, 10);
            }

            Rectangle badgeRect = new Rectangle(cardRect.Left + 14, cardRect.Top + 14, 66, 22);
            using (var badgeBg = new SolidBrush(Color.FromArgb(40, themeColor)))
            using (var badgeBorder = new Pen(themeColor, 1.0f))
            using (var badgeText = new SolidBrush(themeColor))
            {
                FillRoundedRect(g, badgeBg, badgeRect, 4);
                DrawRoundedRect(g, badgeBorder, badgeRect, 4);
                g.DrawString(badge, _fontCardBadge, badgeText, badgeRect, _sfCenter);
            }

            using (var titleBrush = new SolidBrush(isHovered ? Color.White : Color.FromArgb(235, 245, 255)))
            {
                g.DrawString(title, _fontCardTitle, titleBrush, badgeRect.Right + 10, cardRect.Top + 13);
            }

            int lineY = cardRect.Top + 44;
            using (var linePen = new Pen(Color.FromArgb(30, 45, 75), 1.0f))
            {
                g.DrawLine(linePen, cardRect.Left + 14, lineY, cardRect.Right - 14, lineY);
            }

            int bulletY = lineY + 8;
            using (var bulletBrush = new SolidBrush(Color.FromArgb(160, 180, 205)))
            using (var starBrush = new SolidBrush(Color.FromArgb(255, 215, 0)))
            {
                g.DrawString($"•  {b1}", _fontCardBullet, bulletBrush, cardRect.Left + 16, bulletY);
                g.DrawString($"•  {b2}", _fontCardBullet, bulletBrush, cardRect.Left + 16, bulletY + 18);
                g.DrawString($"•  {b3}", _fontCardBullet, bulletBrush, cardRect.Left + 16, bulletY + 36);
                g.DrawString(stars, _fontCardBadge, starBrush, cardRect.Right - 80, bulletY);
            }

            bool isBtnHover = btnRect.Contains(_mousePos);
            DrawCyberButton(g, btnRect, "작전 개시 ▶", themeColor, isBtnHover);
        }

        private void DrawTopHeaderBar(Graphics g)
        {
            DrawCyberButton(g, _btnBackRect, "◀ LIST", Color.FromArgb(120, 140, 170), _btnBackRect.Contains(_mousePos));
            DrawCyberButton(g, _btnModeRect, "🌐 MAP", Color.FromArgb(0, 220, 255), _btnModeRect.Contains(_mousePos));

            bool canUndo = _model.UndoStack.Count > 0 && !_model.IsVictory;
            Color undoColor = canUndo ? Color.FromArgb(255, 170, 0) : Color.FromArgb(70, 75, 85);
            string undoText = canUndo ? $"↶ UNDO ({_model.UndoStack.Count})" : "↶ UNDO";
            DrawCyberButton(g, _btnUndoRect, undoText, undoColor, canUndo && _btnUndoRect.Contains(_mousePos), canUndo);

            Color hintColor = Color.FromArgb(0, 245, 180);
            DrawCyberButton(g, _btnHintRect, "💡 HINT", hintColor, _btnHintRect.Contains(_mousePos));
            DrawCyberButton(g, _btnResetRect, "↺ RESET", Color.FromArgb(0, 200, 255), _btnResetRect.Contains(_mousePos));
        }

        private void DrawStatusHud(Graphics g)
        {
            int w = this.ClientSize.Width;
            Rectangle hudRect = new Rectangle(14, 52, w - 28, 48);

            using (var bgBrush = new SolidBrush(Color.FromArgb(180, 16, 23, 37)))
            using (var borderPen = new Pen(Color.FromArgb(40, 60, 95), 1f))
            {
                FillRoundedRect(g, bgBrush, hudRect, 8);
                DrawRoundedRect(g, borderPen, hudRect, 8);
            }

            int sectionW = hudRect.Width / 4;
            string timeStr = string.Format("{0:D2}:{1:D2}", _playTimeSeconds / 60, _playTimeSeconds % 60);
            DrawHudMetric(g, new Rectangle(hudRect.Left, hudRect.Top, sectionW, hudRect.Height), "⏱️ 경과 시간", timeStr, Color.FromArgb(0, 230, 255));

            int connectedCount = _model.CompletedPaths.Count;
            Color pairCol = connectedCount == _model.PairCount ? Color.FromArgb(0, 255, 170) : Color.FromArgb(0, 220, 255);
            DrawHudMetric(g, new Rectangle(hudRect.Left + sectionW, hudRect.Top, sectionW, hudRect.Height), "⚡ 신호 동기화", $"{connectedCount} / {_model.PairCount} 쌍", pairCol);

            int cov = _model.GetCoveragePercentage();
            Color covCol = cov == 100 ? Color.FromArgb(0, 255, 170) : (cov >= 75 ? Color.FromArgb(255, 200, 50) : Color.FromArgb(170, 190, 215));
            DrawHudMetric(g, new Rectangle(hudRect.Left + sectionW * 2, hudRect.Top, sectionW, hudRect.Height), "🔋 격자 충진율", $"{cov}%", covCol);

            string modeTitle = _model.IsStealthMode ? "🔒 스텔스 암기" : "⚡ 표준 회로";
            Color modeCol = _model.IsStealthMode ? Color.FromArgb(255, 65, 120) : Color.FromArgb(0, 245, 212);
            DrawHudMetric(g, new Rectangle(hudRect.Left + sectionW * 3, hudRect.Top, sectionW, hudRect.Height), "🛡️ 작전 모드", modeTitle, modeCol);
        }

        private void DrawHudMetric(Graphics g, Rectangle r, string label, string val, Color valColor)
        {
            using (var lblBrush = new SolidBrush(Color.FromArgb(140, 155, 175)))
            using (var valBrush = new SolidBrush(valColor))
            {
                g.DrawString(label, _fontHudKey, lblBrush, r.Left + r.Width / 2, r.Top + 7, _sfHorizCenter);
                g.DrawString(val, _fontHudVal, valBrush, r.Left + r.Width / 2, r.Top + 23, _sfHorizCenter);
            }
        }

        // ==========================================
        // 중앙 회로 보드 및 신호 경로 렌더링
        // ==========================================
        private void DrawCircuitBoard(Graphics g)
        {
            using (var bgBrush = new SolidBrush(Color.FromArgb(200, 12, 17, 28)))
            using (var framePen = new Pen(Color.FromArgb(30, 45, 75), 1.5f))
            {
                FillRoundedRect(g, bgBrush, _boardBounds, 12);
                DrawRoundedRect(g, framePen, _boardBounds, 12);
            }

            // 1. 격자 슬롯 배경
            using (var gridPen = new Pen(Color.FromArgb(24, 34, 54), 1f))
            {
                for (int i = 0; i <= _model.GridSize; i++)
                {
                    float y = _boardBounds.Top + i * _cellSize;
                    g.DrawLine(gridPen, _boardBounds.Left, y, _boardBounds.Right, y);

                    float x = _boardBounds.Left + i * _cellSize;
                    g.DrawLine(gridPen, x, _boardBounds.Top, x, _boardBounds.Bottom);
                }
            }

            // 2. AI 힌트 경로 비콘 발광 렌더링
            if (_isHintActive && _hintPath != null && _hintPath.Count > 1)
            {
                Color hintCol = GetSignalBaseColor(_hintColorId);
                float pulse = 0.5f + 0.5f * (float)Math.Sin(_globalPulse * 3f);
                int alpha = Math.Max(0, Math.Min(255, (int)(90 + 110 * pulse)));
                DrawSmoothPipe(g, _hintPath, Color.FromArgb(alpha, hintCol), _cellSize * 0.40f, isDotted: true);
            }

            // 3. 이미 완성된 신호선 렌더링
            foreach (var kvp in _model.CompletedPaths)
            {
                int colorId = kvp.Key;
                var path = kvp.Value;
                if (path.Count < 2) continue;

                bool isPeeking = !_model.IsStealthMode || _model.IsVictory || (_isDragging && _activeColor == colorId);
                Color wireColor = isPeeking ? GetSignalBaseColor(colorId) : Color.FromArgb(120, 130, 145);

                DrawSmoothPipe(g, path, wireColor, _cellSize * 0.36f);
            }

            // 4. 현재 실시간 드래깅 중인 선로 렌더링
            if (_isDragging && _currentPath.Count > 1)
            {
                Color activeCol = GetSignalBaseColor(_activeColor);
                DrawSmoothPipe(g, _currentPath, activeCol, _cellSize * 0.40f, isCurrent: true);
            }

            // 5. 각 칸의 터미널 노드 렌더링
            for (int r = 0; r < _model.GridSize; r++)
            {
                for (int c = 0; c < _model.GridSize; c++)
                {
                    var node = _model.Grid[c, r];
                    if (node.Type == NodeType.Terminal)
                    {
                        DrawTerminalNode(g, node);
                    }
                }
            }
        }

        private void DrawSmoothPipe(Graphics g, List<Point> path, Color baseColor, float thickness, bool isDotted = false, bool isCurrent = false)
        {
            if (path == null || path.Count < 2) return;

            PointF[] pts = new PointF[path.Count];
            for (int i = 0; i < path.Count; i++)
            {
                pts[i] = GetNodeCenter(path[i].X, path[i].Y);
            }

            // 네온 외곽 글로우
            using (var glowPen = new Pen(Color.FromArgb(45, baseColor), thickness + 6f))
            {
                glowPen.StartCap = LineCap.Round;
                glowPen.EndCap = LineCap.Round;
                glowPen.LineJoin = LineJoin.Round;
                g.DrawLines(glowPen, pts);
            }

            // 코어 전류 파이프
            using (var corePen = new Pen(baseColor, thickness))
            {
                corePen.StartCap = LineCap.Round;
                corePen.EndCap = LineCap.Round;
                corePen.LineJoin = LineJoin.Round;
                if (isDotted) corePen.DashStyle = DashStyle.Dash;
                g.DrawLines(corePen, pts);
            }

            // 전류 하이라이트 중심선
            using (var centerPen = new Pen(Color.FromArgb(200, 255, 255, 255), Math.Max(2f, thickness * 0.28f)))
            {
                centerPen.StartCap = LineCap.Round;
                centerPen.EndCap = LineCap.Round;
                centerPen.LineJoin = LineJoin.Round;
                g.DrawLines(centerPen, pts);
            }
        }

        private void DrawTerminalNode(Graphics g, SignalNode node)
        {
            PointF center = GetNodeCenter(node.Pos.X, node.Pos.Y);
            float radius = _cellSize * 0.35f;

            // 스텔스 모드 피킹 검사
            bool isHover = _hoveredGridPos.HasValue && _hoveredGridPos.Value == node.Pos;
            bool isCompleted = _model.CompletedPaths.ContainsKey(node.ColorID);
            bool isPeeking = !_model.IsStealthMode || _model.IsVictory || isHover || isCompleted || (_isDragging && _activeColor == node.ColorID);

            Color nodeColor = isPeeking ? GetSignalBaseColor(node.ColorID) : Color.FromArgb(75, 85, 100);

            // 연결 펄스 애니메이션 확대
            float pulseScale = 1.0f + node.ConnectPulse * 0.35f;
            radius *= pulseScale;

            // 터미널 외곽 마이크로칩 링
            using (var ringPen = new Pen(nodeColor, 2.5f))
            {
                g.DrawEllipse(ringPen, center.X - radius, center.Y - radius, radius * 2, radius * 2);
            }

            // 터미널 내부 발광 코어
            using (var coreBrush = new SolidBrush(Color.FromArgb(isPeeking ? 180 : 90, nodeColor)))
            {
                g.FillEllipse(coreBrush, center.X - radius * 0.75f, center.Y - radius * 0.75f, radius * 1.5f, radius * 1.5f);
            }

            // 중앙 LED 칩
            using (var ledBrush = new SolidBrush(isPeeking ? Color.White : Color.FromArgb(170, 185, 205)))
            {
                g.FillEllipse(ledBrush, center.X - radius * 0.32f, center.Y - radius * 0.32f, radius * 0.64f, radius * 0.64f);
            }

            // 스텔스 모드 암기 기호 표출
            if (!isPeeking)
            {
                using (var glyphBrush = new SolidBrush(Color.FromArgb(180, 200, 220)))
                {
                    g.DrawString("?", _fontTerminalGlyph, glyphBrush, center.X, center.Y, _sfCenter);
                }
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

            // 2. 파티클
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

            string guide = _hoverTooltipText ?? (_model.IsStealthMode
                ? "💡 스텔스 암기 모드: 노드에 호버하거나 드래그하여 신호 정체를 복호화하고 기억하십시오! (Z: 언도, H: 힌트)"
                : "💡 팁: 동일한 색상의 단말을 드래그하여 연결하십시오. 전 구역을 100% 충진해야 합니다. (Z: 언도, H: 힌트)");

            using (var brush = new SolidBrush(_hoverTooltipText != null ? Color.FromArgb(0, 255, 210) : Color.FromArgb(120, 140, 165)))
            {
                g.DrawString(guide, _fontBottomLegend, brush, new Rectangle(14, h - 38, w - 28, 28), _sfCenter);
            }
        }

        private void DrawVictoryOverlay(Graphics g)
        {
            int w = this.ClientSize.Width;
            int h = this.ClientSize.Height;

            using (var dimBrush = new SolidBrush(Color.FromArgb(220, 8, 12, 22)))
            {
                g.FillRectangle(dimBrush, 0, 0, w, h);
            }

            int modalW = 420;
            int modalH = 430;
            Rectangle modalRect = new Rectangle((w - modalW) / 2, (h - modalH) / 2, modalW, modalH);

            using (var boxBrush = new SolidBrush(Color.FromArgb(240, 16, 24, 40)))
            using (var borderPen = new Pen(Color.FromArgb(0, 245, 212), 2.0f))
            {
                FillRoundedRect(g, boxBrush, modalRect, 14);
                DrawRoundedRect(g, borderPen, modalRect, 14);
            }

            using (var titleBrush = new SolidBrush(Color.FromArgb(0, 245, 212)))
            using (var subBrush = new SolidBrush(Color.FromArgb(170, 190, 215)))
            {
                g.DrawString("NETWORK SYNCHRONIZED", _fontModalTitle, titleBrush, modalRect.Left + modalW / 2, modalRect.Top + 24, _sfHorizCenter);
                g.DrawString("모든 신호 회로망 완전 동기화 성공!", _fontModalSub, subBrush, modalRect.Left + modalW / 2, modalRect.Top + 58, _sfHorizCenter);
            }

            int stars = _model.GetStarRating();
            string starGlyph = stars == 3 ? "★ ★ ★" : (stars == 2 ? "★ ★ ☆" : "★ ☆ ☆");
            using (var starBrush = new SolidBrush(Color.FromArgb(255, 215, 0)))
            {
                g.DrawString(starGlyph, _fontModalStar, starBrush, modalRect.Left + modalW / 2, modalRect.Top + 90, _sfHorizCenter);
            }

            int statsY = modalRect.Top + 145;
            DrawModalStatRow(g, modalRect.Left + 40, statsY, modalW - 80, "클리어 시간", string.Format("{0:D2}:{1:D2}", _playTimeSeconds / 60, _playTimeSeconds % 60));
            DrawModalStatRow(g, modalRect.Left + 40, statsY + 32, modalW - 80, "회로 충진율", "100% 완전 동기화");
            DrawModalStatRow(g, modalRect.Left + 40, statsY + 64, modalW - 80, "AI 힌트 사용", $"{_model.HintsUsed}회");
            string rankTitle = stars == 3 ? "S+ QUANTUM MASTER" : (stars == 2 ? "A+ PROTOCOL EXPERT" : "B SIGNAL LINKER");
            DrawModalStatRow(g, modalRect.Left + 40, statsY + 96, modalW - 80, "최종 랭크", rankTitle);

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

        // ==========================================
        // 마우스 상호작용 및 고속 궤적 보간(Bresenham)
        // ==========================================
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (_showVictoryModal)
            {
                if (_modalNextRect.Contains(e.Location)) { AdvanceToNextDifficulty(); return; }
                if (_modalRetryRect.Contains(e.Location)) { StartNewGame(_model.Difficulty); return; }
                if (_modalModeRect.Contains(e.Location)) { _isInMapSelect = true; _showVictoryModal = false; this.Invalidate(); return; }
                if (_modalListRect.Contains(e.Location)) { GoBackToList(); return; }
                return;
            }

            if (_isInMapSelect)
            {
                if (_mapBtnEasy.Contains(e.Location)) { StartNewGame("Easy"); return; }
                if (_mapBtnNormal.Contains(e.Location)) { StartNewGame("Normal"); return; }
                if (_mapBtnHard.Contains(e.Location)) { StartNewGame("Hard"); return; }
                if (_mapBtnExpert.Contains(e.Location)) { StartNewGame("Expert"); return; }
                if (_btnMapSelectBack.Contains(e.Location)) { GoBackToList(); return; }
                return;
            }

            if (_btnBackRect.Contains(e.Location)) { GoBackToList(); return; }
            if (_btnModeRect.Contains(e.Location)) { _isInMapSelect = true; this.Invalidate(); return; }
            if (_btnResetRect.Contains(e.Location)) { StartNewGame(_model.Difficulty); return; }
            if (_btnUndoRect.Contains(e.Location)) { TriggerUndo(); return; }
            if (_btnHintRect.Contains(e.Location)) { TriggerAiHint(); return; }

            // 회로 노드 클릭 시작
            Point? pt = GetGridCoordsAt(e.Location);
            if (pt.HasValue && _model.IsGameActive)
            {
                var clickedNode = _model.Grid[pt.Value.X, pt.Value.Y];

                // 1. 단말 노드 클릭 시: 해당 색상 경로 드래그 개시
                if (clickedNode.Type == NodeType.Terminal)
                {
                    _isDragging = true;
                    _activeColor = clickedNode.ColorID;
                    _model.RemovePath(_activeColor); // 기존 선로가 있었다면 초기화
                    _currentPath.Clear();
                    _currentPath.Add(pt.Value);
                    PlayTactileClickSound();
                    this.Invalidate();
                    return;
                }

                // 2. 이미 그려진 경로 중간 클릭 시: 해당 지점까지 즉시 잘라내기(Trimming) 및 계속 잇기
                foreach (var kvp in _model.CompletedPaths)
                {
                    int colorId = kvp.Key;
                    int idx = kvp.Value.IndexOf(pt.Value);
                    if (idx >= 0)
                    {
                        _isDragging = true;
                        _activeColor = colorId;
                        _currentPath = kvp.Value.Take(idx + 1).ToList();
                        _model.RemovePath(colorId);
                        PlayTactileClickSound();
                        this.Invalidate();
                        return;
                    }
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            _mousePos = e.Location;

            Point? pt = GetGridCoordsAt(e.Location);
            _hoveredGridPos = pt;

            if (_isInMapSelect || !_model.IsGameActive)
            {
                this.Invalidate();
                return;
            }

            if (_isDragging && pt.HasValue)
            {
                Point targetPt = pt.Value;
                Point lastPt = _currentPath[_currentPath.Count - 1];

                // 고속 드래그 시 대각선/다중 칸 건너뜀을 해결하는 고속 궤적 보간 (Bresenham)
                if (targetPt != lastPt)
                {
                    ProcessPathStep(lastPt, targetPt);
                }
            }

            this.Invalidate();
        }

        private void ProcessPathStep(Point from, Point to)
        {
            // 되돌리기: 이전 칸으로 복귀하면 마지막 칸 삭제
            if (_currentPath.Count > 1 && to == _currentPath[_currentPath.Count - 2])
            {
                _currentPath.RemoveAt(_currentPath.Count - 1);
                PlayTactileClickSound();
                return;
            }

            // 거리 차이
            int dx = to.X - from.X;
            int dy = to.Y - from.Y;

            // 단일 맨해튼 스텝 이동
            if (Math.Abs(dx) + Math.Abs(dy) == 1)
            {
                TryAddNodeToCurrentPath(to);
            }
            else
            {
                // 고속 이동 시 중간 노드 연속 보간 (X축 먼저 시도 후 Y축 이동)
                int stepX = Math.Sign(dx);
                int stepY = Math.Sign(dy);

                Point curr = from;
                while (curr.X != to.X)
                {
                    Point next = new Point(curr.X + stepX, curr.Y);
                    if (!TryAddNodeToCurrentPath(next)) return;
                    curr = next;
                }
                while (curr.Y != to.Y)
                {
                    Point next = new Point(curr.X, curr.Y + stepY);
                    if (!TryAddNodeToCurrentPath(next)) return;
                    curr = next;
                }
            }
        }

        private bool TryAddNodeToCurrentPath(Point pt)
        {
            if (pt.X < 0 || pt.X >= _model.GridSize || pt.Y < 0 || pt.Y >= _model.GridSize) return false;

            // 1. 자기 자신 경로와의 충돌 (이미 현재 경로에 포함된 칸이면 루프 방지)
            int existingIdx = _currentPath.IndexOf(pt);
            if (existingIdx >= 0)
            {
                // 이전 지점으로 되감기 (Rollback)
                _currentPath = _currentPath.Take(existingIdx + 1).ToList();
                return true;
            }

            // 2. 다른 완성된 신호선과의 충돌
            foreach (var kvp in _model.CompletedPaths)
            {
                if (kvp.Key == _activeColor) continue;
                if (kvp.Value.Contains(pt)) return false; // 막힘
            }

            // 3. 다른 색상 터미널 덮어쓰기 금지
            var node = _model.Grid[pt.X, pt.Y];
            if (node.Type == NodeType.Terminal && node.ColorID != _activeColor) return false;

            _currentPath.Add(pt);

            // 4. 목표 단말에 도달 시 즉시 연결 확정
            if (node.Type == NodeType.Terminal && node.ColorID == _activeColor && _currentPath.Count > 1)
            {
                FinishCurrentConnection();
                return true;
            }

            return true;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (_isDragging)
            {
                FinishCurrentConnection();
            }
        }

        private void FinishCurrentConnection()
        {
            if (_isDragging && _currentPath.Count >= 2)
            {
                if (_model.CommitPath(_activeColor, _currentPath))
                {
                    Point endPt = _currentPath.Last();
                    PointF center = GetNodeCenter(endPt.X, endPt.Y);

                    // 연결 성공 쇼크웨이브 방출
                    _shockwaves.Add(new ShockwaveEffect
                    {
                        Center = center,
                        Radius = 10f,
                        MaxRadius = _cellSize * 1.6f,
                        WaveColor = GetSignalBaseColor(_activeColor)
                    });

                    // 스파크 파티클
                    SpawnSparks(center, GetSignalBaseColor(_activeColor), 14);

                    // 플로팅 알림
                    _floatingTexts.Add(new FloatingNotification
                    {
                        Position = new PointF(center.X, center.Y - 15),
                        Text = $"⚡ SIGNAL #{_activeColor} LINKED!",
                        TextColor = GetSignalBaseColor(_activeColor)
                    });

                    PlayTactileClickSound();

                    // 승리 체크
                    if (_model.IsVictory && !_showVictoryModal && !_isVictoryDelayPending)
                    {
                        _isVictoryDelayPending = true;
                        _victoryDelayTimer = 0f;

                        PointF boardCenter = new PointF(_boardBounds.Left + _boardBounds.Width / 2f, _boardBounds.Top + _boardBounds.Height / 2f);
                        _shockwaves.Add(new ShockwaveEffect
                        {
                            Center = boardCenter,
                            Radius = 20f,
                            MaxRadius = _boardBounds.Width * 0.95f,
                            WaveColor = Color.FromArgb(0, 245, 212)
                        });
                    }
                }
            }

            _isDragging = false;
            _currentPath.Clear();
            this.Invalidate();
        }

        private void TriggerUndo()
        {
            if (_model.Undo())
            {
                _floatingTexts.Add(new FloatingNotification
                {
                    Position = new PointF(this.ClientSize.Width / 2f, _boardBounds.Top - 20),
                    Text = "↶ 신호 연결 취소 완료",
                    TextColor = Color.FromArgb(255, 180, 50)
                });
                PlayTactileClickSound();
                this.Invalidate();
            }
        }

        private void TriggerAiHint()
        {
            int? hintColor = _model.RequestAiHint(out var hintPath);
            if (hintColor.HasValue && hintPath != null)
            {
                _isHintActive = true;
                _hintColorId = hintColor.Value;
                _hintPath = hintPath;
                _hintRemainingSeconds = 3.5f;

                PointF center = GetNodeCenter(hintPath[0].X, hintPath[0].Y);
                _shockwaves.Add(new ShockwaveEffect
                {
                    Center = center,
                    Radius = 15f,
                    MaxRadius = _cellSize * 1.8f,
                    WaveColor = GetSignalBaseColor(_hintColorId)
                });

                _floatingTexts.Add(new FloatingNotification
                {
                    Position = new PointF(this.ClientSize.Width / 2f, _boardBounds.Top - 20),
                    Text = $"💡 AI 회로 스캔: 신호 #{_hintColorId} 최적 경로 감지!",
                    TextColor = GetSignalBaseColor(_hintColorId)
                });

                PlayTactileClickSound();
                this.Invalidate();
            }
            else
            {
                _floatingTexts.Add(new FloatingNotification
                {
                    Position = new PointF(this.ClientSize.Width / 2f, _boardBounds.Top - 20),
                    Text = "💡 모든 신호가 이미 연결되어 있습니다.",
                    TextColor = Color.FromArgb(170, 185, 205)
                });
            }
        }

        public void StartNewGame(string diff)
        {
            _isInMapSelect = false;
            _isDragging = false;
            _currentPath.Clear();
            _isHintActive = false;
            _hintPath = null;
            _model.InitializeGrid(diff);
            _playTimeSeconds = 0;
            _showVictoryModal = false;
            _isVictoryDelayPending = false;
            _shockwaves.Clear();
            _particles.Clear();
            _floatingTexts.Clear();
            RecalculateLayout();
            this.Invalidate();
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

        private void GoBackToList()
        {
            if (this.ParentForm is SandBox sb)
            {
                sb.BackToList();
            }
        }

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
        // 헬퍼 유틸리티
        // ==========================================
        private PointF GetNodeCenter(int c, int r)
        {
            return new PointF(
                _boardBounds.Left + c * _cellSize + _cellSize / 2f,
                _boardBounds.Top + r * _cellSize + _cellSize / 2f
            );
        }

        private Point? GetGridCoordsAt(Point p)
        {
            if (!_boardBounds.Contains(p) || _cellSize <= 0) return null;
            int col = (int)((p.X - _boardBounds.Left) / _cellSize);
            int row = (int)((p.Y - _boardBounds.Top) / _cellSize);

            if (col >= 0 && col < _model.GridSize && row >= 0 && row < _model.GridSize)
                return new Point(col, row);

            return null;
        }

        private Color GetSignalBaseColor(int colorId)
        {
            if (colorId >= 1 && colorId < _signalColors.Length)
                return _signalColors[colorId];
            return _signalColors[0];
        }

        private void SpawnSparks(PointF center, Color color, int count)
        {
            var rand = new Random();
            for (int i = 0; i < count; i++)
            {
                double angle = rand.NextDouble() * Math.PI * 2;
                float speed = 1.5f + (float)rand.NextDouble() * 3.5f;
                _particles.Add(new SparkParticle
                {
                    Position = center,
                    Velocity = new PointF((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed),
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
            GraphicsPath path = new GraphicsPath();
            int diameter = Math.Max(2, radius * 2);
            Rectangle arc = new Rectangle(bounds.Location, new Size(diameter, diameter));

            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}