using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GateHelper.StockAdvisor.Models;
using GateHelper.StockAdvisor.Services;

namespace GateHelper.StockAdvisor
{
    public partial class StockAdvisorControl
    {
        // 대한민국 시장 전체(코스피/코스닥) 실시간 스캔 기반 오늘의 AI 알짜 추천주 배너 UI 컴포넌트
        private Panel _pnlTopPickBanner;
        private FlowLayoutPanel _flpTopPickCards;
        private Label _lblTopPickTitle;
        private Label _lblTopPickSummary;
        private Button _btnToggleTopPickBanner;
        private bool _isTopPickBannerCollapsed = false;
        private bool _isScanningMarketTopPicks = false;

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
                Width = 220,
                BackColor = Color.Transparent
            };

            _lblTopPickTitle = new Label
            {
                Text = "★ 오늘의 시장 알짜 추천주",
                Font = new Font("Malgun Gothic", 9.8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 215, 0),
                Location = new Point(2, 6),
                AutoSize = true
            };

            _lblTopPickSummary = new Label
            {
                Text = "대한민국 시장 전체 스캔 중...",
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

            // 최초 시장 전체 알짜 스캔 비동기 가동
            _ = RefreshMarketTopPicksAsync(forceRefresh: false);
        }

        /// <summary>
        /// 동기화 호환용 배너 갱신 호출부
        /// </summary>
        public void UpdateTopPickBanner()
        {
            _ = RefreshMarketTopPicksAsync(forceRefresh: false);
        }

        /// <summary>
        /// 대한민국 시장 전체(코스피/코스닥 150대 주도주 풀)에서
        /// [종합 8점↑ + 메이저 매수 + 지뢰 0건] 초엄격 알짜 종목 0~3개 선별 비동기 실행
        /// </summary>
        public async Task RefreshMarketTopPicksAsync(bool forceRefresh = false)
        {
            if (_isScanningMarketTopPicks) return;
            _isScanningMarketTopPicks = true;

            try
            {
                if (this.InvokeRequired)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        if (_lblTopPickSummary != null)
                        {
                            _lblTopPickSummary.Text = "대한민국 시장 전체 스캔 중...";
                            _lblTopPickSummary.ForeColor = Color.FromArgb(140, 160, 185);
                        }
                    }));
                }

                // 시장 전체 코스피 100 + 코스닥 50 주도주 풀 스캔
                var leaders = await MarketLeaderRadarService.ScanMarketLeadersAsync(forceRefresh).ConfigureAwait(false);

                // 초엄격 알짜 필터링
                var topPicks = (leaders ?? new List<MarketLeaderItem>())
                    .Where(x => !x.IsEtf) // ETF/ETN 제외, 순수 알짜 주도주
                    .Where(x => x.FinalScore >= 8) // 8점 이상 A/S급 우량주
                    .Where(x => !x.HasCriticalLandmine && (x.LandmineBadge.Contains("안전") || x.LandmineBadge.Contains("우수") || x.LandmineBadge.Contains("클린"))) // 지뢰 0건
                    .Where(x => x.SupplyBadge.Contains("메이저") || x.SupplyBadge.Contains("쌍끌이")) // 외인+기관 동시 대량 매집
                    .Where(x => x.CanBuy || x.IsGoldenPullback || x.IsOversoldBounce || x.IsVolumeBreakout) // 매수 가능 신호
                    .OrderByDescending(x => x.FinalScore)
                    .ThenByDescending(x => x.ChartScore + x.SupplyFinanceScore)
                    .ThenByDescending(x => x.TradingValueRaw)
                    .Take(3)
                    .ToList();

                if (this.IsDisposed || !this.IsHandleCreated) return;

                this.BeginInvoke(new Action(() =>
                {
                    RenderTopPicksUi(topPicks);
                }));
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "StockAdvisor.TopPickBanner.Refresh");
            }
            finally
            {
                _isScanningMarketTopPicks = false;
            }
        }

        private void RenderTopPicksUi(List<MarketLeaderItem> topPicks)
        {
            if (_pnlTopPickBanner == null || _flpTopPickCards == null) return;

            _flpTopPickCards.SuspendLayout();
            _flpTopPickCards.Controls.Clear();

            if (topPicks == null || topPicks.Count == 0)
            {
                // [0개인 경우]: 억지로 추천하지 않고 정직하게 시장 전체 현금 관망 안내
                _lblTopPickSummary.Text = "시장 전체 엄격 통과: 0종목";
                _lblTopPickSummary.ForeColor = Color.FromArgb(140, 160, 185);

                var pnlEmpty = new Panel
                {
                    Width = 640,
                    Height = 66,
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
                    Text = "● 현재 대한민국 시장 전체에서 초엄격 알짜 기준(8.0점↑, 메이저매수, 지뢰0건)을 통과한 종목이 없습니다.",
                    Font = new Font("Malgun Gothic", 8.8f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(200, 215, 235),
                    AutoSize = true,
                    Location = new Point(12, 12)
                };

                var lblEmptySub = new Label
                {
                    Text = "무리한 추격 매수를 자제하고, 확실한 큰손 수급이 유입될 때까지 [현금 관망]을 권장합니다.",
                    Font = new Font("Segoe UI", 8.2f),
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
                _lblTopPickSummary.Text = $"시장 전체 엄격 통과: {topPicks.Count}종목 포착!";
                _lblTopPickSummary.ForeColor = Color.FromArgb(0, 230, 118);

                for (int i = 0; i < topPicks.Count; i++)
                {
                    var stock = topPicks[i];
                    var card = CreateMarketTopPickCard(stock, i + 1);
                    _flpTopPickCards.Controls.Add(card);
                }
            }

            _flpTopPickCards.ResumeLayout();
        }

        private Panel CreateMarketTopPickCard(MarketLeaderItem stock, int rank)
        {
            var pnlCard = new Panel
            {
                Width = 275,
                Height = 68,
                BackColor = Color.FromArgb(22, 30, 44),
                Margin = new Padding(0, 0, 10, 0),
                Padding = new Padding(8, 6, 8, 6),
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

            // 카드 클릭 시 해당 종목 AI 종합 평가 다이얼로그 즉시 팝업
            pnlCard.Click += (s, e) => ShowStockEvaluatorDialog(stock.StockCode);

            // 1. 상단 행: 순위 + 종목명 + 원클릭 [+담기] 버튼
            var lblRank = new Label
            {
                Text = $"[{rank}위]",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = (rank == 1) ? Color.FromArgb(255, 215, 0) : Color.FromArgb(0, 216, 255),
                AutoSize = true,
                Location = new Point(6, 6),
                Cursor = Cursors.Hand
            };
            lblRank.Click += (s, e) => ShowStockEvaluatorDialog(stock.StockCode);

            var lblName = new Label
            {
                Text = stock.StockName,
                Font = new Font("Malgun Gothic", 9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(40, 4),
                Cursor = Cursors.Hand
            };
            lblName.Click += (s, e) => ShowStockEvaluatorDialog(stock.StockCode);

            // [+ 내 종목 담기] 버튼
            bool isAlreadyIn = _portfolio.Items.Any(x => x.StockCode.TrimStart('0').Equals(stock.StockCode.TrimStart('0'), StringComparison.OrdinalIgnoreCase));
            var btnAdd = new Button
            {
                Text = isAlreadyIn ? "✔ 담김" : "+ 담기",
                Size = new Size(58, 24),
                Location = new Point(210, 5),
                FlatStyle = FlatStyle.Flat,
                BackColor = isAlreadyIn ? Color.FromArgb(34, 46, 62) : Color.FromArgb(0, 150, 110),
                ForeColor = isAlreadyIn ? Color.FromArgb(140, 175, 205) : Color.White,
                Font = new Font("Malgun Gothic", 7.8f, FontStyle.Bold),
                Cursor = isAlreadyIn ? Cursors.Default : Cursors.Hand,
                Enabled = !isAlreadyIn
            };
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.Click += (s, e) =>
            {
                AddTopPickToWatchlist(stock);
                btnAdd.Text = "✔ 담김";
                btnAdd.BackColor = Color.FromArgb(34, 46, 62);
                btnAdd.ForeColor = Color.FromArgb(140, 175, 205);
                btnAdd.Enabled = false;
            };

            // 2. 중간 행: 현재가 + 등락률 + 거래대금
            string priceText = $"{stock.CurrentPrice:N0}원";
            string rateText = (stock.ChangeRate >= 0) ? $"+{stock.ChangeRate:F2}%" : $"{stock.ChangeRate:F2}%";
            Color rateColor = (stock.ChangeRate > 0) ? Color.FromArgb(255, 90, 90) : (stock.ChangeRate < 0 ? Color.FromArgb(90, 160, 255) : Color.White);

            var lblPrice = new Label
            {
                Text = $"{priceText} ({rateText}) · {stock.TradingValueText}",
                Font = new Font("Segoe UI", 7.8f, FontStyle.Bold),
                ForeColor = rateColor,
                AutoSize = true,
                Location = new Point(6, 27),
                Cursor = Cursors.Hand
            };
            lblPrice.Click += (s, e) => ShowStockEvaluatorDialog(stock.StockCode);

            // 3. 하단 행: 점수 + 수급 + 매수 전략 뱃지
            var lblScore = new Label
            {
                Text = $"★ {stock.FinalScore}점",
                Font = new Font("Segoe UI", 8.2f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 215, 0),
                AutoSize = true,
                Location = new Point(6, 47),
                Cursor = Cursors.Hand
            };
            lblScore.Click += (s, e) => ShowStockEvaluatorDialog(stock.StockCode);

            var lblAction = new Label
            {
                Text = $"● {stock.PrimaryStrategyBadge}",
                Font = new Font("Segoe UI", 8.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 230, 118),
                AutoSize = true,
                Location = new Point(58, 48),
                Cursor = Cursors.Hand
            };
            lblAction.Click += (s, e) => ShowStockEvaluatorDialog(stock.StockCode);

            var lblSupply = new Label
            {
                Text = "[메이저 매수]",
                Font = new Font("Segoe UI", 7.8f),
                ForeColor = Color.FromArgb(140, 210, 255),
                AutoSize = true,
                Location = new Point(148, 49),
                Cursor = Cursors.Hand
            };
            lblSupply.Click += (s, e) => ShowStockEvaluatorDialog(stock.StockCode);

            var tt = new ToolTip();
            tt.SetToolTip(pnlCard, $"클릭 시 {stock.StockName} 종목의 AI 정밀 진단 팝업을 엽니다.\n오른쪽 [+ 담기]를 누르면 내 관심종목 리스트에 즉시 추가됩니다.");

            pnlCard.Controls.Add(lblRank);
            pnlCard.Controls.Add(lblName);
            pnlCard.Controls.Add(btnAdd);
            pnlCard.Controls.Add(lblPrice);
            pnlCard.Controls.Add(lblScore);
            pnlCard.Controls.Add(lblAction);
            pnlCard.Controls.Add(lblSupply);

            return pnlCard;
        }

        /// <summary>
        /// 추천 카드에서 [+ 담기] 클릭 시 내 관심종목에 즉시 등록 및 리스트뷰 자동 동기화
        /// </summary>
        private void AddTopPickToWatchlist(MarketLeaderItem leader)
        {
            if (leader == null || _portfolio == null) return;

            string normCode = leader.StockCode.TrimStart('0');
            if (_portfolio.Items.Any(x => x.StockCode.TrimStart('0').Equals(normCode, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show($"'{leader.StockName}' 종목은 이미 관심/보유 목록에 등록되어 있습니다.", "종목 등록", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var newItem = new StockHoldingItem
            {
                StockCode = leader.StockCode,
                StockName = leader.StockName,
                Quantity = 0, // 관심종목
                BuyPrice = 0m,
                CurrentPrice = leader.CurrentPrice,
                ChangeRate = leader.ChangeRate,
                FinalScore = leader.FinalScore,
                ChartScore = leader.ChartScore,
                SupplyFinanceScore = leader.SupplyFinanceScore,
                MajorSupplyBadge = leader.SupplyBadge,
                LandmineBadge = leader.LandmineBadge,
                HasCriticalLandmine = leader.HasCriticalLandmine,
                TimingBadge = leader.PrimaryStrategyBadge,
                Memo = $"[AI 시장 알짜 추천] {leader.PrimaryStrategyBadge} 발굴 등록"
            };

            _portfolio.Items.Add(newItem);
            SavePortfolio();
            UpdateUi();

            MessageBox.Show(
                $"[{leader.StockName}] 종목이 내 관심종목에 성공적으로 추가되었습니다!\n\n" +
                $"• 현재가: {leader.CurrentPrice:N0}원 ({leader.ChangeRate:+0.00;-0.00}%)\n" +
                $"• 종합 점수: {leader.FinalScore}점 (A/S급 최우량)\n" +
                $"• 수급 상태: 외인·기관 메이저 매수\n" +
                $"• AI 추천 전략: {leader.PrimaryStrategyBadge}",
                "★ 알짜 종목 관심 등록 완료",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
}
