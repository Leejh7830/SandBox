using System;
using System.Collections.Generic;
using GateHelper.EcoSim.Models;

namespace GateHelper.EcoSim.Services
{
    /// <summary>
    /// WinForms UI와 완전히 분리된 순수 C# 생태계 진화 시뮬레이션 엔진.
    /// </summary>
    public sealed partial class EcoSimulationEngine
    {
        private readonly Random _rng = new Random();
        private readonly EcoConfig _config;
        private readonly List<Organism> _organisms = new List<Organism>(512);
        private readonly List<Organism> _pendingOffspring = new List<Organism>(64);
        private readonly List<FoodPellet> _foods = new List<FoodPellet>(512);
        private readonly List<EcoShelterZone> _shelters = new List<EcoShelterZone>(8);
        private readonly List<CompostSpot> _composts = new List<CompostSpot>(128);
        private readonly List<HunterShot> _hunterShots = new List<HunterShot>(32);
        private readonly List<TerrainObstacle> _terrains = new List<TerrainObstacle>(8);
        private readonly List<CampfireSite> _campfires = new List<CampfireSite>(4);
        private readonly List<EcoStatsSnapshot> _history = new List<EcoStatsSnapshot>(256);

        private readonly SpatialGrid _organismGrid;
        private readonly SpatialGrid _foodGrid;
        private readonly List<int> _queryBuffer = new List<int>(64);

        private int _nextOrganismId = 1;
        private float _foodSpawnAccumulator;
        private int _preyExtinctTicks;
        private int _predatorExtinctTicks;
        private int _bearExtinctTicks;
        private int _humanExtinctTicks;

        public EcoConfig Config => _config;
        public IReadOnlyList<Organism> Organisms => _organisms;
        public IReadOnlyList<FoodPellet> Foods => _foods;
        public IReadOnlyList<EcoShelterZone> Shelters => _shelters;
        public IReadOnlyList<CompostSpot> Composts => _composts;
        public IReadOnlyList<HunterShot> HunterShots => _hunterShots;
        public IReadOnlyList<TerrainObstacle> Terrains => _terrains;
        public IReadOnlyList<CampfireSite> Campfires => _campfires;
        public EcoHallOfFame HallOfFame { get; } = new EcoHallOfFame();
        public IReadOnlyList<EcoAchievement> Achievements => _achievements;
        public string AchievementToastText { get; private set; } = "";
        public int AchievementToastTicks { get; private set; }
        public IReadOnlyList<EcoStatsSnapshot> History => _history;

        private readonly List<EcoAchievement> _achievements = new List<EcoAchievement>(8);

        public int TickCount { get; private set; }
        public int MaxGeneration { get; private set; } = 1;
        public int PreyCount { get; private set; }
        public int PredatorCount { get; private set; }
        public int BearCount { get; private set; }
        public int HumanCount { get; private set; }
        public bool IsAbundance { get; private set; }
        public bool IsDrought { get; private set; }
        public bool IsHighMutation { get; private set; }

        public bool EnableShelters { get; set; } = true;
        public bool EnableCompost { get; set; } = true;
        public bool EnableSeasons { get; set; } = true;
        public bool EnableWildMigration { get; set; } = true;
        public bool EnableDisasters { get; set; } = true;
        public EcoSeason CurrentSeason { get; private set; } = EcoSeason.Spring;

        // 랜덤 자연재해 시각 상태 (0: 없음, 1: 낙뢰, 2: 한파 돌풍, 3: 단비)
        public int ActiveDisasterType { get; private set; }
        public float ActiveDisasterX { get; private set; }
        public float ActiveDisasterY { get; private set; }
        public float ActiveDisasterRadius { get; private set; }
        public int ActiveDisasterTicks { get; private set; }
        private int _nextDisasterCountdown = 460;

        // 플레이어 직접 조종(내 개체 키우기) 상태
        public bool IsPlayerMode { get; private set; }
        public int PlayerId { get; private set; } = -1;
        public int PlayerStage { get; private set; } = 1;
        public float PlayerExp { get; private set; }
        public float PlayerNextExp { get; private set; } = 100f;
        public int PlayerDeathCount { get; private set; }
        public int PlayerBestStage { get; private set; } = 1;
        public string PlayerNotice { get; private set; } = "";
        public bool PendingPerkChoice { get; private set; }
        public float PlayerSpeedMult { get; private set; } = 1.0f;
        public float PlayerVitalityMult { get; private set; } = 1.0f;
        public float PlayerGainMult { get; private set; } = 1.0f;
        public float PlayerCooldownMult { get; private set; } = 1.0f;
        public float PlayerDamageMult { get; private set; } = 1.0f;
        public string PlayerPerkSummary { get; private set; } = "특성: 없음";
        public int AlphaBossId { get; private set; } = -1;
        public float BossSwipeX { get; private set; }
        public float BossSwipeY { get; private set; }
        public float BossSwipeRadius { get; private set; }
        public int BossSwipeTicks { get; private set; }

        public const int MaxInventoryStack = 20;
        public const int CampfireWoodCost = 5;
        private const int MaxCampfires = 3;
        private const int CampfireDurationTicks = 480;
        private int _nextCampfireGroupId = 1;

        private float _playerInputX;
        private float _playerInputY;
        private bool _playerSkillPressed;
        private bool _playerBuildCampfirePressed;
        private int _alphaBossCooldown;
        private bool _freezeWinterSeason;

        public EcoSimulationEngine(EcoConfig config = null)
        {
            _config = config ?? new EcoConfig();
            _organismGrid = new SpatialGrid(_config.WorldWidth, _config.WorldHeight, 96f);
            _foodGrid = new SpatialGrid(_config.WorldWidth, _config.WorldHeight, 96f);
            InitAchievements();
            Reset();
        }
        public void Reset()
        {
            _organisms.Clear();
            _pendingOffspring.Clear();
            _foods.Clear();
            _shelters.Clear();
            _composts.Clear();
            _hunterShots.Clear();
            _terrains.Clear();
            _campfires.Clear();
            _history.Clear();
            HallOfFame.Reset();

            _nextOrganismId = 1;
            _foodSpawnAccumulator = 0f;
            _preyExtinctTicks = 0;
            _predatorExtinctTicks = 0;
            _bearExtinctTicks = 0;
            _humanExtinctTicks = 0;
            TickCount = 0;
            MaxGeneration = 1;
            IsAbundance = false;
            IsDrought = false;
            IsHighMutation = false;
            CurrentSeason = EcoSeason.Spring;
            _freezeWinterSeason = false;
            _nextCampfireGroupId = 1;
            AlphaBossId = -1;
            _alphaBossCooldown = 0;
            BossSwipeTicks = 0;
            ActiveDisasterType = 0;
            ActiveDisasterTicks = 0;
            _nextDisasterCountdown = 460;
            bool keepPlayer = IsPlayerMode;
            PlayerId = -1;
            PlayerStage = 1;
            PlayerExp = 0f;
            PlayerNextExp = 200f;
            PlayerNotice = "";
            ResetPlayerPerks();

            InitShelters();
            InitTerrain();

            for (int i = 0; i < _config.InitialFoodCount; i++)
            {
                SpawnRandomFood();
            }
            for (int i = 0; i < _config.InitialPreyCount; i++)
            {
                SpawnOrganism(
                    EcoSpecies.Prey,
                    RandomRange(30f, _config.WorldWidth - 30f),
                    RandomRange(30f, _config.WorldHeight - 30f),
                    Genome.CreateDefaultPrey(_rng),
                    generation: 1);
            }
            for (int i = 0; i < _config.InitialPredatorCount; i++)
            {
                SpawnOrganism(
                    EcoSpecies.Predator,
                    RandomRange(30f, _config.WorldWidth - 30f),
                    RandomRange(30f, _config.WorldHeight - 30f),
                    Genome.CreateDefaultPredator(_rng),
                    generation: 1);
            }
            for (int i = 0; i < _config.InitialBearCount; i++)
            {
                SpawnOrganism(
                    EcoSpecies.Bear,
                    RandomRange(40f, _config.WorldWidth - 40f),
                    RandomRange(40f, _config.WorldHeight - 40f),
                    Genome.CreateDefaultBear(_rng),
                    generation: 1);
            }
            for (int i = 0; i < _config.InitialHumanCount; i++)
            {
                SpawnOrganism(
                    EcoSpecies.Human,
                    RandomRange(40f, _config.WorldWidth - 40f),
                    RandomRange(40f, _config.WorldHeight - 40f),
                    Genome.CreateDefaultHuman(_rng),
                    generation: 1);
            }

            RecountAndRecordSnapshot(forceRecord: true);
            if (keepPlayer)
            {
                SpawnPlayerStage1();
            }
        }

        public void ApplyPreset(EcoPreset preset)
        {
            Reset();
            if (preset == EcoPreset.Default) return;

            _organisms.Clear();
            _foods.Clear();
            _campfires.Clear();
            _history.Clear();
            _nextOrganismId = 1;

            int foodCnt = 150, preyCnt = 48, predCnt = 11, bearCnt = 2, humanCnt = 2;
            switch (preset)
            {
                case EcoPreset.BearVsHumanWar:
                    foodCnt = 130; preyCnt = 36; predCnt = 6; bearCnt = 10; humanCnt = 10;
                    break;
                case EcoPreset.IceAge:
                    foodCnt = 75; preyCnt = 32; predCnt = 10; bearCnt = 4; humanCnt = 4;
                    IsDrought = true;
                    _freezeWinterSeason = true;
                    CurrentSeason = EcoSeason.Winter;
                    break;
                case EcoPreset.HyperEvolution:
                    foodCnt = 260; preyCnt = 72; predCnt = 18; bearCnt = 3; humanCnt = 3;
                    IsAbundance = true;
                    IsHighMutation = true;
                    break;
            }

            for (int i = 0; i < foodCnt; i++) SpawnRandomFood();
            for (int i = 0; i < preyCnt; i++) SpawnOrganism(EcoSpecies.Prey, RandomRange(30f, _config.WorldWidth - 30f), RandomRange(30f, _config.WorldHeight - 30f), Genome.CreateDefaultPrey(_rng), 1);
            for (int i = 0; i < predCnt; i++) SpawnOrganism(EcoSpecies.Predator, RandomRange(30f, _config.WorldWidth - 30f), RandomRange(30f, _config.WorldHeight - 30f), Genome.CreateDefaultPredator(_rng), 1);
            for (int i = 0; i < bearCnt; i++) SpawnOrganism(EcoSpecies.Bear, RandomRange(40f, _config.WorldWidth - 40f), RandomRange(40f, _config.WorldHeight - 40f), Genome.CreateDefaultBear(_rng), 1);
            for (int i = 0; i < humanCnt; i++) SpawnOrganism(EcoSpecies.Human, RandomRange(40f, _config.WorldWidth - 40f), RandomRange(40f, _config.WorldHeight - 40f), Genome.CreateDefaultHuman(_rng), 1);

            RecountAndRecordSnapshot(forceRecord: true);
            if (IsPlayerMode) SpawnPlayerStage1();
        }

        public void ToggleEvent(EcoEnvironmentEvent envEvent)
        {
            switch (envEvent)
            {
                case EcoEnvironmentEvent.Abundance:
                    IsAbundance = !IsAbundance;
                    if (IsAbundance) IsDrought = false;
                    break;
                case EcoEnvironmentEvent.Drought:
                    IsDrought = !IsDrought;
                    if (IsDrought) IsAbundance = false;
                    break;
                case EcoEnvironmentEvent.HighMutation:
                    IsHighMutation = !IsHighMutation;
                    break;
                default:
                    IsAbundance = false;
                    IsDrought = false;
                    IsHighMutation = false;
                    break;
            }
        }

        public void DropFoodCluster(float x, float y, int count = 10, float spread = 38f)
        {
            for (int i = 0; i < count && _foods.Count < _config.MaxFoodCount; i++)
            {
                float fx = Clamp(x + RandomRange(-spread, spread), 12f, _config.WorldWidth - 12f);
                float fy = Clamp(y + RandomRange(-spread, spread), 12f, _config.WorldHeight - 12f);
                if (!IsInsideTerrain(fx, fy, 6f))
                {
                    _foods.Add(new FoodPellet(fx, fy, _config.FoodEnergyValue));
                }
            }
        }

        public Organism SpawnOrganism(EcoSpecies species, float x, float y, Genome dna, int generation = 1, float? initialEnergy = null)
        {
            if (_organisms.Count >= _config.MaxOrganismCount)
                return null;

            float maxEnergy = ComputeMaxEnergy(species, dna);
            float startEnergy = initialEnergy ?? (maxEnergy * 0.60f);

            double angle = _rng.NextDouble() * Math.PI * 2.0;
            var org = new Organism
            {
                Id = _nextOrganismId++,
                Species = species,
                Generation = generation,
                Dna = dna,
                X = Clamp(x, 10f, _config.WorldWidth - 10f),
                Y = Clamp(y, 10f, _config.WorldHeight - 10f),
                Vx = (float)Math.Cos(angle) * dna.Speed,
                Vy = (float)Math.Sin(angle) * dna.Speed,
                Energy = startEnergy,
                MaxEnergy = maxEnergy,
                IsAlive = true
            };

            if (generation > MaxGeneration)
                MaxGeneration = generation;

            _organisms.Add(org);
            return org;
        }

        public Organism FindOrganismAt(float x, float y, float extraRadius = 10f)
        {
            Organism best = null;
            float bestDistSq = float.MaxValue;
            for (int i = 0; i < _organisms.Count; i++)
            {
                var o = _organisms[i];
                if (!o.IsAlive) continue;
                float hitR = o.Dna.Size + extraRadius;
                float dx = o.X - x;
                float dy = o.Y - y;
                float d2 = dx * dx + dy * dy;
                if (d2 <= hitR * hitR && d2 < bestDistSq)
                {
                    bestDistSq = d2;
                    best = o;
                }
            }
            return best;
        }

        public Organism FindOrganismById(int id)
        {
            for (int i = 0; i < _organisms.Count; i++)
            {
                if (_organisms[i].Id == id && _organisms[i].IsAlive)
                    return _organisms[i];
            }
            return null;
        }

        public void Step()
        {
            TickCount++;
            UpdateSeason();
            UpdateComposts();
            UpdateHunterShots();
            UpdateCampfires();
            UpdateRandomDisasters();
            if (BossSwipeTicks > 0) BossSwipeTicks--;
            if (AchievementToastTicks > 0) AchievementToastTicks--;
            SpawnPeriodicFood();
            RebuildSpatialGrids();

            _pendingOffspring.Clear();

            float mutationRate = IsHighMutation
                ? Math.Min(0.85f, _config.MutationRate * 3f)
                : _config.MutationRate;

            for (int i = 0; i < _organisms.Count; i++)
            {
                var org = _organisms[i];
                if (!org.IsAlive) continue;

                org.AgeTicks++;
                if (org.AttackCooldown > 0) org.AttackCooldown--;
                if (org.GatherCooldown > 0) org.GatherCooldown--;
                if (org.EatingTicks > 0) org.EatingTicks--;
                if (org.SkillActiveTicks > 0) org.SkillActiveTicks--;
                if (org.EscapeTicks > 0) org.EscapeTicks--;

                if (org.IsPlayer)
                {
                    StepPlayer(org);
                    continue;
                }

                switch (org.Species)
                {
                    case EcoSpecies.Prey:
                        StepPrey(org, mutationRate);
                        break;
                    case EcoSpecies.Predator:
                        StepPredator(org, mutationRate);
                        break;
                    case EcoSpecies.Bear:
                        StepBear(org, mutationRate);
                        break;
                    case EcoSpecies.Human:
                        StepHuman(org, mutationRate);
                        break;
                }
            }

            // 죽은 개체 제거 (사체-비료 생성 및 플레이어 1단계 환생 처리 포함)
            bool playerDied = false;
            int deadStage = PlayerStage;
            int deadAgeTicks = 0;

            for (int i = _organisms.Count - 1; i >= 0; i--)
            {
                if (!_organisms[i].IsAlive)
                {
                    var dead = _organisms[i];
                    if (dead.IsPlayer)
                    {
                        playerDied = true;
                        deadAgeTicks = dead.AgeTicks;
                    }
                    if (EnableCompost && _composts.Count < 120)
                    {
                        _composts.Add(new CompostSpot(dead.X, dead.Y, 45, dead.Species == EcoSpecies.Predator ? 3 : 2));
                    }
                    int last = _organisms.Count - 1;
                    _organisms[i] = _organisms[last];
                    _organisms.RemoveAt(last);
                }
            }

            if (playerDied && IsPlayerMode)
            {
                PlayerDeathCount++;
                PlayerNotice = $"사망! ({deadStage}단계 · {deadAgeTicks / 880}년) → 1단계 환생";
                ResetPlayerPerks();
                SpawnPlayerStage1();
            }

            // 5단계(인간 활) 도달 후 알파 곰 보스 주기적 출현
            if (IsPlayerMode && PlayerStage >= 5)
            {
                var boss = AlphaBossId >= 0 ? FindOrganismById(AlphaBossId) : null;
                if (boss == null)
                {
                    AlphaBossId = -1;
                    _alphaBossCooldown++;
                    if (_alphaBossCooldown >= 180)
                    {
                        _alphaBossCooldown = 0;
                        SpawnAlphaBearBoss();
                    }
                }
            }

            // 신규 자손 합류
            for (int i = 0; i < _pendingOffspring.Count && _organisms.Count < _config.MaxOrganismCount; i++)
            {
                _organisms.Add(_pendingOffspring[i]);
            }

            bool shouldSample = (TickCount % _config.StatsSampleIntervalTicks) == 0;
            RecountAndRecordSnapshot(shouldSample);
            HandleWildMigration();
        }

        private void RebuildSpatialGrids()
        {
            _organismGrid.Clear();
            for (int i = 0; i < _organisms.Count; i++)
            {
                if (_organisms[i].IsAlive)
                {
                    _organismGrid.Insert(i, _organisms[i].X, _organisms[i].Y);
                }
            }

            _foodGrid.Clear();
            for (int i = 0; i < _foods.Count; i++)
            {
                _foodGrid.Insert(i, _foods[i].X, _foods[i].Y);
            }
        }

        private void RecountAndRecordSnapshot(bool forceRecord)
        {
            int prey = 0, pred = 0, bear = 0, human = 0;
            float preySpeedSum = 0f, predSpeedSum = 0f;
            float preyVisionSum = 0f, predVisionSum = 0f;

            int bestLegendId = -1;
            int bestLegendScore = -1;

            for (int i = 0; i < _organisms.Count; i++)
            {
                var o = _organisms[i];
                if (!o.IsAlive) continue;

                // 명예의 전당 실시간 집계
                if (o.AgeTicks > HallOfFame.OldestAgeTicks)
                {
                    HallOfFame.OldestAgeTicks = o.AgeTicks;
                    HallOfFame.OldestId = o.Id;
                    HallOfFame.OldestSpecies = o.Species;
                }
                if (o.Kills > HallOfFame.MostKills)
                {
                    HallOfFame.MostKills = o.Kills;
                    HallOfFame.TopKillerId = o.Id;
                    HallOfFame.TopKillerSpecies = o.Species;
                }
                if (o.OffspringCount > HallOfFame.MostOffspring)
                {
                    HallOfFame.MostOffspring = o.OffspringCount;
                    HallOfFame.TopParentId = o.Id;
                    HallOfFame.TopParentSpecies = o.Species;
                }
                if (o.Dna.Speed > HallOfFame.FastestSpeed)
                {
                    HallOfFame.FastestSpeed = o.Dna.Speed;
                    HallOfFame.FastestId = o.Id;
                    HallOfFame.FastestSpecies = o.Species;
                }

                int legendScore = o.Kills * 220 + o.OffspringCount * 120 + o.AgeTicks / 8;
                if (legendScore > bestLegendScore)
                {
                    bestLegendScore = legendScore;
                    bestLegendId = o.Id;
                }

                switch (o.Species)
                {
                    case EcoSpecies.Prey:
                        prey++;
                        preySpeedSum += o.Dna.Speed;
                        preyVisionSum += o.Dna.Vision;
                        break;
                    case EcoSpecies.Predator:
                        pred++;
                        predSpeedSum += o.Dna.Speed;
                        predVisionSum += o.Dna.Vision;
                        break;
                    case EcoSpecies.Bear:
                        bear++;
                        break;
                    case EcoSpecies.Human:
                        human++;
                        break;
                }
            }

            PreyCount = prey;
            PredatorCount = pred;
            BearCount = bear;
            HumanCount = human;
            HallOfFame.LivingLegendId = bestLegendId;

            if (HallOfFame.MostKills >= 10) UnlockAchievement("kills10");
            if (MaxGeneration >= 50) UnlockAchievement("gen50");
            if (HallOfFame.OldestAgeTicks >= 1760) UnlockAchievement("age2y");
            if (_campfires.Count > 0) UnlockAchievement("fire");

            if (!forceRecord) return;

            if (_history.Count >= _config.MaxStatsHistory)
            {
                _history.RemoveAt(0);
            }

            _history.Add(new EcoStatsSnapshot
            {
                Tick = TickCount,
                PreyCount = prey,
                PredatorCount = pred,
                BearCount = bear,
                HumanCount = human,
                FoodCount = _foods.Count,
                AvgPreySpeed = prey > 0 ? preySpeedSum / prey : 0f,
                AvgPredatorSpeed = pred > 0 ? predSpeedSum / pred : 0f,
                AvgPreyVision = prey > 0 ? preyVisionSum / prey : 0f,
                AvgPredatorVision = pred > 0 ? predVisionSum / pred : 0f,
                MaxGeneration = MaxGeneration
            });
        }

        private float RandomRange(float min, float max) => min + (float)_rng.NextDouble() * (max - min);

        private static float Clamp(float val, float min, float max)
        {
            if (val < min) return min;
            if (val > max) return max;
            return val;
        }
    }
}
