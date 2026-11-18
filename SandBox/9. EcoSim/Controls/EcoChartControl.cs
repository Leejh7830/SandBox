using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using GateHelper.EcoSim.Models;
using GateHelper.EcoSim.Rendering;

namespace GateHelper.EcoSim.Controls
{
    /// <summary>
    /// 초식/육식 개체수 파동 및 평균 속도 진화 추이를 표시하는 실시간 GDI+ 그래프.
    /// </summary>
    public sealed class EcoChartControl : Control
    {
        private IReadOnlyList<EcoStatsSnapshot> _history;
        private readonly PointF[] _preyPointsBuffer = new PointF[256];
        private readonly PointF[] _predPointsBuffer = new PointF[256];
        private readonly PointF[] _bearPointsBuffer = new PointF[256];
        private readonly PointF[] _humanPointsBuffer = new PointF[256];

        public EcoChartControl()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
            BackColor = Color.FromArgb(24, 30, 41);
        }

        public void SetHistory(IReadOnlyList<EcoStatsSnapshot> history)
        {
            _history = history;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            g.FillRectangle(EcoRenderCache.PanelBgBrush, 0, 0, Width, Height);
            g.DrawRectangle(EcoRenderCache.BorderPen, 0, 0, Width - 1, Height - 1);

            if (Width < 40 || Height < 60) return;

            int padLeft = 10;
            int padRight = 10;
            int padTop = 26;
            int padBottom = 14;
            int plotW = Width - padLeft - padRight;
            int plotH = Height - padTop - padBottom;

            g.DrawString("개체수 파동 (초식·육식·곰·인간)", EcoRenderCache.BoldSmallFont, EcoRenderCache.TextPrimaryBrush, 10, 6);

            for (int i = 1; i <= 3; i++)
            {
                float gy = padTop + (plotH * i / 4f);
                g.DrawLine(EcoRenderCache.ChartGridPen, padLeft, gy, padLeft + plotW, gy);
            }

            if (_history == null || _history.Count < 2) return;

            int count = Math.Min(_history.Count, _preyPointsBuffer.Length);
            int maxPop = 20;
            for (int i = 0; i < count; i++)
            {
                var snap = _history[i];
                if (snap.PreyCount > maxPop) maxPop = snap.PreyCount;
                if (snap.PredatorCount > maxPop) maxPop = snap.PredatorCount;
                if (snap.BearCount > maxPop) maxPop = snap.BearCount;
                if (snap.HumanCount > maxPop) maxPop = snap.HumanCount;
            }
            maxPop = (int)(maxPop * 1.15f);

            for (int i = 0; i < count; i++)
            {
                var snap = _history[i];
                float x = padLeft + (plotW * i / (float)(count - 1));
                _preyPointsBuffer[i] = new PointF(x, padTop + plotH - (snap.PreyCount / (float)maxPop) * plotH);
                _predPointsBuffer[i] = new PointF(x, padTop + plotH - (snap.PredatorCount / (float)maxPop) * plotH);
                _bearPointsBuffer[i] = new PointF(x, padTop + plotH - (snap.BearCount / (float)maxPop) * plotH);
                _humanPointsBuffer[i] = new PointF(x, padTop + plotH - (snap.HumanCount / (float)maxPop) * plotH);
            }

            DrawSeriesSegment(g, EcoRenderCache.ChartPreyPen, _preyPointsBuffer, count);
            DrawSeriesSegment(g, EcoRenderCache.ChartPredPen, _predPointsBuffer, count);
            DrawSeriesSegment(g, EcoRenderCache.ChartBearPen, _bearPointsBuffer, count);
            DrawSeriesSegment(g, EcoRenderCache.ChartHumanPen, _humanPointsBuffer, count);
        }

        private static void DrawSeriesSegment(Graphics g, Pen pen, PointF[] pts, int count)
        {
            for (int i = 1; i < count; i++)
            {
                g.DrawLine(pen, pts[i - 1], pts[i]);
            }
        }
    }
}
