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
        private bool _isMouseDown;
        private int _lastWeatherTick = -1;

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

        private void HandleMouseAction(int px, int py, bool isInitialClick)
        {
            if (Width <= 0 || Height <= 0 || _engine == null) return;

            float wx = (px / (float)Width) * _engine.Config.WorldWidth;
            float wy = (py / (float)Height) * _engine.Config.WorldHeight;

            if (_toolMode == EcoToolMode.Inspect)
            {
                if (!isInitialClick) return;
                var found = _engine.FindOrganismAt(wx, wy, 14f);
                _selectedOrganismId = found != null ? found.Id : -1;
                OrganismSelected?.Invoke(_selectedOrganismId);
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

            float sx = Width / _engine.Config.WorldWidth;
            float sy = Height / _engine.Config.WorldHeight;
            float scaleAvg = (sx + sy) * 0.5f;

            // -1. 지형 장애물 (연못 & 바위) 렌더링
            var terrains = _engine.Terrains;
            for (int i = 0; i < terrains.Count; i++)
            {
                var t = terrains[i];
                float tx = t.X * sx;
                float ty = t.Y * sy;
                float trx = t.Radius * sx;
                float try_ = t.Radius * sy;
                if (t.IsWater)
                {
                    g.FillEllipse(EcoRenderCache.PondFillBrush, tx - trx, ty - try_, trx * 2f, try_ * 2f);
                    g.DrawEllipse(EcoRenderCache.PondBorderPen, tx - trx, ty - try_, trx * 2f, try_ * 2f);
                }
                else
                {
                    g.FillEllipse(EcoRenderCache.RockFillBrush, tx - trx, ty - try_, trx * 2f, try_ * 2f);
                    g.DrawEllipse(EcoRenderCache.RockBorderPen, tx - trx, ty - try_, trx * 2f, try_ * 2f);
                }
            }

            // 0. 은신처(수풀) 지대 렌더링
            if (_engine.EnableShelters)
            {
                var shelters = _engine.Shelters;
                for (int i = 0; i < shelters.Count; i++)
                {
                    var sh = shelters[i];
                    float shX = sh.X * sx;
                    float shY = sh.Y * sy;
                    float shRx = sh.Radius * sx;
                    float shRy = sh.Radius * sy;
                    g.FillEllipse(EcoRenderCache.ShelterFillBrush, shX - shRx, shY - shRy, shRx * 2f, shRy * 2f);
                    g.DrawEllipse(EcoRenderCache.ShelterBorderPen, shX - shRx, shY - shRy, shRx * 2f, shRy * 2f);
                }
            }

            // 0.5 사체-비료 지점 렌더링
            if (_engine.EnableCompost)
            {
                var composts = _engine.Composts;
                for (int i = 0; i < composts.Count; i++)
                {
                    var c = composts[i];
                    float cx = c.X * sx;
                    float cy = c.Y * sy;
                    g.FillEllipse(EcoRenderCache.CompostBrush, cx - 3.2f, cy - 3.2f, 6.4f, 6.4f);
                }
            }

            // 0.6 인간 거점 모닥불 렌더링 (장작 + 흔들리는 불꽃 스프라이트)
            var campfires = _engine.Campfires;
            bool isWinterNow = _engine.EnableSeasons && _engine.CurrentSeason == EcoSeason.Winter;
            for (int i = 0; i < campfires.Count; i++)
            {
                var cf = campfires[i];
                float cfx = cf.X * sx;
                float cfy = cf.Y * sy;
                float cfrx = cf.Radius * sx;
                float cfry = cf.Radius * sy;
                g.FillEllipse(EcoRenderCache.CampfireGlowBrush, cfx - cfrx, cfy - cfry, cfrx * 2f, cfry * 2f);
                g.DrawEllipse(EcoRenderCache.CampfireRingPen, cfx - cfrx, cfy - cfry, cfrx * 2f, cfry * 2f);
                if (isWinterNow)
                {
                    float gRx = cf.Radius * 1.45f * sx;
                    float gRy = cf.Radius * 1.45f * sy;
                    g.DrawEllipse(EcoRenderCache.CampfireRingPen, cfx - gRx, cfy - gRy, gRx * 2f, gRy * 2f);
                }

                const float spriteSize = 24f;
                g.DrawImage(EcoRenderCache.CampfireSprite, cfx - spriteSize * 0.5f, cfy - spriteSize * 0.5f, spriteSize, spriteSize);
                if (cf.MaxTicks > 0)
                {
                    DrawMiniHpBar(g, cfx, cfy + spriteSize * 0.5f + 2f, 24f, 3f, cf.RemainingTicks / (float)cf.MaxTicks, EcoRenderCache.CampfireCoreBrush);
                }
            }

            // 1. 먹이 렌더링 (새싹 스프라이트)
            var foods = _engine.Foods;
            for (int i = 0; i < foods.Count; i++)
            {
                var f = foods[i];
                float fx = f.X * sx;
                float fy = f.Y * sy;
                g.DrawImage(EcoRenderCache.FoodSprite, fx - 5.5f, fy - 5.5f, 11f, 11f);
            }

            // 2. 생명체 렌더링 (진행 방향으로 회전하는 벡터 스프라이트)
            var orgs = _engine.Organisms;
            for (int i = 0; i < orgs.Count; i++)
            {
                var o = orgs[i];
                if (!o.IsAlive) continue;

                float ox = o.X * sx;
                float oy = o.Y * sy;
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
                        sprite = EcoRenderCache.HumanSprite;
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
                g.DrawLine(EcoRenderCache.HunterShotPen, sh.X1 * sx, sh.Y1 * sy, sh.X2 * sx, sh.Y2 * sy);
            }

            // 4. 계절별 초경량 배경 입자 이펙트 (Zero-GC)
            DrawSeasonWeatherOverlay(g);

            // 4.5 랜덤 자연재해 구역 이펙트
            if (_engine.ActiveDisasterTicks > 0 && _engine.ActiveDisasterType > 0)
            {
                float dx = _engine.ActiveDisasterX * sx;
                float dy = _engine.ActiveDisasterY * sy;
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
            }

            // 5. 업적 달성 토스트 배너
            if (_engine.AchievementToastTicks > 0 && !string.IsNullOrEmpty(_engine.AchievementToastText))
            {
                g.FillRectangle(EcoRenderCache.ToastBgBrush, 14f, 14f, 250f, 30f);
                g.DrawRectangle(EcoRenderCache.ToastBorderPen, 14f, 14f, 250f, 30f);
                g.DrawString(_engine.AchievementToastText, EcoRenderCache.BoldSmallFont, EcoRenderCache.LegendStarBrush, 24f, 21f);
            }

            g.DrawRectangle(EcoRenderCache.BorderPen, 0, 0, Width - 1, Height - 1);
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
