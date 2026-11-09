using System;
using System.Collections.Generic;

namespace GateHelper.PocketFarm
{
    public class AchievementDef
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int TargetValue { get; set; }
        public int CurrentProgress { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsClaimed { get; set; }
        public int RewardGold { get; set; }
        public int RewardFame { get; set; }

        public AchievementDef(string id, string title, string desc, int target, int rewardGold, int rewardFame)
        {
            Id = id;
            Title = title;
            Description = desc;
            TargetValue = target;
            CurrentProgress = 0;
            IsCompleted = false;
            IsClaimed = false;
            RewardGold = rewardGold;
            RewardFame = rewardFame;
        }
    }

    public class FarmAchievementSystem
    {
        public List<AchievementDef> Achievements { get; private set; } = new List<AchievementDef>();

        // 누적 기록 지표
        public int TotalHarvestCount { get; set; } = 0;
        public int TotalMealsServed { get; set; } = 0;
        public int TotalBreedingCount { get; set; } = 0;
        public int TotalVipServedCount { get; set; } = 0;
        public int LifetimeGoldEarned { get; set; } = 0;

        public FarmAchievementSystem()
        {
            InitializeAchievements();
        }

        private void InitializeAchievements()
        {
            Achievements.Clear();
            Achievements.Add(new AchievementDef("ach_harvest_10", "첫걸음 농부", "밭 작물/어획/축산물 총 10회 수확", 10, 150, 10));
            Achievements.Add(new AchievementDef("ach_harvest_100", "대지의 수호자", "누적 수확 100회 달성", 100, 800, 50));
            Achievements.Add(new AchievementDef("ach_serve_10", "골목 비스트로", "식당 손님 10명 식사 대접", 10, 300, 25));
            Achievements.Add(new AchievementDef("ach_serve_50", "소문난 맛집", "식당 손님 50명 식사 대접", 50, 1200, 100));
            Achievements.Add(new AchievementDef("ach_breed_first", "원예 육종가", "신품종 작물 1종 개량 완료", 1, 400, 30));
            Achievements.Add(new AchievementDef("ach_breed_all", "마스터 브리더", "신품종 작물 4종 모두 개량 완료", 4, 3000, 200));
            Achievements.Add(new AchievementDef("ach_tech_auto", "산업 혁명", "스마트 자율 파견 기술 연구 완료", 1, 500, 40));
            Achievements.Add(new AchievementDef("ach_vip_served", "미식가의 찬사", "VIP 미식 평가원 식사 만족 서빙", 1, 1500, 100));
            Achievements.Add(new AchievementDef("ach_all_facilities", "만능 영지", "모든 밭, 낚시터, 축사 시설 개방 (14곳)", 14, 3500, 250));
            Achievements.Add(new AchievementDef("ach_gold_rich", "거상의 길", "누적 총 50,000G 골드 획득 달성", 50000, 5000, 300));
        }

        public void CheckAllMilestones(PocketFarmModel model)
        {
            if (model == null) return;
            foreach (var ach in Achievements)
            {
                ach.CurrentProgress = GetCurrentProgress(ach.Id, model);
                if (ach.CurrentProgress >= ach.TargetValue)
                {
                    ach.IsCompleted = true;
                }
            }
        }

        public int GetCurrentProgress(string achId, PocketFarmModel model)
        {
            if (model == null) return 0;
            switch (achId)
            {
                case "ach_harvest_10":
                case "ach_harvest_100":
                    return TotalHarvestCount;

                case "ach_serve_10":
                case "ach_serve_50":
                    return TotalMealsServed;

                case "ach_breed_first":
                case "ach_breed_all":
                    int bredCount = 0;
                    foreach (var b in model.Breedings) if (b.IsResearched) bredCount++;
                    return bredCount;

                case "ach_tech_auto":
                    return model.IsTechUnlocked("tech_auto_dispatch") ? 1 : 0;

                case "ach_vip_served":
                    return TotalVipServedCount;

                case "ach_all_facilities":
                    int facCount = 0;
                    foreach (var p in model.Plots) if (p.IsUnlocked) facCount++;
                    foreach (var f in model.FishingSpots) if (f.IsUnlocked) facCount++;
                    foreach (var l in model.LivestockPens) if (l.IsUnlocked) facCount++;
                    return facCount;

                case "ach_gold_rich":
                    return Math.Max(model.Gold, LifetimeGoldEarned);

                default:
                    return 0;
            }
        }

        public bool CanClaim(AchievementDef ach, PocketFarmModel model)
        {
            if (ach.IsClaimed) return false;
            return ach.IsCompleted || (model != null && GetCurrentProgress(ach.Id, model) >= ach.TargetValue);
        }

        public int GetClaimableCount(PocketFarmModel model = null)
        {
            int count = 0;
            foreach (var ach in Achievements)
            {
                if (!ach.IsClaimed)
                {
                    if (ach.IsCompleted || (model != null && GetCurrentProgress(ach.Id, model) >= ach.TargetValue))
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        public bool ClaimReward(string achId, PocketFarmModel model, out int goldGained, out int fameGained)
        {
            goldGained = 0;
            fameGained = 0;

            var ach = Achievements.Find(a => a.Id == achId);
            if (ach == null || !CanClaim(ach, model)) return false;

            ach.IsClaimed = true;
            ach.IsCompleted = true;
            goldGained = ach.RewardGold;
            fameGained = ach.RewardFame;

            model.Gold += goldGained;
            model.Fame += fameGained;
            model.CheckLevelUp();
            model.Save();
            return true;
        }
    }
}
