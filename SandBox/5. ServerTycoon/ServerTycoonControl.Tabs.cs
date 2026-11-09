using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace GateHelper.ServerTycoon
{
    public partial class ServerTycoonControl : UserControl
    {
        private void DrawTechTreeTab(Graphics g, int w, int topY)
        {
            // 상단 환생 배너 & 액션 버튼
            int claimable = _state.CalculatePrestigeTokens();
            Rectangle presBannerRect = new Rectangle(10, topY + 4, w - 20, 44);
            using (SolidBrush bannerBg = new SolidBrush(Color.FromArgb(28, 22, 42)))
            using (Pen bannerPen = new Pen(Color.FromArgb(179, 136, 255), 1))
            {
                g.FillRectangle(bannerBg, presBannerRect);
                g.DrawRectangle(bannerPen, presBannerRect);
            }

            using (Font tFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (SolidBrush tokenBrush = new SolidBrush(Color.FromArgb(225, 205, 255)))
            {
                string statusInfo;
                if (claimable > 0)
                {
                    statusInfo = $"회차 수익: ${FormatCompactNumber(_state.CurrentRunEarned)} ➔ ◈ +{claimable}개 토큰 수령 가능!";
                }
                else
                {
                    double remain = Math.Max(0, 1000000.0 - _state.CurrentRunEarned);
                    statusInfo = $"회차 수익: ${FormatCompactNumber(_state.CurrentRunEarned)} (첫 토큰까지: ${FormatCompactNumber(remain)})";
                }

                string info = $"◈ 보유 연구 토큰: {_state.ResearchTokens}개 (환생 배율: x{_state.GetPrestigeMultiplier():F2})\n{statusInfo}";
                g.DrawString(info, tFont, tokenBrush, 22, topY + 7);
            }

            // [☁ 마이그레이션 실행] 버튼
            int pBtnW = 145;
            int pBtnH = 32;
            int px = presBannerRect.Right - pBtnW - 6;
            int py = presBannerRect.Y + 6;
            _prestigeActionBtnRect = new Rectangle(px, py, pBtnW, pBtnH);

            bool canPrestige = claimable >= 1;
            Color pBg = canPrestige ? Color.FromArgb(120, 60, 200) : Color.FromArgb(36, 32, 46);
            Color pBorder = canPrestige ? Color.FromArgb(220, 180, 255) : Color.FromArgb(50, 46, 62);

            using (SolidBrush pBrush = new SolidBrush(pBg))
            using (Pen pPen = new Pen(pBorder, canPrestige ? 1.5f : 1f))
            using (Font bFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (SolidBrush tBrush = new SolidBrush(canPrestige ? Color.White : Color.FromArgb(120, 115, 135)))
            {
                g.FillRectangle(pBrush, _prestigeActionBtnRect);
                g.DrawRectangle(pPen, _prestigeActionBtnRect);
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                string btnText = canPrestige ? $"★ 환생 (+{claimable})" : "수익 부족 (환생 불가)";
                g.DrawString(btnText, bFont, tBrush, _prestigeActionBtnRect, sf);
            }

            // 6종 영구 스킬 카드 2열 3행
            var skills = TycoonGameState.GetSkillDefinitions();
            int cardPad = 8;
            int cardW = (w - 20 - cardPad) / 2;
            int cardH = 58;
            int startCardY = topY + 52;

            for (int i = 0; i < skills.Count; i++)
            {
                int col = i % 2;
                int row = i / 2;
                int cx = 10 + col * (cardW + cardPad);
                int cy = startCardY + row * (cardH + 4);

                _cardBounds[i] = new Rectangle(cx, cy, cardW, cardH);
                DrawSkillCard(g, _cardBounds[i], skills[i], i);
            }
        }

        private void DrawSkillCard(Graphics g, Rectangle rect, SkillNode skill, int index)
        {
            int currentLvl = _state.GetSkillLevel(skill.Id);
            bool isMax = currentLvl >= skill.MaxLevel;
            int nextCost = isMax ? 0 : skill.GetCostForLevel(currentLvl);
            bool canUpgrade = !isMax && _state.ResearchTokens >= nextCost;
            bool isHovered = _hoveredCardIndex == index;

            Color cBg = currentLvl > 0 ? Color.FromArgb(26, 32, 42) : Color.FromArgb(20, 22, 30);
            Color cBorder = isMax ? Color.FromArgb(0, 230, 118) : (isHovered ? Color.FromArgb(179, 136, 255) : Color.FromArgb(44, 48, 62));

            using (SolidBrush brush = new SolidBrush(cBg))
            using (Pen pen = new Pen(cBorder, isMax ? 1.5f : (isHovered ? 1.5f : 1f)))
            {
                g.FillRectangle(brush, rect);
                g.DrawRectangle(pen, rect);
            }

            // 고유 벡터 아이콘 뱃지 렌더링
            DrawUnitVectorIcon(g, new Rectangle(rect.X + 6, rect.Y + (rect.Height - 34) / 2, 34, 34), skill.Id, Color.FromArgb(179, 136, 255));

            using (Font nameFont = new Font("Segoe UI", 8f, FontStyle.Bold))
            using (Font lvlFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            using (Font descFont = new Font("Segoe UI", 7.2f))
            using (SolidBrush nameBrush = new SolidBrush(Color.FromArgb(235, 240, 255)))
            using (SolidBrush lvlBrush = new SolidBrush(isMax ? Color.FromArgb(0, 230, 118) : Color.FromArgb(179, 136, 255)))
            using (SolidBrush descBrush = new SolidBrush(Color.FromArgb(145, 160, 180)))
            {
                g.DrawString(skill.Name, nameFont, nameBrush, rect.X + 46, rect.Y + 6);
                string lvlText = isMax ? "[MAX]" : $"[Lv.{currentLvl}/{skill.MaxLevel}]";
                g.DrawString(lvlText, lvlFont, lvlBrush, rect.X + 46 + (int)g.MeasureString(skill.Name, nameFont).Width + 4, rect.Y + 6);

                string dynamicDesc = skill.GetLevelDescription(currentLvl);
                g.DrawString(dynamicDesc, descFont, descBrush, 
                    new Rectangle(rect.X + 46, rect.Y + 23, rect.Width - 120, 32));
            }

            // 우측 연구/보유 버튼
            int btnW = 68;
            int btnH = 38;
            int bx = rect.Right - btnW - 6;
            int by = rect.Y + (rect.Height - btnH) / 2;
            Rectangle buyBtnRect = new Rectangle(bx, by, btnW, btnH);
            _cardBuyButtonBounds[index] = buyBtnRect;

            if (isMax)
            {
                using (SolidBrush bBg = new SolidBrush(Color.FromArgb(25, 70, 45)))
                using (Font bFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
                using (SolidBrush tBrush = new SolidBrush(Color.FromArgb(120, 255, 180)))
                {
                    g.FillRectangle(bBg, buyBtnRect);
                    StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString("마스터\n(MAX)", bFont, tBrush, buyBtnRect, sf);
                }
            }
            else
            {
                Color bBg = canUpgrade ? Color.FromArgb(95, 40, 150) : Color.FromArgb(36, 32, 46);
                Color bBorder = canUpgrade ? Color.FromArgb(179, 136, 255) : Color.FromArgb(50, 46, 62);
                Color textCol = canUpgrade ? Color.White : Color.FromArgb(130, 125, 140);

                using (SolidBrush bBrush = new SolidBrush(bBg))
                using (Pen bPen = new Pen(bBorder, canUpgrade ? 1.5f : 1f))
                using (Font bFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
                using (SolidBrush tBrush = new SolidBrush(textCol))
                {
                    g.FillRectangle(bBrush, buyBtnRect);
                    g.DrawRectangle(bPen, buyBtnRect);
                    StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    string btnLabel = currentLvl == 0 ? $"연구 시작\n◈ {nextCost}개" : $"Lv.UP\n◈ {nextCost}개";
                    g.DrawString(btnLabel, bFont, tBrush, buyBtnRect, sf);
                }
            }
        }
        #region 1. S/W Studio Tab & Logic
        private void DrawSoftwareStudioTab(Graphics g, int w, int topY)
        {
            var studio = _state.SoftwareStudio;
            var categories = SoftwareStudioState.GetCategories();

            using (Font titleFont = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (SolidBrush titleBrush = new SolidBrush(Color.FromArgb(200, 220, 245)))
            {
                double royaltyRps = _state.GetSoftwareRoyaltyRps();
                string title = $"◈ 소프트웨어 개발실 (초당 로열티: +${royaltyRps:N1}/초 | 총 출시: {studio.TotalSoftwareShipped}건)";
                g.DrawString(title, titleFont, titleBrush, 14, topY + 6);
            }

            int leftW = (int)(w * 0.52);
            int rightX = leftW + 18;
            int rightW = w - rightX - 10;
            int panelY = topY + 28;

            if (studio.CurrentProject != null)
            {
                var prj = studio.CurrentProject;
                var def = categories.Find(c => c.Id == prj.CategoryId) ?? categories[0];

                Rectangle projBox = new Rectangle(10, panelY, leftW, 205);
                using (SolidBrush bg = new SolidBrush(Color.FromArgb(22, 28, 38)))
                using (Pen pen = new Pen(Color.FromArgb(0, 229, 255), 1.5f))
                {
                    g.FillRectangle(bg, projBox);
                    g.DrawRectangle(pen, projBox);
                }

                // 프로젝트 카테고리 벡터 뱃지
                Rectangle pIconRect = new Rectangle(projBox.Right - 44, panelY + 12, 34, 34);
                DrawSoftwareCategoryIcon(g, pIconRect, prj.CategoryId, ColorTranslator.FromHtml(def.AccentColorHex ?? "#00E5FF"), true);

                int textW = projBox.Width - 68;
                using (Font pFont = new Font("Segoe UI", 10.5f, FontStyle.Bold))
                using (Font subFont = new Font("Segoe UI", 7.8f))
                using (SolidBrush pBrush = new SolidBrush(Color.White))
                using (SolidBrush sBrush = new SolidBrush(Color.FromArgb(160, 180, 205)))
                using (StringFormat sfTitle = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
                {
                    g.DrawString($"▶ 개발 진행: {prj.Title}", pFont, pBrush, new Rectangle(22, panelY + 10, textW, 22), sfTitle);
                    g.DrawString($"분야: {def.Name} (권장: 디자인 {def.OptimalUx}% / 기능 {def.OptimalAlgo}% / 보안 {def.OptimalSec}%)", subFont, sBrush, new Rectangle(22, panelY + 34, textW, 18), sfTitle);
                }

                int barX = 22;
                int barY = panelY + 60;
                int barW = leftW - 44;
                int barH = 24;
                Rectangle barBgRect = new Rectangle(barX, barY, barW, barH);

                double pct = prj.ProgressPercent;
                int fillW = (int)((pct / 100.0) * barW);

                using (SolidBrush bBg = new SolidBrush(Color.FromArgb(14, 18, 24)))
                using (Pen bPen = new Pen(Color.FromArgb(40, 50, 70), 1))
                {
                    g.FillRectangle(bBg, barBgRect);
                    g.DrawRectangle(bPen, barBgRect);
                }

                if (fillW > 0)
                {
                    Color fillCol = prj.IsCompleted ? Color.FromArgb(0, 230, 118) : Color.FromArgb(0, 229, 255);
                    using (SolidBrush fillBrush = new SolidBrush(fillCol))
                    {
                        g.FillRectangle(fillBrush, barX, barY, fillW, barH);
                    }
                }

                using (Font progFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                using (SolidBrush progText = new SolidBrush(Color.White))
                {
                    StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    string progStr = prj.IsCompleted ? "★ 프로그램 개발 완료! (100.0%)" : $"{pct:F1}% ({prj.CurrentPoints:N0} / {prj.TargetPoints:N0} 개발력)";
                    g.DrawString(progStr, progFont, progText, barBgRect, sf);
                }

                double devRps = _state.CalculateTotalRps();
                double speed = Math.Max(25.0, devRps * 0.45);
                using (Font infoFont = new Font("Segoe UI", 8f, FontStyle.Bold))
                using (SolidBrush spdBrush = new SolidBrush(Color.FromArgb(255, 215, 64)))
                using (SolidBrush balBrush = new SolidBrush(Color.FromArgb(140, 160, 185)))
                {
                    g.DrawString($"⚡ 서버 개발 지원: +{speed:N0} 개발력/초", infoFont, spdBrush, 22, panelY + 92);
                    g.DrawString($"투자 비율: 디자인 {prj.FocusUx}%  |  핵심 기능 {prj.FocusAlgo}%  |  보안 {prj.FocusSec}%", infoFont, balBrush, 22, panelY + 110);
                }

                if (!prj.IsCompleted)
                {
                    _swCrunchBtnRect = new Rectangle(22, panelY + 140, barW, 42);
                    using (SolidBrush cBg = new SolidBrush(Color.FromArgb(36, 48, 70)))
                    using (Pen cPen = new Pen(Color.FromArgb(0, 229, 255), 1.5f))
                    using (Font cFont = new Font("Segoe UI", 9f, FontStyle.Bold))
                    using (SolidBrush cText = new SolidBrush(Color.White))
                    {
                        g.FillRectangle(cBg, _swCrunchBtnRect);
                        g.DrawRectangle(cPen, _swCrunchBtnRect);
                        StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        g.DrawString("⚡ 열정 부스트 코딩! (+250 개발력 즉시 추가)", cFont, cText, _swCrunchBtnRect, sf);
                    }
                }
                else
                {
                    _swLaunchBtnRect = new Rectangle(22, panelY + 140, barW, 42);
                    using (SolidBrush lBg = new SolidBrush(Color.FromArgb(0, 180, 80)))
                    using (Pen lPen = new Pen(Color.FromArgb(0, 255, 128), 2f))
                    using (Font lFont = new Font("Segoe UI", 9.2f, FontStyle.Bold))
                    using (SolidBrush lText = new SolidBrush(Color.Black))
                    {
                        g.FillRectangle(lBg, _swLaunchBtnRect);
                        g.DrawRectangle(lPen, _swLaunchBtnRect);
                        StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        g.DrawString("★ 글로벌 마켓 공식 출시! (보너스 & 수입 수령)", lFont, lText, _swLaunchBtnRect, sf);
                    }
                }
            }
            else
            {
                Rectangle planBox = new Rectangle(10, panelY, leftW, 205);
                using (SolidBrush bg = new SolidBrush(Color.FromArgb(20, 24, 32)))
                using (Pen pen = new Pen(Color.FromArgb(38, 46, 60), 1))
                {
                    g.FillRectangle(bg, planBox);
                    g.DrawRectangle(pen, planBox);
                }

                int catW = (leftW - 24) / 5;
                for (int i = 0; i < categories.Count; i++)
                {
                    int cx = 14 + i * (catW + 2);
                    _swCategoryBounds[i] = new Rectangle(cx, panelY + 8, catW, 58);
                    bool isSel = studio.SelectedCategoryIndex == i;

                    Color cBg = isSel ? Color.FromArgb(36, 48, 70) : Color.FromArgb(16, 20, 28);
                    Color cBorder = isSel ? Color.FromArgb(0, 229, 255) : Color.FromArgb(40, 48, 62);

                    using (SolidBrush cb = new SolidBrush(cBg))
                    using (Pen cp = new Pen(cBorder, isSel ? 1.5f : 1f))
                    using (Font catFont = new Font("Segoe UI", 7.2f, FontStyle.Bold))
                    using (SolidBrush ct = new SolidBrush(isSel ? Color.White : Color.FromArgb(140, 155, 175)))
                    {
                        g.FillRectangle(cb, _swCategoryBounds[i]);
                        g.DrawRectangle(cp, _swCategoryBounds[i]);

                        // 상단: 멈춰있는 고품질 GDI+ 절차적 벡터 아이콘
                        Rectangle iconRect = new Rectangle(cx + (catW - 26) / 2, panelY + 11, 26, 24);
                        DrawSoftwareCategoryIcon(g, iconRect, categories[i].Id, ColorTranslator.FromHtml(categories[i].AccentColorHex ?? "#00E5FF"), isSel);

                        // 하단: 간결한 카테고리 레이블
                        Rectangle nameRect = new Rectangle(cx + 1, panelY + 38, catW - 2, 17);
                        StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        g.DrawString(GetCategoryShortName(categories[i].Id), catFont, ct, nameRect, sf);
                    }
                }

                var selDef = categories[Math.Min(categories.Count - 1, studio.SelectedCategoryIndex)];

                using (Font subTitleFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                using (Font descFont = new Font("Segoe UI", 7.5f))
                using (SolidBrush wBrush = new SolidBrush(Color.FromArgb(230, 240, 255)))
                using (SolidBrush dBrush = new SolidBrush(Color.FromArgb(150, 170, 195)))
                {
                    g.DrawString($"[선택] {selDef.Name} ({selDef.SubTitle})", subTitleFont, wBrush, 16, panelY + 72);
                    g.DrawString($"개발비: ${selDef.UnlockCost:N0}  |  필요 개발력: {selDef.RequiredPoints:N0}  |  기본 수입: +${selDef.BaseRoyaltyPerSec:N0}/초", descFont, dBrush, 16, panelY + 90);
                }

                int sliderY = panelY + 114;
                DrawFactorStepper(g, 16, sliderY, "디자인", studio.SelectedCategoryIndex, 0, out _swUxMinusRect, out _swUxPlusRect);
                DrawFactorStepper(g, 16, sliderY + 24, "핵심 기능", studio.SelectedCategoryIndex, 1, out _swAlgoMinusRect, out _swAlgoPlusRect);
                DrawFactorStepper(g, 16, sliderY + 48, "보안 및 안정", studio.SelectedCategoryIndex, 2, out _swSecMinusRect, out _swSecPlusRect);

                _swStartDevBtnRect = new Rectangle(leftW - 130, panelY + 130, 120, 36);
                bool canAfford = _state.Money >= selDef.UnlockCost;
                Color sBg = canAfford ? Color.FromArgb(0, 150, 200) : Color.FromArgb(32, 36, 46);
                Color sBorder = canAfford ? Color.FromArgb(0, 229, 255) : Color.FromArgb(50, 56, 70);

                using (SolidBrush sb = new SolidBrush(sBg))
                using (Pen sp = new Pen(sBorder, canAfford ? 1.5f : 1f))
                using (Font sFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                using (SolidBrush st = new SolidBrush(canAfford ? Color.White : Color.FromArgb(120, 130, 145)))
                {
                    g.FillRectangle(sb, _swStartDevBtnRect);
                    g.DrawRectangle(sp, _swStartDevBtnRect);
                    StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString($"▶ 개발 시작\n(-${FormatCompactNumber(selDef.UnlockCost)})", sFont, st, _swStartDevBtnRect, sf);
                }
            }

            // 우측 패널: 출시된 S/W 마켓 현황 & 포트폴리오
            Rectangle rightBox = new Rectangle(rightX, panelY, rightW, 205);
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(18, 22, 30)))
            using (Pen pen = new Pen(Color.FromArgb(36, 44, 58), 1))
            {
                g.FillRectangle(bg, rightBox);
                g.DrawRectangle(pen, rightBox);
            }

            using (Font rTitleFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (SolidBrush rTitleBrush = new SolidBrush(Color.FromArgb(200, 215, 235)))
            {
                g.DrawString("◈ 출시된 프로그램 목록 (정기 수입)", rTitleFont, rTitleBrush, rightX + 12, panelY + 10);
            }

            if (studio.ReleasedProducts.Count == 0)
            {
                using (Font emptyFont = new Font("Segoe UI", 8f))
                using (SolidBrush emptyBrush = new SolidBrush(Color.FromArgb(110, 125, 145)))
                {
                    StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString("현재 서비스 중인 프로그램이 없습니다.\n좌측에서 새 프로그램을 기획하고 출시해 보세요!", emptyFont, emptyBrush, rightBox, sf);
                }
            }
            else
            {
                int itemH = 38;
                for (int i = 0; i < studio.ReleasedProducts.Count && i < 4; i++)
                {
                    var prod = studio.ReleasedProducts[i];
                    int iy = panelY + 34 + i * (itemH + 3);
                    Rectangle iRect = new Rectangle(rightX + 8, iy, rightW - 16, itemH);

                    using (SolidBrush ib = new SolidBrush(Color.FromArgb(24, 30, 42)))
                    using (Pen ip = new Pen(Color.FromArgb(45, 55, 75), 1))
                    using (Font nFont = new Font("Segoe UI", 8f, FontStyle.Bold))
                    using (Font starFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
                    using (Font subFont = new Font("Segoe UI", 7.2f))
                    using (SolidBrush nBrush = new SolidBrush(Color.White))
                    using (SolidBrush sBrush = new SolidBrush(Color.FromArgb(255, 215, 64)))
                    using (SolidBrush dBrush = new SolidBrush(Color.FromArgb(0, 230, 118)))
                    using (SolidBrush tBrush = new SolidBrush(Color.FromArgb(130, 145, 165)))
                    {
                        g.FillRectangle(ib, iRect);
                        g.DrawRectangle(ip, iRect);

                        g.DrawString(prod.Title, nFont, nBrush, iRect.X + 8, iRect.Y + 4);
                        g.DrawString($"★ {prod.Rating:F1}", starFont, sBrush, iRect.X + 8 + (int)g.MeasureString(prod.Title, nFont).Width + 6, iRect.Y + 4);
                        g.DrawString($"\"{prod.ReviewSummary}\"", subFont, tBrush, iRect.X + 8, iRect.Y + 20);

                        StringFormat rsf = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
                        Rectangle earnRect = new Rectangle(iRect.Right - 120, iRect.Y, 112, iRect.Height);
                        g.DrawString($"+${prod.RoyaltyPerSec:N1}/초\n(잔여 {prod.RemainingSecs}s)", subFont, dBrush, earnRect, rsf);
                    }
                }
            }
        }

        private void DrawFactorStepper(Graphics g, int x, int y, string label, int catIdx, int factorIdx, out Rectangle minusRect, out Rectangle plusRect)
        {
            minusRect = new Rectangle(x + 85, y, 20, 18);
            plusRect = new Rectangle(x + 145, y, 20, 18);

            int val = 33;
            var def = SoftwareStudioState.GetCategories()[catIdx];
            if (factorIdx == 0) val = def.OptimalUx;
            else if (factorIdx == 1) val = def.OptimalAlgo;
            else val = def.OptimalSec;

            using (Font f = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            using (SolidBrush tBrush = new SolidBrush(Color.FromArgb(170, 185, 205)))
            {
                g.DrawString(label, f, tBrush, x, y + 2);
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString($"{val}% (최적)", f, tBrush, new Rectangle(x + 70, y, 100, 18), sf);
            }
        }

        private void HandleSoftwareStudioClick(Point pt)
        {
            var studio = _state.SoftwareStudio;
            var categories = SoftwareStudioState.GetCategories();

            if (studio.CurrentProject != null)
            {
                var prj = studio.CurrentProject;
                if (!prj.IsCompleted && _swCrunchBtnRect.Contains(pt))
                {
                    prj.CurrentPoints += 250.0;
                    AddFloatingText("+250 개발력!", pt.X, pt.Y, Color.FromArgb(0, 229, 255), true);
                    Invalidate();
                    return;
                }
                else if (prj.IsCompleted && _swLaunchBtnRect.Contains(pt))
                {
                    var def = categories.Find(c => c.Id == prj.CategoryId) ?? categories[0];
                    int uxDiff = Math.Abs(prj.FocusUx - def.OptimalUx);
                    int algoDiff = Math.Abs(prj.FocusAlgo - def.OptimalAlgo);
                    int secDiff = Math.Abs(prj.FocusSec - def.OptimalSec);
                    double penalty = (uxDiff + algoDiff + secDiff) * 0.05;
                    double rating = Math.Max(3.5, Math.Min(10.0, 9.4 - penalty + (_rand.NextDouble() * 1.2 - 0.5)));
                    rating = Math.Round(rating, 1);

                    double royalty = Math.Round(def.BaseRoyaltyPerSec * (rating / 6.0) * _state.GetPrestigeMultiplier(), 1);
                    double launchBonus = Math.Round(def.UnlockCost * (rating / 4.0), 0);

                    _state.Money += launchBonus;
                    _state.CurrentRunEarned += launchBonus;
                    _state.TotalEarned += launchBonus;

                    string review = rating >= 8.5 ? "★ 역대급 완성도! 전 세계 유저 대호평!" : (rating >= 6.5 ? "준수한 기능과 안정적인 시장 반응" : "최적화와 UX 부족으로 아쉬운 평점");
                    studio.ReleasedProducts.Add(new ReleasedSoftware
                    {
                        Title = prj.Title,
                        CategoryName = def.Name,
                        Rating = rating,
                        ReviewSummary = review,
                        RoyaltyPerSec = royalty,
                        RemainingSecs = 240,
                        TotalEarned = 0
                    });

                    studio.TotalSoftwareShipped++;
                    studio.CurrentProject = null;
                    AddFloatingText($"★ 론칭 완료! 평점 {rating:F1}점! +${launchBonus:N0}", pt.X, pt.Y, Color.FromArgb(255, 215, 64), true);
                    Invalidate();
                    return;
                }
            }
            else
            {
                for (int i = 0; i < _swCategoryBounds.Length; i++)
                {
                    if (_swCategoryBounds[i].Contains(pt))
                    {
                        studio.SelectedCategoryIndex = i;
                        Invalidate();
                        return;
                    }
                }

                if (_swStartDevBtnRect.Contains(pt))
                {
                    var def = categories[Math.Min(categories.Count - 1, studio.SelectedCategoryIndex)];
                    if (_state.Money >= def.UnlockCost)
                    {
                        _state.Money -= def.UnlockCost;
                        studio.CurrentProject = new ActiveSoftwareProject
                        {
                            CategoryId = def.Id,
                            Title = $"{def.Name} v{studio.TotalSoftwareShipped + 1}.0",
                            TargetPoints = def.RequiredPoints,
                            CurrentPoints = 0,
                            FocusUx = def.OptimalUx,
                            FocusAlgo = def.OptimalAlgo,
                            FocusSec = def.OptimalSec
                        };
                        AddFloatingText($"▶ {def.Name} 개발 착수!", pt.X, pt.Y, Color.FromArgb(0, 229, 255), true);
                        Invalidate();
                    }
                    else
                    {
                        AddFloatingText($"착수금 부족! (${def.UnlockCost:N0} 필요)", pt.X, pt.Y, Color.FromArgb(255, 82, 82), false);
                    }
                    return;
                }
            }
        }
        #endregion

        #region 2. B2B Contracts Tab & Logic
        private void DrawB2BContractsTab(Graphics g, int w, int topY)
        {
            var b2b = _state.B2BMarket;
            if (b2b.AvailableOffers.Count == 0)
            {
                b2b.RefreshOffers(_rand, _state.CalculateTotalRps());
            }

            using (Font titleFont = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (SolidBrush titleBrush = new SolidBrush(Color.FromArgb(200, 220, 245)))
            using (Font tierFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (SolidBrush tierBrush = new SolidBrush(Color.FromArgb(255, 215, 64)))
            {
                string tierDesc = $"[{b2b.ReputationTier} RANK (신용도 {b2b.ReputationScore}점)]";
                g.DrawString($"★ 기업 외주 수주 계약실  |  내 서버실 신용 등급: ", titleFont, titleBrush, 14, topY + 6);
                int offset = (int)g.MeasureString("★ 기업 외주 수주 계약실  |  내 서버실 신용 등급: ", titleFont).Width + 14;
                g.DrawString(tierDesc, tierFont, tierBrush, offset, topY + 5);
            }

            _b2bRefreshBtnRect = new Rectangle(w - 95, topY + 4, 85, 22);
            using (SolidBrush refBg = new SolidBrush(Color.FromArgb(32, 42, 60)))
            using (Pen refPen = new Pen(Color.FromArgb(0, 229, 255), 1))
            using (Font refF = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            using (SolidBrush refT = new SolidBrush(Color.White))
            {
                g.FillRectangle(refBg, _b2bRefreshBtnRect);
                g.DrawRectangle(refPen, _b2bRefreshBtnRect);
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("● 새 의뢰 찾기", refF, refT, _b2bRefreshBtnRect, sf);
            }

            int panelY = topY + 28;

            if (b2b.ActiveContract != null)
            {
                var c = b2b.ActiveContract;
                Rectangle missionBox = new Rectangle(14, panelY, w - 28, 195);

                bool isWarning = c.ViolationsCount > 0;
                Color mBg = isWarning ? Color.FromArgb(32, 20, 26) : Color.FromArgb(20, 28, 38);
                Color mBorder = isWarning ? Color.FromArgb(255, 82, 82) : Color.FromArgb(0, 229, 255);

                using (SolidBrush bg = new SolidBrush(mBg))
                using (Pen pen = new Pen(mBorder, 2f))
                {
                    g.FillRectangle(bg, missionBox);
                    g.DrawRectangle(pen, missionBox);
                }

                using (Font clientFont = new Font("Segoe UI", 12f, FontStyle.Bold))
                using (Font subFont = new Font("Segoe UI", 8.5f))
                using (SolidBrush cBrush = new SolidBrush(Color.White))
                using (SolidBrush subBrush = new SolidBrush(Color.FromArgb(170, 190, 215)))
                {
                    g.DrawString($"▶ 계약 진행 중: {c.ClientName} - {c.Title}", clientFont, cBrush, 28, panelY + 16);
                    g.DrawString($"요구 서버 성능: {c.RequiredRps:N0}  |  계약 준수 조건: {c.SlaRuleDescription}", subFont, subBrush, 28, panelY + 42);
                }

                int barX = 28;
                int barY = panelY + 70;
                int barW = w - 56;
                int barH = 28;
                Rectangle barRect = new Rectangle(barX, barY, barW, barH);

                double timeRatio = (double)c.RemainingSecs / Math.Max(1, c.DurationSecs);
                int timeFill = (int)(timeRatio * barW);

                using (SolidBrush bBg = new SolidBrush(Color.FromArgb(14, 18, 24)))
                using (Pen bPen = new Pen(Color.FromArgb(45, 55, 75), 1))
                {
                    g.FillRectangle(bBg, barRect);
                    g.DrawRectangle(bPen, barRect);
                }

                if (timeFill > 0)
                {
                    Color fCol = isWarning ? Color.FromArgb(255, 120, 80) : Color.FromArgb(0, 230, 118);
                    using (SolidBrush fb = new SolidBrush(fCol))
                    {
                        g.FillRectangle(fb, barX, barY, timeFill, barH);
                    }
                }

                using (Font tFont = new Font("Segoe UI", 9f, FontStyle.Bold))
                using (SolidBrush tb = new SolidBrush(Color.White))
                {
                    StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString($"납기 완수까지: {c.RemainingSecs}초 남음 ({timeRatio * 100:F0}%)", tFont, tb, barRect, sf);
                }

                using (Font slaFont = new Font("Segoe UI", 9f, FontStyle.Bold))
                using (SolidBrush sBrush = new SolidBrush(c.ViolationsCount == 0 ? Color.FromArgb(0, 230, 118) : Color.FromArgb(255, 82, 82)))
                {
                    string slaText = c.ViolationsCount == 0 
                        ? "● 계약 준수 상태: 무결점 진행 중 (정상)" 
                        : $"▲ 계약 위반 경고: 누적 {c.ViolationsCount}회! (4회 이상 시 계약 파기 및 위약금 발생)";
                    g.DrawString(slaText, slaFont, sBrush, 28, panelY + 110);
                }

                using (Font rFont = new Font("Segoe UI", 10f, FontStyle.Bold))
                using (SolidBrush rBrush = new SolidBrush(Color.FromArgb(255, 215, 64)))
                {
                    g.DrawString($"성공 완수 보수: +${c.RewardCash:N0}  |  위약금: -${c.PenaltyCash:N0}", rFont, rBrush, 28, panelY + 135);
                }

                _b2bCancelBtnRect = new Rectangle(w - 140, panelY + 145, 110, 32);
                using (SolidBrush cBg = new SolidBrush(Color.FromArgb(60, 25, 30)))
                using (Pen cPen = new Pen(Color.FromArgb(200, 60, 70), 1))
                using (Font bFont = new Font("Segoe UI", 8f, FontStyle.Bold))
                using (SolidBrush tBrush = new SolidBrush(Color.FromArgb(255, 150, 160)))
                {
                    g.FillRectangle(cBg, _b2bCancelBtnRect);
                    g.DrawRectangle(cPen, _b2bCancelBtnRect);
                    StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString("계약 중도 파기", bFont, tBrush, _b2bCancelBtnRect, sf);
                }
            }
            else
            {
                int cardPad = 8;
                int cardW = (w - 20 - cardPad * 2) / 3;
                int cardH = 185;

                double currentRps = _state.CalculateTotalRps();

                for (int i = 0; i < b2b.AvailableOffers.Count && i < 3; i++)
                {
                    var offer = b2b.AvailableOffers[i];
                    int cx = 10 + i * (cardW + cardPad);
                    Rectangle oRect = new Rectangle(cx, panelY, cardW, cardH);
                    _b2bOfferBounds[i] = oRect;

                    bool canAccept = currentRps >= offer.RequiredRps;

                    using (SolidBrush ob = new SolidBrush(Color.FromArgb(20, 24, 34)))
                    using (Pen op = new Pen(ColorTranslator.FromHtml(offer.ClientColorHex ?? "#00E5FF"), 1.2f))
                    {
                        g.FillRectangle(ob, oRect);
                        g.DrawRectangle(op, oRect);
                    }

                    using (Font cNameFont = new Font("Segoe UI", 9f, FontStyle.Bold))
                    using (Font tierFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
                    using (SolidBrush cNameBrush = new SolidBrush(Color.White))
                    using (SolidBrush badgeBg = new SolidBrush(ColorTranslator.FromHtml(offer.ClientColorHex ?? "#00E5FF")))
                    using (SolidBrush badgeText = new SolidBrush(Color.Black))
                    {
                        Rectangle badgeRect = new Rectangle(oRect.X + 8, oRect.Y + 8, 20, 16);
                        g.FillRectangle(badgeBg, badgeRect);
                        StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        g.DrawString(offer.TierBadge, tierFont, badgeText, badgeRect, sf);

                        g.DrawString(offer.ClientName, cNameFont, cNameBrush, oRect.X + 32, oRect.Y + 8);
                    }

                    using (Font tFont = new Font("Segoe UI", 7.8f, FontStyle.Bold))
                    using (Font dFont = new Font("Segoe UI", 7.2f))
                    using (SolidBrush tBrush = new SolidBrush(Color.FromArgb(220, 235, 255)))
                    using (SolidBrush rpsBrush = new SolidBrush(canAccept ? Color.FromArgb(0, 230, 118) : Color.FromArgb(255, 82, 82)))
                    using (SolidBrush dBrush = new SolidBrush(Color.FromArgb(145, 160, 185)))
                    using (SolidBrush wColorBrush = new SolidBrush(Color.FromArgb(255, 200, 100)))
                    {
                        g.DrawString(offer.Title, tFont, tBrush, new Rectangle(oRect.X + 8, oRect.Y + 30, oRect.Width - 16, 32));
                        g.DrawString($"⚡ 필요 서버 성능: {offer.RequiredRps:N0}", dFont, rpsBrush, oRect.X + 8, oRect.Y + 64);
                        g.DrawString($"● 기간: {offer.DurationSecs}초  |  보수: +${FormatCompactNumber(offer.RewardCash)}", dFont, dBrush, oRect.X + 8, oRect.Y + 80);
                        g.DrawString($"● 조건: {offer.SlaRuleDescription}", dFont, wColorBrush, new Rectangle(oRect.X + 8, oRect.Y + 97, oRect.Width - 16, 36));
                    }

                    int btnH = 34;
                    Rectangle acceptRect = new Rectangle(oRect.X + 8, oRect.Bottom - btnH - 8, oRect.Width - 16, btnH);
                    _b2bAcceptBtnBounds[i] = acceptRect;

                    Color btnBg = canAccept ? Color.FromArgb(0, 140, 90) : Color.FromArgb(32, 36, 44);
                    Color btnBorder = canAccept ? Color.FromArgb(0, 230, 118) : Color.FromArgb(50, 56, 68);

                    using (SolidBrush bb = new SolidBrush(btnBg))
                    using (Pen bp = new Pen(btnBorder, canAccept ? 1.5f : 1f))
                    using (Font btnFont = new Font("Segoe UI", 8.0f, FontStyle.Bold))
                    using (SolidBrush bt = new SolidBrush(canAccept ? Color.White : Color.FromArgb(120, 130, 145)))
                    {
                        g.FillRectangle(bb, acceptRect);
                        g.DrawRectangle(bp, acceptRect);
                        StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        string bText = canAccept ? $"외주 계약 수락 (+${FormatCompactNumber(offer.RewardCash)})" : "서버 성능 부족";
                        g.DrawString(bText, btnFont, bt, acceptRect, sf);
                    }
                }
            }
        }

        private void HandleB2BContractsClick(Point pt)
        {
            var b2b = _state.B2BMarket;
            if (_b2bRefreshBtnRect.Contains(pt))
            {
                b2b.RefreshOffers(_rand, _state.CalculateTotalRps());
                AddFloatingText("새로운 의뢰 도착!", pt.X, pt.Y, Color.FromArgb(0, 229, 255), false);
                Invalidate();
                return;
            }

            if (b2b.ActiveContract != null)
            {
                if (_b2bCancelBtnRect.Contains(pt))
                {
                    b2b.ActiveContract = null;
                    AddFloatingText("계약 중도 취소", pt.X, pt.Y, Color.FromArgb(255, 82, 82), false);
                    Invalidate();
                    return;
                }
            }
            else
            {
                for (int i = 0; i < _b2bAcceptBtnBounds.Length && i < b2b.AvailableOffers.Count; i++)
                {
                    if (_b2bAcceptBtnBounds[i].Contains(pt))
                    {
                        var offer = b2b.AvailableOffers[i];
                        if (_state.CalculateTotalRps() >= offer.RequiredRps)
                        {
                            b2b.ActiveContract = offer;
                            b2b.AvailableOffers.RemoveAt(i);
                            AddFloatingText($"★ {offer.ClientName} 계약 체결!", pt.X, pt.Y, Color.FromArgb(0, 229, 255), true);
                        }
                        else
                        {
                            AddFloatingText("서버 성능 부족으로 수락 불가!", pt.X, pt.Y, Color.FromArgb(255, 82, 82), false);
                        }
                        Invalidate();
                        return;
                    }
                }
            }
        }
        #endregion

        #region 3. Crypto Market Tab & Logic
        private void DrawCryptoMarketTab(Graphics g, int w, int topY)
        {
            var crypto = _state.CryptoMarket;
            crypto.InitHistory();

            using (Font titleFont = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (Font tickerFont = new Font("Segoe UI", 10.5f, FontStyle.Bold))
            using (SolidBrush titleBrush = new SolidBrush(Color.FromArgb(200, 220, 245)))
            {
                bool isUp = crypto.PriceChangePercent >= 0;
                Color tickCol = isUp ? Color.FromArgb(0, 230, 118) : Color.FromArgb(255, 82, 82);
                string sign = isUp ? "▲ +" : "▼ ";

                g.DrawString("▲ 바이트코인(BYT) 거래소  |  현재 시세: ", titleFont, titleBrush, 14, topY + 6);
                int off = (int)g.MeasureString("▲ 바이트코인(BYT) 거래소  |  현재 시세: ", titleFont).Width + 14;
                using (SolidBrush tb = new SolidBrush(tickCol))
                {
                    g.DrawString($"${crypto.CurrentPrice:N2} ({sign}{crypto.PriceChangePercent:F2}%)", tickerFont, tb, off, topY + 4);
                }
            }

            int panelY = topY + 28;
            int chartW = (int)(w * 0.55);
            int rightX = chartW + 18;
            int rightW = w - rightX - 10;

            Rectangle chartBox = new Rectangle(10, panelY, chartW, 170);
            using (SolidBrush cBg = new SolidBrush(Color.FromArgb(16, 20, 28)))
            using (Pen cPen = new Pen(Color.FromArgb(36, 46, 62), 1))
            {
                g.FillRectangle(cBg, chartBox);
                g.DrawRectangle(cPen, chartBox);
            }

            using (Pen gridPen = new Pen(Color.FromArgb(26, 34, 46), 1))
            {
                for (int y = chartBox.Y + 20; y < chartBox.Bottom; y += 35)
                {
                    g.DrawLine(gridPen, chartBox.X, y, chartBox.Right, y);
                }
            }

            if (crypto.PriceHistory.Count >= 2)
            {
                double minP = double.MaxValue, maxP = double.MinValue;
                foreach (var p in crypto.PriceHistory)
                {
                    if (p < minP) minP = p;
                    if (p > maxP) maxP = p;
                }
                if (maxP - minP < 5.0) { minP -= 2.5; maxP += 2.5; }

                PointF[] points = new PointF[crypto.PriceHistory.Count];
                float stepX = (chartBox.Width - 40) / (float)(crypto.PriceHistory.Count - 1);

                for (int i = 0; i < crypto.PriceHistory.Count; i++)
                {
                    float px = chartBox.X + 20 + i * stepX;
                    double norm = (crypto.PriceHistory[i] - minP) / (maxP - minP);
                    float py = (float)(chartBox.Bottom - 25 - norm * (chartBox.Height - 50));
                    points[i] = new PointF(px, py);
                }

                bool isBull = crypto.CurrentPrice >= crypto.PriceHistory[0];
                Color lineColor = isBull ? Color.FromArgb(0, 230, 118) : Color.FromArgb(255, 82, 82);

                PointF[] fillPoly = new PointF[points.Length + 2];
                Array.Copy(points, fillPoly, points.Length);
                fillPoly[points.Length] = new PointF(points[points.Length - 1].X, chartBox.Bottom - 10);
                fillPoly[points.Length + 1] = new PointF(points[0].X, chartBox.Bottom - 10);

                using (SolidBrush areaBrush = new SolidBrush(Color.FromArgb(25, lineColor)))
                {
                    g.FillPolygon(areaBrush, fillPoly);
                }

                using (Pen lPen = new Pen(lineColor, 2f))
                {
                    g.DrawLines(lPen, points);
                }

                PointF lastPt = points[points.Length - 1];
                using (SolidBrush dotB = new SolidBrush(lineColor))
                using (Pen dotP = new Pen(Color.White, 1.5f))
                {
                    g.FillEllipse(dotB, lastPt.X - 4, lastPt.Y - 4, 8, 8);
                    g.DrawEllipse(dotP, lastPt.X - 4, lastPt.Y - 4, 8, 8);
                }
            }

            Rectangle newsBox = new Rectangle(10, chartBox.Bottom + 4, chartW, 26);
            using (SolidBrush nBg = new SolidBrush(Color.FromArgb(22, 28, 38)))
            using (Pen nPen = new Pen(Color.FromArgb(42, 54, 72), 1))
            using (Font nFont = new Font("Segoe UI", 7.5f))
            using (SolidBrush nBrush = new SolidBrush(Color.FromArgb(255, 200, 100)))
            {
                g.FillRectangle(nBg, newsBox);
                g.DrawRectangle(nPen, newsBox);
                StringFormat sf = new StringFormat { LineAlignment = StringAlignment.Center };
                g.DrawString($"[NEWS] {crypto.MarketNews}", nFont, nBrush, new Rectangle(newsBox.X + 8, newsBox.Y, newsBox.Width - 16, newsBox.Height), sf);
            }

            // 우측 메인 패널: 내 투자 현황판 & 매매 콘솔
            Rectangle rightBox = new Rectangle(rightX, panelY, rightW, 200);
            using (SolidBrush rBg = new SolidBrush(Color.FromArgb(20, 24, 32)))
            using (Pen rPen = new Pen(Color.FromArgb(38, 48, 62), 1))
            {
                g.FillRectangle(rBg, rightBox);
                g.DrawRectangle(rPen, rightBox);
            }

            // 1. [★ 내 투자 현황판] 요약 카드
            Rectangle cardRect = new Rectangle(rightX + 8, panelY + 6, rightW - 16, 78);
            using (SolidBrush cardBg = new SolidBrush(Color.FromArgb(26, 32, 44)))
            using (Pen cardPen = new Pen(Color.FromArgb(48, 60, 80), 1))
            {
                g.FillRectangle(cardBg, cardRect);
                g.DrawRectangle(cardPen, cardRect);
            }

            // 뱃지 아이콘 (우측 상단 장식)
            DrawCryptoCoinBadge(g, new Rectangle(cardRect.Right - 32, cardRect.Y + 6, 26, 26));

            double valDollars = crypto.TotalValuation;
            double profitDollars = crypto.UnrealizedProfit;
            double profitRate = crypto.ProfitRatePercent;
            bool hasCoins = crypto.MinedCoins > 0.000001;

            using (Font valFont = new Font("Segoe UI", 11.5f, FontStyle.Bold))
            using (Font profitFont = new Font("Segoe UI", 8.2f, FontStyle.Bold))
            using (Font infoFont = new Font("Segoe UI", 7.4f))
            using (Font labelFont = new Font("Segoe UI", 7f))
            using (SolidBrush goldBrush = new SolidBrush(Color.FromArgb(255, 215, 64)))
            using (SolidBrush labelBrush = new SolidBrush(Color.FromArgb(140, 155, 175)))
            using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(220, 230, 245)))
            {
                // 총 평가금액
                g.DrawString("내 코인 평가 금액", labelFont, labelBrush, cardRect.X + 8, cardRect.Y + 5);
                g.DrawString($"${FormatCompactNumber(valDollars)}", valFont, goldBrush, cardRect.X + 8, cardRect.Y + 16);

                // 평가 손익 및 수익률 (수익: 초록, 손실: 빨강, 무보유: 회색)
                int profitW = cardRect.Width - 148;
                using (StringFormat pSf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
                {
                    if (hasCoins)
                    {
                        bool isProfit = profitDollars >= 0;
                        Color pColor = isProfit ? Color.FromArgb(0, 230, 118) : Color.FromArgb(255, 82, 82);
                        string pSign = isProfit ? "+$" : "-$";
                        string rSign = isProfit ? "+" : "";
                        using (SolidBrush pb = new SolidBrush(pColor))
                        {
                            string profitStr = $"{pSign}{Math.Abs(profitDollars):N1} ({rSign}{profitRate:F1}%)";
                            g.DrawString(profitStr, profitFont, pb, new Rectangle(cardRect.X + 110, cardRect.Y + 18, profitW, 18), pSf);
                        }
                    }
                    else
                    {
                        using (SolidBrush pb = new SolidBrush(Color.FromArgb(120, 135, 150)))
                        {
                            g.DrawString("보유 코인 없음", profitFont, pb, new Rectangle(cardRect.X + 110, cardRect.Y + 18, profitW, 18), pSf);
                        }
                    }
                }

                // 구분선
                using (Pen sepPen = new Pen(Color.FromArgb(42, 52, 68), 1))
                {
                    g.DrawLine(sepPen, cardRect.X + 8, cardRect.Y + 39, cardRect.Right - 8, cardRect.Y + 39);
                }

                // 2열 상세 정보: [매수 평단가 / 투자 원금] & [보유 수량 / 채굴 속도]
                int col1X = cardRect.X + 8;
                int col2X = cardRect.X + (cardRect.Width / 2) + 2;

                string avgBuyStr = hasCoins 
                    ? (crypto.TotalInvestedUsd > 0.001 ? $"${crypto.AverageBuyPrice:N1}" : "$0.0 (채굴)")
                    : "-";
                string investedStr = crypto.TotalInvestedUsd > 0.001 ? $"${FormatCompactNumber(crypto.TotalInvestedUsd)}" : "$0";

                using (StringFormat infoSf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
                {
                    int colW = (cardRect.Width / 2) - 8;
                    g.DrawString($"매수 평단: {avgBuyStr}", infoFont, textBrush, new Rectangle(col1X, cardRect.Y + 43, colW, 16), infoSf);
                    g.DrawString($"투자 원금: {investedStr}", infoFont, textBrush, new Rectangle(col2X, cardRect.Y + 43, colW, 16), infoSf);

                    double rps = _state.CalculateTotalRps();
                    double mineRate = (crypto.MiningHashPercent > 0 && rps > 0)
                        ? (rps * (crypto.MiningHashPercent / 100.0)) / Math.Max(20.0, crypto.CurrentPrice * 2.2)
                        : 0.0;

                    g.DrawString($"보유: {crypto.MinedCoins:F4} BYT", infoFont, textBrush, new Rectangle(col1X, cardRect.Y + 59, colW, 16), infoSf);
                    g.DrawString($"채굴: +{mineRate:F4}/초", infoFont, textBrush, new Rectangle(col2X, cardRect.Y + 59, colW, 16), infoSf);
                }
            }

            // 2. 서버 채굴 전환 비율 버튼 (Y: panelY + 89)
            using (Font secFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            using (SolidBrush secBrush = new SolidBrush(Color.FromArgb(180, 200, 225)))
            {
                g.DrawString("⚡ 서버 자원 채굴 전환 비율:", secFont, secBrush, rightX + 12, panelY + 89);
            }

            int btnW = (rightW - 36) / 4;
            int btnY = panelY + 106;
            _cryptoHash0Rect = new Rectangle(rightX + 12, btnY, btnW, 20);
            _cryptoHash25Rect = new Rectangle(rightX + 12 + btnW + 4, btnY, btnW, 20);
            _cryptoHash50Rect = new Rectangle(rightX + 12 + (btnW + 4) * 2, btnY, btnW, 20);
            _cryptoHash100Rect = new Rectangle(rightX + 12 + (btnW + 4) * 3, btnY, btnW, 20);

            DrawToggleButton(g, _cryptoHash0Rect, "0%", crypto.MiningHashPercent == 0);
            DrawToggleButton(g, _cryptoHash25Rect, "25%", crypto.MiningHashPercent == 25);
            DrawToggleButton(g, _cryptoHash50Rect, "50%", crypto.MiningHashPercent == 50);
            DrawToggleButton(g, _cryptoHash100Rect, "100%", crypto.MiningHashPercent == 100);

            // 3. 간편 매수 & 매도 버튼 (2행 2열)
            int tBtnW = (rightW - 28) / 2;
            int row1Y = panelY + 132;
            int row2Y = panelY + 164;

            _cryptoBuy1kBtnRect = new Rectangle(rightX + 10, row1Y, tBtnW, 26);
            _cryptoBuyMaxBtnRect = new Rectangle(rightX + 18 + tBtnW, row1Y, tBtnW, 26);

            _cryptoSell50BtnRect = new Rectangle(rightX + 10, row2Y, tBtnW, 26);
            _cryptoSellAllBtnRect = new Rectangle(rightX + 18 + tBtnW, row2Y, tBtnW, 26);

            DrawTradingButton(g, _cryptoBuy1kBtnRect, "▲ $1,000 매수", Color.FromArgb(24, 110, 220));
            DrawTradingButton(g, _cryptoBuyMaxBtnRect, "▲ 최대 풀매수 (MAX)", Color.FromArgb(30, 136, 229));

            DrawTradingButton(g, _cryptoSell50BtnRect, "▼ 50% 절반 매도", Color.FromArgb(0, 140, 90));
            DrawTradingButton(g, _cryptoSellAllBtnRect, "▼ 전량 매도 (100%)", Color.FromArgb(0, 180, 100));
        }

        private void DrawTradingButton(Graphics g, Rectangle rect, string text, Color accent)
        {
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(35, accent)))
            using (Pen pen = new Pen(accent, 1.2f))
            using (Font f = new Font("Segoe UI", 7.8f, FontStyle.Bold))
            using (SolidBrush tb = new SolidBrush(Color.White))
            {
                g.FillRectangle(bg, rect);
                g.DrawRectangle(pen, rect);
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(text, f, tb, rect, sf);
            }
        }

        private void DrawCryptoCoinBadge(Graphics g, Rectangle bounds)
        {
            int cx = bounds.X + bounds.Width / 2;
            int cy = bounds.Y + bounds.Height / 2;
            int radius = Math.Min(bounds.Width, bounds.Height) / 2;

            using (SolidBrush coinBg = new SolidBrush(Color.FromArgb(24, 32, 48)))
            using (Pen goldRing = new Pen(Color.FromArgb(255, 215, 64), 2f))
            using (Pen cyanInner = new Pen(Color.FromArgb(0, 229, 255), 1f))
            using (Font f = new Font("Segoe UI", 12f, FontStyle.Bold))
            using (SolidBrush bBrush = new SolidBrush(Color.FromArgb(255, 215, 64)))
            using (Pen hashPen = new Pen(Color.FromArgb(255, 215, 64), 1.5f))
            {
                Rectangle ringRect = new Rectangle(cx - radius + 2, cy - radius + 2, (radius - 2) * 2, (radius - 2) * 2);
                g.FillEllipse(coinBg, ringRect);
                g.DrawEllipse(goldRing, ringRect);
                g.DrawEllipse(cyanInner, ringRect.X + 3, ringRect.Y + 3, ringRect.Width - 6, ringRect.Height - 6);

                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("B", f, bBrush, cx, cy - 1, sf);
                g.DrawLine(hashPen, cx - 2, cy - radius + 5, cx - 2, cy - radius + 9);
                g.DrawLine(hashPen, cx + 2, cy - radius + 5, cx + 2, cy - radius + 9);
                g.DrawLine(hashPen, cx - 2, cy + radius - 9, cx - 2, cy + radius - 5);
                g.DrawLine(hashPen, cx + 2, cy + radius - 9, cx + 2, cy + radius - 5);
            }
        }

        private string GetCategoryShortName(int id)
        {
            switch (id)
            {
                case 0: return "인디 게임";
                case 1: return "업무 관리";
                case 2: return "인공지능";
                case 3: return "보안 백신";
                case 4: return "클라우드";
                default: return "프로그램";
            }
        }

        private void DrawSoftwareCategoryIcon(Graphics g, Rectangle bounds, int categoryId, Color accent, bool isSelected)
        {
            int cx = bounds.X + bounds.Width / 2;
            int cy = bounds.Y + bounds.Height / 2;

            using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(isSelected ? 60 : 30, accent)))
            using (Pen borderPen = new Pen(accent, isSelected ? 1.5f : 1f))
            {
                g.FillEllipse(bgBrush, bounds);
                g.DrawEllipse(borderPen, bounds);
            }

            switch (categoryId)
            {
                case 0:
                    // 🎮 인디 모바일 게임: 레트로 콘솔 게임패드
                    using (SolidBrush padBody = new SolidBrush(Color.FromArgb(45, 55, 75)))
                    using (Pen padEdge = new Pen(Color.FromArgb(255, 64, 129), 1f))
                    using (SolidBrush btnPink = new SolidBrush(Color.FromArgb(255, 64, 129)))
                    using (SolidBrush btnCyan = new SolidBrush(Color.FromArgb(0, 229, 255)))
                    using (Pen dpadPen = new Pen(Color.FromArgb(220, 230, 245), 1.2f))
                    {
                        Rectangle padRect = new Rectangle(cx - 8, cy - 5, 16, 10);
                        g.FillRectangle(padBody, padRect);
                        g.DrawRectangle(padEdge, padRect);

                        // 좌측 십자키 (+)
                        g.DrawLine(dpadPen, cx - 5, cy - 3, cx - 5, cy + 3);
                        g.DrawLine(dpadPen, cx - 8, cy, cx - 2, cy);

                        // 우측 버튼 2개
                        g.FillEllipse(btnPink, cx + 3, cy - 3, 3, 3);
                        g.FillEllipse(btnCyan, cx + 1, cy + 1, 3, 3);
                    }
                    break;

                case 1:
                    // 📊 B2B 전사 ERP: 상승 성장 차트 막대 & 트렌드 화살표
                    using (Pen baseLine = new Pen(Color.FromArgb(140, 160, 190), 1f))
                    using (SolidBrush bar1 = new SolidBrush(Color.FromArgb(68, 138, 255)))
                    using (SolidBrush bar2 = new SolidBrush(Color.FromArgb(0, 229, 255)))
                    using (SolidBrush bar3 = new SolidBrush(Color.FromArgb(0, 230, 118)))
                    using (Pen trendPen = new Pen(Color.FromArgb(255, 215, 64), 1.2f))
                    {
                        g.DrawLine(baseLine, cx - 7, cy + 5, cx + 7, cy + 5);
                        g.FillRectangle(bar1, cx - 6, cy + 1, 3, 4);
                        g.FillRectangle(bar2, cx - 1, cy - 3, 3, 8);
                        g.FillRectangle(bar3, cx + 4, cy - 6, 3, 11);

                        g.DrawLine(trendPen, cx - 6, cy + 1, cx + 5, cy - 5);
                        g.DrawLine(trendPen, cx + 5, cy - 5, cx + 2, cy - 5);
                        g.DrawLine(trendPen, cx + 5, cy - 5, cx + 5, cy - 2);
                    }
                    break;

                case 2:
                    // 🧠 생성형 AI 모델: 인공신경망 코어 & 시냅스 노드
                    using (Pen synapsePen = new Pen(Color.FromArgb(179, 136, 255), 1f))
                    using (SolidBrush coreNode = new SolidBrush(Color.White))
                    using (SolidBrush subNode = new SolidBrush(Color.FromArgb(179, 136, 255)))
                    {
                        PointF c0 = new PointF(cx, cy);
                        PointF p1 = new PointF(cx - 5, cy - 5);
                        PointF p2 = new PointF(cx + 5, cy - 5);
                        PointF p3 = new PointF(cx - 5, cy + 5);
                        PointF p4 = new PointF(cx + 5, cy + 5);

                        g.DrawLine(synapsePen, c0, p1);
                        g.DrawLine(synapsePen, c0, p2);
                        g.DrawLine(synapsePen, c0, p3);
                        g.DrawLine(synapsePen, c0, p4);

                        g.FillEllipse(subNode, p1.X - 2, p1.Y - 2, 4, 4);
                        g.FillEllipse(subNode, p2.X - 2, p2.Y - 2, 4, 4);
                        g.FillEllipse(subNode, p3.X - 2, p3.Y - 2, 4, 4);
                        g.FillEllipse(subNode, p4.X - 2, p4.Y - 2, 4, 4);
                        g.FillEllipse(coreNode, c0.X - 3, c0.Y - 3, 6, 6);
                    }
                    break;

                case 3:
                    // 🛡️ 제로트러스트 보안 백신: 방패 & 확인 체크마크
                    using (SolidBrush shieldBg = new SolidBrush(Color.FromArgb(0, 230, 118)))
                    using (Pen shieldBorder = new Pen(Color.FromArgb(220, 255, 240), 1.2f))
                    using (Pen checkPen = new Pen(Color.FromArgb(20, 24, 32), 1.5f))
                    {
                        Point[] shieldPts = {
                            new Point(cx - 6, cy - 6),
                            new Point(cx + 6, cy - 6),
                            new Point(cx + 6, cy + 1),
                            new Point(cx, cy + 7),
                            new Point(cx - 6, cy + 1)
                        };
                        g.FillPolygon(shieldBg, shieldPts);
                        g.DrawPolygon(shieldBorder, shieldPts);

                        g.DrawLine(checkPen, cx - 4, cy, cx - 1, cy + 3);
                        g.DrawLine(checkPen, cx - 1, cy + 3, cx + 4, cy - 2);
                    }
                    break;

                case 4:
                    // 🌌 분산 클라우드 OS: 클라우드 & 궤도 링
                    using (Pen orbitPen = new Pen(Color.FromArgb(255, 215, 64), 1.2f))
                    using (SolidBrush cloudBrush = new SolidBrush(Color.FromArgb(255, 235, 120)))
                    using (SolidBrush satNode = new SolidBrush(Color.FromArgb(0, 229, 255)))
                    {
                        g.FillEllipse(cloudBrush, cx - 6, cy - 3, 7, 7);
                        g.FillEllipse(cloudBrush, cx - 2, cy - 5, 8, 8);
                        g.FillEllipse(cloudBrush, cx + 2, cy - 2, 6, 6);

                        g.DrawEllipse(orbitPen, cx - 8, cy - 4, 16, 8);
                        g.FillEllipse(satNode, cx - 7, cy - 2, 3, 3);
                        g.FillEllipse(satNode, cx + 5, cy + 1, 3, 3);
                    }
                    break;
            }
        }

        private void HandleCryptoMarketClick(Point pt)
        {
            var crypto = _state.CryptoMarket;

            if (_cryptoHash0Rect.Contains(pt)) { crypto.MiningHashPercent = 0; Invalidate(); return; }
            if (_cryptoHash25Rect.Contains(pt)) { crypto.MiningHashPercent = 25; Invalidate(); return; }
            if (_cryptoHash50Rect.Contains(pt)) { crypto.MiningHashPercent = 50; Invalidate(); return; }
            if (_cryptoHash100Rect.Contains(pt)) { crypto.MiningHashPercent = 100; Invalidate(); return; }

            // 1. $1,000 매수 (자금 부족 시 남은 잔액 전액 매수)
            if (_cryptoBuy1kBtnRect.Contains(pt))
            {
                double spend = Math.Min(1000.0, _state.Money);
                if (spend >= 1.0)
                {
                    double coins = spend / crypto.CurrentPrice;
                    _state.Money -= spend;
                    crypto.MinedCoins += coins;
                    crypto.TotalInvestedUsd += spend;
                    AddFloatingText($"BYT 매수: +{coins:F2}개 (평단: ${crypto.AverageBuyPrice:N1})", pt.X, pt.Y, Color.FromArgb(0, 229, 255), true);
                    Invalidate();
                }
                else
                {
                    AddFloatingText("매수 자금 부족! ($1 이상 필요)", pt.X, pt.Y, Color.FromArgb(255, 82, 82), false);
                }
                return;
            }

            // 2. 최대 풀매수
            if (_cryptoBuyMaxBtnRect.Contains(pt))
            {
                double spend = _state.Money;
                if (spend >= 1.0)
                {
                    double coins = spend / crypto.CurrentPrice;
                    _state.Money = 0;
                    crypto.MinedCoins += coins;
                    crypto.TotalInvestedUsd += spend;
                    AddFloatingText($"BYT 풀매수: +{coins:F2}개 (평단: ${crypto.AverageBuyPrice:N1})", pt.X, pt.Y, Color.FromArgb(0, 229, 255), true);
                    Invalidate();
                }
                else
                {
                    AddFloatingText("매수 자금 부족!", pt.X, pt.Y, Color.FromArgb(255, 82, 82), false);
                }
                return;
            }

            // 3. 50% 절반 매도
            if (_cryptoSell50BtnRect.Contains(pt))
            {
                double sellRatio = 0.50;
                double coinsToSell = crypto.MinedCoins * sellRatio;
                if (coinsToSell > 0.00001)
                {
                    double proceeds = coinsToSell * crypto.CurrentPrice;
                    double costBasis = crypto.TotalInvestedUsd * sellRatio;
                    double profit = proceeds - costBasis;

                    crypto.MinedCoins -= coinsToSell;
                    crypto.TotalInvestedUsd = Math.Max(0, crypto.TotalInvestedUsd - costBasis);
                    if (crypto.MinedCoins < 0.00001)
                    {
                        crypto.MinedCoins = 0;
                        crypto.TotalInvestedUsd = 0;
                    }
                    crypto.TotalRealizedProfit += profit;
                    _state.Money += proceeds;
                    if (profit > 0)
                    {
                        _state.CurrentRunEarned += profit;
                    }
                    if (profit > 0)
                    {
                        _state.TotalEarned += profit;
                    }

                    string profitStr = (costBasis > 0.001) 
                        ? (profit >= 0 ? $"+${profit:N0} 수익" : $"-${Math.Abs(profit):N0} 손실")
                        : "+100% 순수익 (채굴분)";
                    Color fCol = profit >= 0 ? Color.FromArgb(0, 230, 118) : Color.FromArgb(255, 82, 82);
                    AddFloatingText($"50% 매도: +${proceeds:N0} ({profitStr})", pt.X, pt.Y, fCol, true);
                    Invalidate();
                }
                else
                {
                    AddFloatingText("매도할 코인이 없습니다!", pt.X, pt.Y, Color.FromArgb(255, 82, 82), false);
                }
                return;
            }

            // 4. 전량 매도
            if (_cryptoSellAllBtnRect.Contains(pt))
            {
                double coinsToSell = crypto.MinedCoins;
                if (coinsToSell > 0.00001)
                {
                    double proceeds = coinsToSell * crypto.CurrentPrice;
                    double costBasis = crypto.TotalInvestedUsd;
                    double profit = proceeds - costBasis;

                    crypto.MinedCoins = 0;
                    crypto.TotalInvestedUsd = 0;
                    crypto.TotalRealizedProfit += profit;
                    _state.Money += proceeds;
                    if (profit > 0)
                    {
                        _state.CurrentRunEarned += profit;
                    }
                    if (profit > 0)
                    {
                        _state.TotalEarned += profit;
                    }

                    string profitStr = (costBasis > 0.001) 
                        ? (profit >= 0 ? $"+${profit:N0} 수익" : $"-${Math.Abs(profit):N0} 손실")
                        : "+100% 순수익 (채굴분)";
                    Color fCol = profit >= 0 ? Color.FromArgb(0, 230, 118) : Color.FromArgb(255, 82, 82);
                    AddFloatingText($"전량 매도: +${proceeds:N0} ({profitStr})", pt.X, pt.Y, fCol, true);
                    Invalidate();
                }
                else
                {
                    AddFloatingText("매도할 코인이 없습니다!", pt.X, pt.Y, Color.FromArgb(255, 82, 82), false);
                }
                return;
            }
        }
        #endregion
    }
}
