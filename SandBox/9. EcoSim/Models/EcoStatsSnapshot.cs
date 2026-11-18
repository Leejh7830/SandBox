namespace GateHelper.EcoSim.Models
{
    /// <summary>
    /// 실시간 생태계 및 진화 추이 그래프를 위한 스냅샷 데이터.
    /// </summary>
    public struct EcoStatsSnapshot
    {
        public int Tick;
        public int PreyCount;
        public int PredatorCount;
        public int BearCount;
        public int HumanCount;
        public int FoodCount;
        public float AvgPreySpeed;
        public float AvgPredatorSpeed;
        public float AvgPreyVision;
        public float AvgPredatorVision;
        public int MaxGeneration;
    }
}
