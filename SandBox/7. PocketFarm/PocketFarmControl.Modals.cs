using System;
using System.Drawing;

namespace GateHelper.PocketFarm
{
    public partial class PocketFarmControl
    {
        private Rectangle _rcModalClose;
        private Rectangle _rcModalBox;

        // 업그레이드 버튼 캐시 (분야별 도구 3종 + 인프라 3종)
        private Rectangle _rcBtnUpgFarmTool;
        private Rectangle _rcBtnUpgFishingTool;
        private Rectangle _rcBtnUpgLivestockTool;
        private Rectangle _rcBtnUpgWorker;
        private Rectangle _rcBtnUpgCart;
        private Rectangle _rcBtnUpgSilo;

        // 기술 연구 나무 버튼 캐시 (11종)
        private Rectangle[] _rcBtnTechNodes;

        // 품종개량 버튼 캐시
        private Rectangle[] _rcBtnBreedings;

        // 레시피 버튼 캐시
        private Rectangle[] _rcBtnRecipeList;

        // 업적 버튼 캐시
        private Rectangle[] _rcBtnAchClaim;

        // 납품 계약 버튼 캐시
        private Rectangle[] _rcBtnContractFulfill;

        private static readonly StringFormat _sfWrap = new StringFormat
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Near,
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = 0
        };

        private static readonly SolidBrush _brModalDim = new SolidBrush(Color.FromArgb(180, 10, 14, 20));

        private void DrawActiveModal(Graphics g)
        {
            // 1. 반투명 딤(Dim) 배경 (정적 캐시 0 Byte GC)
            g.FillRectangle(_brModalDim, this.ClientRectangle);

            // 2. 모달 컨테이너 박스 (기술트리, 업적, 의뢰는 와이드 뷰 적용)
            bool isWide = (_activeModal == ModalViewType.TechTree || _activeModal == ModalViewType.Achievements || _activeModal == ModalViewType.Contracts);
            int modalW = isWide ? 620 : 560;
            int modalH = isWide ? 506 : 480;
            int modalX = (this.Width - modalW) / 2;
            int modalY = isWide ? 22 : 44;
            _rcModalBox = new Rectangle(modalX, modalY, modalW, modalH);

            g.FillRectangle(_brHeaderBg, _rcModalBox);
            g.DrawRectangle(_penActive, _rcModalBox);

            // 닫기 버튼
            _rcModalClose = new Rectangle(modalX + modalW - 38, modalY + 12, 26, 26);
            bool isHoverClose = _rcModalClose.Contains(_hoverPoint);
            g.FillRectangle(isHoverClose ? _brSoil : _brCardBg, _rcModalClose);
            g.DrawRectangle(_penBorder, _rcModalClose);
            g.DrawString("✕", _fontBold, _brTextWhite, _rcModalClose, _sfCenter);

            // 모달별 내용 렌더링
            switch (_activeModal)
            {
                case ModalViewType.Upgrades:
                    DrawUpgradeModal(g, modalX, modalY);
                    break;
                case ModalViewType.TechTree:
                    DrawTechTreeModal(g, modalX, modalY);
                    break;
                case ModalViewType.Breeding:
                    DrawBreedingModal(g, modalX, modalY);
                    break;
                case ModalViewType.Recipes:
                    DrawRecipesModal(g, modalX, modalY);
                    break;
                case ModalViewType.Achievements:
                    DrawAchievementsModal(g, modalX, modalY);
                    break;
                case ModalViewType.Contracts:
                    DrawContractsModal(g, modalX, modalY);
                    break;
            }
        }

        private void DrawUpgradeModal(Graphics g, int mx, int my)
        {
            g.DrawString("[도구] 농장 & 작업 도구 업그레이드", _fontTitle, _brTextWhite, mx + 24, my + 14);
            g.DrawString("작업별(농업·어업·목축) 전용 도구와 일꾼 고용, 창고 증축으로 효율을 극대화합니다.", _fontSmall, _brTextMuted, mx + 24, my + 40);

            int startY = my + 64;
            int rowH = 62;

            // 1. 농업 도구 강화 (최대 Lv.5)
            bool isFarmMax = _model.FarmToolLevel >= 5;
            int costFarmTool = 70 * _model.FarmToolLevel;
            _rcBtnUpgFarmTool = new Rectangle(mx + 380, startY + 12, 124, 32);
            string farmToolName = _model.GetFarmToolName(_model.FarmToolLevel);
            DrawUpgradeRow(g, mx + 24, startY, $"[농업 도구] {farmToolName}", isFarmMax ? "Lv.5 (최고 레벨) / 밭 수확속도 +100%" : $"Lv.{_model.FarmToolLevel} / 밭 수확 및 가꾸기 작업속도 +25%", isFarmMax ? "[MAX] 완료" : $"비용: {costFarmTool}G", _rcBtnUpgFarmTool, !isFarmMax && _model.Gold >= costFarmTool, isFarmMax);

            // 2. 어업 도구 강화 (최대 Lv.5)
            bool isFishMax = _model.FishingToolLevel >= 5;
            int costFishTool = 85 * _model.FishingToolLevel;
            _rcBtnUpgFishingTool = new Rectangle(mx + 380, startY + rowH + 12, 124, 32);
            string fishToolName = _model.GetFishingToolName(_model.FishingToolLevel);
            DrawUpgradeRow(g, mx + 24, startY + rowH, $"[어업 도구] {fishToolName}", isFishMax ? "Lv.5 (최고 레벨) / 낚시 어획속도 +100%" : $"Lv.{_model.FishingToolLevel} / 낚시터 어획 작업속도 +25%", isFishMax ? "[MAX] 완료" : $"비용: {costFishTool}G", _rcBtnUpgFishingTool, !isFishMax && _model.Gold >= costFishTool, isFishMax);

            // 3. 목축 도구 강화 (최대 Lv.5)
            bool isLiveMax = _model.LivestockToolLevel >= 5;
            int costLiveTool = 95 * _model.LivestockToolLevel;
            _rcBtnUpgLivestockTool = new Rectangle(mx + 380, startY + rowH * 2 + 12, 124, 32);
            string liveToolName = _model.GetLivestockToolName(_model.LivestockToolLevel);
            DrawUpgradeRow(g, mx + 24, startY + rowH * 2, $"[목축 도구] {liveToolName}", isLiveMax ? "Lv.5 (최고 레벨) / 축사 채집속도 +100%" : $"Lv.{_model.LivestockToolLevel} / 축사 채집 및 돌보기 작업속도 +25%", isLiveMax ? "[MAX] 완료" : $"비용: {costLiveTool}G", _rcBtnUpgLivestockTool, !isLiveMax && _model.Gold >= costLiveTool, isLiveMax);

            // 4. 일꾼 추가 고용 (최대 8명)
            bool isWorkerMax = _model.Workers.Count >= 8;
            int costWorker = 60 * _model.WorkerCountUpgradeLevel;
            _rcBtnUpgWorker = new Rectangle(mx + 380, startY + rowH * 3 + 12, 124, 32);
            DrawUpgradeRow(g, mx + 24, startY + rowH * 3, "[일꾼] 일꾼 인원 추가 고용", isWorkerMax ? "정원 달성 (8/8명) / 모든 일터 자율 지원" : $"현재 {_model.Workers.Count}명 / 모든 일터 대기 인원 자동 지원", isWorkerMax ? "[정원 달성]" : $"비용: {costWorker}G", _rcBtnUpgWorker, !isWorkerMax && _model.Gold >= costWorker, isWorkerMax);

            // 5. 손수레 개량 (최대 Lv.5)
            bool isCartMax = _model.CartCapacityUpgradeLevel >= 5;
            int costCart = 100 * _model.CartCapacityUpgradeLevel;
            _rcBtnUpgCart = new Rectangle(mx + 380, startY + rowH * 4 + 12, 124, 32);
            DrawUpgradeRow(g, mx + 24, startY + rowH * 4, "[손수레] 손수레 & 바구니 개량", isCartMax ? "Lv.5 (최고 레벨) / 운반적재량 5개, 이속 +80%" : $"Lv.{_model.CartCapacityUpgradeLevel} / 운반적재량 +1, 이동속도 +20%", isCartMax ? "[MAX] 완료" : $"비용: {costCart}G", _rcBtnUpgCart, !isCartMax && _model.Gold >= costCart, isCartMax);

            // 6. 창고 증축 (최대 Lv.10, 600칸)
            bool isSiloMax = _model.SiloUpgradeLevel >= 10;
            int costSilo = 90 * _model.SiloUpgradeLevel;
            _rcBtnUpgSilo = new Rectangle(mx + 380, startY + rowH * 5 + 12, 124, 32);
            DrawUpgradeRow(g, mx + 24, startY + rowH * 5, "[창고] 중앙 보관 창고 증축", isSiloMax ? $"최대 확장 완료 ({_model.SiloCapacity}개 보관)" : $"현재 최대 {_model.SiloCapacity}개 보관 가능 (+60칸 확장)", isSiloMax ? "[MAX] 완료" : $"비용: {costSilo}G", _rcBtnUpgSilo, !isSiloMax && _model.Gold >= costSilo, isSiloMax);
        }

        private void DrawUpgradeRow(Graphics g, int x, int y, string title, string sub, string cost, Rectangle btnRc, bool canAfford, bool isMax = false)
        {
            Rectangle rcBg = new Rectangle(x, y, 492, 54);
            g.FillRectangle(_brCardBg, rcBg);
            g.DrawRectangle(_penBorder, rcBg);

            g.DrawString(title, _fontBold, _brTextWhite, x + 12, y + 8);
            g.DrawString(sub, _fontSmall, _brTextMuted, x + 12, y + 30);

            bool isHover = btnRc.Contains(_hoverPoint);
            SolidBrush brBtn = isMax ? _brHeaderBg : (canAfford ? (isHover ? _brMeadow : _brSoil) : _brCardHover);
            Pen penBtn = isMax ? _penBorder : (canAfford ? _penActive : _penBorder);
            SolidBrush brText = isMax ? _brTextMuted : (canAfford ? _brTextGold : _brTextMuted);

            g.FillRectangle(brBtn, btnRc);
            g.DrawRectangle(penBtn, btnRc);
            g.DrawString(cost, _fontBold, brText, btnRc, _sfCenter);
        }

        private void DrawTechTreeModal(Graphics g, int mx, int my)
        {
            if (_model == null || _model.TechTree.Count < 11) return;

            g.DrawString("★ 포켓 테크놀로지 (기술 연구 나무)", _fontTitle, _brTextGold, mx + 20, my + 12);
            g.DrawString("골드(G)와 식당 명성(P)을 연구비로 투자하여 5대 산업 분야 테크트리를 단계별로 해금합니다.", _fontSmall, _brTextMuted, mx + 20, my + 34);

            int count = _model.TechTree.Count;
            if (_rcBtnTechNodes == null || _rcBtnTechNodes.Length != count)
            {
                _rcBtnTechNodes = new Rectangle[count];
            }

            int startY = my + 52;
            int rowH = 78;

            // 1. 농경 혁신 (Agriculture: 2단계)
            int y0 = startY;
            DrawTrackBadge(g, new Rectangle(mx + 16, y0, 68, rowH), "[농경]", "농업혁신");
            DrawTechCard(g, _model.TechTree[0], new Rectangle(mx + 92, y0, 222, rowH), 0);
            DrawTreeConnector(g, mx + 314, y0 + rowH / 2, mx + 352, _model.IsTechUnlocked(_model.TechTree[0].Id));
            DrawTechCard(g, _model.TechTree[1], new Rectangle(mx + 352, y0, 222, rowH), 1);

            // 2. 수산 공학 (Fishery: 2단계)
            int y1 = startY + 88;
            DrawTrackBadge(g, new Rectangle(mx + 16, y1, 68, rowH), "[수산]", "수산공학");
            DrawTechCard(g, _model.TechTree[2], new Rectangle(mx + 92, y1, 222, rowH), 2);
            DrawTreeConnector(g, mx + 314, y1 + rowH / 2, mx + 352, _model.IsTechUnlocked(_model.TechTree[2].Id));
            DrawTechCard(g, _model.TechTree[3], new Rectangle(mx + 352, y1, 222, rowH), 3);

            // 3. 생명 공학 (Livestock: 2단계)
            int y2 = startY + 88 * 2;
            DrawTrackBadge(g, new Rectangle(mx + 16, y2, 68, rowH), "[목축]", "생명공학");
            DrawTechCard(g, _model.TechTree[4], new Rectangle(mx + 92, y2, 222, rowH), 4);
            DrawTreeConnector(g, mx + 314, y2 + rowH / 2, mx + 352, _model.IsTechUnlocked(_model.TechTree[4].Id));
            DrawTechCard(g, _model.TechTree[5], new Rectangle(mx + 352, y2, 222, rowH), 5);

            // 4. 자동 및 보존 과학 (Logistics: 3단계 트리!)
            int y3 = startY + 88 * 3;
            DrawTrackBadge(g, new Rectangle(mx + 16, y3, 68, rowH), "[자동]", "물류보존");
            DrawTechCard(g, _model.TechTree[6], new Rectangle(mx + 92, y3, 148, rowH), 6);
            DrawTreeConnector(g, mx + 240, y3 + rowH / 2, mx + 262, _model.IsTechUnlocked(_model.TechTree[6].Id));
            DrawTechCard(g, _model.TechTree[7], new Rectangle(mx + 262, y3, 148, rowH), 7);
            DrawTreeConnector(g, mx + 410, y3 + rowH / 2, mx + 432, _model.IsTechUnlocked(_model.TechTree[7].Id));
            DrawTechCard(g, _model.TechTree[8], new Rectangle(mx + 432, y3, 148, rowH), 8);

            // 5. 외식 및 유통 (Commerce: 2단계)
            int y4 = startY + 88 * 4;
            DrawTrackBadge(g, new Rectangle(mx + 16, y4, 68, rowH), "[유통]", "외식유통");
            DrawTechCard(g, _model.TechTree[9], new Rectangle(mx + 92, y4, 222, rowH), 9);
            DrawTreeConnector(g, mx + 314, y4 + rowH / 2, mx + 352, _model.IsTechUnlocked(_model.TechTree[9].Id));
            DrawTechCard(g, _model.TechTree[10], new Rectangle(mx + 352, y4, 222, rowH), 10);
        }

        private void DrawTrackBadge(Graphics g, Rectangle rc, string symbol, string title)
        {
            g.FillRectangle(_brCardBg, rc);
            g.DrawRectangle(_penBorder, rc);
            g.DrawString(symbol, _fontBold, _brTextGold, rc.X + 11, rc.Y + 16);
            g.DrawString(title, _fontSmall, _brTextWhite, rc.X + 8, rc.Y + 42);
        }

        private void DrawTechCard(Graphics g, TechNodeDef t, Rectangle rc, int btnIdx)
        {
            bool isUnlocked = t.IsUnlocked;
            bool parentReady = string.IsNullOrEmpty(t.RequiredParentId) || _model.IsTechUnlocked(t.RequiredParentId);
            bool canAfford = parentReady && _model.Gold >= t.CostGold && _model.Fame >= t.CostFame;

            SolidBrush bg = isUnlocked ? _brCardBg : (parentReady ? _brCardBg : _brHeaderBg);
            Pen pen = isUnlocked ? _penActive : (canAfford ? _penGold : _penBorder);
            g.FillRectangle(bg, rc);
            g.DrawRectangle(pen, rc);

            // 1. 단계 및 기술명
            string tierStr = $"[{t.Tier}단계] {t.Name}";
            g.DrawString(tierStr, _fontBold, isUnlocked ? _brTextGreen : (parentReady ? _brTextWhite : _brTextMuted), rc.X + 6, rc.Y + 4);

            // 2. 효과 요약
            g.DrawString(t.EffectSummary, _fontSmall, isUnlocked ? _brTextGold : (parentReady ? _brTextGreen : _brTextMuted), rc.X + 6, rc.Y + 20);

            // 3. 설명 (글자 잘림 및 폼 밖 튀어나감 100% 방지: rcDesc 경계 내 텍스트 줄바꿈 및 말줄임)
            RectangleF rcDesc = new RectangleF(rc.X + 6, rc.Y + 34, rc.Width - 12, 18);
            g.DrawString(t.Description, _fontSmall, _brTextMuted, rcDesc, _sfWrap);

            // 4. 연구 실행 버튼
            Rectangle rcBtn = new Rectangle(rc.X + 6, rc.Y + 54, rc.Width - 12, 19);
            _rcBtnTechNodes[btnIdx] = rcBtn;

            if (isUnlocked)
            {
                g.FillRectangle(_brHeaderBg, rcBtn);
                g.DrawRectangle(_penActive, rcBtn);
                g.DrawString("[완료] 연구 완료", _fontSmall, _brTextGreen, rcBtn, _sfCenter);
            }
            else if (!parentReady)
            {
                g.FillRectangle(_brHeaderBg, rcBtn);
                g.DrawRectangle(_penBorder, rcBtn);
                g.DrawString("[잠김] 선행 연구 필요", _fontSmall, _brTextMuted, rcBtn, _sfCenter);
            }
            else
            {
                bool isHover = rcBtn.Contains(_hoverPoint);
                SolidBrush btnBg = canAfford ? (isHover ? _brMeadow : _brSoil) : _brCardHover;
                Pen btnPen = canAfford ? _penActive : _penBorder;
                g.FillRectangle(btnBg, rcBtn);
                g.DrawRectangle(btnPen, rcBtn);
                string costText = t.CostFame > 0 ? $"{t.CostGold}G|명성{t.CostFame}P" : $"{t.CostGold}G 연구";
                g.DrawString(costText, _fontSmall, canAfford ? _brTextGold : _brTextMuted, rcBtn, _sfCenter);
            }
        }

        private void DrawTreeConnector(Graphics g, int x1, int y, int x2, bool isActivated)
        {
            Pen p = isActivated ? _penActive : _penBorder;
            g.DrawLine(p, x1, y, x2, y);

            Point[] arrow = new Point[]
            {
                new Point(x2, y),
                new Point(x2 - 6, y - 4),
                new Point(x2 - 6, y + 4)
            };
            g.FillPolygon(isActivated ? _brTextGreen : _brTextMuted, arrow);
        }

        private void DrawBreedingModal(Graphics g, int mx, int my)
        {
            g.DrawString("✦ 품종 개량 연구소", _fontTitle, _brTextBlue, mx + 24, my + 18);
            g.DrawString("수확한 작물을 연구소에 축적하여 더 높은 가치의 상위 신품종을 해금합니다.", _fontSmall, _brTextMuted, mx + 24, my + 46);

            int startY = my + 78;
            int count = _model.Breedings.Count;
            if (_rcBtnBreedings == null || _rcBtnBreedings.Length != count)
            {
                _rcBtnBreedings = new Rectangle[count];
            }

            for (int i = 0; i < count; i++)
            {
                var breed = _model.Breedings[i];
                int y = startY + i * 82;
                Rectangle rcBg = new Rectangle(mx + 24, y, 492, 72);
                g.FillRectangle(_brCardBg, rcBg);
                g.DrawRectangle(_penBorder, rcBg);

                var baseItem = _model.Items.ContainsKey(breed.BaseCropId) ? _model.Items[breed.BaseCropId] : null;
                var evoItem = _model.Items.ContainsKey(breed.EvolvedCropId) ? _model.Items[breed.EvolvedCropId] : null;
                string baseName = baseItem != null ? baseItem.Name : breed.BaseCropId;
                string evoName = evoItem != null ? evoItem.Name : breed.EvolvedCropId;
                int currentStock = _model.Inventory.ContainsKey(breed.BaseCropId) ? _model.Inventory[breed.BaseCropId] : 0;

                g.DrawString($"{baseName} ➔ {evoName}", _fontBold, breed.IsResearched ? _brTextGreen : _brTextWhite, rcBg.X + 14, y + 10);
                g.DrawString(breed.Description, _fontSmall, _brTextMuted, rcBg.X + 14, y + 32);

                string stockStr = $"축적량: {currentStock}/{breed.RequiredStockpile}개  |  연구비: {breed.ResearchCostGold}G";
                g.DrawString(stockStr, _fontSmall, currentStock >= breed.RequiredStockpile ? _brTextGreen : _brTextGold, rcBg.X + 14, y + 49);

                // 연구 버튼
                _rcBtnBreedings[i] = new Rectangle(mx + 385, y + 18, 115, 36);
                if (breed.IsResearched)
                {
                    g.FillRectangle(_brCardBg, _rcBtnBreedings[i]);
                    g.DrawRectangle(_penActive, _rcBtnBreedings[i]);
                    g.DrawString("완료 됨 ★", _fontBold, _brTextGreen, _rcBtnBreedings[i], _sfCenter);
                }
                else
                {
                    bool canResearch = currentStock >= breed.RequiredStockpile && _model.Gold >= breed.ResearchCostGold;
                    bool isHover = _rcBtnBreedings[i].Contains(_hoverPoint);
                    g.FillRectangle(canResearch ? (isHover ? _brMeadow : _brSoil) : _brCardHover, _rcBtnBreedings[i]);
                    g.DrawRectangle(canResearch ? _penActive : _penBorder, _rcBtnBreedings[i]);
                    g.DrawString("연구 시작", _fontBold, canResearch ? _brTextWhite : _brTextMuted, _rcBtnBreedings[i], _sfCenter);
                }
            }
        }

        private void DrawRecipesModal(Graphics g, int mx, int my)
        {
            g.DrawString("≡ 포켓 비스트로 레시피 북 (6종 명품 코스)", _fontTitle, _brTextGold, mx + 20, my + 14);
            g.DrawString("농산물, 어획물, 축산물을 조합하여 명품 요리를 조리하고 영업 시 대접합니다.", _fontSmall, _brTextMuted, mx + 20, my + 38);

            int startY = my + 60;
            int count = _model.Recipes.Count;
            if (_rcBtnRecipeList == null || _rcBtnRecipeList.Length != count)
            {
                _rcBtnRecipeList = new Rectangle[count];
            }

            for (int i = 0; i < count; i++)
            {
                var r = _model.Recipes[i];
                int y = startY + i * 66;
                Rectangle rcBg = new Rectangle(mx + 18, y, 524, 60);
                g.FillRectangle(_brCardBg, rcBg);
                g.DrawRectangle(_penBorder, rcBg);

                Rectangle rcDishBadge = new Rectangle(rcBg.X + 10, y + 14, 54, 32);
                g.FillRectangle(_brHeaderBg, rcDishBadge);
                g.DrawRectangle(_penBorder, rcDishBadge);
                g.DrawString(r.DishSymbol, _fontSmall, _brTextGold, rcDishBadge, _sfCenter);

                g.DrawString(r.Name, _fontBold, _brTextWhite, rcBg.X + 72, y + 9);
                string masteryPerk = r.MasteryLevel == 3 ? " [★3 MASTER: 골드+30%, 명성+40%, 30% 더블결제]" : (r.MasteryLevel == 2 ? " [★2: 골드+15%, 명성+20%]" : "");
                string priceStr = $"판매가: {r.SellingPrice:N0}G | 명성: +{r.FameGain}P | {r.MasteryStars} ({r.CookCount}회 조리)";
                g.DrawString(priceStr, _fontSmall, _brTextGold, rcBg.X + 185, y + 10);
                g.DrawString(r.Description + masteryPerk, _fontSmall, r.MasteryLevel > 1 ? _brTextGreen : _brTextMuted, rcBg.X + 72, y + 33);

                _rcBtnRecipeList[i] = new Rectangle(mx + 412, y + 13, 102, 34);
                if (r.IsUnlocked)
                {
                    bool canCook = _model.CanCookRecipe(r);
                    bool isHover = _rcBtnRecipeList[i].Contains(_hoverPoint);
                    g.FillRectangle(canCook ? (isHover ? _brMeadow : _brSoil) : _brCardHover, _rcBtnRecipeList[i]);
                    g.DrawRectangle(canCook ? _penGold : _penBorder, _rcBtnRecipeList[i]);
                    g.DrawString(canCook ? "[조리 준비]" : "[재료 부족]", _fontSmall, canCook ? _brTextGold : _brTextMuted, _rcBtnRecipeList[i], _sfCenter);
                }
                else
                {
                    bool canUnlock = _model.Gold >= r.UnlockCost;
                    bool isHover = _rcBtnRecipeList[i].Contains(_hoverPoint);
                    g.FillRectangle(canUnlock ? (isHover ? _brMeadow : _brSoil) : _brCardHover, _rcBtnRecipeList[i]);
                    g.DrawRectangle(canUnlock ? _penActive : _penBorder, _rcBtnRecipeList[i]);
                    g.DrawString($"{r.UnlockCost}G 해금", _fontBold, canUnlock ? _brTextWhite : _brTextMuted, _rcBtnRecipeList[i], _sfCenter);
                }
            }
        }

        private void DrawAchievementsModal(Graphics g, int mx, int my)
        {
            if (_model.AchievementSystem == null) return;
            var list = _model.AchievementSystem.Achievements;
            int count = list.Count;
            if (_rcBtnAchClaim == null || _rcBtnAchClaim.Length != count)
            {
                _rcBtnAchClaim = new Rectangle[count];
            }

            int claimableCount = _model.AchievementSystem.GetClaimableCount();
            int completedCount = list.FindAll(a => a.IsCompleted).Count;

            g.DrawString($"[★ 업적] 명예의 전당 (달성 {completedCount}/{count})", _fontTitle, _brTextGold, mx + 20, my + 14);
            g.DrawString("농장 경영과 레스토랑 운영의 마일스톤을 달성하고 특별 지원금을 수령하세요.", _fontSmall, _brTextMuted, mx + 20, my + 38);

            int startY = my + 64;
            int cardW = 278;
            int cardH = 72;
            int gapX = 16;
            int gapY = 8;

            for (int i = 0; i < count; i++)
            {
                var ach = list[i];
                int col = i % 2;
                int row = i / 2;
                int x = mx + 22 + col * (cardW + gapX);
                int y = startY + row * (cardH + gapY);

                Rectangle rcCard = new Rectangle(x, y, cardW, cardH);
                g.FillRectangle(_brCardBg, rcCard);
                g.DrawRectangle(ach.IsClaimed ? _penBorder : (ach.IsCompleted ? _penGold : _penBorder), rcCard);

                // 배지 및 칭호
                g.DrawString(ach.Title, _fontBold, ach.IsCompleted ? _brTextGold : _brTextWhite, x + 10, y + 8);
                g.DrawString(ach.Description, _fontSmall, _brTextMuted, x + 10, y + 28);

                // 진행도
                string progStr = ach.IsClaimed
                    ? "[수령완료]"
                    : (ach.IsCompleted ? "[달성 완료!]" : $"진행: {ach.CurrentProgress}/{ach.TargetValue}");
                g.DrawString(progStr, _fontSmall, ach.IsCompleted ? _brTextGreen : _brTextMuted, x + 10, y + 48);

                // 보상 수령 버튼
                Rectangle rcBtn = new Rectangle(x + cardW - 84, y + 42, 76, 24);
                _rcBtnAchClaim[i] = rcBtn;

                if (ach.IsClaimed)
                {
                    g.FillRectangle(_brHeaderBg, rcBtn);
                    g.DrawRectangle(_penBorder, rcBtn);
                    g.DrawString("수령완료", _fontSmall, _brTextMuted, rcBtn, _sfCenter);
                }
                else if (ach.IsCompleted)
                {
                    bool isHover = rcBtn.Contains(_hoverPoint);
                    g.FillRectangle(isHover ? _brMeadow : _brSoil, rcBtn);
                    g.DrawRectangle(_penGold, rcBtn);
                    g.DrawString("[보상수령]", _fontBold, _brTextGold, rcBtn, _sfCenter);
                }
                else
                {
                    g.FillRectangle(_brHeaderBg, rcBtn);
                    g.DrawRectangle(_penBorder, rcBtn);
                    g.DrawString($"+{ach.RewardGold}G", _fontSmall, _brTextMuted, rcBtn, _sfCenter);
                }
            }
        }

        private void DrawContractsModal(Graphics g, int mx, int my)
        {
            if (_model.ContractSystem == null) return;
            var list = _model.ContractSystem.DailyContracts;
            int count = list.Count;
            if (_rcBtnContractFulfill == null || _rcBtnContractFulfill.Length != count)
            {
                _rcBtnContractFulfill = new Rectangle[count];
            }

            int completedCount = list.FindAll(c => c.IsCompleted).Count;

            g.DrawString($"[✦ 일일 의뢰] 로컬 푸드 직거래 납품소 ({completedCount}/3 완수)", _fontTitle, _brTextGold, mx + 22, my + 14);
            g.DrawString("매일 긴급 조달되는 농·수·축산물 납품 계약을 완수하고 올클리어 보너스를 획득하세요.", _fontSmall, _brTextMuted, mx + 22, my + 38);

            int startY = my + 64;
            int cardW = 576;
            int cardH = 88;
            int gapY = 10;

            for (int i = 0; i < count; i++)
            {
                var c = list[i];
                int y = startY + i * (cardH + gapY);
                Rectangle rc = new Rectangle(mx + 22, y, cardW, cardH);
                g.FillRectangle(_brCardBg, rc);
                g.DrawRectangle(c.IsCompleted ? _penActive : _penBorder, rc);

                // 아이템 정보
                string itemName = _model.Items.ContainsKey(c.RequiredItemId) ? _model.Items[c.RequiredItemId].Name : c.RequiredItemId;
                int currentStock = _model.Inventory.ContainsKey(c.RequiredItemId) ? _model.Inventory[c.RequiredItemId] : 0;

                g.DrawString(c.Title, _fontBold, c.IsCompleted ? _brTextGreen : _brTextWhite, rc.X + 16, y + 10);
                g.DrawString(c.Description, _fontSmall, _brTextMuted, rc.X + 16, y + 32);

                string reqStr = $"납품 대상: {itemName} {c.RequiredAmount}개 (현재 보유량: {currentStock}개)";
                SolidBrush brStock = currentStock >= c.RequiredAmount ? _brTextGreen : _brTextGold;
                g.DrawString(reqStr, _fontSmall, brStock, rc.X + 16, y + 54);

                string rewardStr = $"보상: +{c.RewardGold}G | 명성 +{c.RewardFame}P";
                g.DrawString(rewardStr, _fontBold, _brTextGold, rc.X + 270, y + 54);

                Rectangle rcBtn = new Rectangle(rc.Right - 105, y + 25, 92, 36);
                _rcBtnContractFulfill[i] = rcBtn;

                if (c.IsCompleted)
                {
                    g.FillRectangle(_brHeaderBg, rcBtn);
                    g.DrawRectangle(_penActive, rcBtn);
                    g.DrawString("[납품완료]", _fontBold, _brTextGreen, rcBtn, _sfCenter);
                }
                else
                {
                    bool canDeliver = currentStock >= c.RequiredAmount;
                    bool isHover = rcBtn.Contains(_hoverPoint);
                    g.FillRectangle(canDeliver ? (isHover ? _brMeadow : _brSoil) : _brHeaderBg, rcBtn);
                    g.DrawRectangle(canDeliver ? _penGold : _penBorder, rcBtn);
                    g.DrawString(canDeliver ? "[납품하기]" : "[재료부족]", _fontBold, canDeliver ? _brTextGold : _brTextMuted, rcBtn, _sfCenter);
                }
            }

            // 올클리어 일일 보너스 HUD
            Rectangle rcBonus = new Rectangle(mx + 22, my + 368, cardW, 80);
            g.FillRectangle(_brHeaderBg, rcBonus);
            g.DrawRectangle(_penGold, rcBonus);

            g.DrawString("★ 일일 의뢰 ALL CLEAR 특별 지원금 (+500G, +50 식당 명성P)", _fontBold, _brTextGold, rcBonus.X + 16, rcBonus.Y + 14);
            string bonusDesc = _model.ContractSystem.DailyBonusClaimed
                ? "[보너스 수령 완료] 오늘 의뢰를 모두 달성하셨습니다! 매일 자정에 새로운 의뢰가 갱신됩니다."
                : $"3개 분야 의뢰를 모두 납품 완료하면 특별 지원금이 즉시 자동 지급됩니다. (현재 {completedCount}/3 완료)";
            g.DrawString(bonusDesc, _fontSmall, _model.ContractSystem.DailyBonusClaimed ? _brTextGreen : _brTextMuted, rcBonus.X + 16, rcBonus.Y + 40);
        }

        private void HandleModalClick(Point pt)
        {
            // 닫기 버튼 또는 박스 바깥 클릭 시 닫기
            if (_rcModalClose.Contains(pt) || !_rcModalBox.Contains(pt))
            {
                _activeModal = ModalViewType.None;
                return;
            }

            if (_activeModal == ModalViewType.Upgrades)
            {
                if (_rcBtnUpgFarmTool.Contains(pt)) { _model.UpgradeFarmTool(); }
                else if (_rcBtnUpgFishingTool.Contains(pt)) { _model.UpgradeFishingTool(); }
                else if (_rcBtnUpgLivestockTool.Contains(pt)) { _model.UpgradeLivestockTool(); }
                else if (_rcBtnUpgWorker.Contains(pt)) { _model.UpgradeWorkerCount(); }
                else if (_rcBtnUpgCart.Contains(pt)) { _model.UpgradeCartCapacity(); }
                else if (_rcBtnUpgSilo.Contains(pt)) { _model.UpgradeSilo(); }
            }
            else if (_activeModal == ModalViewType.TechTree && _rcBtnTechNodes != null)
            {
                for (int i = 0; i < _rcBtnTechNodes.Length && i < _model.TechTree.Count; i++)
                {
                    if (_rcBtnTechNodes[i].Contains(pt))
                    {
                        _model.ResearchTech(_model.TechTree[i].Id);
                        break;
                    }
                }
            }
            else if (_activeModal == ModalViewType.Breeding && _rcBtnBreedings != null)
            {
                for (int i = 0; i < _rcBtnBreedings.Length; i++)
                {
                    if (_rcBtnBreedings[i].Contains(pt))
                    {
                        _model.PerformBreeding(i);
                        break;
                    }
                }
            }
            else if (_activeModal == ModalViewType.Recipes && _rcBtnRecipeList != null)
            {
                for (int i = 0; i < _rcBtnRecipeList.Length; i++)
                {
                    if (_rcBtnRecipeList[i].Contains(pt))
                    {
                        var r = _model.Recipes[i];
                        if (!r.IsUnlocked)
                        {
                            if (_model.Gold >= r.UnlockCost)
                            {
                                _model.Gold -= r.UnlockCost;
                                r.IsUnlocked = true;
                                if (_model.AchievementSystem != null) _model.AchievementSystem.CheckAllMilestones(_model);
                                _model.AddFloatingText($"[≡] [{r.Name}] 해금 완료!", 325, 280, Color.FromArgb(255, 215, 0));
                                _model.Save();
                            }
                            else
                            {
                                _model.AddFloatingText("해금 골드가 부족합니다!", 325, 280, Color.FromArgb(255, 120, 100));
                            }
                        }
                        else
                        {
                            _model.AddFloatingText($"[{r.Name}]은 식당 영업 시 손님 주문에 맞춰 자동 조리됩니다.", 325, 280, Color.FromArgb(100, 220, 255));
                        }
                        break;
                    }
                }
            }
            else if (_activeModal == ModalViewType.Achievements && _rcBtnAchClaim != null && _model.AchievementSystem != null)
            {
                for (int i = 0; i < _rcBtnAchClaim.Length && i < _model.AchievementSystem.Achievements.Count; i++)
                {
                    if (_rcBtnAchClaim[i].Contains(pt))
                    {
                        var ach = _model.AchievementSystem.Achievements[i];
                        if (ach.IsCompleted && !ach.IsClaimed)
                        {
                            _model.ClaimAchievementReward(ach.Id);
                        }
                        break;
                    }
                }
            }
            else if (_activeModal == ModalViewType.Contracts && _rcBtnContractFulfill != null && _model.ContractSystem != null)
            {
                for (int i = 0; i < _rcBtnContractFulfill.Length && i < _model.ContractSystem.DailyContracts.Count; i++)
                {
                    if (_rcBtnContractFulfill[i].Contains(pt))
                    {
                        var c = _model.ContractSystem.DailyContracts[i];
                        if (!c.IsCompleted)
                        {
                            _model.FulfillContract(c.Id);
                        }
                        break;
                    }
                }
            }
        }
    }
}
