namespace GateHelper.EcoSim.Models
{
    /// <summary>
    /// 사계절 순환 상태.
    /// </summary>
    public enum EcoSeason
    {
        Spring = 0,
        Summer = 1,
        Autumn = 2,
        Winter = 3
    }

    /// <summary>
    /// 초식동물이 포식자의 시야를 피할 수 있는 수풀 은신처 구역.
    /// </summary>
    public struct EcoShelterZone
    {
        public float X;
        public float Y;
        public float Radius;

        public EcoShelterZone(float x, float y, float radius)
        {
            X = x;
            Y = y;
            Radius = radius;
        }
    }

    /// <summary>
    /// 개체 사망/사냥 후 일정 시간 뒤 풀로 환원되는 비료(양분) 지점.
    /// </summary>
    public struct CompostSpot
    {
        public float X;
        public float Y;
        public int RemainingTicks;
        public int FoodYield;

        public CompostSpot(float x, float y, int remainingTicks, int foodYield)
        {
            X = x;
            Y = y;
            RemainingTicks = remainingTicks;
            FoodYield = foodYield;
        }
    }

    /// <summary>
    /// 인간 사냥꾼의 원거리 투사체(활/창) 시각 궤적.
    /// </summary>
    public struct HunterShot
    {
        public float X1;
        public float Y1;
        public float X2;
        public float Y2;
        public int RemainingTicks;

        public HunterShot(float x1, float y1, float x2, float y2, int remainingTicks)
        {
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
            RemainingTicks = remainingTicks;
        }
    }

    /// <summary>
    /// 맵 지형 장애물 (바위 또는 연못).
    /// </summary>
    public struct TerrainObstacle
    {
        public float X;
        public float Y;
        public float Radius;
        public bool IsWater;

        public TerrainObstacle(float x, float y, float radius, bool isWater)
        {
            X = x;
            Y = y;
            Radius = radius;
            IsWater = isWater;
        }
    }

    /// <summary>
    /// 인간 사냥꾼이 설치하는 거점 모닥불 (주변 인간 회복 / 곰 습격 대상).
    /// </summary>
    public struct CampfireSite
    {
        public int GroupId;
        public float X;
        public float Y;
        public float Radius;
        public float Hp;
        public float MaxHp;
        public int RemainingTicks;
        public int MaxTicks;

        public CampfireSite(int groupId, float x, float y, float radius, float maxHp, int remainingTicks)
        {
            GroupId = groupId;
            X = x;
            Y = y;
            Radius = radius;
            Hp = maxHp;
            MaxHp = maxHp;
            RemainingTicks = remainingTicks;
            MaxTicks = remainingTicks;
        }
    }

    /// <summary>
    /// 생태계 명예의 전당(역대 최고 기록 및 현재 생존 전설 개체) 집계 데이터.
    /// </summary>
    public sealed class EcoHallOfFame
    {
        public int OldestId { get; set; }
        public EcoSpecies OldestSpecies { get; set; }
        public int OldestAgeTicks { get; set; }

        public int TopKillerId { get; set; }
        public EcoSpecies TopKillerSpecies { get; set; }
        public int MostKills { get; set; }

        public int TopParentId { get; set; }
        public EcoSpecies TopParentSpecies { get; set; }
        public int MostOffspring { get; set; }

        public int FastestId { get; set; }
        public EcoSpecies FastestSpecies { get; set; }
        public float FastestSpeed { get; set; }

        public int LivingLegendId { get; set; } = -1;

        public void Reset()
        {
            OldestId = 0; OldestAgeTicks = 0;
            TopKillerId = 0; MostKills = 0;
            TopParentId = 0; MostOffspring = 0;
            FastestId = 0; FastestSpeed = 0f;
            LivingLegendId = -1;
        }
    }

    /// <summary>
    /// 생태계 및 내 개체 플레이 업적 항목.
    /// </summary>
    public sealed class EcoAchievement
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool Unlocked { get; set; }
    }
}
