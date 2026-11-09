using System;
using System.Collections.Generic;
using System.Drawing;

namespace GateHelper.DataFlip
{
    public partial class CoreFrontGameState
    {
        public const int DefaultCols = 14;
        public const int DefaultRows = 8;
        public static int StaticCols => DefaultCols;
        public static int StaticRows => DefaultRows;

        public int Cols { get; set; } = DefaultCols;
        public int Rows { get; set; } = DefaultRows;
        public MapSizeType CurrentMapSize { get; set; } = MapSizeType.Standard;
        public BattlefieldTheme CurrentTheme { get; set; } = BattlefieldTheme.Standard;
        public const int BaseMaxHp = 25;

        // 맵 그리드
        public TileType[,] MapTiles { get; private set; }
        public int[,] ObstacleHp { get; private set; }
        public Building[,] Buildings { get; private set; }
        public List<Unit> Units { get; private set; }

        // 게임 종료 후 전투 통계 대시보드 (유저 흥미 & 전적 분석)
        public int StatsPlayerUnitsKilled { get; set; } = 0;
        public int StatsPlayerUnitsLost { get; set; } = 0;
        public int StatsPlayerBuildingsDestroyed { get; set; } = 0;
        public int StatsObstaclesDestroyed { get; set; } = 0;
        public int StatsTotalCreditEarned { get; set; } = 6;
        public int StatsDamageDealtToAiCore { get; set; } = 0;
        public int StatsTurnsElapsed => TurnNumber;

        // 도전과제 정밀 판별용 런타임 추적 플래그
        public DateTime MatchStartTime { get; set; } = DateTime.Now;
        public bool HasSpawnedVanguard { get; set; } = false;
        public bool HasSpawnedNonVanguard { get; set; } = false;
        public int PlayerBuiltPowerPlantsDuringGame { get; set; } = 0;
        public int PlayerBuiltTurrets { get; set; } = 0;
        public bool HasSelectedUnitCard { get; set; } = false;
        public bool HasSpawnedFromFactoryBridgehead { get; set; } = false;
        public bool HasUsedOrbitalStrikeDestroy { get; set; } = false;

        // 초경량 턴별 전투 리플레이 스냅샷 기록
        public List<ReplayTurnSnapshot> ReplaySnapshots { get; set; } = new List<ReplayTurnSnapshot>();

        // 본진 체력
        public int PlayerCoreMaxHp { get; set; } = BaseMaxHp;
        public int AiCoreMaxHp { get; set; } = BaseMaxHp;
        public int PlayerCoreHp { get; set; } = BaseMaxHp;
        public int AiCoreHp { get; set; } = BaseMaxHp;

        // 자원 & 턴
        public int TurnNumber { get; set; } = 1;
        public Owner CurrentTurn { get; set; } = Owner.Player;
        public int PlayerCredit { get; set; } = 6;
        public int AiCredit { get; set; } = 6;

        // 상태
        public bool IsGameOver { get; set; } = false;
        public Owner Winner { get; set; } = Owner.None;
        public string StatusMessage { get; set; } = "당신의 턴 - 건물이나 유닛을 배치하고 [턴 종료]를 누르세요.";

        // 카드 선택 로그라이크 상태
        public bool IsCardSelectionActive { get; set; } = false;
        public List<ProtocolCard> CurrentOfferedCards { get; set; } = new List<ProtocolCard>();

        // 카드 선택 후 유닛 직접 배치 지정 대기 상태
        public UnitType PendingDropUnitType { get; set; } = UnitType.None;
        public int PendingDropRemainingCount { get; set; } = 0;
        public int PendingDropMinCol { get; set; } = 1;
        public int PendingDropMaxCol { get; set; } = 1;
        public int PendingDropCustomHp { get; set; } = 0;    // 후반부 공수 강화: 투하되는 유닛에게만 부여되는 커스텀 기본 체력
        public int PendingDropCustomAtk { get; set; } = 0;   // 후반부 공수 강화: 투하되는 유닛에게만 부여되는 커스텀 공격력
        public bool PendingDropForceShield { get; set; } = false; // 후반부 공수 강화: 투하되는 유닛에게 부여되는 실드 여부

        // 1턴 첫 발전소 직접 배치 모드 (플레이어가 Col 1~2 원하는 곳에 무료 건설)
        public bool IsInitialBasePlacementActive { get; set; } = false;
        public int InitialBaseRemainingCount { get; set; } = 0;
        public int InitialBaseTotalCount { get; set; } = 1;

        // 패시브 보너스 (무제한 누적 스택)
        public int PlayerIncomeBonus { get; set; } = 0;
        public int AiIncomeBonus { get; set; } = 0;
        public int PlayerUnitHpBonus { get; set; } = 0;
        public int AiUnitHpBonus { get; set; } = 0;
        public int PlayerUnitAtkBonus { get; set; } = 0;
        public int AiUnitAtkBonus { get; set; } = 0;
        public int PlayerCostDiscount { get; set; } = 0;
        public int TurretRangeBonus { get; set; } = 0;

        // 8대 유닛/인프라 고유 특수 기술 해금 상태 (프로토콜 카드로 해금)
        public bool PlayerHasSiegeAnchor { get; set; } = false;   // 콜로서스 시즈 앵커 (제자리 포격 Hold & Fire)
        public bool AiHasSiegeAnchor { get; set; } = false;

        public bool PlayerHasColossusMelee { get; set; } = false; // 콜로서스 근접 방어 발칸포 (1칸 근접 교전/반격 허용)
        public bool AiHasColossusMelee { get; set; } = false;

        public bool PlayerHasCheongungTrajectory { get; set; } = false; // 천궁 탄도 궤적 정밀 연산 (사거리 4->5, 최소사거리 2->3)
        public bool AiHasCheongungTrajectory { get; set; } = false;

        public bool PlayerHasPhalanxShield { get; set; } = false; // 보병 전술 방패 (스폰 시 1회 피해 무효화 실드)
        public bool AiHasPhalanxShield { get; set; } = false;

        public bool PlayerHasStealthCloak { get; set; } = false;  // 침투병 광학 스텔스 (근접 공격 전까지 원거리/포탑 타깃팅 면역)
        public bool AiHasStealthCloak { get; set; } = false;

        public bool PlayerHasTurretRadar { get; set; } = false;   // 포탑 양자 레이더 망 (은폐 유닛도 13칸 요격 가능)
        public bool AiHasTurretRadar { get; set; } = false;

        public bool PlayerHasKineticSlam { get; set; } = false;   // 타이탄 운동에너지 충격파 (1칸 넉백 및 연쇄 충돌 피해)
        public bool AiHasKineticSlam { get; set; } = false;

        public bool PlayerHasChainPlasma { get; set; } = false;   // 드레드노트 연쇄 플라즈마 (십자 4방향 2뎀 스플래시 방전)
        public bool AiHasChainPlasma { get; set; } = false;

        // AI 전술 페르소나 (판마다 다른 전략 구사)
        public AiStrategyPersona CurrentAiPersona { get; set; } = AiStrategyPersona.Tech;

        // 한줄 전투 로그 시스템
        public string LatestCombatLog { get; set; } = "작전 개시. 전술 명령을 대기 중입니다.";
        public List<string> CombatLogHistory { get; } = new List<string>();

        // AI 액티브 스킬 및 전술 연출 큐
        public List<CombatEvent> PendingAiCombatEvents { get; } = new List<CombatEvent>();
        public List<CombatLaserBeam> PendingAiLaserBeams { get; } = new List<CombatLaserBeam>();

        // AI 라인 집중 웨이브(한타 콤보) 상태
        public int AiPushWaveRow { get; set; } = -1;
        public int AiPushWaveTurnsRemaining { get; set; } = 0;
        public int AiPushWaveCooldown { get; set; } = 0;

        public void AddCombatLog(string log)
        {
            if (string.IsNullOrWhiteSpace(log)) return;
            LatestCombatLog = log;
            CombatLogHistory.Insert(0, $"[턴 {TurnNumber}] {log}");
            if (CombatLogHistory.Count > 30)
            {
                CombatLogHistory.RemoveAt(CombatLogHistory.Count - 1);
            }
        }

        // 발전소 보유 한도 (기본 3기 -> 카드로 대폭 확장 또는 골드 투자로 점진 증설 가능)
        public int PlayerPlantLimit { get; set; } = 3;
        public int AiPlantLimit { get; set; } = 3;

        public string GetThemeName()
        {
            switch (CurrentTheme)
            {
                case BattlefieldTheme.Inferno: return "과열 화염 용광로";
                case BattlefieldTheme.Highway: return "가속 하이웨이";
                case BattlefieldTheme.NanoWell: return "나노 수복 샘";
                case BattlefieldTheme.SmokeField: return "스모크 필드";
                default: return "표준 서버 룸";
            }
        }

        public string GetThemeIcon()
        {
            switch (CurrentTheme)
            {
                case BattlefieldTheme.Inferno: return "🔥";
                case BattlefieldTheme.Highway: return "⏩";
                case BattlefieldTheme.NanoWell: return "💚";
                case BattlefieldTheme.SmokeField: return "🌫️";
                default: return "⚡";
            }
        }

        public string GetThemeStatusBrief()
        {
            switch (CurrentTheme)
            {
                case BattlefieldTheme.Inferno: return "화염 지대 지속 -1 피해!";
                case BattlefieldTheme.Highway: return "가속 레일 전방 +1칸 초고속 기동!";
                case BattlefieldTheme.NanoWell: return "수복 거점 매턴 +1 HP 치유!";
                case BattlefieldTheme.SmokeField: return "은폐 연막 포탑/원거리 요격 면역!";
                default: return "8대 중립 랙 돌파전!";
            }
        }

        public string GetThemeDescription()
        {
            switch (CurrentTheme)
            {
                case BattlefieldTheme.Inferno: return "화염 지대: 유닛 진입 및 턴 종료 시 -1 피해를 입습니다. (건물 건설 불가)";
                case BattlefieldTheme.Highway: return "가속 레일: 유닛 이동 시 전방으로 +1칸 즉시 추가 가속 전진합니다.";
                case BattlefieldTheme.NanoWell: return "나노 수복 샘: 턴 종료 시 타일 위에 머무르는 유닛의 체력이 +1 HP 회복됩니다.";
                case BattlefieldTheme.SmokeField: return "스모크 필드: 타일 위 유닛은 포탑 요격 및 원거리 타깃팅에 면역(은폐)됩니다.";
                default: return "표준 서버 룸: 중앙에 배치된 8대의 중립 서버 랙을 완파하면 +2G 현상금을 획득합니다.";
            }
        }

        public Color GetThemeColor()
        {
            switch (CurrentTheme)
            {
                case BattlefieldTheme.Inferno: return Color.FromArgb(255, 110, 64);
                case BattlefieldTheme.Highway: return Color.FromArgb(0, 229, 255);
                case BattlefieldTheme.NanoWell: return Color.FromArgb(76, 175, 80);
                case BattlefieldTheme.SmokeField: return Color.FromArgb(186, 104, 200);
                default: return Color.FromArgb(255, 215, 0);
            }
        }

        public int GetPlantLimit(Owner owner) => (owner == Owner.Player) ? PlayerPlantLimit : AiPlantLimit;

        public int GetGridExpandCost(Owner owner)
        {
            int currentLimit = GetPlantLimit(owner);
            // 3기->4기: 10G, 4기->5기: 14G, 5기->6기: 18G (점진적 가중치 상향)
            return 10 + Math.Max(0, currentLimit - 3) * 4;
        }

        public bool UpgradePowerGrid(Owner owner)
        {
            int cost = GetGridExpandCost(owner);
            int credit = (owner == Owner.Player) ? PlayerCredit : AiCredit;
            if (credit < cost) return false;

            if (owner == Owner.Player)
            {
                PlayerCredit -= cost;
                PlayerPlantLimit++;
                AddCombatLog($"⚡ [플레이어] 전력망 증설 완료 (발전소 최대 한도 {PlayerPlantLimit}기 확장)");
            }
            else
            {
                AiCredit -= cost;
                AiPlantLimit++;
                AddCombatLog($"⚡ [AI 인프라] 전력망 증설 완료 (발전소 최대 한도 {AiPlantLimit}기 확장)");
            }
            return true;
        }

        private int _unitIdCounter = 1;
        private static readonly Random _rand = new Random();

        public CoreFrontGameState()
        {
            MapTiles = new TileType[Rows, Cols];
            ObstacleHp = new int[Rows, Cols];
            Buildings = new Building[Rows, Cols];
            Units = new List<Unit>();
        }

        public void RecordReplaySnapshot(string note = "")
        {
            string defaultNote;
            if (IsGameOver)
            {
                defaultNote = (Winner == Owner.Player) ? "🎉 작전 성공 (적 코어 완파)" : "💀 작전 실패 (아군 코어 파괴)";
            }
            else
            {
                defaultNote = (TurnNumber <= 1 && IsInitialBasePlacementActive) ? "초기 기지 설립 중" : $"턴 {this.TurnNumber} 전황";
            }

            var snap = new ReplayTurnSnapshot
            {
                TurnNumber = this.TurnNumber,
                CurrentTurn = this.CurrentTurn,
                PlayerCoreHp = this.PlayerCoreHp,
                AiCoreHp = this.AiCoreHp,
                PlayerCredit = this.PlayerCredit,
                AiCredit = this.AiCredit,
                Note = string.IsNullOrEmpty(note) ? defaultNote : note
            };

            foreach (var u in Units)
            {
                snap.Units.Add(new ReplayUnitData
                {
                    Col = u.Col,
                    Row = u.Row,
                    Owner = u.Owner,
                    Type = u.Type,
                    Hp = u.Hp,
                    MaxHp = u.MaxHp,
                    HasShield = u.HasShield,
                    IsCloaked = u.IsCloaked,
                    HasBomb = u.HasBomb
                });
            }

            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    var b = Buildings[r, c];
                    if (b != null)
                    {
                        snap.Buildings.Add(new ReplayBuildingData
                        {
                            Col = c,
                            Row = r,
                            Owner = b.Owner,
                            Type = b.Type,
                            Hp = b.Hp,
                            MaxHp = b.MaxHp,
                            EmpTurns = b.EmpTurns
                        });
                    }
                    if (ObstacleHp[r, c] > 0)
                    {
                        snap.Obstacles.Add(new ReplayObstacleData
                        {
                            Col = c,
                            Row = r,
                            Hp = ObstacleHp[r, c]
                        });
                    }
                }
            }

            ReplaySnapshots.Add(snap);
        }

        public void InitializeMap(MapSizeType size = MapSizeType.Standard, BattlefieldTheme requestedTheme = BattlefieldTheme.Random)
        {
            CurrentMapSize = size;
            if (size == MapSizeType.Huge)
            {
                CurrentTheme = (requestedTheme == BattlefieldTheme.Random)
                    ? (BattlefieldTheme)_rand.Next(5)
                    : requestedTheme;
            }
            else
            {
                CurrentTheme = BattlefieldTheme.Standard;
            }

            switch (size)
            {
                case MapSizeType.Small:
                    Cols = 12;
                    Rows = 6;
                    break;
                case MapSizeType.Standard:
                    Cols = 14;
                    Rows = 8;
                    break;
                case MapSizeType.Large:
                    Cols = 18;
                    Rows = 8;
                    break;
                case MapSizeType.Huge:
                    Cols = 20;
                    Rows = 10;
                    break;
            }

            MapTiles = new TileType[Rows, Cols];
            ObstacleHp = new int[Rows, Cols];
            Buildings = new Building[Rows, Cols];
            Units.Clear();
            _unitIdCounter = 1;

            PlayerCoreMaxHp = BaseMaxHp;
            AiCoreMaxHp = BaseMaxHp;
            PlayerCoreHp = BaseMaxHp;
            AiCoreHp = BaseMaxHp;
            TurnNumber = 1;
            CurrentTurn = Owner.Player;
            PlayerCredit = 6;
            AiCredit = 6;
            IsGameOver = false;
            Winner = Owner.None;
            IsCardSelectionActive = false;
            PendingDropUnitType = UnitType.None;
            PendingDropRemainingCount = 0;
            PendingDropCustomHp = 0;
            PendingDropCustomAtk = 0;
            PendingDropForceShield = false;

            // 통계 및 리플레이 초기화
            StatsPlayerUnitsKilled = 0;
            StatsPlayerUnitsLost = 0;
            StatsPlayerBuildingsDestroyed = 0;
            StatsObstaclesDestroyed = 0;
            StatsTotalCreditEarned = 6;
            StatsDamageDealtToAiCore = 0;
            MatchStartTime = DateTime.Now;
            HasSpawnedVanguard = false;
            HasSpawnedNonVanguard = false;
            PlayerBuiltPowerPlantsDuringGame = 0;
            PlayerBuiltTurrets = 0;
            HasSelectedUnitCard = false;
            HasSpawnedFromFactoryBridgehead = false;
            HasUsedOrbitalStrikeDestroy = false;
            ReplaySnapshots.Clear();
            CombatLogHistory.Clear();
            PendingAiCombatEvents.Clear();
            PendingAiLaserBeams.Clear();
            AiPushWaveRow = -1;
            AiPushWaveTurnsRemaining = 0;
            AiPushWaveCooldown = 0;
            LatestCombatLog = "작전 개시. 전술 명령을 대기 중입니다.";

            // 판마다 AI 전술 성향 무작위 배정 (Rush 33%, Siege 33%, Tech 34%)
            int personaRoll = _rand.Next(3);
            CurrentAiPersona = (personaRoll == 0) ? AiStrategyPersona.Rush :
                               (personaRoll == 1) ? AiStrategyPersona.Siege :
                                                    AiStrategyPersona.Tech;

            PlayerIncomeBonus = 0;
            AiIncomeBonus = 0;
            PlayerUnitHpBonus = 0;
            AiUnitHpBonus = 0;
            PlayerUnitAtkBonus = 0;
            AiUnitAtkBonus = 0;
            PlayerCostDiscount = 0;
            TurretRangeBonus = 0;

            PlayerHasSiegeAnchor = false;
            AiHasSiegeAnchor = false;
            PlayerHasColossusMelee = false;
            AiHasColossusMelee = false;
            PlayerHasCheongungTrajectory = false;
            AiHasCheongungTrajectory = false;
            PlayerHasPhalanxShield = false;
            AiHasPhalanxShield = false;
            PlayerHasStealthCloak = false;
            AiHasStealthCloak = false;
            PlayerHasTurretRadar = false;
            AiHasTurretRadar = false;
            PlayerHasKineticSlam = false;
            AiHasKineticSlam = false;
            PlayerHasChainPlasma = false;
            AiHasChainPlasma = false;

            PendingDropCustomAtk = 0;
            PendingDropForceShield = false;

            PlayerPlantLimit = (size == MapSizeType.Huge) ? 5 : 3;
            AiPlantLimit = (size == MapSizeType.Huge) ? 5 : 3;

            // 1. 기본 타일 초기화
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    MapTiles[r, c] = TileType.Plain;
                    ObstacleHp[r, c] = 0;
                    Buildings[r, c] = null;
                }
            }

            // 2. 전장 구역 대칭 분할 (공평한 중앙 장애물 존 & 좌우 자원 존)
            // 중앙 장애물 존: 맵 정중앙을 기준으로 좌우 대칭인 중립 격전 구역
            int midLeft;
            int midRight;
            if (Cols <= 12) // 소형 (12열: 0..11) -> 중앙 5, 6열 (2열)
            {
                midLeft = 5;
                midRight = 6;
            }
            else if (Cols <= 14) // 표준 (14열: 0..13) -> 중앙 5, 6, 7, 8열 (4열: 좌2, 우2)
            {
                midLeft = 5;
                midRight = 8;
            }
            else if (Cols <= 18) // 대형 (18열: 0..17) -> 중앙 7, 8, 9, 10열 (4열: 좌2, 우2)
            {
                midLeft = 7;
                midRight = 10;
            }
            else // 초대형 (20열: 0..19) -> 중앙 8, 9, 10, 11열 (4열: 좌2, 우2)
            {
                midLeft = 8;
                midRight = 11;
            }

            int targetResCount = (Cols >= 20) ? 3 : 2;

            // 좌측 자원 노드: 플레이어 전진 구역 (중앙 장애물 존 왼쪽)
            int leftColStart = Math.Max(2, midLeft - (Cols >= 20 ? 4 : 3));
            int leftColEnd = midLeft - 1;
            int leftPlaced = 0;
            int tryLeft = 0;
            while (leftPlaced < targetResCount && tryLeft < 120)
            {
                tryLeft++;
                int r = _rand.Next(Rows);
                int c = _rand.Next(leftColStart, leftColEnd + 1);
                if (MapTiles[r, c] == TileType.Plain && !(r == Rows / 2 && c <= 2))
                {
                    MapTiles[r, c] = TileType.Resource;
                    leftPlaced++;
                }
            }

            // 우측 자원 노드: AI 전진 구역 (중앙 장애물 존 오른쪽)
            int rightColStart = midRight + 1;
            int rightColEnd = Math.Min(Cols - 3, midRight + (Cols >= 20 ? 4 : 3));
            int rightPlaced = 0;
            int tryRight = 0;
            while (rightPlaced < targetResCount && tryRight < 120)
            {
                tryRight++;
                int r = _rand.Next(Rows);
                int c = _rand.Next(rightColStart, rightColEnd + 1);
                if (MapTiles[r, c] == TileType.Plain && !(r == Rows / 2 && c >= Cols - 2))
                {
                    MapTiles[r, c] = TileType.Resource;
                    rightPlaced++;
                }
            }

            // 3. 파괴 가능한 서버 랙 장애물 및 테마별 기믹 지형 무작위 배치 (중앙 전장 존)
            if (size == MapSizeType.Huge)
            {
                switch (CurrentTheme)
                {
                    case BattlefieldTheme.Inferno:
                        PlaceObstaclesAndGimmicks(midLeft, midRight, 4, TileType.Fire, 10);
                        break;
                    case BattlefieldTheme.Highway:
                        PlaceHighwayTheme(midLeft, midRight, 4);
                        break;
                    case BattlefieldTheme.NanoWell:
                        PlaceObstaclesAndGimmicks(midLeft, midRight, 4, TileType.NanoWell, 6);
                        break;
                    case BattlefieldTheme.SmokeField:
                        PlaceObstaclesAndGimmicks(midLeft, midRight, 4, TileType.Smoke, 10);
                        break;
                    default:
                        PlaceStandardObstacles(midLeft, midRight, 8);
                        break;
                }
            }
            else
            {
                PlaceStandardObstacles(midLeft, midRight, (Rows >= 10) ? 8 : 6);
            }

            // 4. 초기 기본 건물 배치 (AI는 고정/자원배치, 플레이어는 Col 1~2 직접 선택 배치 활성화)
            if (size == MapSizeType.Huge)
            {
                // 초대형: AI도 Row 3, Row 6에 발전소 2기 대칭 분산 배치
                Buildings[3, Cols - 2] = new Building { Owner = Owner.Ai, Type = BuildingType.PowerPlant, Hp = 4, MaxHp = 4, Col = Cols - 2, Row = 3 };
                Buildings[6, Cols - 2] = new Building { Owner = Owner.Ai, Type = BuildingType.PowerPlant, Hp = 4, MaxHp = 4, Col = Cols - 2, Row = 6 };
                InitialBaseRemainingCount = 2;
                InitialBaseTotalCount = 2;
                IsInitialBasePlacementActive = true;
                StatusMessage = $"⚡ [{GetThemeIcon()} {GetThemeName()}] {GetThemeStatusBrief()} 첫 발전소(1/2)를 건설하세요.";
            }
            else
            {
                int aiPlantRow = Rows / 2;
                Buildings[aiPlantRow, Cols - 2] = new Building { Owner = Owner.Ai, Type = BuildingType.PowerPlant, Hp = 4, MaxHp = 4, Col = Cols - 2, Row = aiPlantRow };
                InitialBaseRemainingCount = 1;
                InitialBaseTotalCount = 1;
                IsInitialBasePlacementActive = true;
                StatusMessage = "⚡ 작전 개시! 아군 본진 구역(Col 1~2)에 첫 무료 발전소를 배치할 위치를 클릭하십시오.";
            }

            // 5. 1턴 초기 리플레이 스냅샷 1회 기록
            RecordReplaySnapshot();
        }

        private void PlaceStandardObstacles(int midLeft, int midRight, int targetObsCount)
        {
            int obsPlaced = 0;
            int[] rowObsCounts = new int[Rows];
            int attempts = 0;
            while (obsPlaced < targetObsCount && attempts < 250)
            {
                attempts++;
                int r = _rand.Next(Rows);
                int cMin = (obsPlaced % 2 == 0) ? midLeft : (midLeft + midRight + 1) / 2;
                int cMax = (obsPlaced % 2 == 0) ? (midLeft + midRight) / 2 : midRight;
                int c = _rand.Next(cMin, cMax + 1);

                if (c >= midLeft && c <= midRight && MapTiles[r, c] == TileType.Plain && rowObsCounts[r] < 1)
                {
                    SetObstacle(r, c, 6);
                    rowObsCounts[r]++;
                    obsPlaced++;
                }
            }
            while (obsPlaced < targetObsCount)
            {
                int r = _rand.Next(Rows);
                int c = _rand.Next(midLeft, midRight + 1);
                if (MapTiles[r, c] == TileType.Plain)
                {
                    SetObstacle(r, c, 6);
                    obsPlaced++;
                }
            }
        }

        private void PlaceObstaclesAndGimmicks(int midLeft, int midRight, int obsCount, TileType gimmickType, int gimmickCount)
        {
            // 1. 서버 랙 장애물 배치 (좌/우 대칭)
            PlaceStandardObstacles(midLeft, midRight, obsCount);

            // 2. 기믹 타일 대칭 무작위 배치 (Cols: midLeft - 2 ~ midRight + 2)
            int minCol = Math.Max(3, midLeft - 2);
            int maxCol = Math.Min(Cols - 4, midRight + 2);
            int midCol = (minCol + maxCol) / 2;

            int placed = 0;
            int attempts = 0;
            while (placed < gimmickCount && attempts < 300)
            {
                attempts++;
                int r = _rand.Next(Rows);
                int cMin = (placed % 2 == 0) ? minCol : midCol + 1;
                int cMax = (placed % 2 == 0) ? midCol : maxCol;
                int c = _rand.Next(cMin, cMax + 1);

                if (MapTiles[r, c] == TileType.Plain)
                {
                    MapTiles[r, c] = gimmickType;
                    placed++;
                }
            }
        }

        private void PlaceHighwayTheme(int midLeft, int midRight, int obsCount)
        {
            // 10개 라인 중 3개 라인을 고속 가속 레일 라인으로 지정 (Row 2, Row 5, Row 8)
            int[] highwayRows = { 2, 5, 8 };
            int minCol = Math.Max(4, midLeft - 3);
            int maxCol = Math.Min(Cols - 5, midRight + 3);

            foreach (int hr in highwayRows)
            {
                for (int c = minCol; c <= maxCol; c++)
                {
                    if (MapTiles[hr, c] == TileType.Plain)
                    {
                        MapTiles[hr, c] = TileType.Highway;
                    }
                }
            }

            // 나머지 라인에 서버 랙 4개 배치 (레일 라인은 가로막지 않음)
            int obsPlaced = 0;
            int attempts = 0;
            while (obsPlaced < obsCount && attempts < 250)
            {
                attempts++;
                int r = _rand.Next(Rows);
                if (Array.IndexOf(highwayRows, r) >= 0) continue;

                int cMin = (obsPlaced % 2 == 0) ? midLeft : (midLeft + midRight + 1) / 2;
                int cMax = (obsPlaced % 2 == 0) ? (midLeft + midRight) / 2 : midRight;
                int c = _rand.Next(cMin, cMax + 1);

                if (MapTiles[r, c] == TileType.Plain)
                {
                    SetObstacle(r, c, 6);
                    obsPlaced++;
                }
            }
        }

        public bool PlaceInitialPlayerBase(int col, int row)
        {
            if (!IsInitialBasePlacementActive || InitialBaseRemainingCount <= 0) return false;
            if (col < 1 || col > 2 || row < 0 || row >= Rows) return false;
            if (MapTiles[row, col] != TileType.Plain && MapTiles[row, col] != TileType.Resource) return false;
            if (Buildings[row, col] != null) return false;

            Buildings[row, col] = new Building
            {
                Owner = Owner.Player,
                Type = BuildingType.PowerPlant,
                Hp = 4,
                MaxHp = 4,
                Col = col,
                Row = row
            };

            InitialBaseRemainingCount--;

            if (InitialBaseRemainingCount > 0)
            {
                int step = InitialBaseTotalCount - InitialBaseRemainingCount + 1;
                StatusMessage = $"⚡ 발전소 설립 완료! 다음 발전소({step}/{InitialBaseTotalCount})를 배치할 위치를 클릭하십시오.";
            }
            else
            {
                IsInitialBasePlacementActive = false;
                StatusMessage = "기지 인프라 가동 개시! 유닛이나 방어 건물을 배치하고 [턴 종료]를 누르세요.";
                RecordReplaySnapshot("초기 기지 설립 완료 (전투 준비)");
            }
            return true;
        }

        /// <summary>
        /// 턴 시작 시 건물 자체 수리 (2턴당 1회복) 및 EMP 무력화 잔여 턴수 차감
        /// </summary>
        public List<CombatEvent> ProcessTurnStartBuildingRegenAndEmp()
        {
            var events = new List<CombatEvent>();
            bool isRegenTurn = (TurnNumber % 2 == 0);

            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    var b = Buildings[r, c];
                    if (b != null && b.Hp > 0)
                    {
                        // EMP 무력화 턴수 감소
                        if (b.EmpTurns > 0)
                        {
                            b.EmpTurns--;
                        }

                        // 2턴당 체력 1 자체 수리 (최대 체력 한도 내)
                        if (isRegenTurn && b.Hp < b.MaxHp)
                        {
                            b.Hp = Math.Min(b.MaxHp, b.Hp + 1);
                            events.Add(new CombatEvent
                            {
                                Col = c,
                                Row = r,
                                Text = "+1 자가수리 🔧",
                                TextColor = (b.Owner == Owner.Player) ? Color.FromArgb(76, 175, 80) : Color.FromArgb(239, 83, 80)
                            });
                        }

                        // 수리 격납고 (RepairDepot): 턴마다 전장에 있는 부상당한 아군 유닛 1기 체력 회복 +1
                        if (b.Type == BuildingType.RepairDepot && b.EmpTurns <= 0)
                        {
                            var woundedUnits = Units.FindAll(u => u.Owner == b.Owner && u.Hp > 0 && u.Hp < u.MaxHp);
                            if (woundedUnits.Count > 0)
                            {
                                var targetUnit = woundedUnits[0];
                                foreach (var wu in woundedUnits)
                                {
                                    if (wu.Hp < targetUnit.Hp) targetUnit = wu;
                                }
                                targetUnit.Hp = Math.Min(targetUnit.MaxHp, targetUnit.Hp + 2);
                                targetUnit.HitFlashAlpha = 0.8f;
                                events.Add(new CombatEvent
                                {
                                    Col = targetUnit.Col,
                                    Row = targetUnit.Row,
                                    Text = "+2 격납고 수리 🏥",
                                    TextColor = (targetUnit.Owner == Owner.Player) ? Color.FromArgb(76, 175, 80) : Color.FromArgb(239, 83, 80)
                                });
                            }
                        }
                    }
                }
            }
            return events;
        }

        private void SetObstacle(int r, int c, int hp)
        {
            MapTiles[r, c] = TileType.Obstacle;
            ObstacleHp[r, c] = hp;
        }

        #region Cost Helpers

        public int GetBuildingCost(BuildingType type, Owner owner = Owner.Player)
        {
            int baseCost;
            switch (type)
            {
                case BuildingType.PowerPlant:
                    // 발전소 건설비 점진 가중치 완화: 기본 6G + 보유당 +3G (회수 기간 단축 및 접근성 개선)
                    // 0기: 6G, 1기: 9G, 2기: 12G, 3기: 15G...
                    int plantCount = GetBuildingCount(BuildingType.PowerPlant, owner);
                    baseCost = 6 + (plantCount * 3);
                    break;
                case BuildingType.Turret:
                    // 포탑 건설비 점진 가중치: 기본 6G + 진영별 기존 보유 포탑 1기당 +2G (파괴 시 비용 복원)
                    int turretCount = GetBuildingCount(BuildingType.Turret, owner);
                    baseCost = 6 + (turretCount * 2);
                    break;
                case BuildingType.Wall: baseCost = 3; break;
                case BuildingType.Factory: baseCost = 8; break; // 8G로 완화
                case BuildingType.RepairDepot: baseCost = 6; break; // 6G, HP 6, 턴당 부상 아군 1기 +2 수리
                case BuildingType.PhaseGate: baseCost = 5; break;   // 5G, HP 6, 통과하는 유닛 이동력 +1
                default: baseCost = 0; break;
            }
            int discount = (owner == Owner.Player) ? PlayerCostDiscount : 0;
            return Math.Max(1, baseCost - discount);
        }

        public int GetBuildingCount(BuildingType type, Owner owner)
        {
            int count = 0;
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    if (Buildings[r, c] != null && Buildings[r, c].Owner == owner && Buildings[r, c].Type == type)
                        count++;
                }
            }
            return count;
        }

        public int GetUnitCost(UnitType type)
        {
            int baseCost;
            switch (type)
            {
                case UnitType.Vanguard: baseCost = 2; break;
                case UnitType.Raider: baseCost = 4; break;
                case UnitType.Titan: baseCost = 10; break;
                case UnitType.Colossus: baseCost = 14; break;
                case UnitType.Dreadnought: baseCost = 18; break;
                case UnitType.Cheongung: baseCost = 12; break; // 12G (대건물/서버랙 6뎀 공성 결전 미사일 유닛)
                default: baseCost = 0; break;
            }
            return Math.Max(1, baseCost - PlayerCostDiscount);
        }

        public int GetSkillCost(UserSkillType skill)
        {
            switch (skill)
            {
                case UserSkillType.OrbitalStrike: return 8; // [1] 8G: 단일 건물/서버랙 즉시 6뎀
                case UserSkillType.EmpJammer: return 5;     // [2] 5G: 건물 및 3x3 범위 2턴간 기능 정지
                case UserSkillType.HealingBeam: return 4;   // [3] 4G: 지정 행(Row) 아군 유닛+건물 체력 +6 회복
                case UserSkillType.FortifyBeam: return 5;   // [4] 5G: 지정 행(Row) 아군 유닛 최대 체력 +2 & 현재 체력 +2
                case UserSkillType.ReconScan: return 3;     // [5] 3G: 전장의 모든 은폐 침투병 강제 해제
                case UserSkillType.NanoRepair: return 4;    // (구버전 호환용)
                default: return 0;
            }
        }

        public bool CastSkill(UserSkillType skill, int targetCol = 0, int targetRow = 0, Owner caster = Owner.Player, bool isFree = false)
        {
            return CastSkill(skill, targetCol, targetRow, caster, out _, out _, isFree);
        }

        public bool CastSkill(UserSkillType skill, int targetCol, int targetRow, Owner caster, out List<CombatEvent> outEvents, out CombatLaserBeam outBeam, bool isFree = false)
        {
            outEvents = new List<CombatEvent>();
            outBeam = null;
            int cost = isFree ? 0 : GetSkillCost(skill);
            int credit = (caster == Owner.Player) ? PlayerCredit : AiCredit;
            if (!isFree && credit < cost) return false;

            Owner opponent = (caster == Owner.Player) ? Owner.Ai : Owner.Player;

            switch (skill)
            {
                case UserSkillType.NanoRepair:
                    if (!isFree)
                    {
                        if (caster == Owner.Player) PlayerCredit -= cost;
                        else AiCredit -= cost;
                    }
                    for (int r = 0; r < Rows; r++)
                    {
                        for (int c = 0; c < Cols; c++)
                        {
                            var b = Buildings[r, c];
                            if (b != null && b.Owner == caster && b.Hp > 0)
                            {
                                b.Hp = Math.Min(b.MaxHp, b.Hp + 3);
                                b.HitFlashAlpha = 0.8f;
                                outEvents.Add(new CombatEvent { Col = c, Row = r, Text = "+3 나노수리 🔧", TextColor = Color.FromArgb(76, 175, 80) });
                            }
                        }
                    }
                    AddCombatLog($"🔧 [{(caster == Owner.Player ? "플레이어" : "AI 전술")}] 전역 나노 복구 네트워크 가동 (+3 수리)");
                    return true;

                case UserSkillType.ReconScan:
                    if (!isFree)
                    {
                        if (caster == Owner.Player) PlayerCredit -= cost;
                        else AiCredit -= cost;
                    }
                    int revealed = 0;
                    foreach (var u in Units)
                    {
                        if (u.Owner == opponent && u.IsCloaked)
                        {
                            u.IsCloaked = false;
                            revealed++;
                            outEvents.Add(new CombatEvent { Col = u.Col, Row = u.Row, Text = "스텔스 노출! 📡", TextColor = Color.FromArgb(0, 229, 255) });
                        }
                    }
                    if (revealed == 0)
                    {
                        outEvents.Add(new CombatEvent { Col = Cols / 2, Row = Rows / 2, Text = "스캔 완료 (은폐 없음) 📡", TextColor = Color.FromArgb(0, 229, 255) });
                    }
                    AddCombatLog($"📡 [{(caster == Owner.Player ? "플레이어" : "AI 전술")}] 정찰스캔 가동! {(revealed > 0 ? $"적 은폐 {revealed}기 해제" : "은폐 없음")}");
                    return true;
                case UserSkillType.OrbitalStrike:
                    if (targetCol <= 0 || targetCol >= Cols - 1 || targetRow < 0 || targetRow >= Rows) return false;
                    var targetBldg = Buildings[targetRow, targetCol];
                    bool isObstacle = (MapTiles[targetRow, targetCol] == TileType.Obstacle);
                    if (targetBldg == null && !isObstacle) return false;
                    if (targetBldg != null && targetBldg.Owner != opponent) return false;

                    if (!isFree)
                    {
                        if (caster == Owner.Player) PlayerCredit -= cost;
                        else AiCredit -= cost;
                    }

                    outBeam = new CombatLaserBeam
                    {
                        StartCol = targetCol,
                        StartRow = -1, // 하늘에서 내리꽂히는 궤도 레이저
                        EndCol = targetCol,
                        EndRow = targetRow,
                        BeamColor = Color.FromArgb(255, 60, 60),
                        Alpha = 1.0f
                    };

                    if (targetBldg != null)
                    {
                        targetBldg.Hp -= 6;
                        targetBldg.HitFlashAlpha = 1.0f;
                        targetBldg.ShakeOffset = 6f;
                        outEvents.Add(new CombatEvent { Col = targetCol, Row = targetRow, Text = "-6 궤도폭격! 💥", TextColor = Color.FromArgb(255, 23, 68), IsCritical = true });
                        if (targetBldg.Hp <= 0)
                        {
                            if (targetBldg.Owner == Owner.Ai) StatsPlayerBuildingsDestroyed++;
                            Buildings[targetRow, targetCol] = null;
                            if (caster == Owner.Player) HasUsedOrbitalStrikeDestroy = true;
                        }
                    }
                    else if (isObstacle)
                    {
                        ObstacleHp[targetRow, targetCol] -= 6;
                        outEvents.Add(new CombatEvent { Col = targetCol, Row = targetRow, Text = "-6 궤도폭격! 💥", TextColor = Color.FromArgb(255, 23, 68), IsCritical = true });
                        if (ObstacleHp[targetRow, targetCol] <= 0)
                        {
                            MapTiles[targetRow, targetCol] = TileType.Plain;
                            StatsObstaclesDestroyed++;
                            if (caster == Owner.Player) HasUsedOrbitalStrikeDestroy = true;
                            if (caster == Owner.Player)
                            {
                                PlayerCredit += 2;
                                StatsTotalCreditEarned += 2;
                            }
                            else AiCredit += 2;
                            string rewardText = (caster == Owner.Player) ? "아군 랙 완파! +2G 국고 ⚡" : "적 랙 완파! (적 +2G) ⚡";
                            Color rewardCol = (caster == Owner.Player) ? Color.FromArgb(255, 215, 0) : Color.FromArgb(255, 110, 64);
                            outEvents.Add(new CombatEvent { Col = targetCol, Row = targetRow, Text = rewardText, TextColor = rewardCol, IsCritical = true });
                        }
                    }
                    string tBldgName = targetBldg != null ? targetBldg.Type.ToString() : "서버 랙";
                    AddCombatLog($"💥 [{(caster == Owner.Player ? "플레이어" : "AI 전술")}] ({targetRow + 1}행 {targetCol + 1}열) {tBldgName}에 궤도폭격 직격! (-6 피해)");
                    return true;

                case UserSkillType.EmpJammer:
                    if (targetCol <= 0 || targetCol >= Cols - 1 || targetRow < 0 || targetRow >= Rows) return false;

                    if (!isFree)
                    {
                        if (caster == Owner.Player) PlayerCredit -= cost;
                        else AiCredit -= cost;
                    }

                    int empCount = 0;
                    for (int r = Math.Max(0, targetRow - 1); r <= Math.Min(Rows - 1, targetRow + 1); r++)
                    {
                        for (int c = Math.Max(1, targetCol - 1); c <= Math.Min(Cols - 2, targetCol + 1); c++)
                        {
                            var b = Buildings[r, c];
                            if (b != null && b.Owner == opponent && b.Hp > 0)
                            {
                                b.EmpTurns = Math.Max(b.EmpTurns, 2);
                                b.HitFlashAlpha = 1.0f;
                                empCount++;
                                outEvents.Add(new CombatEvent { Col = c, Row = r, Text = "⚡EMP 2T 정지!", TextColor = Color.FromArgb(0, 229, 255) });
                            }
                        }
                    }
                    if (empCount == 0)
                    {
                        outEvents.Add(new CombatEvent { Col = targetCol, Row = targetRow, Text = "⚡EMP 방전 (적 건물 없음)", TextColor = Color.FromArgb(0, 229, 255) });
                    }
                    AddCombatLog($"⚡ [{(caster == Owner.Player ? "플레이어" : "AI 전술")}] ({targetRow + 1}행 {targetCol + 1}열) 방어선에 EMP 교란 투하! (2턴 기능정지)");
                    return true;

                case UserSkillType.HealingBeam:
                    if (targetRow < 0 || targetRow >= Rows) return false;

                    if (!isFree)
                    {
                        if (caster == Owner.Player) PlayerCredit -= cost;
                        else AiCredit -= cost;
                    }

                    outBeam = new CombatLaserBeam
                    {
                        StartCol = 0,
                        StartRow = targetRow,
                        EndCol = Cols - 1,
                        EndRow = targetRow,
                        BeamColor = Color.FromArgb(0, 230, 118),
                        Alpha = 1.0f
                    };

                    int healedCount = 0;
                    // 1. 해당 행 아군 유닛 집중 치유 (+6 HP)
                    foreach (var u in Units)
                    {
                        if (u.Owner == caster && u.Row == targetRow && u.Hp > 0)
                        {
                            if (u.Hp < u.MaxHp)
                            {
                                u.Hp = Math.Min(u.MaxHp, u.Hp + 6);
                                u.HitFlashAlpha = 0.8f;
                                outEvents.Add(new CombatEvent { Col = u.Col, Row = u.Row, Text = "+6 치료 💚", TextColor = Color.FromArgb(0, 230, 118) });
                                healedCount++;
                            }
                        }
                    }
                    // 2. 해당 행 아군 건물 동시 수리 (+6 HP)
                    for (int c = 1; c < Cols - 1; c++)
                    {
                        var b = Buildings[targetRow, c];
                        if (b != null && b.Owner == caster && b.Hp > 0 && b.Hp < b.MaxHp)
                        {
                            b.Hp = Math.Min(b.MaxHp, b.Hp + 6);
                            b.HitFlashAlpha = 0.8f;
                            outEvents.Add(new CombatEvent { Col = c, Row = targetRow, Text = "+6 수리 💚", TextColor = Color.FromArgb(0, 230, 118) });
                            healedCount++;
                        }
                    }
                    if (healedCount == 0)
                    {
                        outEvents.Add(new CombatEvent { Col = Cols / 2, Row = targetRow, Text = "치료 광선 조사 완료 💚", TextColor = Color.FromArgb(0, 230, 118) });
                    }
                    AddCombatLog($"💚 [{(caster == Owner.Player ? "플레이어" : "AI 전술")}] {targetRow + 1}행에 치료광선 조사! (+6 HP 수복)");
                    return true;

                case UserSkillType.FortifyBeam:
                    if (targetRow < 0 || targetRow >= Rows) return false;

                    if (!isFree)
                    {
                        if (caster == Owner.Player) PlayerCredit -= cost;
                        else AiCredit -= cost;
                    }

                    outBeam = new CombatLaserBeam
                    {
                        StartCol = 0,
                        StartRow = targetRow,
                        EndCol = Cols - 1,
                        EndRow = targetRow,
                        BeamColor = Color.FromArgb(0, 229, 255),
                        Alpha = 1.0f
                    };

                    int fortifyCount = 0;
                    foreach (var u in Units)
                    {
                        if (u.Owner == caster && u.Row == targetRow && u.Hp > 0)
                        {
                            u.MaxHp += 2;
                            u.Hp += 2;
                            u.HitFlashAlpha = 0.8f;
                            outEvents.Add(new CombatEvent { Col = u.Col, Row = u.Row, Text = "+2 구축 🛡️", TextColor = Color.FromArgb(0, 229, 255) });
                            fortifyCount++;
                        }
                    }
                    if (fortifyCount == 0)
                    {
                        outEvents.Add(new CombatEvent { Col = Cols / 2, Row = targetRow, Text = "구축 광선 조사 완료 🛡️", TextColor = Color.FromArgb(0, 229, 255) });
                    }
                    AddCombatLog($"🛡️ [{(caster == Owner.Player ? "플레이어" : "AI 전술")}] {targetRow + 1}행 방어선에 구축광선 강화! (+2 내구도)");
                    return true;
            }
            return false;
        }

        public int CalculateIncome(Owner owner)
        {
            int income = 2; // 기본 수입
            income += (owner == Owner.Player) ? PlayerIncomeBonus : AiIncomeBonus;

            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    var b = Buildings[r, c];
                    // EMP 상태인 발전소는 크레딧을 생산하지 못함!
                    if (b != null && b.Owner == owner && b.Type == BuildingType.PowerPlant && b.EmpTurns <= 0)
                    {
                        // 자원 노드 위 발전소는 3G (일반의 3배), 일반 타일은 1G
                        income += (MapTiles[r, c] == TileType.Resource) ? 3 : 1;
                    }
                }
            }
            return income;
        }

        #endregion

        #region Placement Rules

        public bool IsValidBuildingPlacement(int col, int row, Owner owner)
        {
            if (col <= 0 || col >= Cols - 1 || row < 0 || row >= Rows) return false;
            if (MapTiles[row, col] != TileType.Plain && MapTiles[row, col] != TileType.Resource) return false;
            if (Buildings[row, col] != null) return false;
            if (Units.Exists(u => u.Col == col && u.Row == row)) return false;

            // 건설 가능 영역: 아군 진영 베이스라인(Col 1~2 for Player, Col (Cols-3)~(Cols-2) for AI)
            // 또는 아군 기존 건물 인접 2칸 이내
            if (owner == Owner.Player)
            {
                if (col <= 2) return true;
            }
            else
            {
                if (col >= Cols - 3) return true;
            }

            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    if (Buildings[r, c] != null && Buildings[r, c].Owner == owner)
                    {
                        int dist = Math.Abs(col - c) + Math.Abs(row - r);
                        if (dist <= 2) return true;
                    }
                }
            }

            return false;
        }

        public bool IsValidUnitSpawnPlacement(int col, int row, Owner owner)
        {
            if (col <= 0 || col >= Cols - 1 || row < 0 || row >= Rows) return false;
            if (MapTiles[row, col] == TileType.Obstacle) return false;
            if (Buildings[row, col] != null && Buildings[row, col].Type == BuildingType.Wall) return false;
            if (Units.Exists(u => u.Col == col && u.Row == row)) return false;

            // 스폰 가능 위치:
            // 1. 본진 앞 베이스라인 (소형/표준: Col 1, 대형/초대형: Col 1~2)
            if (owner == Owner.Player)
            {
                if (col == 1) return true;
                if ((CurrentMapSize == MapSizeType.Large || CurrentMapSize == MapSizeType.Huge) && col == 2) return true;
            }
            else
            {
                if (col == Cols - 2) return true;
                if ((CurrentMapSize == MapSizeType.Large || CurrentMapSize == MapSizeType.Huge) && col == Cols - 3) return true;
            }

            // 2. 아군 전진공장(Factory)의 전진 교두보 (공장 앞 1~2칸 및 상하 인접 타일에서 신속 스폰)
            for (int r = Math.Max(0, row - 1); r <= Math.Min(Rows - 1, row + 1); r++)
            {
                for (int c = 1; c < Cols - 1; c++)
                {
                    var b = Buildings[r, c];
                    if (b != null && b.Owner == owner && b.Type == BuildingType.Factory && b.Hp > 0 && b.EmpTurns == 0)
                    {
                        // 플레이어 공장: 공장 동쪽 1~2칸 (상하 1칸 범위 내 교두보)
                        if (owner == Owner.Player && col > c && col <= c + 2)
                            return true;
                        // AI 공장: 공장 서쪽 1~2칸 (상하 1칸 범위 내 교두보)
                        if (owner == Owner.Ai && col < c && col >= c - 2)
                            return true;
                    }
                }
            }

            return false;
        }

        public bool Build(int col, int row, BuildingType type, Owner owner)
        {
            // 발전소는 진영당 최대 한도(기본 3기, 업그레이드/카드로 확장 가능)로 제한
            if (type == BuildingType.PowerPlant && GetBuildingCount(BuildingType.PowerPlant, owner) >= GetPlantLimit(owner))
                return false;

            int cost = GetBuildingCost(type, owner);
            int credit = (owner == Owner.Player) ? PlayerCredit : AiCredit;
            if (credit < cost) return false;
            if (!IsValidBuildingPlacement(col, row, owner)) return false;

            if (owner == Owner.Player) PlayerCredit -= cost;
            else AiCredit -= cost;

            int hp;
            switch (type)
            {
                case BuildingType.PowerPlant: hp = 4; break;
                case BuildingType.Turret: hp = 6; break;
                case BuildingType.Wall: hp = 8; break;
                case BuildingType.Factory: hp = 10; break;
                case BuildingType.RepairDepot: hp = 6; break;
                case BuildingType.PhaseGate: hp = 6; break;
                default: hp = 5; break;
            }

            Buildings[row, col] = new Building
            {
                Owner = owner,
                Type = type,
                Hp = hp,
                MaxHp = hp,
                Col = col,
                Row = row
            };

            string bName = (type == BuildingType.PowerPlant) ? "발전소" :
                           (type == BuildingType.Turret) ? "포탑" :
                           (type == BuildingType.Wall) ? "방화벽" :
                           (type == BuildingType.Factory) ? "전진공장" :
                           (type == BuildingType.RepairDepot) ? "수리격납고" :
                           (type == BuildingType.PhaseGate) ? "가속관문" : "건물";
            AddCombatLog($"🏗️ [{(owner == Owner.Player ? "플레이어" : "AI 전술")}] ({row + 1}행 {col + 1}열) {bName} 건설 완료");

            if (owner == Owner.Player)
            {
                if (type == BuildingType.PowerPlant) PlayerBuiltPowerPlantsDuringGame++;
                else if (type == BuildingType.Turret) PlayerBuiltTurrets++;
            }

            return true;
        }

        public bool SpawnUnit(int col, int row, UnitType type, Owner owner)
        {
            int cost = GetUnitCost(type);
            int credit = (owner == Owner.Player) ? PlayerCredit : AiCredit;
            if (credit < cost) return false;
            if (!IsValidUnitSpawnPlacement(col, row, owner)) return false;

            if (owner == Owner.Player) PlayerCredit -= cost;
            else AiCredit -= cost;

            int hp = 3;
            int atk = 1;
            int spd = 1;
            int rng = 1;
            int minRng = 1;

            switch (type)
            {
                case UnitType.Vanguard:
                    hp = 3 + ((owner == Owner.Player) ? PlayerUnitHpBonus : AiUnitHpBonus);
                    atk = 1 + ((owner == Owner.Player) ? PlayerUnitAtkBonus : AiUnitAtkBonus);
                    spd = 1;
                    rng = 1;
                    minRng = 1;
                    break;
                case UnitType.Raider:
                    hp = 3 + ((owner == Owner.Player) ? PlayerUnitHpBonus : AiUnitHpBonus);
                    atk = 2 + ((owner == Owner.Player) ? PlayerUnitAtkBonus : AiUnitAtkBonus);
                    spd = 2; // 2칸 질주
                    rng = 1;
                    minRng = 1;
                    break;
                case UnitType.Titan:
                    hp = 10 + ((owner == Owner.Player) ? PlayerUnitHpBonus : AiUnitHpBonus);
                    atk = 3 + ((owner == Owner.Player) ? PlayerUnitAtkBonus : AiUnitAtkBonus);
                    spd = 1;
                    rng = 2; // 2칸 원거리 공성 사격
                    minRng = 1;
                    break;
                case UnitType.Colossus:
                    hp = 8 + ((owner == Owner.Player) ? PlayerUnitHpBonus : AiUnitHpBonus);
                    atk = 4 + ((owner == Owner.Player) ? PlayerUnitAtkBonus : AiUnitAtkBonus);
                    spd = 1;
                    rng = 3; // 3칸 초장거리 포격
                    // 근접 방어 발칸포 카드 해금 시 근접 1칸 사격 및 반격 허용!
                    minRng = ((owner == Owner.Player ? PlayerHasColossusMelee : AiHasColossusMelee) ? 1 : 2);
                    break;
                case UnitType.Dreadnought:
                    hp = 12 + ((owner == Owner.Player) ? PlayerUnitHpBonus : AiUnitHpBonus);
                    atk = 3 + ((owner == Owner.Player) ? PlayerUnitAtkBonus : AiUnitAtkBonus);
                    spd = 1;
                    rng = 2; // 2칸 사거리 (십자 스플래시 방전)
                    minRng = 1;
                    break;
                case UnitType.Cheongung:
                    hp = 5 + ((owner == Owner.Player) ? PlayerUnitHpBonus : AiUnitHpBonus);
                    atk = 3 + ((owner == Owner.Player) ? PlayerUnitAtkBonus : AiUnitAtkBonus);
                    spd = 1;
                    bool hasTraj = (owner == Owner.Player) ? PlayerHasCheongungTrajectory : AiHasCheongungTrajectory;
                    rng = hasTraj ? 5 : 4;
                    minRng = hasTraj ? 3 : 2;
                    break;
            }

            bool hasShield = (type == UnitType.Vanguard) && ((owner == Owner.Player) ? PlayerHasPhalanxShield : AiHasPhalanxShield);
            bool isCloaked = (type == UnitType.Raider) && ((owner == Owner.Player) ? PlayerHasStealthCloak : AiHasStealthCloak);

            Units.Add(new Unit
            {
                Id = _unitIdCounter++,
                Owner = owner,
                Type = type,
                Hp = hp,
                MaxHp = hp,
                Atk = atk,
                Speed = spd,
                Range = rng,
                MinRange = minRng,
                HasShield = hasShield,
                IsCloaked = isCloaked,
                Col = col,
                Row = row,
                VisualCol = col,
                TargetCol = col,
                HasActed = true // 소환된 턴에는 즉시 진격하지 않음
            });

            if (owner == Owner.Player)
            {
                if (type == UnitType.Vanguard) HasSpawnedVanguard = true;
                else HasSpawnedNonVanguard = true;

                bool isBaseCol = (col == 1 || ((CurrentMapSize == MapSizeType.Large || CurrentMapSize == MapSizeType.Huge) && col == 2));
                if (!isBaseCol)
                {
                    HasSpawnedFromFactoryBridgehead = true;
                }
            }

            return true;
        }

        /// <summary>
        /// 프로토콜 카드(공수/증원 등)를 통한 무료 타일 직접 지정 스폰
        /// </summary>
        public bool SpawnUnitDirect(int col, int row, UnitType type, Owner owner)
        {
            if (col <= 0 || col >= Cols - 1 || row < 0 || row >= Rows) return false;
            if (MapTiles[row, col] == TileType.Obstacle) return false;
            if (Buildings[row, col] != null && Buildings[row, col].Type == BuildingType.Wall) return false;
            if (Units.Exists(u => u.Col == col && u.Row == row)) return false;

            int hp = 3;
            int atk = 1;
            int spd = 1;
            int rng = 1;
            int minRng = 1;

            switch (type)
            {
                case UnitType.Vanguard:
                    int vBaseHp = (PendingDropCustomHp > 0) ? PendingDropCustomHp : 3;
                    int vBaseAtk = (PendingDropCustomAtk > 0) ? PendingDropCustomAtk : 1;
                    hp = vBaseHp + ((owner == Owner.Player) ? PlayerUnitHpBonus : AiUnitHpBonus);
                    atk = vBaseAtk + ((owner == Owner.Player) ? PlayerUnitAtkBonus : AiUnitAtkBonus);
                    spd = 1;
                    rng = 1;
                    minRng = 1;
                    break;
                case UnitType.Raider:
                    hp = 3 + ((owner == Owner.Player) ? PlayerUnitHpBonus : AiUnitHpBonus);
                    atk = 2 + ((owner == Owner.Player) ? PlayerUnitAtkBonus : AiUnitAtkBonus);
                    spd = 2;
                    rng = 1;
                    minRng = 1;
                    break;
                case UnitType.Titan:
                    hp = 10 + ((owner == Owner.Player) ? PlayerUnitHpBonus : AiUnitHpBonus);
                    atk = 3 + ((owner == Owner.Player) ? PlayerUnitAtkBonus : AiUnitAtkBonus);
                    spd = 1;
                    rng = 2;
                    minRng = 1;
                    break;
                case UnitType.Colossus:
                    hp = 8 + ((owner == Owner.Player) ? PlayerUnitHpBonus : AiUnitHpBonus);
                    atk = 4 + ((owner == Owner.Player) ? PlayerUnitAtkBonus : AiUnitAtkBonus);
                    spd = 1;
                    rng = 3;
                    minRng = ((owner == Owner.Player ? PlayerHasColossusMelee : AiHasColossusMelee) ? 1 : 2);
                    break;
                case UnitType.Dreadnought:
                    hp = 12 + ((owner == Owner.Player) ? PlayerUnitHpBonus : AiUnitHpBonus);
                    atk = 3 + ((owner == Owner.Player) ? PlayerUnitAtkBonus : AiUnitAtkBonus);
                    spd = 1;
                    rng = 2;
                    minRng = 1;
                    break;
                case UnitType.Cheongung:
                    hp = 5 + ((owner == Owner.Player) ? PlayerUnitHpBonus : AiUnitHpBonus);
                    atk = 3 + ((owner == Owner.Player) ? PlayerUnitAtkBonus : AiUnitAtkBonus);
                    spd = 1;
                    bool hasTraj = (owner == Owner.Player) ? PlayerHasCheongungTrajectory : AiHasCheongungTrajectory;
                    rng = hasTraj ? 5 : 4;
                    minRng = hasTraj ? 3 : 2;
                    break;
            }

            bool hasShield = (type == UnitType.Vanguard) && (((owner == Owner.Player) ? PlayerHasPhalanxShield : AiHasPhalanxShield) || PendingDropForceShield);
            bool isCloaked = (type == UnitType.Raider) && ((owner == Owner.Player) ? PlayerHasStealthCloak : AiHasStealthCloak);

            Units.Add(new Unit
            {
                Id = _unitIdCounter++,
                Owner = owner,
                Type = type,
                Hp = hp,
                MaxHp = hp,
                Atk = atk,
                Speed = spd,
                Range = rng,
                MinRange = minRng,
                HasShield = hasShield,
                IsCloaked = isCloaked,
                Col = col,
                Row = row,
                VisualCol = col,
                TargetCol = col,
                HasActed = false // 무료 공수 유닛은 즉시 전열 참여 가능
            });

            if (owner == Owner.Player)
            {
                if (type == UnitType.Vanguard) HasSpawnedVanguard = true;
                else HasSpawnedNonVanguard = true;
            }

            return true;
        }

        #endregion

        #region Step-by-Step Turn & Combat Engine (타격감 & 시각화 지원)

        /// <summary>
        /// 포탑의 전방위 요격 범위 판정 (인접 1칸 사각지대 제외, 거리 2~3칸 요격망)
        /// </summary>
        public static bool IsTileInTurretRange(int turretRow, int turretCol, int targetRow, int targetCol, int rangeBonus = 0)
        {
            int dr = Math.Abs(targetRow - turretRow);
            int dc = Math.Abs(targetCol - turretCol);

            // 포탑 사각지대: 바로 인접 1칸(상하좌우 및 본인 타일)은 포탑이 요격할 수 없음!
            if (dr + dc <= 1) return false;

            if (rangeBonus == 0)
            {
                // 기본 요격망: 3x3 대각 4칸(dr==1 && dc==1) + 상하좌우 십자 2칸(dr==2, dc==0 또는 dr==0, dc==2)
                if (dr <= 1 && dc <= 1) return true;
                if ((dr == 2 && dc == 0) || (dr == 0 && dc == 2)) return true;
                return false;
            }
            else
            {
                // 보너스 사거리(레일건 개조 카드) 적용 시
                int limitDist = 2 + rangeBonus;
                if (dr + dc <= limitDist) return true;
                if (dr <= 1 + rangeBonus && dc <= 1 + rangeBonus) return true;
                return false;
            }
        }

        private bool IsTurretLineBlockedByObstacle(int tr, int tc, int targetR, int targetC)
        {
            if (MapTiles[targetR, targetC] == TileType.Obstacle) return true;

            // 동일 행(Row)인 경우 직선 경로 사이 장애물 체크
            if (tr == targetR)
            {
                int minC = Math.Min(tc, targetC);
                int maxC = Math.Max(tc, targetC);
                for (int c = minC + 1; c < maxC; c++)
                {
                    if (MapTiles[tr, c] == TileType.Obstacle) return true;
                }
            }
            // 동일 열(Col)인 경우 직선 경로 사이 장애물 체크
            else if (tc == targetC)
            {
                int minR = Math.Min(tr, targetR);
                int maxR = Math.Max(tr, targetR);
                for (int r = minR + 1; r < maxR; r++)
                {
                    if (MapTiles[r, tc] == TileType.Obstacle) return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 1. 포탑 사격 실행 (13칸 전방위 요격 & 가장 위협적인 단일 타깃 우선 포격)
        /// </summary>
        public List<CombatLaserBeam> ExecuteTurretAttacks(Owner actingOwner, List<CombatEvent> outEvents)
        {
            var beams = new List<CombatLaserBeam>();
            Owner opponent = (actingOwner == Owner.Player) ? Owner.Ai : Owner.Player;
            Color beamColor = (actingOwner == Owner.Player) ? Color.FromArgb(0, 230, 118) : Color.FromArgb(255, 23, 68);

            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    var b = Buildings[r, c];
                    if (b != null && b.Owner == actingOwner && b.Type == BuildingType.Turret && b.Hp > 0 && b.EmpTurns <= 0)
                    {
                        int rangeBonus = (actingOwner == Owner.Player) ? TurretRangeBonus : 0;

                        // 13칸 범위 내 적 유닛들 중 가장 위협적인(본진에 가까운) 유닛 탐색
                        Unit bestTargetUnit = null;
                        int bestThreatScore = -9999;
                        bool hasRadar = (actingOwner == Owner.Player) ? PlayerHasTurretRadar : AiHasTurretRadar;

                        foreach (var u in Units)
                        {
                            if (u.Owner != opponent || u.Hp <= 0) continue;
                            if (!IsTileInTurretRange(r, c, u.Row, u.Col, rangeBonus)) continue;

                            // [은폐 및 연막 감지 체크]: 침투병이 은폐 상태이거나 스모크 타일에 있고 포탑 진영에 레이더가 없으면 요격 불가!
                            bool inSmoke = (MapTiles[u.Row, u.Col] == TileType.Smoke);
                            if ((u.IsCloaked || inSmoke) && !hasRadar) continue;

                            // 직선 경로에 장애물 랙이 가로막고 있는지 확인 (직선 시야 차단)
                            if (IsTurretLineBlockedByObstacle(r, c, u.Row, u.Col)) continue;

                            // 위협도 점수:
                            // 플레이어 포탑: 적 유닛의 Col이 작을수록(플레이어 코어 쪽으로 더 깊이 침공할수록) 위협적
                            // AI 포탑: 적 유닛의 Col이 클수록(AI 코어 쪽으로 더 깊이 침공할수록) 위협적
                            int threatScore = (actingOwner == Owner.Player) ? (100 - u.Col * 5) : (u.Col * 5);
                            // 남은 체력이 적은 유닛 우선 마무리(+10점)
                            threatScore += (10 - Math.Min(10, u.Hp));

                            if (threatScore > bestThreatScore)
                            {
                                bestThreatScore = threatScore;
                                bestTargetUnit = u;
                            }
                        }

                        if (bestTargetUnit != null)
                        {
                            int dmg = 2;
                            if (bestTargetUnit.HasShield)
                            {
                                bestTargetUnit.HasShield = false;
                                dmg = 0;
                                outEvents.Add(new CombatEvent
                                {
                                    Col = bestTargetUnit.Col,
                                    Row = bestTargetUnit.Row,
                                    Text = "실드 방어! 🛡️",
                                    TextColor = Color.FromArgb(64, 196, 255)
                                });
                            }
                            else
                            {
                                bestTargetUnit.Hp -= dmg;
                                bestTargetUnit.HitFlashAlpha = 1.0f; // 피격 플래시!
                                bestTargetUnit.ShakeOffset = (actingOwner == Owner.Player) ? 4f : -4f;
                                outEvents.Add(new CombatEvent
                                {
                                    Col = bestTargetUnit.Col,
                                    Row = bestTargetUnit.Row,
                                    Text = $"-{dmg} ⚡",
                                    TextColor = beamColor
                                });
                            }

                            beams.Add(new CombatLaserBeam
                            {
                                StartCol = c,
                                StartRow = r,
                                EndCol = bestTargetUnit.Col,
                                EndRow = bestTargetUnit.Row,
                                BeamColor = beamColor,
                                Alpha = 1.0f
                            });

                            continue;
                        }

                        // 유닛이 없다면 13칸 범위 내 적 건물도 포격 가능
                        Building bestTargetBldg = null;
                        int targetBldgR = -1, targetBldgC = -1;

                        for (int br = 0; br < Rows; br++)
                        {
                            for (int bc = 0; bc < Cols; bc++)
                            {
                                var enemyBldg = Buildings[br, bc];
                                if (enemyBldg != null && enemyBldg.Owner == opponent && enemyBldg.Hp > 0)
                                {
                                    if (IsTileInTurretRange(r, c, br, bc, rangeBonus) && !IsTurretLineBlockedByObstacle(r, c, br, bc))
                                    {
                                        bestTargetBldg = enemyBldg;
                                        targetBldgR = br;
                                        targetBldgC = bc;
                                        break;
                                    }
                                }
                            }
                            if (bestTargetBldg != null) break;
                        }

                        if (bestTargetBldg != null)
                        {
                            int dmg = 2;
                            bestTargetBldg.Hp -= dmg;
                            bestTargetBldg.HitFlashAlpha = 1.0f;
                            bestTargetBldg.ShakeOffset = (actingOwner == Owner.Player) ? 4f : -4f;

                            beams.Add(new CombatLaserBeam
                            {
                                StartCol = c,
                                StartRow = r,
                                EndCol = targetBldgC,
                                EndRow = targetBldgR,
                                BeamColor = beamColor,
                                Alpha = 1.0f
                            });

                            outEvents.Add(new CombatEvent
                            {
                                Col = targetBldgC,
                                Row = targetBldgR,
                                Text = $"-{dmg} ⚡",
                                TextColor = beamColor
                            });

                            if (bestTargetBldg.Hp <= 0)
                            {
                                Buildings[targetBldgR, targetBldgC] = null;
                                if (bestTargetBldg.Owner == Owner.Ai) StatsPlayerBuildingsDestroyed++;
                            }
                        }
                    }
                }
            }

            var deadUnits = Units.FindAll(u => u.Hp <= 0);
            foreach (var dead in deadUnits)
            {
                if (dead.Owner == Owner.Ai) StatsPlayerUnitsKilled++;
                else StatsPlayerUnitsLost++;
                TriggerMartyrdomBombExplosion(dead, outEvents);
            }
            Units.RemoveAll(u => u.Hp <= 0);
            return beams;
        }

        /// <summary>
        /// 2. 유닛 이동 목표 계산 (슬라이드 애니메이션을 위해 TargetCol 설정)
        /// </summary>
        public void PrepareUnitMarchTargets(Owner actingOwner)
        {
            Owner opponent = (actingOwner == Owner.Player) ? Owner.Ai : Owner.Player;
            int stepDir = (actingOwner == Owner.Player) ? 1 : -1;

            // 기저 상태 동기화
            foreach (var u in Units)
            {
                if (u.TargetCol < 0) u.TargetCol = u.Col;
            }

            var actingUnits = Units.FindAll(u => u.Owner == actingOwner);
            // 전선에 가장 가까운 유닛부터 전진 계산 (충돌 및 병목 방지)
            actingUnits.Sort((a, b) => (actingOwner == Owner.Player) ? b.Col.CompareTo(a.Col) : a.Col.CompareTo(b.Col));

            foreach (var unit in actingUnits)
            {
                if (unit.Hp <= 0 || unit.HasActed) continue;

                int targetCol = unit.Col;

                // 가속 관문 (PhaseGate) 및 가속 하이웨이 레일 (Highway): 유닛 이동력 +1
                int maxSteps = unit.Speed;
                bool gateBoosted = false;
                bool highwayBoosted = false;

                if (MapTiles[unit.Row, unit.Col] == TileType.Highway)
                {
                    highwayBoosted = true;
                    unit.HighwayBoosted = true;
                    maxSteps++;
                }

                var currentBldg = Buildings[unit.Row, unit.Col];
                if (currentBldg != null && currentBldg.Owner == actingOwner && currentBldg.Type == BuildingType.PhaseGate && currentBldg.Hp > 0 && currentBldg.EmpTurns <= 0)
                {
                    gateBoosted = true;
                    maxSteps++;
                }

                for (int step = 0; step < maxSteps; step++)
                {
                    // [시즈 포격 규칙]: 
                    // 콜로서스는 '시즈 앵커(Siege Anchor)' 카드가 해금되었을 때만 제자리 포격(Hold & Fire) 발동!
                    // (카드가 없으면 일반 유닛처럼 전진하다가 충돌 시 정지)
                    bool canHoldAndFire = (unit.Type == UnitType.Colossus) && ((actingOwner == Owner.Player) ? PlayerHasSiegeAnchor : AiHasSiegeAnchor);
                    if (canHoldAndFire)
                    {
                        bool hasTargetInRange = false;
                        for (int dist = unit.MinRange; dist <= unit.Range; dist++)
                        {
                            int checkCol = targetCol + dist * stepDir;
                            if (checkCol <= 0 || checkCol >= Cols - 1) break;

                            // 은폐 상태이거나 스모크 타일에 있는 유닛은 dist > 1일 때 시야에 잡히지 않음
                            if (Units.Exists(u => u.Hp > 0 && u.Col == checkCol && u.Row == unit.Row && u.Owner == opponent && ((!u.IsCloaked && MapTiles[u.Row, u.Col] != TileType.Smoke) || dist == 1)))
                            {
                                hasTargetInRange = true;
                                break;
                            }
                            if (Buildings[unit.Row, checkCol] != null && Buildings[unit.Row, checkCol].Owner == opponent && Buildings[unit.Row, checkCol].Hp > 0)
                            {
                                hasTargetInRange = true;
                                break;
                            }
                            if (MapTiles[unit.Row, checkCol] == TileType.Obstacle)
                            {
                                hasTargetInRange = true;
                                break;
                            }
                        }

                        if (hasTargetInRange)
                            break;
                    }

                    int nextCol = targetCol + stepDir;

                    // 1. 상대 본진 골라인 도달 (Core 진입)
                    if ((actingOwner == Owner.Player && nextCol >= Cols - 1) ||
                        (actingOwner == Owner.Ai && nextCol <= 0))
                    {
                        targetCol = nextCol;
                        break;
                    }

                    // 2. 장애물 랙에 막힘
                    if (MapTiles[unit.Row, nextCol] == TileType.Obstacle)
                        break;

                    // 3. 적 유닛 마주침: 적 칸으로 진입하지 않고 마주보는 바로 앞 칸에서 정지하여 저지선 형성!
                    if (Units.Exists(u => u.Hp > 0 && u.Col == nextCol && u.Row == unit.Row && u.Owner == opponent))
                        break;

                    // 4. 적 건물 마주침: 적 건물 칸으로 진입하지 않고 바로 앞 칸에서 정지하여 공성 대치!
                    if (Buildings[unit.Row, nextCol] != null && Buildings[unit.Row, nextCol].Owner == opponent && Buildings[unit.Row, nextCol].Hp > 0)
                        break;

                    // 5. 아군 방화벽 격벽(Wall) 마주침: 격벽은 통과 불가 방어벽이므로 멈춤
                    var friendlyBuilding = Buildings[unit.Row, nextCol];
                    if (friendlyBuilding != null && friendlyBuilding.Owner == actingOwner && friendlyBuilding.Type == BuildingType.Wall)
                        break;
                    // (규칙 반영: 아군 발전소, 포탑, 전진 공장은 아군 유닛이 자유롭게 통과하여 계속 전진 가능!)

                    // 6. 아군 유닛 충돌 방지: 이미 앞서 이동 결정을 마친 아군 유닛이 해당 칸을 차지했거나 정지해 있는 경우
                    bool friendlyUnitOccupied = Units.Exists(u => u.Hp > 0 && u != unit && u.Owner == actingOwner && u.Row == unit.Row &&
                        (Math.Abs(u.TargetCol - nextCol) < 0.1f || (u.HasActed && u.Col == nextCol)));
                    if (friendlyUnitOccupied)
                        break;

                    targetCol = nextCol;

                    // 이동 중 가속 관문 통과 시 추가 이동력 +1 (턴당 최대 1회 부스트)
                    if (!gateBoosted)
                    {
                        var steppedBldg = Buildings[unit.Row, nextCol];
                        if (steppedBldg != null && steppedBldg.Owner == actingOwner && steppedBldg.Type == BuildingType.PhaseGate && steppedBldg.Hp > 0 && steppedBldg.EmpTurns <= 0)
                        {
                            gateBoosted = true;
                            maxSteps++;
                        }
                    }

                    // 이동 중 가속 하이웨이 레일 진입 시 추가 이동력 +1 (턴당 최대 1회 부스트)
                    if (!highwayBoosted && nextCol > 0 && nextCol < Cols - 1 && MapTiles[unit.Row, nextCol] == TileType.Highway)
                    {
                        highwayBoosted = true;
                        unit.HighwayBoosted = true;
                        maxSteps++;
                    }
                }

                unit.TargetCol = targetCol;
            }
        }

        /// <summary>
        /// 3. 유닛 이동 완료 후 교전 판정 및 본진 타격 해결
        /// </summary>
        public List<CombatEvent> ResolveUnitCombatAndCoreStrikes(Owner actingOwner)
        {
            var events = new List<CombatEvent>();
            Owner opponent = (actingOwner == Owner.Player) ? Owner.Ai : Owner.Player;
            int stepDir = (actingOwner == Owner.Player) ? 1 : -1;

            var actingUnits = Units.FindAll(u => u.Owner == actingOwner);
            actingUnits.Sort((a, b) => (actingOwner == Owner.Player) ? b.Col.CompareTo(a.Col) : a.Col.CompareTo(b.Col));

            // 이동 애니메이션 완료 후 논리 좌표 동기화 (TargetCol이 설정된 경우에만 동기화)
            foreach (var u in actingUnits)
            {
                if (u.TargetCol >= 0)
                {
                    u.Col = (int)Math.Round(u.TargetCol);
                    u.VisualCol = u.Col;
                }
            }

            foreach (var unit in actingUnits)
            {
                if (unit.Hp <= 0) continue;
                if (unit.HasActed)
                {
                    unit.HasActed = false;
                    continue;
                }

                // [지형 기믹 효과 해결]
                if (unit.Col > 0 && unit.Col < Cols - 1)
                {
                    // 1) 가속 하이웨이 부스트 연출 피드백
                    if (unit.HighwayBoosted)
                    {
                        unit.HighwayBoosted = false;
                        events.Add(new CombatEvent
                        {
                            Col = unit.Col,
                            Row = unit.Row,
                            Text = "+1 하이웨이 ⏩",
                            TextColor = Color.FromArgb(0, 229, 255)
                        });
                    }

                    // 2) 화염 지대 (Fire): 지속 -1 데미지 피해
                    if (MapTiles[unit.Row, unit.Col] == TileType.Fire)
                    {
                        unit.Hp -= 1;
                        unit.HitFlashAlpha = 1.0f;
                        events.Add(new CombatEvent
                        {
                            Col = unit.Col,
                            Row = unit.Row,
                            Text = "-1 화염 🔥",
                            TextColor = Color.FromArgb(255, 87, 34),
                            IsCritical = true
                        });
                        if (unit.Hp <= 0)
                        {
                            TriggerMartyrdomBombExplosion(unit, events);
                            if (unit.Owner == Owner.Ai) StatsPlayerUnitsKilled++;
                            else StatsPlayerUnitsLost++;
                            continue; // 화염 피해로 사망
                        }
                    }
                    // 3) 나노 수복 샘 (NanoWell): 매 턴 +1 HP 회복
                    else if (MapTiles[unit.Row, unit.Col] == TileType.NanoWell && unit.Hp < unit.MaxHp)
                    {
                        unit.Hp = Math.Min(unit.MaxHp, unit.Hp + 1);
                        unit.HitFlashAlpha = 0.8f;
                        events.Add(new CombatEvent
                        {
                            Col = unit.Col,
                            Row = unit.Row,
                            Text = "+1 나노수복 💚",
                            TextColor = Color.FromArgb(76, 175, 80)
                        });
                    }
                }

                // 1. 상대 본진 골라인 도달 시 코어 폭발 타격
                if ((actingOwner == Owner.Player && unit.Col >= Cols - 1) ||
                    (actingOwner == Owner.Ai && unit.Col <= 0))
                {
                    if (unit.IsCloaked) unit.IsCloaked = false;
                    int dmg = unit.Atk * 2;
                    if (actingOwner == Owner.Player)
                    {
                        AiCoreHp = Math.Max(0, AiCoreHp - dmg);
                        StatsDamageDealtToAiCore += dmg;
                        events.Add(new CombatEvent { Col = Cols - 1, Row = unit.Row, Text = $"CORE HIT -{dmg}", TextColor = Color.FromArgb(0, 229, 255), IsCritical = true });
                    }
                    else
                    {
                        PlayerCoreHp = Math.Max(0, PlayerCoreHp - dmg);
                        events.Add(new CombatEvent { Col = 0, Row = unit.Row, Text = $"CORE HIT -{dmg}", TextColor = Color.FromArgb(255, 23, 68), IsCritical = true });
                    }
                    unit.Hp = 0;
                    continue;
                }

                // 2. 전방 사거리 내(Range) 적 유닛 또는 적 건물 탐색 및 공격 (MinRange 반영: 근접 불가 유닛 지원)
                for (int dist = unit.MinRange; dist <= unit.Range; dist++)
                {
                    int targetCol = unit.Col + dist * stepDir;
                    if (targetCol <= 0 || targetCol >= Cols - 1) break;

                    // (A) 적 유닛과의 교전
                    var enemyUnit = Units.Find(u => u.Col == targetCol && u.Row == unit.Row && u.Owner == opponent && u.Hp > 0);
                    // [은폐 및 연막 회피]: 은폐 상태이거나 스모크 필드 타일에 있는 적 유닛은 dist > 1인 원거리에서는 타깃팅되지 않음!
                    bool enemyInSmoke = (enemyUnit != null && MapTiles[enemyUnit.Row, enemyUnit.Col] == TileType.Smoke);
                    if (enemyUnit != null && (enemyUnit.IsCloaked || enemyInSmoke) && dist > 1)
                    {
                        enemyUnit = null;
                    }

                    if (enemyUnit != null)
                    {
                        // 공격자 침투병은 첫 공격 개시 시 은폐 해제!
                        if (unit.IsCloaked)
                        {
                            unit.IsCloaked = false;
                            events.Add(new CombatEvent { Col = unit.Col, Row = unit.Row, Text = "은폐 해제! ⚡", TextColor = Color.FromArgb(0, 229, 255) });
                        }

                        unit.BumpOffset = stepDir * 6f;
                        enemyUnit.HitFlashAlpha = 1.0f;
                        enemyUnit.ShakeOffset = stepDir * 4f;

                        // 선공 타격 (드레드노트 취약성 체크: 보병/침투병에게 맞을 시 +2뎀)
                        int atkDmg = unit.Atk;
                        bool isVuln = (enemyUnit.Type == UnitType.Dreadnought && (unit.Type == UnitType.Vanguard || unit.Type == UnitType.Raider));
                        if (isVuln) atkDmg += 2;

                        // [보병 전술 방패]: 첫 1회 피해를 완전히 무효화!
                        if (enemyUnit.HasShield)
                        {
                            enemyUnit.HasShield = false;
                            events.Add(new CombatEvent { Col = targetCol, Row = unit.Row, Text = "실드 방어! 🛡️", TextColor = Color.FromArgb(64, 196, 255) });
                        }
                        else
                        {
                            enemyUnit.Hp -= atkDmg;
                            string hitText = isVuln ? $"-{atkDmg} 취약!" : $"-{atkDmg}";
                            events.Add(new CombatEvent { Col = targetCol, Row = unit.Row, Text = hitText, TextColor = isVuln ? Color.FromArgb(255, 60, 60) : Color.FromArgb(255, 193, 7) });
                        }

                        // 타이탄 특수능력: [운동에너지 충격파] - 1칸 넉백 및 연쇄 충돌 피해
                        bool hasKineticSlam = (actingOwner == Owner.Player) ? PlayerHasKineticSlam : AiHasKineticSlam;
                        if (unit.Type == UnitType.Titan && hasKineticSlam && enemyUnit.Hp > 0)
                        {
                            int behindCol = enemyUnit.Col + stepDir;
                            bool isBlocked = false;

                            if (behindCol <= 0 || behindCol >= Cols - 1)
                            {
                                isBlocked = true;
                                if (behindCol <= 0) PlayerCoreHp = Math.Max(0, PlayerCoreHp - 1);
                                else
                                {
                                    AiCoreHp = Math.Max(0, AiCoreHp - 1);
                                    StatsDamageDealtToAiCore += 1;
                                }
                            }
                            else if (MapTiles[unit.Row, behindCol] == TileType.Obstacle)
                            {
                                isBlocked = true;
                                ObstacleHp[unit.Row, behindCol] -= 1;
                                if (ObstacleHp[unit.Row, behindCol] <= 0)
                                {
                                    MapTiles[unit.Row, behindCol] = TileType.Plain;
                                    StatsObstaclesDestroyed++;
                                    if (actingOwner == Owner.Player) { PlayerCredit += 2; StatsTotalCreditEarned += 2; }
                                    else AiCredit += 2;
                                    string rewardText = (actingOwner == Owner.Player) ? "아군 랙 돌파! +2G ⚡" : "적 랙 돌파! (적 +2G) ⚡";
                                    Color rewardCol = (actingOwner == Owner.Player) ? Color.FromArgb(255, 215, 0) : Color.FromArgb(255, 110, 64);
                                    events.Add(new CombatEvent { Col = behindCol, Row = unit.Row, Text = rewardText, TextColor = rewardCol, IsCritical = true });
                                }
                            }
                            else if (Buildings[unit.Row, behindCol] != null && Buildings[unit.Row, behindCol].Hp > 0)
                            {
                                isBlocked = true;
                                Buildings[unit.Row, behindCol].Hp -= 1;
                                if (Buildings[unit.Row, behindCol].Hp <= 0)
                                {
                                    if (Buildings[unit.Row, behindCol].Owner == Owner.Ai) StatsPlayerBuildingsDestroyed++;
                                    Buildings[unit.Row, behindCol] = null;
                                }
                            }
                            else
                            {
                                var behindUnit = Units.Find(u => u.Col == behindCol && u.Row == unit.Row && u.Hp > 0);
                                if (behindUnit != null)
                                {
                                    isBlocked = true;
                                    behindUnit.Hp -= 1;
                                    behindUnit.HitFlashAlpha = 0.8f;
                                    events.Add(new CombatEvent { Col = behindCol, Row = unit.Row, Text = "연쇄충돌! -1 💥", TextColor = Color.FromArgb(255, 87, 34) });
                                }
                            }

                            if (isBlocked)
                            {
                                enemyUnit.Hp -= 1;
                                enemyUnit.HitFlashAlpha = 1.0f;
                                events.Add(new CombatEvent { Col = enemyUnit.Col, Row = unit.Row, Text = "충돌! -1 💥", TextColor = Color.FromArgb(255, 60, 60) });
                            }
                            else
                            {
                                enemyUnit.Col = behindCol;
                                enemyUnit.VisualCol = behindCol;
                                enemyUnit.TargetCol = behindCol;
                                events.Add(new CombatEvent { Col = behindCol, Row = unit.Row, Text = "넉백! 💥", TextColor = Color.FromArgb(255, 152, 0) });
                            }
                        }

                        // 드레드노트 특수능력: [연쇄 플라즈마 방전] 카드 해금 시에만 십자 스플래시 발동
                        bool hasChainPlasma = (actingOwner == Owner.Player) ? PlayerHasChainPlasma : AiHasChainPlasma;
                        if (unit.Type == UnitType.Dreadnought && hasChainPlasma)
                        {
                            ApplyCrossSplash(targetCol, unit.Row, opponent, actingOwner, events);
                        }

                        // 근접 맞대결(dist == 1)인 경우 방어 유닛도 반격 (단, 방어 유닛이 근접 불가 유닛인 경우 반격 불가!)
                        if (dist == 1 && enemyUnit.Hp > 0 && enemyUnit.MinRange <= 1)
                        {
                            int retDmg = enemyUnit.Atk;
                            bool isUnitVuln = (unit.Type == UnitType.Dreadnought && (enemyUnit.Type == UnitType.Vanguard || enemyUnit.Type == UnitType.Raider));
                            if (isUnitVuln) retDmg += 2;

                            if (unit.HasShield)
                            {
                                unit.HasShield = false;
                                events.Add(new CombatEvent { Col = unit.Col, Row = unit.Row, Text = "실드 방어! 🛡️", TextColor = Color.FromArgb(64, 196, 255) });
                            }
                            else
                            {
                                unit.Hp -= retDmg;
                                unit.HitFlashAlpha = 0.8f;
                                unit.ShakeOffset = -stepDir * 3f;
                                string retText = isUnitVuln ? $"-{retDmg} 취약!" : $"-{retDmg}";
                                events.Add(new CombatEvent { Col = unit.Col, Row = unit.Row, Text = retText, TextColor = isUnitVuln ? Color.FromArgb(255, 60, 60) : Color.FromArgb(255, 87, 34) });
                            }
                        }

                        if (enemyUnit.Hp <= 0)
                        {
                            if (enemyUnit.Owner == Owner.Ai) StatsPlayerUnitsKilled++;
                            else StatsPlayerUnitsLost++;
                            TriggerMartyrdomBombExplosion(enemyUnit, events);
                            Units.Remove(enemyUnit);
                        }
                        break;
                    }

                    // (B) 적 건물과의 공성 공격
                    var enemyBuilding = Buildings[unit.Row, targetCol];
                    if (enemyBuilding != null && enemyBuilding.Owner == opponent && enemyBuilding.Hp > 0)
                    {
                        if (unit.IsCloaked) unit.IsCloaked = false;
                        unit.BumpOffset = stepDir * 6f;
                        enemyBuilding.HitFlashAlpha = 1.0f;
                        enemyBuilding.ShakeOffset = stepDir * 4f;

                        // 천궁(Cheongung) 특성: 건물 공격 시 2배 피해(시즈버스터)!
                        int bldgDmg = (unit.Type == UnitType.Cheongung) ? unit.Atk * 2 : unit.Atk;
                        enemyBuilding.Hp -= bldgDmg;
                        events.Add(new CombatEvent
                        {
                            Col = targetCol,
                            Row = unit.Row,
                            Text = (unit.Type == UnitType.Cheongung) ? $"-{bldgDmg} 시즈버스터! 💥" : $"-{unit.Atk} 🧱",
                            TextColor = (unit.Type == UnitType.Cheongung) ? Color.FromArgb(255, 60, 60) : Color.FromArgb(255, 152, 0),
                            IsCritical = (unit.Type == UnitType.Cheongung)
                        });

                        // 드레드노트 십자 스플래시 (해금 시)
                        bool hasChainPlasma = (actingOwner == Owner.Player) ? PlayerHasChainPlasma : AiHasChainPlasma;
                        if (unit.Type == UnitType.Dreadnought && hasChainPlasma)
                        {
                            ApplyCrossSplash(targetCol, unit.Row, opponent, actingOwner, events);
                        }

                        if (enemyBuilding.Hp <= 0)
                        {
                            if (enemyBuilding.Owner == Owner.Ai) StatsPlayerBuildingsDestroyed++;
                            Buildings[unit.Row, targetCol] = null;
                        }
                        break;
                    }

                    // (C) 중립 장애물 랙 파괴 공격!
                    if (MapTiles[unit.Row, targetCol] == TileType.Obstacle)
                    {
                        if (unit.IsCloaked) unit.IsCloaked = false;
                        unit.BumpOffset = stepDir * 6f;

                        // 천궁 특성: 장애물 랙에도 2배 피해(시즈버스터)!
                        int obsDmg = (unit.Type == UnitType.Cheongung) ? unit.Atk * 2 : unit.Atk;
                        ObstacleHp[unit.Row, targetCol] -= obsDmg;
                        events.Add(new CombatEvent
                        {
                            Col = targetCol,
                            Row = unit.Row,
                            Text = (unit.Type == UnitType.Cheongung) ? $"-{obsDmg} 시즈버스터! 💥" : $"-{unit.Atk} 랙 타격",
                            TextColor = (unit.Type == UnitType.Cheongung) ? Color.FromArgb(255, 87, 34) : Color.FromArgb(200, 210, 225),
                            IsCritical = (unit.Type == UnitType.Cheongung)
                        });

                        // 드레드노트 십자 스플래시 (해금 시)
                        bool hasChainPlasma = (actingOwner == Owner.Player) ? PlayerHasChainPlasma : AiHasChainPlasma;
                        if (unit.Type == UnitType.Dreadnought && hasChainPlasma)
                        {
                            ApplyCrossSplash(targetCol, unit.Row, opponent, actingOwner, events);
                        }

                        if (ObstacleHp[unit.Row, targetCol] <= 0)
                        {
                            MapTiles[unit.Row, targetCol] = TileType.Plain;
                            StatsObstaclesDestroyed++;
                            if (actingOwner == Owner.Player)
                            {
                                PlayerCredit += 2;
                                StatsTotalCreditEarned += 2;
                            }
                            else AiCredit += 2;

                            string rewardText = (actingOwner == Owner.Player) ? "아군 랙 돌파! +2G ⚡" : "적 랙 돌파! (적 +2G) ⚡";
                            Color rewardCol = (actingOwner == Owner.Player) ? Color.FromArgb(255, 215, 0) : Color.FromArgb(255, 110, 64);
                            events.Add(new CombatEvent
                            {
                                Col = targetCol,
                                Row = unit.Row,
                                Text = rewardText,
                                TextColor = rewardCol,
                                IsCritical = true
                            });
                        }
                        break;
                    }
                }
            }

            var combatDeadUnits = Units.FindAll(u => u.Hp <= 0);
            foreach (var dead in combatDeadUnits)
            {
                if (dead.Owner == Owner.Ai) StatsPlayerUnitsKilled++;
                else StatsPlayerUnitsLost++;
                TriggerMartyrdomBombExplosion(dead, events);
            }
            Units.RemoveAll(u => u.Hp <= 0);

            // 승리 체크
            if (AiCoreHp <= 0)
            {
                IsGameOver = true;
                Winner = Owner.Player;
                StatusMessage = "🎉 승리! 적 메인프레임 코어를 완파했습니다!";
                RecordReplaySnapshot();
                EvaluateCoreFrontAchievements(true);
            }
            else if (PlayerCoreHp <= 0)
            {
                IsGameOver = true;
                Winner = Owner.Ai;
                StatusMessage = "💀 패배! 아군 코어가 파괴되었습니다.";
                RecordReplaySnapshot();
            }

            return events;
        }

        /// <summary>
        /// 최후의 자폭 코어 기폭: 사망 시 상하좌우 4방향 적 기물/코어에 2 데미지 (아군 피해 없음)
        /// </summary>
        private void TriggerMartyrdomBombExplosion(Unit dead, List<CombatEvent> events)
        {
            if (!dead.HasBomb) return;
            dead.HasBomb = false; // 1회 기폭 보장

            Owner opponent = (dead.Owner == Owner.Player) ? Owner.Ai : Owner.Player;
            events.Add(new CombatEvent
            {
                Col = dead.Col,
                Row = dead.Row,
                Text = "💣 자폭 코어 기폭!",
                TextColor = Color.FromArgb(255, 23, 68),
                IsCritical = true
            });

            int[] dR = { -1, 1, 0, 0 };
            int[] dC = { 0, 0, -1, 1 };

            for (int i = 0; i < 4; i++)
            {
                int nr = dead.Row + dR[i];
                int nc = dead.Col + dC[i];

                // 코어 도달 체크
                if (dead.Owner == Owner.Player && nc >= Cols - 1 && nr >= 0 && nr < Rows)
                {
                    AiCoreHp = Math.Max(0, AiCoreHp - 2);
                    StatsDamageDealtToAiCore += 2;
                    events.Add(new CombatEvent { Col = Cols - 1, Row = nr, Text = "💣 적 코어 -2", TextColor = Color.FromArgb(255, 23, 68), IsCritical = true });
                    continue;
                }
                else if (dead.Owner == Owner.Ai && nc <= 0 && nr >= 0 && nr < Rows)
                {
                    PlayerCoreHp = Math.Max(0, PlayerCoreHp - 2);
                    events.Add(new CombatEvent { Col = 0, Row = nr, Text = "💣 아군 코어 -2", TextColor = Color.FromArgb(255, 23, 68), IsCritical = true });
                    continue;
                }

                if (nr < 0 || nr >= Rows || nc <= 0 || nc >= Cols - 1) continue;

                // 1. 적 유닛만 타격
                var enemyUnit = Units.Find(u => u.Col == nc && u.Row == nr && u.Owner == opponent && u.Hp > 0);
                if (enemyUnit != null)
                {
                    if (enemyUnit.HasShield)
                    {
                        enemyUnit.HasShield = false;
                        events.Add(new CombatEvent { Col = nc, Row = nr, Text = "실드 방어! 🛡️", TextColor = Color.FromArgb(64, 196, 255) });
                    }
                    else
                    {
                        enemyUnit.Hp -= 2;
                        enemyUnit.HitFlashAlpha = 1.0f;
                        events.Add(new CombatEvent { Col = nc, Row = nr, Text = "-2 자폭! 💣", TextColor = Color.FromArgb(255, 23, 68) });
                        if (enemyUnit.Hp <= 0 && enemyUnit.HasBomb)
                        {
                            TriggerMartyrdomBombExplosion(enemyUnit, events);
                        }
                    }
                }

                // 2. 적 건물만 타격
                var enemyBldg = Buildings[nr, nc];
                if (enemyBldg != null && enemyBldg.Owner == opponent && enemyBldg.Hp > 0)
                {
                    enemyBldg.Hp -= 2;
                    enemyBldg.HitFlashAlpha = 1.0f;
                    events.Add(new CombatEvent { Col = nc, Row = nr, Text = "-2 자폭! 💣", TextColor = Color.FromArgb(255, 23, 68) });
                    if (enemyBldg.Hp <= 0)
                    {
                        if (enemyBldg.Owner == Owner.Ai) StatsPlayerBuildingsDestroyed++;
                        Buildings[nr, nc] = null;
                    }
                }

                // 3. 서버 랙 장애물 타격
                if (MapTiles[nr, nc] == TileType.Obstacle && ObstacleHp[nr, nc] > 0)
                {
                    ObstacleHp[nr, nc] -= 2;
                    events.Add(new CombatEvent { Col = nc, Row = nr, Text = "-2 랙 폭발! 💣", TextColor = Color.FromArgb(255, 110, 64) });
                    if (ObstacleHp[nr, nc] <= 0)
                    {
                        MapTiles[nr, nc] = TileType.Plain;
                        StatsObstaclesDestroyed++;
                        if (dead.Owner == Owner.Player)
                        {
                            PlayerCredit += 2;
                            StatsTotalCreditEarned += 2;
                        }
                        else AiCredit += 2;
                        string rewardText = (dead.Owner == Owner.Player) ? "아군 랙 돌파! +2G ⚡" : "적 랙 돌파! (적 +2G) ⚡";
                        Color rewardCol = (dead.Owner == Owner.Player) ? Color.FromArgb(255, 215, 0) : Color.FromArgb(255, 110, 64);
                        events.Add(new CombatEvent { Col = nc, Row = nr, Text = rewardText, TextColor = rewardCol, IsCritical = true });
                    }
                }
            }
        }

        /// <summary>
        /// 플라즈마 드레드노트 특수 능력: 목표 지점 기준 상/하/좌/우 십자 4방향 스플래시 방전 데미지 (2뎀)
        /// </summary>
        private void ApplyCrossSplash(int centerCol, int centerRow, Owner opponent, Owner actingOwner, List<CombatEvent> events)
        {
            int splashDmg = 2;
            int[] dRow = { -1, 1, 0, 0 };
            int[] dCol = { 0, 0, -1, 1 };

            for (int i = 0; i < 4; i++)
            {
                int sc = centerCol + dCol[i];
                int sr = centerRow + dRow[i];
                if (sc <= 0 || sc >= Cols - 1 || sr < 0 || sr >= Rows) continue;

                // 1. 십자 범위 내 적 유닛
                var splashUnit = Units.Find(u => u.Col == sc && u.Row == sr && u.Owner == opponent && u.Hp > 0);
                if (splashUnit != null)
                {
                    splashUnit.Hp -= splashDmg;
                    splashUnit.HitFlashAlpha = 0.8f;
                    events.Add(new CombatEvent { Col = sc, Row = sr, Text = $"-{splashDmg} ⚡", TextColor = Color.FromArgb(0, 229, 255) });
                }

                // 2. 십자 범위 내 적 건물
                var splashBldg = Buildings[sr, sc];
                if (splashBldg != null && splashBldg.Owner == opponent && splashBldg.Hp > 0)
                {
                    splashBldg.Hp -= splashDmg;
                    splashBldg.HitFlashAlpha = 0.8f;
                    events.Add(new CombatEvent { Col = sc, Row = sr, Text = $"-{splashDmg} ⚡", TextColor = Color.FromArgb(0, 229, 255) });
                    if (splashBldg.Hp <= 0)
                    {
                        Buildings[sr, sc] = null;
                    }
                }

                // 3. 십자 범위 내 중립 장애물 랙
                if (MapTiles[sr, sc] == TileType.Obstacle)
                {
                    ObstacleHp[sr, sc] -= splashDmg;
                    events.Add(new CombatEvent { Col = sc, Row = sr, Text = $"-{splashDmg} ⚡", TextColor = Color.FromArgb(180, 200, 220) });
                    if (ObstacleHp[sr, sc] <= 0)
                    {
                        MapTiles[sr, sc] = TileType.Plain;
                        if (actingOwner == Owner.Player) PlayerCredit += 2;
                        else AiCredit += 2;

                        string rewardText = (actingOwner == Owner.Player) ? "아군 랙 돌파! +2G ⚡" : "적 랙 돌파! (적 +2G) ⚡";
                        Color rewardCol = (actingOwner == Owner.Player) ? Color.FromArgb(255, 215, 0) : Color.FromArgb(255, 110, 64);
                        events.Add(new CombatEvent
                        {
                            Col = sc,
                            Row = sr,
                            Text = rewardText,
                            TextColor = rewardCol,
                            IsCritical = true
                        });
                    }
                }
            }
        }

        #endregion

        #region Steam-Style Achievement Evaluation

        public List<string> EvaluateCoreFrontAchievements(bool isVictory)
        {
            var newlyUnlocked = new List<string>();

            void TryUnlock(string achId)
            {
                if (Dashboard.DashboardManager.UnlockCoreFrontAchievement(achId, out string title, out int pts))
                {
                    newlyUnlocked.Add(title);
                    AddCombatLog($"🏆 [도전과제 달성] {title} (+{pts} AP)");
                }
            }

            // 실시간 / 턴 종료 달성 가능한 과제
            if (StatsTotalCreditEarned >= 600)
            {
                TryUnlock("CF_GOLD_600");
            }
            if (HasSpawnedFromFactoryBridgehead)
            {
                TryUnlock("CF_FORWARD_BASE");
            }
            if (HasUsedOrbitalStrikeDestroy)
            {
                TryUnlock("CF_TACTICAL_STRIKE");
            }

            // 승리 시 달성 가능한 과제
            if (isVictory)
            {
                Dashboard.DashboardManager.RecordCoreFrontWin();

                // 1. 작전 개시 (첫 승)
                TryUnlock("CF_FIRST_WIN");

                // 2. 전선의 사령관 (누적 5승)
                if (Dashboard.DashboardManager.Stats.CoreFrontWins >= 5)
                {
                    TryUnlock("CF_VETERAN");
                }

                // 4. 보병의 돌격 (보병 최소 1기 직접 생산 + 비보병 생산 없음)
                if (HasSpawnedVanguard && !HasSpawnedNonVanguard)
                {
                    TryUnlock("CF_ONLY_INFANTRY");
                }

                // 5. 무정전 작전 (진행 중 추가 발전소 0채)
                if (PlayerBuiltPowerPlantsDuringGame == 0)
                {
                    TryUnlock("CF_NO_POWERPLANT");
                }

                // 6. 공세 일변도 (포탑 0채)
                if (PlayerBuiltTurrets == 0)
                {
                    TryUnlock("CF_NO_TURRET");
                }

                // 7. 순수 군략 (유닛 특수 카드 선택 없음)
                if (!HasSelectedUnitCard)
                {
                    TryUnlock("CF_NO_UNIT_CARDS");
                }

                // 9. 피말리는 지구전 (게임 시간 10분 초과)
                if ((DateTime.Now - MatchStartTime).TotalMinutes >= 10.0)
                {
                    TryUnlock("CF_TIME_10MIN");
                }

                // 11. 10열 총력전 (초대형 맵)
                if (CurrentMapSize == MapSizeType.Huge)
                {
                    TryUnlock("CF_HUGE_TOTAL_WAR");
                }

                // 12. 난공불락의 코어 (아군 코어 HP 25/25 완벽 방어)
                if (PlayerCoreHp >= BaseMaxHp)
                {
                    TryUnlock("CF_FLAWLESS_CORE");
                }

                // 13. 8턴 전격전 (8턴 이내 승리)
                if (TurnNumber <= 8)
                {
                    TryUnlock("CF_BLITZKRIEG");
                }
            }

            return newlyUnlocked;
        }

        #endregion

    }
}
