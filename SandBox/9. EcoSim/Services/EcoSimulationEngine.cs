using System;
using System.Collections.Generic;
using GateHelper.EcoSim.Models;

namespace GateHelper.EcoSim.Services
{
    /// <summary>
    /// WinForms UI와 완전히 분리된 순수 C# 생태계 진화 시뮬레이션 엔진.
    /// </summary>
    public sealed class EcoSimulationEngine
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

        private void InitAchievements()
        {
            _achievements.Clear();
            _achievements.Add(new EcoAchievement { Id = "evo2", Title = "첫 진화의 발걸음", Description = "내 개체 2단계(여우) 도달" });
            _achievements.Add(new EcoAchievement { Id = "evo3", Title = "숲의 제왕", Description = "내 개체 3단계(곰) 도달" });
            _achievements.Add(new EcoAchievement { Id = "evo5", Title = "전설의 명사수", Description = "내 개체 5단계(인간 궁수) 도달" });
            _achievements.Add(new EcoAchievement { Id = "boss", Title = "거대 맹수 토벌", Description = "알파 곰 보스 처치" });
            _achievements.Add(new EcoAchievement { Id = "kills10", Title = "백전노장 사냥꾼", Description = "단일 개체 사냥 10회 달성" });
            _achievements.Add(new EcoAchievement { Id = "gen50", Title = "진화의 목격자", Description = "생태계 50세대 돌파" });
            _achievements.Add(new EcoAchievement { Id = "age2y", Title = "혹한의 생존자", Description = "나이 2년(1,760틱) 개체 탄생" });
            _achievements.Add(new EcoAchievement { Id = "fire", Title = "문명의 불꽃", Description = "인간 거점 모닥불 설치" });
        }

        private void UnlockAchievement(string id)
        {
            for (int i = 0; i < _achievements.Count; i++)
            {
                if (_achievements[i].Id == id && !_achievements[i].Unlocked)
                {
                    _achievements[i].Unlocked = true;
                    AchievementToastText = $"★ 업적 달성: {_achievements[i].Title}";
                    AchievementToastTicks = 95;
                    break;
                }
            }
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
            AlphaBossId = -1;
            _alphaBossCooldown = 0;
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

        public PlayerPerk[] RollPerkChoices(int count = 3)
        {
            var all = new[] { PlayerPerk.Swiftness, PlayerPerk.Vitality, PlayerPerk.PredatorInstinct, PlayerPerk.QuickCooldown, PlayerPerk.SharpWeapon };
            for (int i = all.Length - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                var tmp = all[i]; all[i] = all[j]; all[j] = tmp;
            }
            var res = new PlayerPerk[Math.Min(count, all.Length)];
            Array.Copy(all, res, res.Length);
            return res;
        }

        public void ApplyPlayerPerk(PlayerPerk perk)
        {
            PendingPerkChoice = false;
            string tag;
            switch (perk)
            {
                case PlayerPerk.Swiftness:
                    PlayerSpeedMult *= 1.15f;
                    tag = "속도+";
                    break;
                case PlayerPerk.Vitality:
                    PlayerVitalityMult *= 1.25f;
                    tag = "체력+";
                    break;
                case PlayerPerk.PredatorInstinct:
                    PlayerGainMult *= 1.30f;
                    tag = "흡혈+";
                    break;
                case PlayerPerk.QuickCooldown:
                    PlayerCooldownMult *= 0.75f;
                    tag = "쿨감+";
                    break;
                default:
                    PlayerDamageMult *= 1.30f;
                    tag = "강공+";
                    break;
            }
            PlayerPerkSummary = PlayerPerkSummary == "특성: 없음" ? $"특성: {tag}" : $"{PlayerPerkSummary}, {tag}";

            var p = FindOrganismById(PlayerId);
            if (p != null)
            {
                p.MaxEnergy = ComputeMaxEnergy(p.Species, p.Dna) * PlayerVitalityMult;
                p.Energy = Math.Min(p.MaxEnergy, p.Energy + p.MaxEnergy * 0.35f);
            }
        }

        private void ResetPlayerPerks()
        {
            PendingPerkChoice = false;
            PlayerSpeedMult = 1.0f;
            PlayerVitalityMult = 1.0f;
            PlayerGainMult = 1.0f;
            PlayerCooldownMult = 1.0f;
            PlayerDamageMult = 1.0f;
            PlayerPerkSummary = "특성: 없음";
        }

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

        public void SetPlayerInput(float moveX, float moveY, bool skillPressed, bool buildCampfirePressed = false)
        {
            _playerInputX = moveX;
            _playerInputY = moveY;
            if (skillPressed) _playerSkillPressed = true;
            if (buildCampfirePressed) _playerBuildCampfirePressed = true;
        }

        public void StartPlayerMode()
        {
            IsPlayerMode = true;
            PlayerDeathCount = 0;
            PlayerBestStage = 1;
            PlayerNotice = "방향키/WASD 이동 · Space 스킬/대시";
            if (FindOrganismById(PlayerId) == null)
            {
                SpawnPlayerStage1();
            }
        }

        public void StopPlayerMode()
        {
            IsPlayerMode = false;
            var p = FindOrganismById(PlayerId);
            if (p != null)
            {
                p.IsPlayer = false;
            }
            PlayerId = -1;
            PlayerNotice = "";
        }

        private void SpawnPlayerStage1()
        {
            float px = _config.WorldWidth * 0.5f;
            float py = _config.WorldHeight * 0.5f;
            for (int attempt = 0; attempt < 15; attempt++)
            {
                float tx = RandomRange(60f, _config.WorldWidth - 60f);
                float ty = RandomRange(60f, _config.WorldHeight - 60f);
                if (!IsInsideTerrain(tx, ty, 20f))
                {
                    px = tx;
                    py = ty;
                    break;
                }
            }

            var dna = new Genome(2.75f, 110f, 6.8f);
            var player = SpawnOrganism(EcoSpecies.Prey, px, py, dna, Math.Max(1, MaxGeneration), 130f);
            if (player == null && _organisms.Count > 0)
            {
                // 개체수가 꽉 찼으면 가장 약한 비플레이어 1마리 교체
                _organisms.RemoveAt(0);
                player = SpawnOrganism(EcoSpecies.Prey, px, py, dna, Math.Max(1, MaxGeneration), 130f);
            }
            if (player != null)
            {
                player.IsPlayer = true;
                PlayerId = player.Id;
                PlayerStage = 1;
                PlayerExp = 0f;
                PlayerNextExp = 200f;
            }
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

        private void StepPlayer(Organism player)
        {
            player.IsHibernating = false;

            // 1. 스킬 / 대시 발동 (Space)
            if (_playerSkillPressed)
            {
                _playerSkillPressed = false;
                if (player.AttackCooldown == 0)
                {
                    if (player.Species == EcoSpecies.Human)
                    {
                        // 4단계 인간(창): 사거리 54, 5단계 인간(활): 사거리 128 (+강공 특성 보너스)
                        bool isArcher = PlayerStage >= 5;
                        float pRange = (isArcher ? 128f : 54f) * (PlayerDamageMult > 1.05f ? 1.15f : 1.0f);
                        _organismGrid.QueryRadius(player.X, player.Y, pRange, _queryBuffer);
                        Organism bestTarget = null;
                        float bestD2 = pRange * pRange;
                        for (int i = 0; i < _queryBuffer.Count; i++)
                        {
                            var other = _organisms[_queryBuffer[i]];
                            if (!other.IsAlive || other.Id == player.Id) continue;
                            float dx = other.X - player.X;
                            float dy = other.Y - player.Y;
                            float d2 = dx * dx + dy * dy;
                            if (d2 < bestD2)
                            {
                                bestD2 = d2;
                                bestTarget = other;
                            }
                        }

                        player.AttackCooldown = Math.Max(5, (int)((isArcher ? 14 : 18) * PlayerCooldownMult));
                        if (bestTarget != null)
                        {
                            if (_hunterShots.Count < 32)
                                _hunterShots.Add(new HunterShot(player.X, player.Y, bestTarget.X, bestTarget.Y, isArcher ? 7 : 5));

                            if (bestTarget.Species == EcoSpecies.Bear)
                            {
                                bestTarget.Energy -= (isArcher ? 85f : 60f) * PlayerDamageMult;
                                WakeBearWithCharge(bestTarget);
                                if (bestTarget.Energy <= 0f)
                                {
                                    bestTarget.IsAlive = false;
                                    player.Kills++;
                                    player.MeatCount++;
                                    bool wasBoss = bestTarget.IsAlphaBoss;
                                    player.Energy = Math.Min(player.MaxEnergy, player.Energy + (wasBoss ? 260f : 130f) * PlayerGainMult);
                                    PlayerExp += (wasBoss ? 250f : 80f) * PlayerGainMult;
                                    if (wasBoss)
                                    {
                                        PlayerNotice = "위업 달성! [알파 곰 보스]를 처치했습니다!";
                                        UnlockAchievement("boss");
                                    }
                                }
                            }
                            else
                            {
                                bestTarget.IsAlive = false;
                                player.Kills++;
                                player.MeatCount++;
                                player.Energy = Math.Min(player.MaxEnergy, player.Energy + 65f * PlayerGainMult);
                                PlayerExp += 45f * PlayerGainMult;
                            }
                        }
                        else
                        {
                            float vLen = (float)Math.Sqrt(player.Vx * player.Vx + player.Vy * player.Vy);
                            float dirX = vLen > 0.01f ? player.Vx / vLen : 1f;
                            float dirY = vLen > 0.01f ? player.Vy / vLen : 0f;
                            if (_hunterShots.Count < 32)
                                _hunterShots.Add(new HunterShot(player.X, player.Y, player.X + dirX * pRange, player.Y + dirY * pRange, 5));
                        }
                    }
                    else
                    {
                        // 1~3단계: 순간 가속 대시 / 광폭 돌진
                        player.SkillActiveTicks = player.Species == EcoSpecies.Bear ? 20 : 12;
                        player.AttackCooldown = Math.Max(12, (int)((player.Species == EcoSpecies.Bear ? 75 : 42) * PlayerCooldownMult));
                    }
                }
            }

            // 2. 키보드 방향 입력 적용 (로그라이크 속도 특성 반영)
            float speedMult = (player.SkillActiveTicks > 0 ? (player.Species == EcoSpecies.Bear ? 1.85f : 1.75f) : 1.15f) * PlayerSpeedMult;
            float inLen = (float)Math.Sqrt(_playerInputX * _playerInputX + _playerInputY * _playerInputY);
            if (inLen > 0.01f)
            {
                float nx = _playerInputX / inLen;
                float ny = _playerInputY / inLen;
                player.Vx = nx * player.Dna.Speed * speedMult;
                player.Vy = ny * player.Dna.Speed * speedMult;
                MoveAndBounce(player);
            }
            else
            {
                // 키를 떼면 제자리에 멈춤 (방향만 유지)
                player.Vx *= 0.25f;
                player.Vy *= 0.25f;
            }

            // 3. 섭식 & 사냥 처리
            if (player.Species == EcoSpecies.Prey || player.Species == EcoSpecies.Bear)
            {
                _foodGrid.QueryRadius(player.X, player.Y, player.Dna.Size + 8f, _queryBuffer);
                float eatR = player.Dna.Size + 7f;
                for (int i = 0; i < _queryBuffer.Count; i++)
                {
                    int fIdx = _queryBuffer[i];
                    if (fIdx >= _foods.Count) continue;
                    var f = _foods[fIdx];
                    float dx = f.X - player.X;
                    float dy = f.Y - player.Y;
                    if (dx * dx + dy * dy <= eatR * eatR)
                    {
                        float mult = player.Species == EcoSpecies.Bear ? 1.35f : 1.15f;
                        player.Energy = Math.Min(player.MaxEnergy, player.Energy + f.EnergyValue * mult);
                        PlayerExp += player.Species == EcoSpecies.Prey ? 22f : 12f;
                        RemoveFoodSwapPop(fIdx);
                        break;
                    }
                }
            }
            else if (player.Species == EcoSpecies.Human)
            {
                TryGatherWood(player);
            }

            if (player.Species != EcoSpecies.Prey)
            {
                _organismGrid.QueryRadius(player.X, player.Y, player.Dna.Size + 16f, _queryBuffer);
                for (int i = 0; i < _queryBuffer.Count; i++)
                {
                    var other = _organisms[_queryBuffer[i]];
                    if (!other.IsAlive || other.Id == player.Id) continue;

                    bool canEat = false;
                    float expGain = 0f;
                    float energyGain = 0f;

                    if (player.Species == EcoSpecies.Predator && other.Species == EcoSpecies.Prey)
                    {
                        canEat = true; expGain = 45f; energyGain = 70f;
                    }
                    else if (player.Species == EcoSpecies.Bear && other.Species != EcoSpecies.Bear)
                    {
                        canEat = true;
                        expGain = other.Species == EcoSpecies.Human ? 110f : (other.Species == EcoSpecies.Predator ? 65f : 28f);
                        energyGain = other.Species == EcoSpecies.Human ? 140f : 85f;
                    }
                    else if (player.Species == EcoSpecies.Human &&
                             (other.Species == EcoSpecies.Prey || other.Species == EcoSpecies.Predator))
                    {
                        // 인간은 여우/토끼만 근접 제압 가능 (곰은 Space 창/활로만 상대해야 하며 몸통 박치기 즉사 불가)
                        canEat = true; expGain = 45f; energyGain = 65f;
                    }

                    if (!canEat) continue;

                    float catchR = player.Dna.Size + other.Dna.Size * 0.85f;
                    float dx = other.X - player.X;
                    float dy = other.Y - player.Y;
                    if (dx * dx + dy * dy <= catchR * catchR)
                    {
                        other.IsAlive = false;
                        player.Kills++;
                        if (player.Species == EcoSpecies.Human) player.MeatCount++;
                        player.Energy = Math.Min(player.MaxEnergy, player.Energy + energyGain);
                        PlayerExp += expGain;
                        break; // 한 틱에 1마리만 사냥하여 겹친 개체로 인한 순식간 2단 진화 방지
                    }
                }
            }

            // 곰 3단계일 때 모닥불 부수기
            if (player.Species == EcoSpecies.Bear && _campfires.Count > 0)
            {
                int cfIdx = FindNearestCampfire(player.X, player.Y, player.Dna.Size + 18f);
                if (cfIdx >= 0)
                {
                    var cf = _campfires[cfIdx];
                    cf.Hp -= 35f;
                    if (cf.Hp <= 0f)
                    {
                        RemoveCampfireSwapPop(cfIdx);
                        PlayerExp += 45f;
                    }
                    else
                    {
                        _campfires[cfIdx] = cf;
                    }
                }
            }

            // 3-2. 플레이어 인간: 모닥불 건설(F) & 모닥불 보온/고기 회복
            bool buildFirePressed = _playerBuildCampfirePressed;
            _playerBuildCampfirePressed = false;
            bool playerNearCampfire = false;
            if (player.Species == EcoSpecies.Human)
            {
                if (buildFirePressed)
                {
                    if (player.WoodCount < CampfireWoodCost)
                        PlayerNotice = $"모닥불 건설에 나뭇가지 {CampfireWoodCost}개가 필요합니다. (보유: {player.WoodCount})";
                    else if (!TryBuildCampfireWithWood(player, true))
                        PlayerNotice = "주변에 이미 모닥불이 있거나 최대 개수에 도달했습니다.";
                }
                playerNearCampfire = FindNearestCampfire(player.X, player.Y, 72f) >= 0;
                if (playerNearCampfire && player.Energy > 0f)
                {
                    player.Energy = Math.Min(player.MaxEnergy, player.Energy + 0.35f);
                }
            }

            // 4. 생존 대사 & 생존 경험치
            float drainMult = inLen > 0.01f ? 0.82f : 0.35f;
            if (playerNearCampfire) drainMult *= 0.65f;
            player.Energy -= player.Dna.ComputeMetabolicCost(player.Species) * drainMult;
            if (player.Species == EcoSpecies.Human)
            {
                TryConsumeMeatForRecovery(player, playerNearCampfire, true);
            }
            if (player.Energy <= 0f)
            {
                player.IsAlive = false;
                return;
            }

            PlayerExp += 0.16f;
            TryEvolvePlayer(player);
        }

        private void TryEvolvePlayer(Organism player)
        {
            if (PlayerStage >= 5 || PlayerExp < PlayerNextExp) return;

            PlayerStage++;
            if (PlayerStage > PlayerBestStage) PlayerBestStage = PlayerStage;
            PlayerExp = 0f;

            switch (PlayerStage)
            {
                case 2:
                    player.Species = EcoSpecies.Predator;
                    player.Dna = new Genome(3.05f, 125f, 8.6f);
                    PlayerNextExp = 320f;
                    PlayerNotice = "진화 성공! [2단계: 육식(여우)]";
                    break;
                case 3:
                    player.Species = EcoSpecies.Bear;
                    player.Dna = new Genome(2.75f, 135f, 12.2f);
                    PlayerNextExp = 520f;
                    PlayerNotice = "진화 성공! [3단계: 최상위 곰]";
                    break;
                case 4:
                    player.Species = EcoSpecies.Human;
                    player.Dna = new Genome(2.85f, 145f, 9.2f);
                    PlayerNextExp = 480f;
                    PlayerNotice = "진화 성공! [4단계: 인간(창)]";
                    break;
                case 5:
                    player.Species = EcoSpecies.Human;
                    player.Dna = new Genome(2.95f, 160f, 9.5f);
                    PlayerNextExp = 999f;
                    PlayerNotice = "최종 진화 달성! [5단계: 인간(활)]";
                    break;
            }

            player.MaxEnergy = ComputeMaxEnergy(player.Species, player.Dna) * PlayerVitalityMult;
            player.Energy = player.MaxEnergy * 0.85f;
            player.SkillActiveTicks = 0;
            player.AttackCooldown = 0;
            PendingPerkChoice = true;

            if (PlayerStage >= 2) UnlockAchievement("evo2");
            if (PlayerStage >= 3) UnlockAchievement("evo3");
            if (PlayerStage >= 5) UnlockAchievement("evo5");
        }

        private void StepPrey(Organism prey, float mutationRate)
        {
            // 1. 시야 내 가장 가까운 포식자 탐색 (도주 우선)
            float visionSq = prey.Dna.Vision * prey.Dna.Vision;
            _organismGrid.QueryRadius(prey.X, prey.Y, prey.Dna.Vision, _queryBuffer);

            Organism nearestPredator = null;
            float nearestPredDistSq = visionSq;

            for (int i = 0; i < _queryBuffer.Count; i++)
            {
                var other = _organisms[_queryBuffer[i]];
                if (!other.IsAlive || other.Species == EcoSpecies.Prey) continue;
                float dx = other.X - prey.X;
                float dy = other.Y - prey.Y;
                float d2 = dx * dx + dy * dy;
                if (d2 < nearestPredDistSq)
                {
                    nearestPredDistSq = d2;
                    nearestPredator = other;
                }
            }

            if (nearestPredator != null)
            {
                SteerAway(prey, nearestPredator.X, nearestPredator.Y, 1.12f);
                // 위협을 피해 도망칠 때 잠시 탐색 모드로 전환해 같은 자리로 되돌아오는 핑퐁 방지
                if (prey.EscapeTicks < 18) prey.EscapeTicks = 18;
            }
            else if (prey.EscapeTicks > 0)
            {
                Wander(prey);
            }
            else
            {
                // 45틱마다 제자리 맴돌기(핑퐁) 감지 -> 강제 탐색 이탈
                if ((prey.AgeTicks % 45) == 0)
                {
                    float dxA = prey.X - prey.AnchorX;
                    float dyA = prey.Y - prey.AnchorY;
                    if (dxA * dxA + dyA * dyA < 26f * 26f)
                    {
                        prey.EscapeTicks = 35;
                    }
                    prey.AnchorX = prey.X;
                    prey.AnchorY = prey.Y;
                }

                // 2. 시야 내 가장 가까운 '안전한' 먹이 탐색 (모닥불/위협 근처 먹이 제외)
                _foodGrid.QueryRadius(prey.X, prey.Y, prey.Dna.Vision, _queryBuffer);
                int nearestFoodIdx = -1;
                float nearestFoodDistSq = visionSq;

                for (int i = 0; i < _queryBuffer.Count; i++)
                {
                    int fIdx = _queryBuffer[i];
                    if (fIdx >= _foods.Count) continue;
                    var f = _foods[fIdx];
                    if (f.EnergyValue <= 0f) continue;
                    if (FindNearestCampfire(f.X, f.Y, 64f) >= 0) continue;

                    float dx = f.X - prey.X;
                    float dy = f.Y - prey.Y;
                    float d2 = dx * dx + dy * dy;
                    if (d2 < nearestFoodDistSq)
                    {
                        nearestFoodDistSq = d2;
                        nearestFoodIdx = fIdx;
                    }
                }

                if (nearestFoodIdx >= 0)
                {
                    var targetFood = _foods[nearestFoodIdx];
                    SteerToward(prey, targetFood.X, targetFood.Y, 1.0f);

                    float eatRadius = prey.Dna.Size + 5f;
                    if (nearestFoodDistSq <= eatRadius * eatRadius)
                    {
                        prey.Energy = Math.Min(prey.MaxEnergy, prey.Energy + targetFood.EnergyValue);
                        RemoveFoodSwapPop(nearestFoodIdx);
                    }
                }
                else
                {
                    Wander(prey);
                }
            }

            MoveAndBounce(prey);
            float seasonCostMult = GetSeasonMetabolicMultiplier();
            prey.Energy -= prey.Dna.ComputeMetabolicCost(EcoSpecies.Prey) * seasonCostMult;

            if (prey.Energy <= 0f)
            {
                prey.IsAlive = false;
                return;
            }

            if (prey.Energy >= _config.PreyReproduceThreshold &&
                (_organisms.Count + _pendingOffspring.Count) < _config.MaxOrganismCount)
            {
                Reproduce(prey, mutationRate);
            }
        }

        private void StepPredator(Organism pred, float mutationRate)
        {
            float visionSq = pred.Dna.Vision * pred.Dna.Vision;
            _organismGrid.QueryRadius(pred.X, pred.Y, pred.Dna.Vision, _queryBuffer);

            Organism nearestThreat = null;
            float nearestThreatDistSq = visionSq;
            Organism nearestPrey = null;
            float nearestPreyDistSq = visionSq;

            for (int i = 0; i < _queryBuffer.Count; i++)
            {
                var other = _organisms[_queryBuffer[i]];
                if (!other.IsAlive) continue;

                float dx = other.X - pred.X;
                float dy = other.Y - pred.Y;
                float d2 = dx * dx + dy * dy;

                if (other.Species == EcoSpecies.Bear || other.Species == EcoSpecies.Human)
                {
                    if (!other.IsHibernating && d2 < nearestThreatDistSq)
                    {
                        nearestThreatDistSq = d2;
                        nearestThreat = other;
                    }
                    continue;
                }

                if (other.Species != EcoSpecies.Prey) continue;

                float effectiveVisionSq = nearestPreyDistSq;
                if (EnableShelters && IsInsideShelter(other.X, other.Y))
                {
                    float stealthR = pred.Dna.Vision * 0.42f;
                    float stealthSq = stealthR * stealthR;
                    if (stealthSq < effectiveVisionSq) effectiveVisionSq = stealthSq;
                }

                if (d2 < effectiveVisionSq)
                {
                    nearestPreyDistSq = d2;
                    nearestPrey = other;
                }
            }

            if (nearestThreat != null)
            {
                SteerAway(pred, nearestThreat.X, nearestThreat.Y, 1.10f);
            }
            else if (nearestPrey != null)
            {
                SteerToward(pred, nearestPrey.X, nearestPrey.Y, 1.05f);
                float catchR = pred.Dna.Size + nearestPrey.Dna.Size * 0.7f;
                if (nearestPreyDistSq <= catchR * catchR)
                {
                    nearestPrey.IsAlive = false;
                    pred.Kills++;
                    pred.Energy = Math.Min(pred.MaxEnergy, pred.Energy + _config.PreyEnergyValue + nearestPrey.Dna.Size * 2f);
                }
            }
            else
            {
                Wander(pred);
            }

            ApplySameSpeciesSeparation(pred, 22f);
            MoveAndBounce(pred);
            float seasonCostMult = GetSeasonMetabolicMultiplier();
            pred.Energy -= pred.Dna.ComputeMetabolicCost(EcoSpecies.Predator) * seasonCostMult;

            if (pred.Energy <= 0f)
            {
                pred.IsAlive = false;
                return;
            }

            if (pred.Energy >= _config.PredatorReproduceThreshold &&
                (_organisms.Count + _pendingOffspring.Count) < _config.MaxOrganismCount)
            {
                Reproduce(pred, mutationRate);
            }
        }

        private static void WakeBearWithCharge(Organism bear)
        {
            if (!bear.IsHibernating) return;
            bear.IsHibernating = false;
            bear.SkillActiveTicks = 45;
            bear.AttackCooldown = 260;
        }

        private void StepBear(Organism bear, float mutationRate)
        {
            if (EnableSeasons && CurrentSeason == EcoSeason.Winter && bear.SkillActiveTicks == 0)
            {
                bear.IsHibernating = true;
                bear.Energy -= bear.Dna.ComputeMetabolicCost(EcoSpecies.Bear) * 0.14f;
                if (bear.Energy <= 0f) bear.IsAlive = false;
                return;
            }
            bear.IsHibernating = false;

            float effectiveVision = bear.SkillActiveTicks > 0 ? bear.Dna.Vision * 1.5f : bear.Dna.Vision;
            float visionSq = effectiveVision * effectiveVision;
            _organismGrid.QueryRadius(bear.X, bear.Y, effectiveVision, _queryBuffer);

            Organism targetPred = null;
            float predDistSq = visionSq;
            Organism targetHuman = null;
            float humanDistSq = visionSq;
            Organism targetPrey = null;
            float preyDistSq = visionSq;

            for (int i = 0; i < _queryBuffer.Count; i++)
            {
                var other = _organisms[_queryBuffer[i]];
                if (!other.IsAlive || other.Id == bear.Id) continue;
                float dx = other.X - bear.X;
                float dy = other.Y - bear.Y;
                float d2 = dx * dx + dy * dy;

                if (other.Species == EcoSpecies.Predator && d2 < predDistSq)
                {
                    predDistSq = d2;
                    targetPred = other;
                }
                else if (other.Species == EcoSpecies.Human && d2 < humanDistSq)
                {
                    humanDistSq = d2;
                    targetHuman = other;
                }
                else if (other.Species == EcoSpecies.Prey && d2 < preyDistSq)
                {
                    preyDistSq = d2;
                    targetPrey = other;
                }
            }

            // 곰 우선순위: 가까운 인간(영역 대립) -> 육식동물(여우) -> 초식동물 -> 풀(잡식)
            Organism huntTarget = null;
            float huntDistSq = float.MaxValue;
            float humanAggroRange = bear.SkillActiveTicks > 0 ? effectiveVision : bear.Dna.Vision * 0.75f;
            if (targetHuman != null && humanDistSq < humanAggroRange * humanAggroRange)
            {
                huntTarget = targetHuman;
                huntDistSq = humanDistSq;
            }
            else if (targetPred != null)
            {
                huntTarget = targetPred;
                huntDistSq = predDistSq;
            }
            else if (targetPrey != null)
            {
                huntTarget = targetPrey;
                huntDistSq = preyDistSq;
            }

            if (huntTarget != null)
            {
                // 곰 전용 스킬: 죽기 직전(체력 30% 미만) 발악 광폭 돌진 (긴 쿨타임 260틱)
                if (bear.SkillActiveTicks == 0 && bear.AttackCooldown == 0 &&
                    bear.Energy < bear.MaxEnergy * 0.30f)
                {
                    bear.SkillActiveTicks = 20;
                    bear.AttackCooldown = 260;
                }

                float moveMult = bear.SkillActiveTicks > 0 ? 1.85f : 1.04f;
                SteerToward(bear, huntTarget.X, huntTarget.Y, moveMult);
                float catchR = bear.Dna.Size + huntTarget.Dna.Size * 0.8f;
                if (huntDistSq <= catchR * catchR)
                {
                    if (huntTarget.Species == EcoSpecies.Human)
                    {
                        float dmg = bear.SkillActiveTicks > 0 ? 165f : 120f;
                        huntTarget.Energy -= dmg;
                        if (huntTarget.Energy <= 0f)
                        {
                            huntTarget.IsAlive = false;
                            bear.Kills++;
                            bear.Energy = Math.Min(bear.MaxEnergy, bear.Energy + 135f);
                        }
                    }
                    else
                    {
                        huntTarget.IsAlive = false;
                        bear.Kills++;
                        float gain = huntTarget.Species == EcoSpecies.Predator ? 95f : 52f;
                        bear.Energy = Math.Min(bear.MaxEnergy, bear.Energy + gain);
                    }
                }
            }
            else
            {
                // 곰이 근처 인간 모닥불을 발견하면 습격 시도
                int targetCampIdx = FindNearestCampfire(bear.X, bear.Y, bear.Dna.Vision * 0.9f);
                if (targetCampIdx >= 0)
                {
                    var cf = _campfires[targetCampIdx];
                    SteerToward(bear, cf.X, cf.Y, 1.0f);
                    float dx = cf.X - bear.X;
                    float dy = cf.Y - bear.Y;
                    if (dx * dx + dy * dy <= (bear.Dna.Size + 18f) * (bear.Dna.Size + 18f))
                    {
                        cf.Hp -= 24f;
                        if (cf.Hp <= 0f)
                        {
                            RemoveCampfireSwapPop(targetCampIdx);
                        }
                        else
                        {
                            _campfires[targetCampIdx] = cf;
                        }
                    }
                }
                else
                {
                    // 사냥감이 없으면 잡식성으로 풀(열매) 섭취
                    _foodGrid.QueryRadius(bear.X, bear.Y, bear.Dna.Vision * 0.8f, _queryBuffer);
                    int nearestFoodIdx = -1;
                    float nearestFoodDistSq = visionSq;
                    for (int i = 0; i < _queryBuffer.Count; i++)
                    {
                        int fIdx = _queryBuffer[i];
                        if (fIdx >= _foods.Count) continue;
                        var f = _foods[fIdx];
                        float dx = f.X - bear.X;
                        float dy = f.Y - bear.Y;
                        float d2 = dx * dx + dy * dy;
                        if (d2 < nearestFoodDistSq)
                        {
                            nearestFoodDistSq = d2;
                            nearestFoodIdx = fIdx;
                        }
                    }

                    if (nearestFoodIdx >= 0)
                    {
                        var f = _foods[nearestFoodIdx];
                        SteerToward(bear, f.X, f.Y, 0.9f);
                        float eatR = bear.Dna.Size + 5f;
                        if (nearestFoodDistSq <= eatR * eatR)
                        {
                            bear.Energy = Math.Min(bear.MaxEnergy, bear.Energy + f.EnergyValue * 1.35f);
                            RemoveFoodSwapPop(nearestFoodIdx);
                        }
                    }
                    else
                    {
                        Wander(bear);
                    }
                }
            }

            ApplySameSpeciesSeparation(bear, 30f);
            MoveAndBounce(bear);
            bear.Energy -= bear.Dna.ComputeMetabolicCost(EcoSpecies.Bear) * GetSeasonMetabolicMultiplier();

            if (bear.Energy <= 0f)
            {
                bear.IsAlive = false;
                return;
            }

            if (bear.Energy >= _config.BearReproduceThreshold &&
                BearCount < 6 &&
                (_organisms.Count + _pendingOffspring.Count) < _config.MaxOrganismCount)
            {
                Reproduce(bear, mutationRate);
            }
        }

        private void StepHuman(Organism human, float mutationRate)
        {
            human.IsHibernating = false;
            float visionSq = human.Dna.Vision * human.Dna.Vision;
            _organismGrid.QueryRadius(human.X, human.Y, human.Dna.Vision, _queryBuffer);

            Organism targetBear = null;
            float bearDistSq = visionSq;
            Organism targetPred = null;
            float predDistSq = visionSq;
            Organism targetPrey = null;
            float preyDistSq = visionSq;

            for (int i = 0; i < _queryBuffer.Count; i++)
            {
                var other = _organisms[_queryBuffer[i]];
                if (!other.IsAlive || other.Id == human.Id) continue;
                float dx = other.X - human.X;
                float dy = other.Y - human.Y;
                float d2 = dx * dx + dy * dy;

                if (other.Species == EcoSpecies.Bear && d2 < bearDistSq)
                {
                    bearDistSq = d2;
                    targetBear = other;
                }
                else if (other.Species == EcoSpecies.Predator && d2 < predDistSq)
                {
                    predDistSq = d2;
                    targetPred = other;
                }
                else if (other.Species == EcoSpecies.Prey && d2 < preyDistSq)
                {
                    preyDistSq = d2;
                    targetPrey = other;
                }
            }

            bool isWinter = EnableSeasons && CurrentSeason == EcoSeason.Winter;
            int myCampIdx = human.CampfireGroupId > 0 ? FindCampfireByGroupId(human.CampfireGroupId) : -1;
            if (myCampIdx < 0)
            {
                human.CampfireGroupId = 0;
                int joinIdx = FindNearestCampfire(human.X, human.Y, isWinter ? 520f : 220f);
                if (joinIdx >= 0)
                {
                    human.CampfireGroupId = _campfires[joinIdx].GroupId;
                    myCampIdx = joinIdx;
                }
            }

            bool shelterByFire = isWinter && myCampIdx >= 0 && human.Energy >= human.MaxEnergy * 0.35f;

            bool isArcher = human.Kills >= 3;
            float weaponRange = isArcher ? 94f : 48f;

            // 겨울에는 본인 그룹 모닥불 반경 안에서 방어/활동, 굶주릴 때만 원정 사냥
            Organism target;
            if (isWinter && myCampIdx >= 0)
            {
                var groupCamp = _campfires[myCampIdx];
                float campZoneR = groupCamp.Radius * 1.45f;
                float campZoneSq = campZoneR * campZoneR;
                Organism bearInZone = (targetBear != null && ((targetBear.X - groupCamp.X) * (targetBear.X - groupCamp.X) + (targetBear.Y - groupCamp.Y) * (targetBear.Y - groupCamp.Y) <= campZoneSq || bearDistSq <= weaponRange * weaponRange)) ? targetBear : null;
                Organism predInZone = (targetPred != null && ((targetPred.X - groupCamp.X) * (targetPred.X - groupCamp.X) + (targetPred.Y - groupCamp.Y) * (targetPred.Y - groupCamp.Y) <= campZoneSq || predDistSq <= weaponRange * weaponRange)) ? targetPred : null;
                Organism preyInZone = (targetPrey != null && human.Energy < human.MaxEnergy * 0.55f && ((targetPrey.X - groupCamp.X) * (targetPrey.X - groupCamp.X) + (targetPrey.Y - groupCamp.Y) * (targetPrey.Y - groupCamp.Y) <= campZoneSq)) ? targetPrey : null;
                target = bearInZone ?? predInZone ?? preyInZone;
            }
            else if (isWinter)
            {
                const float selfDefendSq = 80f * 80f;
                if (targetBear != null && bearDistSq <= selfDefendSq) target = targetBear;
                else if (human.Energy < human.MaxEnergy * 0.65f) target = targetBear ?? targetPred ?? targetPrey;
                else target = null;
            }
            else
            {
                target = targetBear ?? targetPred ?? (human.Energy < human.MaxEnergy * 0.72f ? targetPrey : null);
            }
            float targetDistSq = target == targetBear ? bearDistSq : (target == targetPred ? predDistSq : preyDistSq);

            if (target != null)
            {
                float kiteMinRange = isArcher ? 48f : 28f;
                float winterMoveMult = isWinter ? 0.75f : 1.0f;

                if (target.Species == EcoSpecies.Bear && targetDistSq < kiteMinRange * kiteMinRange)
                {
                    // 곰이 너무 가까우면 뒤로 빠지며 카이팅
                    SteerAway(human, target.X, target.Y, 1.08f * winterMoveMult);
                }
                else if (targetDistSq > weaponRange * weaponRange && !shelterByFire)
                {
                    SteerToward(human, target.X, target.Y, 1.0f * winterMoveMult);
                }
                else
                {
                    // 사거리 내 조준 감속
                    human.Vx *= 0.85f;
                    human.Vy *= 0.85f;
                }

                if (targetDistSq <= weaponRange * weaponRange && human.AttackCooldown == 0)
                {
                    human.AttackCooldown = isArcher ? 30 : 24;
                    if (_hunterShots.Count < 32)
                    {
                        _hunterShots.Add(new HunterShot(human.X, human.Y, target.X, target.Y, isArcher ? 6 : 4));
                    }

                    if (target.Species == EcoSpecies.Bear)
                    {
                        target.Energy -= isArcher ? 72f : 55f;
                        WakeBearWithCharge(target);
                        if (target.Energy <= 0f)
                        {
                            target.IsAlive = false;
                            human.Kills++;
                            human.MeatCount++;
                            human.Energy = Math.Min(human.MaxEnergy, human.Energy + 130f);
                        }
                    }
                    else if (target.IsPlayer)
                    {
                        // 플레이어 개체는 창/화살 1방에 즉사하지 않고 회피 기회 부여
                        target.Energy -= isArcher ? 65f : 45f;
                        if (target.Energy <= 0f)
                        {
                            target.IsAlive = false;
                            human.Kills++;
                            human.MeatCount++;
                            human.Energy = Math.Min(human.MaxEnergy, human.Energy + 75f);
                        }
                    }
                    else
                    {
                        target.IsAlive = false;
                        human.Kills++;
                        human.MeatCount++;
                        float gain = target.Species == EcoSpecies.Predator ? 72f : 44f;
                        human.Energy = Math.Min(human.MaxEnergy, human.Energy + gain);
                    }
                }
            }
            else if (isWinter && myCampIdx >= 0)
            {
                var cf = _campfires[myCampIdx];
                float dx = cf.X - human.X;
                float dy = cf.Y - human.Y;
                float d2 = dx * dx + dy * dy;
                float roamR = cf.Radius * 1.45f;
                if (d2 > roamR * roamR)
                {
                    SteerToward(human, cf.X, cf.Y, 0.92f);
                }
                else if (shelterByFire && d2 > 36f * 36f)
                {
                    SteerToward(human, cf.X, cf.Y, 0.78f);
                }
                else
                {
                    // 본인 그룹 모닥불 범위 내 순찰/휴식
                    human.Vx *= 0.78f;
                    human.Vy *= 0.78f;
                }
            }
            else
            {
                Wander(human);
                if (isWinter)
                {
                    human.Vx *= 0.72f;
                    human.Vy *= 0.72f;
                }
            }

            ApplySameSpeciesSeparation(human, 26f);
            MoveAndBounce(human);
            if (target == null)
            {
                TryGatherWood(human);
            }

            bool nearCampfire = FindNearestCampfire(human.X, human.Y, 72f) >= 0;
            float humanSeasonCost = GetSeasonMetabolicMultiplier() * (nearCampfire ? 0.70f : 1.0f);
            human.Energy -= human.Dna.ComputeMetabolicCost(EcoSpecies.Human) * humanSeasonCost;

            if (nearCampfire && human.Energy > 0f)
            {
                human.Energy = Math.Min(human.MaxEnergy, human.Energy + 0.35f);
            }

            TryConsumeMeatForRecovery(human, nearCampfire, false);

            // 겨울에만 나뭇가지가 충분히 모이면 거점 모닥불 설치 (봄/여름/가을엔 비축)
            if (isWinter && human.WoodCount >= CampfireWoodCost)
            {
                TryBuildCampfireWithWood(human, false);
            }

            if (human.Energy <= 0f)
            {
                human.IsAlive = false;
                return;
            }

            if (human.Energy >= _config.HumanReproduceThreshold &&
                HumanCount < 4 &&
                (_organisms.Count + _pendingOffspring.Count) < _config.MaxOrganismCount)
            {
                Reproduce(human, mutationRate);
            }
        }

        private bool TryBuildCampfireWithWood(Organism human, bool isPlayer)
        {
            if (human.WoodCount < CampfireWoodCost || _campfires.Count >= MaxCampfires) return false;
            if (FindNearestCampfire(human.X, human.Y, 150f) >= 0) return false;

            human.WoodCount -= CampfireWoodCost;
            int groupId = _nextCampfireGroupId++;
            var cf = new CampfireSite(groupId, human.X, human.Y, 72f, 120f, CampfireDurationTicks);
            _campfires.Add(cf);
            human.CampfireGroupId = groupId;

            // 주변 무소속 인간들도 이 모닥불 그룹에 편입
            const float joinRadiusSq = 160f * 160f;
            for (int i = 0; i < _organisms.Count; i++)
            {
                var other = _organisms[i];
                if (!other.IsAlive || other.Species != EcoSpecies.Human || other.IsPlayer) continue;
                if (other.CampfireGroupId > 0 && FindCampfireByGroupId(other.CampfireGroupId) >= 0) continue;
                float dx = other.X - cf.X;
                float dy = other.Y - cf.Y;
                if (dx * dx + dy * dy <= joinRadiusSq)
                {
                    other.CampfireGroupId = groupId;
                }
            }

            if (isPlayer)
            {
                PlayerNotice = $"🔥 나뭇가지 {CampfireWoodCost}개로 모닥불을 건설했습니다!";
            }
            return true;
        }

        private void TryConsumeMeatForRecovery(Organism human, bool nearCampfire, bool isPlayer)
        {
            if (human.MeatCount <= 0 || human.Energy <= 0f) return;

            bool needEmergencyHeal = human.Energy < human.MaxEnergy * 0.38f;
            bool restAtWinterFire = nearCampfire && CurrentSeason == EcoSeason.Winter && human.Energy < human.MaxEnergy * 0.68f;
            if (!needEmergencyHeal && !restAtWinterFire) return;

            human.MeatCount--;
            human.Energy = Math.Min(human.MaxEnergy, human.Energy + 85f);
            if (isPlayer)
            {
                PlayerNotice = "🍖 고기를 먹어 에너지를 회복했습니다! (+85)";
            }
        }

        private void TryGatherWood(Organism human)
        {
            float gatherR = human.Dna.Size + 6f;
            _foodGrid.QueryRadius(human.X, human.Y, gatherR, _queryBuffer);
            float gatherRSq = gatherR * gatherR;
            for (int i = 0; i < _queryBuffer.Count; i++)
            {
                int fIdx = _queryBuffer[i];
                if (fIdx >= _foods.Count) continue;
                var f = _foods[fIdx];
                float dx = f.X - human.X;
                float dy = f.Y - human.Y;
                if (dx * dx + dy * dy <= gatherRSq)
                {
                    human.WoodCount++;
                    RemoveFoodSwapPop(fIdx);
                    break;
                }
            }
        }

        private void Reproduce(Organism parent, float mutationRate)
        {
            parent.Energy *= 0.48f;
            parent.OffspringCount++;

            var childDna = parent.Dna.Mutate(_rng, mutationRate, _config.MutationScale);
            int childGen = parent.Generation + 1;
            if (childGen > MaxGeneration)
                MaxGeneration = childGen;

            double angle = _rng.NextDouble() * Math.PI * 2.0;
            float maxEnergy = ComputeMaxEnergy(parent.Species, childDna);
            float spawnDist = parent.Species == EcoSpecies.Prey ? 10f : parent.Dna.Size * 2.4f;

            var child = new Organism
            {
                Id = _nextOrganismId++,
                Species = parent.Species,
                Generation = childGen,
                Dna = childDna,
                X = Clamp(parent.X + (float)Math.Cos(angle) * spawnDist, 10f, _config.WorldWidth - 10f),
                Y = Clamp(parent.Y + (float)Math.Sin(angle) * spawnDist, 10f, _config.WorldHeight - 10f),
                Vx = (float)Math.Cos(angle) * childDna.Speed,
                Vy = (float)Math.Sin(angle) * childDna.Speed,
                Energy = parent.Energy,
                MaxEnergy = maxEnergy,
                IsAlive = true
            };

            _pendingOffspring.Add(child);
        }

        private void ApplySameSpeciesSeparation(Organism org, float minSpacing)
        {
            _organismGrid.QueryRadius(org.X, org.Y, minSpacing, _queryBuffer);
            float minDistSq = minSpacing * minSpacing;
            float pushX = 0f;
            float pushY = 0f;
            int neighbors = 0;

            for (int i = 0; i < _queryBuffer.Count; i++)
            {
                int idx = _queryBuffer[i];
                if (idx < 0 || idx >= _organisms.Count) continue;
                var other = _organisms[idx];
                if (!other.IsAlive || other.Id == org.Id || other.Species != org.Species) continue;

                float dx = org.X - other.X;
                float dy = org.Y - other.Y;
                float d2 = dx * dx + dy * dy;
                if (d2 > 0.0001f && d2 < minDistSq)
                {
                    float dist = (float)Math.Sqrt(d2);
                    float weight = (minSpacing - dist) / minSpacing;
                    pushX += (dx / dist) * weight;
                    pushY += (dy / dist) * weight;
                    neighbors++;
                }
                else if (d2 <= 0.0001f)
                {
                    // 완전히 겹친 경우 임의 방향으로 분리
                    pushX += (org.Id % 2 == 0 ? 1f : -1f) * 0.5f;
                    pushY += (org.Id % 3 == 0 ? 1f : -1f) * 0.5f;
                    neighbors++;
                }
            }

            if (neighbors > 0)
            {
                org.Vx += pushX * 0.9f;
                org.Vy += pushY * 0.9f;
                float speed = (float)Math.Sqrt(org.Vx * org.Vx + org.Vy * org.Vy);
                float maxAllowed = org.Dna.Speed * (org.SkillActiveTicks > 0 ? 1.85f : 1.15f);
                if (speed > maxAllowed && speed > 0.001f)
                {
                    org.Vx = (org.Vx / speed) * maxAllowed;
                    org.Vy = (org.Vy / speed) * maxAllowed;
                }
            }
        }

        private void SteerToward(Organism org, float tx, float ty, float speedMultiplier)
        {
            float dx = tx - org.X;
            float dy = ty - org.Y;
            float len = (float)Math.Sqrt(dx * dx + dy * dy);
            if (len < 0.001f) return;

            float desiredVx = (dx / len) * org.Dna.Speed * speedMultiplier;
            float desiredVy = (dy / len) * org.Dna.Speed * speedMultiplier;
            org.Vx = org.Vx * 0.72f + desiredVx * 0.28f;
            org.Vy = org.Vy * 0.72f + desiredVy * 0.28f;
        }

        private void SteerAway(Organism org, float tx, float ty, float speedMultiplier)
        {
            float dx = org.X - tx;
            float dy = org.Y - ty;
            float len = (float)Math.Sqrt(dx * dx + dy * dy);
            if (len < 0.001f) return;

            float desiredVx = (dx / len) * org.Dna.Speed * speedMultiplier;
            float desiredVy = (dy / len) * org.Dna.Speed * speedMultiplier;
            org.Vx = org.Vx * 0.65f + desiredVx * 0.35f;
            org.Vy = org.Vy * 0.65f + desiredVy * 0.35f;
        }

        private void Wander(Organism org)
        {
            double angle = Math.Atan2(org.Vy, org.Vx) + (_rng.NextDouble() - 0.5) * 0.45;
            org.Vx = (float)Math.Cos(angle) * org.Dna.Speed;
            org.Vy = (float)Math.Sin(angle) * org.Dna.Speed;
        }

        private void MoveAndBounce(Organism org)
        {
            org.X += org.Vx;
            org.Y += org.Vy;

            float margin = org.Dna.Size;
            if (org.X < margin)
            {
                org.X = margin;
                org.Vx = Math.Abs(org.Vx);
            }
            else if (org.X > _config.WorldWidth - margin)
            {
                org.X = _config.WorldWidth - margin;
                org.Vx = -Math.Abs(org.Vx);
            }

            if (org.Y < margin)
            {
                org.Y = margin;
                org.Vy = Math.Abs(org.Vy);
            }
            else if (org.Y > _config.WorldHeight - margin)
            {
                org.Y = _config.WorldHeight - margin;
                org.Vy = -Math.Abs(org.Vy);
            }

            // 바위/연못 지형 충돌 및 우회
            for (int i = 0; i < _terrains.Count; i++)
            {
                var t = _terrains[i];
                float dx = org.X - t.X;
                float dy = org.Y - t.Y;
                float minD = t.Radius + org.Dna.Size;
                float d2 = dx * dx + dy * dy;
                if (d2 < minD * minD)
                {
                    float d = (float)Math.Sqrt(d2);
                    if (d < 0.001f) { dx = 1f; dy = 0f; d = 1f; }
                    float nx = dx / d;
                    float ny = dy / d;

                    // 바위는 고체 장애물, 연못 깊은 중심은 반발 + 접선 우회 슬라이드
                    float pushR = t.IsWater ? (t.Radius * 0.72f + org.Dna.Size) : minD;
                    if (d < pushR)
                    {
                        org.X = t.X + nx * pushR;
                        org.Y = t.Y + ny * pushR;
                        float dot = org.Vx * nx + org.Vy * ny;
                        if (dot < 0f)
                        {
                            org.Vx -= 1.2f * dot * nx;
                            org.Vy -= 1.2f * dot * ny;
                        }
                        // 바위/연못에 막혀 정체되지 않도록 접선 방향으로 부드럽게 우회
                        float tangentDir = (org.Id % 2 == 0) ? 1f : -1f;
                        org.Vx += -ny * tangentDir * (org.Dna.Speed * 0.45f);
                        org.Vy += nx * tangentDir * (org.Dna.Speed * 0.45f);
                    }
                }
            }
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
            for (int i = _campfires.Count - 1; i >= 0; i--)
            {
                var cf = _campfires[i];
                cf.RemainingTicks--;
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
