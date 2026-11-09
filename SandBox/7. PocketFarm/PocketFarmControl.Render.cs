using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace GateHelper.PocketFarm
{
    public partial class PocketFarmControl
    {
        // --- 폰트 캐시 ---
        private Font _fontTitle;
        private Font _fontHeader;
        private Font _fontBody;
        private Font _fontBold;
        private Font _fontSmall;
        private Font _fontIconLarge;
        private Font _fontIconSmall;

        // --- 브러시 캐시 ---
        private SolidBrush _brBg;
        private SolidBrush _brHeaderBg;
        private SolidBrush _brCardBg;
        private SolidBrush _brCardHover;
        private SolidBrush _brTextWhite;
        private SolidBrush _brTextMuted;
        private SolidBrush _brTextGold;
        private SolidBrush _brTextGreen;
        private SolidBrush _brTextBlue;
        private SolidBrush _brSoil;
        private SolidBrush _brMeadow;
        private SolidBrush _brWater;
        private SolidBrush _brWood;
        private SolidBrush _brFloatingText;
        private SolidBrush _brFloatingShadow;

        // --- 펜 캐시 ---
        private Pen _penBorder;
        private Pen _penActive;
        private Pen _penGold;

        private void InitializeGdiResources()
        {
            _fontTitle = new Font("맑은 고딕", 13f, FontStyle.Bold);
            _fontHeader = new Font("맑은 고딕", 10.5f, FontStyle.Bold);
            _fontBody = new Font("맑은 고딕", 9f, FontStyle.Regular);
            _fontBold = new Font("맑은 고딕", 9f, FontStyle.Bold);
            _fontSmall = new Font("맑은 고딕", 8f, FontStyle.Regular);
            _fontIconLarge = new Font("맑은 고딕", 14f, FontStyle.Bold);
            _fontIconSmall = new Font("맑은 고딕", 9f, FontStyle.Bold);

            _brBg = new SolidBrush(Color.FromArgb(22, 26, 32));
            _brHeaderBg = new SolidBrush(Color.FromArgb(28, 34, 44));
            _brCardBg = new SolidBrush(Color.FromArgb(36, 44, 56));
            _brCardHover = new SolidBrush(Color.FromArgb(46, 56, 72));
            _brTextWhite = new SolidBrush(Color.FromArgb(240, 245, 250));
            _brTextMuted = new SolidBrush(Color.FromArgb(145, 155, 170));
            _brTextGold = new SolidBrush(Color.FromArgb(255, 215, 60));
            _brTextGreen = new SolidBrush(Color.FromArgb(100, 230, 130));
            _brTextBlue = new SolidBrush(Color.FromArgb(100, 190, 255));
            _brSoil = new SolidBrush(Color.FromArgb(75, 52, 38));
            _brMeadow = new SolidBrush(Color.FromArgb(50, 110, 65));
            _brWater = new SolidBrush(Color.FromArgb(40, 95, 155));
            _brWood = new SolidBrush(Color.FromArgb(110, 75, 45));
            _brFloatingText = new SolidBrush(Color.White);
            _brFloatingShadow = new SolidBrush(Color.FromArgb(128, 0, 0, 0));

            _penBorder = new Pen(Color.FromArgb(55, 65, 80), 1f);
            _penActive = new Pen(Color.FromArgb(80, 200, 120), 2f);
            _penGold = new Pen(Color.FromArgb(255, 215, 60), 1.5f);
        }

        private void DisposeGdiResources()
        {
            _fontTitle?.Dispose();
            _fontHeader?.Dispose();
            _fontBody?.Dispose();
            _fontBold?.Dispose();
            _fontSmall?.Dispose();
            _fontIconLarge?.Dispose();
            _fontIconSmall?.Dispose();

            _brBg?.Dispose();
            _brHeaderBg?.Dispose();
            _brCardBg?.Dispose();
            _brCardHover?.Dispose();
            _brTextWhite?.Dispose();
            _brTextMuted?.Dispose();
            _brTextGold?.Dispose();
            _brTextGreen?.Dispose();
            _brTextBlue?.Dispose();
            _brSoil?.Dispose();
            _brMeadow?.Dispose();
            _brWater?.Dispose();
            _brWood?.Dispose();
            _brFloatingText?.Dispose();
            _brFloatingShadow?.Dispose();

            _penBorder?.Dispose();
            _penActive?.Dispose();
            _penGold?.Dispose();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // 1. 전체 배경
            g.FillRectangle(_brBg, this.ClientRectangle);

            // 2. 상단 헤더 HUD (골드, 명성, 창고 현황, 뒤로가기)
            DrawHeader(g);

            // 3. 4대 탭 바 (농장 / 낚시 / 목축 / 식당)
            DrawTabBar(g);

            // 4. 메인 뷰 (선택 탭별 렌더링)
            switch (_currentTab)
            {
                case FarmTabType.Farm:
                    DrawFarmTab(g);
                    break;
                case FarmTabType.Fishing:
                    DrawFishingTab(g);
                    break;
                case FarmTabType.Livestock:
                    DrawLivestockTab(g);
                    break;
                case FarmTabType.Restaurant:
                    DrawRestaurantTab(g);
                    break;
            }

            // 5. 일꾼들 (홈 및 작업장 이동 애니메이션)
            DrawWorkers(g);

            // 6. 하단 스마트 컨트롤 바
            DrawBottomBar(g);

            // 7. 플로팅 텍스트
            DrawFloatingTexts(g);

            // 8. 활성 모달 (업그레이드 / 품종개량 / 레시피)
            if (_activeModal != ModalViewType.None)
            {
                DrawActiveModal(g);
            }
        }

        private void DrawHeader(Graphics g)
        {
            int w = this.Width;
            Rectangle rcHeader = new Rectangle(0, 0, w, 54);
            g.FillRectangle(_brHeaderBg, rcHeader);
            g.DrawLine(_penBorder, 0, 54, w, 54);

            // 뒤로가기 버튼
            _rcBtnBack = new Rectangle(12, 12, 64, 30);
            bool hoverBack = _rcBtnBack.Contains(_hoverPoint);
            g.FillRectangle(hoverBack ? _brCardHover : _brCardBg, _rcBtnBack);
            g.DrawRectangle(_penBorder, _rcBtnBack);
            g.DrawString("◀ 목록", _fontBold, _brTextWhite, 20, 18);

            // 타이틀 & 지표 HUD (1행)
            g.DrawString("✦ 포켓 팜 & 다이너", _fontHeader, _brTextGreen, 86, 11);

            // 골드 & 명성 & 창고
            string strGold = $"$ {_model.Gold:N0} G";
            string strFame = $"★ 명성 {_model.Fame:N0}P (Lv.{_model.Level})";
            int storedCount = _model.GetTotalStoredItemCount();
            string strSilo = $"■ 창고 {storedCount}/{_model.SiloCapacity}";

            g.DrawString(strGold, _fontBold, _brTextGold, 235, 12);
            g.DrawString(strFame, _fontBold, _brTextBlue, 360, 12);
            g.DrawString(strSilo, _fontBold, storedCount >= _model.SiloCapacity ? _brTextGold : _brTextWhite, 520, 12);

            // 2행: 동적 기후 정보 & 레벨 칭호
            string weatherName = _model.WeatherSystem != null ? _model.WeatherSystem.CurrentWeatherName : "화창함";
            string weatherDesc = _model.WeatherSystem != null ? _model.WeatherSystem.CurrentWeatherDescription : "평화로운 하루";
            string weatherLine = $"[기후] {weatherName}: {weatherDesc}";
            g.DrawString(weatherLine, _fontSmall, _brTextGold, 86, 33);

            string levelPerk = $"(Lv.{_model.Level} {_model.GetLevelTitle()} - 전역 생산속도 +{(_model.Level - 1) * 5}%)";
            g.DrawString(levelPerk, _fontSmall, _brTextMuted, 360, 33);
        }

        private void DrawTabBar(Graphics g)
        {
            int y = 58;
            int tabH = 34;

            _rcTabFarm = new Rectangle(12, y, 84, tabH);
            _rcTabFishing = new Rectangle(100, y, 84, tabH);
            _rcTabLivestock = new Rectangle(188, y, 92, tabH);
            _rcTabRestaurant = new Rectangle(284, y, 94, tabH);

            _rcBtnAchievements = new Rectangle(382, y, 78, tabH);
            _rcBtnContracts = new Rectangle(464, y, 78, tabH);
            _rcBtnAdmin = new Rectangle(546, y + 2, 44, 30);
            _rcBtnReset = new Rectangle(594, y + 2, 48, 30);

            string restTabTitle = "♨ 식당 (다이닝)";
            if (_model.ActiveCustomers.Count > 0) restTabTitle = $"♨ 식당({_model.ActiveCustomers.Count})";
            else if (_model.RestaurantOpen) restTabTitle = "♨ 식당(영업중)";
            else restTabTitle = "♨ 식당(마감)";

            bool anyPenNeedFeed = false;
            foreach (var pen in _model.LivestockPens)
            {
                if (pen.IsUnlocked && !pen.HasFeed) { anyPenNeedFeed = true; break; }
            }
            string liveTabTitle = anyPenNeedFeed && (!_model.Inventory.ContainsKey("crop_wheat") || _model.Inventory["crop_wheat"] <= 0)
                ? "● 목축(사료부족)"
                : "● 목축 (목장)";

            DrawTabButton(g, _rcTabFarm, "✦ 농장 (작물)", _currentTab == FarmTabType.Farm);
            DrawTabButton(g, _rcTabFishing, "~ 낚시 (어획)", _currentTab == FarmTabType.Fishing);
            DrawTabButton(g, _rcTabLivestock, liveTabTitle, _currentTab == FarmTabType.Livestock);
            DrawTabButton(g, _rcTabRestaurant, restTabTitle, _currentTab == FarmTabType.Restaurant);

            // [★ 업적] 버튼
            int claimable = _model.AchievementSystem != null ? _model.AchievementSystem.GetClaimableCount() : 0;
            string achTitle = claimable > 0 ? $"★ 업적 (+{claimable})" : "★ 업적";
            bool hoverAch = _rcBtnAchievements.Contains(_hoverPoint);
            g.FillRectangle(claimable > 0 ? (hoverAch ? _brMeadow : _brSoil) : (hoverAch ? _brCardHover : _brCardBg), _rcBtnAchievements);
            g.DrawRectangle(claimable > 0 ? _penGold : _penBorder, _rcBtnAchievements);
            g.DrawString(achTitle, _fontSmall, claimable > 0 ? _brTextGold : _brTextWhite, _rcBtnAchievements, _sfCenter);

            // [◆ 의뢰] 버튼
            int completedContracts = _model.ContractSystem != null ? _model.ContractSystem.DailyContracts.FindAll(c => c.IsCompleted).Count : 0;
            string contractTitle = $"◆ 의뢰 ({completedContracts}/3)";
            bool hoverContract = _rcBtnContracts.Contains(_hoverPoint);
            g.FillRectangle(hoverContract ? _brCardHover : _brCardBg, _rcBtnContracts);
            g.DrawRectangle(_model.ContractSystem != null && _model.ContractSystem.DailyBonusClaimed ? _penActive : _penBorder, _rcBtnContracts);
            g.DrawString(contractTitle, _fontSmall, _model.ContractSystem != null && _model.ContractSystem.DailyBonusClaimed ? _brTextGreen : _brTextWhite, _rcBtnContracts, _sfCenter);

            // 임시/테스트용 관리자 치트 & 초기화 버튼 (메인 구석)
            bool hoverAdmin = _rcBtnAdmin.Contains(_hoverPoint);
            SolidBrush brAdminBg = _model.IsAdminMode ? (hoverAdmin ? _brMeadow : _brCardBg) : (hoverAdmin ? _brCardHover : _brHeaderBg);
            Pen penAdmin = _model.IsAdminMode ? _penGold : _penBorder;
            g.FillRectangle(brAdminBg, _rcBtnAdmin);
            g.DrawRectangle(penAdmin, _rcBtnAdmin);
            g.DrawString(_model.IsAdminMode ? "[치트]무한" : "[치트]", _fontSmall, _model.IsAdminMode ? _brTextGold : _brTextWhite, _rcBtnAdmin, _sfCenter);

            bool hoverReset = _rcBtnReset.Contains(_hoverPoint);
            g.FillRectangle(hoverReset ? _brSoil : _brHeaderBg, _rcBtnReset);
            g.DrawRectangle(_penBorder, _rcBtnReset);
            g.DrawString("[초기화]", _fontSmall, hoverReset ? _brTextWhite : _brTextMuted, _rcBtnReset, _sfCenter);
        }

        private void DrawTabButton(Graphics g, Rectangle rc, string text, bool isSelected)
        {
            bool isHover = rc.Contains(_hoverPoint);
            SolidBrush bg = isSelected ? _brMeadow : (isHover ? _brCardHover : _brCardBg);
            g.FillRectangle(bg, rc);
            g.DrawRectangle(isSelected ? _penActive : _penBorder, rc);
            g.DrawString(text, _fontBold, isSelected ? _brTextWhite : _brTextMuted, rc.X + 12, rc.Y + 9);
        }

        private void DrawFarmTab(Graphics g)
        {
            int startX = 20;
            int startY = 105;
            int plotW = 188;
            int plotH = 140;
            int gapX = 18;
            int gapY = 18;

            for (int i = 0; i < _model.Plots.Count; i++)
            {
                var plot = _model.Plots[i];
                int col = i % 3;
                int row = i / 3;
                Rectangle rc = new Rectangle(startX + col * (plotW + gapX), startY + row * (plotH + gapY), plotW, plotH);
                plot.Bounds = rc;

                if (!plot.IsUnlocked)
                {
                    // 미해금 밭
                    g.FillRectangle(_brCardBg, rc);
                    g.DrawRectangle(_penBorder, rc);
                    g.DrawString("■ 미개간 밭", _fontHeader, _brTextMuted, rc.X + 48, rc.Y + 45);
                    g.DrawString($"개간비: {plot.UnlockCost}G (클릭)", _fontBold, _brTextGold, rc.X + 28, rc.Y + 75);
                    continue;
                }

                // 개간된 밭
                g.FillRectangle(_brSoil, rc);
                g.DrawRectangle(_penBorder, rc);

                var itemDef = _model.Items.ContainsKey(plot.CropId) ? _model.Items[plot.CropId] : null;
                string cropName = itemDef != null ? itemDef.Name : "작물";
                string icon = itemDef != null ? itemDef.IconSymbol : "✦";

                // 심볼 배지 렌더링
                Rectangle rcBadge = new Rectangle(rc.X + 14, rc.Y + 14, 32, 32);
                g.FillEllipse(_brCardBg, rcBadge);
                g.DrawEllipse(_penBorder, rcBadge);
                g.DrawString(icon, _fontIconLarge, Brushes.White, rc.X + 20, rc.Y + 18);

                g.DrawString(cropName, _fontBold, _brTextWhite, rc.X + 54, rc.Y + 18);
                g.DrawString("(클릭: 작물 전환)", _fontSmall, _brTextMuted, rc.X + 54, rc.Y + 36);

                if (plot.State == TaskPlotState.Ready)
                {
                    g.DrawString("[완성] 수확 가능!", _fontBold, _brTextGreen, rc.X + 35, rc.Y + 68);
                }
                else
                {
                    // 성장 게이지
                    int barW = rc.Width - 30;
                    int barH = 12;
                    Rectangle rcBarBg = new Rectangle(rc.X + 15, rc.Y + 75, barW, barH);
                    g.FillRectangle(_brHeaderBg, rcBarBg);

                    int filled = (int)(barW * Math.Min(1f, plot.GrowthProgress));
                    if (filled > 0)
                    {
                        g.FillRectangle(_brMeadow, rc.X + 15, rc.Y + 75, filled, barH);
                    }
                    g.DrawRectangle(_penBorder, rcBarBg);
                    g.DrawString($"성장 중 ({(int)(plot.GrowthProgress * 100)}%)", _fontSmall, _brTextMuted, rc.X + 48, rc.Y + 95);
                }
            }

            // 하단 헛간/창고 (일꾼 대기 구역) - 600px 너비로 균형 통일 및 팁 안내
            Rectangle rcBarn = new Rectangle(20, 425, this.Width - 40, 80);
            g.FillRectangle(_brCardBg, rcBarn);
            g.DrawRectangle(_penBorder, rcBarn);
            g.DrawString("■ 농가 & 중앙 헛간", _fontBold, _brTextWhite, rcBarn.X + 15, rcBarn.Y + 12);
            int farmBusy = _model.GetWorkersInRoleCount(WorkerRole.Farmer);
            int idle = _model.GetIdleWorkerCount();
            g.DrawString($"대기 농부: {idle}/{_model.Workers.Count}명 (밭 작업: {farmBusy}명)", _fontSmall, _brTextGreen, rcBarn.X + 15, rcBarn.Y + 35);
            g.DrawString("파견 시 밭 수확 및 물주기 가속(2배)", _fontSmall, _brTextMuted, rcBarn.X + 15, rcBarn.Y + 54);

            // 우측 안내 팁
            g.DrawString("★ TIP: 밭 클릭 시 개량된 작물(밀/황금밀/당근/감자/딸기)로 자유 전환!", _fontSmall, _brTextGold, rcBarn.X + 260, rcBarn.Y + 35);
            g.DrawString("생산된 곡물과 채소는 식당 요리 재료 또는 [$ 안전출하]로 환전됩니다.", _fontSmall, _brTextMuted, rcBarn.X + 260, rcBarn.Y + 54);
        }

        private void DrawFishingTab(Graphics g)
        {
            // 낚시터 캔버스
            Rectangle rcWater = new Rectangle(20, 105, this.Width - 40, 310);
            g.FillRectangle(_brWater, rcWater);
            g.DrawRectangle(_penBorder, rcWater);

            g.DrawString("~ 잔잔한 물결 낚시터", _fontHeader, _brTextWhite, 35, 120);

            int cardW = 280;
            int cardH = 118;
            int startX = 35;
            int startY = 155;
            int gapX = 14;
            int gapY = 14;

            for (int i = 0; i < _model.FishingSpots.Count; i++)
            {
                var spot = _model.FishingSpots[i];
                int col = i % 2;
                int row = i / 2;
                Rectangle rc = new Rectangle(startX + col * (cardW + gapX), startY + row * (cardH + gapY), cardW, cardH);
                spot.Bounds = rc;

                if (!spot.IsUnlocked)
                {
                    g.FillRectangle(_brCardBg, rc);
                    g.DrawRectangle(_penBorder, rc);
                    g.DrawString("■ 낚시 포인트 미개방", _fontBold, _brTextMuted, rc.X + 65, rc.Y + 38);
                    g.DrawString($"개방비: {spot.UnlockCost:N0}G (클릭)", _fontBold, _brTextGold, rc.X + 65, rc.Y + 65);
                    continue;
                }

                g.FillRectangle(_brWood, rc);
                g.DrawRectangle(_penBorder, rc);

                var itemDef = _model.Items.ContainsKey(spot.FishId) ? _model.Items[spot.FishId] : null;
                string fishName = itemDef != null ? itemDef.Name : "어종";
                string icon = itemDef != null ? itemDef.IconSymbol : "~>";

                Rectangle rcBadge = new Rectangle(rc.X + 14, rc.Y + 14, 38, 34);
                g.FillRectangle(_brCardBg, rcBadge);
                g.DrawRectangle(_penBorder, rcBadge);
                g.DrawString(icon, _fontIconSmall, _brTextBlue, rc.X + 17, rc.Y + 20);

                g.DrawString(spot.SpotName, _fontBold, _brTextWhite, rc.X + 60, rc.Y + 14);
                g.DrawString($"수확: {fishName} ({itemDef?.BasePrice}G)", _fontSmall, _brTextGold, rc.X + 60, rc.Y + 34);

                if (spot.State == TaskPlotState.Ready)
                {
                    g.DrawString("[완성] 어획 가능!", _fontBold, _brTextGreen, rc.X + 60, rc.Y + 75);
                }
                else
                {
                    int barW = rc.Width - 30;
                    Rectangle rcBar = new Rectangle(rc.X + 15, rc.Y + 75, barW, 12);
                    g.FillRectangle(_brHeaderBg, rcBar);
                    int fill = (int)(barW * Math.Min(1f, spot.CatchProgress));
                    if (fill > 0) g.FillRectangle(_brTextBlue, rc.X + 15, rc.Y + 75, fill, 12);
                    g.DrawRectangle(_penBorder, rcBar);
                }
            }

            // 하단 부두 데크 안내
            Rectangle rcDock = new Rectangle(20, 425, this.Width - 40, 80);
            g.FillRectangle(_brCardBg, rcDock);
            g.DrawRectangle(_penBorder, rcDock);
            g.DrawString("~ 어부 선착장 & 심해 원양 어장", _fontBold, _brTextWhite, rcDock.X + 15, rcDock.Y + 12);
            int fishBusy = _model.GetWorkersInRoleCount(WorkerRole.Fisherman);
            int idle = _model.GetIdleWorkerCount();
            g.DrawString($"대기 어부: {idle}/{_model.Workers.Count}명 (낚시 중: {fishBusy}명)", _fontSmall, _brTextBlue, rcDock.X + 15, rcDock.Y + 35);
            g.DrawString("수확된 생선은 요리(피라미 튀김, 잉어 찜, 연어 초밥, 오마카세) 또는 고가 출하 가능!", _fontSmall, _brTextGreen, rcDock.X + 15, rcDock.Y + 54);
        }

        private void DrawLivestockTab(Graphics g)
        {
            Rectangle rcField = new Rectangle(20, 105, this.Width - 40, 310);
            g.FillRectangle(_brMeadow, rcField);
            g.DrawRectangle(_penBorder, rcField);

            g.DrawString("● 푸른 목초지 방목장", _fontHeader, _brTextWhite, 35, 120);

            int cardW = 280;
            int cardH = 118;
            int startX = 35;
            int startY = 155;
            int gapX = 14;
            int gapY = 14;

            for (int i = 0; i < _model.LivestockPens.Count; i++)
            {
                var pen = _model.LivestockPens[i];
                int col = i % 2;
                int row = i / 2;
                Rectangle rc = new Rectangle(startX + col * (cardW + gapX), startY + row * (cardH + gapY), cardW, cardH);
                pen.Bounds = rc;

                if (!pen.IsUnlocked)
                {
                    g.FillRectangle(_brCardBg, rc);
                    g.DrawRectangle(_penBorder, rc);
                    g.DrawString("■ 축사 미증축", _fontBold, _brTextMuted, rc.X + 75, rc.Y + 38);
                    g.DrawString($"증축비: {pen.UnlockCost:N0}G (클릭)", _fontBold, _brTextGold, rc.X + 75, rc.Y + 65);
                    continue;
                }

                g.FillRectangle(_brWood, rc);
                g.DrawRectangle(_penBorder, rc);

                var itemDef = _model.Items.ContainsKey(pen.ProductId) ? _model.Items[pen.ProductId] : null;
                string prodName = itemDef != null ? itemDef.Name : "생산품";
                string icon = itemDef != null ? itemDef.IconSymbol : "(●)";

                Rectangle rcBadge = new Rectangle(rc.X + 14, rc.Y + 14, 38, 34);
                g.FillRectangle(_brCardBg, rcBadge);
                g.DrawRectangle(_penBorder, rcBadge);
                g.DrawString(icon, _fontIconSmall, _brTextGold, rc.X + 17, rc.Y + 20);

                g.DrawString(pen.AnimalName, _fontBold, _brTextWhite, rc.X + 60, rc.Y + 14);
                g.DrawString($"생산: {prodName} ({itemDef?.BasePrice}G)", _fontSmall, _brTextGold, rc.X + 60, rc.Y + 34);

                // 사료 상태
                string feedStr = pen.HasFeed ? "[사료완비] 2배 가속" : "[사료부족] 밀 자동투입 대기";
                g.DrawString(feedStr, _fontSmall, pen.HasFeed ? _brTextGreen : _brTextMuted, rc.X + 15, rc.Y + 56);

                if (pen.State == TaskPlotState.Ready)
                {
                    g.DrawString("[완성] 채집 가능!", _fontBold, _brTextGreen, rc.X + 60, rc.Y + 85);
                }
                else
                {
                    int barW = rc.Width - 30;
                    Rectangle rcBar = new Rectangle(rc.X + 15, rc.Y + 85, barW, 12);
                    g.FillRectangle(_brHeaderBg, rcBar);
                    int fill = (int)(barW * Math.Min(1f, pen.ProduceProgress));
                    if (fill > 0) g.FillRectangle(_brTextGreen, rc.X + 15, rc.Y + 85, fill, 12);
                    g.DrawRectangle(_penBorder, rcBar);
                }
            }

            Rectangle rcRanch = new Rectangle(20, 425, this.Width - 40, 80);
            g.FillRectangle(_brCardBg, rcRanch);
            g.DrawRectangle(_penBorder, rcRanch);
            g.DrawString("● 목동 관리실 & 초원 한우 목장", _fontBold, _brTextWhite, rcRanch.X + 15, rcRanch.Y + 12);
            int ranchBusy = _model.GetWorkersInRoleCount(WorkerRole.Rancher);
            int idle = _model.GetIdleWorkerCount();
            g.DrawString($"대기 목동: {idle}/{_model.Workers.Count}명 (목장 관리: {ranchBusy}명)", _fontSmall, _brTextGold, rcRanch.X + 15, rcRanch.Y + 35);
            g.DrawString("창고에 밀이 있으면 자동 사료 급여(2배)! 달걀/양모/우유/마블비프 생산", _fontSmall, _brTextGreen, rcRanch.X + 15, rcRanch.Y + 54);
        }

        private void DrawRestaurantTab(Graphics g)
        {
            Rectangle rcRest = new Rectangle(20, 105, this.Width - 40, 310);
            g.FillRectangle(_brCardBg, rcRest);
            g.DrawRectangle(_penBorder, rcRest);

            g.DrawString("♨ 포켓 비스트로 & 다이닝", _fontHeader, _brTextGold, 35, 120);
            g.DrawString("수확한 농작물, 갓 잡은 생선, 신선한 축산품으로 맛있는 코스 요리를 대접하세요!", _fontSmall, _brTextMuted, 35, 142);

            // 테이블 3개 렌더링
            int tableW = 175;
            int tableH = 135;
            for (int i = 0; i < 3; i++)
            {
                Rectangle rcTable = new Rectangle(40 + i * 195, 175, tableW, tableH);
                g.FillRectangle(_brHeaderBg, rcTable);

                // 손님이 앉아 있는지 확인
                RestaurantCustomer cust = _model.ActiveCustomers.Find(c => c.TableIndex == i);
                if (cust != null && cust.IsVip)
                {
                    g.DrawRectangle(_penGold, rcTable);
                }
                else
                {
                    g.DrawRectangle(_penBorder, rcTable);
                }

                string tableTitle = (cust != null && cust.IsVip) ? "★ VIP 다이닝석" : $"≡ 다이닝 테이블 #{i + 1}";
                g.DrawString(tableTitle, _fontBold, (cust != null && cust.IsVip) ? _brTextGold : _brTextWhite, rcTable.X + 18, rcTable.Y + 14);

                if (cust != null)
                {
                    Rectangle rcDishBadge = new Rectangle(rcTable.X + 12, rcTable.Y + 40, 48, 28);
                    g.FillRectangle(_brCardBg, rcDishBadge);
                    g.DrawRectangle(cust.IsVip ? _penGold : _penBorder, rcDishBadge);
                    g.DrawString(cust.OrderedRecipe.DishSymbol, _fontSmall, _brTextGold, rcTable.X + 15, rcTable.Y + 46);

                    string custInfo = cust.IsVip ? $"{cust.OrderedRecipe.Name} (VIP)" : cust.OrderedRecipe.Name;
                    g.DrawString(custInfo, _fontBold, cust.IsVip ? _brTextGold : _brTextWhite, rcTable.X + 66, rcTable.Y + 42);
                    g.DrawString($"결제 예정: {cust.TotalPaid:N0}G", _fontSmall, _brTextGreen, rcTable.X + 66, rcTable.Y + 62);

                    // 식사 게이지
                    int barW = rcTable.Width - 30;
                    Rectangle rcBar = new Rectangle(rcTable.X + 15, rcTable.Y + 98, barW, 10);
                    g.FillRectangle(_brBg, rcBar);
                    int fill = (int)(barW * Math.Min(1f, cust.EatProgress));
                    if (fill > 0) g.FillRectangle(cust.IsVip ? _brTextGold : _brTextGreen, rcTable.X + 15, rcTable.Y + 98, fill, 10);
                    g.DrawRectangle(_penBorder, rcBar);
                }
                else
                {
                    if (_model.RestaurantOpen)
                    {
                        bool anyCookable = false;
                        foreach (var r in _model.Recipes)
                        {
                            if (r.IsUnlocked && _model.CanCookRecipe(r))
                            {
                                anyCookable = true;
                                break;
                            }
                        }
                        if (anyCookable)
                        {
                            g.DrawString("손님 방문 대기 중...", _fontBody, _brTextGreen, rcTable.X + 28, rcTable.Y + 65);
                        }
                        else
                        {
                            g.DrawString("식재료 부족 (조리 대기)", _fontSmall, _brTextMuted, rcTable.X + 18, rcTable.Y + 65);
                        }
                    }
                    else
                    {
                        g.DrawString("영업 준비 중 (마감)", _fontBody, _brTextMuted, rcTable.X + 32, rcTable.Y + 65);
                    }
                }
            }

            // 하단 영업 개시 토글 버튼
            Rectangle rcToggle = new Rectangle(235, 350, 180, 42);
            bool isHoverToggle = rcToggle.Contains(_hoverPoint);
            SolidBrush brToggle = _model.RestaurantOpen ? (isHoverToggle ? _brCardHover : _brSoil) : (isHoverToggle ? _brMeadow : _brCardBg);
            g.FillRectangle(brToggle, rcToggle);
            g.DrawRectangle(_model.RestaurantOpen ? _penGold : _penBorder, rcToggle);

            string toggleText = _model.RestaurantOpen ? "[마감] 영업 종료하기" : "[영업] 다이닝 오픈!";
            g.DrawString(toggleText, _fontBold, _model.RestaurantOpen ? _brTextGold : _brTextGreen, rcToggle, _sfCenter);
        }

        private void DrawWorkers(Graphics g)
        {
            // 현재 활성화된 탭에 해당하는 작업자만 선별 렌더링 (타 탭으로 이동 시 화면 간섭 0화)
            foreach (var w in _model.Workers)
            {
                if (w.State == WorkerState.Idle) continue;

                // 탭과 일꾼 역할 일치 검사
                bool matchesTab = false;
                if (_currentTab == FarmTabType.Farm && w.Role == WorkerRole.Farmer) matchesTab = true;
                else if (_currentTab == FarmTabType.Fishing && w.Role == WorkerRole.Fisherman) matchesTab = true;
                else if (_currentTab == FarmTabType.Livestock && w.Role == WorkerRole.Rancher) matchesTab = true;

                if (!matchesTab) continue;

                int x = (int)w.CurrentPos.X;
                int y = (int)w.CurrentPos.Y;
                Rectangle rcAvatar = new Rectangle(x - 13, y - 13, 26, 26);

                // 역할별 뱃지 컬러 및 직관적인 한글 표기 (농: 농부, 어: 어부, 목: 목동)
                SolidBrush brBadge = _brMeadow;
                string roleText = "농";
                if (w.Role == WorkerRole.Fisherman)
                {
                    brBadge = _brWater;
                    roleText = "어";
                }
                else if (w.Role == WorkerRole.Rancher)
                {
                    brBadge = _brWood;
                    roleText = "목";
                }

                g.FillEllipse(brBadge, rcAvatar);
                g.DrawEllipse(_penGold, rcAvatar);
                g.DrawString(roleText, _fontBold, Brushes.White, x - 7, y - 7);

                // 운반 중인 물품 아이콘 표시 (품목 기호)
                if (w.State == WorkerState.ReturningToStorage && !string.IsNullOrEmpty(w.CarriedItemId))
                {
                    string carrySym = "✦";
                    if (_model.Items.ContainsKey(w.CarriedItemId))
                    {
                        carrySym = _model.Items[w.CarriedItemId].IconSymbol;
                    }
                    Rectangle rcBag = new Rectangle(x - 14, y - 28, 28, 16);
                    g.FillRectangle(_brHeaderBg, rcBag);
                    g.DrawRectangle(_penBorder, rcBag);
                    g.DrawString(carrySym, _fontSmall, _brTextGold, x - 12, y - 28);
                }
            }
        }

        private static readonly StringFormat _sfCenter = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        private void DrawBottomBar(Graphics g)
        {
            int y = 525;
            int btnH = 38;

            _rcBtnDispatch = new Rectangle(10, y, 84, btnH);
            _rcBtnAutoToggle = new Rectangle(98, y, 84, btnH);
            _rcBtnSellAll = new Rectangle(186, y, 76, btnH);
            _rcBtnUpgrade = new Rectangle(266, y, 92, btnH);
            _rcBtnTech = new Rectangle(362, y, 92, btnH);
            _rcBtnBreeding = new Rectangle(458, y, 92, btnH);
            _rcBtnRecipes = new Rectangle(554, y, 84, btnH);

            string dispatchText = "▶ 파견";
            if (_currentTab == FarmTabType.Farm) dispatchText = "▶ 밭 파견";
            else if (_currentTab == FarmTabType.Fishing) dispatchText = "▶ 낚시 파견";
            else if (_currentTab == FarmTabType.Livestock) dispatchText = "▶ 목축 파견";

            bool hasAuto = _model.IsTechUnlocked("tech_auto_dispatch");
            string autoText = hasAuto ? (_model.AutoDispatch ? "[자동] ON" : "[자동] OFF") : "[잠김] 자동";
            SolidBrush autoBg = hasAuto ? (_model.AutoDispatch ? _brCardBg : _brHeaderBg) : _brHeaderBg;
            Pen autoPen = hasAuto ? (_model.AutoDispatch ? _penActive : _penBorder) : _penBorder;
            SolidBrush autoTextBr = hasAuto ? (_model.AutoDispatch ? _brTextGreen : _brTextMuted) : _brTextMuted;

            DrawActionButton(g, _rcBtnDispatch, dispatchText, _brMeadow, _penActive, _brTextWhite);
            DrawActionButton(g, _rcBtnAutoToggle, autoText, autoBg, autoPen, autoTextBr);
            DrawActionButton(g, _rcBtnSellAll, "$ 안전출하", _brCardBg, _penGold, _brTextGold);
            DrawActionButton(g, _rcBtnUpgrade, "[도구/설비]", _brCardBg, _penBorder, _brTextWhite);
            DrawActionButton(g, _rcBtnTech, "[기술트리]", _brCardBg, _penGold, _brTextGold);
            DrawActionButton(g, _rcBtnBreeding, "[품종개량]", _brCardBg, _penBorder, _brTextBlue);
            DrawActionButton(g, _rcBtnRecipes, "[레시피]", _brCardBg, _penBorder, _brTextGreen);
        }

        private void DrawActionButton(Graphics g, Rectangle rc, string text, SolidBrush bg, Pen pen, SolidBrush textBr)
        {
            bool isHover = rc.Contains(_hoverPoint);
            g.FillRectangle(isHover ? _brCardHover : bg, rc);
            g.DrawRectangle(pen, rc);
            g.DrawString(text, _fontBold, textBr, rc, _sfCenter);
        }

        private void DrawFloatingTexts(Graphics g)
        {
            foreach (var ft in _model.FloatingTexts)
            {
                int a = Math.Max(0, Math.Min(255, (int)(ft.Alpha * 255)));
                _brFloatingShadow.Color = Color.FromArgb(a / 2, 0, 0, 0);
                _brFloatingText.Color = Color.FromArgb(a, ft.Color);

                // 1px 그림자 및 중심 정렬로 모든 배경에서 100% 가독성 보장 (GC 0 Byte)
                g.DrawString(ft.Text, _fontBold, _brFloatingShadow, ft.X + 1, ft.Y + 1, _sfCenter);
                g.DrawString(ft.Text, _fontBold, _brFloatingText, ft.X, ft.Y, _sfCenter);
            }
        }

    }
}
