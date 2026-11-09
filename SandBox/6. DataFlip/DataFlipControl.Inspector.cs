using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GateHelper.DataFlip
{
    public partial class DataFlipControl
    {
        private void DrawInspectorCard(Graphics g)
        {
            if (_game.IsCardSelectionActive || _showGuide || _game.IsGameOver || _isTurnRunning) return;

            string title = "";
            string teamTag = "";
            Color accentColor = _colorCyan;
            string stat1 = "";
            string stat2 = "";
            string desc1 = "";
            string desc2 = "";

            int cardW = 245;
            int cardH = 98;
            int cardX = 0;
            int cardY = 0;

            // 1. 하단 슬롯 액션 버튼에 마우스 호버 시 실시간 스펙 가이드 팝업
            if ((_hoverButtonId >= 1 && _hoverButtonId <= 6) || _hoverButtonId == 95)
            {
                Rectangle slotRect = (_hoverButtonId == 95) ? _btnExpandGridRect : _slotRects[_hoverButtonId - 1];
                cardX = slotRect.X + (slotRect.Width - cardW) / 2;
                cardX = Math.Max(12, Math.Min(this.Width - cardW - 12, cardX));
                cardY = (_hoverButtonId == 95) ? _slotRects[0].Y - cardH - 8 : slotRect.Y - cardH - 8;

                if (_hoverButtonId == 95)
                {
                    title = "⚡ 전력망 한도 증설";
                    int eCost = _game.GetGridExpandCost(Owner.Player);
                    accentColor = _colorGold;
                    teamTag = $"{eCost}G 연구";
                    stat1 = $"비용 {eCost}G  |  현재 한도 {_game.PlayerPlantLimit}기";
                    stat2 = "발전소 보유 최대 한도를 즉시 +1기 확장";
                    desc1 = "발전소 3기 초과 건설을 위한 전력망 인프라 투자";
                    desc2 = "증설할 때마다 다음 비용이 +4G 점진 증가";
                }
                else if (_currentCategory == BottomCategory.Units)
                {
                    int slotIndex = _hoverButtonId - 1;
                    switch (slotIndex)
                    {
                        case 0: // 보병
                            title = "● 보병 (Vanguard)";
                            accentColor = _colorCyan;
                            teamTag = "2G 유닛";
                            stat1 = $"HP {3 + _game.PlayerUnitHpBonus}  |  공격력 {1 + _game.PlayerUnitAtkBonus}  |  이속 1칸";
                            stat2 = _game.PlayerHasPhalanxShield ? "🛡️ [전술 방패 결진 활성화] 1회 무효 실드" : "사거리 1칸 근접  |  아군 건물 통과";
                            desc1 = "가성비 뛰어난 전선의 핵심 기본 보병";
                            desc2 = _game.PlayerHasPhalanxShield ? "특수: 소환 시 1회 피해를 완전 무효화하는 실드 장착!" : "적 유닛 조우 시 전선 저지선 형성 맞대치";
                            break;
                        case 1: // 침투병
                            title = "▲ 고속 침투병 (Raider)";
                            accentColor = Color.FromArgb(255, 170, 0);
                            teamTag = "4G 유닛";
                            stat1 = $"HP {3 + _game.PlayerUnitHpBonus}  |  공격력 {2 + _game.PlayerUnitAtkBonus}  |  이속 2칸!";
                            stat2 = _game.PlayerHasStealthCloak ? "⚡ [광학 스텔스 활성화] 첫 공격 시까지 은폐" : "사거리 1칸 근접  |  고속 기동 암살";
                            desc1 = "고속 이동으로 적 빈틈 침투 및 적 발전소 타격";
                            desc2 = _game.PlayerHasStealthCloak ? "특수: 적 바로 앞까지 도달해 공격할 때까지 원거리 공격 불가!" : "보병 상대 시 선공 피해를 입히고 반격을 받음";
                            break;
                        case 2: // 타이탄
                            title = "■ 타이탄 (Titan)";
                            accentColor = _colorCrimson;
                            teamTag = "10G 유닛";
                            stat1 = $"HP {10 + _game.PlayerUnitHpBonus}  |  공격력 {3 + _game.PlayerUnitAtkBonus}  |  이속 1칸";
                            stat2 = _game.PlayerHasKineticSlam ? "💥 [충격파 슬램 활성화] 1칸 넉백 & 충돌뎀" : "사거리 2칸 포격  |  반격 없는 공성";
                            desc1 = "중장갑 결전 병기 · 사거리 2칸 일방적 포격";
                            desc2 = _game.PlayerHasKineticSlam ? "특수: 적중 시 뒤로 1칸 밀쳐냄! 뒤가 막히면 양쪽 충돌뎀 1!" : "사거리 2에서 포격 시 적 근접 유닛 반격 없음";
                            break;
                        case 3: // 천궁
                            title = "🚀 천궁 미사일 포대 (Cheongung)";
                            accentColor = Color.FromArgb(255, 120, 50);
                            teamTag = "12G 유닛";
                            stat1 = $"HP {5 + _game.PlayerUnitHpBonus}  |  공격력 {3 + _game.PlayerUnitAtkBonus}  |  이속 1칸";
                            string cgRngStr = _game.PlayerHasCheongungTrajectory ? "사거리 3~5칸 [탄도 궤적 정밀 연산]" : "사거리 2~4칸 초장거리 포격 (1칸 근접불가)";
                            stat2 = cgRngStr;
                            desc1 = "공성 특화: 건물 및 서버 랙 타격 시 공격력 2배 (6 피해!)";
                            desc2 = "직진 행군: 시즈 모드 없이 멈추지 않고 앞으로 전진";
                            break;
                        case 4: // 콜로서스
                            title = "⬢ 공성 콜로서스 (Colossus)";
                            accentColor = Color.FromArgb(220, 100, 255);
                            teamTag = "14G 유닛";
                            stat1 = $"HP {8 + _game.PlayerUnitHpBonus}  |  공격력 {4 + _game.PlayerUnitAtkBonus}  |  이속 1칸";
                            string colRngStr = _game.PlayerHasColossusMelee ? "사거리 1~3칸 전거리 포격 [근접발칸]" : "사거리 2~3칸 초장거리 포격 (1칸 근접불가)";
                            stat2 = _game.PlayerHasSiegeAnchor ? "🔭 [시즈 닻 활성화] 사거리 내 제자리 포격!" : colRngStr;
                            desc1 = _game.PlayerHasSiegeAnchor ? "특수: 유효 사거리 내 적 포착 시 제자리 고정 시즈 포격!" : "특수: 일반 유닛처럼 전진하며 행군 (시즈 앵커 카드로 해금)";
                            desc2 = _game.PlayerHasColossusMelee ? "특수: 근접 방어 발칸포 탑재! 1칸 인접 근접 공격 및 반격 가능!" : "주의: 인접 1칸 근접 공격 불가 및 피격 시 반격 불가";
                            break;
                        case 5: // 드레드노트
                            title = "✦ 드레드노트 (Dreadnought)";
                            accentColor = Color.FromArgb(255, 190, 0);
                            teamTag = "18G 유닛";
                            stat1 = $"HP {12 + _game.PlayerUnitHpBonus}  |  공격력 {3 + _game.PlayerUnitAtkBonus}  |  사거리 2칸";
                            stat2 = _game.PlayerHasChainPlasma ? "✦ [연쇄 플라즈마 활성화] 십자 4방향 2뎀" : "사거리 2칸 원거리 워커  |  단일 타격";
                            desc1 = _game.PlayerHasChainPlasma ? "특수: 주 타격 시 상/하/좌/우 십자 4방향 2뎀 스플래시 방전!" : "최종 결전 워커 · (연쇄 플라즈마 카드로 스플래시 해금)";
                            desc2 = "핸디캡: 보병/침투병(소형)에게 맞을 시 +2 추가 피해!";
                            break;
                    }
                }
                else if (_currentCategory == BottomCategory.Buildings)
                {
                    int slotIndex = _hoverButtonId - 1;
                    switch (slotIndex)
                    {
                        case 0: // 발전소
                            title = "⚡ 발전소 (PowerPlant)";
                            accentColor = _colorGold;
                            int plantCost = _game.GetBuildingCost(BuildingType.PowerPlant, Owner.Player);
                            teamTag = $"{plantCost}G 건물";
                            int curP = _game.GetBuildingCount(BuildingType.PowerPlant, Owner.Player);
                            int limP = _game.PlayerPlantLimit;
                            stat1 = $"HP 4  |  비용 {plantCost}G (기본 6G, 보유당 +3G)";
                            stat2 = $"현재 가동 {curP}/{limP}기" + (curP >= limP ? " [한도 도달]" : "");
                            desc1 = "턴당 +1G 생산 (자원 노드 위: +3G 3배 생산!)";
                            desc2 = "한도 초과 시 [5] 한도증설로 그리드 확장 필요 (2턴당 +1HP 자체수리)";
                            break;
                        case 1: // 포탑
                            title = "🛡️ 방화벽 포탑 (Turret)";
                            int tCost = _game.GetBuildingCost(BuildingType.Turret, Owner.Player);
                            accentColor = _colorEmerald;
                            teamTag = $"{tCost}G 건물";
                            stat1 = $"HP 6  |  요격력 2  |  비용 {tCost}G (지을수록 +2G)";
                            stat2 = _game.PlayerHasTurretRadar ? "📡 [양자 레이더 가동] 은폐 침투병 요격 가능!" : "13칸 전방위 요격 (1칸 인접 사각지대 존재)";
                            desc1 = "13칸 반경 내 가장 위험한 적 1기 매 턴 자동 요격";
                            desc2 = "주의: 인접 1칸(상하좌우) 사각지대 존재! 천궁이나 원거리 유닛으로 견제";
                            break;
                        case 2: // 격벽
                            title = "🧱 방화벽 격벽 (Wall)";
                            accentColor = Color.FromArgb(160, 180, 200);
                            teamTag = "3G 건물";
                            stat1 = "HP 8  |  공격력 0  |  바리케이드";
                            stat2 = "길목 차단  |  적과 아군 모두 이동 불가";
                            desc1 = "적 유닛의 진격을 물리적으로 막는 방어벽";
                            desc2 = "주의: 아군 유닛의 진격도 차단합니다!";
                            break;
                        case 3: // 공장
                            title = "🏭 전진 공장 (Factory)";
                            accentColor = _colorPurple;
                            teamTag = "8G 건물";
                            stat1 = "HP 10  |  비용 8G  |  전진 스폰 포인트 제공";
                            stat2 = "공장 바로 앞 1칸에서 유닛 즉시 스폰 가능";
                            desc1 = "본진이 아닌 전선 바로 앞에서 병력 전진 배치";
                            desc2 = "아군 유닛 자유 통과 · 이동 턴 절약";
                            break;
                        case 4: // 수리 격납고
                            title = "🏥 수리 격납고 (Repair Depot)";
                            int rdCost = _game.GetBuildingCost(BuildingType.RepairDepot, Owner.Player);
                            accentColor = Color.FromArgb(0, 229, 255);
                            teamTag = $"{rdCost}G 건물";
                            stat1 = $"HP 6  |  비용 {rdCost}G  |  턴당 아군 1기 +2 수리";
                            stat2 = "매 턴 부상당한 아군 유닛 1기 나노 드론 +2 HP 회복";
                            desc1 = "나노 수리 드론을 파견하여 전선 유닛을 신속하게 정비합니다.";
                            desc2 = "아군 유닛 자유 통과 가능 (2턴당 +1HP 자체수리)";
                            break;
                        case 5: // 가속 관문
                            title = "⏩ 가속 관문 (Phase Gate)";
                            int pgCost = _game.GetBuildingCost(BuildingType.PhaseGate, Owner.Player);
                            accentColor = Color.FromArgb(206, 147, 216);
                            teamTag = $"{pgCost}G 건물";
                            stat1 = $"HP 6  |  비용 {pgCost}G  |  통과 아군 이동력 +1";
                            stat2 = "통과하거나 관문에서 출발하는 아군 유닛 이동력 +1";
                            desc1 = "공간 도약 필드를 전개하여 아군 유닛의 기동을 가속합니다.";
                            desc2 = "중장갑 병기나 보병의 전선 합류 속도를 대폭 향상!";
                            break;
                    }
                }
                else // BottomCategory.Skills
                {
                    int slotIndex = _hoverButtonId - 1;
                    switch (slotIndex)
                    {
                        case 0: // [1] 궤도 폭격
                            title = "🎯 정밀 궤도 폭격 (Orbital Strike)";
                            accentColor = _colorCrimson;
                            teamTag = "8G 스킬";
                            stat1 = "비용 8G  |  타깃 지정 단일 포격";
                            stat2 = "적 건물 / 서버 랙 즉시 6 피해 (포탑 원샷!)";
                            desc1 = "전장의 적 건물이나 서버 랙 1개를 정밀 타격하여 파괴합니다.";
                            desc2 = "사각지대 없는 궤도 레이저로 방화벽 포탑을 즉시 철거!";
                            break;
                        case 1: // [2] 전자기 EMP 교란
                            title = "⚡ 전자기 EMP 교란 (EMP Jammer)";
                            accentColor = _colorCyan;
                            teamTag = "5G 스킬";
                            stat1 = "비용 5G  |  3x3 광역 전자기 교란";
                            stat2 = "2턴간 발전 / 포탑 사격 완전 마비 (EmpTurns=2)";
                            desc1 = "목표 지점 3x3 범위 내 모든 적 기물의 기능을 2턴간 정지시킵니다.";
                            desc2 = "적의 위험한 포탑 밀집 구역이나 발전소 단지를 일시 무력화!";
                            break;
                        case 2: // [3] 치료 광선
                            title = "💚 전술 치료 광선 (Healing Beam)";
                            accentColor = Color.FromArgb(0, 230, 118);
                            teamTag = "4G 스킬";
                            stat1 = "비용 4G  |  선택 행(Row) 광역 수복 레이저";
                            stat2 = "해당 행 모든 아군 유닛 및 건물 체력 +6 수복";
                            desc1 = "선택한 1개 행의 모든 아군 유닛과 건물을 동시에 +6 HP 대폭 수리합니다.";
                            desc2 = "최대 체력 초과 불가 · 위기 라인을 단번에 재건하는 긴급 수복기!";
                            break;
                        case 3: // [4] 구축 광선
                            title = "🛡️ 전술 구축 광선 (Fortify Beam)";
                            accentColor = Color.FromArgb(0, 229, 255);
                            teamTag = "5G 스킬";
                            stat1 = "비용 5G  |  선택 행(Row) 광역 강화 레이저";
                            stat2 = "해당 행 아군 유닛 최대 체력 +2 & 현재 체력 +2 영구 증강";
                            desc1 = "영구 방호 장갑 필드를 전개하여 1개 행 아군 내구도를 높입니다.";
                            desc2 = "최대 체력과 현재 체력이 동시에 +2 증가!";
                            break;
                        case 4: // [5] 정찰 스캔
                            title = "📡 전장 정찰 스캔 (Recon Scan)";
                            accentColor = _colorGold;
                            teamTag = "3G 스킬";
                            stat1 = "비용 3G  |  즉발 전장 시야 스캔";
                            stat2 = "전장의 모든 은폐 침투병 강제 해제";
                            desc1 = "클로킹 중인 적 침투병의 광학 스텔스를 강제로 영구 해제합니다.";
                            desc2 = "기본 포탑과 원거리 유닛들이 즉시 은폐 유닛을 요격 가능해집니다.";
                            break;
                        case 5: // [6] 미사용
                            break;
                    }
                }
            }
            // 2. 보드 위의 기물 또는 타일 호버 시 인스펙터 팝업
            else if (_hoverTile.X >= 1 && _hoverTile.X < _game.Cols - 1 && _hoverTile.Y >= 0 && _hoverTile.Y < _game.Rows)
            {
                int hx = _hoverTile.X;
                int hy = _hoverTile.Y;

                var unit = _game.Units.Find(u => u.Col == hx && u.Row == hy && u.Hp > 0);
                var bldg = _game.Buildings[hy, hx];
                var tile = _game.MapTiles[hy, hx];

                // 아무것도 없는 일반 빈 타일인 경우 표시하지 않음 (시야 보존)
                if (unit == null && bldg == null && tile == TileType.Plain) return;

                // [핵심 개선] 유닛과 건물이 중첩된 경우: 둘 다 HUD에 명확히 동시 표기!
                if (unit != null && bldg != null)
                {
                    cardW = 275;
                    cardH = 138;
                    accentColor = (_selectedSkill == UserSkillType.OrbitalStrike) ? _colorCrimson :
                                  (unit.Owner == Owner.Player ? _colorCyan : _colorCrimson);

                    string uName = unit.Type == UnitType.Vanguard ? "●보병" :
                                   unit.Type == UnitType.Raider ? "▲침투병" :
                                   unit.Type == UnitType.Titan ? "■타이탄" :
                                   unit.Type == UnitType.Cheongung ? "🚀천궁" :
                                   unit.Type == UnitType.Colossus ? "⬢콜로서스" : "✦드레드노트";

                    string bName = bldg.Type == BuildingType.PowerPlant ? "⚡발전소" :
                                   bldg.Type == BuildingType.Turret ? "🛡️포탑" :
                                   bldg.Type == BuildingType.Factory ? "🏭전진공장" :
                                   bldg.Type == BuildingType.RepairDepot ? "🏥수리격납고" :
                                   bldg.Type == BuildingType.PhaseGate ? "⏩가속관문" : "🧱격벽";

                    string uTag = unit.Owner == Owner.Player ? "아군" : "적";
                    string bTag = bldg.Owner == Owner.Player ? "아군" : "적";

                    title = $"{uName} + {bName}";
                    teamTag = $"{uTag}/{bTag}";

                    stat1 = $"⚔️ [유닛] HP {unit.Hp}/{unit.MaxHp}  |  공 {unit.Atk}  |  이속 {unit.Speed}칸" + (unit.HasActed ? " [⏳대기]" : "");
                    string bldgEffect = (bldg.Type == BuildingType.PowerPlant) ? (tile == TileType.Resource ? "+3G/턴 (3배!)" : "+1G/턴") :
                                        (bldg.Type == BuildingType.Turret) ? "요격 2 (13칸)" :
                                        (bldg.Type == BuildingType.Factory) ? "전진 스폰" :
                                        (bldg.Type == BuildingType.RepairDepot) ? "턴당 1기수리" :
                                        (bldg.Type == BuildingType.PhaseGate) ? "이동력+1" : "길목 차단";
                    stat2 = $"🏗️ [건물] {bName} (HP {bldg.Hp}/{bldg.MaxHp})  |  {bldgEffect}";
                    desc1 = (bldg.Owner == Owner.Player) ? "아군 유닛이 아군 인프라 위를 통과/점유 중입니다." : "적 유닛이 건물 타일 상에 위치해 있습니다.";
                    desc2 = (_selectedSkill == UserSkillType.OrbitalStrike) ?
                        "🎯 [궤도 폭격 타깃!] 클릭 시 하단 건물에 6 피해 적중 (원샷 철거)" :
                        "💡 건물과 유닛이 중첩된 타일 (폭격 시 건물 우선 타격)";
                }
                else if (unit != null)
                {
                    accentColor = (unit.Owner == Owner.Player) ? _colorCyan : _colorCrimson;
                    teamTag = (unit.Owner == Owner.Player) ? "아군" : "적 데몬";

                    switch (unit.Type)
                    {
                        case UnitType.Vanguard:
                            title = "● 보병 (Vanguard)";
                            desc1 = "든든한 기본 보병 · 아군 인프라 자유 통과";
                            desc2 = "적 유닛 조우 시 전선 저지선 형성 맞대치";
                            break;
                        case UnitType.Raider:
                            title = "▲ 고속 침투병 (Raider)";
                            desc1 = "이속 2칸 고속 기동 · 빈 라인 기습 침투";
                            desc2 = "적 발전소 기습 파괴 및 코어 자폭 특화";
                            break;
                        case UnitType.Titan:
                            title = "■ 타이탄 (Titan)";
                            desc1 = "중장갑 공성 거신 · 사거리 2칸 원거리 포격";
                            desc2 = "사거리 2에서 포격 시 적 근접 유닛 반격 없음";
                            break;
                        case UnitType.Colossus:
                            title = "⬢ 공성 콜로서스 (Colossus)";
                            accentColor = Color.FromArgb(220, 100, 255);
                            desc1 = "최강 공성 거포 · 사거리 2~3칸 일방적 초원거리 포격";
                            desc2 = "특성: 인접 1칸 근접 공격 불가 & 피격 시 반격 불가";
                            break;
                        case UnitType.Dreadnought:
                            title = "✦ 드레드노트 (Dreadnought)";
                            accentColor = Color.FromArgb(255, 190, 0);
                            desc1 = "최종 결전 워커 · 사거리 2칸 [가로세로 십자 스플래시 2뎀]";
                            desc2 = "핸디캡: 보병/침투병(소형)에게 피격 시 +2 추가 피해!";
                            break;
                        case UnitType.Cheongung:
                            title = "🚀 천궁 미사일 포대 (Cheongung)";
                            accentColor = Color.FromArgb(255, 120, 50);
                            desc1 = "초장거리 미사일 포대 · 건물/서버랙 2배 피해 (6 피해)";
                            desc2 = "무조건 전진 행군 (시즈 모드 없음, 최소 사거리 존재)";
                            break;
                    }

                    stat1 = $"HP {unit.Hp}/{unit.MaxHp}  |  공격력 {unit.Atk}";
                    stat2 = $"이속 {unit.Speed}칸  |  사거리 {unit.Range}칸" + (unit.HasActed ? "  [⏳ 준비중]" : "");
                }
                else if (bldg != null)
                {
                    accentColor = (bldg.Owner == Owner.Player) ? _colorEmerald : _colorCrimson;
                    teamTag = (bldg.Owner == Owner.Player) ? "아군" : "적 데몬";

                    switch (bldg.Type)
                    {
                        case BuildingType.PowerPlant:
                            title = "⚡ 발전소 (PowerPlant)";
                            accentColor = _colorGold;
                            int plantCount = _game.GetBuildingCount(BuildingType.PowerPlant, bldg.Owner);
                            int plantLimit = _game.GetPlantLimit(bldg.Owner);
                            stat1 = $"HP {bldg.Hp}/{bldg.MaxHp}  |  비용 {_game.GetBuildingCost(BuildingType.PowerPlant)}G [가동 {plantCount}/{plantLimit}기]";
                            bool onResource = (tile == TileType.Resource);
                            stat2 = onResource ? "⚡ 자원 노드 가동 중: 수입 +3G/턴 (3배!)" : "기본 전력 생산 중: 수입 +1G/턴";
                            desc1 = $"매 턴 시작 시 크레딧(G) 생산 (현재 한도 {plantLimit}기)";
                            desc2 = "특성: 아군 유닛이 자유롭게 통과 가능 (한도 초과 시 골드로 증설)";
                            break;
                        case BuildingType.Turret:
                            title = "🛡️ 방화벽 포탑 (Turret)";
                            int tCost = _game.GetBuildingCost(BuildingType.Turret, bldg.Owner);
                            stat1 = $"HP {bldg.Hp}/{bldg.MaxHp}  |  비용 {tCost}G (지을수록 +2G)";
                            int bonusR = (bldg.Owner == Owner.Player) ? _game.TurretRangeBonus : 0;
                            stat2 = "요격 공격력 2  |  13칸 전방위 수호 요새";
                            desc1 = "13칸 반경 내 가장 위협적인 적 1기를 매 턴 자동 요격";
                            desc2 = "특성: 아군 유닛 자유 통과 (3x3 박스 및 상하좌우 십자 사거리)";
                            break;
                        case BuildingType.Wall:
                            title = "🧱 방화벽 격벽 (Wall)";
                            accentColor = Color.FromArgb(160, 180, 200);
                            stat1 = $"HP {bldg.Hp}/{bldg.MaxHp}  |  비용 {_game.GetBuildingCost(BuildingType.Wall)}G";
                            stat2 = "방어 바리케이드  |  공격력 0";
                            desc1 = "적 유닛의 진격을 막아 아군 기지를 보호";
                            desc2 = "주의: 아군 유닛의 진격도 차단합니다!";
                            break;
                        case BuildingType.Factory:
                            title = "🏭 전진 공장 (Factory)";
                            accentColor = _colorPurple;
                            stat1 = $"HP {bldg.Hp}/{bldg.MaxHp}  |  비용 {_game.GetBuildingCost(BuildingType.Factory)}G";
                            stat2 = "전진 생산 기지  |  스폰 거점";
                            desc1 = "공장 바로 앞 1칸에서 유닛을 즉시 전진 소환";
                            desc2 = "특성: 아군 유닛이 자유롭게 통과 가능";
                            break;
                        case BuildingType.RepairDepot:
                            title = "🏥 수리 격납고 (Repair Depot)";
                            accentColor = Color.FromArgb(0, 229, 255);
                            stat1 = $"HP {bldg.Hp}/{bldg.MaxHp}  |  비용 {_game.GetBuildingCost(BuildingType.RepairDepot)}G";
                            stat2 = "턴당 아군 1기 +1 수리  |  지원 기지";
                            desc1 = "매 턴 시작 시 부상당한 아군 유닛 1기를 나노 수리";
                            desc2 = "특성: 아군 유닛이 자유롭게 통과 가능 (2턴당 +1HP 자체수리)";
                            break;
                        case BuildingType.PhaseGate:
                            title = "⏩ 가속 관문 (Phase Gate)";
                            accentColor = Color.FromArgb(206, 147, 216);
                            stat1 = $"HP {bldg.Hp}/{bldg.MaxHp}  |  비용 {_game.GetBuildingCost(BuildingType.PhaseGate)}G";
                            stat2 = "통과 아군 이동력 +1  |  기동 가속";
                            desc1 = "관문을 통과하거나 관문에서 출발하는 아군 유닛 이동력 +1";
                            desc2 = "특성: 아군 유닛이 자유롭게 통과 가능 (2턴당 +1HP 자체수리)";
                            break;
                    }
                }
                else if (tile == TileType.Resource)
                {
                    title = "⚡ 중립 에너지 노드";
                    teamTag = "중립 자원";
                    accentColor = _colorGold;
                    stat1 = "자원 타일  |  선점 쟁탈 거점";
                    stat2 = "발전소 건설 시 턴당 +3G 수입 (일반의 3배!)";
                    desc1 = "전장의 핵심 에너지 집적 구역입니다.";
                    desc2 = "이곳에 발전소를 먼저 건설하면 자원 우위를 점합니다.";
                }
                else if (tile == TileType.Obstacle)
                {
                    int hp = _game.ObstacleHp[hy, hx];
                    title = "🛑 보안 서버 랙";
                    teamTag = "파괴 가능";
                    accentColor = Color.FromArgb(160, 175, 195);
                    stat1 = $"HP {hp} / 6  |  파괴 보상: +2G";
                    stat2 = "중립 방어벽  |  모든 공격/이동 차단";
                    desc1 = "유닛 또는 타이탄 포격으로 직접 파괴할 수 있습니다.";
                    desc2 = "파괴 시 통로가 개방되며 +2G 데이터 스크랩을 획득합니다!";
                }
                else if (tile == TileType.Fire)
                {
                    title = "🔥 과열 화염 지대";
                    teamTag = "위험 지형";
                    accentColor = Color.FromArgb(255, 87, 34);
                    stat1 = "지형 피해: -1 HP  |  건물 건설 불가";
                    stat2 = "유닛 통과 및 턴 종료 시 지속 화염 피해";
                    desc1 = "이글거리는 고열 리액터 파편 지대입니다.";
                    desc2 = "체력이 낮은 유닛은 통과 중 파괴될 수 있으니 우회하세요!";
                }
                else if (tile == TileType.Highway)
                {
                    title = "⏩ 가속 하이웨이 레일";
                    teamTag = "고속 기동";
                    accentColor = Color.FromArgb(0, 229, 255);
                    stat1 = "기동 가속: 전방 +1칸  |  초고속 회랑";
                    stat2 = "레일에 진입하거나 위에서 전진 시 즉시 추가 이동";
                    desc1 = "양자 자기부상 고속 기동 레일입니다.";
                    desc2 = "전선으로 병력을 초고속 투입하는 핵심 통로로 활용하세요!";
                }
                else if (tile == TileType.NanoWell)
                {
                    title = "💚 나노 수복 샘";
                    teamTag = "치유 거점";
                    accentColor = Color.FromArgb(76, 175, 80);
                    stat1 = "치유 효과: 매 턴 +1 HP 회복  |  전술 요충지";
                    stat2 = "턴 종료 시 머무르는 아군/적군 유닛 나노 수리";
                    desc1 = "자가 증식 나노머신이 방출되는 회복 거점입니다.";
                    desc2 = "이곳을 점령하고 버티며 전선을 압박하는 전초기지로 활용하세요!";
                }
                else if (tile == TileType.Smoke)
                {
                    title = "🌫️ 스모크 필드";
                    teamTag = "은폐 연막";
                    accentColor = Color.FromArgb(186, 104, 200);
                    stat1 = "효과: 원거리 요격/공격 면역  |  잠입 회랑";
                    stat2 = "타일 위 유닛 포탑 요격 불가 · 1칸 근접 교전만 가능";
                    desc1 = "홀로그램 전파 방해 안개가 자욱한 특수 구역입니다.";
                    desc2 = "적 포탑 방어선을 우회하여 본진 깊숙이 침투하는 통로입니다!";
                }

                // 카드 위치 계산 (스마트 앵커링)
                int tileScreenX = _boardRect.X + hx * _tileW;
                int tileScreenY = _boardRect.Y + hy * _tileH;

                // X 위치: 좌/우 여백에 따라 타일 옆으로 자연스럽게 배치 (중앙 기준 6열)
                cardX = (hx <= 6) ? tileScreenX + _tileW + 10 : tileScreenX - cardW - 10;
                // Y 위치: 타일 수평 중앙 맞춤 후 화면 밖 벗어남 방지 클램핑
                cardY = tileScreenY + (_tileH - cardH) / 2;
                cardY = Math.Max(_boardRect.Y + 2, Math.Min(_boardRect.Bottom - cardH - 2, cardY));
            }
            else
            {
                return;
            }

            var cardRect = new Rectangle(cardX, cardY, cardW, cardH);

            // 1. 카드 배경 (다크 네온 글래스)
            using (var bgBrush = new SolidBrush(Color.FromArgb(242, 12, 18, 28)))
            using (var borderPen = new Pen(accentColor, 1.5f))
            {
                g.FillRoundedRectangle(bgBrush, cardRect, 6);
                g.DrawRoundedRectangle(borderPen, cardRect, 6);
            }

            // 2. 상단 헤더 (타이틀 + 소속 태그)
            using (var titleFont = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (var tagFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            {
                TextRenderer.DrawText(g, title, titleFont, new Rectangle(cardRect.X + 8, cardRect.Y + 6, cardRect.Width - 68, 18),
                    Color.White, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

                // 소속 태그 뱃지
                var tagRect = new Rectangle(cardRect.Right - 58, cardRect.Y + 6, 50, 16);
                using (var tBg = new SolidBrush(Color.FromArgb(50, accentColor)))
                using (var tPen = new Pen(accentColor, 1f))
                {
                    g.FillRoundedRectangle(tBg, tagRect, 3);
                    g.DrawRoundedRectangle(tPen, tagRect, 3);
                }
                TextRenderer.DrawText(g, teamTag, tagFont, tagRect, accentColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            // 구분선
            using (var divPen = new Pen(Color.FromArgb(55, 75, 100), 1f))
            {
                g.DrawLine(divPen, cardRect.X + 8, cardRect.Y + 26, cardRect.Right - 8, cardRect.Y + 26);
            }

            // 3. 스탯 정보 (Row 1 & Row 2)
            using (var statFont = new Font("Segoe UI", 8f, FontStyle.Bold))
            using (var descFont = new Font("Segoe UI", 7.5f))
            {
                TextRenderer.DrawText(g, stat1, statFont, new Rectangle(cardRect.X + 8, cardRect.Y + 29, cardRect.Width - 16, 16),
                    _colorGold, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

                TextRenderer.DrawText(g, stat2, statFont, new Rectangle(cardRect.X + 8, cardRect.Y + 47, cardRect.Width - 16, 16),
                    Color.FromArgb(200, 225, 250), TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

                // 4. 설명 텍스트
                int d1Y = (cardH > 100) ? cardRect.Y + 68 : cardRect.Y + 63;
                int d2Y = (cardH > 100) ? cardRect.Y + 88 : cardRect.Y + 77;
                Color d2Color = (_selectedSkill == UserSkillType.OrbitalStrike) ?
                    Color.FromArgb(255, 110, 110) : Color.FromArgb(115, 145, 170);

                TextRenderer.DrawText(g, desc1, descFont, new Rectangle(cardRect.X + 8, d1Y, cardRect.Width - 16, 16),
                    Color.FromArgb(145, 170, 195), TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

                TextRenderer.DrawText(g, desc2, descFont, new Rectangle(cardRect.X + 8, d2Y, cardRect.Width - 16, (cardH > 100) ? 38 : 16),
                    d2Color, TextFormatFlags.Left | TextFormatFlags.WordBreak);
            }
        }
    }
}
