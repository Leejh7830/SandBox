using System;
using System.Collections.Generic;
using System.Drawing;

namespace GateHelper.PocketFarm
{
    public partial class PocketFarmModel
    {
        private static readonly Random _rnd = new Random();

        public void Tick(float dt)
        {
            // 0. 동적 기후 시스템 및 일일 의뢰 갱신
            if (WeatherSystem != null)
            {
                var prevWeather = WeatherSystem.CurrentWeather;
                WeatherSystem.Tick(dt);
                if (WeatherSystem.CurrentWeather != prevWeather)
                {
                    AddFloatingText($"[날씨 변화] {WeatherSystem.CurrentWeatherName} ({WeatherSystem.CurrentWeatherDescription})", 325, 45, Color.FromArgb(255, 230, 100));
                }
            }
            if (ContractSystem != null)
            {
                ContractSystem.Tick(dt);
            }

            // 1. 밭 작물 자연 성장 (점적 관수 파이프 + 기후 보너스)
            float weatherCropMult = WeatherSystem != null ? WeatherSystem.CropSpeedMultiplier : 1.0f;
            float cropSpeed = (IsTechUnlocked("tech_drip_irrigation") ? 1.20f : 1.0f) * weatherCropMult;
            foreach (var plot in Plots)
            {
                if (plot.IsUnlocked && plot.State == TaskPlotState.Growing)
                {
                    plot.GrowthProgress += (dt * cropSpeed) / Math.Max(1f, plot.GrowthDuration);
                    if (plot.GrowthProgress >= 1.0f)
                    {
                        plot.GrowthProgress = 1.0f;
                        plot.State = TaskPlotState.Ready;
                    }
                }
            }

            // 2. 낚시터 어획 진행 (초음파 집어등 & 심해 유인 어망 + 기후 보너스)
            float weatherFishMult = WeatherSystem != null ? WeatherSystem.FishingSpeedMultiplier : 1.0f;
            float fishSpeed = 1.0f * weatherFishMult;
            if (IsTechUnlocked("tech_sonar_lure")) fishSpeed *= 1.25f;
            if (IsTechUnlocked("tech_deep_sonar")) fishSpeed *= 1.20f;

            foreach (var spot in FishingSpots)
            {
                if (spot.IsUnlocked && spot.State == TaskPlotState.Growing)
                {
                    spot.CatchProgress += (dt * fishSpeed) / Math.Max(1f, spot.CatchDuration);
                    if (spot.CatchProgress >= 1.0f)
                    {
                        spot.CatchProgress = 1.0f;
                        spot.State = TaskPlotState.Ready;
                    }
                }
            }

            // 3. 목장 축산품 생산 (사료 급여 2배, 면역 허브 배합 & 온열 바이오 축사)
            float herbSpeed = 1.0f;
            if (IsTechUnlocked("tech_herbal_feed")) herbSpeed *= 1.25f;
            if (IsTechUnlocked("tech_warm_shed")) herbSpeed *= 1.20f;

            foreach (var pen in LivestockPens)
            {
                if (pen.IsUnlocked)
                {
                    // 자동 사료 공급 편의성 (노클릭): 밀이 있고 축사가 가동(성장) 중일 때만 사료 공급
                    if (!pen.HasFeed && pen.State == TaskPlotState.Growing && Inventory.ContainsKey("crop_wheat") && Inventory["crop_wheat"] > 0)
                    {
                        Inventory["crop_wheat"]--;
                        pen.HasFeed = true;
                    }

                    if (pen.State == TaskPlotState.Growing)
                    {
                        float levelSpeed = 1.0f + (Level - 1) * 0.05f; // 레벨당 전 생산속도 +5%
                        float speed = (pen.HasFeed ? 2.0f : 1.0f) * herbSpeed * levelSpeed;
                        pen.ProduceProgress += (dt * speed) / Math.Max(1f, pen.ProduceDuration);
                        if (pen.ProduceProgress >= 1.0f)
                        {
                            pen.ProduceProgress = 1.0f;
                            pen.State = TaskPlotState.Ready;
                        }
                    }
                }
            }

            // 4. 일꾼 FSM (유휴 / 이동 / 작업 / 창고 복귀)
            UpdateWorkers(dt);

            // 5. 식당 시스템 (손님 도착, 식사, 결제 및 명성 획득)
            if (RestaurantOpen)
            {
                UpdateRestaurant(dt);
            }

            // 6. 플로팅 텍스트 애니메이션 갱신
            for (int i = FloatingTexts.Count - 1; i >= 0; i--)
            {
                var ft = FloatingTexts[i];
                ft.Y -= dt * 28f;
                ft.Lifetime -= dt;
                ft.Alpha = Math.Max(0f, ft.Lifetime / 1.5f);
                if (ft.Lifetime <= 0f)
                {
                    FloatingTexts.RemoveAt(i);
                }
            }

            // 7. 자동 파견 루틴 (기술 연구 [스마트 자율 파견] 완료 시에만 가동)
            if (AutoDispatch && IsTechUnlocked("tech_auto_dispatch"))
            {
                TryAutoAssignIdleWorkers();
            }
        }

        private void UpdateWorkers(float dt)
        {
            foreach (var worker in Workers)
            {
                switch (worker.State)
                {
                    case WorkerState.MovingToTask:
                        // 목표 지점으로 점진적 이동
                        worker.MoveT += dt * worker.MoveSpeed * 0.9f;
                        worker.CurrentPos = new PointF(
                            worker.HomePos.X + (worker.TargetPos.X - worker.HomePos.X) * Math.Min(1f, worker.MoveT),
                            worker.HomePos.Y + (worker.TargetPos.Y - worker.HomePos.Y) * Math.Min(1f, worker.MoveT)
                        );

                        if (worker.MoveT >= 1.0f)
                        {
                            worker.MoveT = 1.0f;
                            worker.CurrentPos = worker.TargetPos;
                            worker.State = WorkerState.Working;
                            worker.WorkTimer = 1.8f; // 작업 소요 기본 1.8초
                        }
                        break;

                    case WorkerState.Working:
                        float toolSpeed = 1.0f;
                        if (worker.Role == WorkerRole.Farmer) toolSpeed += (FarmToolLevel - 1) * 0.25f;
                        else if (worker.Role == WorkerRole.Fisherman) toolSpeed += (FishingToolLevel - 1) * 0.25f;
                        else if (worker.Role == WorkerRole.Rancher) toolSpeed += (LivestockToolLevel - 1) * 0.25f;

                        worker.WorkTimer -= dt * worker.WorkSpeedMult * toolSpeed;
                        if (worker.WorkTimer <= 0f)
                        {
                            // 작업 완료: 수확물 획득 및 복귀 시작
                            FinishTaskAndCollect(worker);
                            worker.State = WorkerState.ReturningToStorage;
                            worker.MoveT = 0f;
                        }
                        break;

                    case WorkerState.ReturningToStorage:
                        // 헛간/창고(HomePos)로 복귀 이동
                        worker.MoveT += dt * worker.MoveSpeed * 0.9f;
                        worker.CurrentPos = new PointF(
                            worker.TargetPos.X + (worker.HomePos.X - worker.TargetPos.X) * Math.Min(1f, worker.MoveT),
                            worker.TargetPos.Y + (worker.HomePos.Y - worker.TargetPos.Y) * Math.Min(1f, worker.MoveT)
                        );

                        if (worker.MoveT >= 1.0f)
                        {
                            worker.CurrentPos = worker.HomePos;
                            // 창고에 자재 입고
                            DepositCarriedGoods(worker);
                            worker.State = WorkerState.Idle;
                            worker.TargetTaskId = -1;
                            worker.CarriedItemId = null;
                            worker.CarriedCount = 0;
                        }
                        break;

                    case WorkerState.Idle:
                        // 휴식 중 (HomePos 위치 유지)
                        worker.CurrentPos = worker.HomePos;
                        break;
                }
            }
        }

        private void FinishTaskAndCollect(Worker worker)
        {
            int carry = worker.CarryCapacity;

            if (worker.Role == WorkerRole.Farmer && worker.TargetTaskId >= 0 && worker.TargetTaskId < Plots.Count)
            {
                var plot = Plots[worker.TargetTaskId];
                if (worker.TaskType == WorkerTaskType.Harvest)
                {
                    if (IsTechUnlocked("tech_bio_fertilizer") && _rnd.NextDouble() < 0.25)
                    {
                        carry += 1;
                        AddFloatingText("+보너스 수확 (+1)", plot.Bounds.X + 35, plot.Bounds.Y + 10, Color.FromArgb(255, 230, 100));
                    }
                    worker.CarriedItemId = plot.CropId;
                    worker.CarriedCount = carry;
                    // 밭 초기화 및 재성장 시작
                    plot.GrowthProgress = 0f;
                    plot.State = TaskPlotState.Growing;
                    if (AchievementSystem != null)
                    {
                        AchievementSystem.TotalHarvestCount += carry;
                        AchievementSystem.CheckAllMilestones(this);
                    }
                }
                else // WorkerTaskType.Tend (물주기 및 가꾸기 가속)
                {
                    plot.GrowthProgress = Math.Min(1.0f, plot.GrowthProgress + 0.35f);
                    if (plot.GrowthProgress >= 1.0f)
                    {
                        plot.GrowthProgress = 1.0f;
                        plot.State = TaskPlotState.Ready;
                    }
                    AddFloatingText("+물주기 가속!", plot.Bounds.X + 35, plot.Bounds.Y + 25, Color.FromArgb(120, 220, 255));
                }
                plot.AssignedWorkerId = -1;
            }
            else if (worker.Role == WorkerRole.Fisherman && worker.TargetTaskId >= 0 && worker.TargetTaskId < FishingSpots.Count)
            {
                var spot = FishingSpots[worker.TargetTaskId];
                if (worker.TaskType == WorkerTaskType.Harvest)
                {
                    if (IsTechUnlocked("tech_deep_sonar") && _rnd.NextDouble() < 0.25)
                    {
                        carry += 1;
                        AddFloatingText("+유인망 대어 (+1)", spot.Bounds.X + 35, spot.Bounds.Y + 10, Color.FromArgb(120, 220, 255));
                    }
                    else if (IsTechUnlocked("tech_sonar_lure") && _rnd.NextDouble() < 0.20)
                    {
                        carry += 1;
                        AddFloatingText("+대어 보너스 (+1)", spot.Bounds.X + 35, spot.Bounds.Y + 10, Color.FromArgb(120, 220, 255));
                    }
                    worker.CarriedItemId = spot.FishId;
                    worker.CarriedCount = carry;
                    spot.CatchProgress = 0f;
                    spot.State = TaskPlotState.Growing;
                    if (AchievementSystem != null)
                    {
                        AchievementSystem.TotalHarvestCount += carry;
                        AchievementSystem.CheckAllMilestones(this);
                    }
                }
                else
                {
                    spot.CatchProgress = Math.Min(1.0f, spot.CatchProgress + 0.35f);
                    if (spot.CatchProgress >= 1.0f)
                    {
                        spot.CatchProgress = 1.0f;
                        spot.State = TaskPlotState.Ready;
                    }
                    AddFloatingText("+미끼 투척!", spot.Bounds.X + 35, spot.Bounds.Y + 25, Color.FromArgb(120, 220, 255));
                }
                spot.AssignedWorkerId = -1;
            }
            else if (worker.Role == WorkerRole.Rancher && worker.TargetTaskId >= 0 && worker.TargetTaskId < LivestockPens.Count)
            {
                var pen = LivestockPens[worker.TargetTaskId];
                if (worker.TaskType == WorkerTaskType.Harvest)
                {
                    if (IsTechUnlocked("tech_warm_shed") && _rnd.NextDouble() < 0.25)
                    {
                        carry += 1;
                        AddFloatingText("+온열축사 보너스 (+1)", pen.Bounds.X + 35, pen.Bounds.Y + 10, Color.FromArgb(255, 215, 120));
                    }
                    else if (IsTechUnlocked("tech_herbal_feed") && _rnd.NextDouble() < 0.20)
                    {
                        carry += 1;
                        AddFloatingText("+특등품 보너스 (+1)", pen.Bounds.X + 35, pen.Bounds.Y + 10, Color.FromArgb(255, 215, 120));
                    }
                    worker.CarriedItemId = pen.ProductId;
                    worker.CarriedCount = carry;
                    pen.ProduceProgress = 0f;
                    pen.State = TaskPlotState.Growing;
                    pen.HasFeed = false;
                    if (AchievementSystem != null)
                    {
                        AchievementSystem.TotalHarvestCount += carry;
                        AchievementSystem.CheckAllMilestones(this);
                    }
                }
                else
                {
                    pen.ProduceProgress = Math.Min(1.0f, pen.ProduceProgress + 0.35f);
                    if (pen.ProduceProgress >= 1.0f)
                    {
                        pen.ProduceProgress = 1.0f;
                        pen.State = TaskPlotState.Ready;
                    }
                    AddFloatingText("+가축 돌보기!", pen.Bounds.X + 35, pen.Bounds.Y + 25, Color.FromArgb(120, 220, 255));
                }
                pen.AssignedWorkerId = -1;
            }
        }

        private void DepositCarriedGoods(Worker worker)
        {
            if (string.IsNullOrEmpty(worker.CarriedItemId) || worker.CarriedCount <= 0) return;

            int currentTotal = GetTotalStoredItemCount();
            int canStore = Math.Max(0, SiloCapacity - currentTotal);
            int toStore = Math.Min(canStore, worker.CarriedCount);
            int overflow = worker.CarriedCount - toStore;

            if (toStore > 0)
            {
                if (!Inventory.ContainsKey(worker.CarriedItemId))
                {
                    Inventory[worker.CarriedItemId] = 0;
                }
                Inventory[worker.CarriedItemId] += toStore;

                string itemName = Items.ContainsKey(worker.CarriedItemId) ? Items[worker.CarriedItemId].Name : worker.CarriedItemId;
                AddFloatingText($"+{toStore} {itemName}", worker.HomePos.X, worker.HomePos.Y - 15, Color.FromArgb(100, 255, 120));
            }

            if (overflow > 0)
            {
                // 창고 초과분은 즉시 제값에 골드로 자동 환전
                int unitPrice = Items.ContainsKey(worker.CarriedItemId) ? Items[worker.CarriedItemId].BasePrice : 6;
                int gain = overflow * unitPrice;
                Gold += gain;
                if (AchievementSystem != null)
                {
                    AchievementSystem.LifetimeGoldEarned += gain;
                    AchievementSystem.CheckAllMilestones(this);
                }
                AddFloatingText($"창고 가득 참! (+{gain}G 자동 판매)", worker.HomePos.X, worker.HomePos.Y - 30, Color.FromArgb(255, 215, 60));
            }

            ClampInventoryToCapacity();
        }

        public void DispatchAllWorkers(FarmTabType preferredTab = FarmTabType.Farm)
        {
            if (preferredTab == FarmTabType.Restaurant)
            {
                AddFloatingText("식당은 주방에서 직접 메뉴를 조리하여 운영합니다.", 325, 480, Color.FromArgb(255, 200, 100));
                return;
            }

            int dispatched = 0;
            int idleCount = 0;

            foreach (var worker in Workers)
            {
                if (worker.State == WorkerState.Idle)
                {
                    idleCount++;
                    bool success = false;
                    if (preferredTab == FarmTabType.Farm)
                    {
                        success = TryAssignFarmTask(worker);
                    }
                    else if (preferredTab == FarmTabType.Fishing)
                    {
                        success = TryAssignFishingTask(worker);
                    }
                    else if (preferredTab == FarmTabType.Livestock)
                    {
                        success = TryAssignLivestockTask(worker);
                    }

                    if (success)
                    {
                        dispatched++;
                    }
                }
            }

            if (dispatched > 0)
            {
                string tabName = "일터";
                if (preferredTab == FarmTabType.Farm) tabName = "밭 (농부)";
                else if (preferredTab == FarmTabType.Fishing) tabName = "낚시터 (어부)";
                else if (preferredTab == FarmTabType.Livestock) tabName = "목장 (목동)";
                AddFloatingText($"▶ {tabName} {dispatched}명 출동!", 325, 480, Color.FromArgb(100, 255, 150));
            }
            else if (idleCount == 0)
            {
                AddFloatingText("대기 중인 유휴 일꾼이 없습니다!", 325, 480, Color.FromArgb(200, 200, 200));
            }
            else
            {
                string tabName = preferredTab == FarmTabType.Farm ? "밭" : (preferredTab == FarmTabType.Fishing ? "낚시터" : "목장");
                AddFloatingText($"현재 {tabName}에 수확/생산 완료된 작업이 없습니다.", 325, 480, Color.FromArgb(255, 180, 100));
            }
        }

        private void TryAutoAssignIdleWorkers()
        {
            foreach (var worker in Workers)
            {
                if (worker.State == WorkerState.Idle)
                {
                    if (TryAssignFarmTask(worker)) continue;
                    if (TryAssignFishingTask(worker)) continue;
                    if (TryAssignLivestockTask(worker)) continue;
                }
            }
        }

        private bool AssignWorkerToBestTask(Worker worker, FarmTabType preferredTab)
        {
            if (preferredTab == FarmTabType.Fishing)
            {
                if (TryAssignFishingTask(worker)) return true;
                if (TryAssignFarmTask(worker)) return true;
                if (TryAssignLivestockTask(worker)) return true;
            }
            else if (preferredTab == FarmTabType.Livestock)
            {
                if (TryAssignLivestockTask(worker)) return true;
                if (TryAssignFarmTask(worker)) return true;
                if (TryAssignFishingTask(worker)) return true;
            }
            else
            {
                if (TryAssignFarmTask(worker)) return true;
                if (TryAssignFishingTask(worker)) return true;
                if (TryAssignLivestockTask(worker)) return true;
            }

            return false;
        }

        private bool TryAssignFarmTask(Worker worker)
        {
            // 수확 준비 완료된 밭에만 파견 (성장 중일 때는 출발하지 않음)
            for (int i = 0; i < Plots.Count; i++)
            {
                var plot = Plots[i];
                if (plot.IsUnlocked && plot.State == TaskPlotState.Ready && plot.AssignedWorkerId == -1)
                {
                    SendWorkerToTask(worker, WorkerRole.Farmer, WorkerTaskType.Harvest, i, new PointF(plot.Bounds.X + plot.Bounds.Width / 2, plot.Bounds.Y + plot.Bounds.Height / 2));
                    plot.AssignedWorkerId = worker.WorkerId;
                    return true;
                }
            }
            return false;
        }

        private bool TryAssignFishingTask(Worker worker)
        {
            // 어획 준비 완료된 스팟에만 파견 (진행 중일 때는 출발하지 않음)
            for (int i = 0; i < FishingSpots.Count; i++)
            {
                var spot = FishingSpots[i];
                if (spot.IsUnlocked && spot.State == TaskPlotState.Ready && spot.AssignedWorkerId == -1)
                {
                    SendWorkerToTask(worker, WorkerRole.Fisherman, WorkerTaskType.Harvest, i, new PointF(spot.Bounds.X + spot.Bounds.Width / 2, spot.Bounds.Y + spot.Bounds.Height / 2));
                    spot.AssignedWorkerId = worker.WorkerId;
                    return true;
                }
            }
            return false;
        }

        private bool TryAssignLivestockTask(Worker worker)
        {
            // 채집 준비 완료된 축사에만 파견 (생산 중일 때는 출발하지 않음)
            for (int i = 0; i < LivestockPens.Count; i++)
            {
                var pen = LivestockPens[i];
                if (pen.IsUnlocked && pen.State == TaskPlotState.Ready && pen.AssignedWorkerId == -1)
                {
                    SendWorkerToTask(worker, WorkerRole.Rancher, WorkerTaskType.Harvest, i, new PointF(pen.Bounds.X + pen.Bounds.Width / 2, pen.Bounds.Y + pen.Bounds.Height / 2));
                    pen.AssignedWorkerId = worker.WorkerId;
                    return true;
                }
            }
            return false;
        }

        private void SendWorkerToTask(Worker worker, WorkerRole role, WorkerTaskType taskType, int taskId, PointF targetPos)
        {
            worker.Role = role;
            worker.TaskType = taskType;
            worker.TargetTaskId = taskId;
            worker.TargetPos = targetPos;
            worker.MoveT = 0f;
            worker.State = WorkerState.MovingToTask;
        }

        private void UpdateRestaurant(float dt)
        {
            RestaurantSpawnTimer += dt;
            // 3초마다 빈 테이블 확인 및 손님 맞이
            if (RestaurantSpawnTimer >= 3.0f)
            {
                RestaurantSpawnTimer = 0f;
                if (ActiveCustomers.Count < 3)
                {
                    TrySeatNewCustomer();
                }
            }

            // 손님 식사 진행
            for (int i = ActiveCustomers.Count - 1; i >= 0; i--)
            {
                var cust = ActiveCustomers[i];
                cust.EatProgress += dt / Math.Max(1f, cust.EatDuration);
                if (cust.EatProgress >= 1.0f)
                {
                    // 결제 및 보상 획득
                    Gold += cust.TotalPaid;
                    Fame += cust.FameAwarded;

                    if (AchievementSystem != null)
                    {
                        AchievementSystem.LifetimeGoldEarned += cust.TotalPaid;
                        AchievementSystem.TotalMealsServed++;
                        if (cust.IsVip)
                        {
                            AchievementSystem.TotalVipServedCount++;
                        }
                        AchievementSystem.CheckAllMilestones(this);
                    }

                    string finishMsg = cust.IsVip
                        ? $"+{cust.TotalPaid}G [VIP 평론가 극찬! 명성+{cust.FameAwarded}]"
                        : $"+{cust.TotalPaid}G [식사정산]";
                    AddFloatingText(finishMsg, 320 + cust.TableIndex * 90, 260, cust.IsVip ? Color.Gold : Color.FromArgb(255, 220, 80));
                    ActiveCustomers.RemoveAt(i);
                    CheckLevelUp(); // 식당 명성 획득 후 레벨업 자동 검사!
                }
            }
        }

        private void TrySeatNewCustomer()
        {
            // 사용 가능한 레시피 탐색
            List<RecipeDef> availableCookable = new List<RecipeDef>();
            foreach (var r in Recipes)
            {
                if (r.IsUnlocked && CanCookRecipe(r))
                {
                    availableCookable.Add(r);
                }
            }

            if (availableCookable.Count == 0) return;

            // 랜덤 레시피 주문 및 식재료 즉시 소비
            var chosen = availableCookable[_rnd.Next(availableCookable.Count)];
            ConsumeRecipeIngredients(chosen);

            int tableIdx = GetFirstFreeTableIndex();
            if (tableIdx < 0) return;

            // 요리 숙련도 집계 및 레벨업 체크
            int prevMastery = chosen.MasteryLevel;
            chosen.CookCount++;
            int newMastery = chosen.MasteryLevel;
            if (newMastery > prevMastery)
            {
                AddFloatingText($"★ [{chosen.Name}] 숙련도 상승! ({chosen.MasteryStars})", 325, 250, Color.Gold);
            }

            float masteryGoldMult = newMastery == 3 ? 1.30f : (newMastery == 2 ? 1.15f : 1.0f);
            float masteryFameMult = newMastery == 3 ? 1.40f : (newMastery == 2 ? 1.20f : 1.0f);
            bool isDoublePay = (newMastery == 3 && _rnd.NextDouble() < 0.30);

            float weatherTipMult = WeatherSystem != null ? WeatherSystem.TipMultiplier : 1.0f;
            float weatherFameMult = WeatherSystem != null ? WeatherSystem.FameMultiplier : 1.0f;

            int tip = (int)(_rnd.Next(10, 35) * weatherTipMult);
            int basePaid = (int)((chosen.SellingPrice + tip) * masteryGoldMult * (isDoublePay ? 2.0f : 1.0f));
            int baseFame = (int)(chosen.FameGain * masteryFameMult * weatherFameMult);

            // VIP 미식 평론가 방문 확률 (명성 50P 이상 시 15% 확률)
            bool isVip = (Fame >= 50 && _rnd.NextDouble() < 0.15);
            if (isVip)
            {
                basePaid += 250;
                baseFame += 60;
            }

            if (IsTechUnlocked("tech_gourmet_review"))
            {
                basePaid = (int)(basePaid * 1.20f);
                baseFame = (int)(baseFame * 1.30f);
            }

            ActiveCustomers.Add(new RestaurantCustomer
            {
                TableIndex = tableIdx,
                CustomerName = isVip ? "특급 미식 평론가" : $"단골 손님 #{_rnd.Next(10, 99)}",
                OrderedRecipe = chosen,
                EatDuration = Math.Max(4.0f, chosen.CookTimeSec * 0.75f) * (isVip ? 1.25f : 1.0f),
                EatProgress = 0f,
                State = CustomerState.Eating,
                TotalPaid = basePaid,
                FameAwarded = baseFame,
                IsVip = isVip,
                VipTitle = isVip ? "[VIP 미식 평론가]" : ""
            });

            if (isVip)
            {
                AddFloatingText("★ VIP 미식 평론가 도착! (골드/명성 보너스)", 320 + tableIdx * 90, 230, Color.Gold);
            }
        }

        private int GetFirstFreeTableIndex()
        {
            bool[] occupied = new bool[3];
            foreach (var c in ActiveCustomers)
            {
                if (c.TableIndex >= 0 && c.TableIndex < 3)
                {
                    occupied[c.TableIndex] = true;
                }
            }
            for (int i = 0; i < 3; i++)
            {
                if (!occupied[i]) return i;
            }
            return -1;
        }

        public bool CanCookRecipe(RecipeDef recipe)
        {
            foreach (var kvp in recipe.Ingredients)
            {
                if (!Inventory.ContainsKey(kvp.Key) || Inventory[kvp.Key] < kvp.Value)
                {
                    return false;
                }
            }
            return true;
        }

        private void ConsumeRecipeIngredients(RecipeDef recipe)
        {
            foreach (var kvp in recipe.Ingredients)
            {
                Inventory[kvp.Key] -= kvp.Value;
            }
        }

        public void SellAllProduce(bool safeMode = true)
        {
            int gainedGold = 0;
            int soldItems = 0;
            List<string> keys = new List<string>(Inventory.Keys);

            // 스마트 안전 출하: 해금된 레시피 식재료(최소 2개) + 진행 중인 품종개량 목표치 비축 보호!
            Dictionary<string, int> reservedSafeStock = new Dictionary<string, int>();
            if (safeMode)
            {
                // 1. 요리 레시피 필수 식재료 최소 2개 보존
                foreach (var r in Recipes)
                {
                    if (r.IsUnlocked)
                    {
                        foreach (var ing in r.Ingredients)
                        {
                            reservedSafeStock[ing.Key] = Math.Max(reservedSafeStock.ContainsKey(ing.Key) ? reservedSafeStock[ing.Key] : 0, 2);
                        }
                    }
                }

                // 2. 미완료 품종 개량 연구 목표 작물 비축량 보호 (자원 낭비 및 노가다 초기화 방지)
                foreach (var b in Breedings)
                {
                    if (!b.IsResearched)
                    {
                        int req = b.RequiredStockpile;
                        reservedSafeStock[b.BaseCropId] = Math.Max(reservedSafeStock.ContainsKey(b.BaseCropId) ? reservedSafeStock[b.BaseCropId] : 0, req);
                    }
                }
            }

            foreach (var id in keys)
            {
                if (Items.ContainsKey(id))
                {
                    var item = Items[id];
                    // 요리를 제외한 농작물, 생선, 축산품 출하
                    if (item.Category != ItemCategory.Dish && Inventory[id] > 0)
                    {
                        int currentCount = Inventory[id];
                        int keep = reservedSafeStock.ContainsKey(id) ? reservedSafeStock[id] : 0;
                        int sellable = Math.Max(0, currentCount - keep);
                        if (sellable <= 0) continue;

                        int price = item.BasePrice;
                        if (IsTechUnlocked("tech_direct_trade"))
                        {
                            price = (int)(price * 1.15f);
                        }

                        gainedGold += sellable * price;
                        soldItems += sellable;
                        Inventory[id] -= sellable;
                    }
                }
            }

            if (gainedGold > 0)
            {
                Gold += gainedGold;
                if (AchievementSystem != null)
                {
                    AchievementSystem.LifetimeGoldEarned += gainedGold;
                    AchievementSystem.CheckAllMilestones(this);
                }
                string msg = safeMode
                    ? $"+{gainedGold:N0}G 안전 출하 완료! ({soldItems}개 판매, 요리/개량재료 보존)"
                    : $"+{gainedGold:N0}G 전량 출하 완료! ({soldItems}개 판매)";
                AddFloatingText(msg, 325, 300, Color.FromArgb(255, 235, 80));
                Save();
            }
            else
            {
                AddFloatingText("출하할 수 있는 잉여 식자재가 없습니다. (요리 및 품종개량 재료 안전 보관 중)", 325, 300, Color.FromArgb(200, 200, 200));
            }
        }

        public bool PerformBreeding(int breedingIndex)
        {
            if (breedingIndex < 0 || breedingIndex >= Breedings.Count) return false;
            var breed = Breedings[breedingIndex];
            if (breed.IsResearched) return false;

            if (Gold < breed.ResearchCostGold)
            {
                AddFloatingText("골드가 부족합니다!", 325, 300, Color.FromArgb(255, 100, 100));
                return false;
            }

            int currentStock = Inventory.ContainsKey(breed.BaseCropId) ? Inventory[breed.BaseCropId] : 0;
            if (currentStock < breed.RequiredStockpile)
            {
                AddFloatingText($"작물 축적량이 부족합니다! ({currentStock}/{breed.RequiredStockpile})", 325, 300, Color.FromArgb(255, 150, 100));
                return false;
            }

            // 연구 성공!
            Gold -= breed.ResearchCostGold;
            Inventory[breed.BaseCropId] -= breed.RequiredStockpile;
            breed.IsResearched = true;
            if (AchievementSystem != null)
            {
                AchievementSystem.TotalBreedingCount++;
                AchievementSystem.CheckAllMilestones(this);
            }

            // 밭에 개량된 신품종을 1곳에 시범 적용 (나머지는 클릭으로 자유 전환하여 하위 작물 보존!)
            bool applied = false;
            foreach (var p in Plots)
            {
                if (p.IsUnlocked && p.CropId == breed.BaseCropId && !applied)
                {
                    p.CropId = breed.EvolvedCropId;
                    p.GrowthDuration = Math.Max(8f, p.GrowthDuration * 1.25f);
                    p.GrowthProgress = 0f;
                    applied = true;
                }
            }

            string evolvedName = Items.ContainsKey(breed.EvolvedCropId) ? Items[breed.EvolvedCropId].Name : breed.EvolvedCropId;
            AddFloatingText($"★ 신품종 [{evolvedName}] 개량 성공! (밭 클릭 시 작물 전환 가능)", 325, 280, Color.FromArgb(255, 215, 0));
            Fame += 50;
            CheckLevelUp(); // 명성 획득 후 레벨업 검사
            Save();
            return true;
        }

        // --- 업그레이드 메서드 ---
        // --- 업그레이드 메서드 ---
        public bool UpgradeWorkerCount()
        {
            if (Workers.Count >= 8)
            {
                AddFloatingText("일꾼 최대 정원(8명)에 도달했습니다!", 325, 280, Color.FromArgb(255, 200, 100));
                return false;
            }
            int cost = 60 * WorkerCountUpgradeLevel;
            if (Gold < cost) return false;

            Gold -= cost;
            WorkerCountUpgradeLevel++;
            int newId = Workers.Count + 1;
            var w = new Worker(newId, $"일꾼 #{newId}", WorkerRole.Farmer, new PointF(60 + Workers.Count * 22, 490))
            {
                CarryCapacity = 1 + (CartCapacityUpgradeLevel - 1),
                MoveSpeed = 0.85f + (CartCapacityUpgradeLevel - 1) * 0.2f
            };
            Workers.Add(w);
            AddFloatingText("★ 새 일꾼 고용 완료!", 325, 280, Color.FromArgb(100, 255, 150));
            Save();
            return true;
        }

        public bool UpgradeFarmTool()
        {
            if (FarmToolLevel >= 5)
            {
                AddFloatingText("농업 도구 최고 레벨에 도달했습니다!", 325, 280, Color.FromArgb(255, 200, 100));
                return false;
            }
            int cost = 70 * FarmToolLevel;
            if (Gold < cost) return false;

            Gold -= cost;
            FarmToolLevel++;
            string toolName = GetFarmToolName(FarmToolLevel);
            AddFloatingText($"★ 농업 도구 강화! [{toolName}] (작업속도 +25%)", 325, 280, Color.FromArgb(120, 220, 255));
            Save();
            return true;
        }

        public bool UpgradeFishingTool()
        {
            if (FishingToolLevel >= 5)
            {
                AddFloatingText("어업 도구 최고 레벨에 도달했습니다!", 325, 280, Color.FromArgb(255, 200, 100));
                return false;
            }
            int cost = 85 * FishingToolLevel;
            if (Gold < cost) return false;

            Gold -= cost;
            FishingToolLevel++;
            string toolName = GetFishingToolName(FishingToolLevel);
            AddFloatingText($"★ 어업 도구 강화! [{toolName}] (작업속도 +25%)", 325, 280, Color.FromArgb(120, 220, 255));
            Save();
            return true;
        }

        public bool UpgradeLivestockTool()
        {
            if (LivestockToolLevel >= 5)
            {
                AddFloatingText("목축 도구 최고 레벨에 도달했습니다!", 325, 280, Color.FromArgb(255, 200, 100));
                return false;
            }
            int cost = 95 * LivestockToolLevel;
            if (Gold < cost) return false;

            Gold -= cost;
            LivestockToolLevel++;
            string toolName = GetLivestockToolName(LivestockToolLevel);
            AddFloatingText($"★ 목축 도구 강화! [{toolName}] (작업속도 +25%)", 325, 280, Color.FromArgb(120, 220, 255));
            Save();
            return true;
        }

        public bool UpgradeToolSpeed()
        {
            return UpgradeFarmTool();
        }

        public bool UpgradeCartCapacity()
        {
            if (CartCapacityUpgradeLevel >= 5)
            {
                AddFloatingText("손수레 최고 레벨에 도달했습니다!", 325, 280, Color.FromArgb(255, 200, 100));
                return false;
            }
            int cost = 100 * CartCapacityUpgradeLevel;
            if (Gold < cost) return false;

            Gold -= cost;
            CartCapacityUpgradeLevel++;
            foreach (var w in Workers)
            {
                w.CarryCapacity += 1;
                w.MoveSpeed += 0.2f;
            }
            AddFloatingText("◆ 손수레 개량 완료! (적재량 +1, 이동속도 +20%)", 325, 280, Color.FromArgb(255, 200, 100));
            Save();
            return true;
        }

        public bool UpgradeSilo()
        {
            if (SiloUpgradeLevel >= 10)
            {
                AddFloatingText("창고 최대 증축(600칸)에 도달했습니다!", 325, 280, Color.FromArgb(255, 200, 100));
                return false;
            }
            int cost = 90 * SiloUpgradeLevel;
            if (Gold < cost) return false;

            Gold -= cost;
            SiloUpgradeLevel++;
            SiloCapacity += 60;
            AddFloatingText($"■ 창고 증축 완료! (최대 {SiloCapacity}개 보관)", 325, 280, Color.FromArgb(200, 240, 120));
            Save();
            return true;
        }

        public bool ResearchTech(string techId)
        {
            var node = TechTree.Find(t => t.Id == techId);
            if (node == null || node.IsUnlocked) return false;

            if (!string.IsNullOrEmpty(node.RequiredParentId))
            {
                if (!IsTechUnlocked(node.RequiredParentId))
                {
                    AddFloatingText("선행 기술 연구가 필요합니다!", 325, 300, Color.FromArgb(255, 120, 100));
                    return false;
                }
            }

            if (Gold < node.CostGold)
            {
                AddFloatingText("골드가 부족합니다!", 325, 300, Color.FromArgb(255, 120, 100));
                return false;
            }

            if (Fame < node.CostFame)
            {
                AddFloatingText($"식당 명성이 부족합니다! ({Fame}/{node.CostFame}P - 식당 손님 영업으로 획득)", 325, 300, Color.FromArgb(255, 150, 100));
                return false;
            }

            Gold -= node.CostGold;
            Fame = Math.Max(0, Fame - node.CostFame);
            node.IsUnlocked = true;

            AddFloatingText($"★ 기술 연구 완료: [{node.Name}]!", 325, 270, Color.FromArgb(120, 255, 200));
            if (AchievementSystem != null)
            {
                AchievementSystem.CheckAllMilestones(this);
            }
            Save();
            return true;
        }

        public bool FulfillContract(string contractId)
        {
            if (ContractSystem == null) return false;
            var contract = ContractSystem.DailyContracts.Find(c => c.Id == contractId);
            if (contract == null || contract.IsCompleted) return false;

            if (!Inventory.ContainsKey(contract.RequiredItemId) || Inventory[contract.RequiredItemId] < contract.RequiredAmount)
            {
                AddFloatingText("납품에 필요한 식자재가 부족합니다!", 325, 280, Color.FromArgb(255, 120, 100));
                return false;
            }

            Inventory[contract.RequiredItemId] -= contract.RequiredAmount;
            contract.IsCompleted = true;
            Gold += contract.RewardGold;
            Fame += contract.RewardFame;
            if (AchievementSystem != null)
            {
                AchievementSystem.LifetimeGoldEarned += contract.RewardGold;
                AchievementSystem.CheckAllMilestones(this);
            }

            AddFloatingText($"★ 납품 성공! (+{contract.RewardGold}G, +{contract.RewardFame}P)", 325, 260, Color.FromArgb(100, 255, 150));

            // 모든 3개 의뢰 완료 검사
            bool allDone = true;
            foreach (var c in ContractSystem.DailyContracts)
            {
                if (!c.IsCompleted) { allDone = false; break; }
            }

            if (allDone && !ContractSystem.DailyBonusClaimed)
            {
                ContractSystem.DailyBonusClaimed = true;
                Gold += 500;
                Fame += 50;
                if (AchievementSystem != null)
                {
                    AchievementSystem.LifetimeGoldEarned += 500;
                    AchievementSystem.CheckAllMilestones(this);
                }
                AddFloatingText("★ 일일 의뢰 ALL CLEAR 보너스! (+500G, +50P)", 325, 230, Color.Gold);
            }

            CheckLevelUp();
            Save();
            return true;
        }

        public bool ClaimAchievementReward(string achievementId)
        {
            if (AchievementSystem == null) return false;
            var ach = AchievementSystem.Achievements.Find(a => a.Id == achievementId);
            if (ach == null || !ach.IsCompleted || ach.IsClaimed) return false;

            ach.IsClaimed = true;
            Gold += ach.RewardGold;
            Fame += ach.RewardFame;
            if (AchievementSystem != null)
            {
                AchievementSystem.LifetimeGoldEarned += ach.RewardGold;
                AchievementSystem.CheckAllMilestones(this);
            }

            AddFloatingText($"★ 업적 달성 보상 획득! (+{ach.RewardGold}G, +{ach.RewardFame}P)", 325, 260, Color.FromArgb(255, 220, 80));
            CheckLevelUp();
            Save();
            return true;
        }

        public void CalculateOfflineProgress(double elapsedSeconds)
        {
            if (elapsedSeconds < 5.0) return;
            // 연구된 보존 기술에 따라 최대 4시간 ~ 10시간까지 보존
            double maxSec = GetMaxOfflineSeconds();
            double validSec = Math.Min(maxSec, elapsedSeconds);

            int totalHarvested = 0;
            int totalAutoSoldGold = 0;

            // 1. 밭 작물 생산
            foreach (var p in Plots)
            {
                if (p.IsUnlocked)
                {
                    int cycles = (int)(validSec / Math.Max(6f, p.GrowthDuration * 2.0f));
                    if (cycles > 0)
                    {
                        int currentTotal = GetTotalStoredItemCount();
                        int space = Math.Max(0, SiloCapacity - currentTotal);
                        int toStore = Math.Min(space, cycles);
                        int overflow = cycles - toStore;

                        if (toStore > 0)
                        {
                            if (!Inventory.ContainsKey(p.CropId)) Inventory[p.CropId] = 0;
                            Inventory[p.CropId] += toStore;
                            totalHarvested += toStore;
                        }

                        if (overflow > 0)
                        {
                            int price = Items.ContainsKey(p.CropId) ? Items[p.CropId].BasePrice : 6;
                            int gain = overflow * price;
                            Gold += gain;
                            totalAutoSoldGold += gain;
                        }
                    }
                }
            }

            // 2. 낚시터 어획 생산
            foreach (var s in FishingSpots)
            {
                if (s.IsUnlocked)
                {
                    int cycles = (int)(validSec / Math.Max(12f, s.CatchDuration * 2.5f));
                    if (cycles > 0)
                    {
                        int currentTotal = GetTotalStoredItemCount();
                        int space = Math.Max(0, SiloCapacity - currentTotal);
                        int toStore = Math.Min(space, cycles);
                        int overflow = cycles - toStore;

                        if (toStore > 0)
                        {
                            if (!Inventory.ContainsKey(s.FishId)) Inventory[s.FishId] = 0;
                            Inventory[s.FishId] += toStore;
                            totalHarvested += toStore;
                        }

                        if (overflow > 0)
                        {
                            int price = Items.ContainsKey(s.FishId) ? Items[s.FishId].BasePrice : 35;
                            int gain = overflow * price;
                            Gold += gain;
                            totalAutoSoldGold += gain;
                        }
                    }
                }
            }

            // 3. 목장 축산품 생산
            foreach (var pen in LivestockPens)
            {
                if (pen.IsUnlocked)
                {
                    int cycles = (int)(validSec / Math.Max(14f, pen.ProduceDuration * 2.5f));
                    if (cycles > 0)
                    {
                        int currentTotal = GetTotalStoredItemCount();
                        int space = Math.Max(0, SiloCapacity - currentTotal);
                        int toStore = Math.Min(space, cycles);
                        int overflow = cycles - toStore;

                        if (toStore > 0)
                        {
                            if (!Inventory.ContainsKey(pen.ProductId)) Inventory[pen.ProductId] = 0;
                            Inventory[pen.ProductId] += toStore;
                            totalHarvested += toStore;
                        }

                        if (overflow > 0)
                        {
                            int price = Items.ContainsKey(pen.ProductId) ? Items[pen.ProductId].BasePrice : 30;
                            int gain = overflow * price;
                            Gold += gain;
                            totalAutoSoldGold += gain;
                        }
                    }
                }
            }

            // 4. 식당 오프라인 영업 (영업 개시 상태인 경우 수확된 재료로 손님 서빙 정산)
            int offlineMealsServed = 0;
            int offlineDiningGold = 0;
            int offlineDiningFame = 0;

            if (RestaurantOpen)
            {
                // 약 25초당 1명의 손님이 조리 가능한 요리를 식사
                int maxPotentialCustomers = (int)(validSec / 25.0);
                for (int m = 0; m < maxPotentialCustomers; m++)
                {
                    List<RecipeDef> cookable = new List<RecipeDef>();
                    foreach (var r in Recipes)
                    {
                        if (r.IsUnlocked && CanCookRecipe(r)) cookable.Add(r);
                    }
                    if (cookable.Count == 0) break; // 재료 소진

                    var chosen = cookable[_rnd.Next(cookable.Count)];
                    ConsumeRecipeIngredients(chosen);

                    int paid = chosen.SellingPrice + 20;
                    int fame = chosen.FameGain;
                    if (IsTechUnlocked("tech_gourmet_review"))
                    {
                        paid = (int)(paid * 1.20f);
                        fame = (int)(fame * 1.30f);
                    }

                    Gold += paid;
                    Fame += fame;
                    offlineDiningGold += paid;
                    offlineDiningFame += fame;
                    offlineMealsServed++;
                }

                if (offlineMealsServed > 0)
                {
                    CheckLevelUp();
                }
            }

            ClampInventoryToCapacity();

            if (totalHarvested > 0 || totalAutoSoldGold > 0 || offlineMealsServed > 0)
            {
                string msg;
                if (totalHarvested == 0 && totalAutoSoldGold > 0)
                {
                    msg = $"[오프라인 복귀] 창고 가득 참 (+{totalAutoSoldGold:N0}G 자동판매)";
                }
                else
                {
                    msg = $"[오프라인 복귀] {totalHarvested}개 수확";
                    if (totalAutoSoldGold > 0)
                    {
                        msg += $" (+{totalAutoSoldGold:N0}G 초과판매)";
                    }
                }

                if (offlineMealsServed > 0)
                {
                    msg += $" / 식당 {offlineMealsServed}명 서빙 (+{offlineDiningGold:N0}G, +{offlineDiningFame}P)";
                }

                AddFloatingText(msg, 325, 270, Color.FromArgb(120, 255, 200));
            }
        }
    }
}
