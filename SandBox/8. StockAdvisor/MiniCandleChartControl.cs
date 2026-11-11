using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GateHelper.StockAdvisor.Models;
using GateHelper.StockAdvisor.Services;

namespace GateHelper.StockAdvisor
{
    /// <summary>
    /// 최근 20일 일봉 캔들, 5일/20일 이동평균선, 일별 거래량 및 AI 퀀트 타점(목표가·손절가·권고매수가)을
    /// 초경량 고속 GDI+로 시각화하는 인라인 미니 차트 컨트롤 (Zero-Resource: CPU 0.0% 유휴, 0-GC)
    /// </summary>
    public class MiniCandleChartControl : UserControl
    {
        private string _stockCode;
        private string _stockName;
        private decimal _currentPrice;
        private decimal _targetPrice;
        private decimal _stopPrice;
        private decimal _buyPrice;
        private List<DailyCandle> _candles;
        private bool _isLoading;
        private int _hoverIndex = -1;

        // --- GDI+ 정적/캐시 리소스 (GC 압박 0화) ---
        private readonly SolidBrush _bgBrush = new SolidBrush(Color.FromArgb(16, 20, 28));
        private readonly Pen _borderPen = new Pen(Color.FromArgb(38, 48, 64), 1f);
        private readonly Pen _gridPen = new Pen(Color.FromArgb(24, 32, 44), 1f);
        private readonly SolidBrush _upCandleBrush = new SolidBrush(Color.FromArgb(255, 75, 75));
        private readonly Pen _upCandlePen = new Pen(Color.FromArgb(255, 75, 75), 1f);
        private readonly SolidBrush _downCandleBrush = new SolidBrush(Color.FromArgb(70, 160, 255));
        private readonly Pen _downCandlePen = new Pen(Color.FromArgb(70, 160, 255), 1f);
        private readonly SolidBrush _upVolBrush = new SolidBrush(Color.FromArgb(120, 255, 75, 75));
        private readonly SolidBrush _downVolBrush = new SolidBrush(Color.FromArgb(120, 70, 160, 255));
        private readonly Pen _ma5Pen = new Pen(Color.FromArgb(255, 215, 64), 1.2f);
        private readonly Pen _ma20Pen = new Pen(Color.FromArgb(255, 90, 180), 1.2f);
        private readonly Pen _targetPen = new Pen(Color.FromArgb(70, 230, 140), 1f) { DashStyle = DashStyle.Dash };
        private readonly Pen _stopPen = new Pen(Color.FromArgb(255, 80, 80), 1f) { DashStyle = DashStyle.Dash };
        private readonly Pen _buyPen = new Pen(Color.FromArgb(0, 216, 255), 1f) { DashStyle = DashStyle.Dot };
        private readonly Pen _crosshairPen = new Pen(Color.FromArgb(90, 220, 240, 255), 1f) { DashStyle = DashStyle.Dash };
        private readonly SolidBrush _dimBrush = new SolidBrush(Color.FromArgb(130, 150, 175));
        private readonly SolidBrush _cyanBrush = new SolidBrush(Color.FromArgb(0, 216, 255));
        private readonly SolidBrush _brightBrush = new SolidBrush(Color.FromArgb(235, 245, 255));
        private readonly SolidBrush _targetBrush = new SolidBrush(Color.FromArgb(70, 230, 140));
        private readonly SolidBrush _stopBrush = new SolidBrush(Color.FromArgb(255, 90, 90));
        private readonly SolidBrush _buyBrush = new SolidBrush(Color.FromArgb(0, 216, 255));
        private readonly SolidBrush _ma5Brush = new SolidBrush(Color.FromArgb(255, 215, 64));
        private readonly SolidBrush _ma20Brush = new SolidBrush(Color.FromArgb(255, 90, 180));
        private readonly Font _fontSmall = new Font("Malgun Gothic", 7.8f);
        private readonly Font _fontBold = new Font("Malgun Gothic", 8.2f, FontStyle.Bold);

        public MiniCandleChartControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            UpdateStyles();

            this.BackColor = Color.FromArgb(16, 20, 28);
            this.Height = 155;
            this.Dock = DockStyle.Top;
        }

        public void SetData(StockHoldingItem item)
        {
            if (item == null)
            {
                _stockCode = null;
                _stockName = null;
                _candles = null;
                _targetPrice = 0;
                _stopPrice = 0;
                _buyPrice = 0;
                _currentPrice = 0;
                _hoverIndex = -1;
                _isLoading = false;
                Invalidate();
                return;
            }

            _stockCode = item.StockCode;
            _stockName = item.StockName;
            _currentPrice = item.CurrentPrice;
            _targetPrice = item.RecommendedTargetPrice > 0 ? item.RecommendedTargetPrice : item.ResistancePrice;
            _stopPrice = item.TargetStopPrice > 0 ? item.TargetStopPrice : (item.SupportPrice > 0 ? item.SupportPrice * 0.97m : item.CurrentPrice * 0.93m);
            _buyPrice = item.RecommendedBuyPrice > 0 ? item.RecommendedBuyPrice : item.BuyPrice;
            _hoverIndex = -1;

            if (item.RecentCandles != null && item.RecentCandles.Count > 0)
            {
                // 최근 일봉을 복사 후 시간 순(과거 -> 현재) 오름차순 정렬
                var asc = new List<DailyCandle>(item.RecentCandles);
                if (asc.Count >= 2 && asc[0].Date > asc[asc.Count - 1].Date)
                {
                    asc.Reverse();
                }
                _candles = asc;
                _isLoading = false;
                Invalidate();
            }
            else if (!string.IsNullOrEmpty(item.StockCode))
            {
                _isLoading = true;
                _candles = null;
                Invalidate();
                _ = LoadCandlesAsync(item);
            }
            else
            {
                _candles = null;
                _isLoading = false;
                Invalidate();
            }
        }

        private async Task LoadCandlesAsync(StockHoldingItem item)
        {
            try
            {
                var list = await TechnicalIndicatorService.Fetch20DailyCandlesAsync(item.StockCode).ConfigureAwait(false);
                if (list != null && list.Count > 0)
                {
                    item.RecentCandles = list;
                    if (_stockCode == item.StockCode)
                    {
                        var asc = new List<DailyCandle>(list);
                        if (asc.Count >= 2 && asc[0].Date > asc[asc.Count - 1].Date)
                        {
                            asc.Reverse();
                        }
                        if (this.IsHandleCreated && !this.IsDisposed)
                        {
                            this.BeginInvoke(new Action(() =>
                            {
                                _candles = asc;
                                _isLoading = false;
                                Invalidate();
                            }));
                        }
                    }
                }
                else
                {
                    if (this.IsHandleCreated && !this.IsDisposed)
                    {
                        this.BeginInvoke(new Action(() =>
                        {
                            _isLoading = false;
                            Invalidate();
                        }));
                    }
                }
            }
            catch
            {
                if (this.IsHandleCreated && !this.IsDisposed)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        _isLoading = false;
                        Invalidate();
                    }));
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_candles == null || _candles.Count == 0) return;

            const int leftMargin = 10;
            const int rightAxisMargin = 58;
            int chartW = this.Width - leftMargin - rightAxisMargin;
            if (chartW <= 0) return;

            float slotW = (float)chartW / _candles.Count;
            int idx = (int)((e.X - leftMargin) / slotW);

            if (idx >= 0 && idx < _candles.Count && e.Y >= 20 && e.Y <= this.Height - 6)
            {
                if (_hoverIndex != idx)
                {
                    _hoverIndex = idx;
                    Invalidate();
                }
            }
            else if (_hoverIndex != -1)
            {
                _hoverIndex = -1;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hoverIndex != -1)
            {
                _hoverIndex = -1;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            int w = this.Width;
            int h = this.Height;

            // 1. 배경 및 테두리
            g.FillRectangle(_bgBrush, 0, 0, w, h);
            g.DrawRectangle(_borderPen, 0, 0, w - 1, h - 1);

            // 2. 미선택 또는 로딩 상태
            if (string.IsNullOrEmpty(_stockCode))
            {
                string msg = "좌측 목록에서 종목을 선택하시면 20일 일봉 차트 및 AI 타점선이 표시됩니다.";
                var sz = g.MeasureString(msg, _fontSmall);
                g.DrawString(msg, _fontSmall, _dimBrush, (w - sz.Width) / 2, (h - sz.Height) / 2);
                return;
            }

            if (_isLoading)
            {
                string msg = $"[{_stockName}] 20일 일봉 캔들 데이터 수집 중...";
                var sz = g.MeasureString(msg, _fontSmall);
                g.DrawString(msg, _fontSmall, _cyanBrush, (w - sz.Width) / 2, (h - sz.Height) / 2);
                return;
            }

            if (_candles == null || _candles.Count == 0)
            {
                string msg = $"[{_stockName}] 일봉 캔들 데이터를 불러올 수 없습니다.";
                var sz = g.MeasureString(msg, _fontSmall);
                g.DrawString(msg, _fontSmall, _dimBrush, (w - sz.Width) / 2, (h - sz.Height) / 2);
                return;
            }

            // 3. 상단 헤더 바 (Y: 2 ~ 18)
            g.DrawString("[ 20일 일봉 차트 ]", _fontBold, _cyanBrush, 10, 3);

            if (_hoverIndex >= 0 && _hoverIndex < _candles.Count)
            {
                var hc = _candles[_hoverIndex];
                decimal prevClose = _hoverIndex > 0 ? _candles[_hoverIndex - 1].Close : hc.Open;
                decimal diff = hc.Close - prevClose;
                decimal diffRate = prevClose > 0 ? (diff / prevClose * 100m) : 0m;
                string sign = diffRate >= 0 ? "+" : "";

                string hoverTxt = $"{hc.Date:MM/dd}  시:{hc.Open:N0}  고:{hc.High:N0}  저:{hc.Low:N0}  종:{hc.Close:N0}({sign}{diffRate:F1}%)  거:{hc.Volume:N0}";
                g.DrawString(hoverTxt, _fontSmall, _brightBrush, 110, 4);
            }
            else
            {
                // 범례 표시
                float legX = w - 235;
                g.DrawLine(_ma5Pen, legX, 10, legX + 12, 10);
                g.DrawString("5일선", _fontSmall, _ma5Brush, legX + 15, 3);

                legX += 52;
                g.DrawLine(_ma20Pen, legX, 10, legX + 12, 10);
                g.DrawString("20일선", _fontSmall, _ma20Brush, legX + 15, 3);

                legX += 58;
                g.DrawLine(_targetPen, legX, 10, legX + 10, 10);
                g.DrawString("AI타점", _fontSmall, _targetBrush, legX + 13, 3);
            }

            // 4. 차트 영역 레이아웃 좌표 계산
            const int leftMargin = 10;
            const int rightAxisMargin = 58;
            const int topMargin = 22;
            const int bottomMargin = 6;

            int chartW = w - leftMargin - rightAxisMargin;
            int totalChartH = h - topMargin - bottomMargin;
            if (chartW <= 20 || totalChartH <= 20) return;

            int priceChartH = (int)(totalChartH * 0.70f);
            int volumeH = totalChartH - priceChartH - 4;
            int priceBottomY = topMargin + priceChartH;
            int volumeTopY = priceBottomY + 4;
            int volumeBottomY = h - bottomMargin;

            // 5. 가격 상하한 범위 스캔 (캔들 + AI 목표가/손절가 수평선 수용)
            decimal minPrice = decimal.MaxValue;
            decimal maxPrice = decimal.MinValue;

            foreach (var c in _candles)
            {
                if (c.Low > 0 && c.Low < minPrice) minPrice = c.Low;
                if (c.High > maxPrice) maxPrice = c.High;
            }

            if (_targetPrice > 0 && _targetPrice <= maxPrice * 1.25m) maxPrice = Math.Max(maxPrice, _targetPrice);
            if (_stopPrice > 0 && _stopPrice >= minPrice * 0.75m) minPrice = Math.Min(minPrice, _stopPrice);
            if (_buyPrice > 0 && _buyPrice <= maxPrice * 1.15m && _buyPrice >= minPrice * 0.85m)
            {
                minPrice = Math.Min(minPrice, _buyPrice);
                maxPrice = Math.Max(maxPrice, _buyPrice);
            }

            if (minPrice == decimal.MaxValue || maxPrice == decimal.MinValue) return;

            // 여백 3% 적용
            decimal pDiff = Math.Max(1m, maxPrice - minPrice);
            minPrice -= pDiff * 0.03m;
            maxPrice += pDiff * 0.03m;
            decimal pRange = maxPrice - minPrice;

            float PriceToY(decimal price)
            {
                if (pRange <= 0) return topMargin + priceChartH / 2f;
                float norm = (float)((maxPrice - price) / pRange);
                norm = Math.Max(0f, Math.Min(1f, norm));
                return topMargin + norm * priceChartH;
            }

            // 6. 그리드 및 우측 가격 눈금 라인
            for (int step = 0; step <= 3; step++)
            {
                float gy = topMargin + (priceChartH * step / 3f);
                g.DrawLine(_gridPen, leftMargin, gy, leftMargin + chartW, gy);

                decimal gridPrice = maxPrice - (pRange * step / 3m);
                g.DrawString($"{gridPrice:N0}", _fontSmall, _dimBrush, leftMargin + chartW + 3, gy - 6);
            }

            // 7. AI 퀀트 타점 수평선 오버레이 (목표가·손절가·권고매수가)
            if (_targetPrice > 0 && _targetPrice <= maxPrice && _targetPrice >= minPrice)
            {
                float ty = PriceToY(_targetPrice);
                g.DrawLine(_targetPen, leftMargin, ty, leftMargin + chartW, ty);
                g.DrawString($"🎯{_targetPrice:N0}", _fontSmall, _targetBrush, leftMargin + chartW + 3, ty - 6);
            }

            if (_stopPrice > 0 && _stopPrice <= maxPrice && _stopPrice >= minPrice)
            {
                float sy = PriceToY(_stopPrice);
                g.DrawLine(_stopPen, leftMargin, sy, leftMargin + chartW, sy);
                g.DrawString($"⚠️{_stopPrice:N0}", _fontSmall, _stopBrush, leftMargin + chartW + 3, sy - 6);
            }

            if (_buyPrice > 0 && _buyPrice <= maxPrice && _buyPrice >= minPrice && Math.Abs(_buyPrice - _stopPrice) > pRange * 0.04m)
            {
                float by = PriceToY(_buyPrice);
                g.DrawLine(_buyPen, leftMargin, by, leftMargin + chartW, by);
                g.DrawString($"💎{_buyPrice:N0}", _fontSmall, _buyBrush, leftMargin + chartW + 3, by - 6);
            }

            // 8. 캔들스틱 및 거래량 렌더링
            int count = _candles.Count;
            float slotW = (float)chartW / count;
            float bodyW = Math.Max(3f, slotW * 0.65f);

            long maxVol = 1;
            foreach (var c in _candles) if (c.Volume > maxVol) maxVol = c.Volume;

            var ma5Points = new List<PointF>();
            var ma20Points = new List<PointF>();

            for (int i = 0; i < count; i++)
            {
                var c = _candles[i];
                float cx = leftMargin + i * slotW + slotW / 2f;
                float highY = PriceToY(c.High);
                float lowY = PriceToY(c.Low);
                float openY = PriceToY(c.Open);
                float closeY = PriceToY(c.Close);

                bool isUp = c.Close >= c.Open;
                var candlePen = isUp ? _upCandlePen : _downCandlePen;
                var candleBrush = isUp ? _upCandleBrush : _downCandleBrush;
                var volBrush = isUp ? _upVolBrush : _downVolBrush;

                // (1) 꼬리 (High - Low)
                g.DrawLine(candlePen, cx, highY, cx, lowY);

                // (2) 몸통 (Open - Close)
                float topBody = Math.Min(openY, closeY);
                float bodyH = Math.Max(2f, Math.Abs(closeY - openY));
                float bodyX = cx - bodyW / 2f;
                g.FillRectangle(candleBrush, bodyX, topBody, bodyW, bodyH);

                // (3) 거래량 바
                float volNorm = (float)c.Volume / maxVol;
                float vh = Math.Max(1f, volNorm * volumeH);
                float vy = volumeBottomY - vh;
                g.FillRectangle(volBrush, bodyX, vy, bodyW, vh);

                // (4) 이동평균선 좌표 계산 (MA5 & MA20)
                decimal sum5 = 0m;
                int c5 = 0;
                for (int k = Math.Max(0, i - 4); k <= i; k++) { sum5 += _candles[k].Close; c5++; }
                if (c5 > 0) ma5Points.Add(new PointF(cx, PriceToY(sum5 / c5)));

                decimal sum20 = 0m;
                int c20 = 0;
                for (int k = Math.Max(0, i - 19); k <= i; k++) { sum20 += _candles[k].Close; c20++; }
                if (c20 > 0) ma20Points.Add(new PointF(cx, PriceToY(sum20 / c20)));
            }

            // 9. 이동평균선 그리기
            if (ma5Points.Count >= 2) g.DrawLines(_ma5Pen, ma5Points.ToArray());
            if (ma20Points.Count >= 2) g.DrawLines(_ma20Pen, ma20Points.ToArray());

            // 10. 마우스 호버 크로스헤어
            if (_hoverIndex >= 0 && _hoverIndex < count)
            {
                float hcx = leftMargin + _hoverIndex * slotW + slotW / 2f;
                g.DrawLine(_crosshairPen, hcx, topMargin, hcx, volumeBottomY);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _bgBrush?.Dispose();
                _borderPen?.Dispose();
                _gridPen?.Dispose();
                _upCandleBrush?.Dispose();
                _upCandlePen?.Dispose();
                _downCandleBrush?.Dispose();
                _downCandlePen?.Dispose();
                _upVolBrush?.Dispose();
                _downVolBrush?.Dispose();
                _ma5Pen?.Dispose();
                _ma20Pen?.Dispose();
                _targetPen?.Dispose();
                _stopPen?.Dispose();
                _buyPen?.Dispose();
                _crosshairPen?.Dispose();
                _dimBrush?.Dispose();
                _cyanBrush?.Dispose();
                _brightBrush?.Dispose();
                _targetBrush?.Dispose();
                _stopBrush?.Dispose();
                _buyBrush?.Dispose();
                _ma5Brush?.Dispose();
                _ma20Brush?.Dispose();
                _fontSmall?.Dispose();
                _fontBold?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
