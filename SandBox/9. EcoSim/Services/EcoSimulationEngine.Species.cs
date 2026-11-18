using System;
using System.Collections.Generic;
using GateHelper.EcoSim.Models;

namespace GateHelper.EcoSim.Services
{
    public sealed partial class EcoSimulationEngine
    {
        private void StepPrey(Organism prey, float mutationRate)
        {
            // 1. 시야 내 가장 가까운 포식자 탐색 (도주 우선)
            float visionSq = prey.Dna.Vision * prey.Dna.Vision;
            _organismGrid.QueryRadius(prey.X, prey.Y, prey.Dna.Vision, _queryBuffer);

            Organism nearestPredator = null;
            float nearestPredDistSq = visionSq;

            for (int i = 0; i < _queryBuffer.Count; i++)
            {
                var other = _organisms[_queryBuffer[i]];
                if (!other.IsAlive || other.Species == EcoSpecies.Prey) continue;
                float dx = other.X - prey.X;
                float dy = other.Y - prey.Y;
                float d2 = dx * dx + dy * dy;
                if (d2 < nearestPredDistSq)
                {
                    nearestPredDistSq = d2;
                    nearestPredator = other;
                }
            }

            if (nearestPredator != null)
            {
                prey.EatingTicks = 0;
                SteerAway(prey, nearestPredator.X, nearestPredator.Y, 1.12f);
                // 위협을 피해 도망칠 때 잠시 탐색 모드로 전환해 같은 자리로 되돌아오는 핑퐁 방지
                if (prey.EscapeTicks < 18) prey.EscapeTicks = 18;
            }
            else if (prey.EatingTicks > 0)
            {
                // 섭취 중 잠시 멈춤 (위협이 오면 즉시 중단됨)
                prey.Vx *= 0.25f;
                prey.Vy *= 0.25f;
            }
            else if (prey.EscapeTicks > 0)
            {
                Wander(prey);
            }
            else
            {
                // 45틱마다 제자리 맴돌기(핑퐁) 감지 -> 강제 탐색 이탈
                if ((prey.AgeTicks % 45) == 0)
                {
                    float dxA = prey.X - prey.AnchorX;
                    float dyA = prey.Y - prey.AnchorY;
                    if (dxA * dxA + dyA * dyA < 26f * 26f)
                    {
                        prey.EscapeTicks = 35;
                    }
                    prey.AnchorX = prey.X;
                    prey.AnchorY = prey.Y;
                }

                // 2. 시야 내 가장 가까운 '안전한' 먹이 탐색 (모닥불/위협 근처 먹이 제외)
                _foodGrid.QueryRadius(prey.X, prey.Y, prey.Dna.Vision, _queryBuffer);
                int nearestFoodIdx = -1;
                float nearestFoodDistSq = visionSq;

                for (int i = 0; i < _queryBuffer.Count; i++)
                {
                    int fIdx = _queryBuffer[i];
                    if (fIdx >= _foods.Count) continue;
                    var f = _foods[fIdx];
                    if (f.EnergyValue <= 0f) continue;
                    if (FindNearestCampfire(f.X, f.Y, 64f) >= 0) continue;

                    float dx = f.X - prey.X;
                    float dy = f.Y - prey.Y;
                    float d2 = dx * dx + dy * dy;
                    if (d2 < nearestFoodDistSq)
                    {
                        nearestFoodDistSq = d2;
                        nearestFoodIdx = fIdx;
                    }
                }

                if (nearestFoodIdx >= 0)
                {
                    var targetFood = _foods[nearestFoodIdx];
                    SteerToward(prey, targetFood.X, targetFood.Y, 1.0f);

                    float eatRadius = prey.Dna.Size + 5f;
                    if (nearestFoodDistSq <= eatRadius * eatRadius)
                    {
                        prey.Energy = Math.Min(prey.MaxEnergy, prey.Energy + targetFood.EnergyValue);
                        prey.EatingTicks = 10;
                        RemoveFoodSwapPop(nearestFoodIdx);
                    }
                }
                else
                {
                    Wander(prey);
                }
            }

            MoveAndBounce(prey);
            float seasonCostMult = GetSeasonMetabolicMultiplier();
            prey.Energy -= prey.Dna.ComputeMetabolicCost(EcoSpecies.Prey) * seasonCostMult;

            if (prey.Energy <= 0f)
            {
                prey.IsAlive = false;
                return;
            }

            if (prey.Energy >= _config.PreyReproduceThreshold &&
                PreyCount < (int)(_config.MaxOrganismCount * 0.55f) &&
                (_organisms.Count + _pendingOffspring.Count) < _config.MaxOrganismCount)
            {
                Reproduce(prey, mutationRate);
            }
        }

        private void StepPredator(Organism pred, float mutationRate)
        {
            float visionSq = pred.Dna.Vision * pred.Dna.Vision;
            _organismGrid.QueryRadius(pred.X, pred.Y, pred.Dna.Vision, _queryBuffer);

            Organism nearestThreat = null;
            float nearestThreatDistSq = visionSq;
            Organism nearestPrey = null;
            float nearestPreyDistSq = visionSq;

            for (int i = 0; i < _queryBuffer.Count; i++)
            {
                var other = _organisms[_queryBuffer[i]];
                if (!other.IsAlive) continue;

                float dx = other.X - pred.X;
                float dy = other.Y - pred.Y;
                float d2 = dx * dx + dy * dy;

                if (other.Species == EcoSpecies.Bear || other.Species == EcoSpecies.Human)
                {
                    if (!other.IsHibernating && d2 < nearestThreatDistSq)
                    {
                        nearestThreatDistSq = d2;
                        nearestThreat = other;
                    }
                    continue;
                }

                if (other.Species != EcoSpecies.Prey) continue;

                float effectiveVisionSq = nearestPreyDistSq;
                if (EnableShelters && IsInsideShelter(other.X, other.Y))
                {
                    float stealthR = pred.Dna.Vision * 0.42f;
                    float stealthSq = stealthR * stealthR;
                    if (stealthSq < effectiveVisionSq) effectiveVisionSq = stealthSq;
                }

                if (d2 < effectiveVisionSq)
                {
                    nearestPreyDistSq = d2;
                    nearestPrey = other;
                }
            }

            bool isHungry = pred.Energy < pred.MaxEnergy * 0.78f;
            if (nearestThreat != null)
            {
                pred.EatingTicks = 0;
                SteerAway(pred, nearestThreat.X, nearestThreat.Y, 1.10f);
            }
            else if (pred.EatingTicks > 0)
            {
                pred.Vx *= 0.25f;
                pred.Vy *= 0.25f;
            }
            else if (nearestPrey != null && isHungry)
            {
                SteerToward(pred, nearestPrey.X, nearestPrey.Y, 1.05f);
                float catchR = pred.Dna.Size + nearestPrey.Dna.Size * 0.7f;
                if (nearestPreyDistSq <= catchR * catchR)
                {
                    nearestPrey.IsAlive = false;
                    pred.Kills++;
                    pred.EatingTicks = 28;
                    pred.Energy = Math.Min(pred.MaxEnergy, pred.Energy + _config.PreyEnergyValue + nearestPrey.Dna.Size * 2f);
                }
            }
            else
            {
                Wander(pred);
            }

            ApplySameSpeciesSeparation(pred, 22f);
            MoveAndBounce(pred);
            float restMult = (pred.EatingTicks > 0 || (!isHungry && nearestThreat == null)) ? 0.62f : 1.0f;
            float seasonCostMult = GetSeasonMetabolicMultiplier() * restMult;
            pred.Energy -= pred.Dna.ComputeMetabolicCost(EcoSpecies.Predator) * seasonCostMult;

            if (pred.Energy <= 0f)
            {
                pred.IsAlive = false;
                return;
            }

            if (pred.Energy >= _config.PredatorReproduceThreshold &&
                PredatorCount < (int)(_config.MaxOrganismCount * 0.25f) &&
                (_organisms.Count + _pendingOffspring.Count) < _config.MaxOrganismCount)
            {
                Reproduce(pred, mutationRate);
            }
        }

        private static void WakeBearWithCharge(Organism bear)
        {
            if (!bear.IsHibernating) return;
            bear.IsHibernating = false;
            bear.SkillActiveTicks = 45;
            bear.AttackCooldown = 260;
        }

        private void StepBear(Organism bear, float mutationRate)
        {
            if (EnableSeasons && CurrentSeason == EcoSeason.Winter && bear.SkillActiveTicks == 0)
            {
                bear.IsHibernating = true;
                bear.Energy -= bear.Dna.ComputeMetabolicCost(EcoSpecies.Bear) * 0.14f;
                if (bear.Energy <= 0f) bear.IsAlive = false;
                return;
            }
            bear.IsHibernating = false;

            float effectiveVision = bear.SkillActiveTicks > 0 ? bear.Dna.Vision * 1.5f : bear.Dna.Vision;
            float visionSq = effectiveVision * effectiveVision;
            _organismGrid.QueryRadius(bear.X, bear.Y, effectiveVision, _queryBuffer);

            Organism targetPred = null;
            float predDistSq = visionSq;
            Organism targetHuman = null;
            float humanDistSq = visionSq;
            Organism targetPrey = null;
            float preyDistSq = visionSq;

            for (int i = 0; i < _queryBuffer.Count; i++)
            {
                var other = _organisms[_queryBuffer[i]];
                if (!other.IsAlive || other.Id == bear.Id) continue;
                float dx = other.X - bear.X;
                float dy = other.Y - bear.Y;
                float d2 = dx * dx + dy * dy;

                if (other.Species == EcoSpecies.Predator && d2 < predDistSq)
                {
                    predDistSq = d2;
                    targetPred = other;
                }
                else if (other.Species == EcoSpecies.Human && d2 < humanDistSq)
                {
                    humanDistSq = d2;
                    targetHuman = other;
                }
                else if (other.Species == EcoSpecies.Prey && d2 < preyDistSq)
                {
                    preyDistSq = d2;
                    targetPrey = other;
                }
            }

            // 곰 우선순위: 가까운 인간(영역 대립) -> 배고플 때(<78%) 육식동물(여우) -> 초식동물 -> 풀(잡식)
            bool isHungry = bear.Energy < bear.MaxEnergy * 0.78f;
            Organism huntTarget = null;
            float huntDistSq = float.MaxValue;
            float humanAggroRange = bear.SkillActiveTicks > 0 ? effectiveVision : bear.Dna.Vision * 0.75f;
            if (targetHuman != null && humanDistSq < humanAggroRange * humanAggroRange)
            {
                bear.EatingTicks = 0;
                huntTarget = targetHuman;
                huntDistSq = humanDistSq;
            }
            else if (bear.EatingTicks == 0 && isHungry)
            {
                if (targetPred != null)
                {
                    huntTarget = targetPred;
                    huntDistSq = predDistSq;
                }
                else if (targetPrey != null)
                {
                    huntTarget = targetPrey;
                    huntDistSq = preyDistSq;
                }
            }

            if (bear.EatingTicks > 0 && huntTarget == null)
            {
                bear.Vx *= 0.25f;
                bear.Vy *= 0.25f;
            }
            else if (huntTarget != null)
            {
                // 알파 곰 보스 전용 스킬: 범위 할퀴기 (반경 56f 내 적 광역 타격 + 넉백)
                if (bear.IsAlphaBoss && bear.AttackCooldown == 0 && huntDistSq <= 56f * 56f)
                {
                    TriggerAlphaBossSwipe(bear);
                }
                // 곰 전용 스킬: 죽기 직전(체력 30% 미만) 발악 광폭 돌진 (긴 쿨타임 260틱)
                if (bear.SkillActiveTicks == 0 && bear.AttackCooldown == 0 &&
                    bear.Energy < bear.MaxEnergy * 0.30f)
                {
                    bear.SkillActiveTicks = 20;
                    bear.AttackCooldown = 260;
                }

                float moveMult = bear.SkillActiveTicks > 0 ? 1.85f : 1.04f;
                SteerToward(bear, huntTarget.X, huntTarget.Y, moveMult);
                float catchR = bear.Dna.Size + huntTarget.Dna.Size * 0.8f;
                if (huntDistSq <= catchR * catchR)
                {
                    if (huntTarget.Species == EcoSpecies.Human)
                    {
                        float dmg = bear.SkillActiveTicks > 0 ? 165f : 120f;
                        huntTarget.Energy -= dmg;
                        if (huntTarget.Energy <= 0f)
                        {
                            huntTarget.IsAlive = false;
                            bear.Kills++;
                            bear.EatingTicks = 26;
                            bear.Energy = Math.Min(bear.MaxEnergy, bear.Energy + 135f);
                        }
                    }
                    else
                    {
                        huntTarget.IsAlive = false;
                        bear.Kills++;
                        bear.EatingTicks = 26;
                        float gain = huntTarget.Species == EcoSpecies.Predator ? 95f : 52f;
                        bear.Energy = Math.Min(bear.MaxEnergy, bear.Energy + gain);
                    }
                }
            }
            else
            {
                // 곰이 근처 인간 모닥불을 발견하면 습격 시도
                int targetCampIdx = FindNearestCampfire(bear.X, bear.Y, bear.Dna.Vision * 0.9f);
                if (targetCampIdx >= 0)
                {
                    var cf = _campfires[targetCampIdx];
                    SteerToward(bear, cf.X, cf.Y, 1.0f);
                    float dx = cf.X - bear.X;
                    float dy = cf.Y - bear.Y;
                    if (dx * dx + dy * dy <= (bear.Dna.Size + 18f) * (bear.Dna.Size + 18f))
                    {
                        cf.Hp -= 24f;
                        if (cf.Hp <= 0f)
                        {
                            RemoveCampfireSwapPop(targetCampIdx);
                        }
                        else
                        {
                            _campfires[targetCampIdx] = cf;
                        }
                    }
                }
                else
                {
                    int targetHouseIdx = FindNearestHouse(bear.X, bear.Y, bear.Dna.Vision * 0.85f);
                    if (targetHouseIdx >= 0)
                    {
                        var h = _houses[targetHouseIdx];
                        SteerToward(bear, h.X, h.Y, 1.0f);
                        float dx = h.X - bear.X;
                        float dy = h.Y - bear.Y;
                        if (dx * dx + dy * dy <= (bear.Dna.Size + 24f) * (bear.Dna.Size + 24f))
                        {
                            h.Hp -= bear.IsAlphaBoss ? 36f : 20f;
                            if (h.Hp <= 0f)
                            {
                                ClearHouseOwners(h.Id);
                                int last = _houses.Count - 1;
                                _houses[targetHouseIdx] = _houses[last];
                                _houses.RemoveAt(last);
                            }
                            else
                            {
                                _houses[targetHouseIdx] = h;
                            }
                        }
                    }
                    else
                    {
                        // 사냥감이 없으면 배고플 때 잡식성으로 풀(열매) 섭취
                    if (isHungry)
                    {
                        _foodGrid.QueryRadius(bear.X, bear.Y, bear.Dna.Vision * 0.8f, _queryBuffer);
                        int nearestFoodIdx = -1;
                        float nearestFoodDistSq = visionSq;
                        for (int i = 0; i < _queryBuffer.Count; i++)
                        {
                            int fIdx = _queryBuffer[i];
                            if (fIdx >= _foods.Count) continue;
                            var f = _foods[fIdx];
                            float dx = f.X - bear.X;
                            float dy = f.Y - bear.Y;
                            float d2 = dx * dx + dy * dy;
                            if (d2 < nearestFoodDistSq)
                            {
                                nearestFoodDistSq = d2;
                                nearestFoodIdx = fIdx;
                            }
                        }

                        if (nearestFoodIdx >= 0)
                        {
                            var f = _foods[nearestFoodIdx];
                            SteerToward(bear, f.X, f.Y, 0.9f);
                            float eatR = bear.Dna.Size + 5f;
                            if (nearestFoodDistSq <= eatR * eatR)
                            {
                                bear.Energy = Math.Min(bear.MaxEnergy, bear.Energy + f.EnergyValue * 1.35f);
                                bear.EatingTicks = 10;
                                RemoveFoodSwapPop(nearestFoodIdx);
                            }
                        }
                        else
                        {
                            Wander(bear);
                        }
                    }
                    else
                    {
                        Wander(bear);
                    }
                }
                }
            }

            ApplySameSpeciesSeparation(bear, 30f);
            MoveAndBounce(bear);
            float bearRestMult = (bear.EatingTicks > 0 || (huntTarget == null && !isHungry)) ? 0.62f : 1.0f;
            bear.Energy -= bear.Dna.ComputeMetabolicCost(EcoSpecies.Bear) * GetSeasonMetabolicMultiplier() * bearRestMult;

            if (bear.Energy <= 0f)
            {
                bear.IsAlive = false;
                return;
            }

            if (bear.Energy >= _config.BearReproduceThreshold &&
                BearCount < Math.Max(3, (int)(_config.MaxOrganismCount * 0.06f)) &&
                (_organisms.Count + _pendingOffspring.Count) < _config.MaxOrganismCount)
            {
                Reproduce(bear, mutationRate);
            }
        }

        private void TriggerAlphaBossSwipe(Organism boss)
        {
            const float swipeR = 56f;
            float swipeRSq = swipeR * swipeR;
            BossSwipeX = boss.X;
            BossSwipeY = boss.Y;
            BossSwipeRadius = swipeR;
            BossSwipeTicks = 14;
            boss.AttackCooldown = 95;

            _organismGrid.QueryRadius(boss.X, boss.Y, swipeR, _queryBuffer);
            for (int i = 0; i < _queryBuffer.Count; i++)
            {
                var other = _organisms[_queryBuffer[i]];
                if (!other.IsAlive || other.Id == boss.Id || other.Species == EcoSpecies.Bear) continue;
                float dx = other.X - boss.X;
                float dy = other.Y - boss.Y;
                float d2 = dx * dx + dy * dy;
                if (d2 > swipeRSq) continue;

                float dmg = other.Species == EcoSpecies.Human ? 70f : 95f;
                other.Energy -= dmg;
                float dist = (float)Math.Sqrt(d2);
                if (dist > 0.01f)
                {
                    other.X = Clamp(other.X + (dx / dist) * 18f, 10f, _config.WorldWidth - 10f);
                    other.Y = Clamp(other.Y + (dy / dist) * 18f, 10f, _config.WorldHeight - 10f);
                }
                if (other.Energy <= 0f)
                {
                    other.IsAlive = false;
                    boss.Kills++;
                    boss.Energy = Math.Min(boss.MaxEnergy, boss.Energy + 65f);
                }
            }
        }

        private void StepHuman(Organism human, float mutationRate)
        {
            human.IsHibernating = false;
            float visionSq = human.Dna.Vision * human.Dna.Vision;
            _organismGrid.QueryRadius(human.X, human.Y, human.Dna.Vision, _queryBuffer);

            Organism targetBear = null;
            float bearDistSq = visionSq;
            Organism targetPred = null;
            float predDistSq = visionSq;
            Organism targetPrey = null;
            float preyDistSq = visionSq;

            for (int i = 0; i < _queryBuffer.Count; i++)
            {
                var other = _organisms[_queryBuffer[i]];
                if (!other.IsAlive || other.Id == human.Id) continue;
                float dx = other.X - human.X;
                float dy = other.Y - human.Y;
                float d2 = dx * dx + dy * dy;

                if (other.Species == EcoSpecies.Bear && d2 < bearDistSq)
                {
                    bearDistSq = d2;
                    targetBear = other;
                }
                else if (other.Species == EcoSpecies.Predator && d2 < predDistSq)
                {
                    predDistSq = d2;
                    targetPred = other;
                }
                else if (other.Species == EcoSpecies.Prey && d2 < preyDistSq)
                {
                    preyDistSq = d2;
                    targetPrey = other;
                }
            }

            bool isWinter = EnableSeasons && CurrentSeason == EcoSeason.Winter;
            int myCampIdx = human.CampfireGroupId > 0 ? FindCampfireByGroupId(human.CampfireGroupId) : -1;
            if (myCampIdx < 0)
            {
                human.CampfireGroupId = 0;
                int joinIdx = FindNearestCampfire(human.X, human.Y, isWinter ? 520f : 220f);
                if (joinIdx >= 0)
                {
                    human.CampfireGroupId = _campfires[joinIdx].GroupId;
                    myCampIdx = joinIdx;
                }
            }

            bool shelterByFire = isWinter && myCampIdx >= 0 && human.Energy >= human.MaxEnergy * 0.35f;

            bool isArcher = human.Kills >= 3;
            float weaponRange = isArcher ? 94f : 48f;

            // 겨울에는 본인 그룹 모닥불 반경 안에서 방어/활동, 굶주릴 때만 원정 사냥
            Organism target;
            if (isWinter && myCampIdx >= 0)
            {
                var groupCamp = _campfires[myCampIdx];
                float campZoneR = groupCamp.Radius * 1.45f;
                float campZoneSq = campZoneR * campZoneR;
                Organism bearInZone = (targetBear != null && ((targetBear.X - groupCamp.X) * (targetBear.X - groupCamp.X) + (targetBear.Y - groupCamp.Y) * (targetBear.Y - groupCamp.Y) <= campZoneSq || bearDistSq <= weaponRange * weaponRange)) ? targetBear : null;
                Organism predInZone = (targetPred != null && ((targetPred.X - groupCamp.X) * (targetPred.X - groupCamp.X) + (targetPred.Y - groupCamp.Y) * (targetPred.Y - groupCamp.Y) <= campZoneSq || predDistSq <= weaponRange * weaponRange)) ? targetPred : null;
                Organism preyInZone = (targetPrey != null && human.Energy < human.MaxEnergy * 0.55f && ((targetPrey.X - groupCamp.X) * (targetPrey.X - groupCamp.X) + (targetPrey.Y - groupCamp.Y) * (targetPrey.Y - groupCamp.Y) <= campZoneSq)) ? targetPrey : null;
                target = bearInZone ?? predInZone ?? preyInZone;
            }
            else if (isWinter)
            {
                const float selfDefendSq = 80f * 80f;
                if (targetBear != null && bearDistSq <= selfDefendSq) target = targetBear;
                else if (human.Energy < human.MaxEnergy * 0.65f) target = targetBear ?? targetPred ?? targetPrey;
                else target = null;
            }
            else
            {
                bool humanHungry = human.Energy < human.MaxEnergy * 0.72f;
                bool bearTooClose = targetBear != null && bearDistSq <= (weaponRange * 1.15f) * (weaponRange * 1.15f);
                if (bearTooClose) target = targetBear;
                else if (humanHungry) target = targetBear ?? targetPred ?? targetPrey;
                else target = null;
            }
            float targetDistSq = target == targetBear ? bearDistSq : (target == targetPred ? predDistSq : preyDistSq);

            if (target != null)
            {
                float kiteMinRange = isArcher ? 48f : 28f;
                float winterMoveMult = isWinter ? 0.75f : 1.0f;

                if (target.Species == EcoSpecies.Bear && targetDistSq < kiteMinRange * kiteMinRange)
                {
                    // 곰이 너무 가까우면 뒤로 빠지며 카이팅
                    SteerAway(human, target.X, target.Y, 1.08f * winterMoveMult);
                }
                else if (targetDistSq > weaponRange * weaponRange && !shelterByFire)
                {
                    SteerToward(human, target.X, target.Y, 1.0f * winterMoveMult);
                }
                else
                {
                    // 사거리 내 조준 감속
                    human.Vx *= 0.85f;
                    human.Vy *= 0.85f;
                }

                if (targetDistSq <= weaponRange * weaponRange && human.AttackCooldown == 0)
                {
                    human.AttackCooldown = isArcher ? 30 : 24;
                    if (_hunterShots.Count < 32)
                    {
                        _hunterShots.Add(new HunterShot(human.X, human.Y, target.X, target.Y, isArcher ? 6 : 4));
                    }

                    if (target.Species == EcoSpecies.Bear)
                    {
                        target.Energy -= isArcher ? 72f : 55f;
                        WakeBearWithCharge(target);
                        if (target.Energy <= 0f)
                        {
                            target.IsAlive = false;
                            human.Kills++;
                            if (human.MeatCount < MaxInventoryStack) human.MeatCount++;
                            human.Energy = Math.Min(human.MaxEnergy, human.Energy + 130f);
                        }
                    }
                    else if (target.IsPlayer)
                    {
                        // 플레이어 개체는 창/화살 1방에 즉사하지 않고 회피 기회 부여
                        target.Energy -= isArcher ? 65f : 45f;
                        if (target.Energy <= 0f)
                        {
                            target.IsAlive = false;
                            human.Kills++;
                            if (human.MeatCount < MaxInventoryStack) human.MeatCount++;
                            human.Energy = Math.Min(human.MaxEnergy, human.Energy + 75f);
                        }
                    }
                    else
                    {
                        target.IsAlive = false;
                        human.Kills++;
                        if (human.MeatCount < MaxInventoryStack) human.MeatCount++;
                        float gain = target.Species == EcoSpecies.Predator ? 72f : 44f;
                        human.Energy = Math.Min(human.MaxEnergy, human.Energy + gain);
                    }
                }
            }
            else if (isWinter && human.HouseId > 0 && FindHouseIndexById(human.HouseId) >= 0)
            {
                var myHouse = _houses[FindHouseIndexById(human.HouseId)];
                float dx = myHouse.X - human.X;
                float dy = myHouse.Y - human.Y;
                if (dx * dx + dy * dy > 22f * 22f)
                {
                    SteerToward(human, myHouse.X, myHouse.Y, 0.95f);
                }
                else
                {
                    human.Vx *= 0.65f;
                    human.Vy *= 0.65f;
                }
            }
            else if (isWinter && myCampIdx >= 0)
            {
                var cf = _campfires[myCampIdx];
                float dx = cf.X - human.X;
                float dy = cf.Y - human.Y;
                float d2 = dx * dx + dy * dy;
                float roamR = cf.Radius * 1.45f;
                if (d2 > roamR * roamR)
                {
                    SteerToward(human, cf.X, cf.Y, 0.92f);
                }
                else if (shelterByFire && d2 > 36f * 36f)
                {
                    SteerToward(human, cf.X, cf.Y, 0.78f);
                }
                else
                {
                    // 본인 그룹 모닥불 범위 내 순찰/휴식
                    human.Vx *= 0.78f;
                    human.Vy *= 0.78f;
                }
            }
            else
            {
                // 비전투 중 공사 중인 집이 있으면 협동 건축하러 이동
                int buildIdx = !isWinter ? FindNearestUnfinishedHouse(human.X, human.Y, 280f) : -1;
                if (buildIdx >= 0)
                {
                    var bh = _houses[buildIdx];
                    SteerToward(human, bh.X, bh.Y, 0.96f);
                }
                else if (IsStoneAgeUnlocked && !isWinter && human.MeatCount >= 1 && human.Energy >= human.MaxEnergy * 0.65f && human.StoneCount < 12)
                {
                    // 식량·체력 여유가 있으면 가끔 주변 바위로 돌 채집
                    int rockIdx = FindNearestRockIndex(human.X, human.Y);
                    if (rockIdx >= 0)
                    {
                        var rk = _terrains[rockIdx];
                        float dx = rk.X - human.X;
                        float dy = rk.Y - human.Y;
                        float wantR = rk.Radius + human.Dna.Size + 8f;
                        if (dx * dx + dy * dy > wantR * wantR)
                        {
                            SteerToward(human, rk.X, rk.Y, 0.88f);
                        }
                        else
                        {
                            human.Vx *= 0.7f;
                            human.Vy *= 0.7f;
                        }
                    }
                    else
                    {
                        Wander(human);
                    }
                }
                else
                {
                    Wander(human);
                    if (isWinter)
                    {
                        human.Vx *= 0.72f;
                        human.Vy *= 0.72f;
                    }
                }
            }

            ApplySameSpeciesSeparation(human, 26f);
            MoveAndBounce(human);
            if (target == null)
            {
                TryGatherWood(human);
                TryGatherStone(human);
                AssistNearbyUnfinishedHouse(human);
            }

            bool nearCampfire = FindNearestCampfire(human.X, human.Y, 72f) >= 0;
            bool inHouse = IsInsideCompletedHouse(human);
            float shelterMult = inHouse ? 0.45f : (nearCampfire ? 0.70f : 1.0f);
            float humanSeasonCost = GetSeasonMetabolicMultiplier() * shelterMult;
            human.Energy -= human.Dna.ComputeMetabolicCost(EcoSpecies.Human) * humanSeasonCost;

            if (inHouse && human.Energy > 0f)
            {
                human.Energy = Math.Min(human.MaxEnergy, human.Energy + 0.55f);
            }
            else if (nearCampfire && human.Energy > 0f)
            {
                human.Energy = Math.Min(human.MaxEnergy, human.Energy + 0.35f);
            }

            TryConsumeMeatForRecovery(human, nearCampfire || inHouse, false);

            // 정착기(3년차+) 해금 후 돌+나뭇가지 충분 & 집 없으면 집 착공 / 겨울엔 모닥불 건설
            if (IsHouseAgeUnlocked && human.HouseId == 0 && human.WoodCount >= HouseWoodCost && human.StoneCount >= HouseStoneCost)
            {
                TryStartHouse(human, false);
            }
            else if (IsCampfireAgeUnlocked && isWinter && human.WoodCount >= CampfireWoodCost)
            {
                TryBuildCampfireWithWood(human, false);
            }

            if (human.Energy <= 0f)
            {
                human.IsAlive = false;
                return;
            }

            float reproThreshold = inHouse ? _config.HumanReproduceThreshold * 0.85f : _config.HumanReproduceThreshold;
            if (human.Energy >= reproThreshold &&
                HumanCount < 4 &&
                (_organisms.Count + _pendingOffspring.Count) < _config.MaxOrganismCount)
            {
                Reproduce(human, mutationRate);
            }
        }

        private bool TryBuildCampfireWithWood(Organism human, bool isPlayer)
        {
            if (!IsCampfireAgeUnlocked || human.WoodCount < CampfireWoodCost || _campfires.Count >= MaxCampfires) return false;
            if (FindNearestCampfire(human.X, human.Y, 150f) >= 0) return false;

            human.WoodCount -= CampfireWoodCost;
            int groupId = _nextCampfireGroupId++;
            var cf = new CampfireSite(groupId, human.X, human.Y, 72f, 120f, CampfireDurationTicks);
            _campfires.Add(cf);
            human.CampfireGroupId = groupId;

            // 주변 무소속 인간들도 이 모닥불 그룹에 편입
            const float joinRadiusSq = 160f * 160f;
            for (int i = 0; i < _organisms.Count; i++)
            {
                var other = _organisms[i];
                if (!other.IsAlive || other.Species != EcoSpecies.Human || other.IsPlayer) continue;
                if (other.CampfireGroupId > 0 && FindCampfireByGroupId(other.CampfireGroupId) >= 0) continue;
                float dx = other.X - cf.X;
                float dy = other.Y - cf.Y;
                if (dx * dx + dy * dy <= joinRadiusSq)
                {
                    other.CampfireGroupId = groupId;
                }
            }

            if (isPlayer)
            {
                PlayerNotice = $"🔥 나뭇가지 {CampfireWoodCost}개로 모닥불을 건설했습니다!";
            }
            return true;
        }

        private void TryConsumeMeatForRecovery(Organism human, bool nearCampfire, bool isPlayer)
        {
            if (human.MeatCount <= 0 || human.Energy <= 0f) return;

            bool needEmergencyHeal = human.Energy < human.MaxEnergy * 0.38f;
            bool restAtWinterFire = nearCampfire && CurrentSeason == EcoSeason.Winter && human.Energy < human.MaxEnergy * 0.68f;
            if (!needEmergencyHeal && !restAtWinterFire) return;

            human.MeatCount--;
            human.Energy = Math.Min(human.MaxEnergy, human.Energy + 85f);
            if (isPlayer)
            {
                PlayerNotice = "🍖 고기를 먹어 에너지를 회복했습니다! (+85)";
            }
        }

        private bool TryStartHouse(Organism human, bool isPlayer)
        {
            if (!IsHouseAgeUnlocked || human.WoodCount < HouseWoodCost || human.StoneCount < HouseStoneCost || _houses.Count >= MaxHouses) return false;
            if (FindNearestHouse(human.X, human.Y, 160f) >= 0) return false;

            human.WoodCount -= HouseWoodCost;
            human.StoneCount -= HouseStoneCost;
            int id = _nextHouseId++;
            var house = new HouseSite(id, human.X, human.Y, 44f, 350f, HouseDurationTicks, human.Id);
            _houses.Add(house);
            human.HouseId = id;
            if (isPlayer)
            {
                PlayerNotice = "🏠 집 건축을 시작했습니다! 동료가 도우면 빠르게 완공됩니다.";
            }
            return true;
        }

        private void AssistNearbyUnfinishedHouse(Organism human)
        {
            for (int i = 0; i < _houses.Count; i++)
            {
                var h = _houses[i];
                float dx = h.X - human.X;
                float dy = h.Y - human.Y;
                if (dx * dx + dy * dy > 60f * 60f) continue;

                // 2번째 집주인 빈자리 배정
                if (h.Owner2Id == 0 && h.Owner1Id != human.Id && human.HouseId == 0)
                {
                    h.Owner2Id = human.Id;
                    human.HouseId = h.Id;
                }

                if (!h.IsComplete)
                {
                    h.BuildProgress = Math.Min(h.BuildTarget, h.BuildProgress + 2);
                    if (h.IsComplete && human.IsPlayer)
                    {
                        PlayerNotice = "🏠 오두막 완공! 집 안에서는 겨울 보온 및 회복 보너스를 받습니다.";
                    }
                }
                _houses[i] = h;
                break;
            }
        }

        private bool IsInsideCompletedHouse(Organism human)
        {
            for (int i = 0; i < _houses.Count; i++)
            {
                var h = _houses[i];
                if (!h.IsComplete) continue;
                float dx = h.X - human.X;
                float dy = h.Y - human.Y;
                if (dx * dx + dy * dy <= h.Radius * h.Radius) return true;
            }
            return false;
        }

        private void TryGatherStone(Organism human)
        {
            if (!IsStoneAgeUnlocked || human.GatherCooldown > 0 || human.StoneCount >= MaxInventoryStack) return;
            for (int i = 0; i < _terrains.Count; i++)
            {
                var t = _terrains[i];
                if (t.IsWater) continue;
                float gatherR = t.Radius + human.Dna.Size + 10f;
                float dx = t.X - human.X;
                float dy = t.Y - human.Y;
                if (dx * dx + dy * dy <= gatherR * gatherR)
                {
                    human.StoneCount = Math.Min(MaxInventoryStack, human.StoneCount + 1);
                    human.GatherCooldown = 28;
                    break;
                }
            }
        }

        private void TryGatherWood(Organism human)
        {
            if (human.GatherCooldown > 0 || human.WoodCount >= MaxInventoryStack) return;

            float gatherR = human.Dna.Size + 6f;
            _foodGrid.QueryRadius(human.X, human.Y, gatherR, _queryBuffer);
            float gatherRSq = gatherR * gatherR;
            for (int i = 0; i < _queryBuffer.Count; i++)
            {
                int fIdx = _queryBuffer[i];
                if (fIdx >= _foods.Count) continue;
                var f = _foods[fIdx];
                float dx = f.X - human.X;
                float dy = f.Y - human.Y;
                if (dx * dx + dy * dy <= gatherRSq)
                {
                    human.WoodCount = Math.Min(MaxInventoryStack, human.WoodCount + 1);
                    human.GatherCooldown = 18;
                    break;
                }
            }
        }

        private void Reproduce(Organism parent, float mutationRate)
        {
            parent.Energy *= 0.48f;
            parent.OffspringCount++;

            var childDna = parent.Dna.Mutate(_rng, mutationRate, _config.MutationScale);
            int childGen = parent.Generation + 1;
            if (childGen > MaxGeneration)
                MaxGeneration = childGen;

            double angle = _rng.NextDouble() * Math.PI * 2.0;
            float maxEnergy = ComputeMaxEnergy(parent.Species, childDna);
            float spawnDist = parent.Species == EcoSpecies.Prey ? 10f : parent.Dna.Size * 2.4f;

            var child = new Organism
            {
                Id = _nextOrganismId++,
                Species = parent.Species,
                Generation = childGen,
                Dna = childDna,
                X = Clamp(parent.X + (float)Math.Cos(angle) * spawnDist, 10f, _config.WorldWidth - 10f),
                Y = Clamp(parent.Y + (float)Math.Sin(angle) * spawnDist, 10f, _config.WorldHeight - 10f),
                Vx = (float)Math.Cos(angle) * childDna.Speed,
                Vy = (float)Math.Sin(angle) * childDna.Speed,
                Energy = parent.Energy,
                MaxEnergy = maxEnergy,
                IsAlive = true
            };

            _pendingOffspring.Add(child);
        }

        private void ApplySameSpeciesSeparation(Organism org, float minSpacing)
        {
            _organismGrid.QueryRadius(org.X, org.Y, minSpacing, _queryBuffer);
            float minDistSq = minSpacing * minSpacing;
            float pushX = 0f;
            float pushY = 0f;
            int neighbors = 0;

            for (int i = 0; i < _queryBuffer.Count; i++)
            {
                int idx = _queryBuffer[i];
                if (idx < 0 || idx >= _organisms.Count) continue;
                var other = _organisms[idx];
                if (!other.IsAlive || other.Id == org.Id || other.Species != org.Species) continue;

                float dx = org.X - other.X;
                float dy = org.Y - other.Y;
                float d2 = dx * dx + dy * dy;
                if (d2 > 0.0001f && d2 < minDistSq)
                {
                    float dist = (float)Math.Sqrt(d2);
                    float weight = (minSpacing - dist) / minSpacing;
                    pushX += (dx / dist) * weight;
                    pushY += (dy / dist) * weight;
                    neighbors++;
                }
                else if (d2 <= 0.0001f)
                {
                    // 완전히 겹친 경우 임의 방향으로 분리
                    pushX += (org.Id % 2 == 0 ? 1f : -1f) * 0.5f;
                    pushY += (org.Id % 3 == 0 ? 1f : -1f) * 0.5f;
                    neighbors++;
                }
            }

            if (neighbors > 0)
            {
                org.Vx += pushX * 0.9f;
                org.Vy += pushY * 0.9f;
                float speed = (float)Math.Sqrt(org.Vx * org.Vx + org.Vy * org.Vy);
                float maxAllowed = org.Dna.Speed * (org.SkillActiveTicks > 0 ? 1.85f : 1.15f);
                if (speed > maxAllowed && speed > 0.001f)
                {
                    org.Vx = (org.Vx / speed) * maxAllowed;
                    org.Vy = (org.Vy / speed) * maxAllowed;
                }
            }
        }

        private void SteerToward(Organism org, float tx, float ty, float speedMultiplier)
        {
            float dx = tx - org.X;
            float dy = ty - org.Y;
            float len = (float)Math.Sqrt(dx * dx + dy * dy);
            if (len < 0.001f) return;

            float desiredVx = (dx / len) * org.Dna.Speed * speedMultiplier;
            float desiredVy = (dy / len) * org.Dna.Speed * speedMultiplier;
            org.Vx = org.Vx * 0.72f + desiredVx * 0.28f;
            org.Vy = org.Vy * 0.72f + desiredVy * 0.28f;
        }

        private void SteerAway(Organism org, float tx, float ty, float speedMultiplier)
        {
            float dx = org.X - tx;
            float dy = org.Y - ty;
            float len = (float)Math.Sqrt(dx * dx + dy * dy);
            if (len < 0.001f) return;

            float desiredVx = (dx / len) * org.Dna.Speed * speedMultiplier;
            float desiredVy = (dy / len) * org.Dna.Speed * speedMultiplier;
            org.Vx = org.Vx * 0.65f + desiredVx * 0.35f;
            org.Vy = org.Vy * 0.65f + desiredVy * 0.35f;
        }

        private void Wander(Organism org)
        {
            double angle = Math.Atan2(org.Vy, org.Vx) + (_rng.NextDouble() - 0.5) * 0.45;
            org.Vx = (float)Math.Cos(angle) * org.Dna.Speed;
            org.Vy = (float)Math.Sin(angle) * org.Dna.Speed;
        }

        private void MoveAndBounce(Organism org)
        {
            org.X += org.Vx;
            org.Y += org.Vy;

            float margin = org.Dna.Size;
            if (org.X < margin)
            {
                org.X = margin;
                org.Vx = Math.Abs(org.Vx);
            }
            else if (org.X > _config.WorldWidth - margin)
            {
                org.X = _config.WorldWidth - margin;
                org.Vx = -Math.Abs(org.Vx);
            }

            if (org.Y < margin)
            {
                org.Y = margin;
                org.Vy = Math.Abs(org.Vy);
            }
            else if (org.Y > _config.WorldHeight - margin)
            {
                org.Y = _config.WorldHeight - margin;
                org.Vy = -Math.Abs(org.Vy);
            }

            // 바위/연못 지형 충돌 및 우회
            for (int i = 0; i < _terrains.Count; i++)
            {
                var t = _terrains[i];
                float dx = org.X - t.X;
                float dy = org.Y - t.Y;
                float minD = t.Radius + org.Dna.Size;
                float d2 = dx * dx + dy * dy;
                if (d2 < minD * minD)
                {
                    float d = (float)Math.Sqrt(d2);
                    if (d < 0.001f) { dx = 1f; dy = 0f; d = 1f; }
                    float nx = dx / d;
                    float ny = dy / d;

                    // 바위는 고체 장애물, 연못 깊은 중심은 반발 + 접선 우회 슬라이드
                    float pushR = t.IsWater ? (t.Radius * 0.72f + org.Dna.Size) : minD;
                    if (d < pushR)
                    {
                        org.X = t.X + nx * pushR;
                        org.Y = t.Y + ny * pushR;
                        float dot = org.Vx * nx + org.Vy * ny;
                        if (dot < 0f)
                        {
                            org.Vx -= 1.2f * dot * nx;
                            org.Vy -= 1.2f * dot * ny;
                        }
                        // 바위/연못에 막혀 정체되지 않도록 접선 방향으로 부드럽게 우회
                        float tangentDir = (org.Id % 2 == 0) ? 1f : -1f;
                        org.Vx += -ny * tangentDir * (org.Dna.Speed * 0.45f);
                        org.Vy += nx * tangentDir * (org.Dna.Speed * 0.45f);
                    }
                }
            }
        }
    }
}
