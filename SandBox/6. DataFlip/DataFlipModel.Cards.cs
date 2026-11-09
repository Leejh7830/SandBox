using System;
using System.Collections.Generic;
using System.Drawing;

namespace GateHelper.DataFlip
{
    public partial class CoreFrontGameState
    {
        #region Roguelike Protocol Card System (영구 누적 & 신규 유닛 적용)

        public void TriggerRoguelikeCardSelection()
        {
            IsCardSelectionActive = true;
            CurrentOfferedCards = GenerateRandomCards(3, Owner.Player);
            StatusMessage = "⚡ 전술 프로토콜 보급 도착! 3개 중 하나를 선택하십시오.";
        }

        public void SelectProtocolCard(ProtocolCard card)
        {
            if (card != null)
            {
                card.ApplyAction?.Invoke(this, Owner.Player);

                // 유닛 특수 카드 선택 여부 추적 (순수 군략 도전과제)
                string cid = card.Id;
                if (cid == "VANGUARD_CALL" || cid == "AIR_DROP" || cid == "TITAN_PROTOCOL" || cid == "INFANTRY_ASSAULT" ||
                    cid == "NANO_ARMOR" || cid == "RAILGUN_BATTERY" || cid == "PHALANX_SHIELD" || cid == "STEALTH_CAMOUFLAGE" ||
                    cid == "COLOSSUS_SIEGE" || cid == "COLOSSUS_MELEE" || cid == "MARTYRDOM_CORE" || cid == "CHAIN_PLASMA" ||
                    cid == "KINETIC_SLAM" || cid == "CHEONGUNG_TRAJECTORY")
                {
                    HasSelectedUnitCard = true;
                }

                // AI도 랜덤하게 1개 버프 획득 (공정한 게임 밸런스)
                var aiPool = GenerateRandomCards(3, Owner.Ai);
                if (aiPool.Count > 0)
                {
                    aiPool[_rand.Next(aiPool.Count)].ApplyAction?.Invoke(this, Owner.Ai);
                }
            }

            IsCardSelectionActive = false;
            CurrentOfferedCards.Clear();
            StatusMessage = $"프로토콜 '{card?.Title}' 적용 완료! 턴 {TurnNumber}을 이어가세요.";
        }

        public List<ProtocolCard> GetAllProtocolCardTemplates()
        {
            // [카드 동적 스케일링 방안 A]: 턴 수에 따라 카드의 가치와 효과 스케일링 강화
            int ocCredit = (this.TurnNumber >= 12) ? 14 : 8;
            string ocTitle = (this.TurnNumber >= 12) ? "⚡ 초대형 오버클럭 펀딩" : "⚡ 오버클럭 펀딩";
            string ocDesc = $"국방 예산 크레딧 +{ocCredit}G를\n즉시 긴급 수급합니다.";

            int vCount = (this.TurnNumber >= 12) ? 4 : (this.TurnNumber >= 7 ? 3 : 2);
            int vBaseAtk = (this.TurnNumber >= 12) ? 2 : 1;
            int vBaseHp = (this.TurnNumber >= 12) ? 4 : 3;
            bool vShield = (this.TurnNumber >= 7);
            string vTitle = (this.TurnNumber >= 12) ? "👥 정예 보병 코만도 강습" : (this.TurnNumber >= 7 ? "👥 보병 강화 증원" : "👥 보병 기동 증원");
            string vSub = (this.TurnNumber >= 12) ? "정예 4기 (기본HP4, 공2, 실드+패시브)" : (this.TurnNumber >= 7 ? "보병 3기 (실드 장착+패시브)" : "기본 보병 2기 지정 배치");
            string vDesc = (this.TurnNumber >= 12)
                ? "본진 진영(Col 1~3)에 정예 코만도 4기를\n즉시 무료 공수 투하합니다.\n• 기본 HP 4, 공격력 2, 1회 전술 실드\n• 보유 중인 유닛 강화(HP/공격력) 100% 누적 합산"
                : (this.TurnNumber >= 7
                    ? "본진 진영(Col 1~3)에 방패를 장착한\n보병 3기를 즉시 무료 공수합니다.\n• 소환 시 1회 방패 장착\n• 보유 중인 유닛 강화(HP/공격력) 100% 누적 합산"
                    : "본진 진영(Col 1~3) 원하는 타일에\n기본 보병 2기를 즉시 무료 공수합니다.\n• 보유 중인 유닛 강화(HP/공격력) 100% 누적 합산");

            int crHeal = (this.TurnNumber >= 12) ? 10 : 6;
            string crTitle = (this.TurnNumber >= 12) ? "🔧 대규모 코어 오버홀" : "🔧 긴급 코어 리부트";
            string crSub = $"본진 코어 HP +{crHeal} 즉시 수리";
            string crDesc = $"손상된 아군 메인프레임 코어를 복구하여\n본진 체력을 즉시 +{crHeal} 회복합니다.\n(최대 체력 한도 내 안전 수리)";

            int adCount = (this.TurnNumber >= 12) ? 3 : 2;
            int adMaxCol = (this.TurnNumber >= 12) ? 9 : 8;
            string adTitle = (this.TurnNumber >= 12) ? "🚀 대규모 스텔스 침투 공수" : "🚀 스텔스 침투 공수";
            string adSub = (this.TurnNumber >= 12) ? "전장 중앙/후방 3기 기습 투하" : "전장 중앙 기습 투하 (지정 배치)";
            string adDesc = $"전장 침투선(Col 4~{adMaxCol})에 고속 침투병\n{adCount}기를 원하는 위치에 공수 투하합니다.\n• 카드 선택 후 원하는 타일 클릭";

            return new List<ProtocolCard>
            {
                // ================= [COMMON: 일반 등급 (50%)] =================
                new ProtocolCard
                {
                    Id = "OVERCLOCK",
                    Title = ocTitle,
                    Subtitle = $"즉시 긴급 군자금 +{ocCredit}G",
                    Description = ocDesc,
                    AccentColor = Color.FromArgb(200, 215, 230),
                    Rarity = CardRarity.Common,
                    ApplyAction = (state, owner) =>
                    {
                        if (owner == Owner.Player) state.PlayerCredit += ocCredit;
                        else state.AiCredit += ocCredit;
                    }
                },
                new ProtocolCard
                {
                    Id = "VANGUARD_CALL",
                    Title = vTitle,
                    Subtitle = vSub,
                    Description = vDesc,
                    AccentColor = Color.FromArgb(100, 210, 255),
                    Rarity = CardRarity.Common,
                    ApplyAction = (state, owner) =>
                    {
                        if (owner == Owner.Player)
                        {
                            state.PendingDropUnitType = UnitType.Vanguard;
                            state.PendingDropRemainingCount = vCount;
                            state.PendingDropMinCol = 1;
                            state.PendingDropMaxCol = 3;
                            state.PendingDropCustomHp = vBaseHp;
                            state.PendingDropCustomAtk = vBaseAtk;
                            state.PendingDropForceShield = vShield;
                            state.StatusMessage = $"⚡ 보병 {vCount}기를 배치할 위치를 클릭하십시오 (Col 1~3)";
                        }
                        else
                        {
                            int c = Cols - 2;
                            int spawnedCount = 0;
                            state.PendingDropCustomHp = vBaseHp;
                            state.PendingDropCustomAtk = vBaseAtk;
                            state.PendingDropForceShield = vShield;
                            for (int r = 0; r < Rows && spawnedCount < vCount; r++)
                            {
                                if (!state.Units.Exists(u => u.Col == c && u.Row == r) && state.Buildings[r, c]?.Type != BuildingType.Wall)
                                {
                                    state.SpawnUnitDirect(c, r, UnitType.Vanguard, Owner.Ai);
                                    spawnedCount++;
                                }
                            }
                            state.PendingDropCustomHp = 0;
                            state.PendingDropCustomAtk = 0;
                            state.PendingDropForceShield = false;
                        }
                    }
                },
                new ProtocolCard
                {
                    Id = "CORE_REPAIR",
                    Title = crTitle,
                    Subtitle = crSub,
                    Description = crDesc,
                    AccentColor = Color.FromArgb(76, 175, 80),
                    Rarity = CardRarity.Common,
                    ApplyAction = (state, owner) =>
                    {
                        if (owner == Owner.Player) state.PlayerCoreHp = Math.Min(state.PlayerCoreMaxHp, state.PlayerCoreHp + crHeal);
                        else state.AiCoreHp = Math.Min(state.AiCoreMaxHp, state.AiCoreHp + crHeal);
                    }
                },
                new ProtocolCard
                {
                    Id = "GREEN_ENERGY",
                    Title = "🔋 초전도 국고 예산",
                    Subtitle = "턴당 기본 수입 영구 +1G",
                    Description = "매 턴 국고로 지급되는 기본 크레딧이\n영구적으로 +1G 증가합니다.\n(발전소 수와 무관하게 턴마다 영구 지급)",
                    AccentColor = Color.FromArgb(76, 175, 80),
                    Rarity = CardRarity.Common,
                    ApplyAction = (state, owner) =>
                    {
                        if (owner == Owner.Player) state.PlayerIncomeBonus += 1;
                        else state.AiIncomeBonus += 1;
                    }
                },
                new ProtocolCard
                {
                    Id = "BARRICADE_AIRDROP",
                    Title = "🧱 긴급 격벽 투하",
                    Subtitle = "전진 방화벽 즉시 공수",
                    Description = "전선 길목(Col 3)에 HP 8 방화벽을\n즉시 1기 무료 공수 건설합니다.",
                    AccentColor = Color.FromArgb(160, 180, 205),
                    Rarity = CardRarity.Common,
                    ApplyAction = (state, owner) =>
                    {
                        int dropCol = (owner == Owner.Player) ? 3 : Cols - 4;
                        for (int r = 2; r < Rows - 2; r++)
                        {
                            if (state.Buildings[r, dropCol] == null && state.MapTiles[r, dropCol] != TileType.Obstacle)
                            {
                                state.Buildings[r, dropCol] = new Building
                                {
                                    Owner = owner,
                                    Type = BuildingType.Wall,
                                    Hp = 8,
                                    MaxHp = 8,
                                    Col = dropCol,
                                    Row = r
                                };
                                break;
                            }
                        }
                    }
                },

                // ================= [RARE: 희귀 등급 (35%)] =================
                new ProtocolCard
                {
                    Id = "CHEONGUNG_TRAJECTORY",
                    Title = "🔭 탄도 궤적 정밀 연산",
                    Subtitle = "사거리 4->5 & 사각 3칸 (고유)",
                    Description = "천궁 정밀 탄도 연산 컴퓨터를 동기화합니다.\n• 최대 사거리 +1 (총 5칸 초장거리 포격)\n• 안전 최소 사거리 +1 (3칸 이내 근접 불가)",
                    AccentColor = Color.FromArgb(255, 87, 34),
                    Rarity = CardRarity.Rare,
                    MinTurn = 9,
                    IsAvailable = (state, owner) => (owner == Owner.Player ? !state.PlayerHasCheongungTrajectory : !state.AiHasCheongungTrajectory),
                    ApplyAction = (state, owner) =>
                    {
                        if (owner == Owner.Player) state.PlayerHasCheongungTrajectory = true;
                        else state.AiHasCheongungTrajectory = true;
                        state.Units.FindAll(u => u.Owner == owner && u.Type == UnitType.Cheongung).ForEach(u => {
                            u.Range = 5;
                            u.MinRange = 3;
                        });
                    }
                },
                new ProtocolCard
                {
                    Id = "CORE_EXPANSION",
                    Title = "🛡️ 메인프레임 코어 증설",
                    Subtitle = "코어 최대 HP +5 & 즉시 수리",
                    Description = "아군 메인프레임 코어 내구도를 증설합니다.\n• 최대 체력 영구 +5 확장\n• 현재 코어 HP +5 즉시 긴급 수리",
                    AccentColor = Color.FromArgb(0, 230, 118),
                    Rarity = CardRarity.Rare,
                    ApplyAction = (state, owner) =>
                    {
                        if (owner == Owner.Player)
                        {
                            state.PlayerCoreMaxHp += 5;
                            state.PlayerCoreHp = Math.Min(state.PlayerCoreMaxHp, state.PlayerCoreHp + 5);
                        }
                        else
                        {
                            state.AiCoreMaxHp += 5;
                            state.AiCoreHp = Math.Min(state.AiCoreMaxHp, state.AiCoreHp + 5);
                        }
                    }
                },
                new ProtocolCard
                {
                    Id = "NANO_ARMOR",
                    Title = "🛡️ 나노 합금 연구",
                    Subtitle = "생산 유닛 체력 영구 +2 (누적)",
                    Description = "향후 새로 생산되는 모든 아군 유닛의\n최대 체력이 영구적으로 +2 증가합니다.",
                    AccentColor = Color.FromArgb(0, 230, 118),
                    Rarity = CardRarity.Rare,
                    ApplyAction = (state, owner) =>
                    {
                        if (owner == Owner.Player) state.PlayerUnitHpBonus += 2;
                        else state.AiUnitHpBonus += 2;
                    }
                },
                new ProtocolCard
                {
                    Id = "CYBER_DDOS",
                    Title = "👾 DDoS 사이버 교란",
                    Subtitle = "상대 수입 -1G & 자금 3G 강탈",
                    Description = "적 통신망을 마비시켜 수입을 억제합니다.\n• 상대 턴당 수입 영구 -1G 차감\n• 적 국고에서 3G 즉시 강탈",
                    AccentColor = Color.FromArgb(255, 61, 0),
                    Rarity = CardRarity.Rare,
                    ApplyAction = (state, owner) =>
                    {
                        Owner enemy = (owner == Owner.Player) ? Owner.Ai : Owner.Player;
                        if (enemy == Owner.Ai)
                        {
                            state.AiIncomeBonus = Math.Max(-2, state.AiIncomeBonus - 1);
                            int stolen = Math.Min(3, state.AiCredit);
                            state.AiCredit -= stolen;
                            state.PlayerCredit += stolen;
                        }
                        else
                        {
                            state.PlayerIncomeBonus = Math.Max(-2, state.PlayerIncomeBonus - 1);
                            int stolen = Math.Min(3, state.PlayerCredit);
                            state.PlayerCredit -= stolen;
                            state.AiCredit += stolen;
                        }
                    }
                },
                new ProtocolCard
                {
                    Id = "RAILGUN_BATTERY",
                    Title = "🎯 레일건 포탑 개조",
                    Subtitle = "포탑 사거리 +1칸 (최대 2회)",
                    Description = "모든 포탑의 요격 사거리가\n+1칸(최대 4칸) 증가합니다.",
                    AccentColor = Color.FromArgb(0, 229, 255),
                    Rarity = CardRarity.Rare,
                    IsAvailable = (state, owner) => state.TurretRangeBonus < 2, // 최대 2회 도달 시 풀에서 제외
                    ApplyAction = (state, owner) =>
                    {
                        if (owner == Owner.Player) state.TurretRangeBonus += 1;
                    }
                },
                new ProtocolCard
                {
                    Id = "TURRET_RADAR",
                    Title = "📡 양자 레이더 망",
                    Subtitle = "포탑 은폐 감지 센서 (고유)",
                    Description = "모든 포탑에 양자 레이더를 장착하여\n은폐 침투병을 원거리에서 요격합니다.",
                    AccentColor = Color.FromArgb(0, 200, 255),
                    Rarity = CardRarity.Rare,
                    IsAvailable = (state, owner) => (owner == Owner.Player ? !state.PlayerHasTurretRadar : !state.AiHasTurretRadar),
                    ApplyAction = (state, owner) =>
                    {
                        if (owner == Owner.Player) state.PlayerHasTurretRadar = true;
                        else state.AiHasTurretRadar = true;
                    }
                },
                new ProtocolCard
                {
                    Id = "PHALANX_SHIELD",
                    Title = "🛡️ 전술 방패 결진",
                    Subtitle = "보병 1회 피해 무효화 실드 (고유)",
                    Description = "향후 생산/증원되는 모든 보병이\n첫 1회 피해 무효화 실드를 장착합니다.",
                    AccentColor = Color.FromArgb(64, 196, 255),
                    Rarity = CardRarity.Rare,
                    IsAvailable = (state, owner) => (owner == Owner.Player ? !state.PlayerHasPhalanxShield : !state.AiHasPhalanxShield),
                    ApplyAction = (state, owner) =>
                    {
                        if (owner == Owner.Player) state.PlayerHasPhalanxShield = true;
                        else state.AiHasPhalanxShield = true;
                    }
                },
                new ProtocolCard
                {
                    Id = "STEALTH_CAMOUFLAGE",
                    Title = "⚡ 광학 스텔스 클로킹",
                    Subtitle = "침투병 첫 공격 시까지 은폐 (고유)",
                    Description = "침투병이 적 앞 칸에 도달할 때까지\n원거리 포격 및 포탑 타깃팅을 회피합니다.",
                    AccentColor = Color.FromArgb(38, 198, 218),
                    Rarity = CardRarity.Rare,
                    MinTurn = 6, // 2차 카드(턴 6)부터 풀에 합류!
                    IsAvailable = (state, owner) => (owner == Owner.Player ? !state.PlayerHasStealthCloak : !state.AiHasStealthCloak),
                    ApplyAction = (state, owner) =>
                    {
                        if (owner == Owner.Player) state.PlayerHasStealthCloak = true;
                        else state.AiHasStealthCloak = true;
                    }
                },
                new ProtocolCard
                {
                    Id = "COLOSSUS_SIEGE",
                    Title = "🔭 시즈 앵커 프로토콜",
                    Subtitle = "콜로서스 제자리 포격 체제 (고유)",
                    Description = "사거리(2~3칸) 내 타깃이 존재하면\n전진을 멈추고 제자리에서 포격합니다.",
                    AccentColor = Color.FromArgb(186, 104, 200),
                    Rarity = CardRarity.Rare,
                    MinTurn = 12, // 4차 카드(턴 12)부터 풀에 합류!
                    IsAvailable = (state, owner) => (owner == Owner.Player ? !state.PlayerHasSiegeAnchor : !state.AiHasSiegeAnchor),
                    ApplyAction = (state, owner) =>
                    {
                        if (owner == Owner.Player) state.PlayerHasSiegeAnchor = true;
                        else state.AiHasSiegeAnchor = true;
                    }
                },
                new ProtocolCard
                {
                    Id = "COLOSSUS_MELEE",
                    Title = "⚔️ 근접 방어 발칸포",
                    Subtitle = "콜로서스 사각지대 해제 (고유)",
                    Description = "콜로서스에 근접 자동 발칸포를 탑재하여\n인접 1칸 근접 공격 및 반격이 가능해집니다.\n(최소 사거리 1칸으로 완화)",
                    AccentColor = Color.FromArgb(255, 179, 0),
                    Rarity = CardRarity.Rare,
                    MinTurn = 12, // 4차 카드(턴 12)부터 풀에 합류!
                    IsAvailable = (state, owner) => (owner == Owner.Player ? !state.PlayerHasColossusMelee : !state.AiHasColossusMelee),
                    ApplyAction = (state, owner) =>
                    {
                        if (owner == Owner.Player) state.PlayerHasColossusMelee = true;
                        else state.AiHasColossusMelee = true;
                        state.Units.FindAll(u => u.Owner == owner && u.Type == UnitType.Colossus).ForEach(u => u.MinRange = 1);
                    }
                },

                // ================= [EPIC: 에픽 등급 (15%)] =================
                new ProtocolCard
                {
                    Id = "GRID_EXPANSION",
                    Title = "⚡ 전력망 그리드 확장",
                    Subtitle = "발전소 한도 +2기 & 3G 환급",
                    Description = "발전소 보유 한도가 영구 +2기 확장되며\n즉시 3G 보조금을 지급받습니다.",
                    AccentColor = Color.FromArgb(255, 215, 0),
                    Rarity = CardRarity.Epic,
                    ApplyAction = (state, owner) =>
                    {
                        if (owner == Owner.Player)
                        {
                            state.PlayerPlantLimit += 2;
                            state.PlayerCredit += 3;
                        }
                        else
                        {
                            state.AiPlantLimit += 2;
                            state.AiCredit += 3;
                        }
                    }
                },
                new ProtocolCard
                {
                    Id = "AIR_DROP",
                    Title = adTitle,
                    Subtitle = adSub,
                    Description = adDesc,
                    AccentColor = Color.FromArgb(0, 229, 255),
                    Rarity = CardRarity.Epic,
                    MinTurn = 6, // 2차 카드(턴 6)부터 풀에 합류!
                    ApplyAction = (state, owner) =>
                    {
                        if (owner == Owner.Player)
                        {
                            state.PendingDropUnitType = UnitType.Raider;
                            state.PendingDropRemainingCount = adCount;
                            state.PendingDropMinCol = 4;
                            state.PendingDropMaxCol = adMaxCol;
                            state.StatusMessage = $"🚀 침투병 {adCount}기를 공수 투하할 위치를 클릭하십시오 (Col 4~{adMaxCol})";
                        }
                        else
                        {
                            int targetCol = (state.TurnNumber >= 12) ? 6 : 7;
                            int spawnedCount = 0;
                            for (int r = 1; r < Rows && spawnedCount < adCount; r += 2)
                            {
                                if (!state.Units.Exists(u => u.Col == targetCol && u.Row == r) && state.MapTiles[r, targetCol] != TileType.Obstacle)
                                {
                                    state.SpawnUnitDirect(targetCol, r, UnitType.Raider, Owner.Ai);
                                    spawnedCount++;
                                }
                            }
                        }
                    }
                },
                new ProtocolCard
                {
                    Id = "ORBITAL_EMP",
                    Title = "💥 궤도 EMP 폭격",
                    Subtitle = "전체 적 유닛 2 피해",
                    Description = "강력한 전자기 펄스 폭격을 투하하여\n현재 전장의 모든 적 유닛에게 2 피해를 입힙니다.\n(실드 장착 시 실드만 파괴)",
                    AccentColor = Color.FromArgb(255, 23, 68),
                    Rarity = CardRarity.Epic,
                    ApplyAction = (state, owner) =>
                    {
                        Owner enemy = (owner == Owner.Player) ? Owner.Ai : Owner.Player;
                        state.Units.FindAll(u => u.Owner == enemy).ForEach(u => {
                            if (u.HasShield)
                            {
                                u.HasShield = false;
                            }
                            else
                            {
                                u.Hp -= 2;
                            }
                            u.HitFlashAlpha = 1.0f;
                        });
                        state.Units.RemoveAll(u => u.Hp <= 0);
                    }
                },
                new ProtocolCard
                {
                    Id = "TITAN_PROTOCOL",
                    Title = "⚙️ 타이탄 기동 명령",
                    Subtitle = "최종 결전 병기 무료 소환 (지정 배치)",
                    Description = "중장갑 결전 병기 '타이탄' 1기를\n원하는 본진 타일에 즉시 무료 소환합니다.\n(카드 선택 후 원하는 타일 클릭)",
                    AccentColor = Color.FromArgb(255, 110, 64),
                    Rarity = CardRarity.Epic,
                    MinTurn = 9, // 3차 카드(턴 9)부터 풀에 합류!
                    ApplyAction = (state, owner) =>
                    {
                        if (owner == Owner.Player)
                        {
                            state.PendingDropUnitType = UnitType.Titan;
                            state.PendingDropRemainingCount = 1;
                            state.PendingDropMinCol = 1;
                            state.PendingDropMaxCol = 3;
                            state.StatusMessage = "⚙️ 타이탄 1기를 배치할 위치를 클릭하십시오 (Col 1~3)";
                        }
                        else
                        {
                            int c = Cols - 2;
                            for (int r = 0; r < Rows; r++)
                            {
                                if (!state.Units.Exists(u => u.Col == c && u.Row == r))
                                {
                                    state.SpawnUnitDirect(c, r, UnitType.Titan, Owner.Ai);
                                    break;
                                }
                            }
                        }
                    }
                },
                new ProtocolCard
                {
                    Id = "INFANTRY_ASSAULT",
                    Title = "🌊 전면 보병 상륙작전",
                    Subtitle = "코어 앞 전 행 보병 동시 상륙",
                    Description = "본진 제1열(Col 1)의 모든 빈 타일에\n정예 보병을 일제히 동시 상륙 배치합니다!\n• 보유 중인 보병 특성(HP/공/실드) 온전 적용",
                    AccentColor = Color.FromArgb(0, 229, 255),
                    Rarity = CardRarity.Epic,
                    MinTurn = 6, // 턴 6부터 등장
                    ApplyAction = (state, owner) =>
                    {
                        int targetCol = (owner == Owner.Player) ? 1 : state.Cols - 2;
                        for (int r = 0; r < state.Rows; r++)
                        {
                            if (state.ObstacleHp[r, targetCol] <= 0 && state.Buildings[r, targetCol] == null && !state.Units.Exists(u => u.Col == targetCol && u.Row == r))
                            {
                                state.SpawnUnitDirect(targetCol, r, UnitType.Vanguard, owner);
                            }
                        }
                    }
                },
                new ProtocolCard
                {
                    Id = "KINETIC_SLAM",
                    Title = "💥 운동에너지 충격파",
                    Subtitle = "타이탄 1칸 넉백 & 충돌 피해 (고유)",
                    Description = "타이탄 포격 적중 시 목표를 뒤로 1칸 밀쳐냅니다. 뒤에 유닛/건물/벽이 있어 밀려나지 못하면 양쪽 모두에게 충돌 데미지 1을 가합니다.",
                    AccentColor = Color.FromArgb(255, 138, 101),
                    Rarity = CardRarity.Epic,
                    MinTurn = 9, // 3차 카드(턴 9)부터 풀에 합류!
                    IsAvailable = (state, owner) => (owner == Owner.Player ? !state.PlayerHasKineticSlam : !state.AiHasKineticSlam),
                    ApplyAction = (state, owner) =>
                    {
                        if (owner == Owner.Player) state.PlayerHasKineticSlam = true;
                        else state.AiHasKineticSlam = true;
                    }
                },
                new ProtocolCard
                {
                    Id = "CHAIN_PLASMA",
                    Title = "✦ 연쇄 플라즈마 방전",
                    Subtitle = "드레드노트 십자 4방향 스플래시 (고유)",
                    Description = "플라즈마 드레드노트 주 타격 시 상/하/좌/우 십자 4방향에 2 데미지 연쇄 방전 피해를 입힙니다.",
                    AccentColor = Color.FromArgb(0, 229, 255),
                    Rarity = CardRarity.Epic,
                    MinTurn = 12, // 4차 카드(턴 12)부터 풀에 합류!
                    IsAvailable = (state, owner) => (owner == Owner.Player ? !state.PlayerHasChainPlasma : !state.AiHasChainPlasma),
                    ApplyAction = (state, owner) =>
                    {
                        if (owner == Owner.Player) state.PlayerHasChainPlasma = true;
                        else state.AiHasChainPlasma = true;
                    }
                },
                new ProtocolCard
                {
                    Id = "QUANTUM_OVERCLOCK",
                    Title = "🔮 퀀텀 뉴럴 오버클럭",
                    Subtitle = "생산 유닛 공격력 영구 +1 (누적)",
                    Description = "향후 새로 생산되는 모든 아군 유닛의 공격력이 영구적으로 +1 증가합니다.",
                    AccentColor = Color.FromArgb(215, 60, 255),
                    Rarity = CardRarity.Epic,
                    ApplyAction = (state, owner) =>
                    {
                        if (owner == Owner.Player) state.PlayerUnitAtkBonus += 1;
                        else state.AiUnitAtkBonus += 1;
                    }
                },
                new ProtocolCard
                {
                    Id = "MARTYRDOM_CORE",
                    Title = "💣 최후의 자폭 코어",
                    Subtitle = "현재 배치된 아군 유닛 전원 자폭 장착",
                    Description = "현재 전장에 배치된 모든 아군 유닛에게\n자폭 폭탄을 장착합니다.\n• 유닛 사망 시 상하좌우 4방향 적에게 2 데미지\n• 아군 피해 없음 (적 기물/코어만 타격)",
                    AccentColor = Color.FromArgb(255, 23, 68),
                    Rarity = CardRarity.Rare,
                    ApplyAction = (state, owner) =>
                    {
                        state.Units.FindAll(u => u.Owner == owner && u.Hp > 0).ForEach(u => u.HasBomb = true);
                    }
                },
                new ProtocolCard
                {
                    Id = "OVERCLOCK_REACTOR",
                    Title = "⚡ 전력과부하 리액터",
                    Subtitle = "발전소 1기 희생 ➔ 전 유닛 데미지 +1",
                    Description = "과부하 에너지를 모든 기동 병기에 주입합니다.\n• 보유 발전소 1기 무작위 폭파\n• 현재/향후 모든 아군 유닛 공격력 +1 영구 증가",
                    AccentColor = Color.FromArgb(255, 152, 0),
                    Rarity = CardRarity.Epic,
                    IsAvailable = (state, owner) => state.GetBuildingCount(BuildingType.PowerPlant, owner) >= 3,
                    ApplyAction = (state, owner) =>
                    {
                        var plants = new List<Building>();
                        for (int r = 0; r < state.Rows; r++)
                        {
                            for (int c = 0; c < state.Cols; c++)
                            {
                                var b = state.Buildings[r, c];
                                if (b != null && b.Owner == owner && b.Type == BuildingType.PowerPlant)
                                    plants.Add(b);
                            }
                        }
                        if (plants.Count > 0)
                        {
                            var targetPlant = plants[_rand.Next(plants.Count)];
                            state.Buildings[targetPlant.Row, targetPlant.Col] = null;
                        }
                        if (owner == Owner.Player) state.PlayerUnitAtkBonus += 1;
                        else state.AiUnitAtkBonus += 1;
                        state.Units.FindAll(u => u.Owner == owner).ForEach(u => u.Atk += 1);
                    }
                },
                new ProtocolCard
                {
                    Id = "GLOBAL_NANO_RESTORATION",
                    Title = "🔧 전역 나노 복구 네트워크",
                    Subtitle = "전장 모든 아군 건물 & 유닛 체력 +3 수리",
                    Description = "전역 나노 복구 펄스를 방출하여\n전장의 모든 아군 건물 체력을 +3 수리하고\n모든 아군 유닛의 체력을 +3 회복합니다.",
                    AccentColor = Color.FromArgb(76, 175, 80),
                    Rarity = CardRarity.Rare,
                    ApplyAction = (state, owner) =>
                    {
                        for (int r = 0; r < state.Rows; r++)
                        {
                            for (int c = 0; c < state.Cols; c++)
                            {
                                var b = state.Buildings[r, c];
                                if (b != null && b.Owner == owner && b.Hp > 0)
                                {
                                    b.Hp = Math.Min(b.MaxHp, b.Hp + 3);
                                    b.HitFlashAlpha = 0.8f;
                                }
                            }
                        }
                        state.Units.FindAll(u => u.Owner == owner && u.Hp > 0).ForEach(u =>
                        {
                            u.Hp = Math.Min(u.MaxHp, u.Hp + 3);
                            u.HitFlashAlpha = 0.8f;
                        });
                    }
                }
            };
        }

        public List<ProtocolCard> GenerateRandomCardsDirect(int count, Owner targetOwner = Owner.Player) => GenerateRandomCards(count, targetOwner);

        private List<ProtocolCard> GenerateRandomCards(int count, Owner targetOwner = Owner.Player)
        {
            var allCards = GetAllProtocolCardTemplates();
            // 획득 가능 조건 만족 카드만 1차 필터링:
            // 1) 턴 수에 따른 유닛 티어 단계적 해금 (보병: 턴3~, 침투병: 턴6~, 타이탄: 턴9~, 콜로서스/드레드노트: 턴12~)
            // 2) 한도 도달 및 이미 해금된 고유 기술 제외
            var availableCards = allCards.FindAll(c => 
                this.TurnNumber >= c.MinTurn && 
                (c.IsAvailable == null || c.IsAvailable(this, targetOwner))
            );
            var result = new List<ProtocolCard>();

            // 가중치 확률 추첨: Common 50%, Rare 35%, Epic 15%
            for (int i = 0; i < count; i++)
            {
                int roll = _rand.Next(100); // 0 ~ 99
                CardRarity targetRarity;
                if (roll < 50) targetRarity = CardRarity.Common;       // 50%
                else if (roll < 85) targetRarity = CardRarity.Rare;    // 35%
                else targetRarity = CardRarity.Epic;                   // 15%

                // 후보군 필터링 (이미 선택지에 추가된 카드 중복 제외)
                var candidates = availableCards.FindAll(c => c.Rarity == targetRarity && !result.Exists(r => r.Id == c.Id));

                // 만약 해당 등급 풀이 고갈되었으면 다른 등급의 사용 가능한 미선택 카드에서 대체
                if (candidates.Count == 0)
                {
                    candidates = availableCards.FindAll(c => !result.Exists(r => r.Id == c.Id));
                }

                if (candidates.Count > 0)
                {
                    int pickIdx = _rand.Next(candidates.Count);
                    result.Add(candidates[pickIdx]);
                }
            }

            return result;
        }

        #endregion
    }
}
