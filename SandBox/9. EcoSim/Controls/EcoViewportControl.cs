using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using GateHelper.EcoSim.Models;
using GateHelper.EcoSim.Rendering;
using GateHelper.EcoSim.Services;

namespace GateHelper.EcoSim.Controls
{
    /// <summary>
    /// 생태계 개체와 먹이, 선택 시야 반경을 렌더링하고 마우스 개입 입력을 받는 GDI+ 뷰포트.
    /// </summary>
    public sealed class EcoViewportControl : Control
    {
        private EcoSimulationEngine _engine;
        private EcoToolMode _toolMode = EcoToolMode.Inspect;
        private int _selectedOrganismId = -1;
        private int _selectedHouseId = -1;
        private bool _isMouseDown;
        private int _lastWeatherTick = -1;
        private float _playerCameraZoom = 1.85f;

        private struct WeatherParticle
        {
            public float X;
            public float Y;
            public float Vx;
            public float Vy;
            public float Size;
        }

        private readonly WeatherParticle[] _weatherParticles = new WeatherParticle[24];

        public event Action<int> OrganismSelected;
        public event Action<int> HouseSelected;
        public event Action WorldMutatedByUser;

        public EcoViewportControl()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
            BackColor = Color.FromArgb(18, 22, 30);
            InitWeatherParticles();
        }

        private void InitWeatherParticles()
        {
            var rng = new Random(77);
            for (int i = 0; i < _weatherParticles.Length; i++)
            {
                _weatherParticles[i] = new WeatherParticle
                {
                    X = (float)rng.NextDouble(),
                    Y = (float)rng.NextDouble(),
                    Vx = ((float)rng.NextDouble() - 0.35f) * 0.0025f,
                    Vy = 0.0018f + (float)rng.NextDouble() * 0.0024f,
                    Size = 2.4f + (float)rng.NextDouble() * 2.2f
                };
            }
        }

        public void BindEngine(EcoSimulationEngine engine)
        {
            _engine = engine;
            Invalidate();
        }

        public void SetToolMode(EcoToolMode mode)
        {
            _toolMode = mode;
            Cursor = mode == EcoToolMode.Inspect ? Cursors.Hand : Cursors.Cross;
        }

        public void SetSelectedOrganism(int organismId)
        {
            _selectedOrganismId = organismId;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (_engine == null || e.Button != MouseButtons.Left) return;
            _isMouseDown = true;
            HandleMouseAction(e.X, e.Y, isInitialClick: true);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!_isMouseDown || _engine == null) return;
            if (_toolMode == EcoToolMode.DropFood)
            {
                HandleMouseAction(e.X, e.Y, isInitialClick: false);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isMouseDown = false;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (_engine == null || !_engine.IsPlayerMode) return;
            _playerCameraZoom += e.Delta > 0 ? 0.15f : -0.15f;
            if (_playerCameraZoom < 1.2f) _playerCameraZoom = 1.2f;
            else if (_playerCameraZoom > 2.6f) _playerCameraZoom = 2.6f;
            Invalidate();
        }

        private void GetCameraTransform(out float viewLeft, out float viewTop, out float viewW, out float viewH, out float sx, out float sy, out float scaleAvg)
        {
            float worldW = _engine.Config.WorldWidth;
            float worldH = _engine.Config.WorldHeight;
            Organism player = _engine.IsPlayerMode ? _engine.FindOrganismById(_engine.PlayerId) : null;

            if (player != null && _playerCameraZoom > 1.01f)
            {
                viewW = worldW / _playerCameraZoom;
                viewH = worldH / _playerCameraZoom;
                viewLeft = player.X - viewW * 0.5f;
                viewTop = player.Y - viewH * 0.5f;
                if (viewLeft < 0f) viewLeft = 0f;
                else if (viewLeft > worldW - viewW) viewLeft = worldW - viewW;
                if (viewTop < 0f) viewTop = 0f;
                else if (viewTop > worldH - viewH) viewTop = worldH - viewH;
            }
            else
            {
                viewLeft = 0f;
                viewTop = 0f;
                viewW = worldW;
                viewH = worldH;
            }

            sx = Width / viewW;
            sy = Height / viewH;
            scaleAvg = (sx + sy) * 0.5f;
        }

        private void HandleMouseAction(int px, int py, bool isInitialClick)
        {
            if (Width <= 0 || Height <= 0 || _engine == null) return;

            GetCameraTransform(out float viewLeft, out float viewTop, out _, out _, out float sx, out float sy, out _);
            float wx = viewLeft + px / sx;
            float wy = viewTop + py / sy;

            if (_toolMode == EcoToolMode.Inspect)
            {
                if (!isInitialClick) return;
                var found = _engine.FindOrganismAt(wx, wy, 14f);
                if (found != null)
                {
                    _selectedOrganismId = found.Id;
                    _selectedHouseId = -1;
                    OrganismSelected?.Invoke(_selectedOrganismId);
                }
                else
                {
                    int houseId = _engine.FindHouseAt(wx, wy, 14f);
                    if (houseId > 0)
                    {
                        _selectedOrganismId = -1;
                        _selectedHouseId = houseId;
                        HouseSelected?.Invoke(_selectedHouseId);
                    }
                    else
                    {
                        _selectedOrganismId = -1;
                        _selectedHouseId = -1;
                        OrganismSelected?.Invoke(-1);
                    }
                }
                Invalidate();
            }
            else if (_toolMode == EcoToolMode.DropFood)
            {
                _engine.DropFoodCluster(wx, wy, isInitialClick ? 10 : 4, 28f);
                WorldMutatedByUser?.Invoke();
                Invalidate();
            }
            else if (_toolMode == EcoToolMode.SpawnPrey && isInitialClick)
            {
                var rng = new Random();
                var org = _engine.SpawnOrganism(EcoSpecies.Prey, wx, wy, Genome.CreateDefaultPrey(rng), 1);
                if (org != null)
                {
                    _selectedOrganismId = org.Id;
                    OrganismSelected?.Invoke(org.Id);
                    WorldMutatedByUser?.Invoke();
                    Invalidate();
                }
            }
            else if ((_toolMode == EcoToolMode.SpawnPredator ||
                      _toolMode == EcoToolMode.SpawnBear ||
                      _toolMode == EcoToolMode.SpawnHuman) && isInitialClick)
            {
                var rng = new Random();
                EcoSpecies sp;
                Genome dna;
                if (_toolMode == EcoToolMode.SpawnBear)
                {
                    sp = EcoSpecies.Bear;
                    dna = Genome.CreateDefaultBear(rng);
                }
                else if (_toolMode == EcoToolMode.SpawnHuman)
                {
                    sp = EcoSpecies.Human;
                    dna = Genome.CreateDefaultHuman(rng);
                }
                else
                {
                    sp = EcoSpecies.Predator;
                    dna = Genome.CreateDefaultPredator(rng);
                }

                var org = _engine.SpawnOrganism(sp, wx, wy, dna, 1);
                if (org != null)
                {
                    _selectedOrganismId = org.Id;
                    OrganismSelected?.Invoke(org.Id);
                    WorldMutatedByUser?.Invoke();
                    Invalidate();
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            g.FillRectangle(EcoRenderCache.BgBrush, 0, 0, Width, Height);

            // 배경 격자
            const int gridStep = 48;
            for (int x = gridStep; x < Width; x += gridStep)
            {
                g.DrawLine(EcoRenderCache.GridPen, x, 0, x, Height);
            }
            for (int y = gridStep; y < Height; y += gridStep)
            {
                g.DrawLine(EcoRenderCache.GridPen, 0, y, Width, y);
            }

            if (_engine == null || Width <= 0 || Height <= 0) return;

            GetCameraTransform(out float viewLeft, out float viewTop, out float viewW, out float viewH, out float sx, out float sy, out float scaleAvg);
            float viewRight = viewLeft + viewW;
            float viewBottom = viewTop + viewH;

            // -1. 지형 장애물 (연못 & 바위) 렌더링
            var terrains = _engine.Terrains;
            for (int i = 0; i < terrains.Count; i++)
            {
                var t = terrains[i];
                if (t.X + t.Radius < viewLeft || t.X - t.Radius > viewRight ||
                    t.Y + t.Radius < viewTop || t.Y - t.Radius > viewBottom) continue;
                float tx = (t.X - viewLeft) * sx;
                float ty = (t.Y - viewTop) * sy;
                float trx = t.Radius * sx;
                float try_ = t.Radius * sy;
                if (t.IsWater)
                {
                    g.DrawImage(EcoRenderCache.PondSprite, tx - trx, ty - try_, trx * 2f, try_ * 2f);
                }
                else
                {
                    g.DrawImage(EcoRenderCache.RockSprite, tx - trx, ty - try_, trx * 2f, try_ * 2f);
                }
            }

            // 0. 은신처(수풀) 지대 렌더링
            if (_engine.EnableShelters)
            {
                var shelters = _engine.Shelters;
                for (int i = 0; i < shelters.Count; i++)
                {
                    var sh = shelters[i];
                    if (sh.X + sh.Radius < viewLeft || sh.X - sh.Radius > viewRight ||
                        sh.Y + sh.Radius < viewTop || sh.Y - sh.Radius > viewBottom) continue;
                    float shX = (sh.X - viewLeft) * sx;
                    float shY = (sh.Y - viewTop) * sy;
                    float shRx = sh.Radius * sx;
                    float shRy = sh.Radius * sy;
                    g.DrawImage(EcoRenderCache.ShelterSprite, shX - shRx, shY - shRy, shRx * 2f, shRy * 2f);
                }
            }

            // 0.5 사체-비료 지점 렌더링
            if (_engine.EnableCompost)
            {
                var composts = _engine.Composts;
                for (int i = 0; i < composts.Count; i++)
                {
                    var c = composts[i];
                    if (c.X < viewLeft - 8f || c.X > viewRight + 8f ||
                        c.Y < viewTop - 8f || c.Y > viewBottom + 8f) continue;
                    float cx = (c.X - viewLeft) * sx;
                    float cy = (c.Y - viewTop) * sy;
                    g.FillEllipse(EcoRenderCache.CompostBrush, cx - 3.2f, cy - 3.2f, 6.4f, 6.4f);
                }
            }

            // 0.6 인간 거점 모닥불 렌더링 (그룹별 고유 색상 + 잔여 수명 바)
            var campfires = _engine.Campfires;
            bool isWinterNow = _engine.EnableSeasons && _engine.CurrentSeason == EcoSeason.Winter;
            for (int i = 0; i < campfires.Count; i++)
            {
                var cf = campfires[i];
                float maxR = cf.Radius * 1.5f;
                if (cf.X + maxR < viewLeft || cf.X - maxR > viewRight ||
                    cf.Y + maxR < viewTop || cf.Y - maxR > viewBottom) continue;
                int gIdx = (cf.GroupId > 0 ? cf.GroupId - 1 : i) & 3;
                float cfx = (cf.X - viewLeft) * sx;
                float cfy = (cf.Y - viewTop) * sy;
                float cfrx = cf.Radius * sx;
                float cfry = cf.Radius * sy;
                g.FillEllipse(EcoRenderCache.CampfireGroupGlowBrushes[gIdx], cfx - cfrx, cfy - cfry, cfrx * 2f, cfry * 2f);
                g.DrawEllipse(EcoRenderCache.CampfireGroupPens[gIdx], cfx - cfrx, cfy - cfry, cfrx * 2f, cfry * 2f);
                if (isWinterNow)
                {
                    float gRx = cf.Radius * 1.45f * sx;
                    float gRy = cf.Radius * 1.45f * sy;
                    g.DrawEllipse(EcoRenderCache.CampfireGroupPens[gIdx], cfx - gRx, cfy - gRy, gRx * 2f, gRy * 2f);
                }

                const float spriteSize = 24f;
                g.DrawImage(EcoRenderCache.CampfireSprite, cfx - spriteSize * 0.5f, cfy - spriteSize * 0.5f, spriteSize, spriteSize);
                if (cf.MaxTicks > 0)
                {
                    DrawMiniHpBar(g, cfx, cfy + spriteSize * 0.5f + 2f, 24f, 3f, cf.RemainingTicks / (float)cf.MaxTicks, EcoRenderCache.CampfireCoreBrush);
                }
            }

            // 0.7 인간 오두막(집) 렌더링
            var houses = _engine.Houses;
            for (int i = 0; i < houses.Count; i++)
            {
                var h = houses[i];
                if (h.X + h.Radius < viewLeft || h.X - h.Radius > viewRight ||
                    h.Y + h.Radius < viewTop || h.Y - h.Radius > viewBottom) continue;
                float hx = (h.X - viewLeft) * sx;
                float hy = (h.Y - viewTop) * sy;
                float hrx = h.Radius * sx;
                float hry = h.Radius * sy;
                g.FillEllipse(EcoRenderCache.HouseZoneBrush, hx - hrx, hy - hry, hrx * 2f, hry * 2f);
                g.DrawEllipse(EcoRenderCache.HouseZonePen, hx - hrx, hy - hry, hrx * 2f, hry * 2f);
                if (h.Id == _selectedHouseId)
                {
                    g.DrawEllipse(EcoRenderCache.SelectionPen, hx - 22f, hy - 22f, 44f, 44f);
                }
                g.DrawImage(EcoRenderCache.HouseSprite, hx - 18f, hy - 18f, 36f, 36f);
                float ratio = h.IsComplete ? (h.Hp / Math.Max(1f, h.MaxHp)) : (h.BuildProgress / (float)Math.Max(1, h.BuildTarget));
                DrawMiniHpBar(g, hx, hy + 20f, 32f, 3.5f, ratio, h.IsComplete ? EcoRenderCache.HouseHpBrush : EcoRenderCache.PlayerTagBrush);
            }

            // 1. 먹이 렌더링 (새싹 스프라이트 + 뷰포트 컬링)
            var foods = _engine.Foods;
            for (int i = 0; i < foods.Count; i++)
            {
                var f = foods[i];
                if (f.X < viewLeft - 10f || f.X > viewRight + 10f ||
                    f.Y < viewTop - 10f || f.Y > viewBottom + 10f) continue;
                float fx = (f.X - viewLeft) * sx;
                float fy = (f.Y - viewTop) * sy;
                g.DrawImage(EcoRenderCache.FoodSprite, fx - 5.5f, fy - 5.5f, 11f, 11f);
            }

            // 2. 생명체 렌더링 (진행 방향으로 회전하는 벡터 스프라이트 + 뷰포트 컬링)
            var orgs = _engine.Organisms;
            for (int i = 0; i < orgs.Count; i++)
            {
                var o = orgs[i];
                if (!o.IsAlive) continue;
                float cullMargin = Math.Max(30f, o.Dna.Vision);
                if (o.X < viewLeft - cullMargin || o.X > viewRight + cullMargin ||
                    o.Y < viewTop - cullMargin || o.Y > viewBottom + cullMargin) continue;

                float ox = (o.X - viewLeft) * sx;
                float oy = (o.Y - viewTop) * sy;
                float r = Math.Max(4.8f, o.Dna.Size * 1.25f * scaleAvg);

                bool isSelected = o.Id == _selectedOrganismId;
                if (isSelected)
                {
                    float vr = o.Dna.Vision * scaleAvg;
                    g.FillEllipse(EcoRenderCache.SelectionVisionBrush, ox - vr, oy - vr, vr * 2f, vr * 2f);
                    g.DrawEllipse(EcoRenderCache.SelectionVisionPen, ox - vr, oy - vr, vr * 2f, vr * 2f);
                }

                Bitmap sprite;
                switch (o.Species)
                {
                    case EcoSpecies.Prey:
                        sprite = o.Dna.Speed >= 3.0f ? EcoRenderCache.PreyFastSprite : EcoRenderCache.PreySprite;
                        break;
                    case EcoSpecies.Predator:
                        sprite = o.Dna.Speed >= 3.4f ? EcoRenderCache.PredatorFastSprite : EcoRenderCache.PredatorSprite;
                        break;
                    case EcoSpecies.Bear:
                        sprite = EcoRenderCache.BearSprite;
                        break;
                    default:
                        sprite = o.CampfireGroupId > 0
                            ? EcoRenderCache.HumanGroupSprites[(o.CampfireGroupId - 1) & 3]
                            : EcoRenderCache.HumanSprite;
                        break;
                }

                float angleDeg = (float)(Math.Atan2(o.Vy, o.Vx) * (180.0 / Math.PI));
                g.TranslateTransform(ox, oy);
                g.RotateTransform(angleDeg);
                g.DrawImage(sprite, -r, -r, r * 2f, r * 2f);
                g.ResetTransform();

                if (o.SkillActiveTicks > 0)
                {
                    g.DrawEllipse(EcoRenderCache.BearFrenzyPen, ox - r - 3f, oy - r - 3f, (r + 3f) * 2f, (r + 3f) * 2f);
                }

                if (o.IsPlayer)
                {
                    // 4~5단계 인간 플레이어일 때 공격 사거리 원 표시
                    if (o.Species == EcoSpecies.Human)
                    {
                        float pRange = (_engine.PlayerStage >= 5 ? 128f : 54f) * (_engine.PlayerDamageMult > 1.05f ? 1.15f : 1.0f);
                        float wrx = pRange * sx;
                        float wry = pRange * sy;
                        g.DrawEllipse(EcoRenderCache.PlayerRangePen, ox - wrx, oy - wry, wrx * 2f, wry * 2f);
                    }
                    g.DrawEllipse(EcoRenderCache.PlayerRingPen, ox - r - 4f, oy - r - 4f, (r + 4f) * 2f, (r + 4f) * 2f);
                    g.DrawString("YOU", EcoRenderCache.BoldSmallFont, EcoRenderCache.PlayerTagBrush, ox - 12f, oy - r - 17f);
                    DrawMiniHpBar(g, ox, oy + r + 5f, 28f, 4f, o.Energy / Math.Max(1f, o.MaxEnergy), EcoRenderCache.PlayerHpBrush);
                }
                else if (o.IsAlphaBoss)
                {
                    g.DrawEllipse(EcoRenderCache.AlphaBossRingPen, ox - r - 4f, oy - r - 4f, (r + 4f) * 2f, (r + 4f) * 2f);
                    g.DrawString("BOSS", EcoRenderCache.BoldSmallFont, EcoRenderCache.AlphaBossTagBrush, ox - 15f, oy - r - 17f);
                    DrawMiniHpBar(g, ox, oy + r + 5f, 38f, 5f, o.Energy / Math.Max(1f, o.MaxEnergy), EcoRenderCache.BossHpBrush);
                }
                else if (o.Id == _engine.HallOfFame.LivingLegendId)
                {
                    g.DrawString("★", EcoRenderCache.BoldSmallFont, EcoRenderCache.LegendStarBrush, ox - 5.5f, oy - r - 13f);
                }

                if (o.IsHibernating)
                {
                    g.DrawString("Zzz", EcoRenderCache.BoldSmallFont, EcoRenderCache.TextMutedBrush, ox + r * 0.5f, oy - r - 10f);
                }

                if (isSelected)
                {
                    g.DrawEllipse(EcoRenderCache.SelectionPen, ox - r - 2f, oy - r - 2f, (r + 2f) * 2f, (r + 2f) * 2f);
                }
            }

            // 3. 인간 사냥꾼 화살/창 궤적 렌더링
            var shots = _engine.HunterShots;
            for (int i = 0; i < shots.Count; i++)
            {
                var sh = shots[i];
                g.DrawLine(EcoRenderCache.HunterShotPen, (sh.X1 - viewLeft) * sx, (sh.Y1 - viewTop) * sy, (sh.X2 - viewLeft) * sx, (sh.Y2 - viewTop) * sy);
            }

            // 4. 계절별 초경량 배경 입자 이펙트 (Zero-GC)
            DrawSeasonWeatherOverlay(g);

            // 4.5 알파 곰 보스 범위 할퀴기 이펙트
            if (_engine.BossSwipeTicks > 0)
            {
                float bx = (_engine.BossSwipeX - viewLeft) * sx;
                float by = (_engine.BossSwipeY - viewTop) * sy;
                float brx = _engine.BossSwipeRadius * sx;
                float bry = _engine.BossSwipeRadius * sy;
                g.FillEllipse(EcoRenderCache.AlphaBossSwipeBrush, bx - brx, by - bry, brx * 2f, bry * 2f);
                g.DrawEllipse(EcoRenderCache.AlphaBossSwipePen, bx - brx, by - bry, brx * 2f, bry * 2f);
            }

            // 4.6 랜덤 자연재해 구역 이펙트
            if (_engine.ActiveDisasterTicks > 0 && _engine.ActiveDisasterType > 0)
            {
                float dx = (_engine.ActiveDisasterX - viewLeft) * sx;
                float dy = (_engine.ActiveDisasterY - viewTop) * sy;
                float drx = _engine.ActiveDisasterRadius * sx;
                float dry = _engine.ActiveDisasterRadius * sy;
                SolidBrush dBrush = _engine.ActiveDisasterType == 1
                    ? EcoRenderCache.DisasterLightningBrush
                    : (_engine.ActiveDisasterType == 2 ? EcoRenderCache.DisasterColdBrush : EcoRenderCache.DisasterRainBrush);
                Pen dPen = _engine.ActiveDisasterType == 1
                    ? EcoRenderCache.DisasterLightningPen
                    : (_engine.ActiveDisasterType == 2 ? EcoRenderCache.DisasterColdPen : EcoRenderCache.DisasterRainPen);
                g.FillEllipse(dBrush, dx - drx, dy - dry, drx * 2f, dry * 2f);
                g.DrawEllipse(dPen, dx - drx, dy - dry, drx * 2f, dry * 2f);
                string dLabel = _engine.ActiveDisasterType == 1 ? "⚡ 낙뢰" : (_engine.ActiveDisasterType == 2 ? "❄ 한파" : "✦ 단비");
                g.DrawString(dLabel, EcoRenderCache.BoldSmallFont, EcoRenderCache.TextPrimaryBrush, dx - 18f, dy - 8f);
            }

            // 5. 업적 달성 토스트 배너
            if (_engine.AchievementToastTicks > 0 && !string.IsNullOrEmpty(_engine.AchievementToastText))
            {
                g.FillRectangle(EcoRenderCache.ToastBgBrush, 14f, 14f, 250f, 30f);
                g.DrawRectangle(EcoRenderCache.ToastBorderPen, 14f, 14f, 250f, 30f);
                g.DrawString(_engine.AchievementToastText, EcoRenderCache.BoldSmallFont, EcoRenderCache.LegendStarBrush, 24f, 21f);
            }

            // 6. 내 개체 키우기 모드 미니맵 (카메라 추적 중 전체 위치 파악용)
            if (_engine.IsPlayerMode && Width > 220 && Height > 150)
            {
                DrawPlayerMinimap(g, viewLeft, viewTop, viewW, viewH);
            }

            g.DrawRectangle(EcoRenderCache.BorderPen, 0, 0, Width - 1, Height - 1);
        }

        private void DrawPlayerMinimap(Graphics g, float viewLeft, float viewTop, float viewW, float viewH)
        {
            const float mw = 120f;
            const float mh = 80f;
            float mx = Width - mw - 14f;
            float my = Height - mh - 14f;
            float worldW = _engine.Config.WorldWidth;
            float worldH = _engine.Config.WorldHeight;

            g.FillRectangle(EcoRenderCache.InvBagBgBrush, mx, my, mw, mh);
            g.DrawRectangle(EcoRenderCache.InvSlotBorderPen, mx, my, mw, mh);

            float msx = mw / worldW;
            float msy = mh / worldH;

            // 모닥불
            var campfires = _engine.Campfires;
            for (int i = 0; i < campfires.Count; i++)
            {
                var cf = campfires[i];
                g.FillEllipse(EcoRenderCache.CampfireCoreBrush, mx + cf.X * msx - 2f, my + cf.Y * msy - 2f, 4f, 4f);
            }

            // 보스
            var boss = _engine.AlphaBossId >= 0 ? _engine.FindOrganismById(_engine.AlphaBossId) : null;
            if (boss != null && boss.IsAlive)
            {
                g.FillEllipse(EcoRenderCache.BossHpBrush, mx + boss.X * msx - 3f, my + boss.Y * msy - 3f, 6f, 6f);
            }

            // 플레이어
            var player = _engine.FindOrganismById(_engine.PlayerId);
            if (player != null && player.IsAlive)
            {
                g.FillEllipse(EcoRenderCache.PlayerTagBrush, mx + player.X * msx - 3f, my + player.Y * msy - 3f, 6f, 6f);
            }

            // 현재 카메라 뷰포트 박스
            g.DrawRectangle(EcoRenderCache.DirectionPen, mx + viewLeft * msx, my + viewTop * msy, viewW * msx, viewH * msy);
        }

        private static void DrawMiniHpBar(Graphics g, float cx, float topY, float w, float h, float ratio, SolidBrush fillBrush)
        {
            float clamped = ratio < 0f ? 0f : (ratio > 1f ? 1f : ratio);
            float left = cx - w * 0.5f;
            g.FillRectangle(EcoRenderCache.HpBgBrush, left, topY, w, h);
            if (clamped > 0f)
            {
                g.FillRectangle(fillBrush, left, topY, w * clamped, h);
            }
            g.DrawRectangle(EcoRenderCache.HpBorderPen, left, topY, w, h);
        }

        private void DrawSeasonWeatherOverlay(Graphics g)
        {
            if (!_engine.EnableSeasons || _engine.CurrentSeason != EcoSeason.Winter) return;

            g.FillRectangle(EcoRenderCache.WinterTintBrush, 0, 0, Width, Height);

            bool tickMoved = _engine.TickCount != _lastWeatherTick;
            _lastWeatherTick = _engine.TickCount;

            for (int i = 0; i < _weatherParticles.Length; i++)
            {
                if (tickMoved)
                {
                    _weatherParticles[i].X += _weatherParticles[i].Vx;
                    _weatherParticles[i].Y += _weatherParticles[i].Vy * 1.15f;

                    if (_weatherParticles[i].X < 0f) _weatherParticles[i].X += 1f;
                    else if (_weatherParticles[i].X > 1f) _weatherParticles[i].X -= 1f;

                    if (_weatherParticles[i].Y > 1f) _weatherParticles[i].Y -= 1f;
                }

                float px = _weatherParticles[i].X * Width;
                float py = _weatherParticles[i].Y * Height;
                float sz = _weatherParticles[i].Size;
                g.FillEllipse(EcoRenderCache.WinterSnowBrush, px, py, sz, sz);
            }
        }
    }
}
