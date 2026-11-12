using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using GateHelper.StockAdvisor.Models;

namespace GateHelper.StockAdvisor
{
    public partial class StockAdvisorControl
    {
        // 오늘의 AI 알짜 추천주 배너 UI 컴포넌트
        private Panel _pnlTopPickBanner;
        private FlowLayoutPanel _flpTopPickCards;
        private Label _lblTopPickTitle;
        private Label _lblTopPickSummary;
        private Button _btnToggleTopPickBanner;
        private bool _isTopPickBannerCollapsed = false;

        private void InitTopPickBanner()
        {
            _pnlTopPickBanner = new Panel
            {
                Dock = DockStyle.Top,
                Height = 84,
                BackColor = Color.FromArgb(18, 24, 34),
                Padding = new Padding(12, 6, 12, 6)
            };

            // 커스텀 하단 테두리 페인팅
            _pnlTopPickBanner.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(38, 48, 68), 1.2f))
                {
                    e.Graphics.DrawLine(pen, 0, _pnlTopPickBanner.Height - 1, _pnlTopPickBanner.Width, _pnlTopPickBanner.Height - 1);
                }
            };

            // 좌측 헤더 영역 (타이틀 + 부제목)
            var pnlLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = 200,
                BackColor = Color.Transparent
            };

            _lblTopPickTitle = new Label
            {
                Text = "★ 오늘의 AI 알짜 추천주",
                Font = new Font("Malgun Gothic", 9.8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 215, 0),
                Location = new Point(2, 6),
                AutoSize = true
            };

            _lblTopPickSummary = new Label
            {
                Text = "초엄격 조건 검증 중...",
                Font = new Font("Segoe UI", 8.2f),
                ForeColor = Color.FromArgb(140, 160, 185),
                Location = new Point(4, 30),
                AutoSize = true
            };

            pnlLeft.Controls.Add(_lblTopPickTitle);
            pnlLeft.Controls.Add(_lblTopPickSummary);

            // 우측 접기/펼치기 토글 버튼
            _btnToggleTopPickBanner = new Button
            {
                Text = "▲ 접기",
                Size = new Size(64, 26),
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(26, 34, 48),
                ForeColor = Color.FromArgb(160, 180, 205),
                Font = new Font("Malgun Gothic", 7.8f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnToggleTopPickBanner.FlatAppearance.BorderColor = Color.FromArgb(48, 60, 80);
            _btnToggleTopPickBanner.Click += (s, e) =>
            {
                _isTopPickBannerCollapsed = !_isTopPickBannerCollapsed;
                if (_isTopPickBannerCollapsed)
                {
                    _pnlTopPickBanner.Height = 36;
                    _btnToggleTopPickBanner.Text = "▼ 펼치기";
                    _flpTopPickCards.Visible = false;
                    _lblTopPickSummary.Visible = false;
                }
                else
                {
                    _pnlTopPickBanner.Height = 84;
                    _btnToggleTopPickBanner.Text = "▲ 접기";
                    _flpTopPickCards.Visible = true;
                    _lblTopPickSummary.Visible = true;
                }
            };

            // 중앙 카드 컨테이너
            _flpTopPickCards = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = false,
                BackColor = Color.Transparent,
                Padding = new Padding(8, 2, 8, 2)
            };

            _pnlTopPickBanner.Controls.Add(_flpTopPickCards);
            _pnlTopPickBanner.Controls.Add(pnlLeft);
            _pnlTopPickBanner.Controls.Add(_btnToggleTopPickBanner);
        }

        /// <summary>
        /// 초엄격 알짜 기준 (종합 8점↑ + 메이저 매수 + 지뢰 0건 + 매수추천/상승추세) 통과 종목 0~3개 선별 및 배너 갱신
        /// </summary>
        public void UpdateTopPickBanner()
        {
            if (_pnlTopPickBanner == null || _flpTopPickCards == null || _portfolio?.Items == null) return;

            // 1. 초엄격 알짜 필터링
            var topPicks = _portfolio.Items.Where(item =>
            {
                // 조건 1: 종합 점수 8점 이상 (A/S급 우량주)
                if (item.FinalScore < 8) return false;

                // 조건 2: 큰손(외인+기관) 동시 매집
                string supply = SupplyHelper.GetSupplyInfo(item.MajorSupplyBadge).Text;
                if (supply != "메이저 매수") return false;

                // 조건 3: 재무 및 공시 지뢰 0건 (안전 or 우수)
                string landmine = LandmineHelper.GetSimplifiedBadge(item).Text;
                if (landmine != "안전" && landmine != "우수") return false;

                // 조건 4: 최종 액션이 매수추천 또는 상승추세 (과열/급락/비중축소/매수금지 전면 배제)
                string action = StockVerdictHelper.Evaluate(item).BadgeText;
                if (action != "매수추천" && action != "상승추세") return false;

                return true;
            })
            .OrderByDescending(x => x.FinalScore)
            .ThenByDescending(x => x.ChartScore + x.SupplyFinanceScore)
            .Take(3)
            .ToList();

            _flpTopPickCards.SuspendLayout();
            _flpTopPickCards.Controls.Clear();

            if (topPicks.Count == 0)
            {
                // [0개인 경우]: 억지로 추천하지 않고 정직하게 현금 관망 안내
                _lblTopPickSummary.Text = "엄격 기준 통과: 0종목";
                _lblTopPickSummary.ForeColor = Color.FromArgb(140, 160, 185);

                var pnlEmpty = new Panel
                {
                    Width = 620,
                    Height = 64,
                    BackColor = Color.FromArgb(24, 30, 42),
                    Margin = new Padding(4, 2, 4, 2),
                    Padding = new Padding(12, 10, 12, 8)
                };

                pnlEmpty.Paint += (s, e) =>
                {
                    using (var pen = new Pen(Color.FromArgb(50, 62, 85), 1f))
                    {
                        e.Graphics.DrawRectangle(pen, 0, 0, pnlEmpty.Width - 1, pnlEmpty.Height - 1);
                    }
                };

                var lblEmptyTitle = new Label
                {
                    Text = "● 현재 엄격한 알짜 기준(8.0점↑, 메이저 매수, 지뢰 0건)을 통과한 종목이 없습니다.",
                    Font = new Font("Malgun Gothic", 9.0f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(200, 215, 235),
                    AutoSize = true,
                    Location = new Point(12, 12)
                };

                var lblEmptySub = new Label
                {
                    Text = "무리한 추격 매수를 자제하고, 확실한 큰손 수급이 유입될 때까지 [현금 관망]을 권장합니다.",
                    Font = new Font("Segoe UI", 8.5f),
                    ForeColor = Color.FromArgb(130, 155, 185),
                    AutoSize = true,
                    Location = new Point(14, 34)
                };

                pnlEmpty.Controls.Add(lblEmptyTitle);
                pnlEmpty.Controls.Add(lblEmptySub);
                _flpTopPickCards.Controls.Add(pnlEmpty);
            }
            else
            {
                // [1~3개인 경우]: 최우량 알짜 카드 렌더링
                _lblTopPickSummary.Text = $"엄격 기준 통과: {topPicks.Count}종목 포착!";
                _lblTopPickSummary.ForeColor = Color.FromArgb(0, 230, 118);

                for (int i = 0; i < topPicks.Count; i++)
                {
                    var stock = topPicks[i];
                    var card = CreateTopPickCard(stock, i + 1);
                    _flpTopPickCards.Controls.Add(card);
                }
            }

            _flpTopPickCards.ResumeLayout();
        }

        private Panel CreateTopPickCard(StockHoldingItem stock, int rank)
        {
            var pnlCard = new Panel
            {
                Width = 240,
                Height = 68,
                BackColor = Color.FromArgb(22, 30, 44),
                Margin = new Padding(0, 0, 10, 0),
                Padding = new Padding(10, 6, 10, 6),
                Cursor = Cursors.Hand
            };

            // 카드 외곽 테두리 페인팅 (1위는 골드, 2~3위는 청록 네온)
            Color borderColor = (rank == 1) ? Color.FromArgb(255, 215, 0) : Color.FromArgb(0, 216, 255);
            pnlCard.Paint += (s, e) =>
            {
                using (var pen = new Pen(borderColor, 1.2f))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlCard.Width - 1, pnlCard.Height - 1);
                }
            };

            // 호버 인터랙션
            pnlCard.MouseEnter += (s, e) => pnlCard.BackColor = Color.FromArgb(32, 44, 64);
            pnlCard.MouseLeave += (s, e) => pnlCard.BackColor = Color.FromArgb(22, 30, 44);

            // 클릭 시 해당 종목 리스트뷰 선택 및 우측 상세 패널 자동 브리핑
            pnlCard.Click += (s, e) => SelectStockInListView(stock.StockCode);

            // 상단 행: 순위 뱃지 + 종목명
            var lblRank = new Label
            {
                Text = $"[{rank}위]",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = (rank == 1) ? Color.FromArgb(255, 215, 0) : Color.FromArgb(0, 216, 255),
                AutoSize = true,
                Location = new Point(8, 6),
                Cursor = Cursors.Hand
            };
            lblRank.Click += (s, e) => SelectStockInListView(stock.StockCode);

            var lblName = new Label
            {
                Text = stock.StockName,
                Font = new Font("Malgun Gothic", 9.8f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(44, 4),
                Cursor = Cursors.Hand
            };
            lblName.Click += (s, e) => SelectStockInListView(stock.StockCode);

            string priceText = $"{stock.CurrentPrice:N0}원";
            string rateText = (stock.ChangeRate >= 0) ? $"+{stock.ChangeRate:F2}%" : $"{stock.ChangeRate:F2}%";
            Color rateColor = (stock.ChangeRate > 0) ? Color.FromArgb(255, 90, 90) : (stock.ChangeRate < 0 ? Color.FromArgb(90, 160, 255) : Color.White);

            var lblPrice = new Label
            {
                Text = $"{priceText} ({rateText})",
                Font = new Font("Segoe UI", 8.0f, FontStyle.Bold),
                ForeColor = rateColor,
                AutoSize = true,
                Location = new Point(8, 27),
                Cursor = Cursors.Hand
            };
            lblPrice.Click += (s, e) => SelectStockInListView(stock.StockCode);

            // 하단 행: 점수 + 액션 + 수급 뱃지
            var lblScore = new Label
            {
                Text = $"★ {stock.FinalScore}점",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 215, 0),
                AutoSize = true,
                Location = new Point(8, 47),
                Cursor = Cursors.Hand
            };
            lblScore.Click += (s, e) => SelectStockInListView(stock.StockCode);

            var lblAction = new Label
            {
                Text = $"● {StockVerdictHelper.Evaluate(stock).BadgeText}",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 230, 118),
                AutoSize = true,
                Location = new Point(66, 47),
                Cursor = Cursors.Hand
            };
            lblAction.Click += (s, e) => SelectStockInListView(stock.StockCode);

            var lblSupply = new Label
            {
                Text = "[메이저 매수]",
                Font = new Font("Segoe UI", 8.0f),
                ForeColor = Color.FromArgb(140, 210, 255),
                AutoSize = true,
                Location = new Point(148, 48),
                Cursor = Cursors.Hand
            };
            lblSupply.Click += (s, e) => SelectStockInListView(stock.StockCode);

            var tt = new ToolTip();
            tt.SetToolTip(pnlCard, $"클릭 시 {stock.StockName} 종목의 AI 상세 진단 리포트를 엽니다.");
            tt.SetToolTip(lblName, $"클릭 시 {stock.StockName} 종목의 AI 상세 진단 리포트를 엽니다.");

            pnlCard.Controls.Add(lblRank);
            pnlCard.Controls.Add(lblName);
            pnlCard.Controls.Add(lblPrice);
            pnlCard.Controls.Add(lblScore);
            pnlCard.Controls.Add(lblAction);
            pnlCard.Controls.Add(lblSupply);

            return pnlCard;
        }

        /// <summary>
        /// 추천 카드 클릭 시 리스트뷰에서 해당 종목을 찾아 자동 포커스 및 상세 브리핑 갱신
        /// </summary>
        private void SelectStockInListView(string stockCode)
        {
            if (_lvStocks == null || string.IsNullOrEmpty(stockCode)) return;

            foreach (ListViewItem lvi in _lvStocks.Items)
            {
                if (lvi.Tag is StockHoldingItem item && item.StockCode.Equals(stockCode, StringComparison.OrdinalIgnoreCase))
                {
                    _lvStocks.SelectedItems.Clear();
                    lvi.Selected = true;
                    lvi.EnsureVisible();
                    _lvStocks.Focus();
                    UpdateDetailPanel(item);
                    break;
                }
            }
        }
    }
}
