using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using GateHelper.EcoSim.Rendering;
using GateHelper.EcoSim.Services;

namespace GateHelper.EcoSim.Controls
{
    /// <summary>
    /// 에이지 오브 엠파이어 스타일의 연도 타임라인 및 세로열 정렬 기술 트리 탭 컨트롤.
    /// </summary>
    public sealed class EcoTechTreeControl : Control
    {
        private EcoSimulationEngine _engine;

        private static readonly SolidBrush CardUnlockedBg = new SolidBrush(Color.FromArgb(28, 38, 54));
        private static readonly SolidBrush CardLockedBg = new SolidBrush(Color.FromArgb(22, 27, 37));
        private static readonly SolidBrush EraHeaderBg = new SolidBrush(Color.FromArgb(30, 41, 59));
        private static readonly SolidBrush TrackBgBrush = new SolidBrush(Color.FromArgb(30, 41, 59));
        private static readonly SolidBrush TrackFillBrush = new SolidBrush(Color.FromArgb(217, 119, 6));
        private static readonly SolidBrush BadgeUnlockedBrush = new SolidBrush(Color.FromArgb(34, 197, 94));
        private static readonly SolidBrush BadgeLockedBrush = new SolidBrush(Color.FromArgb(148, 163, 184));

        private static readonly Pen CardUnlockedPen = new Pen(Color.FromArgb(234, 179, 8), 2f);
        private static readonly Pen CardLockedPen = new Pen(Color.FromArgb(51, 65, 85), 1.4f);
        private static readonly Pen ConnectorUnlockedPen = new Pen(Color.FromArgb(234, 179, 8), 2.0f);
        private static readonly Pen ConnectorLockedPen = new Pen(Color.FromArgb(71, 85, 105), 1.6f) { DashStyle = DashStyle.Dash };
        private static readonly Pen NeedlePen = new Pen(Color.FromArgb(250, 204, 21), 2.4f);

        private static readonly Font TitleFont = new Font("Malgun Gothic", 12f, FontStyle.Bold);
        private static readonly Font EraFont = new Font("Malgun Gothic", 10f, FontStyle.Bold);
        private static readonly Font NodeTitleFont = new Font("Malgun Gothic", 9f, FontStyle.Bold);
        private static readonly Font DescFont = new Font("Malgun Gothic", 8f, FontStyle.Regular);

        public EcoTechTreeControl()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
            BackColor = Color.FromArgb(18, 22, 30);
        }

        public void BindEngine(EcoSimulationEngine engine)
        {
            _engine = engine;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);

            if (_engine == null || Width < 360 || Height < 280) return;

            float yearsElapsed = _engine.TickCount / 880f;
            bool campfireUnlocked = _engine.IsCampfireAgeUnlocked;
            bool stoneUnlocked = _engine.IsStoneAgeUnlocked;
            bool houseUnlocked = _engine.IsHouseAgeUnlocked;

            // 1. 상단 헤더 & 현재 연도/시대 배지
            g.DrawString("문명 연대기 & 기술 트리 (Age Progression)", TitleFont, EcoRenderCache.TextPrimaryBrush, 24f, 18f);
            string eraStatus = $"현재 경과: {yearsElapsed:F1}년 ({_engine.TickCount:N0}틱)  |  시대: {_engine.CurrentEraLabel}";
            g.DrawString(eraStatus, EraFont, houseUnlocked ? EcoRenderCache.LegendStarBrush : EcoRenderCache.PlayerTagBrush, 24f, 44f);

            // 2. 연도 타임라인 프로그레스 바 (0년 ~ 5년, 각 눈금 아래에 기술 카드 세로열 정렬)
            const float maxTimelineYears = 5f;
            float sideMargin = Math.Max(78f, Math.Min(108f, Width * 0.10f));
            float barX = sideMargin;
            float barY = 86f;
            float barW = Math.Max(220f, Width - sideMargin * 2f);
            const float barH = 18f;
            float colStep = barW / maxTimelineYears;
            float cardW = Math.Min(156f, Math.Max(104f, colStep - 14f));
            const float cardH = 148f;
            float cardY = 166f;

            // 2-1. 세로 연결선을 맨 뒤에 먼저 그림 (시대 라벨·카드가 가려지지 않도록)
            for (int yr = 0; yr <= 5; yr++)
            {
                float mx = barX + colStep * yr;
                bool reached = yearsElapsed >= yr;
                Pen dropPen = reached ? ConnectorUnlockedPen : ConnectorLockedPen;
                g.DrawLine(dropPen, mx, barY + barH + 22f, mx, cardY);
            }

            // 2-2. 시대 배경 밴드 (세로선 위에 덮어 깔끔한 Z-Order 유지)
            DrawEraBand(g, barX - cardW * 0.5f, 134f, colStep + cardW, 24f, "시대 I · 원시기 (모닥불)", true);
            DrawEraBand(g, barX + colStep * 2f - cardW * 0.5f, 134f, colStep + cardW, 24f, "시대 II · 정착기 (석기·집)", houseUnlocked);
            DrawEraBand(g, barX + colStep * 4f - cardW * 0.5f, 134f, colStep + cardW, 24f, "시대 III · 확장기 (예정)", false);

            g.FillRectangle(TrackBgBrush, barX, barY, barW, barH);
            float fillRatio = Math.Min(1f, yearsElapsed / maxTimelineYears);
            if (fillRatio > 0f)
            {
                g.FillRectangle(TrackFillBrush, barX, barY, barW * fillRatio, barH);
            }
            g.DrawRectangle(CardLockedPen, barX, barY, barW, barH);

            // 0년~5년 눈금 라벨
            for (int yr = 0; yr <= 5; yr++)
            {
                float mx = barX + colStep * yr;
                bool reached = yearsElapsed >= yr;
                g.DrawLine(yr == 3 ? NeedlePen : CardLockedPen, mx, barY - 4f, mx, barY + barH + 4f);
                string yrLabel = yr == 3 ? "3년 ★집" : $"{yr}년";
                Brush yrBrush = (yr == 3 || reached) ? EcoRenderCache.LegendStarBrush : EcoRenderCache.TextMutedBrush;
                g.DrawString(yrLabel, DescFont, yrBrush, mx - (yr == 3 ? 20f : 10f), barY + barH + 6f);
            }

            // 현재 진행 바늘
            float needleX = barX + barW * fillRatio;
            g.DrawLine(NeedlePen, needleX, barY - 7f, needleX, barY + barH + 7f);

            // 가로 연결 화살표 (0→1→2→3→4→5)
            float midY = cardY + cardH * 0.5f;
            DrawArrow(g, barX + cardW * 0.5f, midY, barX + colStep - cardW * 0.5f, midY, campfireUnlocked);
            DrawArrow(g, barX + colStep + cardW * 0.5f, midY, barX + colStep * 2f - cardW * 0.5f, midY, stoneUnlocked);
            DrawArrow(g, barX + colStep * 2f + cardW * 0.5f, midY, barX + colStep * 3f - cardW * 0.5f, midY, houseUnlocked);
            DrawArrow(g, barX + colStep * 3f + cardW * 0.5f, midY, barX + colStep * 4f - cardW * 0.5f, midY, false);
            DrawArrow(g, barX + colStep * 4f + cardW * 0.5f, midY, barX + colStep * 5f - cardW * 0.5f, midY, false);

            // 각 연도 눈금(mx) 바로 아래에 세로열 정렬된 기술 카드
            DrawTechCard(
                g, barX + colStep * 0 - cardW * 0.5f, cardY, cardW, cardH,
                "채집과 수렵", "사냥 시 고기 +1\n식물 나뭇가지 +1\n(칸당 최대 20개)",
                EcoRenderCache.WoodSprite, unlocked: true, badgeText: "✔ 해금됨");

            DrawTechCard(
                g, barX + colStep * 1 - cardW * 0.5f, cardY, cardW, cardH,
                "모닥불", "나뭇가지 5개\n겨울 월동·그룹색\n플레이어 [F] 건설",
                EcoRenderCache.CampfireSprite, unlocked: campfireUnlocked, badgeText: campfireUnlocked ? "✔ 해금됨" : "🔒 1년차");

            DrawTechCard(
                g, barX + colStep * 2 - cardW * 0.5f, cardY, cardW, cardH,
                "석기 채집", "여유 시 바위에서\n돌 채집 해금\n(최대 20개)",
                EcoRenderCache.StoneSprite, unlocked: stoneUnlocked, badgeText: stoneUnlocked ? "✔ 해금됨" : "🔒 2년차");

            DrawTechCard(
                g, barX + colStep * 3 - cardW * 0.5f, cardY, cardW, cardH,
                "오두막 · 집", "나무 6 + 돌 4\n협동 건축·2인 소유\n3년 수명 · [B]",
                EcoRenderCache.HouseSprite, unlocked: houseUnlocked, badgeText: houseUnlocked ? "✔ 해금됨" : "🔒 3년차");

            DrawTechCard(
                g, barX + colStep * 4 - cardW * 0.5f, cardY, cardW, cardH,
                "목책 · 방어", "곰/알파곰 습격\n거점 방어 시설\n(업데이트 예정)",
                EcoRenderCache.RockSprite, unlocked: false, badgeText: "⏳ 예정");

            DrawTechCard(
                g, barX + colStep * 5 - cardW * 0.5f, cardY, cardW, cardH,
                "텃밭 · 농경", "집 주변 식량\n자급 재배\n(업데이트 예정)",
                EcoRenderCache.FoodSprite, unlocked: false, badgeText: "⏳ 예정");

            g.DrawRectangle(EcoRenderCache.BorderPen, 0, 0, Width - 1, Height - 1);
        }

        private static void DrawEraBand(Graphics g, float x, float y, float w, float h, string title, bool active)
        {
            g.FillRectangle(EraHeaderBg, x, y, w, h);
            g.DrawRectangle(active ? CardUnlockedPen : CardLockedPen, x, y, w, h);
            g.DrawString(title, DescFont, active ? EcoRenderCache.LegendStarBrush : EcoRenderCache.TextMutedBrush, x + 8f, y + 5f);
        }

        private static void DrawArrow(Graphics g, float x1, float y1, float x2, float y2, bool unlocked)
        {
            if (x2 - x1 < 8f) return;
            Pen p = unlocked ? ConnectorUnlockedPen : ConnectorLockedPen;
            g.DrawLine(p, x1 + 2f, y1, x2 - 3f, y2);
            g.DrawLine(p, x2 - 7f, y2 - 4f, x2 - 2f, y2);
            g.DrawLine(p, x2 - 7f, y2 + 4f, x2 - 2f, y2);
        }

        private static void DrawTechCard(
            Graphics g, float x, float y, float w, float h,
            string title, string desc, Bitmap icon, bool unlocked, string badgeText)
        {
            g.FillRectangle(unlocked ? CardUnlockedBg : CardLockedBg, x, y, w, h);
            g.DrawRectangle(unlocked ? CardUnlockedPen : CardLockedPen, x, y, w, h);

            // 상단 중앙 아이콘 박스
            float iconBoxX = x + (w - 36f) * 0.5f;
            g.FillRectangle(EcoRenderCache.InvSlotBgBrush, iconBoxX, y + 10f, 36f, 36f);
            g.DrawRectangle(unlocked ? CardUnlockedPen : CardLockedPen, iconBoxX, y + 10f, 36f, 36f);
            if (icon != null)
            {
                g.DrawImage(icon, iconBoxX + 4f, y + 14f, 28f, 28f);
            }

            // 제목 & 상태 배지 & 설명
            g.DrawString(title, NodeTitleFont, unlocked ? EcoRenderCache.TextPrimaryBrush : EcoRenderCache.TextMutedBrush, x + 10f, y + 54f);
            g.DrawString(badgeText, DescFont, unlocked ? BadgeUnlockedBrush : BadgeLockedBrush, x + 10f, y + 73f);
            g.DrawString(desc, DescFont, unlocked ? EcoRenderCache.TextPrimaryBrush : EcoRenderCache.TextMutedBrush, x + 10f, y + 94f);
        }
    }
}
