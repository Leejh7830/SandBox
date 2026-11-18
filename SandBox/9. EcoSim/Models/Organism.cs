namespace GateHelper.EcoSim.Models
{
    /// <summary>
    /// 생태계 내 개별 생명체(초식 또는 육식 개체)의 런타임 상태.
    /// </summary>
    public sealed class Organism
    {
        public int Id { get; set; }
        public EcoSpecies Species { get; set; }
        public int Generation { get; set; }
        public Genome Dna { get; set; }

        public float X { get; set; }
        public float Y { get; set; }
        public float Vx { get; set; }
        public float Vy { get; set; }

        public float Energy { get; set; }
        public float MaxEnergy { get; set; }

        public int AgeTicks { get; set; }
        public int OffspringCount { get; set; }
        public int Kills { get; set; }
        public int MeatCount { get; set; }
        public int WoodCount { get; set; }
        public int StoneCount { get; set; }
        public int CampfireGroupId { get; set; }
        public int HouseId { get; set; }
        public int AttackCooldown { get; set; }
        public int GatherCooldown { get; set; }
        public int EatingTicks { get; set; }
        public int SkillActiveTicks { get; set; }
        public int EscapeTicks { get; set; }
        public float AnchorX { get; set; }
        public float AnchorY { get; set; }
        public bool IsHibernating { get; set; }
        public bool IsPlayer { get; set; }
        public bool IsAlphaBoss { get; set; }
        public bool IsAlive { get; set; } = true;
    }
}
