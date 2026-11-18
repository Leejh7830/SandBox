using System;

namespace GateHelper.EcoSim.Models
{
    /// <summary>
    /// 개체가 부모로부터 물려받고 돌연변이로 진화하는 유전자(DNA) 구조체.
    /// </summary>
    public struct Genome
    {
        public float Speed { get; set; }
        public float Vision { get; set; }
        public float Size { get; set; }

        public Genome(float speed, float vision, float size)
        {
            Speed = Clamp(speed, 1.0f, 6.2f);
            Vision = Clamp(vision, 28f, 185f);
            Size = Clamp(size, 4.5f, 13.5f);
        }

        public static Genome CreateDefaultPrey(Random rng)
        {
            return new Genome(
                speed: 2.3f + (float)(rng.NextDouble() - 0.5) * 0.6f,
                vision: 76f + (float)(rng.NextDouble() - 0.5) * 18f,
                size: 6.2f + (float)(rng.NextDouble() - 0.5) * 1.2f
            );
        }

        public static Genome CreateDefaultPredator(Random rng)
        {
            return new Genome(
                speed: 2.75f + (float)(rng.NextDouble() - 0.5) * 0.6f,
                vision: 105f + (float)(rng.NextDouble() - 0.5) * 22f,
                size: 8.4f + (float)(rng.NextDouble() - 0.5) * 1.4f
            );
        }

        public static Genome CreateDefaultBear(Random rng)
        {
            return new Genome(
                speed: 2.42f + (float)(rng.NextDouble() - 0.5) * 0.4f,
                vision: 120f + (float)(rng.NextDouble() - 0.5) * 20f,
                size: 11.8f + (float)(rng.NextDouble() - 0.5) * 1.2f
            );
        }

        public static Genome CreateDefaultHuman(Random rng)
        {
            return new Genome(
                speed: 2.45f + (float)(rng.NextDouble() - 0.5) * 0.4f,
                vision: 148f + (float)(rng.NextDouble() - 0.5) * 22f,
                size: 9.0f + (float)(rng.NextDouble() - 0.5) * 1.0f
            );
        }

        /// <summary>
        /// 부모 유전자를 복제하며 확률적으로 돌연변이를 적용합니다.
        /// </summary>
        public Genome Mutate(Random rng, float mutationRate, float mutationScale)
        {
            float s = Speed;
            float v = Vision;
            float z = Size;

            if (rng.NextDouble() < mutationRate)
            {
                s *= 1f + ((float)rng.NextDouble() * 2f - 1f) * mutationScale;
            }
            if (rng.NextDouble() < mutationRate)
            {
                v *= 1f + ((float)rng.NextDouble() * 2f - 1f) * mutationScale;
            }
            if (rng.NextDouble() < mutationRate)
            {
                z *= 1f + ((float)rng.NextDouble() * 2f - 1f) * (mutationScale * 0.8f);
            }

            return new Genome(s, v, z);
        }

        /// <summary>
        /// 빠르고 시야가 넓고 덩치가 클수록 틱당 에너지 소모가 커지는 자연 선택 트레이드오프.
        /// </summary>
        public float ComputeMetabolicCost(EcoSpecies species)
        {
            float baseCost;
            switch (species)
            {
                case EcoSpecies.Predator: baseCost = 0.14f; break;
                case EcoSpecies.Bear: baseCost = 0.13f; break;
                case EcoSpecies.Human: baseCost = 0.15f; break;
                default: baseCost = 0.085f; break;
            }
            float speedCost = Speed * Speed * 0.018f;
            float visionCost = Vision * 0.00055f;
            float sizeCost = Size * 0.008f;
            return baseCost + speedCost + visionCost + sizeCost;
        }

        private static float Clamp(float val, float min, float max)
        {
            if (val < min) return min;
            if (val > max) return max;
            return val;
        }
    }
}
