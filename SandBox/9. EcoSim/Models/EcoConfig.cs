namespace GateHelper.EcoSim.Models
{
    /// <summary>
    /// 생태계 개체 종 구분.
    /// </summary>
    public enum EcoSpecies
    {
        Prey = 0,
        Predator = 1,
        Bear = 2,
        Human = 3
    }

    /// <summary>
    /// 마우스 클릭 시 적용할 신(God) 개입 도구 모드.
    /// </summary>
    public enum EcoToolMode
    {
        Inspect = 0,
        DropFood = 1,
        SpawnPrey = 2,
        SpawnPredator = 3,
        SpawnBear = 4,
        SpawnHuman = 5
    }

    /// <summary>
    /// 생태계 전역 환경 이벤트.
    /// </summary>
    public enum EcoEnvironmentEvent
    {
        None = 0,
        Abundance = 1,
        Drought = 2,
        HighMutation = 3
    }

    /// <summary>
    /// 실험 시나리오 프리셋.
    /// </summary>
    public enum EcoPreset
    {
        Default = 0,
        BearVsHumanWar = 1,
        IceAge = 2,
        HyperEvolution = 3
    }

    /// <summary>
    /// 내 개체 진화 시 선택하는 로그라이크 특성.
    /// </summary>
    public enum PlayerPerk
    {
        Swiftness = 0,
        Vitality = 1,
        PredatorInstinct = 2,
        QuickCooldown = 3,
        SharpWeapon = 4
    }

    /// <summary>
    /// 시뮬레이션 밸런스 및 월드 설정값.
    /// </summary>
    public sealed class EcoConfig
    {
        public float WorldWidth { get; set; } = 1920f;
        public float WorldHeight { get; set; } = 1280f;

        public int InitialFoodCount { get; set; } = 210;
        public int InitialPreyCount { get; set; } = 64;
        public int InitialPredatorCount { get; set; } = 14;
        public int InitialBearCount { get; set; } = 3;
        public int InitialHumanCount { get; set; } = 3;

        public int MaxFoodCount { get; set; } = 540;
        public int MaxOrganismCount { get; set; } = 460;

        public float BaseFoodSpawnPerTick { get; set; } = 1.25f;
        public float FoodEnergyValue { get; set; } = 36f;
        public float PreyEnergyValue { get; set; } = 68f;

        public float MutationRate { get; set; } = 0.25f;
        public float MutationScale { get; set; } = 0.16f;

        public float PreyReproduceThreshold { get; set; } = 115f;
        public float PredatorReproduceThreshold { get; set; } = 155f;
        public float BearReproduceThreshold { get; set; } = 330f;
        public float HumanReproduceThreshold { get; set; } = 230f;

        public int StatsSampleIntervalTicks { get; set; } = 8;
        public int MaxStatsHistory { get; set; } = 180;
    }
}
