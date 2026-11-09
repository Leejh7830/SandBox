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
        #region Rendering (GDI+ Canvas)

        private static readonly SolidBrush _brushSubTextDim = new SolidBrush(Color.FromArgb(120, 135, 155));

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            int w = Width;
            int h = Height;

            // 1. 전체 배경
            using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(15, 17, 23)))
            {
                g.FillRectangle(bgBrush, 0, 0, w, h);
            }

            // 2. 상단 서버실 모니터링 뷰 (Y: 0 ~ 205)
            DrawServerRoom(g, w, 205);

            // 3. 중앙 조작부 & HUD (Y: 205 ~ 320)
            DrawCenterConsole(g, w, 205, 115);

            // 4. 하단 4대 인프라 서브 탭 바 (Y: 320 ~ 348)
            DrawSubTabBar(g, w, 320, 28);

            // 5. 하단 탭별 상점 및 연구 뷰 (Y: 348 ~ h)
            DrawActiveTabContent(g, w, 348, h - 348);

            // 6. 오프라인 방치 보상 배너
            if (_showOfflineBanner)
            {
                DrawOfflineBanner(g, w);
            }

            // 7. 플로팅 텍스트
            DrawFloatingTexts(g);
        }

        private void DrawServerRoom(Graphics g, int w, int roomHeight)
        {
            using (LinearGradientBrush roomBg = new LinearGradientBrush(
                new Point(0, 0), new Point(0, roomHeight),
                Color.FromArgb(20, 24, 34), Color.FromArgb(12, 14, 20)))
            {
                g.FillRectangle(roomBg, 0, 0, w, roomHeight);
            }

            using (Pen borderPen = new Pen(Color.FromArgb(40, 48, 64), 1))
            {
                g.DrawLine(borderPen, 0, roomHeight, w, roomHeight);
            }

            // 바닥 그리드
            using (Pen floorPen = new Pen(Color.FromArgb(22, 28, 38), 1))
            {
                int floorY = 150;
                for (int x = -100; x < w + 100; x += 45)
                {
                    g.DrawLine(floorPen, x, floorY, x + (x - w / 2) * 2, roomHeight);
                }
                g.DrawLine(floorPen, 0, floorY, w, floorY);
            }

            // 상단 타이틀
            using (Font titleFont = new Font("Segoe UI", 10f, FontStyle.Bold))
            using (SolidBrush titleBrush = new SolidBrush(Color.FromArgb(200, 215, 235)))
            {
                string prestigeTag = _state.PrestigeCount > 0 ? $" [환생 +{_state.PrestigeCount}회]" : "";
                g.DrawString($"■ 내 서버실 통합 현황판{prestigeTag}", titleFont, titleBrush, 12, 10);
            }

            // 상단 미니 실시간 텔레메트리 (전력 & 온도)
            double supply = _state.GetTotalPowerSupply();
            double demand = _state.GetTotalPowerDemand();
            double temp = _state.GetCurrentTemperature();
            bool isOverheated = temp > 60.0;
            bool isOptimal = temp <= 35.0;

            using (Font teleFont = new Font("Segoe UI", 8f, FontStyle.Bold))
            using (SolidBrush pwrBrush = new SolidBrush(demand > supply ? Color.FromArgb(255, 82, 82) : Color.FromArgb(255, 167, 38)))
            using (SolidBrush tempBrush = new SolidBrush(isOverheated ? Color.FromArgb(255, 82, 82) : (isOptimal ? Color.FromArgb(0, 229, 255) : Color.FromArgb(76, 175, 80))))
            {
                string pwrText = $"⚡ {demand:N0}W / {supply:N0}W";
                string tempText = isOptimal ? $"● {temp:F1}°C (쾌적 +20%)" : (isOverheated ? $"▲ {temp:F1}°C (과열 -35%)" : $"● {temp:F1}°C");
                g.DrawString($"{pwrText}  |  {tempText}", teleFont, tempBrush, 14, 30);
            }

            // 뒤로가기 버튼 [◀ SandBox]
            _backButtonRect = new Rectangle(w - 95, 8, 85, 24);
            using (SolidBrush backBtnBg = new SolidBrush(Color.FromArgb(32, 38, 52)))
            using (Pen backBtnPen = new Pen(Color.FromArgb(60, 72, 96), 1))
            using (Font btnFont = new Font("Segoe UI", 8f, FontStyle.Bold))
            using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(170, 190, 215)))
            {
                g.FillRectangle(backBtnBg, _backButtonRect);
                g.DrawRectangle(backBtnPen, _backButtonRect);
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("◀ SandBox", btnFont, textBrush, _backButtonRect, sf);
            }

            // 중앙 허브
            float hubX = w / 2f;
            float hubY = 195f;
            using (SolidBrush hubBrush = new SolidBrush(Color.FromArgb(30, 42, 60)))
            using (Pen hubPen = new Pen(Color.FromArgb(0, 229, 255), 1.5f))
            {
                g.FillEllipse(hubBrush, hubX - 16, hubY - 8, 32, 16);
                g.DrawEllipse(hubPen, hubX - 16, hubY - 8, 32, 16);
            }

            // 6개 서버 랙 및 케이블
            int rackWidth = 84;
            int rackHeight = 120;
            int gap = 12;
            int totalRacksWidth = 6 * rackWidth + 5 * gap;
            int startX = (w - totalRacksWidth) / 2;
            int rackY = 48;

            for (int i = 0; i < 6; i++)
            {
                int rx = startX + i * (rackWidth + gap);
                _rackBounds[i] = new Rectangle(rx, rackY, rackWidth, rackHeight);

                var server = i < _state.Servers.Count ? _state.Servers[i] : null;
                bool isUnlocked = server != null && server.Level > 0;

                if (isUnlocked)
                {
                    using (Pen cablePen = new Pen(Color.FromArgb(45, server.AccentColor), 1.5f))
                    {
                        float rackBottomCenter = rx + rackWidth / 2f;
                        g.DrawLine(cablePen, rackBottomCenter, rackY + rackHeight, hubX, hubY);
                    }
                }
            }

            foreach (var p in _particles)
            {
                using (SolidBrush pBrush = new SolidBrush(p.ParticleColor))
                {
                    g.FillEllipse(pBrush, p.X - 2.5f, p.Y - 2.5f, 5, 5);
                }
            }

            for (int i = 0; i < 6; i++)
            {
                DrawSingleRack(g, _rackBounds[i], i);
            }

            if (_state.CurrentIncident != null)
            {
                DrawIncidentBanner(g, _state.CurrentIncident);
            }
        }

        private void DrawSingleRack(Graphics g, Rectangle rect, int index)
        {
            var server = index < _state.Servers.Count ? _state.Servers[index] : null;
            if (server == null) return;

            bool isUnlocked = server.Level > 0;
            bool isHovered = _hoveredRackIndex == index;

            Color chassisColor = isUnlocked ? Color.FromArgb(24, 28, 38) : Color.FromArgb(16, 18, 24);
            Color borderColor = isUnlocked 
                ? (isHovered ? server.AccentColor : Color.FromArgb(48, 58, 76)) 
                : Color.FromArgb(32, 36, 46);

            using (SolidBrush chassisBrush = new SolidBrush(chassisColor))
            using (Pen borderPen = new Pen(borderColor, isHovered ? 2f : 1f))
            {
                g.FillRectangle(chassisBrush, rect);
                g.DrawRectangle(borderPen, rect);
            }

            Rectangle headerRect = new Rectangle(rect.X, rect.Y, rect.Width, 20);
            using (SolidBrush headerBg = new SolidBrush(isUnlocked ? Color.FromArgb(32, 38, 52) : Color.FromArgb(20, 22, 28)))
            using (Font headerFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            using (SolidBrush headerText = new SolidBrush(isUnlocked ? Color.FromArgb(220, 235, 255) : Color.FromArgb(100, 110, 125)))
            {
                g.FillRectangle(headerBg, headerRect);
                using (SolidBrush dotBrush = new SolidBrush(isUnlocked ? server.AccentColor : Color.FromArgb(70, 80, 95)))
                {
                    g.FillEllipse(dotBrush, headerRect.X + 6, headerRect.Y + 7, 6, 6);
                }
                StringFormat sf = new StringFormat { LineAlignment = StringAlignment.Center };
                g.DrawString(GetShortName(server.Id), headerFont, headerText, 
                    new Rectangle(headerRect.X + 16, headerRect.Y, headerRect.Width - 18, headerRect.Height), sf);
            }

            if (!isUnlocked)
            {
                int lx = rect.X + rect.Width / 2;
                int ly = rect.Y + 36;
                using (Pen lockPen = new Pen(Color.FromArgb(90, 105, 125), 1.5f))
                using (SolidBrush lockBody = new SolidBrush(Color.FromArgb(36, 42, 54)))
                using (Font subFont = new Font("Segoe UI", 7f, FontStyle.Bold))
                using (SolidBrush lockBrush = new SolidBrush(Color.FromArgb(120, 135, 155)))
                {
                    // 자물쇠 고리 및 몸통
                    g.DrawArc(lockPen, lx - 5, ly - 9, 10, 10, 180, 180);
                    g.FillRectangle(lockBody, lx - 7, ly - 3, 14, 11);
                    g.DrawRectangle(lockPen, lx - 7, ly - 3, 14, 11);

                    StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString($"${FormatCompactNumber(server.BaseCost)}", subFont, lockBrush, new Rectangle(rect.X, rect.Y + 54, rect.Width, 18), sf);
                }
                return;
            }

            // 블레이드 슬롯 디테일
            int slotY = rect.Y + 24;
            int slotHeight = 16;
            int slotGap = 4;
            int filledSlots = Math.Min(4, Math.Max(1, (server.Level + 1) / 2));

            for (int s = 0; s < 4; s++)
            {
                int sy = slotY + s * (slotHeight + slotGap);
                Rectangle slotRect = new Rectangle(rect.X + 4, sy, rect.Width - 8, slotHeight);
                bool isSlotFilled = s < filledSlots;

                using (SolidBrush slotBrush = new SolidBrush(isSlotFilled ? Color.FromArgb(36, 42, 56) : Color.FromArgb(18, 20, 26)))
                using (Pen slotPen = new Pen(Color.FromArgb(44, 52, 68), 1))
                {
                    g.FillRectangle(slotBrush, slotRect);
                    g.DrawRectangle(slotPen, slotRect);
                }

                if (isSlotFilled)
                {
                    bool ledOn = ((_frameCounter + s * 4 + index * 3) % 18) < 11;
                    Color ledColor = ledOn ? server.AccentColor : Color.FromArgb(30, 45, 55);
                    using (SolidBrush ledBrush = new SolidBrush(ledColor))
                    {
                        g.FillEllipse(ledBrush, slotRect.X + 4, slotRect.Y + 5, 5, 5);
                    }
                }
            }

            // 하단 쿨링 팬 & 레벨 뱃지
            int fanSize = 20;
            int fanX = rect.X + 6;
            int fanY = rect.Bottom - fanSize - 4;
            DrawCoolingFan(g, fanX, fanY, fanSize, _fanAngle, server.AccentColor);

            using (SolidBrush badgeBg = new SolidBrush(Color.FromArgb(40, server.AccentColor)))
            using (Pen badgeBorder = new Pen(server.AccentColor, 1))
            using (Font badgeFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            using (SolidBrush badgeText = new SolidBrush(Color.White))
            {
                Rectangle badgeRect = new Rectangle(rect.Right - 46, rect.Bottom - 18, 40, 15);
                g.FillRectangle(badgeBg, badgeRect);
                g.DrawRectangle(badgeBorder, badgeRect);
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString($"Lv.{server.Level}", badgeFont, badgeText, badgeRect, sf);
            }
        }

        private void DrawCoolingFan(Graphics g, int x, int y, int size, float angle, Color accent)
        {
            Rectangle rect = new Rectangle(x, y, size, size);
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(18, 22, 30)))
            using (Pen border = new Pen(Color.FromArgb(50, 60, 80), 1))
            {
                g.FillEllipse(bg, rect);
                g.DrawEllipse(border, rect);
            }

            float cx = x + size / 2f;
            float cy = y + size / 2f;
            float r = size / 2f - 3;

            using (Pen fanPen = new Pen(Color.FromArgb(160, accent), 1.5f))
            {
                for (int b = 0; b < 4; b++)
                {
                    float a = (float)((angle + b * 90) * Math.PI / 180.0);
                    float ex = cx + (float)Math.Cos(a) * r;
                    float ey = cy + (float)Math.Sin(a) * r;
                    g.DrawLine(fanPen, cx, cy, ex, ey);
                }
            }
        }

        private void DrawIncidentBanner(Graphics g, TycoonIncident incident)
        {
            int targetIndex = incident.TargetServerIndex;
            if (targetIndex < 0 || targetIndex >= _rackBounds.Length) return;

            var rack = _rackBounds[targetIndex];
            int bw = 110;
            int bh = 32;
            int bx = rack.X + (rack.Width - bw) / 2;
            int by = Math.Max(4, rack.Y - bh - 6);

            bool flash = (_frameCounter % 10) < 5;
            Color bannerBg = flash ? Color.FromArgb(180, 20, 30) : Color.FromArgb(120, 15, 20);
            Color textCol = flash ? Color.Yellow : Color.White;

            Rectangle bRect = new Rectangle(bx, by, bw, bh);
            using (SolidBrush bBg = new SolidBrush(bannerBg))
            using (Pen bPen = new Pen(Color.FromArgb(255, 60, 80), 2))
            using (Font bFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            using (SolidBrush tBrush = new SolidBrush(textCol))
            {
                g.FillRectangle(bBg, bRect);
                g.DrawRectangle(bPen, bRect);

                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString($"{incident.Title}\n[클릭 퇴치! +${incident.RewardCash:N0}]", bFont, tBrush, bRect, sf);
            }
        }

        private void DrawCenterConsole(Graphics g, int w, int topY, int height)
        {
            Rectangle consoleRect = new Rectangle(10, topY + 4, w - 20, height - 8);
            using (SolidBrush panelBg = new SolidBrush(Color.FromArgb(20, 24, 32)))
            using (Pen borderPen = new Pen(Color.FromArgb(38, 46, 62), 1))
            {
                g.FillRectangle(panelBg, consoleRect);
                g.DrawRectangle(borderPen, consoleRect);
            }

            int leftPad = 24;
            using (Font moneyFont = new Font("Segoe UI", 17f, FontStyle.Bold))
            using (SolidBrush moneyBrush = new SolidBrush(Color.FromArgb(255, 215, 64)))
            {
                g.DrawString($"$ {FormatCompactNumber(_state.Money)}", moneyFont, moneyBrush, leftPad, topY + 12);
            }

            double totalRps = _state.CalculateTotalRps();
            double clickPower = _state.GetClickPower();
            using (Font statFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (SolidBrush rpsBrush = new SolidBrush(Color.FromArgb(0, 229, 255)))
            using (SolidBrush nodeBrush = new SolidBrush(Color.FromArgb(160, 180, 205)))
            {
                string feverTag = _state.IsFeverActive ? " [★ 2.5x FEVER]" : "";
                g.DrawString($"⚡ +${totalRps:N1}/sec{feverTag}", statFont, rpsBrush, leftPad, topY + 44);
                g.DrawString($"[가동 서버] {_state.GetTotalServerCount()}대  |  [클릭당] +${clickPower:N1}", statFont, nodeBrush, leftPad, topY + 63);
            }

            // 피버 바
            _feverBarRect = new Rectangle(leftPad, topY + 84, 230, 13);
            using (SolidBrush fBg = new SolidBrush(Color.FromArgb(30, 36, 48)))
            using (Pen fPen = new Pen(Color.FromArgb(50, 60, 80), 1))
            {
                g.FillRectangle(fBg, _feverBarRect);
                g.DrawRectangle(fPen, _feverBarRect);
            }

            float fillWidth = _feverBarRect.Width * (_state.FeverMeter / 100f);
            if (fillWidth > 0)
            {
                Rectangle fillRect = new Rectangle(_feverBarRect.X, _feverBarRect.Y, (int)fillWidth, _feverBarRect.Height);
                Color fillCol = _state.IsFeverActive ? Color.FromArgb(255, 60, 0) : Color.FromArgb(255, 150, 0);
                using (SolidBrush fillBrush = new SolidBrush(fillCol))
                {
                    g.FillRectangle(fillBrush, fillRect);
                }
            }

            using (Font fFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            using (SolidBrush textBrush = new SolidBrush(Color.White))
            {
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                string feverText = _state.IsFeverActive 
                    ? $"★ FEVER ACTIVE! ({_state.FeverRemainingSeconds:F0}s)" 
                    : $"연타 시 피버 발동! ({_state.FeverMeter:F0}%)";
                g.DrawString(feverText, fFont, textBrush, _feverBarRect, sf);
            }

            // 우측 [ ⚡ PING / DEPLOY ] 버튼
            int btnWidth = 210;
            int btnHeight = 72;
            int btnX = consoleRect.Right - btnWidth - 14;
            int btnY = topY + 14;
            _deployButtonRect = new Rectangle(btnX, btnY, btnWidth, btnHeight);

            Color btnBgCol = _isDeployHovered ? Color.FromArgb(0, 180, 215) : Color.FromArgb(0, 145, 180);
            if (_state.IsFeverActive)
            {
                btnBgCol = _isDeployHovered ? Color.FromArgb(255, 90, 30) : Color.FromArgb(220, 60, 20);
            }

            using (SolidBrush btnBrush = new SolidBrush(btnBgCol))
            using (Pen btnPen = new Pen(Color.FromArgb(120, 230, 255), _isDeployHovered ? 2f : 1f))
            {
                g.FillRectangle(btnBrush, _deployButtonRect);
                g.DrawRectangle(btnPen, _deployButtonRect);
            }

            using (Font btnTitleFont = new Font("Segoe UI", 11.5f, FontStyle.Bold))
            using (Font btnSubFont = new Font("Segoe UI", 8f, FontStyle.Regular))
            using (SolidBrush btnTextBrush = new SolidBrush(Color.White))
            {
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                Rectangle tRect = new Rectangle(_deployButtonRect.X, _deployButtonRect.Y + 10, _deployButtonRect.Width, 24);
                Rectangle sRect = new Rectangle(_deployButtonRect.X, _deployButtonRect.Y + 36, _deployButtonRect.Width, 20);

                g.DrawString("⚡ 데이터 전송 클릭!", btnTitleFont, btnTextBrush, tRect, sf);
                g.DrawString($"트래픽 처리 수익 (클릭당 +${clickPower:N1})", btnSubFont, btnTextBrush, sRect, sf);
            }

            foreach (var sw in _shockwaves)
            {
                using (Pen swPen = new Pen(Color.FromArgb((int)(sw.Alpha * 255), sw.WaveColor), 2f))
                {
                    g.DrawEllipse(swPen, sw.X - sw.Radius, sw.Y - sw.Radius, sw.Radius * 2, sw.Radius * 2);
                }
            }
        }

        private void DrawSubTabBar(Graphics g, int w, int topY, int height)
        {
            string[] tabNames = { "● 서버실", "⚡ 전력실", "◈ 냉각실", "◆ S/W 개발", "★ 외주 계약", "▲ 코인 거래", "✦ 연구/환생" };
            int tabCount = tabNames.Length;
            int tabGap = 3;
            int tabW = (w - 20 - (tabCount - 1) * tabGap) / tabCount;
            int tabH = height;

            for (int i = 0; i < tabCount; i++)
            {
                int tx = 10 + i * (tabW + tabGap);
                _subTabBounds[i] = new Rectangle(tx, topY, tabW, tabH);
                bool isActive = _activeSubTab == i;

                Color bg = isActive ? Color.FromArgb(32, 42, 60) : Color.FromArgb(20, 24, 32);
                Color border = isActive ? Color.FromArgb(0, 229, 255) : Color.FromArgb(38, 46, 60);
                Color textCol = isActive ? Color.White : Color.FromArgb(150, 165, 185);

                using (SolidBrush brush = new SolidBrush(bg))
                using (Pen pen = new Pen(border, isActive ? 1.5f : 1f))
                using (Font f = new Font("Segoe UI", 8.0f, FontStyle.Bold))
                using (SolidBrush tBrush = new SolidBrush(textCol))
                {
                    g.FillRectangle(brush, _subTabBounds[i]);
                    g.DrawRectangle(pen, _subTabBounds[i]);
                    StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(tabNames[i], f, tBrush, _subTabBounds[i], sf);
                }
            }
        }

        private void DrawActiveTabContent(Graphics g, int w, int topY, int height)
        {
            // 상단 구매 수량 토글 버튼 (탭 0, 1, 2 서버/전력/냉각 공통)
            if (_activeSubTab < 3)
            {
                int toggleX = w - 165;
                _buy1Rect = new Rectangle(toggleX, topY + 4, 42, 20);
                _buy10Rect = new Rectangle(toggleX + 46, topY + 4, 42, 20);
                _buyMaxRect = new Rectangle(toggleX + 92, topY + 4, 46, 20);

                DrawToggleButton(g, _buy1Rect, "x1", _buyMultiplier == 1);
                DrawToggleButton(g, _buy10Rect, "x10", _buyMultiplier == 10);
                DrawToggleButton(g, _buyMaxRect, "MAX", _buyMultiplier == -1);
            }

            switch (_activeSubTab)
            {
                case 0:
                    DrawServersTab(g, w, topY);
                    break;
                case 1:
                    DrawPowerTab(g, w, topY);
                    break;
                case 2:
                    DrawCoolingTab(g, w, topY);
                    break;
                case 3:
                    DrawSoftwareStudioTab(g, w, topY);
                    break;
                case 4:
                    DrawB2BContractsTab(g, w, topY);
                    break;
                case 5:
                    DrawCryptoMarketTab(g, w, topY);
                    break;
                case 6:
                    DrawTechTreeTab(g, w, topY);
                    break;
            }
        }

        private void DrawServersTab(Graphics g, int w, int topY)
        {
            using (Font titleFont = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (SolidBrush titleBrush = new SolidBrush(Color.FromArgb(200, 220, 245)))
            {
                g.DrawString("■ 서버 컴퓨터 증설 & 업그레이드 (수익 증가 | 전력·열 발생)", titleFont, titleBrush, 14, topY + 6);
            }

            int cardPad = 8;
            int cardW = (w - 20 - cardPad) / 2;
            int cardH = 66;
            int startCardY = topY + 28;

            for (int i = 0; i < 6; i++)
            {
                int col = i % 2;
                int row = i / 2;
                int cx = 10 + col * (cardW + cardPad);
                int cy = startCardY + row * (cardH + 5);

                _cardBounds[i] = new Rectangle(cx, cy, cardW, cardH);
                DrawServerCard(g, _cardBounds[i], i);
            }
        }

        private void DrawServerCard(Graphics g, Rectangle rect, int index)
        {
            var server = index < _state.Servers.Count ? _state.Servers[index] : null;
            if (server == null) return;

            int buyCount = _buyMultiplier == -1 ? server.GetMaxAffordable(_state.Money) : _buyMultiplier;
            double cost = server.GetNextCost(buyCount);
            bool canAfford = _state.Money >= cost;
            bool isHovered = _hoveredCardIndex == index;

            using (SolidBrush cBrush = new SolidBrush(Color.FromArgb(22, 26, 36)))
            using (Pen cPen = new Pen(isHovered ? server.AccentColor : Color.FromArgb(42, 50, 66), isHovered ? 1.5f : 1f))
            {
                g.FillRectangle(cBrush, rect);
                g.DrawRectangle(cPen, rect);
            }

            // 고유 벡터 아이콘 뱃지 렌더링 (네모 박스 문제 원천 제거)
            DrawUnitVectorIcon(g, new Rectangle(rect.X + 6, rect.Y + (rect.Height - 36) / 2, 36, 36), server.Id, server.AccentColor);

            int textW = rect.Width - 132;
            using (Font nameFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (Font subFont = new Font("Segoe UI", 7.5f, FontStyle.Regular))
            using (SolidBrush nameBrush = new SolidBrush(Color.FromArgb(230, 240, 255)))
            using (SolidBrush levelBrush = new SolidBrush(Color.FromArgb(160, 180, 205)))
            using (SolidBrush rpsBrush = new SolidBrush(server.AccentColor))
            using (StringFormat subSf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
            {
                g.DrawString(server.Name, nameFont, nameBrush, new Rectangle(rect.X + 48, rect.Y + 6, textW, 18), subSf);
                g.DrawString($"Lv.{server.Level} | +${server.GetTotalRps():N1}/s", subFont, levelBrush, new Rectangle(rect.X + 48, rect.Y + 24, textW, 16), subSf);
                g.DrawString($"소비: {server.PowerWatts:N0}W, 열: +{server.HeatOutput:F1}°", subFont, _brushSubTextDim, new Rectangle(rect.X + 48, rect.Y + 42, textW, 16), subSf);
            }

            int btnW = 74;
            int btnH = 44;
            int bx = rect.Right - btnW - 8;
            int by = rect.Y + (rect.Height - btnH) / 2;
            Rectangle buyBtnRect = new Rectangle(bx, by, btnW, btnH);
            _cardBuyButtonBounds[index] = buyBtnRect;

            Color btnBg = canAfford ? Color.FromArgb(35, 140, 90) : Color.FromArgb(38, 44, 56);
            Color btnBorder = canAfford ? Color.FromArgb(0, 230, 118) : Color.FromArgb(50, 60, 76);
            Color textCol = canAfford ? Color.White : Color.FromArgb(130, 140, 155);

            using (SolidBrush bBg = new SolidBrush(btnBg))
            using (Pen bPen = new Pen(btnBorder, canAfford ? 1.5f : 1f))
            using (Font bFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            using (Font countFont = new Font("Segoe UI", 7f))
            using (SolidBrush tBrush = new SolidBrush(textCol))
            {
                g.FillRectangle(bBg, buyBtnRect);
                g.DrawRectangle(bPen, buyBtnRect);

                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                string countStr = buyCount > 1 ? $"(+{buyCount})" : "";
                Rectangle tRect = new Rectangle(buyBtnRect.X, buyBtnRect.Y + 3, buyBtnRect.Width, 18);
                Rectangle cRect = new Rectangle(buyBtnRect.X, buyBtnRect.Y + 21, buyBtnRect.Width, 18);

                g.DrawString($"${FormatCompactNumber(cost)}", bFont, tBrush, tRect, sf);
                g.DrawString($"구매 {countStr}", countFont, tBrush, cRect, sf);
            }
        }

        private void DrawPowerTab(Graphics g, int w, int topY)
        {
            double supply = _state.GetTotalPowerSupply();
            double demand = _state.GetTotalPowerDemand();
            double capHours = _state.GetOfflineCapSeconds() / 3600.0;

            using (Font titleFont = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (SolidBrush titleBrush = new SolidBrush(Color.FromArgb(255, 167, 38)))
            {
                g.DrawString($"⚡ 전력: 공급 {supply:N0}W / 필요 {demand:N0}W  |  축전: {capHours:F1}h", titleFont, titleBrush, 14, topY + 6);
            }

            int cardPad = 8;
            int cardW = (w - 20 - cardPad) / 2;
            int cardH = 66;
            int startCardY = topY + 28;

            for (int i = 0; i < 6; i++)
            {
                int col = i % 2;
                int row = i / 2;
                int cx = 10 + col * (cardW + cardPad);
                int cy = startCardY + row * (cardH + 5);

                _cardBounds[i] = new Rectangle(cx, cy, cardW, cardH);
                DrawPowerCard(g, _cardBounds[i], i);
            }
        }

        private void DrawPowerCard(Graphics g, Rectangle rect, int index)
        {
            var power = index < _state.PowerFacilities.Count ? _state.PowerFacilities[index] : null;
            if (power == null) return;

            int buyCount = _buyMultiplier == -1 ? power.GetMaxAffordable(_state.Money) : _buyMultiplier;
            double cost = power.GetNextCost(buyCount);
            bool canAfford = _state.Money >= cost && power.BaseCost > 0;
            bool isHovered = _hoveredCardIndex == index;

            using (SolidBrush cBrush = new SolidBrush(Color.FromArgb(24, 26, 34)))
            using (Pen cPen = new Pen(isHovered ? power.AccentColor : Color.FromArgb(46, 50, 64), isHovered ? 1.5f : 1f))
            {
                g.FillRectangle(cBrush, rect);
                g.DrawRectangle(cPen, rect);
            }

            // 고유 벡터 아이콘 뱃지 렌더링
            DrawUnitVectorIcon(g, new Rectangle(rect.X + 6, rect.Y + (rect.Height - 36) / 2, 36, 36), power.Id, power.AccentColor);

            int textW = rect.Width - 132;
            using (Font nameFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (Font subFont = new Font("Segoe UI", 7.5f, FontStyle.Regular))
            using (SolidBrush nameBrush = new SolidBrush(Color.FromArgb(230, 240, 255)))
            using (SolidBrush levelBrush = new SolidBrush(Color.FromArgb(255, 183, 77)))
            using (StringFormat subSf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
            {
                g.DrawString(power.Name, nameFont, nameBrush, new Rectangle(rect.X + 48, rect.Y + 6, textW, 18), subSf);
                string bonusStr = power.OfflineBonusMinutes > 0 ? $" | +{power.OfflineBonusMinutes}분 배터리" : "";
                g.DrawString($"Lv.{power.Level} (+{power.GetTotalSupply():N0}W){bonusStr}", subFont, levelBrush, new Rectangle(rect.X + 48, rect.Y + 24, textW, 16), subSf);
                g.DrawString(power.SubTitle, subFont, _brushSubTextDim, new Rectangle(rect.X + 48, rect.Y + 42, textW, 16), subSf);
            }

            int btnW = 74;
            int btnH = 44;
            int bx = rect.Right - btnW - 8;
            int by = rect.Y + (rect.Height - btnH) / 2;
            Rectangle buyBtnRect = new Rectangle(bx, by, btnW, btnH);
            _cardBuyButtonBounds[index] = buyBtnRect;

            if (power.BaseCost <= 0)
            {
                // 기본 제공 설비
                using (SolidBrush bBg = new SolidBrush(Color.FromArgb(36, 42, 54)))
                using (Font bFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
                using (SolidBrush tBrush = new SolidBrush(Color.FromArgb(140, 160, 180)))
                {
                    g.FillRectangle(bBg, buyBtnRect);
                    StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString("기본 제공", bFont, tBrush, buyBtnRect, sf);
                }
                return;
            }

            Color btnBg = canAfford ? Color.FromArgb(160, 100, 20) : Color.FromArgb(38, 44, 56);
            Color btnBorder = canAfford ? Color.FromArgb(255, 167, 38) : Color.FromArgb(50, 60, 76);
            Color textCol = canAfford ? Color.White : Color.FromArgb(130, 140, 155);

            using (SolidBrush bBg = new SolidBrush(btnBg))
            using (Pen bPen = new Pen(btnBorder, canAfford ? 1.5f : 1f))
            using (Font bFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            using (Font countFont = new Font("Segoe UI", 7f))
            using (SolidBrush tBrush = new SolidBrush(textCol))
            {
                g.FillRectangle(bBg, buyBtnRect);
                g.DrawRectangle(bPen, buyBtnRect);

                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                string countStr = buyCount > 1 ? $"(+{buyCount})" : "";
                Rectangle tRect = new Rectangle(buyBtnRect.X, buyBtnRect.Y + 3, buyBtnRect.Width, 18);
                Rectangle cRect = new Rectangle(buyBtnRect.X, buyBtnRect.Y + 21, buyBtnRect.Width, 18);

                g.DrawString($"${FormatCompactNumber(cost)}", bFont, tBrush, tRect, sf);
                g.DrawString($"증설 {countStr}", countFont, tBrush, cRect, sf);
            }
        }

        private void DrawCoolingTab(Graphics g, int w, int topY)
        {
            double cooling = _state.GetTotalCoolingCapacity();
            double heat = _state.GetTotalHeatGenerated();
            double temp = _state.GetCurrentTemperature();
            string status = temp <= 35.0 ? "[쾌적 보너스 +20%]" : (temp > 60.0 ? "[과열 성능 저하 -35%]" : "[안정 가동]");

            using (Font titleFont = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (SolidBrush titleBrush = new SolidBrush(Color.FromArgb(0, 229, 255)))
            {
                g.DrawString($"◈ 냉각: {cooling:N0} / 발열: {heat:N0}  |  온도: {temp:F1}°C {status}", titleFont, titleBrush, 14, topY + 6);
            }

            int cardPad = 8;
            int cardW = (w - 20 - cardPad) / 2;
            int cardH = 66;
            int startCardY = topY + 28;

            for (int i = 0; i < 6; i++)
            {
                int col = i % 2;
                int row = i / 2;
                int cx = 10 + col * (cardW + cardPad);
                int cy = startCardY + row * (cardH + 5);

                _cardBounds[i] = new Rectangle(cx, cy, cardW, cardH);
                DrawCoolingCard(g, _cardBounds[i], i);
            }
        }

        private void DrawCoolingCard(Graphics g, Rectangle rect, int index)
        {
            var cooling = index < _state.CoolingFacilities.Count ? _state.CoolingFacilities[index] : null;
            if (cooling == null) return;

            int buyCount = _buyMultiplier == -1 ? cooling.GetMaxAffordable(_state.Money) : _buyMultiplier;
            double cost = cooling.GetNextCost(buyCount);
            bool canAfford = _state.Money >= cost && cooling.BaseCost > 0;
            bool isHovered = _hoveredCardIndex == index;

            using (SolidBrush cBrush = new SolidBrush(Color.FromArgb(20, 26, 36)))
            using (Pen cPen = new Pen(isHovered ? cooling.AccentColor : Color.FromArgb(40, 52, 68), isHovered ? 1.5f : 1f))
            {
                g.FillRectangle(cBrush, rect);
                g.DrawRectangle(cPen, rect);
            }

            // 고유 벡터 아이콘 뱃지 렌더링
            DrawUnitVectorIcon(g, new Rectangle(rect.X + 6, rect.Y + (rect.Height - 36) / 2, 36, 36), cooling.Id, cooling.AccentColor);

            int textW = rect.Width - 132;
            using (Font nameFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (Font subFont = new Font("Segoe UI", 7.5f, FontStyle.Regular))
            using (SolidBrush nameBrush = new SolidBrush(Color.FromArgb(230, 240, 255)))
            using (SolidBrush levelBrush = new SolidBrush(Color.FromArgb(0, 229, 255)))
            using (StringFormat subSf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
            {
                g.DrawString(cooling.Name, nameFont, nameBrush, new Rectangle(rect.X + 48, rect.Y + 6, textW, 18), subSf);
                g.DrawString($"Lv.{cooling.Level} (냉각 용량: +{cooling.GetTotalCooling():N0})", subFont, levelBrush, new Rectangle(rect.X + 48, rect.Y + 24, textW, 16), subSf);
                g.DrawString(cooling.SubTitle, subFont, _brushSubTextDim, new Rectangle(rect.X + 48, rect.Y + 42, textW, 16), subSf);
            }

            int btnW = 74;
            int btnH = 44;
            int bx = rect.Right - btnW - 8;
            int by = rect.Y + (rect.Height - btnH) / 2;
            Rectangle buyBtnRect = new Rectangle(bx, by, btnW, btnH);
            _cardBuyButtonBounds[index] = buyBtnRect;

            if (cooling.BaseCost <= 0)
            {
                using (SolidBrush bBg = new SolidBrush(Color.FromArgb(36, 42, 54)))
                using (Font bFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
                using (SolidBrush tBrush = new SolidBrush(Color.FromArgb(140, 160, 180)))
                {
                    g.FillRectangle(bBg, buyBtnRect);
                    StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString("기본 제공", bFont, tBrush, buyBtnRect, sf);
                }
                return;
            }

            Color btnBg = canAfford ? Color.FromArgb(20, 110, 140) : Color.FromArgb(38, 44, 56);
            Color btnBorder = canAfford ? Color.FromArgb(0, 229, 255) : Color.FromArgb(50, 60, 76);
            Color textCol = canAfford ? Color.White : Color.FromArgb(130, 140, 155);

            using (SolidBrush bBg = new SolidBrush(btnBg))
            using (Pen bPen = new Pen(btnBorder, canAfford ? 1.5f : 1f))
            using (Font bFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            using (Font countFont = new Font("Segoe UI", 7f))
            using (SolidBrush tBrush = new SolidBrush(textCol))
            {
                g.FillRectangle(bBg, buyBtnRect);
                g.DrawRectangle(bPen, buyBtnRect);

                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                string countStr = buyCount > 1 ? $"(+{buyCount})" : "";
                Rectangle tRect = new Rectangle(buyBtnRect.X, buyBtnRect.Y + 3, buyBtnRect.Width, 18);
                Rectangle cRect = new Rectangle(buyBtnRect.X, buyBtnRect.Y + 21, buyBtnRect.Width, 18);

                g.DrawString($"${FormatCompactNumber(cost)}", bFont, tBrush, tRect, sf);
                g.DrawString($"설치 {countStr}", countFont, tBrush, cRect, sf);
            }
        }
        private void DrawUnitVectorIcon(Graphics g, Rectangle bounds, string id, Color accent)
        {
            // 배경 서클 뱃지
            using (SolidBrush badgeBg = new SolidBrush(Color.FromArgb(30, accent)))
            using (Pen badgePen = new Pen(Color.FromArgb(85, accent), 1.2f))
            {
                g.FillEllipse(badgeBg, bounds);
                g.DrawEllipse(badgePen, bounds);
            }

            int cx = bounds.X + bounds.Width / 2;
            int cy = bounds.Y + bounds.Height / 2;

            switch (id)
            {
                case "rpi":
                    // 녹색 PCB 기판 + 중앙 칩 + 상단 GPIO 핀
                    using (SolidBrush pcb = new SolidBrush(Color.FromArgb(46, 125, 50)))
                    using (SolidBrush chip = new SolidBrush(Color.FromArgb(33, 33, 33)))
                    using (Pen gold = new Pen(Color.FromArgb(255, 215, 64), 1.2f))
                    {
                        g.FillRectangle(pcb, cx - 9, cy - 7, 18, 14);
                        g.FillRectangle(chip, cx - 4, cy - 2, 8, 6);
                        for (int i = -6; i <= 6; i += 3)
                        {
                            g.DrawLine(gold, cx + i, cy - 7, cx + i, cy - 5);
                        }
                    }
                    break;

                case "tower":
                    // 슬릭 오피스 PC 타워 섀시
                    using (SolidBrush body = new SolidBrush(Color.FromArgb(55, 71, 79)))
                    using (Pen edge = new Pen(Color.FromArgb(144, 164, 174), 1f))
                    using (SolidBrush pwr = new SolidBrush(Color.FromArgb(255, 167, 38)))
                    {
                        g.FillRectangle(body, cx - 6, cy - 9, 12, 18);
                        g.DrawRectangle(edge, cx - 6, cy - 9, 12, 18);
                        g.FillEllipse(pwr, cx - 2, cy - 6, 4, 4);
                        g.DrawLine(edge, cx - 4, cy + 2, cx + 3, cy + 2);
                        g.DrawLine(edge, cx - 4, cy + 5, cx + 3, cy + 5);
                    }
                    break;

                case "blade1u":
                    // 1U 랙 블레이드 서버 본체 + 상태 LED
                    using (SolidBrush body = new SolidBrush(Color.FromArgb(38, 50, 56)))
                    using (Pen edge = new Pen(Color.FromArgb(0, 230, 118), 1f))
                    using (SolidBrush led1 = new SolidBrush(Color.FromArgb(0, 229, 255)))
                    using (SolidBrush led2 = new SolidBrush(Color.FromArgb(0, 230, 118)))
                    {
                        g.FillRectangle(body, cx - 11, cy - 4, 22, 9);
                        g.DrawRectangle(edge, cx - 11, cy - 4, 22, 9);
                        g.FillEllipse(led1, cx - 8, cy - 1, 3, 3);
                        g.FillEllipse(led2, cx - 3, cy - 1, 3, 3);
                        using (Pen vent = new Pen(Color.FromArgb(90, 110, 130), 1f))
                        {
                            g.DrawLine(vent, cx + 3, cy - 2, cx + 3, cy + 3);
                            g.DrawLine(vent, cx + 6, cy - 2, cx + 6, cy + 3);
                        }
                    }
                    break;

                case "xeon4u":
                    // 4U 제온 워크스테이션 + 듀얼 팬
                    using (SolidBrush body = new SolidBrush(Color.FromArgb(33, 43, 54)))
                    using (Pen edge = new Pen(Color.FromArgb(0, 229, 255), 1.2f))
                    {
                        g.FillRectangle(body, cx - 10, cy - 8, 20, 16);
                        g.DrawRectangle(edge, cx - 10, cy - 8, 20, 16);
                        using (Pen fanP = new Pen(Color.FromArgb(0, 229, 255), 1f))
                        {
                            g.DrawEllipse(fanP, cx - 7, cy - 3, 6, 6);
                            g.DrawEllipse(fanP, cx + 1, cy - 3, 6, 6);
                        }
                    }
                    break;

                case "gpu_h100":
                    // 엔비디아 H100 GPU 가속기 카드 + PCIe 골드 핀
                    using (SolidBrush body = new SolidBrush(Color.FromArgb(69, 39, 160)))
                    using (Pen edge = new Pen(Color.FromArgb(179, 136, 255), 1.2f))
                    using (SolidBrush gold = new SolidBrush(Color.FromArgb(255, 215, 64)))
                    {
                        g.FillRectangle(body, cx - 11, cy - 6, 22, 12);
                        g.DrawRectangle(edge, cx - 11, cy - 6, 22, 12);
                        g.FillRectangle(gold, cx - 7, cy + 5, 14, 2);
                        using (Pen fan = new Pen(Color.FromArgb(179, 136, 255), 1f))
                        {
                            g.DrawEllipse(fan, cx - 7, cy - 3, 5, 5);
                            g.DrawEllipse(fan, cx + 2, cy - 3, 5, 5);
                        }
                    }
                    break;

                case "quantum":
                    // 양자 초전도 큐비트 코어 + 원자 궤도 링
                    using (Pen orbit = new Pen(Color.FromArgb(105, 240, 174), 1.2f))
                    using (SolidBrush core = new SolidBrush(Color.White))
                    {
                        g.DrawEllipse(orbit, cx - 9, cy - 4, 18, 8);
                        g.DrawEllipse(orbit, cx - 4, cy - 9, 8, 18);
                        g.FillEllipse(core, cx - 2, cy - 2, 5, 5);
                    }
                    break;

                case "outlet":
                    // 전원 벽면 콘센트
                    using (SolidBrush face = new SolidBrush(Color.FromArgb(236, 239, 241)))
                    using (Pen edge = new Pen(Color.FromArgb(176, 190, 197), 1f))
                    using (SolidBrush pin = new SolidBrush(Color.FromArgb(38, 50, 56)))
                    {
                        g.FillEllipse(face, cx - 8, cy - 8, 16, 16);
                        g.DrawEllipse(edge, cx - 8, cy - 8, 16, 16);
                        g.FillRectangle(pin, cx - 4, cy - 3, 2, 6);
                        g.FillRectangle(pin, cx + 2, cy - 3, 2, 6);
                    }
                    break;

                case "multitab":
                    // 서지 보호 6구 멀티탭 + 적색 스위치
                    using (SolidBrush body = new SolidBrush(Color.FromArgb(55, 71, 79)))
                    using (Pen edge = new Pen(Color.FromArgb(255, 167, 38), 1f))
                    using (SolidBrush sw = new SolidBrush(Color.FromArgb(255, 60, 0)))
                    using (SolidBrush hole = new SolidBrush(Color.FromArgb(30, 36, 42)))
                    {
                        g.FillRectangle(body, cx - 10, cy - 4, 20, 9);
                        g.DrawRectangle(edge, cx - 10, cy - 4, 20, 9);
                        g.FillRectangle(sw, cx - 8, cy - 2, 3, 5);
                        g.FillEllipse(hole, cx - 2, cy - 2, 4, 4);
                        g.FillEllipse(hole, cx + 4, cy - 2, 4, 4);
                    }
                    break;

                case "ups_mini":
                    // UPS 무정전 전원 배터리 + 노란 번개
                    using (SolidBrush body = new SolidBrush(Color.FromArgb(38, 50, 56)))
                    using (Pen edge = new Pen(Color.FromArgb(77, 208, 225), 1.2f))
                    using (SolidBrush bolt = new SolidBrush(Color.FromArgb(255, 215, 64)))
                    {
                        g.FillRectangle(body, cx - 7, cy - 9, 14, 18);
                        g.DrawRectangle(edge, cx - 7, cy - 9, 14, 18);
                        PointF[] pts = { new PointF(cx + 1, cy - 5), new PointF(cx - 3, cy), new PointF(cx, cy), new PointF(cx - 1, cy + 5), new PointF(cx + 4, cy - 1), new PointF(cx + 1, cy - 1) };
                        g.FillPolygon(bolt, pts);
                    }
                    break;

                case "high_voltage":
                    // 3상 고압 송전철탑 트러스
                    using (Pen pylon = new Pen(Color.FromArgb(0, 229, 255), 1.5f))
                    {
                        g.DrawLine(pylon, cx, cy - 9, cx - 7, cy + 9);
                        g.DrawLine(pylon, cx, cy - 9, cx + 7, cy + 9);
                        g.DrawLine(pylon, cx - 8, cy - 2, cx + 8, cy - 2);
                        g.DrawLine(pylon, cx - 5, cy + 3, cx + 5, cy + 3);
                        g.DrawLine(pylon, cx - 7, cy + 9, cx + 7, cy + 9);
                    }
                    break;

                case "solar_array":
                    // 옥상 태양광 패널 그리드 + 태양
                    using (SolidBrush panel = new SolidBrush(Color.FromArgb(21, 101, 192)))
                    using (Pen grid = new Pen(Color.FromArgb(100, 181, 246), 1f))
                    using (SolidBrush sun = new SolidBrush(Color.FromArgb(255, 213, 79)))
                    {
                        Point[] poly = { new Point(cx - 8, cy + 5), new Point(cx + 6, cy + 5), new Point(cx + 9, cy - 5), new Point(cx - 5, cy - 5) };
                        g.FillPolygon(panel, poly);
                        g.DrawPolygon(grid, poly);
                        g.DrawLine(grid, cx - 6, cy, cx + 7, cy);
                        g.DrawLine(grid, cx + 1, cy - 5, cx - 1, cy + 5);
                        g.FillEllipse(sun, cx - 9, cy - 9, 5, 5);
                    }
                    break;

                case "smr_reactor":
                    // SMR 소형 원자로 방사능 심볼 (라디에이션 트레포일)
                    using (SolidBrush lime = new SolidBrush(Color.FromArgb(118, 255, 3)))
                    using (Pen limePen = new Pen(Color.FromArgb(118, 255, 3), 1.2f))
                    {
                        g.DrawEllipse(limePen, cx - 8, cy - 8, 16, 16);
                        g.FillPie(lime, cx - 7, cy - 7, 14, 14, 30, 60);
                        g.FillPie(lime, cx - 7, cy - 7, 14, 14, 150, 60);
                        g.FillPie(lime, cx - 7, cy - 7, 14, 14, 270, 60);
                        using (SolidBrush centerBg = new SolidBrush(Color.FromArgb(20, 24, 32)))
                        {
                            g.FillEllipse(centerBg, cx - 3, cy - 3, 6, 6);
                            g.FillEllipse(lime, cx - 1, cy - 1, 3, 3);
                        }
                    }
                    break;

                case "passive":
                    // 자연 대류 공기 흐름 웨이브
                    using (Pen wave = new Pen(Color.FromArgb(129, 199, 132), 1.5f))
                    {
                        g.DrawArc(wave, cx - 8, cy - 5, 8, 5, 180, 180);
                        g.DrawArc(wave, cx - 2, cy - 5, 8, 5, 0, 180);
                        g.DrawArc(wave, cx - 6, cy, 8, 5, 180, 180);
                        g.DrawArc(wave, cx, cy, 8, 5, 0, 180);
                        g.DrawArc(wave, cx - 8, cy + 5, 8, 5, 180, 180);
                        g.DrawArc(wave, cx - 2, cy + 5, 8, 5, 0, 180);
                    }
                    break;

                case "desk_fan":
                    // 3엽 탁상용 선풍기
                    using (Pen ring = new Pen(Color.FromArgb(79, 195, 247), 1.2f))
                    using (SolidBrush blade = new SolidBrush(Color.FromArgb(79, 195, 247)))
                    {
                        g.DrawEllipse(ring, cx - 8, cy - 8, 16, 16);
                        g.FillEllipse(blade, cx - 2, cy - 2, 4, 4);
                        for (int i = 0; i < 3; i++)
                        {
                            float a = _fanAngle + i * 120;
                            float rad = (float)(a * Math.PI / 180.0);
                            float bx = cx + (float)Math.Cos(rad) * 6;
                            float by = cy + (float)Math.Sin(rad) * 6;
                            g.DrawLine(ring, cx, cy, bx, by);
                        }
                    }
                    break;

                case "crac_unit":
                    // 항온항습기 눈꽃 결정 (Snowflake)
                    using (Pen flake = new Pen(Color.FromArgb(0, 229, 255), 1.5f))
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            float a = i * 60;
                            float rad = (float)(a * Math.PI / 180.0);
                            float x1 = cx + (float)Math.Cos(rad) * 7;
                            float y1 = cy + (float)Math.Sin(rad) * 7;
                            float x2 = cx - (float)Math.Cos(rad) * 7;
                            float y2 = cy - (float)Math.Sin(rad) * 7;
                            g.DrawLine(flake, x1, y1, x2, y2);
                        }
                        g.DrawEllipse(flake, cx - 2, cy - 2, 5, 5);
                    }
                    break;

                case "water_loop":
                    // 커스텀 수냉 워터 루프 + 물방울
                    using (Pen pipe = new Pen(Color.FromArgb(41, 121, 255), 2f))
                    using (SolidBrush drop = new SolidBrush(Color.FromArgb(0, 229, 255)))
                    {
                        g.DrawArc(pipe, cx - 7, cy - 7, 14, 14, 45, 270);
                        g.FillEllipse(drop, cx - 2, cy - 2, 5, 5);
                    }
                    break;

                case "immersion":
                    // 액침 냉각 탱크 + 냉매 기포
                    using (Pen tank = new Pen(Color.FromArgb(179, 136, 255), 1.5f))
                    using (SolidBrush fluid = new SolidBrush(Color.FromArgb(90, 80, 40, 160)))
                    using (SolidBrush bubble = new SolidBrush(Color.FromArgb(200, 225, 255)))
                    {
                        g.FillRectangle(fluid, cx - 7, cy - 2, 14, 9);
                        g.DrawRectangle(tank, cx - 7, cy - 7, 14, 14);
                        g.FillEllipse(bubble, cx - 4, cy + 1, 2, 2);
                        g.FillEllipse(bubble, cx + 2, cy + 3, 2, 2);
                        g.FillEllipse(bubble, cx - 1, cy + 4, 3, 3);
                    }
                    break;

                case "quantum_cryo":
                    // 극저온 헬륨 희석 냉동기 (골드 샹들리에)
                    using (Pen gold = new Pen(Color.FromArgb(255, 215, 64), 1.5f))
                    using (SolidBrush cold = new SolidBrush(Color.FromArgb(105, 240, 174)))
                    {
                        g.DrawLine(gold, cx - 8, cy - 6, cx + 8, cy - 6);
                        g.DrawLine(gold, cx - 6, cy - 1, cx + 6, cy - 1);
                        g.DrawLine(gold, cx - 3, cy + 4, cx + 3, cy + 4);
                        g.DrawLine(gold, cx, cy - 6, cx, cy + 4);
                        g.FillEllipse(cold, cx - 2, cy + 6, 4, 4);
                    }
                    break;

                case "skill_power_eff":
                    using (SolidBrush bolt = new SolidBrush(Color.FromArgb(255, 215, 64)))
                    {
                        PointF[] pts = { new PointF(cx, cy - 7), new PointF(cx - 4, cy), new PointF(cx - 1, cy), new PointF(cx - 2, cy + 6), new PointF(cx + 4, cy), new PointF(cx + 1, cy) };
                        g.FillPolygon(bolt, pts);
                    }
                    break;

                case "skill_cooling_boost":
                    using (Pen ice = new Pen(Color.FromArgb(0, 229, 255), 1.5f))
                    {
                        g.DrawLine(ice, cx, cy - 7, cx, cy + 7);
                        g.DrawLine(ice, cx - 6, cy - 3, cx + 6, cy + 3);
                        g.DrawLine(ice, cx - 6, cy + 3, cx + 6, cy - 3);
                    }
                    break;

                case "skill_backbone":
                    using (Pen net = new Pen(Color.FromArgb(0, 229, 255), 1.2f))
                    using (SolidBrush node = new SolidBrush(Color.White))
                    {
                        g.DrawEllipse(net, cx - 7, cy - 7, 14, 14);
                        g.DrawLine(net, cx - 7, cy, cx + 7, cy);
                        g.DrawLine(net, cx, cy - 7, cx, cy + 7);
                        g.FillEllipse(node, cx - 2, cy - 2, 4, 4);
                    }
                    break;

                case "skill_battery_max":
                    using (Pen bat = new Pen(Color.FromArgb(0, 230, 118), 1.2f))
                    using (SolidBrush bar = new SolidBrush(Color.FromArgb(0, 230, 118)))
                    {
                        g.DrawRectangle(bat, cx - 5, cy - 7, 10, 14);
                        g.FillRectangle(bar, cx - 2, cy - 9, 4, 2);
                        g.FillRectangle(bar, cx - 3, cy - 4, 6, 9);
                    }
                    break;

                case "skill_crit_packet":
                    using (SolidBrush star = new SolidBrush(Color.FromArgb(255, 215, 64)))
                    using (Pen starP = new Pen(Color.FromArgb(255, 80, 220), 1.5f))
                    {
                        g.FillRectangle(star, cx - 3, cy - 3, 6, 6);
                        g.DrawLine(starP, cx, cy - 7, cx, cy + 7);
                        g.DrawLine(starP, cx - 7, cy, cx + 7, cy);
                    }
                    break;

                case "skill_ai_devops":
                    using (SolidBrush bot = new SolidBrush(Color.FromArgb(38, 166, 154)))
                    using (Pen visor = new Pen(Color.FromArgb(0, 229, 255), 1.5f))
                    {
                        g.FillRectangle(bot, cx - 6, cy - 5, 12, 10);
                        g.DrawLine(visor, cx - 4, cy - 1, cx + 4, cy - 1);
                        g.DrawLine(visor, cx, cy - 7, cx, cy - 5);
                    }
                    break;

                default:
                    using (SolidBrush dot = new SolidBrush(accent))
                    {
                        g.FillEllipse(dot, cx - 4, cy - 4, 8, 8);
                    }
                    break;
            }
        }

        private void DrawToggleButton(Graphics g, Rectangle rect, string text, bool isSelected)
        {
            Color bg = isSelected ? Color.FromArgb(0, 180, 215) : Color.FromArgb(30, 36, 48);
            Color textCol = isSelected ? Color.White : Color.FromArgb(160, 175, 195);
            using (SolidBrush brush = new SolidBrush(bg))
            using (Pen pen = new Pen(isSelected ? Color.FromArgb(0, 229, 255) : Color.FromArgb(50, 60, 80), 1))
            using (Font f = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            using (SolidBrush tBrush = new SolidBrush(textCol))
            {
                g.FillRectangle(brush, rect);
                g.DrawRectangle(pen, rect);
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(text, f, tBrush, rect, sf);
            }
        }

        private void DrawOfflineBanner(Graphics g, int w)
        {
            _offlineBannerRect = new Rectangle(20, 14, w - 40, 48);
            using (SolidBrush bannerBg = new SolidBrush(Color.FromArgb(240, 24, 34, 48)))
            using (Pen bannerPen = new Pen(Color.FromArgb(0, 229, 255), 2))
            using (Font font = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (SolidBrush textBrush = new SolidBrush(Color.White))
            {
                g.FillRectangle(bannerBg, _offlineBannerRect);
                g.DrawRectangle(bannerPen, _offlineBannerRect);

                StringFormat sf = new StringFormat { LineAlignment = StringAlignment.Center };
                g.DrawString(_offlineBannerText, font, textBrush, 
                    new Rectangle(_offlineBannerRect.X + 16, _offlineBannerRect.Y, _offlineBannerRect.Width - 110, _offlineBannerRect.Height), sf);
            }

            _offlineClaimButtonRect = new Rectangle(_offlineBannerRect.Right - 90, _offlineBannerRect.Y + 10, 80, 28);
            using (SolidBrush claimBg = new SolidBrush(Color.FromArgb(0, 230, 118)))
            using (Font cFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (SolidBrush cText = new SolidBrush(Color.Black))
            {
                g.FillRectangle(claimBg, _offlineClaimButtonRect);
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("수령하기", cFont, cText, _offlineClaimButtonRect, sf);
            }
        }

        private void DrawFloatingTexts(Graphics g)
        {
            using (Font ftFont = new Font("Segoe UI", 9f, FontStyle.Bold))
            {
                foreach (var ft in _floatingTexts)
                {
                    int alpha = (int)Math.Max(0, Math.Min(255, ft.Alpha * 255));
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(alpha, ft.TextColor)))
                    {
                        g.DrawString(ft.Text, ftFont, b, ft.X, ft.Y);
                    }
                }
            }
        }

        private string GetShortName(string id)
        {
            switch (id)
            {
                case "rpi": return "미니 PC";
                case "tower": return "사무용 PC";
                case "blade1u": return "랙 서버";
                case "xeon4u": return "워크스테이션";
                case "gpu_h100": return "AI 슈퍼컴";
                case "quantum": return "양자컴퓨터";
                default: return id;
            }
        }
        #endregion
    }
}
