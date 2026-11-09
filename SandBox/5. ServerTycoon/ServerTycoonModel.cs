using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace GateHelper.ServerTycoon
{
    public class ServerItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string SubTitle { get; set; }
        public string Icon { get; set; }
        public int Level { get; set; }
        public double BaseCost { get; set; }
        public double BaseRps { get; set; }
        public double PowerWatts { get; set; } // 대당 소비 전력 (W)
        public double HeatOutput { get; set; } // 대당 발열량
        public string AccentColorHex { get; set; }

        [JsonIgnore]
        public Color AccentColor => ColorTranslator.FromHtml(AccentColorHex ?? "#00E5FF");

        public double GetNextCost(int buyCount = 1)
        {
            if (buyCount <= 1)
            {
                double pow = Math.Pow(1.18, Level);
                if (double.IsInfinity(pow) || pow > 1e300) return 1e300;
                double cost = Math.Floor(BaseCost * pow);
                return (double.IsInfinity(cost) || cost > 1e300) ? 1e300 : cost;
            }

            double total = 0;
            for (int i = 0; i < buyCount; i++)
            {
                double pow = Math.Pow(1.18, Level + i);
                if (double.IsInfinity(pow) || pow > 1e300) return 1e300;
                double nextOne = Math.Floor(BaseCost * pow);
                if (double.IsInfinity(nextOne) || total + nextOne > 1e300) return 1e300;
                total += nextOne;
            }
            return total;
        }

        public int GetMaxAffordable(double money)
        {
            int count = 0;
            double cost = 0;
            while (count < 1000)
            {
                double pow = Math.Pow(1.18, Level + count);
                if (double.IsInfinity(pow) || pow > 1e300) break;
                double nextOne = Math.Floor(BaseCost * pow);
                if (cost + nextOne > money || double.IsInfinity(cost + nextOne)) break;
                cost += nextOne;
                count++;
            }
            return count;
        }

        public double GetTotalRps(double multiplier = 1.0)
        {
            return Level * BaseRps * multiplier;
        }

        public double GetTotalPowerDemand(double efficiencyMult = 1.0)
        {
            return Level * PowerWatts * efficiencyMult;
        }

        public double GetTotalHeatOutput()
        {
            return Level * HeatOutput;
        }
    }

    public class PowerItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string SubTitle { get; set; }
        public string Icon { get; set; }
        public int Level { get; set; }
        public double BaseCost { get; set; }
        public double SupplyWatts { get; set; }
        public int OfflineBonusMinutes { get; set; }
        public string AccentColorHex { get; set; }

        [JsonIgnore]
        public Color AccentColor => ColorTranslator.FromHtml(AccentColorHex ?? "#FFA726");

        public double GetNextCost(int buyCount = 1)
        {
            if (buyCount <= 1)
            {
                double pow = Math.Pow(1.22, Level);
                if (double.IsInfinity(pow) || pow > 1e300) return 1e300;
                double cost = Math.Floor(BaseCost * pow);
                return (double.IsInfinity(cost) || cost > 1e300) ? 1e300 : cost;
            }
            double total = 0;
            for (int i = 0; i < buyCount; i++)
            {
                double pow = Math.Pow(1.22, Level + i);
                if (double.IsInfinity(pow) || pow > 1e300) return 1e300;
                double nextOne = Math.Floor(BaseCost * pow);
                if (double.IsInfinity(nextOne) || total + nextOne > 1e300) return 1e300;
                total += nextOne;
            }
            return total;
        }

        public int GetMaxAffordable(double money)
        {
            int count = 0;
            double cost = 0;
            while (count < 1000)
            {
                double pow = Math.Pow(1.22, Level + count);
                if (double.IsInfinity(pow) || pow > 1e300) break;
                double nextOne = Math.Floor(BaseCost * pow);
                if (cost + nextOne > money || double.IsInfinity(cost + nextOne)) break;
                cost += nextOne;
                count++;
            }
            return count;
        }

        public double GetTotalSupply()
        {
            return Level * SupplyWatts;
        }
    }

    public class CoolingItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string SubTitle { get; set; }
        public string Icon { get; set; }
        public int Level { get; set; }
        public double BaseCost { get; set; }
        public double CoolingCapacity { get; set; }
        public string AccentColorHex { get; set; }

        [JsonIgnore]
        public Color AccentColor => ColorTranslator.FromHtml(AccentColorHex ?? "#00E5FF");

        public double GetNextCost(int buyCount = 1)
        {
            if (buyCount <= 1)
            {
                double pow = Math.Pow(1.22, Level);
                if (double.IsInfinity(pow) || pow > 1e300) return 1e300;
                double cost = Math.Floor(BaseCost * pow);
                return (double.IsInfinity(cost) || cost > 1e300) ? 1e300 : cost;
            }
            double total = 0;
            for (int i = 0; i < buyCount; i++)
            {
                double pow = Math.Pow(1.22, Level + i);
                if (double.IsInfinity(pow) || pow > 1e300) return 1e300;
                double nextOne = Math.Floor(BaseCost * pow);
                if (double.IsInfinity(nextOne) || total + nextOne > 1e300) return 1e300;
                total += nextOne;
            }
            return total;
        }

        public int GetMaxAffordable(double money)
        {
            int count = 0;
            double cost = 0;
            while (count < 1000)
            {
                double pow = Math.Pow(1.22, Level + count);
                if (double.IsInfinity(pow) || pow > 1e300) break;
                double nextOne = Math.Floor(BaseCost * pow);
                if (cost + nextOne > money || double.IsInfinity(cost + nextOne)) break;
                cost += nextOne;
                count++;
            }
            return count;
        }

        public double GetTotalCooling(double efficiencyMult = 1.0)
        {
            return Level * CoolingCapacity * efficiencyMult;
        }
    }

    public class SkillNode
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public int MaxLevel { get; set; } = 5;
        public int[] LevelCosts { get; set; } = new int[] { 1, 2, 4, 8, 15 };

        public int GetCostForLevel(int currentLevel)
        {
            if (currentLevel >= MaxLevel) return 0;
            if (LevelCosts != null && currentLevel < LevelCosts.Length)
            {
                return LevelCosts[currentLevel];
            }
            return (currentLevel + 1) * 3;
        }

        public string GetLevelDescription(int currentLevel)
        {
            switch (Id)
            {
                case "skill_power_eff":
                    return currentLevel >= MaxLevel 
                        ? "서버 전력 -30% 절감 (MAX)" 
                        : $"서버 전력 -{currentLevel * 6}% ➔ -{(currentLevel + 1) * 6}% 절감";
                case "skill_cooling_boost":
                    return currentLevel >= MaxLevel 
                        ? "냉각 효율 +50% 증가 (MAX)" 
                        : $"냉각 효율 +{currentLevel * 10}% ➔ +{(currentLevel + 1) * 10}% 증가";
                case "skill_backbone":
                    return currentLevel >= MaxLevel 
                        ? "전체 서버 수입 +60% 증가 (MAX)" 
                        : $"전체 서버 수입 +{currentLevel * 12}% ➔ +{(currentLevel + 1) * 12}% 증가";
                case "skill_battery_max":
                    return currentLevel >= MaxLevel 
                        ? "오프라인 상한 +3h 20m (MAX)" 
                        : $"오프라인 상한 +{currentLevel * 40}분 ➔ +{(currentLevel + 1) * 40}분";
                case "skill_crit_packet":
                    return currentLevel >= MaxLevel 
                        ? "보너스 25% · 잭팟 8배 (MAX)" 
                        : $"보너스 {5 + currentLevel * 4}% (x{3 + currentLevel}) ➔ {5 + (currentLevel + 1) * 4}% (x{4 + currentLevel})";
                case "skill_ai_devops":
                    return currentLevel >= MaxLevel 
                        ? "고장 보상 2.5배 & 자동 수리 (MAX)" 
                        : (currentLevel >= 2 ? $"고장 보상 +{(currentLevel + 1) * 30}% & 자동 수리" : $"고장 보상 +{(currentLevel + 1) * 30}% (Lv.3시 AI 봇 자동수리)");
                default:
                    return Description;
            }
        }
    }

    public enum IncidentType
    {
        None,
        DdosAttack,
        Overheat,
        MemoryLeak
    }

    public class TycoonIncident
    {
        public IncidentType Type { get; set; }
        public int TargetServerIndex { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public double RewardCash { get; set; }
        public float RemainingSeconds { get; set; }

        public static TycoonIncident GenerateRandom(int maxUnlockedServerIndex, Random rand, double currentRps)
        {
            int target = rand.Next(0, Math.Max(1, maxUnlockedServerIndex + 1));
            int roll = rand.Next(0, 3);
            double baseReward = Math.Max(30.0, currentRps * 12.0);

            switch (roll)
            {
                case 0:
                    return new TycoonIncident
                    {
                        Type = IncidentType.DdosAttack,
                        TargetServerIndex = target,
                        Title = "⚡ 대량 접속 트래픽 폭주!",
                        Message = "방문자가 폭주합니다! 긴급 분산 처리 클릭!",
                        RewardCash = Math.Round(baseReward * 1.5, 0),
                        RemainingSeconds = 12f
                    };
                case 1:
                    return new TycoonIncident
                    {
                        Type = IncidentType.Overheat,
                        TargetServerIndex = target,
                        Title = "▲ 서버 과열 경보!",
                        Message = "서버 온도가 너무 높습니다! 냉각 쿨러 클릭!",
                        RewardCash = Math.Round(baseReward * 1.2, 0),
                        RemainingSeconds = 10f
                    };
                default:
                    return new TycoonIncident
                    {
                        Type = IncidentType.MemoryLeak,
                        TargetServerIndex = target,
                        Title = "◈ 시스템 메모리 부족!",
                        Message = "메모리가 꽉 찼습니다! 메모리 정리 클릭!",
                        RewardCash = Math.Round(baseReward * 1.0, 0),
                        RemainingSeconds = 14f
                    };
            }
        }
    }

    public class FloatingText
    {
        public string Text;
        public float X;
        public float Y;
        public float VelocityY;
        public float Alpha;
        public Color TextColor;
        public bool IsCrit;
    }

    public class PacketParticle
    {
        public float X;
        public float Y;
        public float StartX;
        public float StartY;
        public float TargetX;
        public float TargetY;
        public float Progress;
        public float Speed;
        public Color ParticleColor;
        public int ServerIndex;
    }

    public class Shockwave
    {
        public float X;
        public float Y;
        public float Radius;
        public float MaxRadius;
        public float Alpha;
        public Color WaveColor;
    }

    #region 1. S/W Studio Models
    public class SoftwareCategoryDef
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string SubTitle { get; set; }
        public string Icon { get; set; }
        public double UnlockCost { get; set; }
        public double RequiredPoints { get; set; }
        public double BaseRoyaltyPerSec { get; set; }
        public int OptimalUx { get; set; }
        public int OptimalAlgo { get; set; }
        public int OptimalSec { get; set; }
        public string AccentColorHex { get; set; }
    }

    public class ActiveSoftwareProject
    {
        public int CategoryId { get; set; }
        public string Title { get; set; }
        public double TargetPoints { get; set; }
        public double CurrentPoints { get; set; }
        public int FocusUx { get; set; } = 40;
        public int FocusAlgo { get; set; } = 40;
        public int FocusSec { get; set; } = 20;

        [JsonIgnore]
        public bool IsCompleted => CurrentPoints >= TargetPoints;

        [JsonIgnore]
        public double ProgressPercent => TargetPoints > 0 ? Math.Min(100.0, (CurrentPoints / TargetPoints) * 100.0) : 0;
    }

    public class ReleasedSoftware
    {
        public string Title { get; set; }
        public string CategoryName { get; set; }
        public double Rating { get; set; } // 1.0 ~ 10.0
        public string ReviewSummary { get; set; }
        public double RoyaltyPerSec { get; set; }
        public int RemainingSecs { get; set; }
        public double TotalEarned { get; set; }
    }

    public class SoftwareStudioState
    {
        public ActiveSoftwareProject CurrentProject { get; set; }
        public List<ReleasedSoftware> ReleasedProducts { get; set; } = new List<ReleasedSoftware>();
        public int SelectedCategoryIndex { get; set; } = 0;
        public int TotalSoftwareShipped { get; set; } = 0;

        public static List<SoftwareCategoryDef> GetCategories()
        {
            return new List<SoftwareCategoryDef>
            {
                new SoftwareCategoryDef
                {
                    Id = 0,
                    Name = "인디 모바일 게임",
                    SubTitle = "누구나 즐기는 모바일 게임, 빠른 제작과 초기 흥행",
                    Icon = "sw_game",
                    UnlockCost = 2500.0,
                    RequiredPoints = 4000.0,
                    BaseRoyaltyPerSec = 22.0,
                    OptimalUx = 55, OptimalAlgo = 25, OptimalSec = 20,
                    AccentColorHex = "#FF4081"
                },
                new SoftwareCategoryDef
                {
                    Id = 1,
                    Name = "회사 업무 관리 프로그램",
                    SubTitle = "기업에 판매하는 회계 및 재고 관리 시스템",
                    Icon = "sw_erp",
                    UnlockCost = 35000.0,
                    RequiredPoints = 35000.0,
                    BaseRoyaltyPerSec = 260.0,
                    OptimalUx = 20, OptimalAlgo = 45, OptimalSec = 35,
                    AccentColorHex = "#448AFF"
                },
                new SoftwareCategoryDef
                {
                    Id = 2,
                    Name = "대화형 인공지능(AI)",
                    SubTitle = "전 세계 사용자가 구독하는 똑똑한 AI 비서",
                    Icon = "sw_ai",
                    UnlockCost = 350000.0,
                    RequiredPoints = 250000.0,
                    BaseRoyaltyPerSec = 2500.0,
                    OptimalUx = 25, OptimalAlgo = 60, OptimalSec = 15,
                    AccentColorHex = "#B388FF"
                },
                new SoftwareCategoryDef
                {
                    Id = 3,
                    Name = "통합 보안 프로그램",
                    SubTitle = "바이러스와 해킹을 원천 차단하는 백신",
                    Icon = "sw_sec",
                    UnlockCost = 2800000.0,
                    RequiredPoints = 1800000.0,
                    BaseRoyaltyPerSec = 16000.0,
                    OptimalUx = 15, OptimalAlgo = 35, OptimalSec = 50,
                    AccentColorHex = "#00E676"
                },
                new SoftwareCategoryDef
                {
                    Id = 4,
                    Name = "글로벌 클라우드 플랫폼",
                    SubTitle = "전 세계 서버망을 하나로 묶는 대형 플랫폼",
                    Icon = "sw_os",
                    UnlockCost = 35000000.0,
                    RequiredPoints = 18000000.0,
                    BaseRoyaltyPerSec = 150000.0,
                    OptimalUx = 25, OptimalAlgo = 45, OptimalSec = 30,
                    AccentColorHex = "#FFD700"
                }
            };
        }
    }
    #endregion

    #region 2. B2B Contracts Models
    public enum SlaRequirementType
    {
        PowerUptime = 0,    // 전력 부족 정전 금지
        ThermalStrict = 1,  // 온도 45°C 이하 저지연 유지
        HighBandwidth = 2   // 고대역폭 안정 유지 (RPS 100% 가동률)
    }

    public class B2BContract
    {
        public string Id { get; set; }
        public string ClientName { get; set; }
        public string Title { get; set; }
        public string TierBadge { get; set; } // D, C, B, A, S
        public double RequiredRps { get; set; }
        public int DurationSecs { get; set; }
        public double RewardCash { get; set; }
        public double PenaltyCash { get; set; }
        public SlaRequirementType SlaType { get; set; }
        public string SlaRuleDescription { get; set; }
        public int RemainingSecs { get; set; }
        public int ViolationsCount { get; set; }
        public string ClientColorHex { get; set; }

        public static B2BContract GenerateRandom(string repTier, Random rand, double currentRps)
        {
            string[] clients = { "모바일 스타트업", "온라인 쇼핑 결제원", "인기 게임 방송국", "글로벌 AI 연구소", "국가 우주 관제 센터" };
            string[] titles = { "스타트업 모바일 앱 서버 운영", "온라인 쇼핑몰 대규모 결제망 지원", "인기 게임 생방송 실시간 스트리밍", "초거대 인공지능 연구 연산 지원", "우주 관제 위성 통신망 호스팅" };
            SlaRequirementType[] slaTypes = { SlaRequirementType.PowerUptime, SlaRequirementType.ThermalStrict, SlaRequirementType.HighBandwidth };
            string[] slaDescs = { "정전 금지: 전력 공급 100% 유지", "과열 금지: 45°C 이하 냉각 유지", "성능 유지: 서버 풀가동 납기 준수" };
            string[] colors = { "#00E5FF", "#76FF03", "#FFD54F", "#FF4081", "#E040FB" };

            int tierIdx = 0;
            switch (repTier)
            {
                case "C": tierIdx = rand.Next(0, 2); break;
                case "B": tierIdx = rand.Next(1, 3); break;
                case "A": tierIdx = rand.Next(2, 4); break;
                case "S": tierIdx = rand.Next(3, 5); break;
                default: tierIdx = 0; break;
            }

            int slaIdx = rand.Next(0, 3);
            double baseReqRps = Math.Max(25.0, Math.Pow(6.8, tierIdx + 1) * 5.0);
            double reward = Math.Round(baseReqRps * 130.0 * (1.0 + rand.NextDouble() * 0.4), 0);
            double penalty = Math.Round(reward * 0.35, 0);

            return new B2BContract
            {
                Id = Guid.NewGuid().ToString().Substring(0, 8),
                ClientName = clients[tierIdx],
                Title = titles[tierIdx],
                TierBadge = (tierIdx == 0 ? "D" : (tierIdx == 1 ? "C" : (tierIdx == 2 ? "B" : (tierIdx == 3 ? "A" : "S")))),
                RequiredRps = Math.Round(baseReqRps, 0),
                DurationSecs = 35 + (tierIdx * 8),
                RewardCash = reward,
                PenaltyCash = penalty,
                SlaType = slaTypes[slaIdx],
                SlaRuleDescription = slaDescs[slaIdx],
                ClientColorHex = colors[tierIdx],
                RemainingSecs = 35 + (tierIdx * 8),
                ViolationsCount = 0
            };
        }
    }

    public class B2BMarketState
    {
        public string ReputationTier { get; set; } = "D"; // D, C, B, A, S
        public int ReputationScore { get; set; } = 20;
        public int SuccessfulContractsCount { get; set; } = 0;
        public B2BContract ActiveContract { get; set; }
        public List<B2BContract> AvailableOffers { get; set; } = new List<B2BContract>();
        public int RefreshCooldownSecs { get; set; } = 30;

        public void RefreshOffers(Random rand, double currentRps)
        {
            if (AvailableOffers == null) AvailableOffers = new List<B2BContract>();
            AvailableOffers.Clear();
            for (int i = 0; i < 3; i++)
            {
                AvailableOffers.Add(B2BContract.GenerateRandom(ReputationTier, rand, currentRps));
            }
            RefreshCooldownSecs = 35;
        }

        public void AddReputation(int delta)
        {
            ReputationScore = Math.Max(0, Math.Min(1000, ReputationScore + delta));
            if (ReputationScore >= 450) ReputationTier = "S";
            else if (ReputationScore >= 250) ReputationTier = "A";
            else if (ReputationScore >= 120) ReputationTier = "B";
            else if (ReputationScore >= 50) ReputationTier = "C";
            else ReputationTier = "D";
        }
    }
    #endregion

    #region 3. Crypto Mine & Trading Models
    public class CryptoMarketState
    {
        public string CoinSymbol { get; set; } = "BYT";
        public string CoinName { get; set; } = "ByteCoin";
        public double CurrentPrice { get; set; } = 120.0;
        public double TargetPrice { get; set; } = 120.0;
        public double PriceChangePercent { get; set; } = 0.0;
        public List<double> PriceHistory { get; set; } = new List<double>();
        public int MiningHashPercent { get; set; } = 0; // 0, 25, 50, 75, 100
        public double MinedCoins { get; set; } = 0.0;
        public double TotalMinedCoins { get; set; } = 0.0;
        public double TotalInvestedUsd { get; set; } = 0.0; // 코인 매수에 투입된 총 투자 원금 ($)
        public double TotalRealizedProfit { get; set; } = 0.0; // 매도를 통해 확정된 누적 실현 손익 ($)
        public string MarketNews { get; set; } = "글로벌 핀테크, 차세대 분산 원장 노드 인프라 구축 추진 발표.";
        public int NewsCooldownSecs { get; set; } = 18;

        // 매수 평균 단가 (1개당 평균 얼마에 샀는지)
        [JsonIgnore]
        public double AverageBuyPrice => MinedCoins > 0.000001 ? Math.Max(0, TotalInvestedUsd / MinedCoins) : 0.0;

        // 현재 총 평가 금액 (보유 코인 * 현재 시세)
        [JsonIgnore]
        public double TotalValuation => MinedCoins * CurrentPrice;

        // 현재 평가 손익 (총 평가 금액 - 투자 원금)
        [JsonIgnore]
        public double UnrealizedProfit => TotalValuation - TotalInvestedUsd;

        // 투자 수익률 (%)
        [JsonIgnore]
        public double ProfitRatePercent
        {
            get
            {
                if (TotalInvestedUsd > 0.001)
                {
                    return ((TotalValuation - TotalInvestedUsd) / TotalInvestedUsd) * 100.0;
                }
                // 원금 없이 채굴로만 획득한 경우 코인이 있으면 100% 순수익
                return MinedCoins > 0.000001 ? 100.0 : 0.0;
            }
        }

        public void InitHistory()
        {
            if (PriceHistory == null || PriceHistory.Count == 0)
            {
                PriceHistory = new List<double>();
                double p = CurrentPrice > 0 ? CurrentPrice : 120.0;
                for (int i = 0; i < 35; i++) PriceHistory.Add(p);
            }
        }
    }
    #endregion

    public class TycoonGameState
    {
        public double Money { get; set; } = 50.0;
        public double CurrentRunEarned { get; set; } = 50.0; // 현재 회차 누적 자금 (환생 조건 계산용)
        public double TotalEarned { get; set; } = 50.0;     // 역대 총 누적 자금
        public long TotalClicks { get; set; } = 0;
        public int PrestigeCount { get; set; } = 0;
        public int ResearchTokens { get; set; } = 0;
        public int TotalTokensEarned { get; set; } = 0;
        public List<string> UnlockedSkills { get; set; } = new List<string>();
        public Dictionary<string, int> SkillLevels { get; set; } = new Dictionary<string, int>();
        public DateTime LastSaveTimeUtc { get; set; } = DateTime.UtcNow;

        // 신규 3대 게임 시스템 상태
        public SoftwareStudioState SoftwareStudio { get; set; } = new SoftwareStudioState();
        public B2BMarketState B2BMarket { get; set; } = new B2BMarketState();
        public CryptoMarketState CryptoMarket { get; set; } = new CryptoMarketState();

        public int GetSkillLevel(string skillId)
        {
            if (SkillLevels != null && SkillLevels.TryGetValue(skillId, out int lvl))
            {
                return lvl;
            }
            if (UnlockedSkills != null && UnlockedSkills.Contains(skillId))
            {
                return 1;
            }
            return 0;
        }

        public bool HasSkill(string skillId)
        {
            return GetSkillLevel(skillId) > 0;
        }

        public void SetSkillLevel(string skillId, int level)
        {
            if (SkillLevels == null) SkillLevels = new Dictionary<string, int>();
            SkillLevels[skillId] = level;
            if (UnlockedSkills == null) UnlockedSkills = new List<string>();
            if (!UnlockedSkills.Contains(skillId)) UnlockedSkills.Add(skillId);
        }

        // 3대 인프라 자산 리스트
        public List<ServerItem> Servers { get; set; } = new List<ServerItem>();
        public List<PowerItem> PowerFacilities { get; set; } = new List<PowerItem>();
        public List<CoolingItem> CoolingFacilities { get; set; } = new List<CoolingItem>();

        // 런타임 전용 상태 (저장 제외)
        [JsonIgnore] public float FeverMeter = 0f;
        [JsonIgnore] public bool IsFeverActive = false;
        [JsonIgnore] public float FeverRemainingSeconds = 0f;
        [JsonIgnore] public TycoonIncident CurrentIncident = null;

        public static List<ServerItem> CreateDefaultServers()
        {
            return new List<ServerItem>
            {
                new ServerItem
                {
                    Id = "rpi",
                    Name = "초소형 미니 PC",
                    SubTitle = "작고 알뜰한 입문용 컴퓨터",
                    Icon = "🍓",
                    Level = 1,
                    BaseCost = 15.0,
                    BaseRps = 1.0,
                    PowerWatts = 15.0,
                    HeatOutput = 0.4,
                    AccentColorHex = "#FF5376"
                },
                new ServerItem
                {
                    Id = "tower",
                    Name = "사무용 컴퓨터",
                    SubTitle = "흔히 볼 수 있는 기본 데스크탑",
                    Icon = "🖥️",
                    Level = 0,
                    BaseCost = 320.0,
                    BaseRps = 7.5,
                    PowerWatts = 90.0,
                    HeatOutput = 2.2,
                    AccentColorHex = "#FFA726"
                },
                new ServerItem
                {
                    Id = "blade1u",
                    Name = "표준 랙 서버",
                    SubTitle = "데이터센터에 납품되는 슬림형 서버",
                    Icon = "🗄️",
                    Level = 0,
                    BaseCost = 6800.0,
                    BaseRps = 60.0,
                    PowerWatts = 450.0,
                    HeatOutput = 9.0,
                    AccentColorHex = "#00E676"
                },
                new ServerItem
                {
                    Id = "xeon4u",
                    Name = "고성능 워크스테이션",
                    SubTitle = "전문가용 강력한 대형 컴퓨터",
                    Icon = "⚡",
                    Level = 0,
                    BaseCost = 125000.0,
                    BaseRps = 500.0,
                    PowerWatts = 2000.0,
                    HeatOutput = 24.0,
                    AccentColorHex = "#00E5FF"
                },
                new ServerItem
                {
                    Id = "gpu_h100",
                    Name = "AI 슈퍼컴퓨터",
                    SubTitle = "인공지능을 초고속으로 돌리는 최신 장비",
                    Icon = "🔮",
                    Level = 0,
                    BaseCost = 3500000.0,
                    BaseRps = 5500.0,
                    PowerWatts = 11000.0,
                    HeatOutput = 70.0,
                    AccentColorHex = "#B388FF"
                },
                new ServerItem
                {
                    Id = "quantum",
                    Name = "미래형 양자컴퓨터",
                    SubTitle = "압도적인 처리 속도를 지닌 꿈의 컴퓨터",
                    Icon = "⚛️",
                    Level = 0,
                    BaseCost = 150000000.0,
                    BaseRps = 70000.0,
                    PowerWatts = 60000.0,
                    HeatOutput = 180.0,
                    AccentColorHex = "#69F0AE"
                }
            };
        }

        public static List<PowerItem> CreateDefaultPower()
        {
            return new List<PowerItem>
            {
                new PowerItem
                {
                    Id = "outlet",
                    Name = "기본 벽면 콘센트",
                    SubTitle = "방에서 흔히 쓰는 일반 220V 전원",
                    Icon = "🔌",
                    Level = 1, // 기본 3kW 제공
                    BaseCost = 0,
                    SupplyWatts = 3000.0,
                    OfflineBonusMinutes = 0,
                    AccentColorHex = "#FFA726"
                },
                new PowerItem
                {
                    Id = "multitab",
                    Name = "안전 멀티탭",
                    SubTitle = "여러 기기를 꽂아 쓰는 확장 전원",
                    Icon = "🔋",
                    Level = 0,
                    BaseCost = 150.0,
                    SupplyWatts = 1500.0,
                    OfflineBonusMinutes = 10,
                    AccentColorHex = "#FFB74D"
                },
                new PowerItem
                {
                    Id = "ups_mini",
                    Name = "비상 예비 배터리(UPS)",
                    SubTitle = "갑작스러운 정전을 막아주는 배터리",
                    Icon = "⚡",
                    Level = 0,
                    BaseCost = 3500.0,
                    SupplyWatts = 5000.0,
                    OfflineBonusMinutes = 20, // 오프라인 상한 +20분
                    AccentColorHex = "#4DD0E1"
                },
                new PowerItem
                {
                    Id = "high_voltage",
                    Name = "산업용 고압 전력",
                    SubTitle = "대형 빌딩 수준의 강력한 전기 공급",
                    Icon = "🏭",
                    Level = 0,
                    BaseCost = 75000.0,
                    SupplyWatts = 35000.0,
                    OfflineBonusMinutes = 40, // 오프라인 상한 +40분
                    AccentColorHex = "#00E5FF"
                },
                new PowerItem
                {
                    Id = "solar_array",
                    Name = "옥상 태양광 발전기",
                    SubTitle = "친환경 태양광 자체 발전",
                    Icon = "☀️",
                    Level = 0,
                    BaseCost = 2000000.0,
                    SupplyWatts = 180000.0,
                    OfflineBonusMinutes = 60,
                    AccentColorHex = "#FFD54F"
                },
                new PowerItem
                {
                    Id = "smr_reactor",
                    Name = "소형 원자력 발전기",
                    SubTitle = "초대용량 무제한 원자력 발전",
                    Icon = "☢️",
                    Level = 0,
                    BaseCost = 80000000.0,
                    SupplyWatts = 2000000.0,
                    OfflineBonusMinutes = 90, // 오프라인 상한 +90분
                    AccentColorHex = "#76FF03"
                }
            };
        }

        public static List<CoolingItem> CreateDefaultCooling()
        {
            return new List<CoolingItem>
            {
                new CoolingItem
                {
                    Id = "passive",
                    Name = "자연 바람 통풍",
                    SubTitle = "자연 통풍으로 열기 방출",
                    Icon = "🍃",
                    Level = 1,
                    BaseCost = 0,
                    CoolingCapacity = 15.0,
                    AccentColorHex = "#81C784"
                },
                new CoolingItem
                {
                    Id = "desk_fan",
                    Name = "탁상용 선풍기",
                    SubTitle = "선풍기 바람으로 빠른 냉각",
                    Icon = "💨",
                    Level = 0,
                    BaseCost = 120.0,
                    CoolingCapacity = 20.0,
                    AccentColorHex = "#4FC3F7"
                },
                new CoolingItem
                {
                    Id = "crac_unit",
                    Name = "대형 서버실 에어컨",
                    SubTitle = "서버실 온도를 항상 일정하게 유지",
                    Icon = "❄️",
                    Level = 0,
                    BaseCost = 4200.0,
                    CoolingCapacity = 95.0,
                    AccentColorHex = "#00E5FF"
                },
                new CoolingItem
                {
                    Id = "water_loop",
                    Name = "수냉식 쿨러 시스템",
                    SubTitle = "차가운 냉각수를 순환시켜 열을 흡수",
                    Icon = "💧",
                    Level = 0,
                    BaseCost = 90000.0,
                    CoolingCapacity = 680.0,
                    AccentColorHex = "#2979FF"
                },
                new CoolingItem
                {
                    Id = "immersion",
                    Name = "특수 액체 냉각 탱크",
                    SubTitle = "특수 액체에 기기를 담가 통째로 식힘",
                    Icon = "🧊",
                    Level = 0,
                    BaseCost = 2400000.0,
                    CoolingCapacity = 6000.0,
                    AccentColorHex = "#B388FF"
                },
                new CoolingItem
                {
                    Id = "quantum_cryo",
                    Name = "우주급 초극저온 냉동기",
                    SubTitle = "영하 273도 극저온 급속 냉각",
                    Icon = "🌌",
                    Level = 0,
                    BaseCost = 90000000.0,
                    CoolingCapacity = 55000.0,
                    AccentColorHex = "#69F0AE"
                }
            };
        }

        public static List<SkillNode> GetSkillDefinitions()
        {
            return new List<SkillNode>
            {
                new SkillNode
                {
                    Id = "skill_power_eff",
                    Name = "절전 기술 연구",
                    Description = "서버 소비 전력 절감 (Lv당 -6%, 최대 -30%)",
                    Icon = "⚡",
                    MaxLevel = 5,
                    LevelCosts = new int[] { 1, 2, 4, 8, 15 }
                },
                new SkillNode
                {
                    Id = "skill_cooling_boost",
                    Name = "고효율 쿨링 연구",
                    Description = "냉각 장치의 냉각 성능 대폭 강화 (Lv당 +10%, 최대 +50%)",
                    Icon = "❄",
                    MaxLevel = 5,
                    LevelCosts = new int[] { 1, 2, 4, 8, 15 }
                },
                new SkillNode
                {
                    Id = "skill_backbone",
                    Name = "초고속 인터넷망",
                    Description = "인터넷 속도를 올려 모든 서버 수입 증폭 (Lv당 +12%, 최대 +60%)",
                    Icon = "🌐",
                    MaxLevel = 5,
                    LevelCosts = new int[] { 2, 4, 8, 15, 25 }
                },
                new SkillNode
                {
                    Id = "skill_battery_max",
                    Name = "대용량 예비 배터리",
                    Description = "게임을 꺼두었을 때 모이는 방치 시간 증가 (Lv당 +40분, 최대 +3.3시간)",
                    Icon = "🔋",
                    MaxLevel = 5,
                    LevelCosts = new int[] { 2, 4, 8, 15, 25 }
                },
                new SkillNode
                {
                    Id = "skill_crit_packet",
                    Name = "골든 트래픽 보너스",
                    Description = "클릭 시 대박 보너스 확률 및 배율 강화 (최대 25% 확률 8배)",
                    Icon = "★",
                    MaxLevel = 5,
                    LevelCosts = new int[] { 3, 6, 12, 22, 35 }
                },
                new SkillNode
                {
                    Id = "skill_ai_devops",
                    Name = "AI 자동 수리 봇",
                    Description = "고장 보상 증폭(Lv당 +30%) 및 Lv.3 이상 시 자동으로 고장 수리",
                    Icon = "🤖",
                    MaxLevel = 5,
                    LevelCosts = new int[] { 3, 6, 12, 22, 35 }
                }
            };
        }

        #region Infrastructure & Physics Calculations

        public double GetTotalPowerSupply()
        {
            double sum = 0;
            if (PowerFacilities != null)
            {
                foreach (var p in PowerFacilities) sum += p.GetTotalSupply();
            }
            return Math.Max(3000.0, sum);
        }

        public double GetTotalPowerDemand()
        {
            int lvl = GetSkillLevel("skill_power_eff");
            double eff = Math.Max(0.70, 1.0 - (lvl * 0.06));
            double sum = 0;
            if (Servers != null)
            {
                foreach (var s in Servers) sum += s.GetTotalPowerDemand(eff);
            }
            return sum;
        }

        public double GetPowerEfficiencyRatio()
        {
            double supply = GetTotalPowerSupply();
            double demand = GetTotalPowerDemand();
            if (demand <= supply) return 1.0;
            if (demand <= 0) return 1.0;
            // 전력 부족 시 공급 비율만큼만 가동 (최소 20% 보장)
            return Math.Max(0.20, supply / demand);
        }

        public double GetTotalCoolingCapacity()
        {
            int lvl = GetSkillLevel("skill_cooling_boost");
            double eff = 1.0 + (lvl * 0.10);
            double sum = 0;
            if (CoolingFacilities != null)
            {
                foreach (var c in CoolingFacilities) sum += c.GetTotalCooling(eff);
            }
            return Math.Max(15.0, sum);
        }

        public double GetTotalHeatGenerated()
        {
            double sum = 0;
            if (Servers != null)
            {
                foreach (var s in Servers) sum += s.GetTotalHeatOutput();
            }
            return sum;
        }

        public double GetCurrentTemperature()
        {
            double heat = GetTotalHeatGenerated();
            double cooling = GetTotalCoolingCapacity();
            double diff = heat - cooling;

            // 쿨링이 남으면 실온 22°C까지 냉각, 쿨링이 부족하면 95°C까지 과열
            if (diff <= 0)
            {
                return Math.Max(22.0, 28.0 + (diff * 0.05));
            }
            else
            {
                return Math.Min(95.0, 28.0 + (diff * 0.25));
            }
        }

        public double GetTemperatureMultiplier()
        {
            double temp = GetCurrentTemperature();
            if (temp <= 35.0)
            {
                return 1.20; // 쾌적 쿨링 오버클럭 보너스 +20%!
            }
            else if (temp <= 60.0)
            {
                return 1.0; // 정상 온도 100%
            }
            else
            {
                return 0.65; // 고온 스로틀링 감속 페널티 -35%
            }
        }

        public double GetOfflineCapSeconds()
        {
            // 기본 2시간 (7,200초)
            int extraMinutes = 0;
            if (PowerFacilities != null)
            {
                foreach (var p in PowerFacilities)
                {
                    extraMinutes += p.Level * p.OfflineBonusMinutes;
                }
            }
            int batLvl = GetSkillLevel("skill_battery_max");
            extraMinutes += batLvl * 40;

            // 최대 8시간(28,800초)까지만 확장 가능 (인플레이션 방어선)
            double totalSecs = (120 + extraMinutes) * 60.0;
            return Math.Min(28800.0, totalSecs);
        }

        public double GetPrestigeMultiplier()
        {
            // 각 환생당 +15% 기본 생산 배율, 획득한 총 토큰당 +1.5% 미세 패시브 보너스
            return 1.0 + (PrestigeCount * 0.15) + (TotalTokensEarned * 0.015);
        }

        public double GetFeverMultiplier()
        {
            return IsFeverActive ? 2.5 : 1.0;
        }

        public double CalculateTotalRps()
        {
            double sum = 0;
            double mult = GetPrestigeMultiplier() * GetFeverMultiplier();
            int backboneLvl = GetSkillLevel("skill_backbone");
            if (backboneLvl > 0) mult *= (1.0 + backboneLvl * 0.12);

            // 전력 효율과 온도 보너스/페널티 동시 적용
            mult *= GetPowerEfficiencyRatio();
            mult *= GetTemperatureMultiplier();

            foreach (var s in Servers)
            {
                sum += s.GetTotalRps(mult);
            }
            return sum;
        }

        public double GetSoftwareRoyaltyRps()
        {
            if (SoftwareStudio?.ReleasedProducts == null) return 0;
            double sum = 0;
            foreach (var p in SoftwareStudio.ReleasedProducts)
            {
                if (p.RemainingSecs > 0) sum += p.RoyaltyPerSec;
            }
            return sum;
        }

        public double GetEffectiveCashRps()
        {
            double serverRps = CalculateTotalRps();
            double hashShare = (CryptoMarket != null) ? (CryptoMarket.MiningHashPercent / 100.0) : 0.0;
            double cashFromServers = serverRps * (1.0 - hashShare);
            return cashFromServers + GetSoftwareRoyaltyRps();
        }

        public double GetClickPower()
        {
            double rpsBonus = CalculateTotalRps() * 0.04;
            double mult = GetPrestigeMultiplier() * (IsFeverActive ? 3.0 : 1.0);
            return Math.Max(1.0, (1.0 + rpsBonus) * mult);
        }

        public int GetTotalServerCount()
        {
            int total = 0;
            if (Servers != null)
            {
                foreach (var s in Servers) total += s.Level;
            }
            return total;
        }

        public int CalculatePrestigeTokens()
        {
            // 이번 회차에서 최소 $1,000,000 ($1M) 이상 누적되어야 첫 토큰 획득 가능
            if (CurrentRunEarned < 1000000.0) return 0;

            // 지수 감쇄 곡선 (Power 0.38): $1M -> 1개, $8M -> 2개, $30M -> 3개, $80M -> 5개, $250M -> 8개, $1B -> 14개
            int tokens = (int)Math.Floor(Math.Pow(CurrentRunEarned / 1000000.0, 0.38));
            return Math.Max(0, tokens);
        }

        public int CalculateClaimableTokens()
        {
            return CalculatePrestigeTokens();
        }

        #endregion

        #region Save / Load & Offline Progress

        private static string GetSaveFilePath()
        {
            try
            {
                string dir = Path.Combine(Application.StartupPath, "_meta", "sandbox", "serverfarmtycoon");
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                return Path.Combine(dir, "server_tycoon.json");
            }
            catch
            {
                string fallbackDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_meta", "sandbox", "serverfarmtycoon");
                if (!Directory.Exists(fallbackDir))
                {
                    Directory.CreateDirectory(fallbackDir);
                }
                return Path.Combine(fallbackDir, "server_tycoon.json");
            }
        }

        public static TycoonGameState LoadOrCreate()
        {
            string path = GetSaveFilePath();
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    var state = JsonConvert.DeserializeObject<TycoonGameState>(json);
                    if (state != null)
                    {
                        state.EnsureDefaults();
                        return state;
                    }
                }
                catch { }
            }

            var fresh = new TycoonGameState();
            fresh.EnsureDefaults();
            fresh.Save();
            return fresh;
        }

        public void EnsureDefaults()
        {
            var defServers = CreateDefaultServers();
            if (Servers == null || Servers.Count == 0)
            {
                Servers = defServers;
            }
            else
            {
                foreach (var def in defServers)
                {
                    var existing = Servers.Find(s => s.Id == def.Id);
                    if (existing == null)
                    {
                        Servers.Add(def);
                    }
                    else
                    {
                        // 기존 세이브 파일에 누락된 전력, 발열, 최신 비용/RPS 등 강제 동기화 (보유 레벨은 보존)
                        existing.Name = def.Name;
                        existing.SubTitle = def.SubTitle;
                        existing.Icon = def.Icon;
                        existing.BaseCost = def.BaseCost;
                        existing.BaseRps = def.BaseRps;
                        existing.PowerWatts = def.PowerWatts;
                        existing.HeatOutput = def.HeatOutput;
                        existing.AccentColorHex = def.AccentColorHex;
                    }
                }
            }

            var defPower = CreateDefaultPower();
            if (PowerFacilities == null || PowerFacilities.Count == 0)
            {
                PowerFacilities = defPower;
            }
            else
            {
                foreach (var def in defPower)
                {
                    var existing = PowerFacilities.Find(p => p.Id == def.Id);
                    if (existing == null)
                    {
                        PowerFacilities.Add(def);
                    }
                    else
                    {
                        existing.Name = def.Name;
                        existing.SubTitle = def.SubTitle;
                        existing.Icon = def.Icon;
                        existing.BaseCost = def.BaseCost;
                        existing.SupplyWatts = def.SupplyWatts;
                        existing.OfflineBonusMinutes = def.OfflineBonusMinutes;
                        existing.AccentColorHex = def.AccentColorHex;
                    }
                }
            }

            var defCooling = CreateDefaultCooling();
            if (CoolingFacilities == null || CoolingFacilities.Count == 0)
            {
                CoolingFacilities = defCooling;
            }
            else
            {
                foreach (var def in defCooling)
                {
                    var existing = CoolingFacilities.Find(c => c.Id == def.Id);
                    if (existing == null)
                    {
                        CoolingFacilities.Add(def);
                    }
                    else
                    {
                        existing.Name = def.Name;
                        existing.SubTitle = def.SubTitle;
                        existing.Icon = def.Icon;
                        existing.BaseCost = def.BaseCost;
                        existing.CoolingCapacity = def.CoolingCapacity;
                        existing.AccentColorHex = def.AccentColorHex;
                    }
                }
            }

            if (UnlockedSkills == null)
                UnlockedSkills = new List<string>();

            if (SkillLevels == null)
                SkillLevels = new Dictionary<string, int>();

            foreach (var s in UnlockedSkills)
            {
                if (!SkillLevels.ContainsKey(s))
                {
                    SkillLevels[s] = 1;
                }
            }

            if (CurrentRunEarned <= 0)
            {
                CurrentRunEarned = Math.Max(50.0, Money);
            }

            // 과거 버그로 과도하게 쌓인 연구 토큰 상한 보정 (성장 재미 보존)
            if (ResearchTokens > 25)
            {
                ResearchTokens = 25;
            }
            if (TotalTokensEarned > 35)
            {
                TotalTokensEarned = 35;
            }

            // 신규 3대 시스템 기본값 보장
            if (SoftwareStudio == null) SoftwareStudio = new SoftwareStudioState();
            if (SoftwareStudio.ReleasedProducts == null) SoftwareStudio.ReleasedProducts = new List<ReleasedSoftware>();

            if (B2BMarket == null) B2BMarket = new B2BMarketState();
            if (B2BMarket.AvailableOffers == null || B2BMarket.AvailableOffers.Count == 0)
            {
                B2BMarket.RefreshOffers(new Random(), CalculateTotalRps());
            }

            if (CryptoMarket == null) CryptoMarket = new CryptoMarketState();
            CryptoMarket.InitHistory();
        }

        public void Save()
        {
            try
            {
                LastSaveTimeUtc = DateTime.UtcNow;
                string path = GetSaveFilePath();
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(path, json);
            }
            catch { }
        }

        public double ProcessOfflineProgress(out double elapsedSeconds)
        {
            elapsedSeconds = 0;
            try
            {
                DateTime now = DateTime.UtcNow;
                TimeSpan span = now - LastSaveTimeUtc;
                if (span.TotalSeconds < 10) return 0;

                // [핵심 밸런스]: 기본 2시간 상한 (설비에 따라 최대 8시간까지 확장)
                double maxCapSecs = GetOfflineCapSeconds();
                double secs = Math.Min(span.TotalSeconds, maxCapSecs);
                elapsedSeconds = secs;

                double rps = CalculateTotalRps();
                if (rps <= 0) return 0;

                // 방치 효율 75% 적용
                double earned = rps * secs * 0.75;
                Money += earned;
                CurrentRunEarned += earned;
                TotalEarned += earned;
                LastSaveTimeUtc = now;
                Save();
                return earned;
            }
            catch
            {
                return 0;
            }
        }

        #endregion
    }
}
