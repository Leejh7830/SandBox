using System;
using System.Collections.Generic;
using GateHelper.EcoSim.Models;

namespace GateHelper.EcoSim.Services
{
    public sealed partial class EcoSimulationEngine
    {
        public Organism SpawnAlphaBearBoss()
        {
            var dna = new Genome(2.72f, 185f, 16.5f);
            var boss = SpawnOrganism(EcoSpecies.Bear, RandomRange(80f, _config.WorldWidth - 80f), RandomRange(80f, _config.WorldHeight - 80f), dna, Math.Max(1, MaxGeneration), 860f);
            if (boss == null && _organisms.Count > 0)
            {
                for (int i = 0; i < _organisms.Count; i++)
                {
                    if (!_organisms[i].IsPlayer) { _organisms.RemoveAt(i); break; }
                }
                boss = SpawnOrganism(EcoSpecies.Bear, RandomRange(80f, _config.WorldWidth - 80f), RandomRange(80f, _config.WorldHeight - 80f), dna, Math.Max(1, MaxGeneration), 860f);
            }
            if (boss != null)
            {
                boss.IsAlphaBoss = true;
                boss.MaxEnergy = 860f;
                boss.Energy = 860f;
                AlphaBossId = boss.Id;
                if (IsPlayerMode) PlayerNotice = "경고! [알파 곰 보스]가 출현했습니다!";
            }
            return boss;
        }

        private void InitTerrain()
        {
            float w = _config.WorldWidth;
            float h = _config.WorldHeight;
            // 중앙 부근 오아시스 연못 1개 + 전략적 바위 3개
            _terrains.Add(new TerrainObstacle(w * 0.50f, h * 0.50f, 52f, isWater: true));
            _terrains.Add(new TerrainObstacle(w * 0.38f, h * 0.34f, 26f, isWater: false));
            _terrains.Add(new TerrainObstacle(w * 0.63f, h * 0.38f, 28f, isWater: false));
            _terrains.Add(new TerrainObstacle(w * 0.48f, h * 0.72f, 25f, isWater: false));
        }

        private void UpdateCampfires()
        {
            int decay = (EnableSeasons && CurrentSeason == EcoSeason.Spring) ? 4 : 1;
            for (int i = _campfires.Count - 1; i >= 0; i--)
            {
                var cf = _campfires[i];
                cf.RemainingTicks -= decay;
                if (cf.RemainingTicks <= 0 || cf.Hp <= 0f)
                {
                    RemoveCampfireSwapPop(i);
                }
                else
                {
                    _campfires[i] = cf;
                }
            }
        }

        private int FindCampfireByGroupId(int groupId)
        {
            if (groupId <= 0) return -1;
            for (int i = 0; i < _campfires.Count; i++)
            {
                if (_campfires[i].GroupId == groupId) return i;
            }
            return -1;
        }

        private int FindNearestCampfire(float x, float y, float maxRadius)
        {
            int bestIdx = -1;
            float bestDistSq = maxRadius * maxRadius;
            for (int i = 0; i < _campfires.Count; i++)
            {
                var cf = _campfires[i];
                float dx = cf.X - x;
                float dy = cf.Y - y;
                float d2 = dx * dx + dy * dy;
                if (d2 <= bestDistSq)
                {
                    bestDistSq = d2;
                    bestIdx = i;
                }
            }
            return bestIdx;
        }

        private void RemoveCampfireSwapPop(int index)
        {
            int last = _campfires.Count - 1;
            _campfires[index] = _campfires[last];
            _campfires.RemoveAt(last);
        }

        private void InitShelters()
        {
            float w = _config.WorldWidth;
            float h = _config.WorldHeight;
            _shelters.Add(new EcoShelterZone(w * 0.24f, h * 0.28f, 68f));
            _shelters.Add(new EcoShelterZone(w * 0.74f, h * 0.26f, 64f));
            _shelters.Add(new EcoShelterZone(w * 0.28f, h * 0.74f, 64f));
            _shelters.Add(new EcoShelterZone(w * 0.72f, h * 0.72f, 70f));
        }

        private bool IsInsideShelter(float x, float y)
        {
            for (int i = 0; i < _shelters.Count; i++)
            {
                var s = _shelters[i];
                float dx = x - s.X;
                float dy = y - s.Y;
                if (dx * dx + dy * dy <= s.Radius * s.Radius)
                    return true;
            }
            return false;
        }

        private void UpdateRandomDisasters()
        {
            if (ActiveDisasterTicks > 0)
            {
                ActiveDisasterTicks--;
                if (ActiveDisasterTicks == 0) ActiveDisasterType = 0;
            }

            if (!EnableDisasters) return;

            _nextDisasterCountdown--;
            if (_nextDisasterCountdown > 0) return;

            _nextDisasterCountdown = _rng.Next(420, 680);
            float dx = RandomRange(100f, _config.WorldWidth - 100f);
            float dy = RandomRange(90f, _config.WorldHeight - 90f);
            int kind = _rng.Next(1, 4); // 1: 국지 낙뢰, 2: 한파 돌풍, 3: 단비

            ActiveDisasterType = kind;
            ActiveDisasterX = dx;
            ActiveDisasterY = dy;
            ActiveDisasterTicks = 26;

            if (kind == 1)
            {
                ActiveDisasterRadius = 95f;
                ApplyAreaEnergyDelta(dx, dy, 95f, -45f);
                if (EnableCompost && _composts.Count < 115)
                {
                    _composts.Add(new CompostSpot(dx, dy, 35, 3));
                    _composts.Add(new CompostSpot(dx + 18f, dy - 14f, 35, 2));
                }
                AchievementToastText = "⚡ 자연현상: 국지 낙뢰 발생!";
                AchievementToastTicks = 65;
            }
            else if (kind == 2)
            {
                ActiveDisasterRadius = 120f;
                ApplyAreaEnergyDelta(dx, dy, 120f, -28f);
                AchievementToastText = "❄ 자연현상: 차가운 돌풍!";
                AchievementToastTicks = 65;
            }
            else
            {
                ActiveDisasterRadius = 110f;
                DropFoodCluster(dx, dy, 8, 70f);
                AchievementToastText = "✦ 자연현상: 축복의 단비 (새싹 성장)";
                AchievementToastTicks = 65;
            }
        }

        private void ApplyAreaEnergyDelta(float cx, float cy, float radius, float delta)
        {
            float r2 = radius * radius;
            for (int i = 0; i < _organisms.Count; i++)
            {
                var o = _organisms[i];
                if (!o.IsAlive || o.IsHibernating) continue;
                float dx = o.X - cx;
                float dy = o.Y - cy;
                if (dx * dx + dy * dy <= r2)
                {
                    // 플레이어나 일반 개체가 한 번에 억울하게 즉사하지 않도록 최소 10 남김 (이미 빈사 상태 제외)
                    if (delta < 0f && o.Energy > 15f)
                    {
                        o.Energy = Math.Max(10f, o.Energy + delta);
                    }
                    else
                    {
                        o.Energy = Math.Min(o.MaxEnergy, o.Energy + delta);
                    }
                }
            }
        }

        private void UpdateSeason()
        {
            if (_freezeWinterSeason)
            {
                CurrentSeason = EcoSeason.Winter;
                return;
            }
            if (!EnableSeasons)
            {
                CurrentSeason = EcoSeason.Spring;
                return;
            }

            int phase = (TickCount / 220) % 4;
            CurrentSeason = (EcoSeason)phase;
        }

        private float GetSeasonFoodMultiplier()
        {
            if (!EnableSeasons) return 1.0f;
            switch (CurrentSeason)
            {
                case EcoSeason.Spring: return 1.25f;
                case EcoSeason.Summer: return 1.55f;
                case EcoSeason.Autumn: return 0.85f;
                case EcoSeason.Winter: return 0.35f;
                default: return 1.0f;
            }
        }

        private float GetSeasonMetabolicMultiplier()
        {
            if (!EnableSeasons) return 1.0f;
            switch (CurrentSeason)
            {
                case EcoSeason.Spring: return 0.95f;
                case EcoSeason.Summer: return 1.00f;
                case EcoSeason.Autumn: return 1.00f;
                case EcoSeason.Winter: return 1.15f;
                default: return 1.0f;
            }
        }

        private void UpdateComposts()
        {
            if (!EnableCompost)
            {
                if (_composts.Count > 0) _composts.Clear();
                return;
            }

            for (int i = _composts.Count - 1; i >= 0; i--)
            {
                var c = _composts[i];
                c.RemainingTicks--;
                if (c.RemainingTicks <= 0)
                {
                    DropFoodCluster(c.X, c.Y, c.FoodYield, 20f);
                    int last = _composts.Count - 1;
                    _composts[i] = _composts[last];
                    _composts.RemoveAt(last);
                }
                else
                {
                    _composts[i] = c;
                }
            }
        }

        private void HandleWildMigration()
        {
            if (!EnableWildMigration)
            {
                _preyExtinctTicks = 0;
                _predatorExtinctTicks = 0;
                return;
            }

            if (PreyCount == 0)
            {
                _preyExtinctTicks++;
                if (_preyExtinctTicks >= 25)
                {
                    _preyExtinctTicks = 0;
                    for (int i = 0; i < 4; i++)
                    {
                        SpawnAtEdge(EcoSpecies.Prey);
                    }
                }
            }
            else
            {
                _preyExtinctTicks = 0;
            }

            if (PredatorCount == 0 && PreyCount >= 8)
            {
                _predatorExtinctTicks++;
                if (_predatorExtinctTicks >= 30)
                {
                    _predatorExtinctTicks = 0;
                    for (int i = 0; i < 2; i++)
                    {
                        SpawnAtEdge(EcoSpecies.Predator);
                    }
                }
            }
            else
            {
                _predatorExtinctTicks = 0;
            }

            if (BearCount == 0 && PreyCount >= 12)
            {
                _bearExtinctTicks++;
                if (_bearExtinctTicks >= 90)
                {
                    _bearExtinctTicks = 0;
                    SpawnAtEdge(EcoSpecies.Bear);
                }
            }
            else
            {
                _bearExtinctTicks = 0;
            }

            if (HumanCount == 0 && (PreyCount + PredatorCount) >= 12)
            {
                _humanExtinctTicks++;
                if (_humanExtinctTicks >= 95)
                {
                    _humanExtinctTicks = 0;
                    SpawnAtEdge(EcoSpecies.Human);
                }
            }
            else
            {
                _humanExtinctTicks = 0;
            }
        }

        private void SpawnAtEdge(EcoSpecies species)
        {
            bool horizontal = _rng.Next(2) == 0;
            float x = horizontal ? RandomRange(20f, _config.WorldWidth - 20f) : (_rng.Next(2) == 0 ? 16f : _config.WorldWidth - 16f);
            float y = horizontal ? (_rng.Next(2) == 0 ? 16f : _config.WorldHeight - 16f) : RandomRange(20f, _config.WorldHeight - 20f);
            Genome dna;
            switch (species)
            {
                case EcoSpecies.Predator: dna = Genome.CreateDefaultPredator(_rng); break;
                case EcoSpecies.Bear: dna = Genome.CreateDefaultBear(_rng); break;
                case EcoSpecies.Human: dna = Genome.CreateDefaultHuman(_rng); break;
                default: dna = Genome.CreateDefaultPrey(_rng); break;
            }
            SpawnOrganism(species, x, y, dna, Math.Max(1, MaxGeneration));
        }

        private void UpdateHunterShots()
        {
            for (int i = _hunterShots.Count - 1; i >= 0; i--)
            {
                var s = _hunterShots[i];
                s.RemainingTicks--;
                if (s.RemainingTicks <= 0)
                {
                    int last = _hunterShots.Count - 1;
                    _hunterShots[i] = _hunterShots[last];
                    _hunterShots.RemoveAt(last);
                }
                else
                {
                    _hunterShots[i] = s;
                }
            }
        }

        private static float ComputeMaxEnergy(EcoSpecies species, Genome dna)
        {
            switch (species)
            {
                case EcoSpecies.Predator: return 190f + dna.Size * 4f;
                case EcoSpecies.Bear: return 380f + dna.Size * 6f;
                case EcoSpecies.Human: return 255f + dna.Size * 4f;
                default: return 145f + dna.Size * 3f;
            }
        }

        private void SpawnPeriodicFood()
        {
            float mult = GetSeasonFoodMultiplier();
            if (IsAbundance) mult *= 2.4f;
            else if (IsDrought) mult *= 0.25f;

            _foodSpawnAccumulator += _config.BaseFoodSpawnPerTick * mult;
            while (_foodSpawnAccumulator >= 1f)
            {
                _foodSpawnAccumulator -= 1f;
                if (_foods.Count < _config.MaxFoodCount)
                {
                    SpawnRandomFood();
                }
            }
        }

        private bool IsInsideTerrain(float x, float y, float padding)
        {
            for (int i = 0; i < _terrains.Count; i++)
            {
                var t = _terrains[i];
                float r = (t.IsWater ? t.Radius * 0.85f : t.Radius) + padding;
                float dx = x - t.X;
                float dy = y - t.Y;
                if (dx * dx + dy * dy <= r * r)
                    return true;
            }
            return false;
        }

        private void SpawnRandomFood()
        {
            for (int attempt = 0; attempt < 5; attempt++)
            {
                float fx, fy;
                if (attempt == 0 && EnableShelters && _shelters.Count > 0 && _rng.NextDouble() < 0.32)
                {
                    var sh = _shelters[_rng.Next(_shelters.Count)];
                    double ang = _rng.NextDouble() * Math.PI * 2.0;
                    float dist = (float)_rng.NextDouble() * sh.Radius * 1.15f;
                    fx = Clamp(sh.X + (float)Math.Cos(ang) * dist, 14f, _config.WorldWidth - 14f);
                    fy = Clamp(sh.Y + (float)Math.Sin(ang) * dist, 14f, _config.WorldHeight - 14f);
                }
                else if (attempt == 0 && _terrains.Count > 0 && _terrains[0].IsWater && _rng.NextDouble() < 0.25)
                {
                    var pond = _terrains[0];
                    double ang = _rng.NextDouble() * Math.PI * 2.0;
                    float dist = pond.Radius + 10f + (float)_rng.NextDouble() * 34f;
                    fx = Clamp(pond.X + (float)Math.Cos(ang) * dist, 14f, _config.WorldWidth - 14f);
                    fy = Clamp(pond.Y + (float)Math.Sin(ang) * dist, 14f, _config.WorldHeight - 14f);
                }
                else
                {
                    fx = RandomRange(14f, _config.WorldWidth - 14f);
                    fy = RandomRange(14f, _config.WorldHeight - 14f);
                }

                if (!IsInsideTerrain(fx, fy, 7f))
                {
                    _foods.Add(new FoodPellet(fx, fy, _config.FoodEnergyValue));
                    return;
                }
            }
        }

        private void RemoveFoodSwapPop(int index)
        {
            int last = _foods.Count - 1;
            _foods[index] = _foods[last];
            _foods.RemoveAt(last);
        }

    }
}
