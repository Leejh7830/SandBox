using System;
using System.Collections.Generic;
using GateHelper.EcoSim.Models;

namespace GateHelper.EcoSim.Services
{
    public sealed partial class EcoSimulationEngine
    {
        private void InitAchievements()
        {
            _achievements.Clear();
            _achievements.Add(new EcoAchievement { Id = "evo2", Title = "첫 진화의 발걸음", Description = "내 개체 2단계(여우) 도달" });
            _achievements.Add(new EcoAchievement { Id = "evo3", Title = "숲의 제왕", Description = "내 개체 3단계(곰) 도달" });
            _achievements.Add(new EcoAchievement { Id = "evo5", Title = "전설의 명사수", Description = "내 개체 5단계(인간 궁수) 도달" });
            _achievements.Add(new EcoAchievement { Id = "boss", Title = "거대 맹수 토벌", Description = "알파 곰 보스 처치" });
            _achievements.Add(new EcoAchievement { Id = "kills10", Title = "백전노장 사냥꾼", Description = "단일 개체 사냥 10회 달성" });
            _achievements.Add(new EcoAchievement { Id = "gen50", Title = "진화의 목격자", Description = "생태계 50세대 돌파" });
            _achievements.Add(new EcoAchievement { Id = "age2y", Title = "혹한의 생존자", Description = "나이 2년(1,760틱) 개체 탄생" });
            _achievements.Add(new EcoAchievement { Id = "fire", Title = "문명의 불꽃", Description = "인간 거점 모닥불 설치" });
        }

        private void UnlockAchievement(string id)
        {
            for (int i = 0; i < _achievements.Count; i++)
            {
                if (_achievements[i].Id == id && !_achievements[i].Unlocked)
                {
                    _achievements[i].Unlocked = true;
                    AchievementToastText = $"★ 업적 달성: {_achievements[i].Title}";
                    AchievementToastTicks = 95;
                    break;
                }
            }
        }

        public PlayerPerk[] RollPerkChoices(int count = 3)
        {
            var all = new[] { PlayerPerk.Swiftness, PlayerPerk.Vitality, PlayerPerk.PredatorInstinct, PlayerPerk.QuickCooldown, PlayerPerk.SharpWeapon };
            for (int i = all.Length - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                var tmp = all[i]; all[i] = all[j]; all[j] = tmp;
            }
            var res = new PlayerPerk[Math.Min(count, all.Length)];
            Array.Copy(all, res, res.Length);
            return res;
        }

        public void ApplyPlayerPerk(PlayerPerk perk)
        {
            PendingPerkChoice = false;
            string tag;
            switch (perk)
            {
                case PlayerPerk.Swiftness:
                    PlayerSpeedMult *= 1.15f;
                    tag = "속도+";
                    break;
                case PlayerPerk.Vitality:
                    PlayerVitalityMult *= 1.25f;
                    tag = "체력+";
                    break;
                case PlayerPerk.PredatorInstinct:
                    PlayerGainMult *= 1.30f;
                    tag = "흡혈+";
                    break;
                case PlayerPerk.QuickCooldown:
                    PlayerCooldownMult *= 0.75f;
                    tag = "쿨감+";
                    break;
                default:
                    PlayerDamageMult *= 1.30f;
                    tag = "강공+";
                    break;
            }
            PlayerPerkSummary = PlayerPerkSummary == "특성: 없음" ? $"특성: {tag}" : $"{PlayerPerkSummary}, {tag}";

            var p = FindOrganismById(PlayerId);
            if (p != null)
            {
                p.MaxEnergy = ComputeMaxEnergy(p.Species, p.Dna) * PlayerVitalityMult;
                p.Energy = Math.Min(p.MaxEnergy, p.Energy + p.MaxEnergy * 0.35f);
            }
        }

        private void ResetPlayerPerks()
        {
            PendingPerkChoice = false;
            PlayerSpeedMult = 1.0f;
            PlayerVitalityMult = 1.0f;
            PlayerGainMult = 1.0f;
            PlayerCooldownMult = 1.0f;
            PlayerDamageMult = 1.0f;
            PlayerPerkSummary = "특성: 없음";
        }

        public void SetPlayerInput(float moveX, float moveY, bool skillPressed, bool buildCampfirePressed = false)
        {
            _playerInputX = moveX;
            _playerInputY = moveY;
            if (skillPressed) _playerSkillPressed = true;
            if (buildCampfirePressed) _playerBuildCampfirePressed = true;
        }

        public void StartPlayerMode()
        {
            IsPlayerMode = true;
            PlayerDeathCount = 0;
            PlayerBestStage = 1;
            PlayerNotice = "방향키/WASD 이동 · Space 스킬/대시";
            if (FindOrganismById(PlayerId) == null)
            {
                SpawnPlayerStage1();
            }
        }

        public void StopPlayerMode()
        {
            IsPlayerMode = false;
            var p = FindOrganismById(PlayerId);
            if (p != null)
            {
                p.IsPlayer = false;
            }
            PlayerId = -1;
            PlayerNotice = "";
        }

        private void SpawnPlayerStage1()
        {
            float px = _config.WorldWidth * 0.5f;
            float py = _config.WorldHeight * 0.5f;
            for (int attempt = 0; attempt < 15; attempt++)
            {
                float tx = RandomRange(60f, _config.WorldWidth - 60f);
                float ty = RandomRange(60f, _config.WorldHeight - 60f);
                if (!IsInsideTerrain(tx, ty, 20f))
                {
                    px = tx;
                    py = ty;
                    break;
                }
            }

            var dna = new Genome(2.75f, 110f, 6.8f);
            var player = SpawnOrganism(EcoSpecies.Prey, px, py, dna, Math.Max(1, MaxGeneration), 130f);
            if (player == null && _organisms.Count > 0)
            {
                // 개체수가 꽉 찼으면 가장 약한 비플레이어 1마리 교체
                _organisms.RemoveAt(0);
                player = SpawnOrganism(EcoSpecies.Prey, px, py, dna, Math.Max(1, MaxGeneration), 130f);
            }
            if (player != null)
            {
                player.IsPlayer = true;
                PlayerId = player.Id;
                PlayerStage = 1;
                PlayerExp = 0f;
                PlayerNextExp = 200f;
            }
        }

        private void StepPlayer(Organism player)
        {
            player.IsHibernating = false;

            // 1. 스킬 / 대시 발동 (Space)
            if (_playerSkillPressed)
            {
                _playerSkillPressed = false;
                if (player.AttackCooldown == 0)
                {
                    if (player.Species == EcoSpecies.Human)
                    {
                        // 4단계 인간(창): 사거리 54, 5단계 인간(활): 사거리 128 (+강공 특성 보너스)
                        bool isArcher = PlayerStage >= 5;
                        float pRange = (isArcher ? 128f : 54f) * (PlayerDamageMult > 1.05f ? 1.15f : 1.0f);
                        _organismGrid.QueryRadius(player.X, player.Y, pRange, _queryBuffer);
                        Organism bestTarget = null;
                        float bestD2 = pRange * pRange;
                        for (int i = 0; i < _queryBuffer.Count; i++)
                        {
                            var other = _organisms[_queryBuffer[i]];
                            if (!other.IsAlive || other.Id == player.Id) continue;
                            float dx = other.X - player.X;
                            float dy = other.Y - player.Y;
                            float d2 = dx * dx + dy * dy;
                            if (d2 < bestD2)
                            {
                                bestD2 = d2;
                                bestTarget = other;
                            }
                        }

                        player.AttackCooldown = Math.Max(5, (int)((isArcher ? 14 : 18) * PlayerCooldownMult));
                        if (bestTarget != null)
                        {
                            if (_hunterShots.Count < 32)
                                _hunterShots.Add(new HunterShot(player.X, player.Y, bestTarget.X, bestTarget.Y, isArcher ? 7 : 5));

                            if (bestTarget.Species == EcoSpecies.Bear)
                            {
                                bestTarget.Energy -= (isArcher ? 85f : 60f) * PlayerDamageMult;
                                WakeBearWithCharge(bestTarget);
                                if (bestTarget.Energy <= 0f)
                                {
                                    bestTarget.IsAlive = false;
                                    player.Kills++;
                                    if (player.MeatCount < MaxInventoryStack) player.MeatCount++;
                                    bool wasBoss = bestTarget.IsAlphaBoss;
                                    player.Energy = Math.Min(player.MaxEnergy, player.Energy + (wasBoss ? 260f : 130f) * PlayerGainMult);
                                    PlayerExp += (wasBoss ? 250f : 80f) * PlayerGainMult;
                                    if (wasBoss)
                                    {
                                        PlayerNotice = "위업 달성! [알파 곰 보스]를 처치했습니다!";
                                        UnlockAchievement("boss");
                                    }
                                }
                            }
                            else
                            {
                                bestTarget.IsAlive = false;
                                player.Kills++;
                                if (player.MeatCount < MaxInventoryStack) player.MeatCount++;
                                player.Energy = Math.Min(player.MaxEnergy, player.Energy + 65f * PlayerGainMult);
                                PlayerExp += 45f * PlayerGainMult;
                            }
                        }
                        else
                        {
                            float vLen = (float)Math.Sqrt(player.Vx * player.Vx + player.Vy * player.Vy);
                            float dirX = vLen > 0.01f ? player.Vx / vLen : 1f;
                            float dirY = vLen > 0.01f ? player.Vy / vLen : 0f;
                            if (_hunterShots.Count < 32)
                                _hunterShots.Add(new HunterShot(player.X, player.Y, player.X + dirX * pRange, player.Y + dirY * pRange, 5));
                        }
                    }
                    else
                    {
                        // 1~3단계: 순간 가속 대시 / 광폭 돌진
                        player.SkillActiveTicks = player.Species == EcoSpecies.Bear ? 20 : 12;
                        player.AttackCooldown = Math.Max(12, (int)((player.Species == EcoSpecies.Bear ? 75 : 42) * PlayerCooldownMult));
                    }
                }
            }

            // 2. 키보드 방향 입력 적용 (로그라이크 속도 특성 반영)
            float speedMult = (player.SkillActiveTicks > 0 ? (player.Species == EcoSpecies.Bear ? 1.85f : 1.75f) : 1.15f) * PlayerSpeedMult;
            float inLen = (float)Math.Sqrt(_playerInputX * _playerInputX + _playerInputY * _playerInputY);
            if (inLen > 0.01f)
            {
                float nx = _playerInputX / inLen;
                float ny = _playerInputY / inLen;
                player.Vx = nx * player.Dna.Speed * speedMult;
                player.Vy = ny * player.Dna.Speed * speedMult;
                MoveAndBounce(player);
            }
            else
            {
                // 키를 떼면 제자리에 멈춤 (방향만 유지)
                player.Vx *= 0.25f;
                player.Vy *= 0.25f;
            }

            // 3. 섭식 & 사냥 처리
            if (player.Species == EcoSpecies.Prey || player.Species == EcoSpecies.Bear)
            {
                _foodGrid.QueryRadius(player.X, player.Y, player.Dna.Size + 8f, _queryBuffer);
                float eatR = player.Dna.Size + 7f;
                for (int i = 0; i < _queryBuffer.Count; i++)
                {
                    int fIdx = _queryBuffer[i];
                    if (fIdx >= _foods.Count) continue;
                    var f = _foods[fIdx];
                    float dx = f.X - player.X;
                    float dy = f.Y - player.Y;
                    if (dx * dx + dy * dy <= eatR * eatR)
                    {
                        float mult = player.Species == EcoSpecies.Bear ? 1.35f : 1.15f;
                        player.Energy = Math.Min(player.MaxEnergy, player.Energy + f.EnergyValue * mult);
                        PlayerExp += player.Species == EcoSpecies.Prey ? 22f : 12f;
                        RemoveFoodSwapPop(fIdx);
                        break;
                    }
                }
            }
            else if (player.Species == EcoSpecies.Human)
            {
                TryGatherWood(player);
            }

            if (player.Species != EcoSpecies.Prey)
            {
                _organismGrid.QueryRadius(player.X, player.Y, player.Dna.Size + 16f, _queryBuffer);
                for (int i = 0; i < _queryBuffer.Count; i++)
                {
                    var other = _organisms[_queryBuffer[i]];
                    if (!other.IsAlive || other.Id == player.Id) continue;

                    bool canEat = false;
                    float expGain = 0f;
                    float energyGain = 0f;

                    if (player.Species == EcoSpecies.Predator && other.Species == EcoSpecies.Prey)
                    {
                        canEat = true; expGain = 45f; energyGain = 70f;
                    }
                    else if (player.Species == EcoSpecies.Bear && other.Species != EcoSpecies.Bear)
                    {
                        canEat = true;
                        expGain = other.Species == EcoSpecies.Human ? 110f : (other.Species == EcoSpecies.Predator ? 65f : 28f);
                        energyGain = other.Species == EcoSpecies.Human ? 140f : 85f;
                    }
                    else if (player.Species == EcoSpecies.Human &&
                             (other.Species == EcoSpecies.Prey || other.Species == EcoSpecies.Predator))
                    {
                        // 인간은 여우/토끼만 근접 제압 가능 (곰은 Space 창/활로만 상대해야 하며 몸통 박치기 즉사 불가)
                        canEat = true; expGain = 45f; energyGain = 65f;
                    }

                    if (!canEat) continue;

                    float catchR = player.Dna.Size + other.Dna.Size * 0.85f;
                    float dx = other.X - player.X;
                    float dy = other.Y - player.Y;
                    if (dx * dx + dy * dy <= catchR * catchR)
                    {
                        other.IsAlive = false;
                        player.Kills++;
                        if (player.Species == EcoSpecies.Human && player.MeatCount < MaxInventoryStack) player.MeatCount++;
                        player.Energy = Math.Min(player.MaxEnergy, player.Energy + energyGain);
                        PlayerExp += expGain;
                        break; // 한 틱에 1마리만 사냥하여 겹친 개체로 인한 순식간 2단 진화 방지
                    }
                }
            }

            // 곰 3단계일 때 모닥불 부수기
            if (player.Species == EcoSpecies.Bear && _campfires.Count > 0)
            {
                int cfIdx = FindNearestCampfire(player.X, player.Y, player.Dna.Size + 18f);
                if (cfIdx >= 0)
                {
                    var cf = _campfires[cfIdx];
                    cf.Hp -= 35f;
                    if (cf.Hp <= 0f)
                    {
                        RemoveCampfireSwapPop(cfIdx);
                        PlayerExp += 45f;
                    }
                    else
                    {
                        _campfires[cfIdx] = cf;
                    }
                }
            }

            // 3-2. 플레이어 인간: 모닥불 건설(F) & 모닥불 보온/고기 회복
            bool buildFirePressed = _playerBuildCampfirePressed;
            _playerBuildCampfirePressed = false;
            bool playerNearCampfire = false;
            if (player.Species == EcoSpecies.Human)
            {
                if (buildFirePressed)
                {
                    if (player.WoodCount < CampfireWoodCost)
                        PlayerNotice = $"모닥불 건설에 나뭇가지 {CampfireWoodCost}개가 필요합니다. (보유: {player.WoodCount})";
                    else if (!TryBuildCampfireWithWood(player, true))
                        PlayerNotice = "주변에 이미 모닥불이 있거나 최대 개수에 도달했습니다.";
                }
                playerNearCampfire = FindNearestCampfire(player.X, player.Y, 72f) >= 0;
                if (playerNearCampfire && player.Energy > 0f)
                {
                    player.Energy = Math.Min(player.MaxEnergy, player.Energy + 0.35f);
                }
            }

            // 4. 생존 대사 & 생존 경험치
            float drainMult = inLen > 0.01f ? 0.82f : 0.35f;
            if (playerNearCampfire) drainMult *= 0.65f;
            player.Energy -= player.Dna.ComputeMetabolicCost(player.Species) * drainMult;
            if (player.Species == EcoSpecies.Human)
            {
                TryConsumeMeatForRecovery(player, playerNearCampfire, true);
            }
            if (player.Energy <= 0f)
            {
                player.IsAlive = false;
                return;
            }

            PlayerExp += 0.16f;
            TryEvolvePlayer(player);
        }

        private void TryEvolvePlayer(Organism player)
        {
            if (PlayerStage >= 5 || PlayerExp < PlayerNextExp) return;

            PlayerStage++;
            if (PlayerStage > PlayerBestStage) PlayerBestStage = PlayerStage;
            PlayerExp = 0f;

            switch (PlayerStage)
            {
                case 2:
                    player.Species = EcoSpecies.Predator;
                    player.Dna = new Genome(3.05f, 125f, 8.6f);
                    PlayerNextExp = 320f;
                    PlayerNotice = "진화 성공! [2단계: 육식(여우)]";
                    break;
                case 3:
                    player.Species = EcoSpecies.Bear;
                    player.Dna = new Genome(2.75f, 135f, 12.2f);
                    PlayerNextExp = 520f;
                    PlayerNotice = "진화 성공! [3단계: 최상위 곰]";
                    break;
                case 4:
                    player.Species = EcoSpecies.Human;
                    player.Dna = new Genome(2.85f, 145f, 9.2f);
                    PlayerNextExp = 480f;
                    PlayerNotice = "진화 성공! [4단계: 인간(창)]";
                    break;
                case 5:
                    player.Species = EcoSpecies.Human;
                    player.Dna = new Genome(2.95f, 160f, 9.5f);
                    PlayerNextExp = 999f;
                    PlayerNotice = "최종 진화 달성! [5단계: 인간(활)]";
                    break;
            }

            player.MaxEnergy = ComputeMaxEnergy(player.Species, player.Dna) * PlayerVitalityMult;
            player.Energy = player.MaxEnergy * 0.85f;
            player.SkillActiveTicks = 0;
            player.AttackCooldown = 0;
            PendingPerkChoice = true;

            if (PlayerStage >= 2) UnlockAchievement("evo2");
            if (PlayerStage >= 3) UnlockAchievement("evo3");
            if (PlayerStage >= 5) UnlockAchievement("evo5");
        }
    }
}
