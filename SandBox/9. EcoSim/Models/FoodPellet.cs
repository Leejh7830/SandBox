namespace GateHelper.EcoSim.Models
{
    /// <summary>
    /// 초식 개체가 섭취하는 식물성 먹이 단위.
    /// </summary>
    public struct FoodPellet
    {
        public float X;
        public float Y;
        public float EnergyValue;

        public FoodPellet(float x, float y, float energyValue)
        {
            X = x;
            Y = y;
            EnergyValue = energyValue;
        }
    }
}
