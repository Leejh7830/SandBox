using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace GateHelper.DataFlip
{
    public partial class DataFlipControl
    {
        #region Paint Rendering

        protected override void OnPaint(PaintEventArgs e)
        {
            try
            {
                base.OnPaint(e);
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                // 0. 리플레이 전용 시네마틱 뷰어 모드
                if (_isReplaying)
                {
                    DrawReplayMode(g);
                    return;
                }

                g.Clear(_colorBg);

                // 1. 헤더 & HUD (본진 HP, 자원 수입, 패시브 뱃지)
                DrawHeader(g);

                // 1.5 한줄 전투 로그 배너 (컴퓨터/플레이어 동작 실시간 보고)
                DrawCombatLogBanner(g);

                // 2. 12x8 전장 맵 타일 & 일체화된 메인프레임 코어 기지
                DrawMap(g);

                // 3. 건물 & 유닛 렌더링
                DrawBuildings(g);
                DrawUnits(g);

                // 4. 포탑 레이저 빔 렌더링
                DrawLaserBeams(g);

                // 4.5 사거리 원형 링 & 타일 하이라이트 (신규!)
                DrawRangeIndicators(g);

                // 5. 마우스 호버 가이드 / 고스트 렌더링
                DrawPlacementGhost(g);

                // 6. 비주얼 이펙트 & 플로팅 텍스트
                DrawEffects(g);

                // 7. 하단 조작 바 (건물, 유닛, 턴 종료)
                DrawBottomBar(g);

                // 7.5 마우스 호버 기물 & 타일 인스펙터 정보 카드 (신규!)
                DrawInspectorCard(g);

                // 7.8 전투 로그 상세 히스토리 팝업 (배너 마우스 호버 시 최상단 오버레이)
                if (_hoverOnCombatLog)
                {
                    DrawCombatLogHistoryPopup(g);
                }

                // 8. 로그라이크 카드 선택 팝업 (3턴 주기)
                if (_game.IsCardSelectionActive)
                {
                    DrawCardSelectionModal(g);
                }
                // 9. 룰 가이드
                else if (_showGuide)
                {
                    DrawGuideModal(g);
                }
                // 10. 도전과제 모달
                else if (_showAchievements)
                {
                    DrawAchievementsModal(g);
                }
                // 11. 게임 종료 화면
                else if (_game.IsGameOver)
                {
                    DrawGameOverModal(g);
                }
                // 11. 맵 사이즈 선택 모달 (게임 시작 / 재시작 시)
                else if (_isMapSizeSelectActive)
                {
                    DrawMapSizeSelectModal(g);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "[CoreFront] OnPaint exception");
            }
        }

        private void DrawHeader(Graphics g)
        {
            // 로비 버튼
            bool isBackHover = _hoverButtonId == 0;
            using (var bBrush = new SolidBrush(isBackHover ? Color.FromArgb(40, 50, 70) : Color.FromArgb(25, 32, 45)))
            using (var bPen = new Pen(isBackHover ? _colorCyan : _colorGridLine, 1f))
            {
                g.FillRoundedRectangle(bBrush, _btnBackRect, 4);
                g.DrawRoundedRectangle(bPen, _btnBackRect, 4);
            }
            using (var font = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            {
                TextRenderer.DrawText(g, "◀ 로비", font, _btnBackRect, Color.FromArgb(200, 215, 230),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            // 타이틀 (컴팩트 배치)
            using (var font = new Font("Segoe UI", 11f, FontStyle.Bold))
            using (var brush = new SolidBrush(_colorCyan))
            {
                g.DrawString("◈ CORE FRONT", font, brush, 88, 14);
            }

            // [맵 사이즈 선택 토글 4종] (소형 / 표준 / 대형 / 초대형)
            DrawMapSizeToggle(g, _btnMapSmallRect, "소형", MapSizeType.Small, 91);
            DrawMapSizeToggle(g, _btnMapStdRect, "표준", MapSizeType.Standard, 92);
            DrawMapSizeToggle(g, _btnMapLargeRect, "대형", MapSizeType.Large, 93);
            DrawMapSizeToggle(g, _btnMapHugeRect, "초대형", MapSizeType.Huge, 94);

            // [턴 진행 3단 배속 토글] (⚡1x / ⚡2x / ⚡4x)
            DrawTurnSpeedToggle(g, _btnTurnSpeedRect);

            // [🏆 도전과제 버튼] (헤더 구석 원클릭 확인)
            DrawSmallButton(g, _btnAchievementsRect, "🏆 도전과제", 11);

            // 타이틀 줄 우측: 로그라이크 패시브 보너스 뱃지 렌더링 (HUD 바 내부가 아닌 상단 타이틀 줄 우측에 배치하여 겹침 원천 차단!)
            int badgeRight = this.Width - 14;
            if (_game.CurrentMapSize == MapSizeType.Huge)
            {
                DrawPassiveBadgeRight(g, ref badgeRight, 13, $"{_game.GetThemeIcon()} {_game.GetThemeName()}", _game.GetThemeColor(), true);
            }
            int basePlantLimit = (_game.CurrentMapSize == MapSizeType.Huge) ? 5 : 3;
            DrawPassiveBadgeRight(g, ref badgeRight, 13, "⚡한도" + _game.PlayerPlantLimit + "기", _colorCyan, _game.PlayerPlantLimit > basePlantLimit);
            DrawPassiveBadgeRight(g, ref badgeRight, 13, "🎯사거리+" + _game.TurretRangeBonus, _colorPurple, _game.TurretRangeBonus > 0);
            DrawPassiveBadgeRight(g, ref badgeRight, 13, "🔋+" + _game.PlayerIncomeBonus + "G", _colorGold, _game.PlayerIncomeBonus > 0);
            DrawPassiveBadgeRight(g, ref badgeRight, 13, "⚔️공+" + _game.PlayerUnitAtkBonus, Color.FromArgb(215, 60, 255), _game.PlayerUnitAtkBonus > 0);
            DrawPassiveBadgeRight(g, ref badgeRight, 13, "🛡️HP+" + _game.PlayerUnitHpBonus, _colorEmerald, _game.PlayerUnitHpBonus > 0);
            DrawPassiveBadgeRight(g, ref badgeRight, 13, "🔭시즈", Color.FromArgb(186, 104, 200), _game.PlayerHasSiegeAnchor);
            DrawPassiveBadgeRight(g, ref badgeRight, 13, "⚔️근접발칸", Color.FromArgb(255, 179, 0), _game.PlayerHasColossusMelee);
            DrawPassiveBadgeRight(g, ref badgeRight, 13, "🔭정밀궤적", Color.FromArgb(255, 87, 34), _game.PlayerHasCheongungTrajectory);
            DrawPassiveBadgeRight(g, ref badgeRight, 13, "🛡️실드", Color.FromArgb(64, 196, 255), _game.PlayerHasPhalanxShield);
            DrawPassiveBadgeRight(g, ref badgeRight, 13, "⚡스텔스", Color.FromArgb(38, 198, 218), _game.PlayerHasStealthCloak);
            DrawPassiveBadgeRight(g, ref badgeRight, 13, "📡레이더", Color.FromArgb(0, 200, 255), _game.PlayerHasTurretRadar);
            DrawPassiveBadgeRight(g, ref badgeRight, 13, "💥넉백", Color.FromArgb(255, 138, 101), _game.PlayerHasKineticSlam);
            DrawPassiveBadgeRight(g, ref badgeRight, 13, "✦십자방전", Color.FromArgb(0, 229, 255), _game.PlayerHasChainPlasma);

            // 상단 HUD 바 (X: 14, Y: 42, W: Width - 28, H: 28)
            int hudX = 14;
            int hudY = 42;
            int hudW = this.Width - 28;
            var hudRect = new Rectangle(hudX, hudY, hudW, 28);

            using (var brush = new SolidBrush(Color.FromArgb(20, 25, 35)))
            using (var pen = new Pen(_colorGridLine, 1f))
            {
                g.FillRoundedRectangle(brush, hudRect, 4);
                g.DrawRoundedRectangle(pen, hudRect, 4);
            }

            // [1. 좌측 플레이어 구역: HP바 (W:60) + 크레딧]
            int barW = 60;
            int barH = 8;
            int pBarX = hudX + 76;
            int pBarY = hudY + 10;
            float pRatio = (float)_game.PlayerCoreHp / _game.PlayerCoreMaxHp;

            using (var bgBrush = new SolidBrush(Color.FromArgb(40, 20, 30)))
            using (var fillBrush = new SolidBrush(_colorCyan))
            {
                g.FillRoundedRectangle(bgBrush, new Rectangle(pBarX, pBarY, barW, barH), 2);
                if (pRatio > 0)
                    g.FillRoundedRectangle(fillBrush, new Rectangle(pBarX, pBarY, (int)(barW * pRatio), barH), 2);
            }

            // [2. 우측 AI 구역: HP바 (W:60) + 크레딧]
            int aBarX = hudX + hudW - 74 - barW;
            int aBarY = hudY + 10;
            float aRatio = (float)_game.AiCoreHp / _game.AiCoreMaxHp;

            using (var bgBrush = new SolidBrush(Color.FromArgb(40, 20, 30)))
            using (var fillBrush = new SolidBrush(_colorCrimson))
            {
                g.FillRoundedRectangle(bgBrush, new Rectangle(aBarX, aBarY, barW, barH), 2);
                if (aRatio > 0)
                    g.FillRoundedRectangle(fillBrush, new Rectangle(aBarX, aBarY, (int)(barW * aRatio), barH), 2);
            }

            using (var font = new Font("Segoe UI", 9f, FontStyle.Bold))
            {
                // 플레이어 본진 HP
                TextRenderer.DrawText(g, $"본진 HP {_game.PlayerCoreHp}", font, new Rectangle(hudX + 6, hudY + 4, 68, 20),
                    _colorCyan, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

                // 플레이어 크레딧 (HP 바 우측)
                int pIncome = _game.CalculateIncome(Owner.Player);
                TextRenderer.DrawText(g, $"⬡ {_game.PlayerCredit}G (+{pIncome}G)", font, new Rectangle(pBarX + barW + 8, hudY + 4, 90, 20),
                    _colorGold, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

                // [중앙 턴 정보] - 독립된 중앙 영역에 고정 배치 (어떤 해상도에서도 좌우와 겹치지 않음!)
                string turnText = $"[ 턴 {_game.TurnNumber} ]";
                TextRenderer.DrawText(g, turnText, font, new Rectangle(hudX + (hudW - 90) / 2, hudY + 4, 90, 20),
                    Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                // AI 크레딧 (AI 바 좌측)
                int aIncome = _game.CalculateIncome(Owner.Ai);
                TextRenderer.DrawText(g, $"⬡ {_game.AiCredit}G (+{aIncome}G)", font, new Rectangle(aBarX - 98, hudY + 4, 90, 20),
                    Color.FromArgb(220, 160, 120), TextFormatFlags.Right | TextFormatFlags.VerticalCenter);

                // AI 본진 HP
                TextRenderer.DrawText(g, $"적 본진 {_game.AiCoreHp}", font, new Rectangle(hudX + hudW - 74, hudY + 4, 68, 20),
                    _colorCrimson, TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
            }
        }

        private void DrawPassiveBadgeRight(Graphics g, ref int rightX, int y, string text, Color color, bool active)
        {
            if (!active || string.IsNullOrEmpty(text)) return;
            using (var font = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            {
                var measured = TextRenderer.MeasureText(g, text, font, Size.Empty, TextFormatFlags.SingleLine);
                int w = Math.Max(52, measured.Width + 14);
                int h = 20;
                int x = rightX - w;
                var rect = new Rectangle(x, y, w, h);
                using (var brush = new SolidBrush(Color.FromArgb(45, color)))
                using (var pen = new Pen(color, 1.2f))
                {
                    g.FillRoundedRectangle(brush, rect, 3);
                    g.DrawRoundedRectangle(pen, rect, 3);
                }
                TextRenderer.DrawText(g, text, font, rect, color,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
                rightX = x - 4; // 다음 뱃지를 왼쪽으로 4px 띄워서 나란히 배치!
            }
        }

        private void DrawMapSizeToggle(Graphics g, Rectangle rect, string text, MapSizeType size, int btnId)
        {
            bool isCurrent = (_game.CurrentMapSize == size);
            bool isHover = (_hoverButtonId == btnId);

            Color bg = isCurrent ? Color.FromArgb(20, 50, 70) :
                       isHover ? Color.FromArgb(35, 45, 60) : Color.FromArgb(22, 28, 38);
            Color border = isCurrent ? _colorCyan :
                           isHover ? Color.FromArgb(120, 150, 180) : _colorGridLine;
            Color textCol = isCurrent ? _colorCyan :
                            isHover ? Color.White : Color.FromArgb(160, 180, 200);

            using (var brush = new SolidBrush(bg))
            using (var pen = new Pen(border, isCurrent ? 1.5f : 1f))
            {
                g.FillRoundedRectangle(brush, rect, 3);
                g.DrawRoundedRectangle(pen, rect, 3);
            }
            using (var font = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            {
                TextRenderer.DrawText(g, text, font, rect, textCol,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private void DrawTurnSpeedToggle(Graphics g, Rectangle rect)
        {
            if (rect.Width <= 0 || rect.Height <= 0) return;

            bool isHover = (_hoverButtonId == 96);
            string spdText = (_turnPlaySpeed == 1) ? "⚡1x" : (_turnPlaySpeed == 2) ? "⚡2x" : "⚡4x";
            Color speedCol = (_turnPlaySpeed == 4) ? Color.FromArgb(255, 60, 200) :
                             (_turnPlaySpeed == 2) ? _colorGold : _colorCyan;

            Color bg = isHover ? Color.FromArgb(40, 52, 70) : Color.FromArgb(22, 28, 38);
            using (var brush = new SolidBrush(bg))
            using (var pen = new Pen(isHover ? speedCol : _colorGridLine, isHover ? 1.5f : 1f))
            {
                g.FillRoundedRectangle(brush, rect, 3);
                g.DrawRoundedRectangle(pen, rect, 3);
            }
            using (var font = new Font("Segoe UI", 8f, FontStyle.Bold))
            {
                TextRenderer.DrawText(g, spdText, font, rect, speedCol,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private void DrawCombatLogBanner(Graphics g)
        {
            if (_combatLogRect.Width <= 0 || _combatLogRect.Height <= 0) return;

            bool isHover = _hoverOnCombatLog;
            Color borderCol = isHover ? _colorCyan : Color.FromArgb(45, 55, 75);
            Color bgCol = isHover ? Color.FromArgb(28, 36, 50) : Color.FromArgb(18, 22, 32);

            using (var brush = new SolidBrush(bgCol))
            using (var pen = new Pen(borderCol, 1f))
            {
                g.FillRoundedRectangle(brush, _combatLogRect, 3);
                g.DrawRoundedRectangle(pen, _combatLogRect, 3);
            }

            // 좌측 네온 뱃지: [⚡ 전투로그]
            int badgeW = 70;
            var badgeRect = new Rectangle(_combatLogRect.X + 3, _combatLogRect.Y + 2, badgeW, _combatLogRect.Height - 4);
            using (var bBrush = new SolidBrush(isHover ? Color.FromArgb(40, 55, 75) : Color.FromArgb(28, 38, 52)))
            using (var bPen = new Pen(_colorCyan, 1f))
            {
                g.FillRoundedRectangle(bBrush, badgeRect, 2);
                g.DrawRoundedRectangle(bPen, badgeRect, 2);
            }
            using (var bFont = new Font("Segoe UI", 7.8f, FontStyle.Bold))
            {
                TextRenderer.DrawText(g, "⚡ 전투로그", bFont, badgeRect, _colorCyan,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            // 우측 안내 힌트: [기록 ▼]
            int hintW = 54;
            var hintRect = new Rectangle(_combatLogRect.Right - hintW - 4, _combatLogRect.Y + 2, hintW, _combatLogRect.Height - 4);
            using (var hFont = new Font("Segoe UI", 7.5f, FontStyle.Regular))
            {
                TextRenderer.DrawText(g, isHover ? "기록 ▲" : "기록 ▼", hFont, hintRect,
                    isHover ? _colorCyan : Color.FromArgb(120, 140, 165),
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
            }

            // 중앙 최신 전투 로그 한 줄 텍스트
            string logText = string.IsNullOrEmpty(_game.LatestCombatLog)
                ? "[전황 보고] 전황 분석 및 첫 턴 작전 대기 중입니다."
                : _game.LatestCombatLog;

            int textX = badgeRect.Right + 8;
            int textW = Math.Max(50, hintRect.Left - textX - 4);
            var textRect = new Rectangle(textX, _combatLogRect.Y, textW, _combatLogRect.Height);

            Color textCol = logText.Contains("AI") ? Color.FromArgb(255, 185, 185) :
                            logText.Contains("플레이어") ? Color.FromArgb(180, 240, 255) :
                            Color.FromArgb(215, 225, 235);

            using (var font = new Font("Segoe UI", 8.2f, FontStyle.Regular))
            {
                TextRenderer.DrawText(g, logText, font, textRect, textCol,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
        }

        private void DrawCombatLogHistoryPopup(Graphics g)
        {
            var history = _game.CombatLogHistory;
            if (history == null || history.Count == 0) return;

            int popupW = Math.Min(620, _combatLogRect.Width);
            int itemH = 20;
            int count = Math.Min(6, history.Count);
            int popupH = 26 + count * itemH + 6;
            int popupX = _combatLogRect.X + 6;
            int popupY = _combatLogRect.Bottom + 2;

            var popupRect = new Rectangle(popupX, popupY, popupW, popupH);

            // 반투명 다크 글래스 배경 & 네온 테두리
            using (var brush = new SolidBrush(Color.FromArgb(242, 16, 20, 28)))
            using (var pen = new Pen(_colorCyan, 1.2f))
            {
                g.FillRoundedRectangle(brush, popupRect, 4);
                g.DrawRoundedRectangle(pen, popupRect, 4);
            }

            // 팝업 헤더 타이틀
            using (var tFont = new Font("Segoe UI", 8.2f, FontStyle.Bold))
            {
                var hRect = new Rectangle(popupX + 10, popupY + 4, popupW - 20, 18);
                TextRenderer.DrawText(g, $"◈ 전투 로그 히스토리 (최근 {count}건)", tFont, hRect, _colorCyan,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            }

            // 구분선
            using (var sepPen = new Pen(Color.FromArgb(45, 55, 75), 1f))
            {
                g.DrawLine(sepPen, popupX + 6, popupY + 23, popupX + popupW - 6, popupY + 23);
            }

            // 최근 로그 텍스트 목록 렌더링
            using (var font = new Font("Segoe UI", 8f, FontStyle.Regular))
            {
                for (int i = 0; i < count; i++)
                {
                    string entry = history[i];
                    var lineRect = new Rectangle(popupX + 12, popupY + 26 + i * itemH, popupW - 24, itemH);

                    Color cCol = (i == 0) ? Color.FromArgb(255, 255, 255) :
                                 entry.Contains("AI") ? Color.FromArgb(240, 175, 175) :
                                 entry.Contains("플레이어") ? Color.FromArgb(170, 230, 255) :
                                 Color.FromArgb(175, 185, 200);

                    string bullet = (i == 0) ? "▶ " : "• ";
                    TextRenderer.DrawText(g, bullet + entry, font, lineRect, cCol,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                }
            }
        }

        private void DrawMap(Graphics g)
        {
            // 보드 전체 배경
            using (var brush = new SolidBrush(_colorBoardBg))
            using (var pen = new Pen(Color.FromArgb(50, 65, 85), 2f))
            {
                g.FillRoundedRectangle(brush, _boardRect, 6);
                g.DrawRoundedRectangle(pen, _boardRect, 6);
            }

            // 1. Col 0: 플레이어 아군 거대 기지 섀시 (Cyber Fortress)
            int pBaseW = _tileW;
            var pBaseRect = new Rectangle(_boardRect.X, _boardRect.Y, pBaseW, _boardRect.Height);
            using (var baseBrush = new SolidBrush(Color.FromArgb(10, 22, 35)))
            using (var basePen = new Pen(_colorCyan, 1.5f))
            {
                g.FillRectangle(baseBrush, pBaseRect);
                g.DrawRectangle(basePen, pBaseRect.X + 2, pBaseRect.Y + 2, pBaseW - 4, _boardRect.Height - 4);

                // 중앙 코어 리액터 심볼
                int coreCy = _boardRect.Y + _boardRect.Height / 2;
                using (var cBrush = new SolidBrush(Color.FromArgb(60, 0, 229, 255)))
                using (var cPen = new Pen(_colorCyan, 2f))
                {
                    g.FillEllipse(cBrush, pBaseRect.X + 6, coreCy - 16, 32, 32);
                    g.DrawEllipse(cPen, pBaseRect.X + 6, coreCy - 16, 32, 32);
                }
                using (var font = new Font("Segoe UI", 8f, FontStyle.Bold))
                {
                    TextRenderer.DrawText(g, "CORE", font, new Rectangle(pBaseRect.X, coreCy - 8, pBaseW, 16),
                        _colorCyan, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            }

            // 플레이어 에너지 방어선 (Laser Fence between Col 0 and 1)
            int fenceX1 = _boardRect.X + _tileW;
            using (var fencePen = new Pen(_colorCyan, 2.5f) { DashStyle = DashStyle.Dash })
            {
                g.DrawLine(fencePen, fenceX1, _boardRect.Y, fenceX1, _boardRect.Bottom);
            }

            // 2. Col 11: 적 AI 거대 기지 섀시 (Demon Fortress)
            int aBaseX = _boardRect.Right - _tileW;
            var aBaseRect = new Rectangle(aBaseX, _boardRect.Y, pBaseW, _boardRect.Height);
            using (var baseBrush = new SolidBrush(Color.FromArgb(30, 10, 16)))
            using (var basePen = new Pen(_colorCrimson, 1.5f))
            {
                g.FillRectangle(baseBrush, aBaseRect);
                g.DrawRectangle(basePen, aBaseRect.X + 2, aBaseRect.Y + 2, pBaseW - 4, _boardRect.Height - 4);

                int coreCy = _boardRect.Y + _boardRect.Height / 2;
                using (var cBrush = new SolidBrush(Color.FromArgb(60, 255, 23, 68)))
                using (var cPen = new Pen(_colorCrimson, 2f))
                {
                    g.FillEllipse(cBrush, aBaseRect.X + 6, coreCy - 16, 32, 32);
                    g.DrawEllipse(cPen, aBaseRect.X + 6, coreCy - 16, 32, 32);
                }
                using (var font = new Font("Segoe UI", 8f, FontStyle.Bold))
                {
                    TextRenderer.DrawText(g, "CORE", font, new Rectangle(aBaseRect.X, coreCy - 8, pBaseW, 16),
                        _colorCrimson, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            }

            // 적 에너지 방어선 (Laser Fence between Col 10 and 11)
            int fenceX2 = _boardRect.Right - _tileW;
            using (var fencePen = new Pen(_colorCrimson, 2.5f) { DashStyle = DashStyle.Dash })
            {
                g.DrawLine(fencePen, fenceX2, _boardRect.Y, fenceX2, _boardRect.Bottom);
            }

            // 3. 중앙 10개 열(Col 1~10) 활성 전장 타일 격자 렌더링
            for (int r = 0; r < _game.Rows; r++)
            {
                for (int c = 1; c < _game.Cols - 1; c++)
                {
                    int x = _boardRect.X + c * _tileW;
                    int y = _boardRect.Y + r * _tileH;
                    var tileRect = new Rectangle(x, y, _tileW, _tileH);

                    using (var gridPen = new Pen(_colorGridLine, 1f))
                    {
                        g.DrawRectangle(gridPen, tileRect);
                    }

                    var tType = _game.MapTiles[r, c];
                    if (tType == TileType.Resource)
                    {
                        using (var resBrush = new SolidBrush(Color.FromArgb(35, 255, 215, 0)))
                        using (var resPen = new Pen(_colorGold, 1.5f) { DashStyle = DashStyle.Dash })
                        {
                            g.FillRectangle(resBrush, tileRect);
                            g.DrawRectangle(resPen, x + 2, y + 2, _tileW - 4, _tileH - 4);
                        }
                        using (var font = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                        {
                            TextRenderer.DrawText(g, "⚡2X", font, tileRect, _colorGold,
                                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                        }
                    }
                    else if (tType == TileType.Obstacle)
                    {
                        using (var obsBrush = new SolidBrush(Color.FromArgb(35, 42, 54)))
                        using (var obsPen = new Pen(Color.FromArgb(70, 80, 95), 1.5f))
                        {
                            g.FillRectangle(obsBrush, tileRect);
                            g.DrawRectangle(obsPen, x + 3, y + 3, _tileW - 6, _tileH - 6);
                            g.DrawLine(obsPen, x + 5, y + 5, x + _tileW - 5, y + _tileH - 5);
                            g.DrawLine(obsPen, x + _tileW - 5, y + 5, x + 5, y + _tileH - 5);
                        }

                        // 파괴 가능한 서버 랙 체력 바 (HP 6)
                        int obsHp = _game.ObstacleHp[r, c];
                        DrawMiniHpBar(g, x + 4, y + _tileH - 5, _tileW - 8, 3, obsHp, 6, Color.FromArgb(180, 180, 180));
                        using (var font = new Font("Segoe UI", 7.5f, FontStyle.Bold))
                        {
                            TextRenderer.DrawText(g, "🛑", font, tileRect, Color.FromArgb(170, 180, 195),
                                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                        }
                    }
                    else if (tType == TileType.Fire)
                    {
                        // 1. 화염 지대: 이글거리는 용광로 네온
                        int pulseAlpha = 35 + (int)(Math.Sin(_pulseAngle * 2.5f + r + c) * 15);
                        pulseAlpha = Math.Max(20, Math.Min(60, pulseAlpha));
                        using (var fireBrush = new SolidBrush(Color.FromArgb(pulseAlpha, 255, 69, 0)))
                        using (var firePen = new Pen(Color.FromArgb(180, 255, 87, 34), 1.2f) { DashStyle = DashStyle.Dash })
                        {
                            g.FillRectangle(fireBrush, tileRect);
                            g.DrawRectangle(firePen, x + 2, y + 2, _tileW - 4, _tileH - 4);
                        }
                        using (var font = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                        {
                            TextRenderer.DrawText(g, "🔥", font, tileRect, Color.FromArgb(255, 110, 64),
                                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                        }
                    }
                    else if (tType == TileType.Highway)
                    {
                        // 2. 가속 하이웨이: 사이언/블루 고속 네온 레일
                        int pulseAlpha = 35 + (int)(Math.Sin(_pulseAngle * 3f + c) * 15);
                        pulseAlpha = Math.Max(20, Math.Min(60, pulseAlpha));
                        using (var railBrush = new SolidBrush(Color.FromArgb(pulseAlpha, 0, 200, 255)))
                        using (var railPen = new Pen(Color.FromArgb(160, 0, 229, 255), 1.2f))
                        {
                            g.FillRectangle(railBrush, tileRect);
                            g.DrawRectangle(railPen, x + 2, y + 2, _tileW - 4, _tileH - 4);
                        }
                        using (var font = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                        {
                            TextRenderer.DrawText(g, "⏩", font, tileRect, Color.FromArgb(120, 240, 255),
                                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                        }
                    }
                    else if (tType == TileType.NanoWell)
                    {
                        // 3. 나노 수복 샘: 에메랄드 치유 거점
                        int pulseAlpha = 35 + (int)(Math.Sin(_pulseAngle * 2f + r * 2) * 15);
                        pulseAlpha = Math.Max(20, Math.Min(60, pulseAlpha));
                        using (var wellBrush = new SolidBrush(Color.FromArgb(pulseAlpha, 46, 125, 50)))
                        using (var wellPen = new Pen(Color.FromArgb(180, 76, 175, 80), 1.4f))
                        {
                            g.FillRectangle(wellBrush, tileRect);
                            g.DrawRectangle(wellPen, x + 2, y + 2, _tileW - 4, _tileH - 4);
                        }
                        using (var font = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                        {
                            TextRenderer.DrawText(g, "💚", font, tileRect, Color.FromArgb(165, 214, 167),
                                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                        }
                    }
                    else if (tType == TileType.Smoke)
                    {
                        // 4. 스모크 필드: 보라색 홀로그램 은폐 연막
                        int pulseAlpha = 45 + (int)(Math.Sin(_pulseAngle * 1.8f + r + c) * 15);
                        pulseAlpha = Math.Max(25, Math.Min(70, pulseAlpha));
                        using (var smokeBrush = new SolidBrush(Color.FromArgb(pulseAlpha, 123, 31, 162)))
                        using (var smokePen = new Pen(Color.FromArgb(160, 186, 104, 200), 1.2f) { DashStyle = DashStyle.Dot })
                        {
                            g.FillRectangle(smokeBrush, tileRect);
                            g.DrawRectangle(smokePen, x + 2, y + 2, _tileW - 4, _tileH - 4);
                        }
                        using (var font = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                        {
                            TextRenderer.DrawText(g, "🌫️", font, tileRect, Color.FromArgb(225, 190, 231),
                                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                        }
                    }
                }
            }
        }

        private void DrawBuildings(Graphics g)
        {
            for (int r = 0; r < _game.Rows; r++)
            {
                for (int c = 1; c < _game.Cols - 1; c++)
                {
                    var b = _game.Buildings[r, c];
                    if (b == null) continue;

                    int x = _boardRect.X + c * _tileW + (int)b.ShakeOffset;
                    int y = _boardRect.Y + r * _tileH;
                    var rect = new Rectangle(x + 3, y + 3, _tileW - 6, _tileH - 6);

                    Color teamColor = (b.Owner == Owner.Player) ? _colorCyan : _colorCrimson;

                    // 1. 발전소 (Power Plant)
                    if (b.Type == BuildingType.PowerPlant)
                    {
                        using (var bgBrush = new SolidBrush(Color.FromArgb(45, 255, 215, 0)))
                        using (var rimPen = new Pen(_colorGold, 1.8f))
                        {
                            g.FillRoundedRectangle(bgBrush, rect, 4);
                            g.DrawRoundedRectangle(rimPen, rect, 4);
                        }
                        using (var font = new Font("Segoe UI", 10f, FontStyle.Bold))
                        {
                            TextRenderer.DrawText(g, "⚡", font, rect, _colorGold,
                                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                        }
                    }
                    // 2. 포탑 (Turret)
                    else if (b.Type == BuildingType.Turret)
                    {
                        using (var bgBrush = new SolidBrush(Color.FromArgb(45, 0, 230, 118)))
                        using (var rimPen = new Pen(_colorEmerald, 2f))
                        {
                            g.FillEllipse(bgBrush, rect);
                            g.DrawEllipse(rimPen, rect);

                            int dir = (b.Owner == Owner.Player) ? 1 : -1;
                            int cx = rect.X + rect.Width / 2;
                            int cy = rect.Y + rect.Height / 2;
                            g.DrawLine(rimPen, cx, cy, cx + dir * 14, cy);
                        }
                    }
                    // 3. 방화벽 격벽 (Wall)
                    else if (b.Type == BuildingType.Wall)
                    {
                        using (var bgBrush = new SolidBrush(Color.FromArgb(60, 75, 90)))
                        using (var rimPen = new Pen(Color.FromArgb(140, 160, 180), 2f))
                        {
                            g.FillRectangle(bgBrush, rect);
                            g.DrawRectangle(rimPen, rect);
                        }
                        using (var font = new Font("Segoe UI", 9f, FontStyle.Bold))
                        {
                            TextRenderer.DrawText(g, "🧱", font, rect, Color.White,
                                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                        }
                    }
                    // 4. 전진 공장 (Factory)
                    else if (b.Type == BuildingType.Factory)
                    {
                        using (var bgBrush = new SolidBrush(Color.FromArgb(45, teamColor)))
                        using (var rimPen = new Pen(teamColor, 2f))
                        {
                            g.FillRoundedRectangle(bgBrush, rect, 5);
                            g.DrawRoundedRectangle(rimPen, rect, 5);
                        }
                        using (var font = new Font("Segoe UI", 9.5f, FontStyle.Bold))
                        {
                            TextRenderer.DrawText(g, "🏭", font, rect, teamColor,
                                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                        }
                    }
                    // 5. 수리 격납고 (RepairDepot)
                    else if (b.Type == BuildingType.RepairDepot)
                    {
                        using (var bgBrush = new SolidBrush(Color.FromArgb(45, 0, 229, 255)))
                        using (var rimPen = new Pen(Color.FromArgb(0, 229, 255), 2f))
                        {
                            g.FillRoundedRectangle(bgBrush, rect, 4);
                            g.DrawRoundedRectangle(rimPen, rect, 4);
                        }
                        using (var font = new Font("Segoe UI", 9.5f, FontStyle.Bold))
                        {
                            TextRenderer.DrawText(g, "🏥", font, rect, Color.FromArgb(0, 229, 255),
                                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                        }
                    }
                    // 6. 가속 관문 (PhaseGate)
                    else if (b.Type == BuildingType.PhaseGate)
                    {
                        using (var bgBrush = new SolidBrush(Color.FromArgb(50, 186, 104, 200)))
                        using (var rimPen = new Pen(Color.FromArgb(206, 147, 216), 2f))
                        {
                            g.FillEllipse(bgBrush, rect);
                            g.DrawEllipse(rimPen, rect);
                        }
                        using (var font = new Font("Segoe UI", 9.5f, FontStyle.Bold))
                        {
                            TextRenderer.DrawText(g, "⏩", font, rect, Color.FromArgb(225, 190, 231),
                                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                        }
                    }

                    // 피격 플래시 (Hit Flash)
                    if (b.HitFlashAlpha > 0f)
                    {
                        int flashA = Math.Max(0, Math.Min(255, (int)(b.HitFlashAlpha * 180)));
                        using (var fBrush = new SolidBrush(Color.FromArgb(flashA, 255, 255, 255)))
                        {
                            g.FillRectangle(fBrush, rect);
                        }
                    }

                    // EMP 기능 정지 효과 렌더링
                    if (b.EmpTurns > 0)
                    {
                        using (var empBrush = new SolidBrush(Color.FromArgb(160, 10, 20, 35)))
                        using (var empPen = new Pen(_colorCyan, 1.8f) { DashStyle = DashStyle.Dash })
                        {
                            g.FillRoundedRectangle(empBrush, rect, 4);
                            g.DrawRoundedRectangle(empPen, rect, 4);
                        }
                        using (var font = new Font("Segoe UI", 7.5f, FontStyle.Bold))
                        {
                            TextRenderer.DrawText(g, $"⚡EMP {b.EmpTurns}T", font, rect, _colorCyan,
                                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                        }
                    }

                    // 체력 바
                    DrawMiniHpBar(g, rect.X, rect.Bottom - 4, rect.Width, 3, b.Hp, b.MaxHp, teamColor);
                }
            }
        }

        private void DrawUnits(Graphics g)
        {
            foreach (var u in _game.Units)
            {
                float colToDraw = (u.VisualCol >= 0) ? u.VisualCol : u.Col;
                int x = _boardRect.X + (int)(colToDraw * _tileW) + (int)u.BumpOffset + (int)u.ShakeOffset;
                int y = _boardRect.Y + u.Row * _tileH;
                var rect = new Rectangle(x + 4, y + 4, _tileW - 8, _tileH - 8);

                Color teamColor = (u.Owner == Owner.Player) ? _colorCyan : _colorCrimson;

                // 1. 보병 (Vanguard) - 원형 네온 드론
                if (u.Type == UnitType.Vanguard)
                {
                    // 전술 방패 결진 실드 효과 렌더링
                    if (u.HasShield)
                    {
                        using (var sBrush = new SolidBrush(Color.FromArgb(40, 64, 196, 255)))
                        using (var sPen = new Pen(Color.FromArgb(220, 64, 196, 255), 1.8f))
                        {
                            sPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                            var sRect = new Rectangle(rect.X - 3, rect.Y - 3, rect.Width + 6, rect.Height + 6);
                            g.FillEllipse(sBrush, sRect);
                            g.DrawEllipse(sPen, sRect);
                        }
                    }

                    using (var bBrush = new SolidBrush(Color.FromArgb(60, teamColor)))
                    using (var bPen = new Pen(teamColor, 2f))
                    {
                        g.FillEllipse(bBrush, rect);
                        g.DrawEllipse(bPen, rect);
                    }
                    using (var font = new Font("Segoe UI", 9f, FontStyle.Bold))
                    {
                        TextRenderer.DrawText(g, "●", font, rect, Color.White,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }
                }
                // 2. 침투병 (Raider) - 날렵한 세모 화살표 (은폐 시 반투명 아지랑이 & 유령 뱃지)
                else if (u.Type == UnitType.Raider)
                {
                    Point[] pts;
                    if (u.Owner == Owner.Player)
                    {
                        pts = new Point[] {
                            new Point(rect.Right, rect.Y + rect.Height / 2),
                            new Point(rect.Left, rect.Y),
                            new Point(rect.Left + 5, rect.Y + rect.Height / 2),
                            new Point(rect.Left, rect.Bottom)
                        };
                    }
                    else
                    {
                        pts = new Point[] {
                            new Point(rect.Left, rect.Y + rect.Height / 2),
                            new Point(rect.Right, rect.Y),
                            new Point(rect.Right - 5, rect.Y + rect.Height / 2),
                            new Point(rect.Right, rect.Bottom)
                        };
                    }

                    int raiderAlpha = u.IsCloaked ? 35 : 70;
                    float raiderPenW = u.IsCloaked ? 1.5f : 2f;
                    using (var bBrush = new SolidBrush(Color.FromArgb(raiderAlpha, teamColor)))
                    using (var bPen = new Pen(teamColor, raiderPenW))
                    {
                        if (u.IsCloaked) bPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
                        g.FillPolygon(bBrush, pts);
                        g.DrawPolygon(bPen, pts);
                    }

                    if (u.IsCloaked)
                    {
                        using (var ghostFont = new Font("Segoe UI", 7f, FontStyle.Bold))
                        {
                            TextRenderer.DrawText(g, "👻", ghostFont, new Rectangle(rect.X, rect.Y - 11, rect.Width, 12), Color.FromArgb(220, 255, 255, 255),
                                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                        }
                    }
                }
                // 3. 타이탄 (Titan) - 묵직한 이중 장갑차
                else if (u.Type == UnitType.Titan)
                {
                    using (var bBrush = new SolidBrush(Color.FromArgb(80, teamColor)))
                    using (var bPen = new Pen(teamColor, 2.5f))
                    using (var innerPen = new Pen(Color.FromArgb(255, 240, 200), 1.5f))
                    {
                        g.FillRoundedRectangle(bBrush, rect, 4);
                        g.DrawRoundedRectangle(bPen, rect, 4);
                        g.DrawRectangle(innerPen, rect.X + 4, rect.Y + 4, rect.Width - 8, rect.Height - 8);
                    }
                    using (var font = new Font("Segoe UI", 9f, FontStyle.Bold))
                    {
                        TextRenderer.DrawText(g, "■", font, rect, Color.White,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }
                }
                // 4. 공성 콜로서스 (Colossus) - 팔각 중포 거신
                else if (u.Type == UnitType.Colossus)
                {
                    Color colossusAccent = Color.FromArgb(220, 100, 255);
                    Point[] octPts = new Point[] {
                        new Point(rect.Left + 8, rect.Top),
                        new Point(rect.Right - 8, rect.Top),
                        new Point(rect.Right, rect.Top + 8),
                        new Point(rect.Right, rect.Bottom - 8),
                        new Point(rect.Right - 8, rect.Bottom),
                        new Point(rect.Left + 8, rect.Bottom),
                        new Point(rect.Left, rect.Bottom - 8),
                        new Point(rect.Left, rect.Top + 8)
                    };

                    using (var bBrush = new SolidBrush(Color.FromArgb(90, teamColor)))
                    using (var bPen = new Pen(teamColor, 2f))
                    using (var accentPen = new Pen(colossusAccent, 1.5f))
                    {
                        g.FillPolygon(bBrush, octPts);
                        g.DrawPolygon(bPen, octPts);
                        // 포탑 조준 링
                        g.DrawEllipse(accentPen, rect.X + 7, rect.Y + 7, rect.Width - 14, rect.Height - 14);
                    }
                    using (var font = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                    {
                        TextRenderer.DrawText(g, "⬢", font, rect, Color.White,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }
                }
                // 5. 플라즈마 드레드노트 (Dreadnought) - 다이아몬드형 중장갑 십자 방전 워커
                else if (u.Type == UnitType.Dreadnought)
                {
                    Color dreadAccent = Color.FromArgb(255, 190, 0);
                    int cx = rect.X + rect.Width / 2;
                    int cy = rect.Y + rect.Height / 2;
                    Point[] diamondPts = new Point[] {
                        new Point(cx, rect.Top),
                        new Point(rect.Right, cy),
                        new Point(cx, rect.Bottom),
                        new Point(rect.Left, cy)
                    };

                    using (var bBrush = new SolidBrush(Color.FromArgb(95, dreadAccent)))
                    using (var bPen = new Pen(teamColor, 2f))
                    using (var crossPen = new Pen(dreadAccent, 1.8f))
                    {
                        g.FillPolygon(bBrush, diamondPts);
                        g.DrawPolygon(bPen, diamondPts);
                        // 십자 발광 코어 라인
                        g.DrawLine(crossPen, cx - 7, cy, cx + 7, cy);
                        g.DrawLine(crossPen, cx, cy - 7, cx, cy + 7);
                    }
                    using (var font = new Font("Segoe UI", 9f, FontStyle.Bold))
                    {
                        TextRenderer.DrawText(g, "✦", font, rect, Color.White,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }
                }
                // 6. 천궁 (Cheongung) - 초장거리 대공성 미사일 포대
                else if (u.Type == UnitType.Cheongung)
                {
                    Color missileColor = (u.Owner == Owner.Player) ? Color.FromArgb(120, 230, 255) : Color.FromArgb(255, 120, 60);
                    int dir = (u.Owner == Owner.Player) ? 1 : -1;
                    int cx = rect.X + rect.Width / 2;
                    int cy = rect.Y + rect.Height / 2;

                    using (var bBrush = new SolidBrush(Color.FromArgb(95, teamColor)))
                    using (var bPen = new Pen(teamColor, 2f))
                    using (var missilePen = new Pen(missileColor, 2.2f))
                    {
                        g.FillRoundedRectangle(bBrush, rect, 4);
                        g.DrawRoundedRectangle(bPen, rect, 4);
                        // 전방 트윈 미사일 발사 레일
                        g.DrawLine(missilePen, cx - dir * 4, cy - 5, cx + dir * 12, cy - 5);
                        g.DrawLine(missilePen, cx - dir * 4, cy + 5, cx + dir * 12, cy + 5);
                    }
                    using (var font = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                    {
                        TextRenderer.DrawText(g, "🚀", font, rect, Color.White,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }
                }

                // 피격 플래시 (Hit Flash)
                if (u.HitFlashAlpha > 0f)
                {
                    int flashA = Math.Max(0, Math.Min(255, (int)(u.HitFlashAlpha * 200)));
                    using (var fBrush = new SolidBrush(Color.FromArgb(flashA, 255, 255, 255)))
                    {
                        g.FillEllipse(fBrush, rect);
                    }
                }

                // 상단 체력 도트 게이지
                DrawHpDots(g, rect.X, rect.Y - 2, rect.Width, u.Hp, u.MaxHp, teamColor);

                // 자폭 폭탄 장착 시 유닛 좌측 하단 💣 뱃지 렌더링
                if (u.HasBomb)
                {
                    var bombBadgeRect = new Rectangle(rect.Left + 1, rect.Bottom - 13, 14, 12);
                    using (var bBg = new SolidBrush(Color.FromArgb(230, 30, 10, 15)))
                    using (var bPen = new Pen(Color.FromArgb(255, 23, 68), 1.2f))
                    using (var bFont = new Font("Segoe UI", 6f, FontStyle.Bold))
                    {
                        g.FillRoundedRectangle(bBg, bombBadgeRect, 2);
                        g.DrawRoundedRectangle(bPen, bombBadgeRect, 2);
                        TextRenderer.DrawText(g, "💣", bFont, bombBadgeRect, Color.FromArgb(255, 60, 60),
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }
                }

                // 건물 중첩 시 타일 우하단에 건물 미니 뱃지 렌더링
                var underBldg = _game.Buildings[u.Row, u.Col];
                if (underBldg != null && underBldg.Hp > 0)
                {
                    string bIcon = underBldg.Type == BuildingType.PowerPlant ? "⚡" :
                                   underBldg.Type == BuildingType.Turret ? "🛡️" :
                                   underBldg.Type == BuildingType.Wall ? "🧱" :
                                   underBldg.Type == BuildingType.Factory ? "🏭" :
                                   underBldg.Type == BuildingType.RepairDepot ? "🏥" : "⏩";
                    Color bCol = underBldg.Owner == Owner.Player ? _colorGold : _colorCrimson;
                    var bBadgeRect = new Rectangle(rect.Right - 15, rect.Bottom - 13, 14, 12);
                    using (var bBg = new SolidBrush(Color.FromArgb(220, 10, 14, 22)))
                    using (var bPen = new Pen(bCol, 1.2f))
                    using (var bFont = new Font("Segoe UI", 6f, FontStyle.Bold))
                    {
                        g.FillRoundedRectangle(bBg, bBadgeRect, 2);
                        g.DrawRoundedRectangle(bPen, bBadgeRect, 2);
                        TextRenderer.DrawText(g, bIcon, bFont, bBadgeRect, bCol,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }
                }
            }
        }

        private void DrawLaserBeams(Graphics g)
        {
            foreach (var beam in _laserBeams)
            {
                int sx = _boardRect.X + beam.StartCol * _tileW + _tileW / 2;
                int sy = _boardRect.Y + beam.StartRow * _tileH + _tileH / 2;
                int ex = _boardRect.X + beam.EndCol * _tileW + _tileW / 2;
                int ey = _boardRect.Y + beam.EndRow * _tileH + _tileH / 2;

                int a = Math.Max(0, Math.Min(255, (int)(beam.Alpha * 255)));

                // 외곽 글로우
                using (var glowPen = new Pen(Color.FromArgb((int)(a * 0.4f), beam.BeamColor), 6f))
                {
                    g.DrawLine(glowPen, sx, sy, ex, ey);
                }
                // 중앙 고열 백색 코어
                using (var corePen = new Pen(Color.FromArgb(a, Color.White), 2f))
                {
                    g.DrawLine(corePen, sx, sy, ex, ey);
                }
            }
        }

        private void DrawMiniHpBar(Graphics g, int x, int y, int w, int h, int hp, int maxHp, Color color)
        {
            float ratio = Math.Max(0f, Math.Min(1f, (float)hp / maxHp));
            using (var bg = new SolidBrush(Color.FromArgb(50, 0, 0, 0)))
            using (var fill = new SolidBrush(color))
            {
                g.FillRectangle(bg, x, y, w, h);
                g.FillRectangle(fill, x, y, (int)(w * ratio), h);
            }
        }

        private void DrawHpDots(Graphics g, int x, int y, int w, int hp, int maxHp, Color color)
        {
            int dots = Math.Min(8, maxHp);
            int dotSize = 3;
            int totalW = dots * (dotSize + 2);
            int startX = x + (w - totalW) / 2;

            for (int i = 0; i < dots; i++)
            {
                bool active = i < hp;
                using (var brush = new SolidBrush(active ? color : Color.FromArgb(70, 70, 80)))
                {
                    g.FillEllipse(brush, startX + i * (dotSize + 2), y, dotSize, dotSize);
                }
            }
        }

        private void DrawPlacementGhost(Graphics g)
        {
            // 0. [최초 1턴 본진 발전소 무료 배치 모드]
            if (_game.IsInitialBasePlacementActive)
            {
                // (A) Col 1~2 배치 가능 구역 전체에 부드러운 골드 네온 점선 격자 하이라이트
                for (int r = 0; r < _game.Rows; r++)
                {
                    for (int c = 1; c <= 2; c++)
                    {
                        if (_game.MapTiles[r, c] != TileType.Obstacle &&
                            _game.Buildings[r, c] == null &&
                            !_game.Units.Exists(u => u.Col == c && u.Row == r))
                        {
                            int tx = _boardRect.X + c * _tileW;
                            int ty = _boardRect.Y + r * _tileH;
                            var tRect = new Rectangle(tx + 2, ty + 2, _tileW - 4, _tileH - 4);
                            using (var bBrush = new SolidBrush(Color.FromArgb(28, 255, 215, 0)))
                            using (var bPen = new Pen(Color.FromArgb(140, 255, 215, 0), 1.2f) { DashStyle = DashStyle.Dot })
                            {
                                g.FillRectangle(bBrush, tRect);
                                g.DrawRectangle(bPen, tRect);
                            }
                            using (var dotFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
                            {
                                TextRenderer.DrawText(g, "⚡", dotFont, tRect, Color.FromArgb(180, 255, 215, 0),
                                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                            }
                        }
                    }
                }

                // (B) 마우스 호버 타일에 배치될 발전소의 네온 고스트 실루엣
                if (_hoverTile.X >= 1 && _hoverTile.X <= 2 && _hoverTile.Y >= 0 && _hoverTile.Y < _game.Rows)
                {
                    int hc = _hoverTile.X;
                    int hr = _hoverTile.Y;
                    bool isValid = (_game.MapTiles[hr, hc] != TileType.Obstacle &&
                                    _game.Buildings[hr, hc] == null &&
                                    !_game.Units.Exists(u => u.Col == hc && u.Row == hr));

                    Color ghostCol = isValid ? _colorGold : Color.FromArgb(200, 255, 60, 60);
                    int hx = _boardRect.X + hc * _tileW;
                    int hy = _boardRect.Y + hr * _tileH;
                    var hRect = new Rectangle(hx + 3, hy + 3, _tileW - 6, _tileH - 6);

                    using (var gBrush = new SolidBrush(Color.FromArgb(50, ghostCol)))
                    using (var gPen = new Pen(ghostCol, 2f))
                    {
                        g.FillRoundedRectangle(gBrush, hRect, 4);
                        g.DrawRoundedRectangle(gPen, hRect, 4);
                    }

                    using (var font = new Font("Segoe UI", 9f, FontStyle.Bold))
                    {
                        TextRenderer.DrawText(g, "⚡발전소", font, hRect, ghostCol,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }
                }

                // (C) 전장 상단 안내 배너 (가변 맵 최적화 및 텍스트 잘림 방지)
                int bannerW = Math.Max(460, Math.Min(_boardRect.Width - 10, 560));
                int bannerH = 26;
                int bx = _boardRect.X + (_boardRect.Width - bannerW) / 2;
                int by = _boardRect.Y + 6;
                var bannerRect = new Rectangle(bx, by, bannerW, bannerH);
                using (var bbBrush = new SolidBrush(Color.FromArgb(220, 15, 25, 40)))
                using (var bbPen = new Pen(_colorGold, 1.5f))
                {
                    g.FillRoundedRectangle(bbBrush, bannerRect, 4);
                    g.DrawRoundedRectangle(bbPen, bannerRect, 4);
                }
                using (var bFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                {
                    string msg = (_game.InitialBaseTotalCount > 1)
                        ? $"⚡ [초기 기지 설립 {_game.InitialBaseTotalCount - _game.InitialBaseRemainingCount + 1}/{_game.InitialBaseTotalCount}] 1~2열을 클릭하여 발전소를 무료 건설하세요"
                        : "⚡ [초기 기지 설립] 1~2열을 클릭하여 첫 발전소를 무료 건설하세요";
                    TextRenderer.DrawText(g, msg, bFont, bannerRect, _colorGold,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
                return;
            }

            // 1. [프로토콜 카드 전술 투하/배치 지정 모드]
            if (_game.PendingDropRemainingCount > 0 && _game.PendingDropUnitType != UnitType.None)
            {
                // (A) 배치 가능 구역 전체에 부드러운 네온 점선 격자 하이라이트 표시
                for (int r = 0; r < _game.Rows; r++)
                {
                    for (int c = _game.PendingDropMinCol; c <= _game.PendingDropMaxCol; c++)
                    {
                        if (_game.MapTiles[r, c] != TileType.Obstacle &&
                            _game.Buildings[r, c]?.Type != BuildingType.Wall &&
                            !_game.Units.Exists(u => u.Col == c && u.Row == r))
                        {
                            int tx = _boardRect.X + c * _tileW;
                            int ty = _boardRect.Y + r * _tileH;
                            var tRect = new Rectangle(tx + 2, ty + 2, _tileW - 4, _tileH - 4);
                            using (var bBrush = new SolidBrush(Color.FromArgb(28, 0, 229, 255)))
                            using (var bPen = new Pen(Color.FromArgb(140, 0, 229, 255), 1.2f) { DashStyle = DashStyle.Dot })
                            {
                                g.FillRectangle(bBrush, tRect);
                                g.DrawRectangle(bPen, tRect);
                            }
                            using (var dotFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
                            {
                                TextRenderer.DrawText(g, "✦", dotFont, tRect, Color.FromArgb(180, 0, 229, 255),
                                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                            }
                        }
                    }
                }

                // (B) 마우스 호버 타일에 배치될 유닛의 네온 고스트 실루엣 렌더링
                if (_hoverTile.X >= _game.PendingDropMinCol && _hoverTile.X <= _game.PendingDropMaxCol &&
                    _hoverTile.Y >= 0 && _hoverTile.Y < _game.Rows)
                {
                    int hc = _hoverTile.X;
                    int hr = _hoverTile.Y;
                    bool isValid = (_game.MapTiles[hr, hc] != TileType.Obstacle &&
                                    _game.Buildings[hr, hc]?.Type != BuildingType.Wall &&
                                    !_game.Units.Exists(u => u.Col == hc && u.Row == hr));

                    Color ghostCol = isValid ? Color.FromArgb(200, 0, 229, 255) : Color.FromArgb(200, 255, 60, 60);
                    int hx = _boardRect.X + hc * _tileW;
                    int hy = _boardRect.Y + hr * _tileH;
                    var hRect = new Rectangle(hx + 3, hy + 3, _tileW - 6, _tileH - 6);

                    using (var gBrush = new SolidBrush(Color.FromArgb(50, ghostCol)))
                    using (var gPen = new Pen(ghostCol, 2f))
                    {
                        g.FillRoundedRectangle(gBrush, hRect, 4);
                        g.DrawRoundedRectangle(gPen, hRect, 4);
                    }

                    string icon = (_game.PendingDropUnitType == UnitType.Vanguard) ? "●" :
                                  (_game.PendingDropUnitType == UnitType.Raider) ? "▲" : "■";
                    using (var font = new Font("Segoe UI", 11f, FontStyle.Bold))
                    {
                        TextRenderer.DrawText(g, icon, font, hRect, ghostCol,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }
                }

                // (C) 전장 상단 안내 배너 (클릭 안내)
                int bannerW = 340;
                int bannerH = 24;
                int bx = _boardRect.X + (_boardRect.Width - bannerW) / 2;
                int by = _boardRect.Y + 6;
                var bannerRect = new Rectangle(bx, by, bannerW, bannerH);
                using (var bbBrush = new SolidBrush(Color.FromArgb(220, 15, 25, 40)))
                using (var bbPen = new Pen(_colorCyan, 1.5f))
                {
                    g.FillRoundedRectangle(bbBrush, bannerRect, 4);
                    g.DrawRoundedRectangle(bbPen, bannerRect, 4);
                }
                using (var bFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                {
                    string msg = $"🚀 [전술 투하] 타일을 클릭하세요! (남은 유닛: {_game.PendingDropRemainingCount}기)";
                    TextRenderer.DrawText(g, msg, bFont, bannerRect, _colorGold,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
                return;
            }

            // 2. [유저 액션 스킬 타깃팅 모드]
            if (_selectedSkill != UserSkillType.None)
            {
                if (_selectedSkill == UserSkillType.OrbitalStrike)
                {
                    int bannerW = 380;
                    int bannerH = 24;
                    int bx = _boardRect.X + (_boardRect.Width - bannerW) / 2;
                    int by = _boardRect.Y + 6;
                    var bannerRect = new Rectangle(bx, by, bannerW, bannerH);
                    using (var bbBrush = new SolidBrush(Color.FromArgb(220, 25, 15, 20)))
                    using (var bbPen = new Pen(_colorCrimson, 1.5f))
                    {
                        g.FillRoundedRectangle(bbBrush, bannerRect, 4);
                        g.DrawRoundedRectangle(bbPen, bannerRect, 4);
                    }
                    using (var bFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                    {
                        TextRenderer.DrawText(g, "🎯 [정밀 궤도 폭격 (8G)] 타깃 적 건물 또는 서버 랙을 클릭하세요 (6 피해)", bFont, bannerRect, _colorCrimson,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }

                    if (_hoverTile.X >= 1 && _hoverTile.X < _game.Cols - 1 && _hoverTile.Y >= 0 && _hoverTile.Y < _game.Rows)
                    {
                        int hx = _boardRect.X + _hoverTile.X * _tileW;
                        int hy = _boardRect.Y + _hoverTile.Y * _tileH;
                        var tRect = new Rectangle(hx + 2, hy + 2, _tileW - 4, _tileH - 4);
                        var targetBldg = _game.Buildings[_hoverTile.Y, _hoverTile.X];
                        bool isAiBldg = targetBldg?.Owner == Owner.Ai;
                        bool isObstacle = _game.MapTiles[_hoverTile.Y, _hoverTile.X] == TileType.Obstacle;
                        bool isTarget = isAiBldg || isObstacle;
                        Color cColor = isTarget ? _colorCrimson : Color.FromArgb(120, 100, 100, 100);

                        using (var brush = new SolidBrush(Color.FromArgb(50, cColor)))
                        using (var pen = new Pen(cColor, 2f))
                        {
                            g.FillRectangle(brush, tRect);
                            g.DrawRectangle(pen, tRect);
                            g.DrawLine(pen, tRect.X, tRect.Y + tRect.Height / 2, tRect.Right, tRect.Y + tRect.Height / 2);
                            g.DrawLine(pen, tRect.X + tRect.Width / 2, tRect.Y, tRect.X + tRect.Width / 2, tRect.Bottom);
                        }

                        // 타깃 기물 명칭 및 체력 실시간 플로팅 뱃지 (유닛 중첩 시 함께 표시)
                        if (isTarget)
                        {
                            string targetName = isObstacle ? $"서버랙 (HP {_game.ObstacleHp[_hoverTile.Y, _hoverTile.X]})" :
                                                targetBldg.Type == BuildingType.PowerPlant ? $"적 발전소 (HP {targetBldg.Hp})" :
                                                targetBldg.Type == BuildingType.Turret ? $"적 포탑 (HP {targetBldg.Hp})" :
                                                targetBldg.Type == BuildingType.Wall ? $"적 방화벽 (HP {targetBldg.Hp})" : $"적 공장 (HP {targetBldg.Hp})";

                            var onUnit = _game.Units.Find(u => u.Col == _hoverTile.X && u.Row == _hoverTile.Y && u.Hp > 0);
                            if (onUnit != null) targetName += $" + {onUnit.Type}";

                            int tipW = Math.Max(120, targetName.Length * 9 + 16);
                            int tipH = 20;
                            var tipRect = new Rectangle(tRect.X + (tRect.Width - tipW) / 2, tRect.Y - tipH - 3, tipW, tipH);
                            using (var tipBrush = new SolidBrush(Color.FromArgb(230, 20, 10, 15)))
                            using (var tipPen = new Pen(_colorCrimson, 1.2f))
                            using (var tipFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
                            {
                                g.FillRoundedRectangle(tipBrush, tipRect, 3);
                                g.DrawRoundedRectangle(tipPen, tipRect, 3);
                                TextRenderer.DrawText(g, targetName, tipFont, tipRect, Color.White,
                                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                            }
                        }
                    }
                    return;
                }
                else if (_selectedSkill == UserSkillType.EmpJammer)
                {
                    int bannerW = 380;
                    int bannerH = 24;
                    int bx = _boardRect.X + (_boardRect.Width - bannerW) / 2;
                    int by = _boardRect.Y + 6;
                    var bannerRect = new Rectangle(bx, by, bannerW, bannerH);
                    using (var bbBrush = new SolidBrush(Color.FromArgb(220, 15, 25, 40)))
                    using (var bbPen = new Pen(_colorCyan, 1.5f))
                    {
                        g.FillRoundedRectangle(bbBrush, bannerRect, 4);
                        g.DrawRoundedRectangle(bbPen, bannerRect, 4);
                    }
                    using (var bFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                    {
                        TextRenderer.DrawText(g, "⚡ [전자기 EMP 교란 (5G)] 목표 타일을 클릭하세요 (3x3 범위 2턴간 기능 정지)", bFont, bannerRect, _colorCyan,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }

                    if (_hoverTile.X >= 1 && _hoverTile.X < _game.Cols - 1 && _hoverTile.Y >= 0 && _hoverTile.Y < _game.Rows)
                    {
                        for (int dr = -1; dr <= 1; dr++)
                        {
                            for (int dc = -1; dc <= 1; dc++)
                            {
                                int tc = _hoverTile.X + dc;
                                int tr = _hoverTile.Y + dr;
                                if (tc >= 1 && tc < _game.Cols - 1 && tr >= 0 && tr < _game.Rows)
                                {
                                    int tx = _boardRect.X + tc * _tileW;
                                    int ty = _boardRect.Y + tr * _tileH;
                                    var tRect = new Rectangle(tx + 2, ty + 2, _tileW - 4, _tileH - 4);
                                    using (var brush = new SolidBrush(Color.FromArgb(35, 0, 229, 255)))
                                    using (var pen = new Pen(_colorCyan, 1.2f) { DashStyle = DashStyle.Dot })
                                    {
                                        g.FillRectangle(brush, tRect);
                                        g.DrawRectangle(pen, tRect);
                                    }
                                }
                            }
                        }
                    }
                    return;
                }
                else if (_selectedSkill == UserSkillType.HealingBeam)
                {
                    int bannerW = 480;
                    int bannerH = 24;
                    int bx = _boardRect.X + (_boardRect.Width - bannerW) / 2;
                    int by = _boardRect.Y + 6;
                    var bannerRect = new Rectangle(bx, by, bannerW, bannerH);
                    using (var bbBrush = new SolidBrush(Color.FromArgb(220, 10, 30, 20)))
                    using (var bbPen = new Pen(Color.FromArgb(0, 230, 118), 1.5f))
                    {
                        g.FillRoundedRectangle(bbBrush, bannerRect, 4);
                        g.DrawRoundedRectangle(bbPen, bannerRect, 4);
                    }
                    using (var bFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                    {
                        TextRenderer.DrawText(g, "💚 [전술 치료 광선 (4G)] 치유할 행(Row)을 클릭하세요 (해당 행 모든 아군 유닛+건물 HP +6 수복)", bFont, bannerRect, Color.FromArgb(0, 230, 118),
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }

                    if (_hoverTile.Y >= 0 && _hoverTile.Y < _game.Rows)
                    {
                        int hy = _boardRect.Y + _hoverTile.Y * _tileH;
                        var rowRect = new Rectangle(_boardRect.X + _tileW, hy + 2, _boardRect.Width - _tileW * 2, _tileH - 4);
                        using (var brush = new SolidBrush(Color.FromArgb(35, 0, 230, 118)))
                        using (var pen = new Pen(Color.FromArgb(0, 230, 118), 1.8f) { DashStyle = DashStyle.Dash })
                        {
                            g.FillRectangle(brush, rowRect);
                            g.DrawRectangle(pen, rowRect);
                        }

                        int friendlyCount = _game.Units.FindAll(u => u.Owner == Owner.Player && u.Row == _hoverTile.Y && u.Hp > 0).Count;
                        int friendlyBldgCount = 0;
                        for (int c = 0; c < _game.Cols; c++)
                        {
                            var b = _game.Buildings[_hoverTile.Y, c];
                            if (b != null && b.Owner == Owner.Player && b.Hp > 0) friendlyBldgCount++;
                        }
                        string tip = $"💚 수복 대상: Row {_hoverTile.Y + 1} (유닛 {friendlyCount}기 + 건물 {friendlyBldgCount}채)";
                        int tipW = Math.Max(180, tip.Length * 9);
                        var tipRect = new Rectangle(_boardRect.X + (_boardRect.Width - tipW) / 2, hy + (_tileH - 22) / 2, tipW, 22);
                        using (var tipBg = new SolidBrush(Color.FromArgb(230, 10, 30, 20)))
                        using (var tipPen = new Pen(Color.FromArgb(0, 230, 118), 1.2f))
                        using (var tipFont = new Font("Segoe UI", 8f, FontStyle.Bold))
                        {
                            g.FillRoundedRectangle(tipBg, tipRect, 3);
                            g.DrawRoundedRectangle(tipPen, tipRect, 3);
                            TextRenderer.DrawText(g, tip, tipFont, tipRect, Color.White,
                                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                        }
                    }
                    return;
                }
                else if (_selectedSkill == UserSkillType.FortifyBeam)
                {
                    int bannerW = 460;
                    int bannerH = 24;
                    int bx = _boardRect.X + (_boardRect.Width - bannerW) / 2;
                    int by = _boardRect.Y + 6;
                    var bannerRect = new Rectangle(bx, by, bannerW, bannerH);
                    using (var bbBrush = new SolidBrush(Color.FromArgb(220, 10, 25, 35)))
                    using (var bbPen = new Pen(Color.FromArgb(0, 229, 255), 1.5f))
                    {
                        g.FillRoundedRectangle(bbBrush, bannerRect, 4);
                        g.DrawRoundedRectangle(bbPen, bannerRect, 4);
                    }
                    using (var bFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                    {
                        TextRenderer.DrawText(g, "🛡️ [전술 구축 광선 (5G)] 강화할 행(Row)을 클릭하세요 (해당 행 아군 최대HP+2 & 현재HP+2)", bFont, bannerRect, Color.FromArgb(0, 229, 255),
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }

                    if (_hoverTile.Y >= 0 && _hoverTile.Y < _game.Rows)
                    {
                        int hy = _boardRect.Y + _hoverTile.Y * _tileH;
                        var rowRect = new Rectangle(_boardRect.X + _tileW, hy + 2, _boardRect.Width - _tileW * 2, _tileH - 4);
                        using (var brush = new SolidBrush(Color.FromArgb(35, 0, 229, 255)))
                        using (var pen = new Pen(Color.FromArgb(0, 229, 255), 1.8f) { DashStyle = DashStyle.Dash })
                        {
                            g.FillRectangle(brush, rowRect);
                            g.DrawRectangle(pen, rowRect);
                        }

                        int friendlyCount = _game.Units.FindAll(u => u.Owner == Owner.Player && u.Row == _hoverTile.Y && u.Hp > 0).Count;
                        string tip = $"🛡️ 구축 대상: Row {_hoverTile.Y + 1} (아군 유닛 {friendlyCount}기 최대HP/현재HP +2)";
                        int tipW = Math.Max(200, tip.Length * 9);
                        var tipRect = new Rectangle(_boardRect.X + (_boardRect.Width - tipW) / 2, hy + (_tileH - 22) / 2, tipW, 22);
                        using (var tipBg = new SolidBrush(Color.FromArgb(230, 10, 25, 35)))
                        using (var tipPen = new Pen(Color.FromArgb(0, 229, 255), 1.2f))
                        using (var tipFont = new Font("Segoe UI", 8f, FontStyle.Bold))
                        {
                            g.FillRoundedRectangle(tipBg, tipRect, 3);
                            g.DrawRoundedRectangle(tipPen, tipRect, 3);
                            TextRenderer.DrawText(g, tip, tipFont, tipRect, Color.White,
                                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                        }
                    }
                    return;
                }
            }

            // 3. 일반 건물/유닛 배치 모드
            if (_hoverTile.X < 1 || _hoverTile.X > _game.Cols - 2) return;
            if (_selectedBuilding == BuildingType.None && _selectedUnit == UnitType.None) return;

            int c2 = _hoverTile.X;
            int r2 = _hoverTile.Y;
            int x2 = _boardRect.X + c2 * _tileW;
            int y2 = _boardRect.Y + r2 * _tileH;
            var rect2 = new Rectangle(x2, y2, _tileW, _tileH);

            bool isValidNormal = false;
            if (_selectedBuilding != BuildingType.None)
                isValidNormal = _game.IsValidBuildingPlacement(c2, r2, Owner.Player);
            else if (_selectedUnit != UnitType.None)
                isValidNormal = _game.IsValidUnitSpawnPlacement(c2, r2, Owner.Player);

            Color ghostColor2 = isValidNormal ? Color.FromArgb(120, 0, 230, 118) : Color.FromArgb(120, 255, 23, 68);

            using (var brush = new SolidBrush(Color.FromArgb(40, ghostColor2)))
            using (var pen = new Pen(ghostColor2, 2f) { DashStyle = DashStyle.Dot })
            {
                g.FillRectangle(brush, rect2);
                g.DrawRectangle(pen, rect2.X + 2, rect2.Y + 2, rect2.Width - 4, rect2.Height - 4);
            }
        }

        #region Range Indicators & Inspector Tooltip

        private void DrawRangeIndicators(Graphics g)
        {
            if (_game.IsCardSelectionActive || _showGuide || _game.IsGameOver || _isTurnRunning) return;

            // 1. [배치 모드] 포탑 또는 타이탄 선택 상태에서 마우스 호버 시 실시간 사거리 미리보기
            if (_hoverTile.X >= 1 && _hoverTile.X < _game.Cols - 1 && _hoverTile.Y >= 0 && _hoverTile.Y < _game.Rows)
            {
                if (_selectedBuilding == BuildingType.Turret)
                {
                    int bonus = _game.TurretRangeBonus;
                    bool isValid = _game.IsValidBuildingPlacement(_hoverTile.X, _hoverTile.Y, Owner.Player);
                    Color rangeColor = isValid ? _colorEmerald : Color.FromArgb(180, 255, 23, 68);
                    DrawTurretOmniRange(g, _hoverTile.X, _hoverTile.Y, bonus, rangeColor, "🛡️ 포탑 배치 사거리 (13칸 전방위 요격)");
                    return;
                }
                else if (_selectedUnit == UnitType.Titan)
                {
                    bool isValid = _game.IsValidUnitSpawnPlacement(_hoverTile.X, _hoverTile.Y, Owner.Player);
                    Color rangeColor = isValid ? _colorCyan : Color.FromArgb(180, 255, 23, 68);
                    DrawRangeCircleAndTiles(g, _hoverTile.X, _hoverTile.Y, 2, 1, rangeColor, "■ 타이탄 스폰 사거리 (2칸 공성 포격)", 1);
                    return;
                }
                else if (_selectedUnit == UnitType.Colossus)
                {
                    bool isValid = _game.IsValidUnitSpawnPlacement(_hoverTile.X, _hoverTile.Y, Owner.Player);
                    Color rangeColor = isValid ? Color.FromArgb(220, 100, 255) : Color.FromArgb(180, 255, 23, 68);
                    DrawRangeCircleAndTiles(g, _hoverTile.X, _hoverTile.Y, 3, 1, rangeColor, "⬢ 콜로서스 스폰 사거리 (2~3칸 포격, 1칸 근접불가)", 2);
                    return;
                }
                else if (_selectedUnit == UnitType.Dreadnought)
                {
                    bool isValid = _game.IsValidUnitSpawnPlacement(_hoverTile.X, _hoverTile.Y, Owner.Player);
                    Color rangeColor = isValid ? Color.FromArgb(255, 190, 0) : Color.FromArgb(180, 255, 23, 68);
                    DrawRangeCircleAndTiles(g, _hoverTile.X, _hoverTile.Y, 2, 1, rangeColor, "✦ 드레드노트 스폰 사거리 (2칸 십자 스플래시 방전)", 1);
                    return;
                }
                else if (_selectedUnit == UnitType.Cheongung)
                {
                    bool isValid = _game.IsValidUnitSpawnPlacement(_hoverTile.X, _hoverTile.Y, Owner.Player);
                    Color rangeColor = isValid ? Color.FromArgb(255, 120, 50) : Color.FromArgb(180, 255, 23, 68);
                    int rng = _game.PlayerHasCheongungTrajectory ? 5 : 4;
                    int minRng = _game.PlayerHasCheongungTrajectory ? 3 : 2;
                    DrawRangeCircleAndTiles(g, _hoverTile.X, _hoverTile.Y, rng, 1, rangeColor, $"🚀 천궁 스폰 사거리 ({minRng}~{rng}칸 대공/대지 미사일 포대)", minRng);
                    return;
                }
            }

            // 2. [탐색 모드] 보드 위의 유닛 또는 건물에 마우스 호버 시 사거리 링 표시
            if (_hoverTile.X >= 1 && _hoverTile.X < _game.Cols - 1 && _hoverTile.Y >= 0 && _hoverTile.Y < _game.Rows)
            {
                int hx = _hoverTile.X;
                int hy = _hoverTile.Y;

                // (A) 유닛 사거리 확인
                var hoverUnit = _game.Units.Find(u => u.Col == hx && u.Row == hy && u.Hp > 0);
                if (hoverUnit != null)
                {
                    Color uColor = (hoverUnit.Owner == Owner.Player) ? _colorCyan : _colorCrimson;
                    if (hoverUnit.Type == UnitType.Colossus) uColor = Color.FromArgb(220, 100, 255);
                    else if (hoverUnit.Type == UnitType.Dreadnought) uColor = Color.FromArgb(255, 190, 0);
                    else if (hoverUnit.Type == UnitType.Cheongung) uColor = Color.FromArgb(255, 120, 50);
                    int dir = (hoverUnit.Owner == Owner.Player) ? 1 : -1;
                    string teamTag = (hoverUnit.Owner == Owner.Player) ? "아군" : "적군";
                    string label = (hoverUnit.Type == UnitType.Dreadnought)
                        ? $"✦ {teamTag} 드레드노트 사거리 (2칸 십자방전, 소형유닛 취약)"
                        : (hoverUnit.Type == UnitType.Colossus)
                        ? $"⬢ {teamTag} 콜로서스 사거리 (2~3칸 포격, 1칸 근접불가)"
                        : (hoverUnit.Type == UnitType.Cheongung)
                        ? $"🚀 {teamTag} 천궁 미사일 포대 ({hoverUnit.MinRange}~{hoverUnit.Range}칸 사거리, 건물/랙 2배 피해)"
                        : (hoverUnit.Type == UnitType.Titan)
                        ? $"■ {teamTag} 타이탄 사거리 ({hoverUnit.Range}칸 무반격)"
                        : $"{((hoverUnit.Type == UnitType.Raider) ? "▲ 침투병" : "● 보병")} 교전 사거리 (1칸)";

                    DrawRangeCircleAndTiles(g, hoverUnit.Col, hoverUnit.Row, hoverUnit.Range, dir, uColor, label, hoverUnit.MinRange);
                    return;
                }

                // (B) 건물 사거리 확인 (포탑 - 13칸 전방위 수호 요새)
                var hoverBldg = _game.Buildings[hy, hx];
                if (hoverBldg != null && hoverBldg.Type == BuildingType.Turret && hoverBldg.Hp > 0)
                {
                    int bonus = (hoverBldg.Owner == Owner.Player) ? _game.TurretRangeBonus : 0;
                    Color bColor = (hoverBldg.Owner == Owner.Player) ? _colorEmerald : _colorCrimson;
                    string teamTag = (hoverBldg.Owner == Owner.Player) ? "아군" : "적군";
                    DrawTurretOmniRange(g, hoverBldg.Col, hoverBldg.Row, bonus, bColor, $"🛡️ {teamTag} 포탑 수호 구역 (13칸 전방위)");
                    return;
                }
            }
        }

        private void DrawTurretOmniRange(Graphics g, int col, int row, int rangeBonus, Color color, string label)
        {
            float cx = _boardRect.X + col * _tileW + _tileW / 2f;
            float cy = _boardRect.Y + row * _tileH + _tileH / 2f;
            float pulse = (float)Math.Sin(_pulseAngle) * 2.5f;
            float radius = (2.2f + rangeBonus * 0.8f) * _tileW + pulse;

            // 1. 네온 펄스 사거리 원 (Omni Range Circle)
            using (var bgBrush = new SolidBrush(Color.FromArgb(16, color)))
            using (var rimPen = new Pen(Color.FromArgb(160, color), 1.8f) { DashStyle = DashStyle.Dash })
            {
                g.FillEllipse(bgBrush, cx - radius, cy - radius, radius * 2, radius * 2);
                g.DrawEllipse(rimPen, cx - radius, cy - radius, radius * 2, radius * 2);
            }

            // 2. 13칸 전방위 유효 타일 강조
            for (int r = 0; r < _game.Rows; r++)
            {
                for (int c = 1; c < _game.Cols - 1; c++)
                {
                    if (r == row && c == col) continue;
                    if (!CoreFrontGameState.IsTileInTurretRange(row, col, r, c, rangeBonus)) continue;

                    int tx = _boardRect.X + c * _tileW;
                    int ty = _boardRect.Y + r * _tileH;
                    var tRect = new Rectangle(tx + 2, ty + 2, _tileW - 4, _tileH - 4);

                    using (var tileBrush = new SolidBrush(Color.FromArgb(35, color)))
                    using (var tilePen = new Pen(color, 1.2f))
                    {
                        g.FillRoundedRectangle(tileBrush, tRect, 4);
                        g.DrawRoundedRectangle(tilePen, tRect, 4);
                    }

                    using (var font = new Font("Segoe UI", 7.5f, FontStyle.Bold))
                    {
                        TextRenderer.DrawText(g, "🎯", font, tRect, color,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }
                }
            }

            // 3. 사거리 상단 네온 뱃지 라벨
            using (var font = new Font("Segoe UI", 8f, FontStyle.Bold))
            {
                Size sz = TextRenderer.MeasureText(label, font);
                int bw = sz.Width + 14;
                int bh = 20;
                int bx = (int)cx - bw / 2;
                int by = (int)(cy - radius - 16);
                if (by < _boardRect.Y + 4) by = (int)(cy + radius + 4);

                var badgeRect = new Rectangle(bx, by, bw, bh);
                using (var bBrush = new SolidBrush(Color.FromArgb(220, 14, 20, 30)))
                using (var bPen = new Pen(color, 1.2f))
                {
                    g.FillRoundedRectangle(bBrush, badgeRect, 4);
                    g.DrawRoundedRectangle(bPen, badgeRect, 4);
                }
                TextRenderer.DrawText(g, label, font, badgeRect, color,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private void DrawRangeCircleAndTiles(Graphics g, int col, int row, int range, int dir, Color color, string label, int minRange = 1)
        {
            float cx = _boardRect.X + col * _tileW + _tileW / 2f;
            float cy = _boardRect.Y + row * _tileH + _tileH / 2f;
            float pulse = (float)Math.Sin(_pulseAngle) * 2.5f;
            float radius = range * _tileW + 10f + pulse;

            // 1. 네온 펄스 사거리 원 (Range Circle)
            using (var bgBrush = new SolidBrush(Color.FromArgb(18, color)))
            using (var rimPen = new Pen(Color.FromArgb(160, color), 1.8f) { DashStyle = DashStyle.Dash })
            {
                g.FillEllipse(bgBrush, cx - radius, cy - radius, radius * 2, radius * 2);
                g.DrawEllipse(rimPen, cx - radius, cy - radius, radius * 2, radius * 2);
            }

            // 2. 전방 사격 직선 타일 강조 (Target Lane Highlights)
            for (int dist = 1; dist <= range; dist++)
            {
                int tc = col + dist * dir;
                if (tc >= 1 && tc < _game.Cols - 1)
                {
                    int tx = _boardRect.X + tc * _tileW;
                    int ty = _boardRect.Y + row * _tileH;
                    var tRect = new Rectangle(tx + 2, ty + 2, _tileW - 4, _tileH - 4);

                    bool isBlindSpot = dist < minRange;
                    Color tileColor = isBlindSpot ? Color.FromArgb(255, 60, 60) : color;

                    using (var tileBrush = new SolidBrush(Color.FromArgb(isBlindSpot ? 30 : 45, tileColor)))
                    using (var tilePen = new Pen(tileColor, 1.4f) { DashStyle = isBlindSpot ? DashStyle.Dot : DashStyle.Solid })
                    {
                        g.FillRoundedRectangle(tileBrush, tRect, 4);
                        g.DrawRoundedRectangle(tilePen, tRect, 4);
                    }

                    using (var font = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                    {
                        string mark = isBlindSpot ? "🛑사각" : $"🎯{dist}";
                        TextRenderer.DrawText(g, mark, font, tRect, tileColor,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }
                }
            }

            // 3. 사거리 상단 네온 뱃지 라벨
            using (var font = new Font("Segoe UI", 8f, FontStyle.Bold))
            {
                Size sz = TextRenderer.MeasureText(label, font);
                int bw = sz.Width + 14;
                int bh = 20;
                int bx = (int)cx - bw / 2;
                int by = (int)(cy - radius - 16);
                if (by < _boardRect.Y + 4) by = (int)(cy + radius + 4);

                var badgeRect = new Rectangle(bx, by, bw, bh);
                using (var bBrush = new SolidBrush(Color.FromArgb(220, 14, 20, 30)))
                using (var bPen = new Pen(color, 1.2f))
                {
                    g.FillRoundedRectangle(bBrush, badgeRect, 4);
                    g.DrawRoundedRectangle(bPen, badgeRect, 4);
                }
                TextRenderer.DrawText(g, label, font, badgeRect, color,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        #endregion

        private void DrawEffects(Graphics g)
        {
            foreach (var sw in _shockwaves)
            {
                int alpha = Math.Max(0, Math.Min(255, (int)(sw.Alpha * 255)));
                using (var pen = new Pen(Color.FromArgb(alpha, sw.WaveColor), 2f))
                {
                    g.DrawEllipse(pen, sw.X - sw.Radius, sw.Y - sw.Radius, sw.Radius * 2, sw.Radius * 2);
                }
            }

            using (var font = new Font("Segoe UI", 10.5f, FontStyle.Bold))
            {
                foreach (var ft in _floatingTexts)
                {
                    int alpha = Math.Max(0, Math.Min(255, (int)(ft.Alpha * 255)));
                    using (var brush = new SolidBrush(Color.FromArgb(alpha, ft.TextColor)))
                    {
                        g.DrawString(ft.Text, font, brush, ft.X, ft.Y);
                    }
                }
            }
        }

        private void DrawBottomBar(Graphics g)
        {
            // 1. 카테고리 탭 3종 (인프라 / 작전유닛 / 전술스킬)
            DrawCategoryTab(g, _tabBuildingsRect, "🏗️ 인프라 (B)", _currentCategory == BottomCategory.Buildings, 101, _colorGold);
            DrawCategoryTab(g, _tabUnitsRect, "⚔️ 작전유닛 (U)", _currentCategory == BottomCategory.Units, 102, _colorCyan);
            DrawCategoryTab(g, _tabSkillsRect, "⚡ 전술스킬 (S)", _currentCategory == BottomCategory.Skills, 103, Color.FromArgb(255, 100, 200));

            // 2. 카테고리별 5대 슬롯 렌더링
            if (_currentCategory == BottomCategory.Buildings)
            {
                int plantCost = _game.GetBuildingCost(BuildingType.PowerPlant);
                int plantCount = _game.GetBuildingCount(BuildingType.PowerPlant, Owner.Player);
                int plantLimit = _game.GetPlantLimit(Owner.Player);
                bool isPlantLimitReached = plantCount >= plantLimit;
                bool canAffordPlant = _game.PlayerCredit >= plantCost && !isPlantLimitReached;

                string plantTitle = isPlantLimitReached ? "⚡ 발전소(초과)" : "⚡ 발전소";
                string plantSub = isPlantLimitReached ? $"한도({plantCount}/{plantLimit})" : $"{plantCost}G (+1G/턴)";
                Color plantColor = isPlantLimitReached ? Color.FromArgb(255, 75, 75) : _colorGold;

                DrawActionCard(g, _slotRects[0], plantTitle, plantSub, "[1]",
                    _selectedBuilding == BuildingType.PowerPlant, canAffordPlant, 1, plantColor);

                int turretCost = _game.GetBuildingCost(BuildingType.Turret, Owner.Player);
                DrawActionCard(g, _slotRects[1], "🛡️ 포탑", $"{turretCost}G (요격)", "[2]",
                    _selectedBuilding == BuildingType.Turret, _game.PlayerCredit >= turretCost, 2, _colorEmerald);

                int wallCost = _game.GetBuildingCost(BuildingType.Wall);
                DrawActionCard(g, _slotRects[2], "🧱 방화벽", $"{wallCost}G (HP 8)", "[3]",
                    _selectedBuilding == BuildingType.Wall, _game.PlayerCredit >= wallCost, 3, Color.FromArgb(160, 180, 200));

                int factoryCost = _game.GetBuildingCost(BuildingType.Factory);
                DrawActionCard(g, _slotRects[3], "🏭 전진공장", $"{factoryCost}G (전진스폰)", "[4]",
                    _selectedBuilding == BuildingType.Factory, _game.PlayerCredit >= factoryCost, 4, _colorPurple);

                int depotCost = _game.GetBuildingCost(BuildingType.RepairDepot);
                DrawActionCard(g, _slotRects[4], "🏥 수리격납고", $"{depotCost}G (턴당수리)", "[5]",
                    _selectedBuilding == BuildingType.RepairDepot, _game.PlayerCredit >= depotCost, 5, Color.FromArgb(0, 229, 255));

                int gateCost = _game.GetBuildingCost(BuildingType.PhaseGate);
                DrawActionCard(g, _slotRects[5], "⏩ 가속관문", $"{gateCost}G (이동+1)", "[6]",
                    _selectedBuilding == BuildingType.PhaseGate, _game.PlayerCredit >= gateCost, 6, Color.FromArgb(206, 147, 216));

                // 발전소 한도 증설 버튼: 발전소(슬롯 0) 바로 아래 세로 배치!
                int expandCost = _game.GetGridExpandCost(Owner.Player);
                bool canAffordExpand = _game.PlayerCredit >= expandCost;
                DrawActionCard(g, _btnExpandGridRect, "⚡ 한도증설", $"{expandCost}G (한도+1)", "[+]",
                    false, canAffordExpand, 95, _colorGold);
            }
            else if (_currentCategory == BottomCategory.Units)
            {
                int vgCost = _game.GetUnitCost(UnitType.Vanguard);
                DrawActionCard(g, _slotRects[0], "● 보병", $"{vgCost}G (기본)", "[1]",
                    _selectedUnit == UnitType.Vanguard, _game.PlayerCredit >= vgCost, 1, _colorCyan);

                int rdCost = _game.GetUnitCost(UnitType.Raider);
                DrawActionCard(g, _slotRects[1], "▲ 침투병", $"{rdCost}G (이속2칸)", "[2]",
                    _selectedUnit == UnitType.Raider, _game.PlayerCredit >= rdCost, 2, Color.FromArgb(255, 170, 0));

                int ttCost = _game.GetUnitCost(UnitType.Titan);
                DrawActionCard(g, _slotRects[2], "■ 타이탄", $"{ttCost}G (사거리2)", "[3]",
                    _selectedUnit == UnitType.Titan, _game.PlayerCredit >= ttCost, 3, _colorCrimson);

                int cgCost = _game.GetUnitCost(UnitType.Cheongung);
                DrawActionCard(g, _slotRects[3], "🚀 천궁", $"{cgCost}G (공성6뎀)", "[4]",
                    _selectedUnit == UnitType.Cheongung, _game.PlayerCredit >= cgCost, 4, Color.FromArgb(255, 120, 50));

                int clCost = _game.GetUnitCost(UnitType.Colossus);
                DrawActionCard(g, _slotRects[4], "⬢ 콜로서스", $"{clCost}G (사거리3)", "[5]",
                    _selectedUnit == UnitType.Colossus, _game.PlayerCredit >= clCost, 5, Color.FromArgb(220, 100, 255));

                int dnCost = _game.GetUnitCost(UnitType.Dreadnought);
                DrawActionCard(g, _slotRects[5], "✦ 드레드노트", $"{dnCost}G (십자방전)", "[6]",
                    _selectedUnit == UnitType.Dreadnought, _game.PlayerCredit >= dnCost, 6, Color.FromArgb(255, 190, 0));
            }
            else // BottomCategory.Skills
            {
                int obCost = _game.GetSkillCost(UserSkillType.OrbitalStrike);
                DrawActionCard(g, _slotRects[0], "🎯 궤도폭격", $"{obCost}G (건물6뎀)", "[1]",
                    _selectedSkill == UserSkillType.OrbitalStrike, _game.PlayerCredit >= obCost, 1, _colorCrimson);

                int empCost = _game.GetSkillCost(UserSkillType.EmpJammer);
                DrawActionCard(g, _slotRects[1], "⚡ EMP교란", $"{empCost}G (3x3마비)", "[2]",
                    _selectedSkill == UserSkillType.EmpJammer, _game.PlayerCredit >= empCost, 2, _colorCyan);

                int hlCost = _game.GetSkillCost(UserSkillType.HealingBeam);
                DrawActionCard(g, _slotRects[2], "💚 치료광선", $"{hlCost}G (행수복+6)", "[3]",
                    _selectedSkill == UserSkillType.HealingBeam, _game.PlayerCredit >= hlCost, 3, Color.FromArgb(0, 230, 118));

                int ftCost = _game.GetSkillCost(UserSkillType.FortifyBeam);
                DrawActionCard(g, _slotRects[3], "🛡️ 구축광선", $"{ftCost}G (행내구+2)", "[4]",
                    _selectedSkill == UserSkillType.FortifyBeam, _game.PlayerCredit >= ftCost, 4, Color.FromArgb(0, 229, 255));

                int scCost = _game.GetSkillCost(UserSkillType.ReconScan);
                DrawActionCard(g, _slotRects[4], "📡 정찰스캔", $"{scCost}G (은폐해제)", "[5]",
                    false, _game.PlayerCredit >= scCost, 5, _colorGold);
            }

            // 턴 종료 버튼 (애니메이션 중에는 비활성화)
            bool isEndTurnHover = _hoverButtonId == 8 && !_isTurnRunning;
            Color endTurnBg = _isTurnRunning ? Color.FromArgb(20, 30, 25) :
                              isEndTurnHover ? Color.FromArgb(40, 160, 100) : Color.FromArgb(25, 80, 50);

            using (var brush = new SolidBrush(endTurnBg))
            using (var pen = new Pen(_isTurnRunning ? Color.FromArgb(50, 70, 60) : _colorEmerald, 2f))
            {
                g.FillRoundedRectangle(brush, _btnEndTurnRect, 6);
                g.DrawRoundedRectangle(pen, _btnEndTurnRect, 6);
            }
            using (var font = new Font("Segoe UI", 11.5f, FontStyle.Bold))
            {
                string btnText = _isTurnRunning ? "진격 중..." : "▶ 턴 종료";
                TextRenderer.DrawText(g, btnText, font, _btnEndTurnRect, _isTurnRunning ? Color.FromArgb(120, 150, 130) : Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            DrawSmallButton(g, _btnRestartRect, "🔄 리셋", 9);
            DrawSmallButton(g, _btnGuideRect, "❓ 룰", 10);
            DrawSmallButton(g, _btnAchievementsBottomRect, "🏆 과제", 11);
        }

        private void DrawCategoryTab(Graphics g, Rectangle rect, string text, bool isSelected, int btnId, Color accent)
        {
            bool isHover = _hoverButtonId == btnId && !_isTurnRunning;

            Color bgColor = isSelected ? Color.FromArgb(32, 42, 58) :
                            isHover ? Color.FromArgb(24, 30, 42) : Color.FromArgb(16, 20, 28);
            Color borderColor = isSelected ? accent :
                                isHover ? Color.FromArgb(90, 115, 145) : _colorGridLine;

            using (var brush = new SolidBrush(bgColor))
            using (var pen = new Pen(borderColor, isSelected ? 1.8f : 1f))
            {
                g.FillRoundedRectangle(brush, rect, 4);
                g.DrawRoundedRectangle(pen, rect, 4);
            }

            if (isSelected)
            {
                // 하단 활성 인디케이터 라인
                using (var linePen = new Pen(accent, 2f))
                {
                    g.DrawLine(linePen, rect.X + 8, rect.Bottom - 2, rect.Right - 8, rect.Bottom - 2);
                }
            }

            Color textColor = isSelected ? accent : isHover ? Color.FromArgb(220, 230, 245) : Color.FromArgb(150, 165, 185);
            using (var font = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            {
                TextRenderer.DrawText(g, text, font, rect, textColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private void DrawActionCard(Graphics g, Rectangle rect, string title, string sub, string hotkey, bool isSelected, bool canAfford, int btnId, Color accent)
        {
            bool isHover = _hoverButtonId == btnId && !_isTurnRunning;

            Color bgColor = isSelected ? Color.FromArgb(35, 45, 65) :
                            isHover ? Color.FromArgb(28, 35, 48) : Color.FromArgb(18, 22, 30);

            Color borderColor = isSelected ? accent :
                                isHover ? Color.FromArgb(100, 130, 170) : _colorGridLine;

            float bWidth = isSelected ? 2f : 1f;

            using (var brush = new SolidBrush(bgColor))
            using (var pen = new Pen(borderColor, bWidth))
            {
                g.FillRoundedRectangle(brush, rect, 5);
                g.DrawRoundedRectangle(pen, rect, 5);
            }

            // 우측 상단 단축키 미니 뱃지 (글자와 겹치지 않도록 깔끔한 독립 뱃지 처리)
            string keyNum = hotkey.Replace("[", "").Replace("]", "");
            var kRect = new Rectangle(rect.Right - 15, rect.Y + 3, 12, 12);
            using (var kBrush = new SolidBrush(Color.FromArgb(50, isSelected ? accent : Color.White)))
            using (var kPen = new Pen(isSelected ? accent : Color.FromArgb(80, 100, 125), 1f))
            {
                g.FillRoundedRectangle(kBrush, kRect, 3);
                g.DrawRoundedRectangle(kPen, kRect, 3);
            }
            using (var kFont = new Font("Segoe UI", 6.5f, FontStyle.Bold))
            {
                TextRenderer.DrawText(g, keyNum, kFont, kRect, isSelected ? accent : Color.FromArgb(170, 190, 215),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            Color titleColor = !canAfford ? Color.FromArgb(130, 95, 95) :
                               isSelected ? accent : Color.FromArgb(220, 230, 242);

            Color subColor = !canAfford ? Color.FromArgb(200, 75, 75) :
                             isSelected ? _colorGold : Color.FromArgb(130, 145, 165);

            // 타이틀 영역: 우측 뱃지 공간(16px)을 제외한 영역에 텍스트를 배치하여 겹침 100% 방지!
            using (var titleFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (var subFont = new Font("Segoe UI", 7.5f))
            {
                var titleRect = new Rectangle(rect.X + 2, rect.Y + 4, rect.Width - 18, 18);
                TextRenderer.DrawText(g, title, titleFont, titleRect,
                    titleColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                var subRect = new Rectangle(rect.X + 2, rect.Y + 24, rect.Width - 4, 18);
                TextRenderer.DrawText(g, sub, subFont, subRect,
                    subColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private void DrawSmallButton(Graphics g, Rectangle rect, string text, int btnId)
        {
            bool isHover = _hoverButtonId == btnId && !_isTurnRunning;
            Color bg = isHover ? Color.FromArgb(35, 45, 60) : Color.FromArgb(22, 28, 38);
            Color border = isHover ? _colorCyan : _colorGridLine;

            using (var brush = new SolidBrush(bg))
            using (var pen = new Pen(border, 1f))
            {
                g.FillRoundedRectangle(brush, rect, 4);
                g.DrawRoundedRectangle(pen, rect, 4);
            }
            using (var font = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            {
                TextRenderer.DrawText(g, text, font, rect, Color.FromArgb(200, 215, 230),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        #endregion

    }
}
