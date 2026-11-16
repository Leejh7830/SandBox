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
        private bool _isTopPickBannerCollapsed = true; // [사용자 요청] 기본 상태 접힘으로 시작
        private bool _isScanningMarketTopPicks = false;

        // 알짜 종목 포착 시 시각적 네온 깜빡임 타이머 (Zero-Resource: 필요 시에만 가동, 펼치면 즉시 정지)
        private Timer _topPickBlinkTimer;
        private bool _blinkToggleState = false;
        private int _discoveredTopPickCount = 0;

        // Zero-Resource: OnPaint 내 GC 압박 0화를 위한 정적 캐시 펜
        private static readonly Pen PenTopPickBannerBorder = new Pen(Color.FromArgb(38, 48, 68), 1.2f);
        private static readonly Pen PenTopPickEmptyBorder = new Pen(Color.FromArgb(50, 62, 85), 1f);
        private static readonly Pen PenTopPickRank1Border = new Pen(Color.FromArgb(255, 215, 0), 1.2f);
        private static readonly Pen PenTopPickRankOtherBorder = new Pen(Color.FromArgb(0, 216, 255), 1.2f);

        private void InitTopPickBanner()
        {
            _pnlTopPickBanner = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36, // 기본 접힘 높이 (36px)
                BackColor = Color.FromArgb(18, 24, 34),
                Padding = new Padding(12, 5, 12, 5)
            };

            // 커스텀 하단 테두리 페인팅 (정적 캐시 Pen 재사용)
            _pnlTopPickBanner.Paint += (s, e) =>
            {
                e.Graphics.DrawLine(PenTopPickBannerBorder, 0, _pnlTopPickBanner.Height - 1, _pnlTopPickBanner.Width, _pnlTopPickBanner.Height - 1);
            };

            // 좌측 헤더 영역 (타이틀 + 부제목)
            var pnlLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = 480, // 접힌 상태에서도 타이틀 + 부제목이 1행에 나란히 보일 수 있도록 충분한 폭 확보
                BackColor = Color.Transparent
            };

            _lblTopPickTitle = new Label
            {
                Text = "★ 오늘의 시장 알짜 추천주",
                Font = new Font("Malgun Gothic", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 215, 0),
                Location = new Point(2, 6),
                AutoSize = true
            };

            _lblTopPickSummary = new Label
            {
                Text = "전체 시장 스캔 준비 중...",
                Font = new Font("Segoe UI", 8.2f),
                ForeColor = Color.FromArgb(140, 160, 185),
                Location = new Point(190, 7), // 접힌 상태(36px)에서 타이틀 우측에 깔끔하게 인라인 배치
                AutoSize = true
            };

            pnlLeft.Controls.Add(_lblTopPickTitle);
            pnlLeft.Controls.Add(_lblTopPickSummary);

            // 우측 접기/펼치기 토글 버튼 (글자 잘림 원천 해결: 너비 102px 확보)
            _btnToggleTopPickBanner = new Button
            {
                Text = "▼ 펼치기",
                Size = new Size(102, 26),
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(26, 34, 48),
                ForeColor = Color.FromArgb(160, 180, 205),
                Font = new Font("Malgun Gothic", 8.0f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnToggleTopPickBanner.FlatAppearance.BorderColor = Color.FromArgb(48, 60, 80);
            _btnToggleTopPickBanner.Click += (s, e) =>
            {
                ToggleTopPickBannerState();
            };

            // 알짜 종목 포착 시 점멸 타이머 초기화 (Zero-Resource: 650ms 주기, 유휴 시 가동 안 함)
            _topPickBlinkTimer = new Timer
            {
                Interval = 650
            };
            _topPickBlinkTimer.Tick += (s, e) =>
            {
                if (_isTopPickBannerCollapsed && _discoveredTopPickCount > 0)
                {
                    _blinkToggleState = !_blinkToggleState;
                    if (_blinkToggleState)
                    {
                        // 1. 네온 하이라이트 상태 (골드/에메랄드)
                        _btnToggleTopPickBanner.BackColor = Color.FromArgb(255, 193, 7);
                        _btnToggleTopPickBanner.ForeColor = Color.FromArgb(15, 20, 25);
                        _btnToggleTopPickBanner.FlatAppearance.BorderColor = Color.FromArgb(255, 235, 59);
                    }
                    else
                    {
                        // 2. 딥 에메랄드 상태
                        _btnToggleTopPickBanner.BackColor = Color.FromArgb(0, 140, 95);
                        _btnToggleTopPickBanner.ForeColor = Color.White;
                        _btnToggleTopPickBanner.FlatAppearance.BorderColor = Color.FromArgb(0, 230, 118);
                    }
                }
                else
                {
                    _topPickBlinkTimer.Stop();
                    ResetToggleButtonAppearance();
                }
            };

            // 중앙 카드 컨테이너 (기본 접힘 상태이므로 숨김)
            _flpTopPickCards = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = false,
                Visible = false,
                BackColor = Color.Transparent,
                Padding = new Padding(8, 2, 8, 2)
            };

            _pnlTopPickBanner.Controls.Add(_flpTopPickCards);
            _pnlTopPickBanner.Controls.Add(pnlLeft);
            _pnlTopPickBanner.Controls.Add(_btnToggleTopPickBanner);

            // 최초 시장 전체 알짜 스캔 비동기 가동
            _ = RefreshMarketTopPicksAsync(forceRefresh: false);
        }

        private void ToggleTopPickBannerState()
        {
            _isTopPickBannerCollapsed = !_isTopPickBannerCollapsed;
            if (_isTopPickBannerCollapsed)
            {
                // 접기
                _pnlTopPickBanner.Height = 36;
                _flpTopPickCards.Visible = false;
                _lblTopPickSummary.Location = new Point(190, 7); // 인라인 배치
                
                // 종목이 있으면 접힌 상태에서 다시 깜빡임 시작
                if (_discoveredTopPickCount > 0)
                {
                    _btnToggleTopPickBanner.Text = $"▼ 알짜 {_discoveredTopPickCount}개!";
                    _topPickBlinkTimer?.Start();
                }
                else
                {
                    _btnToggleTopPickBanner.Text = "▼ 펼치기";
                    ResetToggleButtonAppearance();
                }
            }
            else
            {
                // 펼치기: 펼치면 깜빡임 즉시 중지 (Zero-Resource)
                _topPickBlinkTimer?.Stop();
                _pnlTopPickBanner.Height = 84;
                _btnToggleTopPickBanner.Text = "▲ 접기";
                ResetToggleButtonAppearance();

                _flpTopPickCards.Visible = true;
                _lblTopPickSummary.Location = new Point(4, 30); // 펼쳐졌을 때는 타이틀 아래 2행 배치
            }
        }

        private void ResetToggleButtonAppearance()
        {
            if (_btnToggleTopPickBanner == null || _btnToggleTopPickBanner.IsDisposed) return;
            _btnToggleTopPickBanner.BackColor = Color.FromArgb(26, 34, 48);
            _btnToggleTopPickBanner.ForeColor = Color.FromArgb(160, 180, 205);
            _btnToggleTopPickBanner.FlatAppearance.BorderColor = Color.FromArgb(48, 60, 80);
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

            _discoveredTopPickCount = (topPicks != null) ? topPicks.Count : 0;

            _flpTopPickCards.SuspendLayout();
            _flpTopPickCards.Controls.Clear();

            if (topPicks == null || topPicks.Count == 0)
            {
                // [0개인 경우]: 억지로 추천하지 않고 정직하게 시장 전체 현금 관망 안내
                _lblTopPickSummary.Text = "시장 전체 엄격 통과: 0종목 (현금 관망)";
                _lblTopPickSummary.ForeColor = Color.FromArgb(140, 160, 185);

                _topPickBlinkTimer?.Stop();
                if (_isTopPickBannerCollapsed)
                {
                    _btnToggleTopPickBanner.Text = "▼ 펼치기";
                    ResetToggleButtonAppearance();
                }

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
                    e.Graphics.DrawRectangle(PenTopPickEmptyBorder, 0, 0, pnlEmpty.Width - 1, pnlEmpty.Height - 1);
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
                // [1~3개인 경우]: 최우량 알짜 카드 렌더링 + 접힌 상태라면 버튼 색상 변경 및 깜빡임 가동!
                _lblTopPickSummary.Text = $"시장 전체 엄격 통과: {topPicks.Count}종목 포착!";
                _lblTopPickSummary.ForeColor = Color.FromArgb(0, 230, 118);

                if (_isTopPickBannerCollapsed)
                {
                    _btnToggleTopPickBanner.Text = $"▼ 알짜 {topPicks.Count}개!";
                    _topPickBlinkTimer?.Start();
                }

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

            // 카드 외곽 테두리 페인팅 (1위는 골드, 2~3위는 청록 네온 - 정적 캐시 Pen 재사용)
            Pen cardPen = (rank == 1) ? PenTopPickRank1Border : PenTopPickRankOtherBorder;
            pnlCard.Paint += (s, e) =>
            {
                e.Graphics.DrawRectangle(cardPen, 0, 0, pnlCard.Width - 1, pnlCard.Height - 1);
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
                ToastNotification.Show($"[{leader.StockName}] 종목은 이미 관심/보유 목록에 등록되어 있습니다.", "알림", ToastIcon.Info);
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

            // 모달 팝업 대신 3초 뒤 자동으로 사라지는 세련된 우측 하단 토스트 알림 연동
            string toastMsg = $"[{leader.StockName}] 관심종목 등록 완료!\n• 현재가: {leader.CurrentPrice:N0}원 ({leader.ChangeRate:+0.00;-0.00}%)\n• 종합점수: {leader.FinalScore}점 ({leader.PrimaryStrategyBadge})";
            ToastNotification.Show(toastMsg, "★ 알짜 종목 등록", ToastIcon.Success, autoCloseMs: 3000);
        }
    }
}
