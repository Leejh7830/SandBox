using System;

namespace GateHelper.PocketFarm
{
    public enum FarmWeatherType
    {
        Sunny,       // 맑음 (표준)
        Rainy,       // 단비 (밭 작물 성장 +35% 가속)
        GoldenTide,  // 황금 조류 (어획 속도 +30% 가속, 대어 확률)
        Festival     // 무지개 축제 (손님 팁 +50%, 식당 명성 +50%)
    }

    public class FarmWeatherSystem
    {
        public FarmWeatherType CurrentWeather { get; set; } = FarmWeatherType.Sunny;
        public float WeatherTimer { get; set; } = 120.0f; // 잔여 시간 (초)
        public float WeatherDuration { get; set; } = 120.0f;

        public string CurrentWeatherName => GetWeatherName(CurrentWeather);
        public string CurrentWeatherDescription => GetWeatherBuffDescription(CurrentWeather);

        private static readonly Random _rnd = new Random();

        public FarmWeatherSystem()
        {
            CurrentWeather = FarmWeatherType.Sunny;
            WeatherDuration = 120.0f;
            WeatherTimer = WeatherDuration;
        }

        public bool Tick(float dt)
        {
            FarmWeatherType dummyWeather;
            string dummyMsg;
            return Tick(dt, out dummyWeather, out dummyMsg);
        }

        public bool Tick(float dt, out FarmWeatherType newWeather, out string weatherMsg)
        {
            newWeather = CurrentWeather;
            weatherMsg = null;

            WeatherTimer -= dt;
            if (WeatherTimer <= 0f)
            {
                // 반드시 다른 날씨로 전환
                FarmWeatherType prev = CurrentWeather;
                int nextIndex = (((int)prev) + _rnd.Next(1, 4)) % 4;
                CurrentWeather = (FarmWeatherType)nextIndex;

                WeatherDuration = _rnd.Next(90, 150);
                WeatherTimer = WeatherDuration;

                newWeather = CurrentWeather;
                weatherMsg = GetWeatherAnnouncement(CurrentWeather);
                return true;
            }

            return false;
        }

        public void SetWeather(FarmWeatherType weather, float duration = 120f)
        {
            CurrentWeather = weather;
            WeatherDuration = duration;
            WeatherTimer = duration;
        }

        public string GetWeatherName(FarmWeatherType weather)
        {
            switch (weather)
            {
                case FarmWeatherType.Sunny: return "맑음";
                case FarmWeatherType.Rainy: return "단비";
                case FarmWeatherType.GoldenTide: return "황금 조류";
                case FarmWeatherType.Festival: return "무지개 축제";
                default: return "맑음";
            }
        }

        public string GetWeatherBadgeText(FarmWeatherType weather)
        {
            switch (weather)
            {
                case FarmWeatherType.Sunny: return "[맑음]";
                case FarmWeatherType.Rainy: return "[단비]";
                case FarmWeatherType.GoldenTide: return "[황금조류]";
                case FarmWeatherType.Festival: return "[축제]";
                default: return "[맑음]";
            }
        }

        public string GetWeatherBuffDescription(FarmWeatherType weather)
        {
            switch (weather)
            {
                case FarmWeatherType.Sunny: return "평화로운 하루 (표준 생산)";
                case FarmWeatherType.Rainy: return "단비가 내려 작물 자연성장 +35% 가속!";
                case FarmWeatherType.GoldenTide: return "풍어 조류 발생! 낚시 속도 +30% 가속!";
                case FarmWeatherType.Festival: return "마을 축제 개막! 식당 손님 팁 & 명성 +50%!";
                default: return "";
            }
        }

        private string GetWeatherAnnouncement(FarmWeatherType weather)
        {
            switch (weather)
            {
                case FarmWeatherType.Rainy:
                    return "날씨 변화: [단비] 촉촉한 비가 내려 모든 밭 작물 성장 +35% 가속!";
                case FarmWeatherType.GoldenTide:
                    return "날씨 변화: [황금 조류] 연못에 물고기 떼 유입! 어획 속도 +30% 가속!";
                case FarmWeatherType.Festival:
                    return "마을 이벤트: [무지개 축제] 손님들의 팁과 식당 명성 획득 +50% 대폭 증가!";
                default:
                    return "날씨 변화: [맑음] 농장에 온화하고 평화로운 햇살이 비춥니다.";
            }
        }

        // --- 버프 계산 프로퍼티 ---
        public float CropSpeedMultiplier => (CurrentWeather == FarmWeatherType.Rainy) ? 1.35f : 1.0f;
        public float FishingSpeedMultiplier => (CurrentWeather == FarmWeatherType.GoldenTide) ? 1.30f : 1.0f;
        public float TipMultiplier => (CurrentWeather == FarmWeatherType.Festival) ? 1.50f : 1.0f;
        public float FameMultiplier => (CurrentWeather == FarmWeatherType.Festival) ? 1.50f : 1.0f;
    }
}
