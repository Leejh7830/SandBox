using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Newtonsoft.Json;

namespace GateHelper.PocketFarm
{
    public partial class PocketFarmModel
    {
        // 1. 기본 경제 & 성장 지표
        public int Gold { get; set; } = 100;
        public int Fame { get; set; } = 0;
        public int Level { get; set; } = 1;
        public int SiloCapacity { get; set; } = 60;
        public Dictionary<string, int> Inventory { get; private set; } = new Dictionary<string, int>();

        // 2. 자동화 및 상태 토글
        public bool AutoDispatch { get; set; } = false;
        public bool RestaurantOpen { get; set; } = false;

        // 테스트용 관리자 모드 치트
        public bool IsAdminMode { get; set; } = false;
        public int PreAdminGold { get; set; } = 100;
        public int PreAdminFame { get; set; } = 0;

        // 3. 업그레이드 레벨
        public int WorkerCountUpgradeLevel { get; set; } = 1;
        public int ToolSpeedUpgradeLevel { get; set; } = 1;
        public int CartCapacityUpgradeLevel { get; set; } = 1;
        public int SiloUpgradeLevel { get; set; } = 1;

        // 3대 전문 분야별 독립 도구 레벨 (농업, 어업, 목축 도구 분리)
        public int FarmToolLevel { get; set; } = 1;
        public int FishingToolLevel { get; set; } = 1;
        public int LivestockToolLevel { get; set; } = 1;

        // 4. 생산 시설 리스트
        public List<CropPlot> Plots { get; private set; } = new List<CropPlot>();
        public List<FishingSpot> FishingSpots { get; private set; } = new List<FishingSpot>();
        public List<LivestockPen> LivestockPens { get; private set; } = new List<LivestockPen>();

        // 5. 일꾼 리스트
        public List<Worker> Workers { get; private set; } = new List<Worker>();

        // 6. 식당 테이블 & 주문
        public List<RestaurantCustomer> ActiveCustomers { get; private set; } = new List<RestaurantCustomer>();
        public float RestaurantSpawnTimer { get; set; } = 0f;

        // 7. 데이터 정의 사전 및 기술트리
        public Dictionary<string, ItemDef> Items { get; private set; } = new Dictionary<string, ItemDef>();
        public List<RecipeDef> Recipes { get; private set; } = new List<RecipeDef>();
        public List<CropBreedingDef> Breedings { get; private set; } = new List<CropBreedingDef>();
        public List<TechNodeDef> TechTree { get; private set; } = new List<TechNodeDef>();

        // 8. 시각 효과 (플로팅 텍스트 등)
        public List<FloatingText> FloatingTexts { get; private set; } = new List<FloatingText>();

        // 9. 오프라인 시간 추적
        public DateTime LastSaveTime { get; set; } = DateTime.Now;

        // 10. 신규 시스템 (날씨, 업적, 의뢰)
        public FarmWeatherSystem WeatherSystem { get; private set; } = new FarmWeatherSystem();
        public FarmAchievementSystem AchievementSystem { get; private set; } = new FarmAchievementSystem();
        public FarmContractSystem ContractSystem { get; private set; } = new FarmContractSystem();

        public PocketFarmModel()
        {
            InitializeItemDatabase();
            InitializeBreedings();
            InitializeRecipes();
            InitializeFacilities();
            InitializeInitialWorkers();
            InitializeTechTree();
        }

        private void InitializeItemDatabase()
        {
            // --- 작물 (Crops) - 100% 렌더링 보장되는 깨끗한 유니코드 BMP 기호 ---
            AddItem("crop_wheat", "밀", ItemCategory.Crop, 1, 6, "✦", Color.FromArgb(235, 185, 80), "기본적인 황금빛 곡물");
            AddItem("crop_golden_wheat", "황금 듀럼밀", ItemCategory.Crop, 2, 15, "★", Color.FromArgb(255, 215, 0), "품종 개량된 최고급 밀");
            AddItem("crop_carrot", "루비 당근", ItemCategory.Crop, 2, 22, "▲", Color.FromArgb(255, 120, 40), "달콤하고 신선한 뿌리 채소");
            AddItem("crop_potato", "왕감자", ItemCategory.Crop, 2, 40, "●", Color.FromArgb(210, 160, 100), "포슬포슬한 주식용 감자");
            AddItem("crop_corn", "태양 옥수수", ItemCategory.Crop, 3, 75, "◆", Color.FromArgb(255, 230, 80), "여름 햇살을 머금은 옥수수");
            AddItem("crop_strawberry", "별빛 딸기", ItemCategory.Crop, 3, 130, "♥", Color.FromArgb(255, 80, 120), "새콤달콤한 디저트용 딸기");

            // --- 해산물 (Fish) ---
            AddItem("fish_minnow", "피라미", ItemCategory.Fish, 1, 35, "~>", Color.FromArgb(120, 180, 220), "연못에서 흔히 잡히는 작은 물고기");
            AddItem("fish_carp", "비단 잉어", ItemCategory.Fish, 2, 80, "><>", Color.FromArgb(255, 140, 70), "살이 통통하게 오른 민물 잉어");
            AddItem("fish_salmon", "강 연어", ItemCategory.Fish, 2, 150, ">=>", Color.FromArgb(240, 120, 100), "풍미가 깊고 기름진 고급 연어");
            AddItem("fish_tuna", "심해 참치", ItemCategory.Fish, 3, 320, ">==>", Color.FromArgb(60, 120, 200), "레스토랑 오마카세 필수 재료");

            // --- 축산물 (Livestock) ---
            AddItem("live_egg", "신선한 달걀", ItemCategory.LivestockProduct, 1, 30, "(●)", Color.FromArgb(250, 240, 200), "아침마다 닭이 낳는 영양란");
            AddItem("live_wool", "부드러운 양모", ItemCategory.LivestockProduct, 2, 70, "{@}", Color.FromArgb(230, 230, 230), "포근한 양털 뭉치");
            AddItem("live_milk", "고소한 우유", ItemCategory.LivestockProduct, 2, 110, "[우유]", Color.FromArgb(245, 245, 255), "젖소에서 갓 짠 신선한 우유");
            AddItem("live_beef", "숙성 마블 비프", ItemCategory.LivestockProduct, 3, 260, "[비프]", Color.FromArgb(220, 70, 70), "마블링이 훌륭한 고급 소고기");

            // --- 요리 (Dishes) ---
            AddItem("dish_bread", "갓 구운 계란빵", ItemCategory.Dish, 1, 120, "[빵]", Color.FromArgb(230, 170, 90), "따끈따끈 고소한 간식빵");
            AddItem("dish_sushi", "연어 초밥 한 상", ItemCategory.Dish, 2, 380, "[초밥]", Color.FromArgb(255, 130, 110), "신선한 생연어와 밥의 조화");
            AddItem("dish_stew", "진한 감자 비프 스튜", ItemCategory.Dish, 2, 650, "[스튜]", Color.FromArgb(190, 80, 50), "속을 든든하게 채워주는 명품 스튜");
            AddItem("dish_omakase", "골든 셰프 오마카세", ItemCategory.Dish, 3, 1600, "[오마카세]", Color.FromArgb(255, 215, 50), "농·어·목축의 정점이 담긴 특제 코스");
        }

        private void AddItem(string id, string name, ItemCategory cat, int tier, int price, string symbol, Color color, string desc)
        {
            Items[id] = new ItemDef
            {
                Id = id,
                Name = name,
                Category = cat,
                Tier = tier,
                BasePrice = price,
                IconSymbol = symbol,
                ThemeColor = color,
                Description = desc
            };
            Inventory[id] = 0;
        }

        private void InitializeBreedings()
        {
            Breedings.Add(new CropBreedingDef
            {
                BaseCropId = "crop_wheat",
                EvolvedCropId = "crop_golden_wheat",
                RequiredStockpile = 20,
                ResearchCostGold = 120,
                IsResearched = false,
                Description = "기본 밀을 개량하여 가치가 2.5배 높은 황금 듀럼밀 생산"
            });
            Breedings.Add(new CropBreedingDef
            {
                BaseCropId = "crop_golden_wheat",
                EvolvedCropId = "crop_carrot",
                RequiredStockpile = 35,
                ResearchCostGold = 350,
                IsResearched = false,
                Description = "농토의 비옥도를 높여 달콤한 루비 당근 재배 가능"
            });
            Breedings.Add(new CropBreedingDef
            {
                BaseCropId = "crop_carrot",
                EvolvedCropId = "crop_potato",
                RequiredStockpile = 40,
                ResearchCostGold = 700,
                IsResearched = false,
                Description = "토양 개량을 통해 묵직하고 비싼 왕감자 품종 도입"
            });
            Breedings.Add(new CropBreedingDef
            {
                BaseCropId = "crop_potato",
                EvolvedCropId = "crop_strawberry",
                RequiredStockpile = 50,
                ResearchCostGold = 1500,
                IsResearched = false,
                Description = "특수 온실 기법으로 최고급 디저트 별빛 딸기 재배"
            });
        }

        private void InitializeRecipes()
        {
            // 레시피 0: 바삭 피라미 튀김 (밀 1 + 피라미 1) - 초반 어획물 활용
            var r0 = new RecipeDef
            {
                Id = "dish_fried_fish",
                Name = "바삭 피라미 튀김",
                Description = "밀 1개 + 피라미 1개로 노릇하게 튀겨낸 고소한 생선 튀김",
                CookTimeSec = 5,
                SellingPrice = 85,
                FameGain = 4,
                IsUnlocked = true,
                UnlockCost = 0,
                DishSymbol = "[튀김]"
            };
            r0.Ingredients["crop_wheat"] = 1;
            r0.Ingredients["fish_minnow"] = 1;
            Recipes.Add(r0);

            // 레시피 1: 계란빵 (밀 2 + 달걀 1)
            var r1 = new RecipeDef
            {
                Id = "dish_bread",
                Name = "갓 구운 계란빵",
                Description = "밀 2개 + 달걀 1개로 만드는 달콤하고 부드러운 빵",
                CookTimeSec = 6,
                SellingPrice = 120,
                FameGain = 5,
                IsUnlocked = true,
                UnlockCost = 0,
                DishSymbol = "[빵]"
            };
            r1.Ingredients["crop_wheat"] = 2;
            r1.Ingredients["live_egg"] = 1;
            Recipes.Add(r1);

            // 레시피 2: 보양 비단 잉어 찜 (밀 1 + 잉어 1) - 중반 어획물 활용
            var rCarps = new RecipeDef
            {
                Id = "dish_steamed_carp",
                Name = "보양 비단 잉어 찜",
                Description = "밀 1개 + 비단 잉어 1개로 정성껏 쪄낸 전통 보양식",
                CookTimeSec = 8,
                SellingPrice = 220,
                FameGain = 10,
                IsUnlocked = false,
                UnlockCost = 250,
                DishSymbol = "[잉어찜]"
            };
            rCarps.Ingredients["crop_wheat"] = 1;
            rCarps.Ingredients["fish_carp"] = 1;
            Recipes.Add(rCarps);

            // 레시피 3: 연어 초밥 (황금밀 2 + 연어 1)
            var r2 = new RecipeDef
            {
                Id = "dish_sushi",
                Name = "연어 초밥 한 상",
                Description = "황금 듀럼밀 2개 + 강 연어 1개로 만드는 일품 초밥",
                CookTimeSec = 10,
                SellingPrice = 380,
                FameGain = 18,
                IsUnlocked = false,
                UnlockCost = 600,
                DishSymbol = "[초밥]"
            };
            r2.Ingredients["crop_golden_wheat"] = 2;
            r2.Ingredients["fish_salmon"] = 1;
            Recipes.Add(r2);

            // 레시피 4: 비프 스튜 (감자 1 + 당근 1 + 우유 1 + 마블비프 1) - 루비 당근 연계
            var r3 = new RecipeDef
            {
                Id = "dish_stew",
                Name = "진한 가든 비프 스튜",
                Description = "왕감자 1개 + 루비 당근 1개 + 우유 1개 + 마블비프 1개로 푹 끓여낸 명품 스튜",
                CookTimeSec = 15,
                SellingPrice = 720,
                FameGain = 45,
                IsUnlocked = false,
                UnlockCost = 1500,
                DishSymbol = "[스튜]"
            };
            r3.Ingredients["crop_potato"] = 1;
            r3.Ingredients["crop_carrot"] = 1;
            r3.Ingredients["live_milk"] = 1;
            r3.Ingredients["live_beef"] = 1;
            Recipes.Add(r3);

            // 레시피 5: 골든 오마카세 (딸기 2 + 참치 1 + 마블비프 1)
            var r4 = new RecipeDef
            {
                Id = "dish_omakase",
                Name = "골든 셰프 오마카세",
                Description = "별빛 딸기 2개 + 심해 참치 1개 + 마블비프 1개로 완성하는 최상급 코스",
                CookTimeSec = 22,
                SellingPrice = 1750,
                FameGain = 90,
                IsUnlocked = false,
                UnlockCost = 3500,
                DishSymbol = "[오마카세]"
            };
            r4.Ingredients["crop_strawberry"] = 2;
            r4.Ingredients["fish_tuna"] = 1;
            r4.Ingredients["live_beef"] = 1;
            Recipes.Add(r4);
        }

        private void InitializeFacilities()
        {
            // 밭 (6개 플롯, 2개 기본 해금, 4개 순차 해금)
            int plotW = 188;
            int plotH = 135;
            int startX = 35;
            int startY = 150;
            int gapX = 18;
            int gapY = 18;

            Plots.Add(new CropPlot(0, "crop_wheat", 6f, true, 0));
            Plots.Add(new CropPlot(1, "crop_wheat", 6f, true, 0));
            Plots.Add(new CropPlot(2, "crop_wheat", 6f, false, 80));
            Plots.Add(new CropPlot(3, "crop_wheat", 8f, false, 200));
            Plots.Add(new CropPlot(4, "crop_wheat", 10f, false, 450));
            Plots.Add(new CropPlot(5, "crop_wheat", 12f, false, 900));

            for (int i = 0; i < Plots.Count; i++)
            {
                int col = i % 3;
                int row = i / 3;
                Plots[i].Bounds = new Rectangle(startX + col * (plotW + gapX), startY + row * (plotH + gapY), plotW, plotH);
            }

            // 낚시터 (4개 포인트, 1개 기본 해금) - 피라미, 잉어, 연어, 참치
            int cardW = 280;
            int cardH = 118;
            int fishX = 35;
            int fishY = 155;
            int fGapX = 14;
            int fGapY = 14;

            FishingSpots.Add(new FishingSpot(0, "평화의 연못", "fish_minnow", 10f, true, 0));
            FishingSpots.Add(new FishingSpot(1, "은빛 잉어 개울", "fish_carp", 16f, false, 350));
            FishingSpots.Add(new FishingSpot(2, "연어 폭포 여울", "fish_salmon", 24f, false, 900));
            FishingSpots.Add(new FishingSpot(3, "심해 원양 어장", "fish_tuna", 32f, false, 1800));

            for (int i = 0; i < FishingSpots.Count; i++)
            {
                int col = i % 2;
                int row = i / 2;
                FishingSpots[i].Bounds = new Rectangle(fishX + col * (cardW + fGapX), fishY + row * (cardH + fGapY), cardW, cardH);
            }

            // 목장 (4개 축사, 1개 기본 해금) - 달걀, 양모, 우유, 마블비프
            LivestockPens.Add(new LivestockPen(0, "양지바른 닭장", "산란계 닭", "live_egg", 12f, true, 0));
            LivestockPens.Add(new LivestockPen(1, "포근한 양 우리", "메리노 양", "live_wool", 20f, false, 500));
            LivestockPens.Add(new LivestockPen(2, "초원 젖소 외양간", "홀스타인 젖소", "live_milk", 28f, false, 1100));
            LivestockPens.Add(new LivestockPen(3, "초원 한우 목장", "흑우 한우", "live_beef", 36f, false, 2000));

            for (int i = 0; i < LivestockPens.Count; i++)
            {
                int col = i % 2;
                int row = i / 2;
                LivestockPens[i].Bounds = new Rectangle(fishX + col * (cardW + fGapX), fishY + row * (cardH + fGapY), cardW, cardH);
            }
        }

        private void InitializeInitialWorkers()
        {
            // 시작 일꾼 3명 기본 배치 (김농부, 이일꾼, 박목동)
            Workers.Add(new Worker(1, "김농부", WorkerRole.Farmer, new PointF(60, 490)));
            Workers.Add(new Worker(2, "이일꾼", WorkerRole.Farmer, new PointF(85, 490)));
            Workers.Add(new Worker(3, "박목동", WorkerRole.Farmer, new PointF(110, 490)));
        }

        public string GetLevelTitle()
        {
            switch (Level)
            {
                case 1: return "초보 농부";
                case 2: return "동네 비스트로";
                case 3: return "소문난 맛집";
                case 4: return "미식가의 명소";
                case 5: return "전국구 레스토랑";
                default: return "미슐랭 스타";
            }
        }

        public void CheckLevelUp()
        {
            int targetLevel = 1;
            if (Fame >= 7500) targetLevel = 6;
            else if (Fame >= 3000) targetLevel = 5;
            else if (Fame >= 1200) targetLevel = 4;
            else if (Fame >= 400) targetLevel = 3;
            else if (Fame >= 100) targetLevel = 2;

            if (targetLevel > Level)
            {
                Level = targetLevel;
                int bonusGold = Level * 500;
                Gold += bonusGold;
                AddFloatingText($"★ 식당 승급! [Lv.{Level} {GetLevelTitle()}] (보너스 +{bonusGold:N0}G)", 325, 220, Color.FromArgb(255, 215, 60));
                Save();
            }
        }

        public List<string> GetUnlockedCropIds()
        {
            List<string> list = new List<string> { "crop_wheat" };
            foreach (var b in Breedings)
            {
                if (b.IsResearched)
                {
                    if (!list.Contains(b.EvolvedCropId)) list.Add(b.EvolvedCropId);
                }
            }
            return list;
        }

        public void CyclePlotCrop(int plotIndex)
        {
            if (plotIndex < 0 || plotIndex >= Plots.Count) return;
            var plot = Plots[plotIndex];
            if (!plot.IsUnlocked) return;

            if (plot.AssignedWorkerId != -1)
            {
                AddFloatingText("일꾼이 작업 중인 밭은 작물을 변경할 수 없습니다.", plot.Bounds.X + 20, plot.Bounds.Y + 20, Color.FromArgb(255, 180, 100));
                return;
            }

            var unlocked = GetUnlockedCropIds();
            if (unlocked.Count <= 1)
            {
                AddFloatingText("개량된 다른 작물이 아직 없습니다. (품종개량 필요)", plot.Bounds.X + 20, plot.Bounds.Y + 20, Color.FromArgb(200, 200, 200));
                return;
            }

            int curIdx = unlocked.IndexOf(plot.CropId);
            int nextIdx = (curIdx + 1) % unlocked.Count;
            plot.CropId = unlocked[nextIdx];
            plot.GrowthProgress = 0f;
            plot.State = TaskPlotState.Growing;

            string cropName = Items.ContainsKey(plot.CropId) ? Items[plot.CropId].Name : plot.CropId;
            AddFloatingText($"작물 전환: [{cropName}]", plot.Bounds.X + 35, plot.Bounds.Y + 30, Color.FromArgb(100, 255, 180));
            Save();
        }

        public int GetTotalStoredItemCount()
        {
            int sum = 0;
            foreach (var kvp in Inventory)
            {
                sum += kvp.Value;
            }
            return sum;
        }

        public void AddFloatingText(string text, float x, float y, Color color)
        {
            FloatingTexts.Add(new FloatingText(text, x, y, color));
        }

        #region Save / Load Persistence

        public static string GetSaveFilePath()
        {
            try
            {
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_meta", "sandbox", "pocketfarm");
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                return Path.Combine(dir, "pocket_farm.json");
            }
            catch
            {
                return "pocket_farm.json";
            }
        }

        public void Save()
        {
            try
            {
                string path = GetSaveFilePath();
                PocketFarmSaveData dto = ExportSaveData();
                string json = JsonConvert.SerializeObject(dto, Formatting.Indented);
                File.WriteAllText(path, json);
            }
            catch
            {
            }
        }

        public static PocketFarmModel LoadOrCreate()
        {
            string path = GetSaveFilePath();
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    var dto = JsonConvert.DeserializeObject<PocketFarmSaveData>(json);
                    if (dto != null)
                    {
                        var model = new PocketFarmModel();
                        model.ApplySaveData(dto);
                        return model;
                    }
                }
                catch
                {
                }
            }

            var fresh = new PocketFarmModel();
            fresh.Save();
            return fresh;
        }

        public PocketFarmSaveData CreateSaveData() => ExportSaveData();

        public PocketFarmSaveData ExportSaveData()
        {
            var dto = new PocketFarmSaveData
            {
                Gold = this.Gold,
                Fame = this.Fame,
                Level = this.Level,
                SiloCapacity = this.SiloCapacity,
                AutoDispatch = this.AutoDispatch,
                RestaurantOpen = this.RestaurantOpen,
                WorkerCountUpgradeLevel = this.WorkerCountUpgradeLevel,
                ToolSpeedUpgradeLevel = this.ToolSpeedUpgradeLevel,
                CartCapacityUpgradeLevel = this.CartCapacityUpgradeLevel,
                SiloUpgradeLevel = this.SiloUpgradeLevel,
                FarmToolLevel = this.FarmToolLevel,
                FishingToolLevel = this.FishingToolLevel,
                LivestockToolLevel = this.LivestockToolLevel,
                LastSaveTime = DateTime.Now
            };

            foreach (var kvp in Inventory)
            {
                dto.Inventory[kvp.Key] = kvp.Value;
            }

            foreach (var p in Plots)
            {
                dto.Plots.Add(new PlotSaveData { PlotId = p.PlotId, CropId = p.CropId, IsUnlocked = p.IsUnlocked });
            }

            foreach (var s in FishingSpots)
            {
                dto.FishingSpots.Add(new SpotSaveData { SpotId = s.SpotId, IsUnlocked = s.IsUnlocked });
            }

            foreach (var pen in LivestockPens)
            {
                dto.LivestockPens.Add(new PenSaveData { PenId = pen.PenId, IsUnlocked = pen.IsUnlocked, HasFeed = pen.HasFeed });
            }

            foreach (var w in Workers)
            {
                dto.Workers.Add(new WorkerSaveData
                {
                    WorkerId = w.WorkerId,
                    Name = w.Name,
                    Role = w.Role,
                    MoveSpeed = w.MoveSpeed,
                    WorkSpeedMult = w.WorkSpeedMult,
                    CarryCapacity = w.CarryCapacity
                });
            }

            foreach (var b in Breedings)
            {
                dto.Breedings.Add(new BreedingSaveData { BaseCropId = b.BaseCropId, IsResearched = b.IsResearched });
            }

            foreach (var r in Recipes)
            {
                dto.Recipes.Add(new RecipeSaveData { Id = r.Id, IsUnlocked = r.IsUnlocked, CookCount = r.CookCount });
                dto.RecipeCookCounts[r.Id] = r.CookCount;
            }

            foreach (var t in TechTree)
            {
                dto.ResearchedTechs.Add(new TechSaveData { Id = t.Id, IsUnlocked = t.IsUnlocked });
            }

            dto.LifetimeGoldEarned = Math.Max(Gold, AchievementSystem.LifetimeGoldEarned);
            dto.TotalHarvestCount = AchievementSystem.TotalHarvestCount;
            dto.TotalMealsServed = AchievementSystem.TotalMealsServed;
            dto.TotalVipServedCount = AchievementSystem.TotalVipServedCount;
            dto.CurrentWeather = WeatherSystem.CurrentWeather;

            foreach (var a in AchievementSystem.Achievements)
            {
                dto.Achievements.Add(new AchievementSaveData { Id = a.Id, IsClaimed = a.IsClaimed });
            }

            foreach (var c in ContractSystem.DailyContracts)
            {
                dto.Contracts.Add(new ContractSaveData
                {
                    ContractId = c.Id,
                    Title = c.Title,
                    ItemId = c.RequiredItemId,
                    RequiredCount = c.RequiredAmount,
                    RewardGold = c.RewardGold,
                    RewardFame = c.RewardFame,
                    IsCompleted = c.IsCompleted
                });
            }
            dto.AllContractsBonusClaimed = ContractSystem.DailyBonusClaimed;

            return dto;
        }

        public void ApplySaveData(PocketFarmSaveData dto)
        {
            this.Gold = dto.Gold;
            this.Fame = dto.Fame;
            this.Level = dto.Level;
            this.SiloCapacity = Math.Max(60, dto.SiloCapacity);
            this.AutoDispatch = dto.AutoDispatch;
            this.RestaurantOpen = dto.RestaurantOpen;
            this.WorkerCountUpgradeLevel = Math.Max(1, dto.WorkerCountUpgradeLevel);
            this.ToolSpeedUpgradeLevel = Math.Max(1, dto.ToolSpeedUpgradeLevel);
            this.CartCapacityUpgradeLevel = Math.Max(1, dto.CartCapacityUpgradeLevel);
            this.SiloUpgradeLevel = Math.Max(1, dto.SiloUpgradeLevel);
            this.FarmToolLevel = Math.Max(1, dto.FarmToolLevel);
            this.FishingToolLevel = Math.Max(1, dto.FishingToolLevel);
            this.LivestockToolLevel = Math.Max(1, dto.LivestockToolLevel);
            this.LastSaveTime = dto.LastSaveTime;

            if (dto.Inventory != null)
            {
                foreach (var kvp in dto.Inventory)
                {
                    this.Inventory[kvp.Key] = kvp.Value;
                }
            }

            if (dto.Plots != null && dto.Plots.Count > 0)
            {
                foreach (var ps in dto.Plots)
                {
                    var plot = Plots.Find(p => p.PlotId == ps.PlotId);
                    if (plot != null)
                    {
                        plot.IsUnlocked = ps.IsUnlocked;
                        plot.CropId = ps.CropId;
                        plot.State = ps.IsUnlocked ? TaskPlotState.Growing : TaskPlotState.Empty;
                    }
                }
            }

            if (dto.FishingSpots != null && dto.FishingSpots.Count > 0)
            {
                foreach (var ss in dto.FishingSpots)
                {
                    var spot = FishingSpots.Find(s => s.SpotId == ss.SpotId);
                    if (spot != null)
                    {
                        spot.IsUnlocked = ss.IsUnlocked;
                        spot.State = ss.IsUnlocked ? TaskPlotState.Growing : TaskPlotState.Empty;
                    }
                }
            }

            if (dto.LivestockPens != null && dto.LivestockPens.Count > 0)
            {
                foreach (var ps in dto.LivestockPens)
                {
                    var pen = LivestockPens.Find(p => p.PenId == ps.PenId);
                    if (pen != null)
                    {
                        pen.IsUnlocked = ps.IsUnlocked;
                        pen.HasFeed = ps.HasFeed;
                        pen.State = ps.IsUnlocked ? TaskPlotState.Growing : TaskPlotState.Empty;
                    }
                }
            }

            if (dto.Workers != null && dto.Workers.Count > 0)
            {
                Workers.Clear();
                for (int i = 0; i < dto.Workers.Count; i++)
                {
                    var ws = dto.Workers[i];
                    var w = new Worker(ws.WorkerId, ws.Name, ws.Role, new PointF(60 + i * 25, 490))
                    {
                        MoveSpeed = ws.MoveSpeed,
                        WorkSpeedMult = ws.WorkSpeedMult,
                        CarryCapacity = ws.CarryCapacity
                    };
                    Workers.Add(w);
                }
            }

            if (dto.Breedings != null)
            {
                foreach (var bs in dto.Breedings)
                {
                    var b = Breedings.Find(br => br.BaseCropId == bs.BaseCropId);
                    if (b != null) b.IsResearched = bs.IsResearched;
                }
            }

            if (dto.Recipes != null)
            {
                foreach (var rs in dto.Recipes)
                {
                    var r = Recipes.Find(rc => rc.Id == rs.Id);
                    if (r != null)
                    {
                        r.IsUnlocked = rs.IsUnlocked;
                        r.CookCount = rs.CookCount;
                    }
                }
            }

            if (dto.ResearchedTechs != null)
            {
                foreach (var ts in dto.ResearchedTechs)
                {
                    var node = TechTree.Find(t => t.Id == ts.Id);
                    if (node != null) node.IsUnlocked = ts.IsUnlocked;
                }
            }

            if (dto.Achievements != null)
            {
                foreach (var a in dto.Achievements)
                {
                    var ach = AchievementSystem.Achievements.Find(x => x.Id == a.Id);
                    if (ach != null) ach.IsClaimed = a.IsClaimed;
                }
            }
            AchievementSystem.LifetimeGoldEarned = Math.Max(dto.LifetimeGoldEarned, dto.Gold);
            AchievementSystem.TotalHarvestCount = dto.TotalHarvestCount;
            AchievementSystem.TotalMealsServed = dto.TotalMealsServed;
            AchievementSystem.TotalVipServedCount = dto.TotalVipServedCount;

            if (dto.Contracts != null && dto.Contracts.Count > 0)
            {
                ContractSystem.DailyContracts.Clear();
                foreach (var cs in dto.Contracts)
                {
                    ContractSystem.DailyContracts.Add(new DeliveryContract(cs.ContractId, FarmTabType.Farm, cs.Title, "", cs.ItemId, cs.RequiredCount, cs.RewardGold, cs.RewardFame)
                    {
                        IsCompleted = cs.IsCompleted
                    });
                }
                ContractSystem.DailyBonusClaimed = dto.AllContractsBonusClaimed;
            }

            WeatherSystem.SetWeather(dto.CurrentWeather);

            // 창고 용량 초과분 보정 (기존 세이브 파일의 초과분도 즉시 골드로 정상 정산)
            ClampInventoryToCapacity();

            // 오프라인 시간 정산
            TimeSpan span = DateTime.Now - dto.LastSaveTime;
            if (span.TotalSeconds > 10.0)
            {
                CalculateOfflineProgress(span.TotalSeconds);
            }

            ClampInventoryToCapacity();
        }

        public void ClampInventoryToCapacity()
        {
            int currentTotal = GetTotalStoredItemCount();
            if (currentTotal > SiloCapacity)
            {
                int excess = currentTotal - SiloCapacity;
                var keys = new List<string>(Inventory.Keys);
                foreach (var key in keys)
                {
                    if (excess <= 0) break;
                    int count = Inventory[key];
                    if (count > 0)
                    {
                        int remove = Math.Min(count, excess);
                        Inventory[key] -= remove;
                        excess -= remove;
                        int unitPrice = Items.ContainsKey(key) ? Items[key].BasePrice : 6;
                        Gold += remove * unitPrice;
                    }
                }
            }
        }

        public int GetWorkersInRoleCount(WorkerRole role)
        {
            int count = 0;
            foreach (var w in Workers)
            {
                if (w.State != WorkerState.Idle && w.Role == role) count++;
            }
            return count;
        }

        public int GetIdleWorkerCount()
        {
            int count = 0;
            foreach (var w in Workers)
            {
                if (w.State == WorkerState.Idle) count++;
            }
            return count;
        }

        public void ToggleAdminMode()
        {
            IsAdminMode = !IsAdminMode;
            if (IsAdminMode)
            {
                PreAdminGold = Gold;
                PreAdminFame = Fame;
                Gold = 9999999;
                Fame = 99999;
                AddFloatingText("[치트] 무한 골드/명성 치트 ON!", 325, 200, Color.FromArgb(255, 215, 60));
            }
            else
            {
                Gold = Math.Max(PreAdminGold, 100);
                Fame = PreAdminFame;
                AddFloatingText("[치트] 치트 OFF (원래 잔액 복원)", 325, 200, Color.FromArgb(200, 200, 200));
            }
            Save();
        }

        public void ResetAllData()
        {
            IsAdminMode = false;
            Gold = 100;
            Fame = 0;
            Level = 1;
            SiloCapacity = 60;
            AutoDispatch = false;
            RestaurantOpen = false;
            WorkerCountUpgradeLevel = 1;
            ToolSpeedUpgradeLevel = 1;
            CartCapacityUpgradeLevel = 1;
            SiloUpgradeLevel = 1;
            FarmToolLevel = 1;
            FishingToolLevel = 1;
            LivestockToolLevel = 1;

            var keys = new List<string>(Inventory.Keys);
            foreach (var k in keys) Inventory[k] = 0;

            for (int i = 0; i < Plots.Count; i++)
            {
                Plots[i].IsUnlocked = (i < 2);
                Plots[i].CropId = "crop_wheat";
                Plots[i].State = (i < 2) ? TaskPlotState.Growing : TaskPlotState.Empty;
                Plots[i].GrowthProgress = 0f;
                Plots[i].AssignedWorkerId = -1;
            }

            for (int i = 0; i < FishingSpots.Count; i++)
            {
                FishingSpots[i].IsUnlocked = (i == 0);
                FishingSpots[i].State = (i == 0) ? TaskPlotState.Growing : TaskPlotState.Empty;
                FishingSpots[i].CatchProgress = 0f;
                FishingSpots[i].AssignedWorkerId = -1;
            }

            for (int i = 0; i < LivestockPens.Count; i++)
            {
                LivestockPens[i].IsUnlocked = (i == 0);
                LivestockPens[i].State = (i == 0) ? TaskPlotState.Growing : TaskPlotState.Empty;
                LivestockPens[i].ProduceProgress = 0f;
                LivestockPens[i].HasFeed = false;
                LivestockPens[i].AssignedWorkerId = -1;
            }

            Workers.Clear();
            Workers.Add(new Worker(1, "김농부", WorkerRole.Farmer, new PointF(60, 490)));
            Workers.Add(new Worker(2, "이일꾼", WorkerRole.Farmer, new PointF(85, 490)));
            Workers.Add(new Worker(3, "박목동", WorkerRole.Farmer, new PointF(110, 490)));

            foreach (var b in Breedings) b.IsResearched = false;
            foreach (var r in Recipes)
            {
                r.IsUnlocked = (r.Id == "dish_fried_fish" || r.Id == "dish_bread");
                r.CookCount = 0;
            }
            foreach (var t in TechTree) t.IsUnlocked = false;

            WeatherSystem.SetWeather(FarmWeatherType.Sunny);
            AchievementSystem = new FarmAchievementSystem();
            ContractSystem = new FarmContractSystem();
            ActiveCustomers.Clear();
            FloatingTexts.Clear();
            RestaurantSpawnTimer = 0f;

            Save();
            AddFloatingText("[초기화] 게임 데이터가 초기화되었습니다!", 325, 200, Color.FromArgb(255, 120, 120));
        }

        #endregion

        #region Tech Tree & Specialized Tools

        private void InitializeTechTree()
        {
            TechTree.Clear();

            // 1. 농경 혁신 (Agriculture)
            TechTree.Add(new TechNodeDef
            {
                Id = "tech_bio_fertilizer",
                Name = "특수 유기농 비료",
                Category = TechCategory.Agriculture,
                Tier = 1,
                CostGold = 160,
                CostFame = 8,
                RequiredParentId = null,
                IsUnlocked = false,
                Description = "천연 발효 비료를 살포하여 작물 수확 시 25% 확률로 +1개 추가 획득",
                EffectSummary = "밭 수확 시 25% 추가 수확"
            });

            TechTree.Add(new TechNodeDef
            {
                Id = "tech_drip_irrigation",
                Name = "점적 관수 파이프",
                Category = TechCategory.Agriculture,
                Tier = 2,
                CostGold = 420,
                CostFame = 30,
                RequiredParentId = "tech_bio_fertilizer",
                IsUnlocked = false,
                Description = "작물 뿌리에 미세 수분을 자동 공급하여 모든 밭 자연 성장 속도 +20% 가속",
                EffectSummary = "모든 밭 자연 성장 +20% 가속"
            });

            // 2. 수산 공학 (Fishery)
            TechTree.Add(new TechNodeDef
            {
                Id = "tech_sonar_lure",
                Name = "초음파 집어등",
                Category = TechCategory.Fishery,
                Tier = 1,
                CostGold = 220,
                CostFame = 15,
                RequiredParentId = null,
                IsUnlocked = false,
                Description = "어종을 유인하는 초음파 장치로 어획 주기 25% 단축 및 20% 확률 +1 포획",
                EffectSummary = "어획 주기 -25%, 추가포획 20%"
            });

            TechTree.Add(new TechNodeDef
            {
                Id = "tech_deep_sonar",
                Name = "심해 유인 어망",
                Category = TechCategory.Fishery,
                Tier = 2,
                CostGold = 500,
                CostFame = 35,
                RequiredParentId = "tech_sonar_lure",
                IsUnlocked = false,
                Description = "심해 수류를 활용하는 첨단 어망으로 어획 주기 +20% 가속 및 25% 추가 포획",
                EffectSummary = "어획 주기 -20%, 추가포획 25%"
            });

            // 3. 생명 공학 (Livestock)
            TechTree.Add(new TechNodeDef
            {
                Id = "tech_herbal_feed",
                Name = "면역 증강 허브 배합",
                Category = TechCategory.Livestock,
                Tier = 1,
                CostGold = 250,
                CostFame = 18,
                RequiredParentId = null,
                IsUnlocked = false,
                Description = "가축의 생체 리듬을 개선하여 축산품 생산 속도 +25% 가속 및 20% 확률 +1 채집",
                EffectSummary = "축사 생산주기 -25%, 추가채집 20%"
            });

            TechTree.Add(new TechNodeDef
            {
                Id = "tech_warm_shed",
                Name = "스마트 온열 축사",
                Category = TechCategory.Livestock,
                Tier = 2,
                CostGold = 550,
                CostFame = 40,
                RequiredParentId = "tech_herbal_feed",
                IsUnlocked = false,
                Description = "항온 항습 바이오 축사를 가동하여 축산물 생산 속도 +20% 가속 및 25% 추가 채집",
                EffectSummary = "축사 생산주기 -20%, 추가채집 25%"
            });

            // 4. 자동화 및 보존 과학 (Logistics & Preservation) - 3단계 트리
            TechTree.Add(new TechNodeDef
            {
                Id = "tech_auto_dispatch",
                Name = "스마트 자율 파견",
                Category = TechCategory.Preservation,
                Tier = 1,
                CostGold = 150,
                CostFame = 5,
                RequiredParentId = null,
                IsUnlocked = false,
                Description = "유휴 일꾼들이 일터 상황을 스스로 판단하여 자동으로 파견되는 자율 순찰 루틴",
                EffectSummary = "하단 [자동 파견] 루틴 해금"
            });

            TechTree.Add(new TechNodeDef
            {
                Id = "tech_vacuum_pack",
                Name = "진공 급속 포장기",
                Category = TechCategory.Preservation,
                Tier = 2,
                CostGold = 350,
                CostFame = 25,
                RequiredParentId = "tech_auto_dispatch",
                IsUnlocked = false,
                Description = "수확물을 밀폐 포장하여 오프라인 보존 시간 +2시간 추가 연장",
                EffectSummary = "오프라인 보존: 최대 6시간"
            });

            TechTree.Add(new TechNodeDef
            {
                Id = "tech_cold_storage",
                Name = "저온 냉동 보관실",
                Category = TechCategory.Preservation,
                Tier = 3,
                CostGold = 700,
                CostFame = 50,
                RequiredParentId = "tech_vacuum_pack",
                IsUnlocked = false,
                Description = "대형 급속 냉동고를 구축하여 오프라인 보존 시간 +4시간 추가 연장",
                EffectSummary = "오프라인 보존: 최대 10시간"
            });

            // 5. 외식 및 유통 (Commerce & Dining)
            TechTree.Add(new TechNodeDef
            {
                Id = "tech_gourmet_review",
                Name = "미식 블로그 마케팅",
                Category = TechCategory.Commerce,
                Tier = 1,
                CostGold = 320,
                CostFame = 25,
                RequiredParentId = null,
                IsUnlocked = false,
                Description = "식당 입소문을 통해 손님이 결제하는 코스 요리 판매가 +20% 및 명성 +30%",
                EffectSummary = "식당 결제액 +20%, 명성 +30%"
            });

            TechTree.Add(new TechNodeDef
            {
                Id = "tech_direct_trade",
                Name = "전국 로컬푸드 직거래",
                Category = TechCategory.Commerce,
                Tier = 2,
                CostGold = 650,
                CostFame = 50,
                RequiredParentId = "tech_gourmet_review",
                IsUnlocked = false,
                Description = "도매상을 거치지 않는 산지 직거래 협약으로 전량 출하($) 시 판매가 +15% 우대",
                EffectSummary = "전량 출하 단가 +15% 보너스"
            });
        }

        public double GetMaxOfflineSeconds()
        {
            double sec = 14400.0; // 기본 4시간 (14400초)
            if (IsTechUnlocked("tech_vacuum_pack")) sec += 7200.0; // +2시간 (6시간)
            if (IsTechUnlocked("tech_cold_storage")) sec += 14400.0; // +4시간 (10시간)
            return sec;
        }

        public bool IsTechUnlocked(string techId)
        {
            var node = TechTree.Find(t => t.Id == techId);
            return node != null && node.IsUnlocked;
        }

        public string GetFarmToolName(int level)
        {
            switch (level)
            {
                case 1: return "무쇠 호미";
                case 2: return "강화 삼지창 괭이";
                case 3: return "강철 날 회전 쟁기";
                case 4: return "자율주행 트랙터";
                default: return "AI 양자 하베스터";
            }
        }

        public string GetFishingToolName(int level)
        {
            switch (level)
            {
                case 1: return "대나무 낚싯대";
                case 2: return "스피닝 릴 낚싯대";
                case 3: return "티타늄 카본 낚싯대";
                case 4: return "초음파 어군탐지 찌";
                default: return "자율항해 첨단 어선";
            }
        }

        public string GetLivestockToolName(int level)
        {
            switch (level)
            {
                case 1: return "나무 양동이";
                case 2: return "천연 모 브러시";
                case 3: return "자동 온열 급수기";
                case 4: return "스마트 바이오 배합기";
                default: return "첨단 나노 케어 축사";
            }
        }

        #endregion
    }
}
