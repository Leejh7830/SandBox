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
        #region Roguelike Modal & Game Over Modal

        private void DrawCardSelectionModal(Graphics g)
        {
            using (var dimBrush = new SolidBrush(Color.FromArgb(220, 6, 9, 15)))
            {
                g.FillRectangle(dimBrush, this.ClientRectangle);
            }

            using (var titleFont = new Font("Segoe UI", 15f, FontStyle.Bold))
            using (var subFont = new Font("Segoe UI", 10f))
            {
                TextRenderer.DrawText(g, "⚡ 전술 프로토콜 보급 (3턴 주기)", titleFont,
                    new Rectangle(0, 40, this.Width, 32), _colorGold, TextFormatFlags.HorizontalCenter);

                TextRenderer.DrawText(g, "이번 턴에 전군에 하달할 프로토콜 카드 1종을 선택하십시오. (효과는 영구 누적됩니다)", subFont,
                    new Rectangle(0, 75, this.Width, 24), Color.FromArgb(200, 215, 230), TextFormatFlags.HorizontalCenter);
            }

            for (int i = 0; i < _game.CurrentOfferedCards.Count && i < 3; i++)
            {
                var card = _game.CurrentOfferedCards[i];
                var rect = _cardRects[i];
                bool isHover = _hoverCardIndex == i;

                // 등급별 스타일 (Common / Rare / Epic)
                string rText;
                Color rColor;
                Color rBg;
                switch (card.Rarity)
                {
                    case CardRarity.Rare:
                        rText = "✦ RARE";
                        rColor = Color.FromArgb(0, 229, 255);
                        rBg = Color.FromArgb(15, 60, 85);
                        break;
                    case CardRarity.Epic:
                        rText = "★ EPIC";
                        rColor = Color.FromArgb(255, 190, 0);
                        rBg = Color.FromArgb(70, 50, 15);
                        break;
                    default:
                        rText = "◈ COMMON";
                        rColor = Color.FromArgb(170, 190, 215);
                        rBg = Color.FromArgb(30, 40, 55);
                        break;
                }

                Color cardBg = isHover ? Color.FromArgb(30, 38, 55) :
                               (card.Rarity == CardRarity.Epic ? Color.FromArgb(28, 26, 32) : Color.FromArgb(20, 26, 38));
                Color border = isHover ? card.AccentColor :
                               (card.Rarity == CardRarity.Epic ? Color.FromArgb(220, 170, 30) :
                               (card.Rarity == CardRarity.Rare ? Color.FromArgb(0, 180, 210) : Color.FromArgb(70, 85, 110)));
                float bWidth = isHover ? 2.5f : (card.Rarity == CardRarity.Epic ? 2f : 1.5f);

                using (var brush = new SolidBrush(cardBg))
                using (var pen = new Pen(border, bWidth))
                {
                    g.FillRoundedRectangle(brush, rect, 10);
                    g.DrawRoundedRectangle(pen, rect, 10);
                }

                // 상단 헤더 배경 (등급 컬러 틴트)
                var headerRect = new Rectangle(rect.X + 1, rect.Y + 1, rect.Width - 2, 56);
                using (var hBrush = new SolidBrush(Color.FromArgb(card.Rarity == CardRarity.Epic ? 45 : 30, rColor)))
                {
                    g.FillRoundedRectangle(hBrush, headerRect, 8);
                }

                using (var fontBadge = new Font("Segoe UI", 7.5f, FontStyle.Bold))
                using (var fontTitle = new Font("Segoe UI", 10.5f, FontStyle.Bold))
                using (var fontSub = new Font("Segoe UI", 8f, FontStyle.Bold))
                using (var fontDesc = new Font("Segoe UI", 8.5f))
                {
                    // 1. 등급 뱃지
                    var badgeRect = new Rectangle(rect.X + (rect.Width - 92) / 2, rect.Y + 6, 92, 16);
                    using (var bBrush = new SolidBrush(rBg))
                    using (var bPen = new Pen(rColor, 1f))
                    {
                        g.FillRoundedRectangle(bBrush, badgeRect, 3);
                        g.DrawRoundedRectangle(bPen, badgeRect, 3);
                    }
                    TextRenderer.DrawText(g, rText, fontBadge, badgeRect, rColor,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                    // 2. 카드 타이틀
                    TextRenderer.DrawText(g, card.Title, fontTitle, new Rectangle(rect.X + 4, rect.Y + 25, rect.Width - 8, 22),
                        card.AccentColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                    // 3. 서브타이틀 (WordBreak 추가로 긴 문장도 깔끔하게 줄바꿈 및 잘림 원천 방지)
                    TextRenderer.DrawText(g, card.Subtitle, fontSub, new Rectangle(rect.X + 6, rect.Y + 49, rect.Width - 12, 28),
                        rColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak);

                    // 4. 상세 설명 (충분한 높이와 WordBreak 및 \n 완벽 지원)
                    var descRect = new Rectangle(rect.X + 12, rect.Y + 84, rect.Width - 24, rect.Height - 84 - 46);
                    TextRenderer.DrawText(g, card.Description, fontDesc, descRect,
                        Color.FromArgb(230, 240, 250), TextFormatFlags.Left | TextFormatFlags.WordBreak);

                    // 5. 선택 버튼
                    var pickRect = new Rectangle(rect.X + 14, rect.Bottom - 40, rect.Width - 28, 30);
                    Color pickBg = isHover ? card.AccentColor : Color.FromArgb(35, 45, 65);
                    Color pickText = isHover ? Color.Black : Color.White;

                    using (var pBrush = new SolidBrush(pickBg))
                    using (var pPen = new Pen(card.AccentColor, 1.5f))
                    {
                        g.FillRoundedRectangle(pBrush, pickRect, 5);
                        g.DrawRoundedRectangle(pPen, pickRect, 5);
                    }
                    TextRenderer.DrawText(g, "선택 (SELECT)", fontSub, pickRect, pickText,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            }
        }

        private void DrawGameOverModal(Graphics g)
        {
            using (var dimBrush = new SolidBrush(Color.FromArgb(220, 6, 8, 14)))
            {
                g.FillRectangle(dimBrush, this.ClientRectangle);
            }

            int cardW = 460;
            int cardH = 350;
            int cardX = (this.Width - cardW) / 2;
            int cardY = (this.Height - cardH) / 2 - 10;
            var cardRect = new Rectangle(cardX, cardY, cardW, cardH);

            Color accent = (_game.Winner == Owner.Player) ? _colorCyan : _colorCrimson;
            string title = (_game.Winner == Owner.Player) ? "🎉 VICTORY! 적 메인프레임 점령" : "💀 DEFEAT! 아군 코어 함락";

            using (var brush = new SolidBrush(Color.FromArgb(20, 26, 38)))
            using (var pen = new Pen(accent, 2.5f))
            {
                g.FillRoundedRectangle(brush, cardRect, 10);
                g.DrawRoundedRectangle(pen, cardRect, 10);
            }

            using (var titleFont = new Font("Segoe UI", 15f, FontStyle.Bold))
            using (var subFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (var statLabelFont = new Font("Segoe UI", 8.5f))
            using (var statValFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (var btnFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            {
                // 1. 승리 / 패배 타이틀
                TextRenderer.DrawText(g, title, titleFont, new Rectangle(cardX, cardY + 16, cardW, 30),
                    accent, TextFormatFlags.HorizontalCenter);

                TextRenderer.DrawText(g, "◈ 전역 작전 결산 및 종합 전투 통계 ◈", subFont, new Rectangle(cardX, cardY + 46, cardW, 18),
                    Color.FromArgb(170, 190, 215), TextFormatFlags.HorizontalCenter);

                // 2. 종합 전투 통계 대시보드 HUD 박스
                int dashX = cardX + 18;
                int dashY = cardY + 70;
                int dashW = cardW - 36;
                int dashH = 205;
                var dashRect = new Rectangle(dashX, dashY, dashW, dashH);

                using (var dBrush = new SolidBrush(Color.FromArgb(14, 18, 26)))
                using (var dPen = new Pen(Color.FromArgb(45, 55, 75), 1.2f))
                {
                    g.FillRoundedRectangle(dBrush, dashRect, 6);
                    g.DrawRoundedRectangle(dPen, dashRect, 6);
                }

                // 통계 8대 항목 (2열 4행 배치)
                var statItems = new (string Icon, string Label, string Value, Color ValColor)[]
                {
                    ("⏱️", "총 작전 소요", $"{_game.StatsTurnsElapsed} 턴", Color.White),
                    ("💰", "총 획득 크레딧", $"{_game.StatsTotalCreditEarned} G", _colorGold),
                    ("⚔️", "적 유닛 처치", $"{_game.StatsPlayerUnitsKilled} 기", _colorCyan),
                    ("🛡️", "아군 유닛 손실", $"{_game.StatsPlayerUnitsLost} 기", Color.FromArgb(255, 140, 140)),
                    ("💥", "적 인프라 철거", $"{_game.StatsPlayerBuildingsDestroyed} 개", Color.FromArgb(255, 179, 0)),
                    ("🖥️", "서버 랙 돌파", $"{_game.StatsObstaclesDestroyed} 개 (+{_game.StatsObstaclesDestroyed * 2}G)", Color.FromArgb(130, 220, 255)),
                    ("🛡️", "아군 코어 상태", $"{_game.PlayerCoreHp} / {_game.PlayerCoreMaxHp} HP", _colorCyan),
                    ("🎯", "적 코어 피해", $"{_game.StatsDamageDealtToAiCore} DMG", Color.FromArgb(255, 75, 75))
                };

                int colW = (dashW - 20) / 2;
                int rowH = 46;

                for (int i = 0; i < statItems.Length; i++)
                {
                    int col = i % 2;
                    int row = i / 2;
                    int itemX = dashX + 10 + col * (colW + 10);
                    int itemY = dashY + 10 + row * rowH;

                    var itemRect = new Rectangle(itemX, itemY, colW, rowH - 6);
                    using (var iBrush = new SolidBrush(Color.FromArgb(22, 28, 40)))
                    using (var iPen = new Pen(Color.FromArgb(35, 45, 60), 1f))
                    {
                        g.FillRoundedRectangle(iBrush, itemRect, 4);
                        g.DrawRoundedRectangle(iPen, itemRect, 4);
                    }

                    var item = statItems[i];
                    TextRenderer.DrawText(g, $"{item.Icon} {item.Label}", statLabelFont,
                        new Rectangle(itemX + 8, itemY + 4, colW - 16, 16),
                        Color.FromArgb(150, 165, 185), TextFormatFlags.Left);

                    TextRenderer.DrawText(g, item.Value, statValFont,
                        new Rectangle(itemX + 8, itemY + 18, colW - 16, 18),
                        item.ValColor, TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
                }

                // 3. 하단 액션 버튼 3종 (리플레이 / 다시하기 / 로비)
                // [▶ 전투 리플레이] 버튼
                bool isReplayHover = _hoverButtonId == 97;
                using (var repBrush = new SolidBrush(isReplayHover ? Color.FromArgb(30, 100, 160) : Color.FromArgb(20, 60, 105)))
                using (var repPen = new Pen(_colorCyan, 1.8f))
                {
                    g.FillRoundedRectangle(repBrush, _btnReplayRect, 5);
                    g.DrawRoundedRectangle(repPen, _btnReplayRect, 5);
                }
                TextRenderer.DrawText(g, "▶ 전투 리플레이", btnFont, _btnReplayRect, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                // [다시 하기] 버튼
                bool isRestartHover = _hoverButtonId == 99;
                using (var rBrush = new SolidBrush(isRestartHover ? Color.FromArgb(40, 160, 100) : Color.FromArgb(25, 110, 65)))
                using (var rPen = new Pen(_colorEmerald, 1.8f))
                {
                    g.FillRoundedRectangle(rBrush, _gameOverRestartBtnRect, 5);
                    g.DrawRoundedRectangle(rPen, _gameOverRestartBtnRect, 5);
                }
                TextRenderer.DrawText(g, "🔄 다시하기", btnFont, _gameOverRestartBtnRect, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                // [로비로] 버튼
                bool isBackHover = _hoverButtonId == 98;
                using (var bBrush = new SolidBrush(isBackHover ? Color.FromArgb(50, 60, 80) : Color.FromArgb(30, 38, 52)))
                using (var bPen = new Pen(Color.FromArgb(120, 140, 170), 1.5f))
                {
                    g.FillRoundedRectangle(bBrush, _gameOverBackBtnRect, 5);
                    g.DrawRoundedRectangle(bPen, _gameOverBackBtnRect, 5);
                }
                TextRenderer.DrawText(g, "◀ 로비", btnFont, _gameOverBackBtnRect, Color.FromArgb(220, 230, 245),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        /// <summary>
        /// 초정밀 전장 시네마틱 인터랙티브 리플레이 뷰어 모드
        /// (지형 기믹 5종, 6종 유닛 및 6종 건물 1:1 비주얼 동기화, 타임라인 스크러버, 미디어 컨트롤러 HUD, 키보드 단축키)
        /// </summary>
        public void DrawReplayMode(Graphics g)
        {
            g.Clear(_colorBg);

            if (_game.ReplaySnapshots.Count == 0) return;
            int snapIdx = Math.Min(_replayIndex, _game.ReplaySnapshots.Count - 1);
            var snap = _game.ReplaySnapshots[snapIdx];

            // 1. 상단 리플레이 시네마틱 헤더 HUD
            var repHeaderRect = new Rectangle(_boardRect.X, 10, _boardRect.Width, 48);
            using (var hBrush = new SolidBrush(Color.FromArgb(20, 26, 38)))
            using (var hPen = new Pen(_colorCyan, 1.5f))
            {
                g.FillRoundedRectangle(hBrush, repHeaderRect, 6);
                g.DrawRoundedRectangle(hPen, repHeaderRect, 6);
            }

            using (var titleFont = new Font("Segoe UI", 10.5f, FontStyle.Bold))
            using (var subFont = new Font("Segoe UI", 8.5f))
            using (var noteFont = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (var statusFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            {
                // 좌측: 단계 번호 & 작전 상태 노트
                string turnTag = $"▶ 작전 리플레이 [ {snapIdx + 1} / {_game.ReplaySnapshots.Count} ]";
                TextRenderer.DrawText(g, turnTag, titleFont, new Rectangle(repHeaderRect.X + 12, repHeaderRect.Y + 6, 220, 18),
                    _colorCyan, TextFormatFlags.Left);

                string noteText = $"• {snap.Note}";
                TextRenderer.DrawText(g, noteText, noteFont, new Rectangle(repHeaderRect.X + 230, repHeaderRect.Y + 7, Math.Max(80, repHeaderRect.Width - 360), 18),
                    _colorGold, TextFormatFlags.Left | TextFormatFlags.EndEllipsis);

                // 2행: 코어 HP 및 크레딧 대조
                string playerHpStr = $"아군 코어 HP {snap.PlayerCoreHp} (⬡{snap.PlayerCredit}G)";
                string aiHpStr = $"적 코어 HP {snap.AiCoreHp} (⬡{snap.AiCredit}G)";
                string versusStr = $"{playerHpStr}   ⚡ VS ⚡   {aiHpStr}";
                TextRenderer.DrawText(g, versusStr, subFont, new Rectangle(repHeaderRect.X + 12, repHeaderRect.Y + 26, 450, 16),
                    Color.FromArgb(210, 225, 240), TextFormatFlags.Left);

                // 우측 테마 뱃지 (초대형 전장인 경우)
                if (_game.CurrentMapSize == MapSizeType.Huge)
                {
                    string themeText = $"{_game.GetThemeIcon()} {_game.GetThemeName()}";
                    Color themeCol = _game.GetThemeColor();
                    Size measured = TextRenderer.MeasureText(themeText, statusFont);
                    int badgeW = Math.Max(70, measured.Width + 14);
                    int badgeX = repHeaderRect.Right - badgeW - 10;
                    var badgeRect = new Rectangle(badgeX, repHeaderRect.Y + 11, badgeW, 26);
                    using (var tbBrush = new SolidBrush(Color.FromArgb(40, themeCol)))
                    using (var tbPen = new Pen(themeCol, 1.2f))
                    {
                        g.FillRoundedRectangle(tbBrush, badgeRect, 4);
                        g.DrawRoundedRectangle(tbPen, badgeRect, 4);
                    }
                    TextRenderer.DrawText(g, themeText, statusFont, badgeRect, themeCol,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            }

            // 2. 보드 배경 렌더링
            using (var boardBrush = new SolidBrush(_colorBoardBg))
            using (var boardPen = new Pen(Color.FromArgb(50, 65, 85), 2f))
            {
                g.FillRoundedRectangle(boardBrush, _boardRect, 6);
                g.DrawRoundedRectangle(boardPen, _boardRect, 6);
            }

            // 2.1 Col 0: 플레이어 아군 거대 기지 섀시 (Cyber Fortress)
            int pBaseW = _tileW;
            var pBaseRect = new Rectangle(_boardRect.X, _boardRect.Y, pBaseW, _boardRect.Height);
            using (var baseBrush = new SolidBrush(Color.FromArgb(10, 22, 35)))
            using (var basePen = new Pen(_colorCyan, 1.5f))
            {
                g.FillRectangle(baseBrush, pBaseRect);
                g.DrawRectangle(basePen, pBaseRect.X + 2, pBaseRect.Y + 2, pBaseW - 4, _boardRect.Height - 4);

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

            // 2.2 Col cols-1: 적 AI 거대 기지 섀시 (Demon Fortress)
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

            // 적 에너지 방어선 (Laser Fence between Col cols-2 and cols-1)
            int fenceX2 = _boardRect.Right - _tileW;
            using (var fencePen = new Pen(_colorCrimson, 2.5f) { DashStyle = DashStyle.Dash })
            {
                g.DrawLine(fencePen, fenceX2, _boardRect.Y, fenceX2, _boardRect.Bottom);
            }

            // 2.3 활성 타일 격자 및 지형 기믹 렌더링
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
                    else if (tType == TileType.Fire)
                    {
                        using (var fireBrush = new SolidBrush(Color.FromArgb(40, 255, 69, 0)))
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
                        using (var railBrush = new SolidBrush(Color.FromArgb(40, 0, 200, 255)))
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
                        using (var wellBrush = new SolidBrush(Color.FromArgb(40, 46, 125, 50)))
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
                        using (var smokeBrush = new SolidBrush(Color.FromArgb(50, 123, 31, 162)))
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

            // 3. 스냅샷 장애물(서버 랙) 렌더링
            foreach (var obs in snap.Obstacles)
            {
                int x = _boardRect.X + obs.Col * _tileW;
                int y = _boardRect.Y + obs.Row * _tileH;
                var rect = new Rectangle(x, y, _tileW, _tileH);

                using (var obsBrush = new SolidBrush(Color.FromArgb(35, 42, 54)))
                using (var obsPen = new Pen(Color.FromArgb(70, 80, 95), 1.5f))
                {
                    g.FillRectangle(obsBrush, rect);
                    g.DrawRectangle(obsPen, x + 3, y + 3, _tileW - 6, _tileH - 6);
                    g.DrawLine(obsPen, x + 5, y + 5, x + _tileW - 5, y + _tileH - 5);
                    g.DrawLine(obsPen, x + _tileW - 5, y + 5, x + 5, y + _tileH - 5);
                }

                DrawMiniHpBar(g, x + 4, y + _tileH - 5, _tileW - 8, 3, obs.Hp, 6, Color.FromArgb(180, 180, 180));
                using (var font = new Font("Segoe UI", 7.5f, FontStyle.Bold))
                {
                    TextRenderer.DrawText(g, "🖥️", font, rect, Color.FromArgb(170, 180, 195),
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            }

            // 4. 스냅샷 건물 렌더링
            foreach (var b in snap.Buildings)
            {
                int x = _boardRect.X + b.Col * _tileW;
                int y = _boardRect.Y + b.Row * _tileH;
                var rect = new Rectangle(x + 3, y + 3, _tileW - 6, _tileH - 6);
                Color teamColor = (b.Owner == Owner.Player) ? _colorCyan : _colorCrimson;

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

                // EMP 정지 표시
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

                DrawMiniHpBar(g, rect.X, rect.Bottom - 4, rect.Width, 3, b.Hp, b.MaxHp, teamColor);
            }

            // 5. 스냅샷 유닛 렌더링
            foreach (var u in snap.Units)
            {
                int x = _boardRect.X + u.Col * _tileW;
                int y = _boardRect.Y + u.Row * _tileH;
                var rect = new Rectangle(x + 4, y + 4, _tileW - 8, _tileH - 8);
                Color teamColor = (u.Owner == Owner.Player) ? _colorCyan : _colorCrimson;

                if (u.Type == UnitType.Vanguard)
                {
                    if (u.HasShield)
                    {
                        using (var sBrush = new SolidBrush(Color.FromArgb(40, 64, 196, 255)))
                        using (var sPen = new Pen(Color.FromArgb(220, 64, 196, 255), 1.8f) { DashStyle = DashStyle.Dash })
                        {
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
                        if (u.IsCloaked) bPen.DashStyle = DashStyle.Dot;
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
                        g.DrawEllipse(accentPen, rect.X + 7, rect.Y + 7, rect.Width - 14, rect.Height - 14);
                    }
                    using (var font = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                    {
                        TextRenderer.DrawText(g, "⬢", font, rect, Color.White,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }
                }
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
                        g.DrawLine(crossPen, cx - 7, cy, cx + 7, cy);
                        g.DrawLine(crossPen, cx, cy - 7, cx, cy + 7);
                    }
                    using (var font = new Font("Segoe UI", 9f, FontStyle.Bold))
                    {
                        TextRenderer.DrawText(g, "✦", font, rect, Color.White,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }
                }
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
                        g.DrawLine(missilePen, cx - dir * 4, cy - 5, cx + dir * 12, cy - 5);
                        g.DrawLine(missilePen, cx - dir * 4, cy + 5, cx + dir * 12, cy + 5);
                    }
                    using (var font = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                    {
                        TextRenderer.DrawText(g, "🚀", font, rect, Color.White,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }
                }

                DrawHpDots(g, rect.X, rect.Y - 2, rect.Width, u.Hp, u.MaxHp, teamColor);

                if (u.HasBomb)
                {
                    var bombBadgeRect = new Rectangle(rect.Left + 1, rect.Bottom - 13, 14, 12);
                    using (var bmbBrush = new SolidBrush(Color.FromArgb(210, 25, 25, 35)))
                    using (var bmbPen = new Pen(Color.FromArgb(255, 110, 64), 1f))
                    {
                        g.FillRectangle(bmbBrush, bombBadgeRect);
                        g.DrawRectangle(bmbPen, bombBadgeRect);
                    }
                    using (var bmbFont = new Font("Segoe UI", 7f))
                    {
                        TextRenderer.DrawText(g, "💣", bmbFont, bombBadgeRect, Color.White,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }
                }
            }

            // 6. 타임라인 진행도 바 (Progress Scrubber Bar)
            using (var trackBrush = new SolidBrush(Color.FromArgb(16, 22, 34)))
            using (var trackPen = new Pen(_hoverReplayBtnId == 8 ? _colorCyan : Color.FromArgb(48, 64, 86), 1.2f))
            {
                g.FillRoundedRectangle(trackBrush, _repProgressBarRect, 5);
                g.DrawRoundedRectangle(trackPen, _repProgressBarRect, 5);
            }

            float progressRatio = (_game.ReplaySnapshots.Count > 1)
                ? (float)snapIdx / (_game.ReplaySnapshots.Count - 1)
                : 1f;
            int fillWidth = Math.Max(6, (int)(_repProgressBarRect.Width * progressRatio));
            var fillRect = new Rectangle(_repProgressBarRect.X, _repProgressBarRect.Y, fillWidth, _repProgressBarRect.Height);
            using (var fillBrush = new LinearGradientBrush(_repProgressBarRect, Color.FromArgb(0, 180, 240), _colorCyan, LinearGradientMode.Horizontal))
            {
                g.FillRoundedRectangle(fillBrush, fillRect, 5);
            }

            // 단계 틱 마크 렌더링
            if (_game.ReplaySnapshots.Count > 1 && _game.ReplaySnapshots.Count <= 80)
            {
                using (var tickPen = new Pen(Color.FromArgb(70, 255, 255, 255), 1f))
                {
                    for (int i = 0; i < _game.ReplaySnapshots.Count; i++)
                    {
                        int tx = _repProgressBarRect.X + (int)((float)i / (_game.ReplaySnapshots.Count - 1) * (_repProgressBarRect.Width - 1));
                        g.DrawLine(tickPen, tx, _repProgressBarRect.Y + 2, tx, _repProgressBarRect.Bottom - 2);
                    }
                }
            }

            // 스크러버 핸들 썸 (Thumb)
            int thumbX = _repProgressBarRect.X + (int)((_repProgressBarRect.Width - 1) * progressRatio);
            int thumbY = _repProgressBarRect.Y + _repProgressBarRect.Height / 2;
            int thumbRadius = (_hoverReplayBtnId == 8) ? 8 : 6;
            using (var thumbBrush = new SolidBrush(Color.White))
            using (var thumbPen = new Pen(_colorCyan, 2f))
            {
                g.FillEllipse(thumbBrush, thumbX - thumbRadius, thumbY - thumbRadius, thumbRadius * 2, thumbRadius * 2);
                g.DrawEllipse(thumbPen, thumbX - thumbRadius, thumbY - thumbRadius, thumbRadius * 2, thumbRadius * 2);
            }

            // 7. 리플레이 미디어 컨트롤러 버튼 7종
            DrawReplayMediaButton(g, _repBtnFirstRect, "⏮ 처음", 1, _colorCyan);
            DrawReplayMediaButton(g, _repBtnPrevRect, "◀ 이전", 2, _colorCyan);

            string playPauseText = _isReplayPaused ? "▶ 재생" : "⏸ 정지";
            Color playPauseColor = _isReplayPaused ? _colorEmerald : _colorGold;
            DrawReplayMediaButton(g, _repBtnPlayPauseRect, playPauseText, 3, playPauseColor);

            DrawReplayMediaButton(g, _repBtnNextRect, "다음 ▶", 4, _colorCyan);
            DrawReplayMediaButton(g, _repBtnLastRect, "끝으로 ⏭", 5, _colorCyan);

            string speedText = $"⚡ {_replaySpeed}x";
            DrawReplayMediaButton(g, _repBtnSpeedRect, speedText, 6, _colorGold);

            DrawReplayMediaButton(g, _repBtnExitRect, "■ 복귀 (ESC)", 7, _colorCrimson);

            // 8. 하단 키보드 단축키 안내
            int hintY = _repBtnFirstRect.Bottom + 10;
            var hintRect = new Rectangle(_boardRect.X, hintY, _boardRect.Width, 22);
            using (var hintFont = new Font("Segoe UI", 8.5f))
            {
                string hintText = "조작 단축키:  [Space] 재생/일시정지   │   [← / A] 이전   │   [→ / D] 다음   │   [Home / End] 처음/끝   │   [1 / 2 / 4] 배속   │   [ESC] 결과 복귀";
                TextRenderer.DrawText(g, hintText, hintFont, hintRect, Color.FromArgb(170, 195, 220),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private void DrawReplayMediaButton(Graphics g, Rectangle rect, string text, int btnId, Color accent)
        {
            bool isHover = (_hoverReplayBtnId == btnId);
            Color bgCol = isHover ? Color.FromArgb(48, accent) : Color.FromArgb(20, 26, 38);
            Color borderCol = isHover ? accent : Color.FromArgb(70, accent);
            Color textCol = isHover ? Color.White : accent;

            using (var brush = new SolidBrush(bgCol))
            using (var pen = new Pen(borderCol, isHover ? 1.8f : 1.2f))
            {
                g.FillRoundedRectangle(brush, rect, 5);
                g.DrawRoundedRectangle(pen, rect, 5);
            }

            using (var font = new Font("Segoe UI", 9f, FontStyle.Bold))
            {
                TextRenderer.DrawText(g, text, font, rect, textCol,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private void DrawGuideModal(Graphics g)
        {
            using (var dimBrush = new SolidBrush(Color.FromArgb(220, 6, 8, 14)))
            {
                g.FillRectangle(dimBrush, this.ClientRectangle);
            }

            int cardW = 580;
            int cardH = 468;
            int cardX = (this.Width - cardW) / 2;
            int cardY = (this.Height - cardH) / 2;
            var cardRect = new Rectangle(cardX, cardY, cardW, cardH);

            using (var brush = new SolidBrush(Color.FromArgb(20, 26, 38)))
            using (var pen = new Pen(_colorCyan, 2f))
            {
                g.FillRoundedRectangle(brush, cardRect, 10);
                g.DrawRoundedRectangle(pen, cardRect, 10);
            }

            using (var titleFont = new Font("Segoe UI", 12.5f, FontStyle.Bold))
            using (var subFont = new Font("Segoe UI", 8.2f))
            using (var hFont = new Font("Segoe UI", 9.3f, FontStyle.Bold))
            using (var bodyFont = new Font("Segoe UI", 8.4f))
            {
                TextRenderer.DrawText(g, "◈ CORE FRONT 전술 작전 지침 & 게임 규칙 ◈", titleFont,
                    new Rectangle(cardX, cardY + 12, cardW, 24), _colorCyan, TextFormatFlags.HorizontalCenter);

                TextRenderer.DrawText(g, "인프라 자원 구축 x 라인 거점 진격 x 3턴 주기 로그라이크 프로토콜", subFont,
                    new Rectangle(cardX, cardY + 34, cardW, 16), Color.FromArgb(160, 185, 215), TextFormatFlags.HorizontalCenter);

                int y = cardY + 52;
                int textW = cardW - 48;

                // 1. 전략 빌드 & 시스템
                TextRenderer.DrawText(g, "1. 3대 전략 빌드 (내정 · 러시 · 방어)", hFont, new Rectangle(cardX + 24, y, textW, 18), _colorGold, TextFormatFlags.Left);
                y += 18;
                TextRenderer.DrawText(g, "• 내정 빌드: 발전소(6G~, +3G스케일링)와 에너지 노드를 선점하고 [한도증설]로 국고 극대화 (건물 2턴당 자체수리 +1HP)", bodyFont, new Rectangle(cardX + 24, y, textW, 15), Color.White, TextFormatFlags.Left);
                y += 15;
                TextRenderer.DrawText(g, "• 고속 러시: 침투병(2칸 이동)으로 적이 비워둔 빈 라인을 기습 돌파하여 발전소/코어 타격", bodyFont, new Rectangle(cardX + 24, y, textW, 15), Color.White, TextFormatFlags.Left);
                y += 15;
                TextRenderer.DrawText(g, "• 우주 방어: 방화벽 포탑(13칸 전방위 요격)과 격벽을 구축하여 버틴 후 결전 병기 및 스킬로 역공", bodyFont, new Rectangle(cardX + 24, y, textW, 15), Color.White, TextFormatFlags.Left);
                y += 21;

                // 2. 건물 & 유닛 & 전술 스킬
                TextRenderer.DrawText(g, "2. 인프라 건물 · 작전 유닛 · 전술 스킬 (단축키: B: 건물, U: 유닛, S: 스킬, 1~6: 슬롯)", hFont, new Rectangle(cardX + 24, y, textW, 18), _colorEmerald, TextFormatFlags.Left);
                y += 18;
                TextRenderer.DrawText(g, "• 건물: ⚡ 발전소(6G~), 🛡️ 포탑(요격), 🧱 방화벽, 🏭 공장(8G), 🏥 수리격납고(턴당+2HP), ⏩ 가속관문(이동+1), ⚡ 한도증설", bodyFont, new Rectangle(cardX + 24, y, textW, 15), Color.White, TextFormatFlags.Left);
                y += 15;
                TextRenderer.DrawText(g, "• 유닛: ● 보병(2G), ▲ 침투병(4G), ■ 타이탄(10G, HP10/Atk3), 🚀 천궁(12G, 공성6뎀), ⬢ 콜로서스(14G), ✦ 드레드노트(18G)", bodyFont, new Rectangle(cardX + 24, y, textW, 15), Color.White, TextFormatFlags.Left);
                y += 15;
                TextRenderer.DrawText(g, "• 전술스킬 [S]: [1] 궤도폭격(8G), [2] EMP교란(5G), [3] 치료광선(4G, 유닛+건물 +6HP), [4] 구축광선(5G, 내구+2), [5] 정찰스캔(3G)", bodyFont, new Rectangle(cardX + 24, y, textW, 15), Color.FromArgb(255, 140, 220), TextFormatFlags.Left);
                y += 21;

                // 3. 전장 오브젝트 & 특수 전투 상성
                TextRenderer.DrawText(g, "3. 전장 오브젝트 & 특수 전투 상성 (핵심 공략!)", hFont, new Rectangle(cardX + 24, y, textW, 18), Color.FromArgb(255, 125, 145), TextFormatFlags.Left);
                y += 18;
                TextRenderer.DrawText(g, "• ⚡ 최초 기지 설립: 1턴 시작 시 아군 본진 발전소 위치(Col 1~2)를 원하는 타일에 무료 직접 결정!", bodyFont, new Rectangle(cardX + 24, y, textW, 15), _colorGold, TextFormatFlags.Left);
                y += 15;
                TextRenderer.DrawText(g, "• 🛑 서버 랙(HP 6): 공격/포격으로 파괴 시 +2G 현상금 즉시 획득 및 진격로 개방", bodyFont, new Rectangle(cardX + 24, y, textW, 15), _colorGold, TextFormatFlags.Left);
                y += 15;
                TextRenderer.DrawText(g, "• 🚀 천궁 포대: 건물/서버랙에 2배 피해(6뎀)! 시즈 모드 없이 멈추지 않고 매 턴 무조건 직진 행군", bodyFont, new Rectangle(cardX + 24, y, textW, 15), Color.FromArgb(255, 160, 90), TextFormatFlags.Left);
                y += 15;
                TextRenderer.DrawText(g, "• 🛡️ 포탑 사각지대: 1칸 인접(상하좌우) 적 요격 불가 · ⬢ 콜로서스: 1칸 근접 불가 (발칸 카드로 해제)", bodyFont, new Rectangle(cardX + 24, y, textW, 15), Color.White, TextFormatFlags.Left);
                y += 15;
                TextRenderer.DrawText(g, "• ✦ 드레드노트(소형 취약): 보병/침투병 피격 시 +2 피해 · ▲ 침투병: 첫 공격 전까지 은폐 ([정찰스캔]으로 즉시 노출)", bodyFont, new Rectangle(cardX + 24, y, textW, 15), Color.White, TextFormatFlags.Left);
                y += 21;

                // 4. 로그라이크 프로토콜
                TextRenderer.DrawText(g, "4. 3턴 주기 로그라이크 전술 프로토콜 & 티어별 해금", hFont, new Rectangle(cardX + 24, y, textW, 18), _colorPurple, TextFormatFlags.Left);
                y += 18;
                TextRenderer.DrawText(g, "• 3턴마다 일반(50%), 희귀(35%), 에픽(15%) 카드 1장 선택. 버프 효과는 영구 누적 중첩!", bodyFont, new Rectangle(cardX + 24, y, textW, 15), Color.White, TextFormatFlags.Left);
                y += 15;
                TextRenderer.DrawText(g, "• 순차 해금: 턴3(보병/내정) ➔ 턴6(침투병) ➔ 턴9(타이탄) ➔ 턴12(콜로서스/드레드노트)", bodyFont, new Rectangle(cardX + 24, y, textW, 15), _colorGold, TextFormatFlags.Left);
                y += 15;
                TextRenderer.DrawText(g, "• 유닛 공수 카드: 선택 후 유효 구역 타일을 직접 클릭하여 무료 전술 즉시 배치!", bodyFont, new Rectangle(cardX + 24, y, textW, 15), Color.White, TextFormatFlags.Left);

                var closeRect = new Rectangle(cardX + (cardW - 130) / 2, cardY + cardH - 38, 130, 28);
                using (var cBrush = new SolidBrush(Color.FromArgb(35, 45, 65)))
                using (var cPen = new Pen(_colorCyan, 1.5f))
                {
                    g.FillRoundedRectangle(cBrush, closeRect, 5);
                    g.DrawRoundedRectangle(cPen, closeRect, 5);
                }
                TextRenderer.DrawText(g, "확인 (ESC / 닫기)", hFont, closeRect, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private void DrawAchievementsModal(Graphics g)
        {
            using (var dimBrush = new SolidBrush(Color.FromArgb(235, 6, 9, 15)))
            {
                g.FillRectangle(dimBrush, this.ClientRectangle);
            }

            int cardW = Math.Min(780, this.Width - 40);
            int cardH = Math.Min(560, this.Height - 40);
            int cardX = (this.Width - cardW) / 2;
            int cardY = (this.Height - cardH) / 2;
            var cardRect = new Rectangle(cardX, cardY, cardW, cardH);

            using (var brush = new SolidBrush(Color.FromArgb(18, 24, 34)))
            using (var pen = new Pen(_colorGold, 2f))
            {
                g.FillRoundedRectangle(brush, cardRect, 10);
                g.DrawRoundedRectangle(pen, cardRect, 10);
            }

            var achievements = Dashboard.DashboardManager.GetCoreFrontAchievements();
            int totalCount = achievements.Count;
            int unlockedCount = 0;
            int totalAp = 0;
            int earnedAp = 0;

            foreach (var ach in achievements)
            {
                totalAp += ach.Points;
                if (Dashboard.DashboardManager.IsCoreFrontAchievementUnlocked(ach.Id))
                {
                    unlockedCount++;
                    earnedAp += ach.Points;
                }
            }

            float percent = (totalCount > 0) ? ((float)unlockedCount / totalCount * 100f) : 0f;

            using (var titleFont = new Font("Segoe UI", 12.5f, FontStyle.Bold))
            using (var subFont = new Font("Segoe UI", 8.5f))
            using (var achTitleFont = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (var achDescFont = new Font("Segoe UI", 7.5f))
            using (var badgeFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            {
                // 타이틀
                TextRenderer.DrawText(g, "◈ CORE FRONT 작전 도전과제 (Steam Achievements) ◈", titleFont,
                    new Rectangle(cardX, cardY + 12, cardW, 24), _colorGold, TextFormatFlags.HorizontalCenter);

                // 진행도 요약
                string summaryText = $"진척도: {unlockedCount} / {totalCount}개 ({percent:F0}%)  •  달성 점수: {earnedAp} / {totalAp} AP  •  티어: {Dashboard.DashboardManager.GetCategoryTiers().Find(x => x.Category == Dashboard.AchievementCategory.CoreFront)?.CurrentTierName}";
                TextRenderer.DrawText(g, summaryText, subFont,
                    new Rectangle(cardX, cardY + 36, cardW, 16), Color.FromArgb(180, 210, 240), TextFormatFlags.HorizontalCenter);

                // 미니 네온 진행 바
                int barX = cardX + 30;
                int barY = cardY + 56;
                int barW = cardW - 60;
                int barH = 5;
                using (var trackBrush = new SolidBrush(Color.FromArgb(40, 50, 70)))
                {
                    g.FillRoundedRectangle(trackBrush, new Rectangle(barX, barY, barW, barH), 2);
                }
                int fillW = (int)(barW * (percent / 100f));
                if (fillW > 0)
                {
                    using (var fillBrush = new SolidBrush(_colorGold))
                    {
                        g.FillRoundedRectangle(fillBrush, new Rectangle(barX, barY, fillW, barH), 2);
                    }
                }

                // 14개 카드 (2열 x 7행)
                int gridStartY = cardY + 68;
                int colW = (cardW - 54) / 2;
                int rowH = 52;
                int gapY = 6;

                for (int i = 0; i < achievements.Count && i < 14; i++)
                {
                    var ach = achievements[i];
                    bool isUnlocked = Dashboard.DashboardManager.IsCoreFrontAchievementUnlocked(ach.Id);

                    int colIdx = i / 7;
                    int rowIdx = i % 7;
                    int itemX = cardX + 22 + colIdx * (colW + 10);
                    int itemY = gridStartY + rowIdx * (rowH + gapY);
                    var itemRect = new Rectangle(itemX, itemY, colW, rowH);

                    Color bgCol = isUnlocked ? Color.FromArgb(24, 34, 48) : Color.FromArgb(14, 18, 26);
                    Color borderCol = isUnlocked ? Color.FromArgb(255, 200, 50) : Color.FromArgb(42, 52, 68);
                    Color barCol = isUnlocked ? Color.FromArgb(0, 230, 118) : Color.FromArgb(60, 70, 85);

                    using (var itemBg = new SolidBrush(bgCol))
                    using (var itemPen = new Pen(borderCol, isUnlocked ? 1.2f : 1f))
                    {
                        g.FillRoundedRectangle(itemBg, itemRect, 6);
                        g.DrawRoundedRectangle(itemPen, itemRect, 6);
                    }

                    // 좌측 액센트 바
                    using (var aBrush = new SolidBrush(barCol))
                    {
                        g.FillRoundedRectangle(aBrush, new Rectangle(itemX + 2, itemY + 4, 3, rowH - 8), 1);
                    }

                    // 아이콘
                    string icon = isUnlocked ? ach.IconSymbol : "🔒";
                    TextRenderer.DrawText(g, icon, titleFont, new Rectangle(itemX + 8, itemY + 12, 28, 28),
                        isUnlocked ? Color.White : Color.Gray, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                    // 타이틀 & 설명
                    Color titleColor = isUnlocked ? _colorGold : Color.FromArgb(170, 175, 185);
                    Color descColor = isUnlocked ? Color.FromArgb(210, 225, 240) : Color.FromArgb(100, 110, 125);

                    int textLeft = itemX + 38;
                    int textWidth = colW - 120;
                    TextRenderer.DrawText(g, ach.Title, achTitleFont, new Rectangle(textLeft, itemY + 8, textWidth, 18),
                        titleColor, TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
                    TextRenderer.DrawText(g, ach.Description, achDescFont, new Rectangle(textLeft, itemY + 28, textWidth, 18),
                        descColor, TextFormatFlags.Left | TextFormatFlags.EndEllipsis);

                    // 우측 뱃지
                    var badgeRect = new Rectangle(itemX + colW - 74, itemY + 14, 66, 24);
                    Color badgeBg = isUnlocked ? Color.FromArgb(15, 60, 40) : Color.FromArgb(25, 30, 40);
                    Color badgeTextCol = isUnlocked ? Color.FromArgb(0, 230, 118) : Color.FromArgb(120, 130, 145);
                    using (var bBrush = new SolidBrush(badgeBg))
                    using (var bPen = new Pen(badgeTextCol, 1f))
                    {
                        g.FillRoundedRectangle(bBrush, badgeRect, 4);
                        g.DrawRoundedRectangle(bPen, badgeRect, 4);
                    }
                    string badgeText = isUnlocked ? $"✔ +{ach.Points}P" : $"{ach.Points} AP";
                    TextRenderer.DrawText(g, badgeText, badgeFont, badgeRect, badgeTextCol,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }

                // 닫기 버튼
                var closeRect = new Rectangle(cardX + (cardW - 140) / 2, cardY + cardH - 34, 140, 26);
                using (var cBrush = new SolidBrush(Color.FromArgb(35, 45, 65)))
                using (var cPen = new Pen(_colorGold, 1.5f))
                {
                    g.FillRoundedRectangle(cBrush, closeRect, 5);
                    g.DrawRoundedRectangle(cPen, closeRect, 5);
                }
                TextRenderer.DrawText(g, "확인 (ESC / 닫기)", subFont, closeRect, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        #endregion

        #region Map Size Selection Modal

        private void DrawMapSizeSelectModal(Graphics g)
        {
            // 1. 전체 화면 어두운 반투명 딤 처리
            using (var dimBrush = new SolidBrush(Color.FromArgb(225, 6, 10, 18)))
            {
                g.FillRectangle(dimBrush, this.ClientRectangle);
            }

            // 2. 모달 컨테이너 박스
            var modalRect = _mapSelectModalRect;
            using (var bgBrush = new SolidBrush(Color.FromArgb(18, 23, 34)))
            using (var borderPen = new Pen(Color.FromArgb(0, 180, 216), 2f))
            {
                g.FillRoundedRectangle(bgBrush, modalRect, 12);
                g.DrawRoundedRectangle(borderPen, modalRect, 12);
            }

            // 헤더 바 장식 라인
            using (var hBrush = new SolidBrush(Color.FromArgb(40, 0, 180, 216)))
            {
                g.FillRoundedRectangle(hBrush, new Rectangle(modalRect.X + 2, modalRect.Y + 2, modalRect.Width - 4, 60), 10);
            }

            // 3. 타이틀 & 서브타이틀
            using (var titleFont = new Font("Segoe UI", 13.5f, FontStyle.Bold))
            using (var subFont = new Font("Segoe UI", 9.5f))
            {
                TextRenderer.DrawText(g, "🌐 작전 구역 전장 규모 선택 (OPERATION THEATER SELECT)", titleFont,
                    new Rectangle(modalRect.X, modalRect.Y + 10, modalRect.Width, 26), _colorGold,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                TextRenderer.DrawText(g, "투입할 전장의 규모를 선택하십시오. (규모에 맞춰 작전창 크기가 자동 최적화됩니다)", subFont,
                    new Rectangle(modalRect.X, modalRect.Y + 36, modalRect.Width, 20), Color.FromArgb(200, 220, 240),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            // 4. 4대 전장 선택 카드 (소형 / 표준 / 대형 / 초대형)
            string[] titles = { "⚡ 소형 국지전", "🛡️ 표준 정규전", "★ 대형 전면전", "💥 초대형 결전" };
            string[] tags = { "12 × 6 TACTICAL", "14 × 8 RECOMMENDED", "18 × 8 EPIC WAR", "20 × 10 TITAN BATTLE" };
            string[] subs = { "[속전속결 기동전]", "[밸런스 공방전 (권장)]", "[광역 총력전]", "[초장거리 10라인 결전]" };
            string[] descs = {
                "• 전선 규모: 12열 × 6라인\n• 교전 거리: 초근접 격돌\n• 특징: 빠른 템포 & 스피디 승부\n• 추천: 기동 침투 기습전\n• 최적 화면: 720 × 680",
                "• 전선 규모: 14열 × 8라인\n• 교전 거리: 표준 전술 공방전\n• 특징: 모든 유닛/포탑 정석 밸런스\n• 추천: 표준 사령관 전략\n• 최적 화면: 820 × 720",
                "• 전선 규모: 18열 × 8라인\n• 교전 거리: 광역 와이드 전선\n• 특징: 다수 자원 노드 & 대군단\n• 추천: 천궁 공성 & 총력 결전\n• 최적 화면: 1000 × 720",
                "• 전선 규모: 20열 × 10라인\n• 환경: 5대 랜덤 작전 테마 🎲\n  (서버룸/용광로/하이웨이/나노샘/스모크)\n• 특전: 초기 발전소 2기 & 한도 5기\n• 최적 화면: 1060 × 800"
            };
            Color[] accentColors = { _colorEmerald, _colorCyan, _colorGold, Color.FromArgb(255, 100, 220) };
            Color[] cardBgs = { Color.FromArgb(22, 28, 42), Color.FromArgb(22, 30, 46), Color.FromArgb(26, 28, 38), Color.FromArgb(28, 24, 42) };

            for (int i = 0; i < 4; i++)
            {
                var card = _mapSelectCardRects[i];
                var btn = _mapSelectBtnRects[i];
                bool isHover = _hoverMapSelectIndex == i;
                Color accent = accentColors[i];

                Color bg = isHover ? Color.FromArgb(32, 42, 64) : cardBgs[i];
                float bWidth = isHover ? 2.5f : 1.2f;

                using (var cBrush = new SolidBrush(bg))
                using (var cPen = new Pen(isHover ? accent : Color.FromArgb(60, 75, 100), bWidth))
                {
                    g.FillRoundedRectangle(cBrush, card, 8);
                    g.DrawRoundedRectangle(cPen, card, 8);
                }

                // 상단 뱃지
                int badgeW = Math.Min(136, card.Width - 12);
                var badgeRect = new Rectangle(card.X + (card.Width - badgeW) / 2, card.Y + 10, badgeW, 18);
                using (var badgeBrush = new SolidBrush(Color.FromArgb(40, accent)))
                using (var badgePen = new Pen(accent, 1f))
                {
                    g.FillRoundedRectangle(badgeBrush, badgeRect, 4);
                    g.DrawRoundedRectangle(badgePen, badgeRect, 4);
                }
                using (var badgeFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
                {
                    TextRenderer.DrawText(g, tags[i], badgeFont, badgeRect, accent,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }

                // 타이틀 & 서브타이틀
                using (var cTitleFont = new Font("Segoe UI", 11f, FontStyle.Bold))
                using (var cSubFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                using (var cDescFont = new Font("Segoe UI", 8f))
                {
                    TextRenderer.DrawText(g, titles[i], cTitleFont, new Rectangle(card.X, card.Y + 32, card.Width, 22),
                        accent, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                    TextRenderer.DrawText(g, subs[i], cSubFont, new Rectangle(card.X, card.Y + 54, card.Width, 16),
                        Color.FromArgb(170, 195, 220), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                    // 본문 설명 박스
                    var descRect = new Rectangle(card.X + 8, card.Y + 74, card.Width - 16, 138);
                    TextRenderer.DrawText(g, descs[i], cDescFont, descRect, Color.FromArgb(200, 215, 230),
                        TextFormatFlags.Left | TextFormatFlags.WordBreak);
                }

                // 하단 진입 버튼
                using (var btnBrush = new SolidBrush(isHover ? accent : Color.FromArgb(35, 45, 65)))
                using (var btnPen = new Pen(accent, 1.2f))
                {
                    g.FillRoundedRectangle(btnBrush, btn, 5);
                    g.DrawRoundedRectangle(btnPen, btn, 5);
                }
                using (var btnFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                {
                    Color btnText = isHover ? Color.FromArgb(10, 15, 24) : accent;
                    string keyHint = $"[작전 투입] (단축키 {i + 1})";
                    TextRenderer.DrawText(g, keyHint, btnFont, btn, btnText,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            }

            // 하단 안내
            using (var tipFont = new Font("Segoe UI", 8.5f))
            {
                TextRenderer.DrawText(g, "💡 팁: 원하는 카드를 클릭하거나 키보드 [1], [2], [3], [4] 키를 누르면 즉시 작전이 시작됩니다.", tipFont,
                    new Rectangle(modalRect.X, modalRect.Bottom - 26, modalRect.Width, 20), Color.FromArgb(140, 165, 190),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        #endregion

    }

    #region Internal Effect Structures

    internal class ShockwaveRing
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Radius { get; set; }
        public float MaxRadius { get; set; }
        public float Alpha { get; set; }
        public Color WaveColor { get; set; }
    }

    internal class CombatFloatingText
    {
        public string Text { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Alpha { get; set; }
        public Color TextColor { get; set; }
    }

    #endregion

    #region Drawing Extensions

    internal static class GraphicsExtensions
    {
        public static void FillRoundedRectangle(this Graphics g, Brush brush, Rectangle bounds, int cornerRadius)
        {
            using (GraphicsPath path = CreateRoundedRectanglePath(bounds, cornerRadius))
            {
                g.FillPath(brush, path);
            }
        }

        public static void DrawRoundedRectangle(this Graphics g, Pen pen, Rectangle bounds, int cornerRadius)
        {
            using (GraphicsPath path = CreateRoundedRectanglePath(bounds, cornerRadius))
            {
                g.DrawPath(pen, path);
            }
        }

        private static GraphicsPath CreateRoundedRectanglePath(Rectangle bounds, int cornerRadius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = cornerRadius * 2;
            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(bounds.Location, size);

            if (bounds.Width <= 0 || bounds.Height <= 0)
                return path;

            // Top-left
            path.AddArc(arc, 180, 90);

            // Top-right
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);

            // Bottom-right
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // Bottom-left
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }
    }

    #endregion
}
