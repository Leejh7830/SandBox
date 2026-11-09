using System;
using System.Collections.Generic;
using System.Drawing;

namespace GateHelper.DataFlip
{
    public enum Owner
    {
        None = 0,
        Player = 1, // Cyan
        Ai = 2      // Crimson
    }

    public enum AiStrategyPersona
    {
        Rush = 0,   // 기동 침투형 (Raider & Vanguard 중심, 빈 라인 기습)
        Siege = 1,  // 원거리 요새형 (Turret & Wall 거점 확보 후 Colossus 포격)
        Tech = 2    // 하이테크형 (발전소 최대 가동 후 Titan & Dreadnought 진격)
    }

    public enum MapSizeType
    {
        Small = 0,    // 12 x 6 (소형: 좁은 전장, 빠른 난타전)
        Standard = 1, // 14 x 8 (표준: 정규 밸런스 전장)
        Large = 2,    // 18 x 8 (대형: 가로 확장, 장거리 천궁/포탑/전진인프라 전략전)
        Huge = 3      // 20 x 10 (초대형: 10개 라인 와이드 대전장, 총력 결전)
    }

    public enum TileType
    {
        Plain = 0,     // 일반 타일
        Resource = 1,  // 자원 타일 (발전소 건설 시 3배 수입: +3G)
        Obstacle = 2,  // 서버 랙 장애물 (통과 불가, 파괴 가능)
        Fire = 3,      // 화염 지대 (유닛 통과/턴 종료 시 -1 피해, 건물 불가)
        Highway = 4,   // 가속 레일 (유닛 이동 시 전방 +1칸 즉시 추가 가속)
        NanoWell = 5,  // 나노 수복 샘 (턴 종료 시 머무르는 유닛 +1 HP 자동 회복)
        Smoke = 6      // 스모크 필드 (타일 위 유닛 포탑 요격 및 원거리 공격 면역)
    }

    public enum BattlefieldTheme
    {
        Standard = 0,   // 표준 서버 룸 (8대 중립 랙 돌파전)
        Inferno = 1,    // 과열 화염 용광로 (화염 타일 -1 데미지 험지)
        Highway = 2,    // 초광속 가속 하이웨이 (가속 레일 초고속 기동전)
        NanoWell = 3,   // 나노 수복 샘 (치유 거점 쟁탈전)
        SmokeField = 4, // 스모크 필드 (은폐 연막 잠입 침투전)
        Random = -1     // 무작위 테마 선택
    }

    public enum BuildingType
    {
        None = 0,
        PowerPlant = 1, // 8G~, HP 4, 턴당 +1G (자원 타일 위: +3G)
        Turret = 2,     // 6G~, HP 6, 13칸 전방위 요격
        Wall = 3,       // 3G, HP 8, 이동 불가 바리케이드
        Factory = 4,    // 9G, HP 10, 전진 스폰 포인트 제공
        RepairDepot = 5,// 6G, HP 6, 턴마다 전장의 부상당한 아군 유닛 1기 체력 회복 +1
        PhaseGate = 6   // 5G, HP 6, 통과하는 아군 유닛 이동력 +1
    }

    public enum UnitType
    {
        None = 0,
        Vanguard = 1, // 2G, HP 3, 공 1, 이속 1칸 (기본 보병)
        Raider = 2,   // 4G, HP 3, 공 2, 이속 2칸 (고속 기동/기습)
        Titan = 3,    // 10G, HP 10, 공 3, 이속 1칸, 사거리 2칸 (중장갑 만능 공성 거신)
        Colossus = 4, // 14G, HP 8, 공 4, 이속 1칸, 사거리 2~3칸 (초장거리 공성 포격, 인접 1칸 근접 공격 불가)
        Dreadnought = 5, // 18G, HP 12, 공 3, 이속 1칸, 사거리 2칸 (십자 스플래시 방전, 보병/침투병 공격에 취약 +2뎀)
        Cheongung = 6 // 12G, HP 5, 공 3, 이속 1칸, 사거리 4칸 (초장거리 미사일 포대, 건물 공격 시 2배 6뎀, 인접 1칸 근접 불가, 시즈모드 없이 전진)
    }

    public enum UserSkillType
    {
        None = 0,
        OrbitalStrike = 1, // [1] 8G: 지정 적 건물/서버랙 6뎀 일격 타격
        EmpJammer = 2,     // [2] 5G: 지정 적 건물 및 3x3 범위 2턴간 기능 정지 (포탑 사격 정지, 발전소 수입 0)
        HealingBeam = 3,   // [3] 4G: 지정 행(Row) 아군 유닛+건물 체력 +6 회복 (최대체력 초과 불가)
        FortifyBeam = 4,   // [4] 5G: 지정 행(Row) 아군 유닛 최대 체력 +2 & 현재 체력 +2 (내구도 증가)
        ReconScan = 5,     // [5] 3G: 전장의 모든 은폐 침투병 은폐 해제 및 위치 노출
        NanoRepair = 6     // (보급 카드로 이전됨 - 호환성 유지)
    }

    public class Unit
    {
        public int Id { get; set; }
        public Owner Owner { get; set; }
        public UnitType Type { get; set; }
        public int Hp { get; set; }
        public int MaxHp { get; set; }
        public int Atk { get; set; }
        public int Speed { get; set; }
        public int Range { get; set; }
        public int MinRange { get; set; } = 1; // 최소 사거리 (1: 근접 가능, 2: 인접 1칸 공격 불가)
        public int Col { get; set; }
        public int Row { get; set; }
        public bool HasActed { get; set; }
        public bool HasShield { get; set; } = false; // 보병 전술 방패 (1회 피해 무효화 실드)
        public bool IsCloaked { get; set; } = false; // 침투병 광학 스텔스 (근접 공격 전까지 원거리/포탑 타깃팅 면역)
        public bool HasBomb { get; set; } = false;   // 최후의 자폭 코어 (사망 시 상하좌우 1칸 적 2 데미지)
        public bool HighwayBoosted { get; set; } = false; // 가속 하이웨이 레일 통과 가속 여부

        // 부드러운 슬로우 이동 보간 및 피격 애니메이션용
        public float VisualCol { get; set; } = -1;
        public float TargetCol { get; set; } = -1;
        public float HitFlashAlpha { get; set; } = 0f;
        public float BumpOffset { get; set; } = 0f;
        public float ShakeOffset { get; set; } = 0f;

        public Unit Clone()
        {
            return new Unit
            {
                Id = this.Id,
                Owner = this.Owner,
                Type = this.Type,
                Hp = this.Hp,
                MaxHp = this.MaxHp,
                Atk = this.Atk,
                Speed = this.Speed,
                Range = this.Range,
                MinRange = this.MinRange,
                HasShield = this.HasShield,
                IsCloaked = this.IsCloaked,
                HasBomb = this.HasBomb,
                HighwayBoosted = this.HighwayBoosted,
                Col = this.Col,
                Row = this.Row,
                HasActed = this.HasActed,
                VisualCol = this.VisualCol,
                TargetCol = this.TargetCol,
                HitFlashAlpha = this.HitFlashAlpha,
                BumpOffset = this.BumpOffset,
                ShakeOffset = this.ShakeOffset
            };
        }
    }

    public class Building
    {
        public Owner Owner { get; set; }
        public BuildingType Type { get; set; }
        public int Hp { get; set; }
        public int MaxHp { get; set; }
        public int Col { get; set; }
        public int Row { get; set; }
        public int EmpTurns { get; set; } = 0; // EMP 기능 정지 잔여 턴수 (>0이면 사격/발전 불가)

        // 피격 시 번쩍임 및 흔들림 연출
        public float HitFlashAlpha { get; set; } = 0f;
        public float ShakeOffset { get; set; } = 0f;

        public Building Clone()
        {
            return new Building
            {
                Owner = this.Owner,
                Type = this.Type,
                Hp = this.Hp,
                MaxHp = this.MaxHp,
                Col = this.Col,
                Row = this.Row,
                EmpTurns = this.EmpTurns,
                HitFlashAlpha = this.HitFlashAlpha,
                ShakeOffset = this.ShakeOffset
            };
        }
    }

    public class CombatLaserBeam
    {
        public int StartCol { get; set; }
        public int StartRow { get; set; }
        public int EndCol { get; set; }
        public int EndRow { get; set; }
        public Color BeamColor { get; set; }
        public float Alpha { get; set; } = 1.0f;
    }

    public enum CardRarity
    {
        Common, // 일반 (출현율 50%)
        Rare,   // 희귀 (출현율 35%)
        Epic    // 에픽 (출현율 15%)
    }

    public class ProtocolCard
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string Description { get; set; }
        public Color AccentColor { get; set; }
        public CardRarity Rarity { get; set; } = CardRarity.Common;
        public int MinTurn { get; set; } = 1; // 카드 풀 등장 최소 턴 (1: 턴 3부터, 6: 턴 6부터, 9: 턴 9부터, 12: 턴 12부터)
        public Action<CoreFrontGameState, Owner> ApplyAction { get; set; }
        public Func<CoreFrontGameState, Owner, bool> IsAvailable { get; set; } // 출현 가능 조건 (한도 도달 및 이미 해금된 고유기술 제외)
    }

    public class CombatEvent
    {
        public int Col { get; set; }
        public int Row { get; set; }
        public string Text { get; set; }
        public Color TextColor { get; set; }
        public bool IsCritical { get; set; }
    }

    public class ReplayUnitData
    {
        public int Col { get; set; }
        public int Row { get; set; }
        public Owner Owner { get; set; }
        public UnitType Type { get; set; }
        public int Hp { get; set; }
        public int MaxHp { get; set; }
        public bool HasShield { get; set; }
        public bool IsCloaked { get; set; }
        public bool HasBomb { get; set; }
    }

    public class ReplayBuildingData
    {
        public int Col { get; set; }
        public int Row { get; set; }
        public Owner Owner { get; set; }
        public BuildingType Type { get; set; }
        public int Hp { get; set; }
        public int MaxHp { get; set; }
        public int EmpTurns { get; set; }
    }

    public class ReplayObstacleData
    {
        public int Col { get; set; }
        public int Row { get; set; }
        public int Hp { get; set; }
    }

    public class ReplayTurnSnapshot
    {
        public int TurnNumber { get; set; }
        public Owner CurrentTurn { get; set; }
        public int PlayerCoreHp { get; set; }
        public int AiCoreHp { get; set; }
        public int PlayerCredit { get; set; }
        public int AiCredit { get; set; }
        public string Note { get; set; } = "";
        public List<ReplayUnitData> Units { get; set; } = new List<ReplayUnitData>();
        public List<ReplayBuildingData> Buildings { get; set; } = new List<ReplayBuildingData>();
        public List<ReplayObstacleData> Obstacles { get; set; } = new List<ReplayObstacleData>();
    }
}
