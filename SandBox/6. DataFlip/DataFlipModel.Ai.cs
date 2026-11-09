using System;
using System.Collections.Generic;
using System.Drawing;

namespace GateHelper.DataFlip
{
    public partial class CoreFrontGameState
    {
        #region AI Strategic Decisions (RTS 스타일 지능형 타이밍 러시 & 고유 빌드오더 & 무결점 전술 판단)

        public void ExecuteAiDecisions()
        {
            // [1. 전황 심층 분석 (Tactical Threat & Board Analysis)]
            var playerUnits = Units.FindAll(u => u.Owner == Owner.Player && u.Hp > 0);
            var aiUnits = Units.FindAll(u => u.Owner == Owner.Ai && u.Hp > 0);

            int dangerRow = -1;
            int maxPlayerCol = -1;
            int playerUnitsInAiHalf = 0;

            // 코어 리액터 직전 라인(Cols - 3 이상) 침입 여부 (초긴급 1순위 방어 대상)
            List<int> coreThreatRows = new List<int>();

            foreach (var pu in playerUnits)
            {
                if (pu.Col > maxPlayerCol)
                {
                    maxPlayerCol = pu.Col;
                    dangerRow = pu.Row;
                }
                if (pu.Col >= Cols / 2)
                {
                    playerUnitsInAiHalf++;
                }
                if (pu.Col >= Cols - 3)
                {
                    if (!coreThreatRows.Contains(pu.Row)) coreThreatRows.Add(pu.Row);
                }
            }

            // 플레이어가 유닛을 하나도 두지 않은 빈 라인 탐색 (기습/침투/타이밍 러시 대상)
            var emptyRows = new List<int>();
            for (int r = 0; r < Rows; r++)
            {
                if (!playerUnits.Exists(u => u.Row == r))
                {
                    emptyRows.Add(r);
                }
            }

            // [2. 플레이어의 전략적 상태 분석 (탐욕/경제확장 vs 결전 병기 vs 빈사)]
            int playerPlantCount = GetBuildingCount(BuildingType.PowerPlant, Owner.Player);
            int playerPlantLimit = GetPlantLimit(Owner.Player);

            // 유저의 경제 집중(배째기/인프라 확장) 포착:
            // 발전소가 2기 이상(초대형은 3기 이상)이거나, 유닛이 0~1기뿐인데 발전소/전력망에 투자한 경우
            bool isPlayerGreedyEco = (playerPlantCount >= ((CurrentMapSize == MapSizeType.Huge) ? 3 : 2)) &&
                                     (playerUnits.Count <= 2);

            // 유저 코어 빈사 상태 (피니시 찬스)
            bool isPlayerCoreVulnerable = (PlayerCoreHp <= 10);

            // 위협 수준 3단계 분류
            // Crisis: 코어 직전 진입(Cols - 4 이상) or 적 타이탄/드레드노트가 Cols - 5 이상 전진
            // Contested: 적 유닛이 전장 절반을 넘어 전진 중
            // Peaceful: 적 유닛이 0기이거나 전장 아군 진영에 전혀 오지 않음
            bool isCrisis = (maxPlayerCol >= Cols - 4) || (playerUnits.Exists(u => (u.Type == UnitType.Titan || u.Type == UnitType.Dreadnought) && u.Col >= Cols - 5));
            bool isContested = !isCrisis && (playerUnitsInAiHalf > 0 || maxPlayerCol >= Cols / 2);
            bool isPeaceful = (playerUnits.Count == 0) || (maxPlayerCol < Cols / 2);

            bool hasPlayerHeavy = playerUnits.Exists(u => u.Type == UnitType.Titan || u.Type == UnitType.Dreadnought || u.Type == UnitType.Colossus);

            // [2.5 라인 집중 웨이브(Line Push Wave) 턴 진행 및 신규 웨이브 결정]
            if (AiPushWaveTurnsRemaining > 0)
            {
                AiPushWaveTurnsRemaining--;
                if (AiPushWaveTurnsRemaining <= 0)
                {
                    AiPushWaveRow = -1;
                    AiPushWaveCooldown = 3; // 웨이브 완료 후 3턴간 쿨다운 (연속 무한 웨이브 방지 및 전선 호흡)
                }
            }
            else if (AiPushWaveCooldown > 0)
            {
                AiPushWaveCooldown--;
            }

            // 신규 집중 웨이브 결정 (쿨다운 완료, 3턴 이후, 자금 6G 이상, 위기 상태가 아닐 때 55% 확률 선언)
            if (AiPushWaveTurnsRemaining <= 0 && AiPushWaveCooldown <= 0 && TurnNumber >= 3 && AiCredit >= 6 && !isCrisis && _rand.Next(100) < 55)
            {
                int waveCandidateRow = -1;
                // 1순위: 가속관문이 있는 라인
                for (int r = 0; r < Rows; r++)
                {
                    for (int c = Cols - 3; c >= Cols - 4; c--)
                    {
                        var b = Buildings[r, c];
                        if (b != null && b.Owner == Owner.Ai && b.Type == BuildingType.PhaseGate)
                        {
                            waveCandidateRow = r;
                            break;
                        }
                    }
                    if (waveCandidateRow >= 0) break;
                }

                // 2순위: 가속 하이웨이 타일이 있는 라인
                if (waveCandidateRow < 0 && CurrentMapSize == MapSizeType.Huge)
                {
                    int[] highwayRows = { 2, 5, 8 };
                    waveCandidateRow = highwayRows[_rand.Next(highwayRows.Length)];
                }

                // 3순위: 빈 라인 (적 유닛이 없는 침투 라인)
                if (waveCandidateRow < 0 && emptyRows.Count > 0)
                {
                    waveCandidateRow = emptyRows[_rand.Next(emptyRows.Count)];
                }

                // 4순위: 무작위 라인
                if (waveCandidateRow < 0)
                {
                    waveCandidateRow = _rand.Next(Rows);
                }

                AiPushWaveRow = waveCandidateRow;
                AiPushWaveTurnsRemaining = 2; // 2턴간 해당 라인에 병력 집중 투입
                AddCombatLog($"⚡ [AI 전술] {AiPushWaveRow + 1}행에 화력 집중 돌파 웨이브 선언! (2턴 전력 집중)");
            }

            // [2.8 AI 액티브 전술 스킬 발동 (정찰스캔, EMP교란, 궤도폭격, 치료광선, 구축광선)]
            ExecuteAiSkills(playerUnits, aiUnits, dangerRow, isCrisis);

            int aiPlantCount = GetBuildingCount(BuildingType.PowerPlant, Owner.Ai);
            int aiPlantLimit = GetPlantLimit(Owner.Ai);
            int plantCost = GetBuildingCost(BuildingType.PowerPlant, Owner.Ai);
            int turretCost = GetBuildingCost(BuildingType.Turret, Owner.Ai);

            // [3. 컴퓨터 고유의 빌드오더 및 자금 저축(Reserve Target) 산정]
            int targetPlants;
            if (CurrentAiPersona == AiStrategyPersona.Rush)
            {
                targetPlants = (CurrentMapSize == MapSizeType.Huge) ? 2 : 1; // 기동 러시는 초기 발전소로 만족하고 병력 몰빵
            }
            else if (CurrentAiPersona == AiStrategyPersona.Siege)
            {
                targetPlants = (CurrentMapSize == MapSizeType.Huge) ? 3 : 2; // 공성 요새는 2~3기 적정 경제
            }
            else // Tech
            {
                targetPlants = (CurrentMapSize == MapSizeType.Huge) ? 4 : 3; // 테크는 3~4기 부유한 경제 구축
            }

            // 위협도 및 전략 타이밍에 따른 자금 저축(Reserve) 산정
            int targetReserve = 0;
            if (isCrisis)
            {
                // 초긴급 상황: 저축 완전 중단! 즉시 가용 전액 방어에 투입
                targetReserve = 0;
            }
            else if (isPlayerGreedyEco)
            {
                // ⚡ [타이밍 러시 기회 포착]: 유저가 경제에 투자하느라 병력이 부족함!
                // 저축을 중단하고 병력을 집중 생산하여 빈 라인으로 타이밍 돌파
                targetReserve = (CurrentAiPersona == AiStrategyPersona.Tech && aiPlantCount < targetPlants) ? plantCost : 0;
            }
            else if (isContested)
            {
                // 일반 전선 교전: 최소한의 자금만 남김
                targetReserve = (CurrentAiPersona == AiStrategyPersona.Tech && aiPlantCount < targetPlants) ? Math.Min(plantCost, 4) : 0;
            }
            else // isPeaceful (평화 / 유저가 본진 대기)
            {
                switch (CurrentAiPersona)
                {
                    case AiStrategyPersona.Tech:
                        if (aiPlantCount < targetPlants)
                        {
                            targetReserve = plantCost; // 발전소 목표액(9G 등)까지 저축
                        }
                        else
                        {
                            // 발전소 충분 시 중장갑 결전병기(Titan 10G or Dreadnought 18G)를 위한 자금 비축
                            targetReserve = (TurnNumber < 8) ? 10 : 18;
                        }
                        break;

                    case AiStrategyPersona.Siege:
                        targetReserve = (aiPlantCount < targetPlants) ? plantCost : turretCost;
                        break;

                    case AiStrategyPersona.Rush:
                    default:
                        targetReserve = 0;
                        break;
                }
            }

            // [4. 인프라 건설 (페르소나별 독자적 빌드 & 전략 건물 구축)]

            // A. 전력망 한도 증설 (Tech형 특전: 발전소 풀가동 시)
            if (CurrentAiPersona == AiStrategyPersona.Tech && aiPlantCount >= aiPlantLimit && AiCredit >= GetGridExpandCost(Owner.Ai) + 6)
            {
                UpgradePowerGrid(Owner.Ai);
                aiPlantLimit = GetPlantLimit(Owner.Ai);
            }

            // B. 발전소 건설 (Tech 우선, Siege 적정, Rush 최소)
            if (aiPlantCount < targetPlants && AiCredit >= plantCost && !isCrisis)
            {
                bool builtPlant = false;
                // 1순위: 중립 자원 노드(Col 8~Cols-4) 선점 (+3G 고수입)
                for (int c = Cols - 4; c >= 8; c--)
                {
                    for (int r = 0; r < Rows; r++)
                    {
                        if (MapTiles[r, c] == TileType.Resource && IsValidBuildingPlacement(c, r, Owner.Ai))
                        {
                            builtPlant = Build(c, r, BuildingType.PowerPlant, Owner.Ai);
                            if (builtPlant) break;
                        }
                    }
                    if (builtPlant) break;
                }

                // 2순위: 안전한 아군 베이스라인
                if (!builtPlant)
                {
                    for (int c = Cols - 2; c >= Cols - 3; c--)
                    {
                        for (int r = 0; r < Rows; r++)
                        {
                            if (IsValidBuildingPlacement(c, r, Owner.Ai))
                            {
                                builtPlant = Build(c, r, BuildingType.PowerPlant, Owner.Ai);
                                if (builtPlant) break;
                            }
                        }
                        if (builtPlant) break;
                    }
                }
            }

            // C. 방화벽(Wall, 3G) 전략적 요충지 봉쇄 (Siege 빌드 & 대형 유닛 저지)
            // 적 중기갑(타이탄/드레드노트)이 진격 중이거나 Siege형이 전선 요새화 시도 시
            if (AiCredit >= 3 && !isCrisis)
            {
                if (hasPlayerHeavy && dangerRow >= 0 && IsValidBuildingPlacement(Cols - 3, dangerRow, Owner.Ai))
                {
                    // 적 중기갑 전면에 8 HP 방화벽을 깔아 진격 차단!
                    Build(Cols - 3, dangerRow, BuildingType.Wall, Owner.Ai);
                }
                else if (CurrentAiPersona == AiStrategyPersona.Siege && GetBuildingCount(BuildingType.Wall, Owner.Ai) < 2 && _rand.Next(100) < 50)
                {
                    int wallRow = (dangerRow >= 0) ? dangerRow : _rand.Next(Rows);
                    if (IsValidBuildingPlacement(Cols - 3, wallRow, Owner.Ai))
                    {
                        Build(Cols - 3, wallRow, BuildingType.Wall, Owner.Ai);
                    }
                }
            }

            // D. 방어 거점(포탑 / 수리격납고) 요새화
            // 위협 감지 시 또는 Siege형 거점 구축
            bool shouldBuildTurret = (isCrisis && AiCredit >= turretCost) ||
                                     (isContested && AiCredit >= turretCost && _rand.Next(100) < 60) ||
                                     (CurrentAiPersona == AiStrategyPersona.Siege && AiCredit >= turretCost && GetBuildingCount(BuildingType.Turret, Owner.Ai) < 3);

            if (shouldBuildTurret)
            {
                int defRow = (dangerRow >= 0) ? dangerRow : _rand.Next(Rows);
                int[] rowCandidates = { defRow, Math.Max(0, defRow - 1), Math.Min(Rows - 1, defRow + 1) };
                foreach (int r in rowCandidates)
                {
                    if (IsValidBuildingPlacement(Cols - 3, r, Owner.Ai))
                    {
                        Build(Cols - 3, r, BuildingType.Turret, Owner.Ai);
                        break;
                    }
                }
            }

            // E. Siege형 수리격납고(RepairDepot, 6G) 전선 보급 유지
            if (CurrentAiPersona == AiStrategyPersona.Siege && TurnNumber >= 5 && AiCredit >= 10 && GetBuildingCount(BuildingType.RepairDepot, Owner.Ai) == 0)
            {
                for (int r = 0; r < Rows; r++)
                {
                    if (IsValidBuildingPlacement(Cols - 2, r, Owner.Ai))
                    {
                        Build(Cols - 2, r, BuildingType.RepairDepot, Owner.Ai);
                        break;
                    }
                }
            }

            // F. Rush형 가속관문(PhaseGate, 5G) 기동 통로 개척
            // 4턴 이후 자금 여유 시 빈 라인에 가속관문 설치 (통과 유닛 이동력 +1 ➔ 초고속 3칸 돌파)
            if (CurrentAiPersona == AiStrategyPersona.Rush && TurnNumber >= 3 && AiCredit >= 7 && GetBuildingCount(BuildingType.PhaseGate, Owner.Ai) == 0)
            {
                int gateRow = (emptyRows.Count > 0) ? emptyRows[_rand.Next(emptyRows.Count)] : _rand.Next(Rows);
                if (IsValidBuildingPlacement(Cols - 3, gateRow, Owner.Ai))
                {
                    Build(Cols - 3, gateRow, BuildingType.PhaseGate, Owner.Ai);
                }
            }

            // [5. 병력 생산 & 유동적 라인 배치]
            // 평화 상태에서 저축 중이더라도, 전장에 아군 유닛이 0기라면 정찰용 보병 1기는 내보냄 (필드가 텅 비지 않도록)
            bool mustSpawnPatrol = (aiUnits.Count == 0 && AiCredit >= 2);

            while (AiCredit >= 2)
            {
                // 저축 목표액에 도달하지 못했고 비상 상황이 아니며 최소 정찰 유닛이 있다면 자금 비축!
                if (!mustSpawnPatrol && AiCredit < targetReserve && !isCrisis && !isPlayerGreedyEco && !isPlayerCoreVulnerable)
                {
                    break;
                }

                UnitType spawnType = UnitType.Vanguard;

                // ----------------------------------------------------
                // [엄격한 전술 지능: 치명적 어이없는 실수 방지 및 독자 전략]
                // ----------------------------------------------------

                // (A) 코어 직전 진입 비상 요격 (최우선 순위: 코어 헌납 방지)
                if (coreThreatRows.Count > 0)
                {
                    // 코어 직전 라인에는 즉시 저지 병력 배치
                    if (AiCredit >= 10 && _rand.Next(100) < 60) spawnType = UnitType.Titan;
                    else if (AiCredit >= 4 && _rand.Next(100) < 60) spawnType = UnitType.Raider;
                    else spawnType = UnitType.Vanguard;
                }
                // (B) 플레이어 코어 빈사 (피니시 찬스): 기동 돌파 병력으로 게임 끝내기
                else if (isPlayerCoreVulnerable)
                {
                    if (AiCredit >= 10 && _rand.Next(100) < 50) spawnType = UnitType.Titan;
                    else if (AiCredit >= 4) spawnType = UnitType.Raider;
                    else spawnType = UnitType.Vanguard;
                }
                // (C) 플레이어 경제 배째기 포착 ➔ ⚡ 타이밍 러시 전개!
                else if (isPlayerGreedyEco)
                {
                    // 유저가 병력이 없으므로, 침투병(4G, 2칸)과 타이탄(10G)으로 빈 라인 전격 돌파
                    if (CurrentAiPersona == AiStrategyPersona.Rush)
                    {
                        if (AiCredit >= 10 && _rand.Next(100) < 45) spawnType = UnitType.Titan;
                        else if (AiCredit >= 4) spawnType = UnitType.Raider;
                        else spawnType = UnitType.Vanguard;
                    }
                    else if (CurrentAiPersona == AiStrategyPersona.Siege)
                    {
                        if (AiCredit >= 12 && _rand.Next(100) < 60) spawnType = UnitType.Cheongung;
                        else if (AiCredit >= 4) spawnType = UnitType.Raider;
                        else spawnType = UnitType.Vanguard;
                    }
                    else // Tech
                    {
                        if (AiCredit >= 10 && _rand.Next(100) < 60) spawnType = UnitType.Titan;
                        else if (AiCredit >= 4) spawnType = UnitType.Raider;
                        else spawnType = UnitType.Vanguard;
                    }
                }
                // (D) 상성 카운터: 플레이어 드레드노트 출현 시 (다수 소형 유닛 협공 or 타이탄 저지)
                else if (playerUnits.Exists(u => u.Type == UnitType.Dreadnought))
                {
                    if (AiCredit >= 10 && _rand.Next(100) < 40) spawnType = UnitType.Titan;
                    else if (AiCredit >= 4) spawnType = UnitType.Raider;
                    else spawnType = UnitType.Vanguard;
                }
                // (E) 유저가 유닛을 안 뽑았을 때 (페르소나별 독자 플랜)
                else if (playerUnits.Count == 0)
                {
                    switch (CurrentAiPersona)
                    {
                        case AiStrategyPersona.Rush:
                            // 러시는 빈 라인을 탐색해 침투병 우선 출격
                            spawnType = (AiCredit >= 4 && _rand.Next(100) < 75) ? UnitType.Raider : UnitType.Vanguard;
                            break;

                        case AiStrategyPersona.Tech:
                            // 테크는 고자본 축적 후 결전 병기 소환
                            if (AiCredit >= 18) spawnType = UnitType.Dreadnought;
                            else if (AiCredit >= 10) spawnType = UnitType.Titan;
                            else spawnType = UnitType.Vanguard;
                            break;

                        case AiStrategyPersona.Siege:
                        default:
                            // 시즈는 장거리 천궁이나 콜로서스 대기
                            if (AiCredit >= 14 && _rand.Next(100) < 50) spawnType = UnitType.Colossus;
                            else if (AiCredit >= 12 && _rand.Next(100) < 50) spawnType = UnitType.Cheongung;
                            else spawnType = UnitType.Vanguard;
                            break;
                    }
                }
                // (F) 일반 교전 시 페르소나별 주력 유닛 픽
                else
                {
                    switch (CurrentAiPersona)
                    {
                        case AiStrategyPersona.Rush:
                            if (AiCredit >= 10 && _rand.Next(100) < 35) spawnType = UnitType.Titan;
                            else if (AiCredit >= 4 && _rand.Next(100) < 70) spawnType = UnitType.Raider;
                            else spawnType = UnitType.Vanguard;
                            break;

                        case AiStrategyPersona.Siege:
                            if (AiCredit >= 14 && _rand.Next(100) < 45) spawnType = UnitType.Colossus;
                            else if (AiCredit >= 12 && _rand.Next(100) < 45) spawnType = UnitType.Cheongung;
                            else if (AiCredit >= 10 && _rand.Next(100) < 35) spawnType = UnitType.Titan;
                            else if (AiCredit >= 4 && _rand.Next(100) < 30) spawnType = UnitType.Raider;
                            else spawnType = UnitType.Vanguard;
                            break;

                        case AiStrategyPersona.Tech:
                        default:
                            if (AiCredit >= 18 && _rand.Next(100) < 50) spawnType = UnitType.Dreadnought;
                            else if (AiCredit >= 14 && _rand.Next(100) < 35) spawnType = UnitType.Colossus;
                            else if (AiCredit >= 12 && _rand.Next(100) < 30) spawnType = UnitType.Cheongung;
                            else if (AiCredit >= 10 && _rand.Next(100) < 50) spawnType = UnitType.Titan;
                            else if (AiCredit >= 4 && _rand.Next(100) < 35) spawnType = UnitType.Raider;
                            else spawnType = UnitType.Vanguard;
                            break;
                    }
                }

                int cost = GetUnitCost(spawnType);
                if (AiCredit < cost)
                {
                    // 비싼 걸 뽑으려다 돈이 부족하면:
                    // 평화 시 저축 중이거나 타이밍 러시를 기다리는 중이면 보병으로 탕진하지 않고 루프 탈출
                    if (targetReserve > 0 && !isCrisis && !mustSpawnPatrol && !isPlayerGreedyEco)
                    {
                        break;
                    }
                    spawnType = UnitType.Vanguard;
                    cost = GetUnitCost(spawnType);
                    if (AiCredit < cost) break;
                }

                // ----------------------------------------------------
                // [소환 라인(Row) 선택: 치명적 방어 실패 방지 & 전술 침투]
                // ----------------------------------------------------
                List<int> preferredRows = new List<int>();

                // 1순위: 코어 직전 위험 라인 (코어 피격 방지 최우선)
                if (coreThreatRows.Count > 0)
                {
                    preferredRows.AddRange(coreThreatRows);
                }
                // 1.5순위: ⚡ 라인 집중 웨이브 (돌파 행에 전력 집중)
                else if (AiPushWaveTurnsRemaining > 0 && AiPushWaveRow >= 0)
                {
                    preferredRows.Add(AiPushWaveRow);
                }
                // 2순위: 위기 라인 (적 주력 진격로 정면 저지)
                else if (isCrisis && dangerRow >= 0)
                {
                    preferredRows.Add(dangerRow);
                    if (dangerRow - 1 >= 0) preferredRows.Add(dangerRow - 1);
                    if (dangerRow + 1 < Rows) preferredRows.Add(dangerRow + 1);
                }
                // 3순위: 타이밍 러시 or 침투병의 빈 라인 우회 기습
                else if ((isPlayerGreedyEco || spawnType == UnitType.Raider || isPlayerCoreVulnerable) && emptyRows.Count > 0)
                {
                    // 가속관문이 있는 행이 있다면 그 행 최우선 선택!
                    for (int r = 0; r < Rows; r++)
                    {
                        for (int c = Cols - 3; c >= Cols - 4; c--)
                        {
                            var b = Buildings[r, c];
                            if (b != null && b.Owner == Owner.Ai && b.Type == BuildingType.PhaseGate)
                            {
                                if (!preferredRows.Contains(r)) preferredRows.Add(r);
                            }
                        }
                    }
                    preferredRows.AddRange(emptyRows);
                }
                // 4순위: 일반 교전 저지선
                else if (dangerRow >= 0)
                {
                    preferredRows.Add(dangerRow);
                    if (dangerRow - 1 >= 0) preferredRows.Add(dangerRow - 1);
                    if (dangerRow + 1 < Rows) preferredRows.Add(dangerRow + 1);
                }

                // 나머지 행들도 후순위 추가
                for (int r = 0; r < Rows; r++)
                {
                    if (!preferredRows.Contains(r)) preferredRows.Add(r);
                }

                bool spawned = false;
                int spawnedRow = -1;
                foreach (int r in preferredRows)
                {
                    if (IsValidUnitSpawnPlacement(Cols - 2, r, Owner.Ai))
                    {
                        spawned = SpawnUnit(Cols - 2, r, spawnType, Owner.Ai);
                        if (spawned) { spawnedRow = r; break; }
                    }
                    else if (IsValidUnitSpawnPlacement(Cols - 3, r, Owner.Ai))
                    {
                        spawned = SpawnUnit(Cols - 3, r, spawnType, Owner.Ai);
                        if (spawned) { spawnedRow = r; break; }
                    }
                }

                if (!spawned) break;

                string uName = (spawnType == UnitType.Vanguard) ? "보병" :
                               (spawnType == UnitType.Raider) ? "침투병" :
                               (spawnType == UnitType.Titan) ? "타이탄" :
                               (spawnType == UnitType.Cheongung) ? "천궁" :
                               (spawnType == UnitType.Colossus) ? "콜로서스" : "드레드노트";

                if (AiPushWaveTurnsRemaining > 0 && spawnedRow == AiPushWaveRow)
                {
                    AddCombatLog($"🚀 [AI 웨이브] {spawnedRow + 1}행 돌파군으로 {uName} 급파!");
                }
                else if (spawnType != UnitType.Vanguard)
                {
                    AddCombatLog($"🚀 [AI 전술] {spawnedRow + 1}행에 {uName} 출격");
                }

                mustSpawnPatrol = false;

                // 유저에게 너무 일방적인 무한 물량 공세가 되지 않도록 조절 (단, 위기나 타이밍러시 때는 쉬지 않고 총력 투입)
                if (!isCrisis && !isPlayerGreedyEco && !isPlayerCoreVulnerable && _rand.Next(100) < 40)
                {
                    break;
                }
            }
        }

        private void ExecuteAiSkills(List<Unit> playerUnits, List<Unit> aiUnits, int dangerRow, bool isCrisis)
        {
            // 1. [정찰 스캔 (ReconScan, 3G)]: 적 은폐 유닛(침투병) 발견 시 즉시 탐지 해제
            if (AiCredit >= 3)
            {
                var cloakedUnit = playerUnits.Find(u => u.IsCloaked && u.Hp > 0);
                if (cloakedUnit != null && (cloakedUnit.Col >= Cols / 2 || _rand.Next(100) < 70))
                {
                    if (CastSkill(UserSkillType.ReconScan, cloakedUnit.Col, cloakedUnit.Row, Owner.Ai, out var sEvents, out var sBeam))
                    {
                        if (sBeam != null) PendingAiLaserBeams.Add(sBeam);
                        PendingAiCombatEvents.AddRange(sEvents);
                    }
                }
            }

            // 2. [EMP 교란 (EmpJammer, 5G)]: 플레이어 전진 포탑 마비
            if (AiCredit >= 5)
            {
                for (int r = 0; r < Rows; r++)
                {
                    for (int c = 1; c < Cols - 1; c++)
                    {
                        var b = Buildings[r, c];
                        if (b != null && b.Owner == Owner.Player && b.Type == BuildingType.Turret && b.EmpTurns == 0)
                        {
                            if (aiUnits.Exists(u => Math.Abs(u.Row - r) <= 1 && u.Col >= c - 2) || isCrisis)
                            {
                                if (CastSkill(UserSkillType.EmpJammer, c, r, Owner.Ai, out var sEvents, out var sBeam))
                                {
                                    if (sBeam != null) PendingAiLaserBeams.Add(sBeam);
                                    PendingAiCombatEvents.AddRange(sEvents);
                                    return;
                                }
                            }
                        }
                    }
                }
            }

            // 3. [궤도 폭격 (OrbitalStrike, 8G)]: 플레이어 핵심 포탑/건물 또는 장애물 정밀 타격
            if (AiCredit >= 8 && (CurrentAiPersona != AiStrategyPersona.Rush || isCrisis))
            {
                for (int r = 0; r < Rows; r++)
                {
                    for (int c = 1; c < Cols - 1; c++)
                    {
                        var b = Buildings[r, c];
                        if (b != null && b.Owner == Owner.Player && b.Type == BuildingType.Turret)
                        {
                            if (CastSkill(UserSkillType.OrbitalStrike, c, r, Owner.Ai, out var sEvents, out var sBeam))
                            {
                                if (sBeam != null) PendingAiLaserBeams.Add(sBeam);
                                PendingAiCombatEvents.AddRange(sEvents);
                                return;
                            }
                        }
                    }
                }

                // 돌파 웨이브 라인의 장애물 서버 랙 파괴
                if (AiPushWaveRow >= 0 && AiPushWaveTurnsRemaining > 0)
                {
                    for (int c = Cols / 2 - 2; c <= Cols / 2 + 2; c++)
                    {
                        if (c >= 1 && c < Cols - 1 && MapTiles[AiPushWaveRow, c] == TileType.Obstacle)
                        {
                            if (CastSkill(UserSkillType.OrbitalStrike, c, AiPushWaveRow, Owner.Ai, out var sEvents, out var sBeam))
                            {
                                if (sBeam != null) PendingAiLaserBeams.Add(sBeam);
                                PendingAiCombatEvents.AddRange(sEvents);
                                return;
                            }
                        }
                    }
                }
            }

            // 4. [치료 광선 (HealingBeam, 4G)]: 부상당한 고가치 유닛(타이탄/드레드노트) 또는 아군 밀집 라인 수복
            if (AiCredit >= 4)
            {
                int bestHealRow = -1;
                int maxHealValue = 0;

                for (int r = 0; r < Rows; r++)
                {
                    int healVal = 0;
                    foreach (var u in aiUnits)
                    {
                        if (u.Row == r && u.Hp < u.MaxHp)
                        {
                            healVal += (u.Type == UnitType.Dreadnought || u.Type == UnitType.Titan) ? 4 : 2;
                        }
                    }
                    for (int c = 1; c < Cols - 1; c++)
                    {
                        var b = Buildings[r, c];
                        if (b != null && b.Owner == Owner.Ai && b.Hp < b.MaxHp)
                        {
                            healVal += (b.Type == BuildingType.Turret) ? 3 : 1;
                        }
                    }
                    if (healVal > maxHealValue)
                    {
                        maxHealValue = healVal;
                        bestHealRow = r;
                    }
                }

                if (bestHealRow >= 0 && maxHealValue >= 3)
                {
                    if (CastSkill(UserSkillType.HealingBeam, Cols - 2, bestHealRow, Owner.Ai, out var sEvents, out var sBeam))
                    {
                        if (sBeam != null) PendingAiLaserBeams.Add(sBeam);
                        PendingAiCombatEvents.AddRange(sEvents);
                        return;
                    }
                }
            }

            // 5. [구축 광선 (FortifyBeam, 5G)]: 방어선 강화 (Siege 페르소나 또는 방어 라인)
            if (AiCredit >= 7 && CurrentAiPersona == AiStrategyPersona.Siege && TurnNumber >= 4)
            {
                int bestFortRow = -1;
                for (int r = 0; r < Rows; r++)
                {
                    int rowUnits = aiUnits.FindAll(u => u.Row == r).Count;
                    if (rowUnits >= 2)
                    {
                        bestFortRow = r;
                        break;
                    }
                }
                if (bestFortRow >= 0)
                {
                    if (CastSkill(UserSkillType.FortifyBeam, Cols - 2, bestFortRow, Owner.Ai, out var sEvents, out var sBeam))
                    {
                        if (sBeam != null) PendingAiLaserBeams.Add(sBeam);
                        PendingAiCombatEvents.AddRange(sEvents);
                        return;
                    }
                }
            }
        }

        #endregion
    }
}
