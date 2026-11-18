using System.Drawing;
using System.Drawing.Drawing2D;

namespace GateHelper.EcoSim.Rendering
{
    /// <summary>
    /// OnPaint 내부에서 프레임당 GC 할당을 0으로 유지하기 위한 정적 GDI+ 리소스 캐시.
    /// </summary>
    internal static class EcoRenderCache
    {
        public static readonly SolidBrush BgBrush = new SolidBrush(Color.FromArgb(18, 22, 30));
        public static readonly SolidBrush PanelBgBrush = new SolidBrush(Color.FromArgb(24, 30, 41));
        public static readonly SolidBrush PondFillBrush = new SolidBrush(Color.FromArgb(75, 14, 116, 144));
        public static readonly SolidBrush RockFillBrush = new SolidBrush(Color.FromArgb(71, 85, 105));
        public static readonly SolidBrush CampfireGlowBrush = new SolidBrush(Color.FromArgb(28, 251, 146, 60));
        public static readonly SolidBrush CampfireCoreBrush = new SolidBrush(Color.FromArgb(249, 115, 22));
        public static readonly SolidBrush LegendStarBrush = new SolidBrush(Color.FromArgb(250, 204, 21));
        public static readonly SolidBrush PlayerTagBrush = new SolidBrush(Color.FromArgb(34, 211, 238));
        public static readonly SolidBrush AlphaBossTagBrush = new SolidBrush(Color.FromArgb(216, 180, 254));
        public static readonly SolidBrush HpBgBrush = new SolidBrush(Color.FromArgb(200, 15, 23, 42));
        public static readonly SolidBrush PlayerHpBrush = new SolidBrush(Color.FromArgb(34, 197, 94));
        public static readonly SolidBrush BossHpBrush = new SolidBrush(Color.FromArgb(217, 70, 239));
        public static readonly SolidBrush ToastBgBrush = new SolidBrush(Color.FromArgb(220, 30, 41, 59));
        public static readonly SolidBrush InvBagBgBrush = new SolidBrush(Color.FromArgb(215, 15, 23, 42));
        public static readonly SolidBrush InvSlotBgBrush = new SolidBrush(Color.FromArgb(235, 30, 41, 59));
        public static readonly SolidBrush DisasterLightningBrush = new SolidBrush(Color.FromArgb(38, 250, 204, 21));
        public static readonly SolidBrush DisasterColdBrush = new SolidBrush(Color.FromArgb(35, 125, 211, 252));
        public static readonly SolidBrush DisasterRainBrush = new SolidBrush(Color.FromArgb(35, 74, 222, 128));
        public static readonly SolidBrush WinterSnowBrush = new SolidBrush(Color.FromArgb(170, 224, 242, 254));
        public static readonly SolidBrush WinterTintBrush = new SolidBrush(Color.FromArgb(14, 186, 230, 253));
        public static readonly SolidBrush ShelterFillBrush = new SolidBrush(Color.FromArgb(26, 34, 197, 94));
        public static readonly SolidBrush CompostBrush = new SolidBrush(Color.FromArgb(180, 180, 120, 60));
        public static readonly SolidBrush FoodBrush = new SolidBrush(Color.FromArgb(74, 222, 128));
        public static readonly SolidBrush FoodGlowBrush = new SolidBrush(Color.FromArgb(45, 74, 222, 128));

        public static readonly SolidBrush PreyBodyBrush = new SolidBrush(Color.FromArgb(56, 189, 248));
        public static readonly SolidBrush PreyFastBrush = new SolidBrush(Color.FromArgb(125, 211, 252));
        public static readonly SolidBrush PredatorBodyBrush = new SolidBrush(Color.FromArgb(248, 113, 113));
        public static readonly SolidBrush PredatorFastBrush = new SolidBrush(Color.FromArgb(251, 146, 60));

        public static readonly SolidBrush SelectionVisionBrush = new SolidBrush(Color.FromArgb(24, 250, 204, 21));
        public static readonly SolidBrush TextPrimaryBrush = new SolidBrush(Color.FromArgb(226, 232, 240));
        public static readonly SolidBrush TextMutedBrush = new SolidBrush(Color.FromArgb(148, 163, 184));

        public static readonly Pen GridPen = new Pen(Color.FromArgb(28, 36, 50), 1f);
        public static readonly Pen PondBorderPen = new Pen(Color.FromArgb(130, 56, 189, 248), 1.8f);
        public static readonly Pen RockBorderPen = new Pen(Color.FromArgb(100, 116, 139), 1.8f);
        public static readonly Pen CampfireRingPen = new Pen(Color.FromArgb(80, 251, 146, 60), 1.2f);
        public static readonly Pen ShelterBorderPen = new Pen(Color.FromArgb(65, 34, 197, 94), 1.4f);
        public static readonly Pen BorderPen = new Pen(Color.FromArgb(45, 55, 72), 1f);
        public static readonly Pen DirectionPen = new Pen(Color.FromArgb(220, 255, 255, 255), 1.4f);
        public static readonly Pen SelectionPen = new Pen(Color.FromArgb(250, 204, 21), 2f);
        public static readonly Pen PlayerRingPen = new Pen(Color.FromArgb(34, 211, 238), 2.4f);
        public static readonly Pen PlayerRangePen = new Pen(Color.FromArgb(70, 56, 189, 248), 1.3f) { DashStyle = DashStyle.Dash };
        public static readonly Pen HpBorderPen = new Pen(Color.FromArgb(140, 100, 116, 139), 1f);
        public static readonly Pen ToastBorderPen = new Pen(Color.FromArgb(250, 204, 21), 1.5f);
        public static readonly Pen InvSlotBorderPen = new Pen(Color.FromArgb(180, 71, 85, 105), 1.2f);
        public static readonly Pen InvSlotReadyPen = new Pen(Color.FromArgb(220, 251, 146, 60), 1.6f);
        public static readonly Pen DisasterLightningPen = new Pen(Color.FromArgb(190, 250, 204, 21), 2.2f);
        public static readonly Pen DisasterColdPen = new Pen(Color.FromArgb(170, 125, 211, 252), 2.0f);
        public static readonly Pen DisasterRainPen = new Pen(Color.FromArgb(170, 74, 222, 128), 2.0f);
        public static readonly Pen AlphaBossRingPen = new Pen(Color.FromArgb(220, 168, 85, 247), 2.8f);
        public static readonly Pen BearFrenzyPen = new Pen(Color.FromArgb(220, 239, 68, 68), 2.2f);
        public static readonly Pen SelectionVisionPen = new Pen(Color.FromArgb(120, 250, 204, 21), 1.2f);

        public static readonly Pen ChartPreyPen = new Pen(Color.FromArgb(56, 189, 248), 2f);
        public static readonly Pen ChartPredPen = new Pen(Color.FromArgb(248, 113, 113), 2f);
        public static readonly Pen ChartBearPen = new Pen(Color.FromArgb(180, 120, 68), 2f);
        public static readonly Pen ChartHumanPen = new Pen(Color.FromArgb(250, 204, 21), 2f);
        public static readonly Pen HunterShotPen = new Pen(Color.FromArgb(230, 250, 204, 21), 2f);
        public static readonly Pen ChartSpeedPreyPen = new Pen(Color.FromArgb(167, 139, 250), 1.8f);
        public static readonly Pen ChartSpeedPredPen = new Pen(Color.FromArgb(251, 191, 36), 1.8f);
        public static readonly Pen ChartGridPen = new Pen(Color.FromArgb(36, 44, 58), 1f);

        public static readonly Font SmallFont = new Font("Malgun Gothic", 8.5f, FontStyle.Regular);
        public static readonly Font BoldSmallFont = new Font("Malgun Gothic", 8.5f, FontStyle.Bold);

        // 프로그램 시작 시 1회만 생성되는 초경량 벡터 스프라이트 캐시 (프레임당 GC 0 Byte)
        public static readonly Bitmap FoodSprite = CreateFoodSprite();
        public static readonly Bitmap PreySprite = CreatePreySprite(Color.FromArgb(56, 189, 248), Color.FromArgb(14, 116, 144));
        public static readonly Bitmap PreyFastSprite = CreatePreySprite(Color.FromArgb(125, 211, 252), Color.FromArgb(56, 189, 248));
        public static readonly Bitmap PredatorSprite = CreatePredatorSprite(Color.FromArgb(248, 113, 113), Color.FromArgb(185, 28, 28));
        public static readonly Bitmap PredatorFastSprite = CreatePredatorSprite(Color.FromArgb(251, 146, 60), Color.FromArgb(194, 65, 12));
        public static readonly Bitmap BearSprite = CreateBearSprite();
        public static readonly Bitmap HumanSprite = CreateHumanSprite();
        public static readonly Bitmap CampfireSprite = CreateCampfireSprite();
        public static readonly Bitmap MeatSprite = CreateMeatSprite();
        public static readonly Bitmap WoodSprite = CreateWoodSprite();

        private static Bitmap CreateFoodSprite()
        {
            var bmp = new Bitmap(24, 24);
            using (var g = Graphics.FromImage(bmp))
            using (var leafBrush = new SolidBrush(Color.FromArgb(74, 222, 128)))
            using (var stemPen = new Pen(Color.FromArgb(34, 197, 94), 2f))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.DrawLine(stemPen, 12, 20, 12, 10);
                g.FillEllipse(leafBrush, 4, 6, 9, 6);
                g.FillEllipse(leafBrush, 11, 6, 9, 6);
                g.FillEllipse(leafBrush, 8, 3, 8, 7);
            }
            return bmp;
        }

        private static Bitmap CreatePreySprite(Color fillColor, Color outlineColor)
        {
            var bmp = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bmp))
            using (var bodyBrush = new SolidBrush(fillColor))
            using (var earBrush = new SolidBrush(Color.FromArgb(224, 242, 254)))
            using (var eyeBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            using (var outlinePen = new Pen(outlineColor, 1.6f))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                // 오른쪽(+X)을 정면으로 바라보는 초식동물(토끼형)
                g.FillEllipse(bodyBrush, 4, 12, 5, 8); // 꼬리
                g.FillEllipse(bodyBrush, 6, 7, 19, 18); // 몸통
                g.DrawEllipse(outlinePen, 6, 7, 19, 18);
                // 귀 2개 (뒤로 젖힌 형태)
                g.FillEllipse(bodyBrush, 11, 4, 8, 5);
                g.FillEllipse(earBrush, 13, 5, 5, 3);
                g.FillEllipse(bodyBrush, 11, 23, 8, 5);
                g.FillEllipse(earBrush, 13, 24, 5, 3);
                // 눈 2개 (정면 오른쪽)
                g.FillEllipse(eyeBrush, 20, 11, 3, 3);
                g.FillEllipse(eyeBrush, 20, 18, 3, 3);
            }
            return bmp;
        }

        private static Bitmap CreatePredatorSprite(Color fillColor, Color outlineColor)
        {
            var bmp = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bmp))
            using (var bodyBrush = new SolidBrush(fillColor))
            using (var eyeBrush = new SolidBrush(Color.FromArgb(254, 240, 138)))
            using (var outlinePen = new Pen(outlineColor, 1.6f))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                // 오른쪽(+X)을 정면으로 바라보는 날렵한 포식자(여우/늑대형)
                var bodyPts = new PointF[]
                {
                    new PointF(4f, 16f),
                    new PointF(11f, 7f),
                    new PointF(20f, 8f),
                    new PointF(29f, 16f),
                    new PointF(20f, 24f),
                    new PointF(11f, 25f)
                };
                g.FillPolygon(bodyBrush, bodyPts);
                g.DrawPolygon(outlinePen, bodyPts);
                // 뾰족한 귀 2개
                g.FillPolygon(bodyBrush, new[] { new PointF(12f, 8f), new PointF(9f, 3f), new PointF(16f, 7f) });
                g.FillPolygon(bodyBrush, new[] { new PointF(12f, 24f), new PointF(9f, 29f), new PointF(16f, 25f) });
                // 날카로운 눈
                g.FillEllipse(eyeBrush, 20, 11, 3.2f, 2.8f);
                g.FillEllipse(eyeBrush, 20, 18, 3.2f, 2.8f);
            }
            return bmp;
        }

        private static Bitmap CreateBearSprite()
        {
            var bmp = new Bitmap(36, 36);
            using (var g = Graphics.FromImage(bmp))
            using (var furBrush = new SolidBrush(Color.FromArgb(146, 92, 48)))
            using (var snoutBrush = new SolidBrush(Color.FromArgb(214, 174, 130)))
            using (var eyeBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            using (var outlinePen = new Pen(Color.FromArgb(92, 54, 24), 1.8f))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                // 둥근 곰 귀 2개
                g.FillEllipse(furBrush, 12, 3, 9, 8);
                g.FillEllipse(furBrush, 12, 25, 9, 8);
                // 묵직한 몸통
                g.FillEllipse(furBrush, 4, 6, 26, 24);
                g.DrawEllipse(outlinePen, 4, 6, 26, 24);
                // 주둥이 (오른쪽 +X 정면)
                g.FillEllipse(snoutBrush, 22, 13, 10, 10);
                g.FillEllipse(eyeBrush, 29, 16.5f, 3, 3);
                // 눈
                g.FillEllipse(eyeBrush, 21, 12, 3, 3);
                g.FillEllipse(eyeBrush, 21, 21, 3, 3);
            }
            return bmp;
        }

        private static Bitmap CreateHumanSprite()
        {
            var bmp = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bmp))
            using (var cloakBrush = new SolidBrush(Color.FromArgb(234, 179, 8)))
            using (var headBrush = new SolidBrush(Color.FromArgb(254, 243, 199)))
            using (var bowPen = new Pen(Color.FromArgb(56, 189, 248), 2.2f))
            using (var outlinePen = new Pen(Color.FromArgb(133, 77, 14), 1.6f))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                // 망토/몸통
                g.FillEllipse(cloakBrush, 6, 7, 17, 18);
                g.DrawEllipse(outlinePen, 6, 7, 17, 18);
                // 머리
                g.FillEllipse(headBrush, 11, 10, 12, 12);
                // 전방(+X) 활 아크
                g.DrawArc(bowPen, 16, 6, 12, 20, -70, 140);
            }
            return bmp;
        }

        private static Bitmap CreateCampfireSprite()
        {
            var bmp = new Bitmap(36, 36);
            using (var g = Graphics.FromImage(bmp))
            using (var logBrush = new SolidBrush(Color.FromArgb(120, 68, 32)))
            using (var logPen = new Pen(Color.FromArgb(78, 42, 18), 1.4f))
            using (var outerFlame = new SolidBrush(Color.FromArgb(239, 68, 68)))
            using (var midFlame = new SolidBrush(Color.FromArgb(249, 115, 22)))
            using (var innerFlame = new SolidBrush(Color.FromArgb(254, 240, 138)))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                // 교차된 장작 2개
                g.TranslateTransform(18, 25);
                g.RotateTransform(26);
                g.FillRectangle(logBrush, -13, -3, 26, 6);
                g.DrawRectangle(logPen, -13, -3, 26, 6);
                g.RotateTransform(-52);
                g.FillRectangle(logBrush, -13, -3, 26, 6);
                g.DrawRectangle(logPen, -13, -3, 26, 6);
                g.ResetTransform();

                // 외곽 붉은 불꽃
                var outerPts = new[]
                {
                    new PointF(18f, 3f),
                    new PointF(27f, 15f),
                    new PointF(25f, 25f),
                    new PointF(11f, 25f),
                    new PointF(9f, 15f)
                };
                g.FillPolygon(outerFlame, outerPts);

                // 중간 주황 불꽃
                var midPts = new[]
                {
                    new PointF(18f, 8f),
                    new PointF(24f, 17f),
                    new PointF(22f, 24f),
                    new PointF(14f, 24f),
                    new PointF(12f, 17f)
                };
                g.FillPolygon(midFlame, midPts);

                // 중심 노란 불꽃 코어
                g.FillEllipse(innerFlame, 14.5f, 15f, 7f, 9f);
            }
            return bmp;
        }

        private static Bitmap CreateMeatSprite()
        {
            var bmp = new Bitmap(24, 24);
            using (var g = Graphics.FromImage(bmp))
            using (var boneBrush = new SolidBrush(Color.FromArgb(241, 245, 249)))
            using (var meatBrush = new SolidBrush(Color.FromArgb(220, 38, 38)))
            using (var fatBrush = new SolidBrush(Color.FromArgb(254, 202, 202)))
            using (var outlinePen = new Pen(Color.FromArgb(127, 29, 29), 1.4f))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                // 뼈 손잡이
                g.TranslateTransform(12, 12);
                g.RotateTransform(-35);
                g.FillRectangle(boneBrush, 2, -2.5f, 8, 5);
                g.FillEllipse(boneBrush, 8, -4f, 4.5f, 4.5f);
                g.FillEllipse(boneBrush, 8, -0.5f, 4.5f, 4.5f);
                // 고기 본체
                g.FillEllipse(meatBrush, -10, -6.5f, 14, 13);
                g.DrawEllipse(outlinePen, -10, -6.5f, 14, 13);
                g.FillEllipse(fatBrush, -6.5f, -3.5f, 4.5f, 3.5f);
                g.ResetTransform();
            }
            return bmp;
        }

        private static Bitmap CreateWoodSprite()
        {
            var bmp = new Bitmap(24, 24);
            using (var g = Graphics.FromImage(bmp))
            using (var barkBrush = new SolidBrush(Color.FromArgb(146, 64, 14)))
            using (var barkPen = new Pen(Color.FromArgb(92, 40, 12), 1.2f))
            using (var cutBrush = new SolidBrush(Color.FromArgb(217, 160, 91)))
            using (var leafBrush = new SolidBrush(Color.FromArgb(74, 222, 128)))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                // 교차된 나뭇가지 2개
                g.TranslateTransform(12, 12);
                g.RotateTransform(-30);
                g.FillRectangle(barkBrush, -9, -2.2f, 18, 4.4f);
                g.DrawRectangle(barkPen, -9, -2.2f, 18, 4.4f);
                g.FillEllipse(cutBrush, 7, -2.2f, 3.5f, 4.4f);
                g.RotateTransform(58);
                g.FillRectangle(barkBrush, -8, -2f, 16, 4f);
                g.DrawRectangle(barkPen, -8, -2f, 16, 4f);
                g.FillEllipse(leafBrush, 4, -5.5f, 5f, 3.5f);
                g.ResetTransform();
            }
            return bmp;
        }
    }
}
