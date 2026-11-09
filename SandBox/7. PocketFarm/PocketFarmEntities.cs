using System;
using System.Collections.Generic;
using System.Drawing;

namespace GateHelper.PocketFarm
{
    public enum FarmTabType
    {
        Farm,        // [농장] 밭
        Fishing,     // [낚시] 어획
        Livestock,   // [목축] 목장
        Restaurant   // [식당] 다이닝
    }

    public enum ItemCategory
    {
        Crop,
        Fish,
        LivestockProduct,
        Dish
    }

    public class ItemDef
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public ItemCategory Category { get; set; }
        public int Tier { get; set; }
        public int BasePrice { get; set; }
        public string IconSymbol { get; set; }
        public Color ThemeColor { get; set; }
        public string Description { get; set; }
    }

    public enum TaskPlotState
    {
        Empty,
        Growing,
        Ready,
        Working
    }

    public class CropPlot
    {
        public int PlotId { get; set; }
        public string CropId { get; set; }
        public float GrowthProgress { get; set; } // 0.0 ~ 1.0
        public float GrowthDuration { get; set; } // seconds
        public bool IsUnlocked { get; set; }
        public int UnlockCost { get; set; }
        public TaskPlotState State { get; set; }
        public int AssignedWorkerId { get; set; }
        public Rectangle Bounds { get; set; }

        public CropPlot(int id, string defaultCrop, float duration, bool unlocked, int cost)
        {
            PlotId = id;
            CropId = defaultCrop;
            GrowthDuration = duration;
            GrowthProgress = 0f;
            IsUnlocked = unlocked;
            UnlockCost = cost;
            State = unlocked ? TaskPlotState.Growing : TaskPlotState.Empty;
            AssignedWorkerId = -1;
        }
    }

    public class FishingSpot
    {
        public int SpotId { get; set; }
        public string SpotName { get; set; }
        public string FishId { get; set; }
        public float CatchProgress { get; set; }
        public float CatchDuration { get; set; }
        public bool IsUnlocked { get; set; }
        public int UnlockCost { get; set; }
        public TaskPlotState State { get; set; }
        public int AssignedWorkerId { get; set; }
        public Rectangle Bounds { get; set; }

        public FishingSpot(int id, string name, string fishId, float duration, bool unlocked, int cost)
        {
            SpotId = id;
            SpotName = name;
            FishId = fishId;
            CatchDuration = duration;
            CatchProgress = 0f;
            IsUnlocked = unlocked;
            UnlockCost = cost;
            State = unlocked ? TaskPlotState.Growing : TaskPlotState.Empty;
            AssignedWorkerId = -1;
        }
    }

    public class LivestockPen
    {
        public int PenId { get; set; }
        public string PenName { get; set; }
        public string AnimalName { get; set; }
        public string ProductId { get; set; }
        public float ProduceProgress { get; set; }
        public float ProduceDuration { get; set; }
        public bool IsUnlocked { get; set; }
        public int UnlockCost { get; set; }
        public bool HasFeed { get; set; }
        public TaskPlotState State { get; set; }
        public int AssignedWorkerId { get; set; }
        public Rectangle Bounds { get; set; }

        public LivestockPen(int id, string penName, string animal, string prodId, float duration, bool unlocked, int cost)
        {
            PenId = id;
            PenName = penName;
            AnimalName = animal;
            ProductId = prodId;
            ProduceDuration = duration;
            ProduceProgress = 0f;
            IsUnlocked = unlocked;
            UnlockCost = cost;
            HasFeed = false;
            State = unlocked ? TaskPlotState.Growing : TaskPlotState.Empty;
            AssignedWorkerId = -1;
        }
    }

    public class RecipeDef
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Dictionary<string, int> Ingredients { get; set; } = new Dictionary<string, int>();
        public int CookTimeSec { get; set; }
        public int SellingPrice { get; set; }
        public int FameGain { get; set; }
        public bool IsUnlocked { get; set; }
        public int UnlockCost { get; set; }
        public string DishSymbol { get; set; }

        public int CookCount { get; set; } = 0;
        public int MasteryLevel => (CookCount >= 20) ? 3 : ((CookCount >= 5) ? 2 : 1);
        public string MasteryStars => (MasteryLevel == 3) ? "★★★ (MASTER)" : ((MasteryLevel == 2) ? "★★☆" : "★☆☆");
    }

    public enum CustomerState
    {
        Waiting,
        Eating,
        Satisfied
    }

    public class RestaurantCustomer
    {
        public int TableIndex { get; set; }
        public string CustomerName { get; set; }
        public RecipeDef OrderedRecipe { get; set; }
        public float EatProgress { get; set; }
        public float EatDuration { get; set; }
        public CustomerState State { get; set; }
        public int TotalPaid { get; set; }
        public int FameAwarded { get; set; }
        public bool IsVip { get; set; } = false;
        public string VipTitle { get; set; } = "";
    }

    public enum WorkerRole
    {
        Farmer,
        Fisherman,
        Rancher,
        Chef
    }

    public enum WorkerState
    {
        Idle,
        MovingToTask,
        Working,
        ReturningToStorage
    }

    public class Worker
    {
        public int WorkerId { get; set; }
        public string Name { get; set; }
        public WorkerRole Role { get; set; }
        public WorkerState State { get; set; }

        public PointF CurrentPos { get; set; }
        public PointF HomePos { get; set; }
        public PointF TargetPos { get; set; }
        public float MoveT { get; set; }

        public int TargetTaskId { get; set; } // PlotId / SpotId / PenId / TableIndex
        public float WorkTimer { get; set; }
        public float WorkDuration { get; set; }

        public string CarriedItemId { get; set; }
        public int CarriedCount { get; set; }

        public float MoveSpeed { get; set; } = 1.0f;
        public float WorkSpeedMult { get; set; } = 1.0f;
        public int CarryCapacity { get; set; } = 1;
        public WorkerTaskType TaskType { get; set; } = WorkerTaskType.None;

        public Worker(int id, string name, WorkerRole role, PointF home)
        {
            WorkerId = id;
            Name = name;
            Role = role;
            HomePos = home;
            CurrentPos = home;
            TargetPos = home;
            State = WorkerState.Idle;
            TargetTaskId = -1;
            CarriedItemId = null;
            CarriedCount = 0;
            TaskType = WorkerTaskType.None;
        }
    }

    public enum WorkerTaskType
    {
        None,
        Harvest,
        Tend
    }

    public class FloatingText
    {
        public string Text { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public Color Color { get; set; }
        public float Alpha { get; set; }
        public float Lifetime { get; set; }

        public FloatingText(string text, float x, float y, Color color)
        {
            Text = text;
            X = x;
            Y = y;
            Color = color;
            Alpha = 1.0f;
            Lifetime = 1.5f;
        }
    }

    public enum ModalViewType
    {
        None,
        Upgrades,
        TechTree,
        Breeding,
        Recipes,
        Achievements,
        Contracts,
        SiloSummary
    }

    public enum TechCategory
    {
        Preservation, // 보존 과학 (오프라인 보존 시간)
        Agriculture,  // 농경 혁신 (작물 수확량 및 속도)
        Fishery,      // 수산 공학 (어획 시간 및 입질)
        Livestock,    // 생명 공학 (가축 생산 및 사료)
        Commerce      // 유통 외식 (출하 가격 및 식당 매출)
    }

    public class TechNodeDef
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public TechCategory Category { get; set; }
        public int Tier { get; set; }
        public int CostGold { get; set; }
        public int CostFame { get; set; }
        public string RequiredParentId { get; set; }
        public bool IsUnlocked { get; set; }
        public string Description { get; set; }
        public string EffectSummary { get; set; }
    }

    public class CropBreedingDef
    {
        public string BaseCropId { get; set; }
        public string EvolvedCropId { get; set; }
        public int RequiredStockpile { get; set; }
        public int ResearchCostGold { get; set; }
        public bool IsResearched { get; set; }
        public string Description { get; set; }
    }

    #region Save / Load Data DTOs

    public class PlotSaveData
    {
        public int PlotId { get; set; }
        public string CropId { get; set; }
        public bool IsUnlocked { get; set; }
    }

    public class SpotSaveData
    {
        public int SpotId { get; set; }
        public bool IsUnlocked { get; set; }
    }

    public class PenSaveData
    {
        public int PenId { get; set; }
        public bool IsUnlocked { get; set; }
        public bool HasFeed { get; set; }
    }

    public class WorkerSaveData
    {
        public int WorkerId { get; set; }
        public string Name { get; set; }
        public WorkerRole Role { get; set; }
        public float MoveSpeed { get; set; }
        public float WorkSpeedMult { get; set; }
        public int CarryCapacity { get; set; }
    }

    public class BreedingSaveData
    {
        public string BaseCropId { get; set; }
        public bool IsResearched { get; set; }
    }

    public class RecipeSaveData
    {
        public string Id { get; set; }
        public bool IsUnlocked { get; set; }
        public int CookCount { get; set; }
    }

    public class AchievementSaveData
    {
        public string Id { get; set; }
        public bool IsClaimed { get; set; }
    }

    public class ContractSaveData
    {
        public string ContractId { get; set; }
        public string Title { get; set; }
        public string ItemId { get; set; }
        public int RequiredCount { get; set; }
        public int RewardGold { get; set; }
        public int RewardFame { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class TechSaveData
    {
        public string Id { get; set; }
        public bool IsUnlocked { get; set; }
    }

    public class PocketFarmSaveData
    {
        public int Gold { get; set; } = 100;
        public int Fame { get; set; } = 0;
        public int Level { get; set; } = 1;
        public int SiloCapacity { get; set; } = 60;
        public Dictionary<string, int> Inventory { get; set; } = new Dictionary<string, int>();

        public bool AutoDispatch { get; set; } = false;
        public bool RestaurantOpen { get; set; } = false;

        public int WorkerCountUpgradeLevel { get; set; } = 1;
        public int ToolSpeedUpgradeLevel { get; set; } = 1;
        public int CartCapacityUpgradeLevel { get; set; } = 1;
        public int SiloUpgradeLevel { get; set; } = 1;

        // 3대 분야별 독립 전문 도구 레벨
        public int FarmToolLevel { get; set; } = 1;
        public int FishingToolLevel { get; set; } = 1;
        public int LivestockToolLevel { get; set; } = 1;

        // 시스템 통계 및 날씨
        public int LifetimeGoldEarned { get; set; } = 0;
        public int TotalHarvestCount { get; set; } = 0;
        public int TotalMealsServed { get; set; } = 0;
        public int TotalVipServedCount { get; set; } = 0;
        public FarmWeatherType CurrentWeather { get; set; } = FarmWeatherType.Sunny;

        public List<PlotSaveData> Plots { get; set; } = new List<PlotSaveData>();
        public List<SpotSaveData> FishingSpots { get; set; } = new List<SpotSaveData>();
        public List<PenSaveData> LivestockPens { get; set; } = new List<PenSaveData>();
        public List<WorkerSaveData> Workers { get; set; } = new List<WorkerSaveData>();
        public List<BreedingSaveData> Breedings { get; set; } = new List<BreedingSaveData>();
        public List<RecipeSaveData> Recipes { get; set; } = new List<RecipeSaveData>();
        public Dictionary<string, int> RecipeCookCounts { get; set; } = new Dictionary<string, int>();
        public List<TechSaveData> ResearchedTechs { get; set; } = new List<TechSaveData>();
        public List<AchievementSaveData> Achievements { get; set; } = new List<AchievementSaveData>();
        public List<ContractSaveData> Contracts { get; set; } = new List<ContractSaveData>();
        public bool AllContractsBonusClaimed { get; set; } = false;

        public DateTime LastSaveTime { get; set; } = DateTime.Now;
    }

    #endregion
}
