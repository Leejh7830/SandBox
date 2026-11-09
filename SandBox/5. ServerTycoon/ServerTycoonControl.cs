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
        private TycoonGameState _state;
        private Timer _renderTimer;
        private Timer _logicTimer;
        private Random _rand = new Random();

        // 렌더링 파티클 및 애니메이션 리스트
        private List<FloatingText> _floatingTexts = new List<FloatingText>();
        private List<PacketParticle> _particles = new List<PacketParticle>();
        private List<Shockwave> _shockwaves = new List<Shockwave>();

        private float _fanAngle = 0f;
        private int _frameCounter = 0;
        private int _incidentSpawnCooldown = 20;
        private int _buyMultiplier = 1; // 1, 10, -1(MAX)

        // 7대 하단 서브 탭: 0=서버, 1=전력, 2=냉각, 3=S/W개발, 4=B2B수주, 5=코인거래, 6=연구/환생
        private int _activeSubTab = 0;
        private Rectangle[] _subTabBounds = new Rectangle[7];

        // UI 영역 캐시
        private Rectangle[] _rackBounds = new Rectangle[6];
        private Rectangle _deployButtonRect;
        private Rectangle _feverBarRect;
        private Rectangle _buy1Rect, _buy10Rect, _buyMaxRect;
        private Rectangle _backButtonRect;

        // 아이템 카드 및 구매 버튼 캐시
        private Rectangle[] _cardBounds = new Rectangle[6];
        private Rectangle[] _cardBuyButtonBounds = new Rectangle[6];
        private Rectangle _prestigeActionBtnRect;

        // S/W 스튜디오 인터랙션 영역
        private Rectangle[] _swCategoryBounds = new Rectangle[5];
        private Rectangle _swUxMinusRect, _swUxPlusRect;
        private Rectangle _swAlgoMinusRect, _swAlgoPlusRect;
        private Rectangle _swSecMinusRect, _swSecPlusRect;
        private Rectangle _swStartDevBtnRect;
        private Rectangle _swCrunchBtnRect;
        private Rectangle _swLaunchBtnRect;

        // B2B 수주 인터랙션 영역
        private Rectangle[] _b2bOfferBounds = new Rectangle[3];
        private Rectangle[] _b2bAcceptBtnBounds = new Rectangle[3];
        private Rectangle _b2bCancelBtnRect;
        private Rectangle _b2bRefreshBtnRect;

        // 코인 거래소 인터랙션 영역
        private Rectangle _cryptoHash0Rect, _cryptoHash25Rect, _cryptoHash50Rect, _cryptoHash100Rect;
        private Rectangle _cryptoSell50BtnRect, _cryptoSellAllBtnRect;
        private Rectangle _cryptoBuy1kBtnRect, _cryptoBuyMaxBtnRect;

        // 마우스 호버 인덱스
        private int _hoveredRackIndex = -1;
        private int _hoveredCardIndex = -1;
        private bool _isDeployHovered = false;

        // 오프라인 보상 배너 상태
        private bool _showOfflineBanner = false;
        private string _offlineBannerText = "";
        private Rectangle _offlineBannerRect;
        private Rectangle _offlineClaimButtonRect;

        public ServerTycoonControl()
        {
            InitializeComponent();
            SetStyle(ControlStyles.AllPaintingInWmPaint | 
                     ControlStyles.UserPaint | 
                     ControlStyles.OptimizedDoubleBuffer | 
                     ControlStyles.ResizeRedraw, true);

            _state = TycoonGameState.LoadOrCreate();

            // 1. 오프라인 방치 보상 계산 (기본 2시간 상한 적용)
            double offlineEarned = _state.ProcessOfflineProgress(out double elapsedSecs);
            if (offlineEarned > 0)
            {
                TimeSpan span = TimeSpan.FromSeconds(elapsedSecs);
                string timeStr = span.TotalHours >= 1 
                    ? $"{(int)span.TotalHours}시간 {span.Minutes}분" 
                    : $"{span.Minutes}분 {span.Seconds}초";
                double capHours = _state.GetOfflineCapSeconds() / 3600.0;
                _offlineBannerText = $"[부재 중 서버 운영 보고] 자리 비운 시간: {timeStr} (최대 {capHours:F1}시간 인정) -> +${offlineEarned:N1} 획득!";
                _showOfflineBanner = true;
            }

            // 2. 렌더링 타이머 (30ms, 약 33fps)
            _renderTimer = new Timer { Interval = 30 };
            _renderTimer.Tick += RenderTimer_Tick;

            // 3. 로직 타이머 (1초 틱)
            _logicTimer = new Timer { Interval = 1000 };
            _logicTimer.Tick += LogicTimer_Tick;

            _renderTimer.Start();
            _logicTimer.Start();
        }
        public void OnTabVisibilityChanged(bool isVisible)
        {
            if (isVisible)
            {
                if (_renderTimer != null && !_renderTimer.Enabled) _renderTimer.Start();
                if (_logicTimer != null && !_logicTimer.Enabled) _logicTimer.Start();
                Invalidate();
            }
            else
            {
                if (_renderTimer != null && _renderTimer.Enabled) _renderTimer.Stop();
                if (_logicTimer != null && _logicTimer.Enabled) _logicTimer.Stop();
                if (_state != null) _state.Save();
            }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (this.Visible)
            {
                if (_renderTimer != null && !_renderTimer.Enabled) _renderTimer.Start();
                if (_logicTimer != null && !_logicTimer.Enabled) _logicTimer.Start();
                Invalidate();
            }
            else
            {
                if (_renderTimer != null && _renderTimer.Enabled) _renderTimer.Stop();
                if (_logicTimer != null && _logicTimer.Enabled) _logicTimer.Stop();
                if (_state != null) _state.Save();
            }
        }
        #region Game Loops (Logic & Render)

        private void LogicTimer_Tick(object sender, EventArgs e)
        {
            // 1. 초당 자동 수입 적립 (서버 수익 + S/W 로열티, 코인 채굴 비중 차감)
            double rps = _state.CalculateTotalRps();
            double hashRatio = (_state.CryptoMarket != null) ? (_state.CryptoMarket.MiningHashPercent / 100.0) : 0.0;
            double cashFromServers = rps * (1.0 - hashRatio);
            double royalties = _state.GetSoftwareRoyaltyRps();
            double totalIncome = cashFromServers + royalties;

            if (totalIncome > 0)
            {
                _state.Money += totalIncome;
                _state.CurrentRunEarned += totalIncome;
                _state.TotalEarned += totalIncome;
            }

            // 가상 코인 채굴
            if (hashRatio > 0 && rps > 0 && _state.CryptoMarket != null)
            {
                double mined = (rps * hashRatio) / Math.Max(20.0, _state.CryptoMarket.CurrentPrice * 2.2);
                _state.CryptoMarket.MinedCoins += mined;
                _state.CryptoMarket.TotalMinedCoins += mined;
            }

            // S/W 스튜디오 개발 진척 및 라이선스 감쇄
            if (_state.SoftwareStudio?.CurrentProject != null)
            {
                var prj = _state.SoftwareStudio.CurrentProject;
                if (!prj.IsCompleted)
                {
                    double devSpeed = Math.Max(25.0, rps * 0.45);
                    prj.CurrentPoints = Math.Min(prj.TargetPoints, prj.CurrentPoints + devSpeed);
                }
            }
            if (_state.SoftwareStudio?.ReleasedProducts != null)
            {
                for (int i = _state.SoftwareStudio.ReleasedProducts.Count - 1; i >= 0; i--)
                {
                    var rel = _state.SoftwareStudio.ReleasedProducts[i];
                    rel.RemainingSecs--;
                    rel.TotalEarned += rel.RoyaltyPerSec;
                    if (rel.RemainingSecs <= 0)
                    {
                        _state.SoftwareStudio.ReleasedProducts.RemoveAt(i);
                    }
                }
            }

            // B2B 수주 계약 이행 및 SLA 검증
            if (_state.B2BMarket != null)
            {
                if (_state.B2BMarket.ActiveContract != null)
                {
                    var contract = _state.B2BMarket.ActiveContract;
                    contract.RemainingSecs--;

                    bool violation = false;
                    if (contract.SlaType == SlaRequirementType.ThermalStrict && _state.GetCurrentTemperature() > 45.0)
                        violation = true;
                    else if (contract.SlaType == SlaRequirementType.PowerUptime && _state.GetTotalPowerDemand() > _state.GetTotalPowerSupply())
                        violation = true;
                    else if (contract.SlaType == SlaRequirementType.HighBandwidth && rps < contract.RequiredRps * 0.75)
                        violation = true;

                    if (violation) contract.ViolationsCount++;

                    if (contract.RemainingSecs <= 0)
                    {
                        if (contract.ViolationsCount <= 3)
                        {
                            _state.Money += contract.RewardCash;
                            _state.CurrentRunEarned += contract.RewardCash;
                            _state.TotalEarned += contract.RewardCash;
                            _state.B2BMarket.SuccessfulContractsCount++;
                            _state.B2BMarket.AddReputation(15);
                            AddFloatingText($"★ B2B 납기 완수! +${contract.RewardCash:N0} (신용도 ↑)", Width / 2f, 130, Color.FromArgb(0, 255, 128), true);
                        }
                        else
                        {
                            double penalty = Math.Min(_state.Money, contract.PenaltyCash);
                            _state.Money -= penalty;
                            _state.B2BMarket.AddReputation(-10);
                            AddFloatingText($"▲ SLA 위반 계약 파기! -${penalty:N0}", Width / 2f, 130, Color.FromArgb(255, 82, 82), true);
                        }
                        _state.B2BMarket.ActiveContract = null;
                        _state.B2BMarket.RefreshOffers(_rand, rps);
                    }
                }
                else
                {
                    _state.B2BMarket.RefreshCooldownSecs--;
                    if (_state.B2BMarket.RefreshCooldownSecs <= 0)
                    {
                        _state.B2BMarket.RefreshOffers(_rand, rps);
                    }
                }
            }

            // 가상 코인 시세 및 뉴스 틱
            if (_state.CryptoMarket != null)
            {
                var crypto = _state.CryptoMarket;
                crypto.InitHistory();

                crypto.NewsCooldownSecs--;
                if (crypto.NewsCooldownSecs <= 0)
                {
                    crypto.NewsCooldownSecs = _rand.Next(15, 30);
                    int newsRoll = _rand.Next(0, 5);
                    switch (newsRoll)
                    {
                        case 0:
                            crypto.MarketNews = "[속보] 대형 기관 투자자, ByteCoin 전략 매수 유입!";
                            crypto.TargetPrice = Math.Min(3500.0, crypto.CurrentPrice * (1.20 + _rand.NextDouble() * 0.15));
                            break;
                        case 1:
                            crypto.MarketNews = "[경보] 해외 거래소 점검 루머로 차익 실현 매물 출회!";
                            crypto.TargetPrice = Math.Max(25.0, crypto.CurrentPrice * (0.82 - _rand.NextDouble() * 0.08));
                            break;
                        case 2:
                            crypto.MarketNews = "[호재] 글로벌 클라우드 IDC 협의체, 공식 결제 화폐 채택!";
                            crypto.TargetPrice = Math.Min(3500.0, crypto.CurrentPrice * (1.15 + _rand.NextDouble() * 0.12));
                            break;
                        case 3:
                            crypto.MarketNews = "[조정] 급등세 이후 횡보 및 매물 소화 국면.";
                            crypto.TargetPrice = Math.Max(25.0, crypto.CurrentPrice * 0.92);
                            break;
                        default:
                            crypto.MarketNews = "[동향] 온체인 채굴 해시레이트 견조 유지 중.";
                            crypto.TargetPrice = crypto.CurrentPrice * (0.97 + _rand.NextDouble() * 0.06);
                            break;
                    }
                }

                double diff = crypto.TargetPrice - crypto.CurrentPrice;
                double step = diff * 0.18 + (_rand.NextDouble() - 0.5) * (crypto.CurrentPrice * 0.025);
                crypto.CurrentPrice = Math.Max(15.0, Math.Round(crypto.CurrentPrice + step, 2));

                crypto.PriceHistory.Add(crypto.CurrentPrice);
                if (crypto.PriceHistory.Count > 35) crypto.PriceHistory.RemoveAt(0);

                double firstP = crypto.PriceHistory[0];
                crypto.PriceChangePercent = firstP > 0 ? ((crypto.CurrentPrice - firstP) / firstP) * 100.0 : 0.0;
            }

            // 2. 피버 타이머 감쇄
            if (_state.IsFeverActive)
            {
                _state.FeverRemainingSeconds -= 1.0f;
                if (_state.FeverRemainingSeconds <= 0)
                {
                    _state.IsFeverActive = false;
                    _state.FeverMeter = 0f;
                    AddFloatingText("FEVER 종료", Width / 2f, 180, Color.FromArgb(255, 170, 0), false);
                }
            }
            else if (_state.FeverMeter > 0)
            {
                _state.FeverMeter = Math.Max(0f, _state.FeverMeter - 2.5f);
            }

            // 3. 돌발 장애 이벤트 관리 (AI DevOps Lv.3 이상 시 드론 자동 진압)
            if (_state.CurrentIncident != null)
            {
                _state.CurrentIncident.RemainingSeconds -= 1.0f;

                int aiLvl = _state.GetSkillLevel("skill_ai_devops");
                if (aiLvl >= 3 && _state.CurrentIncident.RemainingSeconds <= 8f)
                {
                    // AI 자동 복구 드론 발동
                    double rewardMult = 1.0 + (aiLvl * 0.30);
                    double reward = Math.Round(_state.CurrentIncident.RewardCash * rewardMult, 0);
                    _state.Money += reward;
                    _state.CurrentRunEarned += reward;
                    _state.TotalEarned += reward;
                    AddFloatingText($"[AI DevOps] 드론 자동 복구! +${reward:N0}", Width / 2f, 120, Color.FromArgb(0, 229, 255), true);
                    _state.CurrentIncident = null;
                }
                else if (_state.CurrentIncident.RemainingSeconds <= 0)
                {
                    _state.CurrentIncident = null;
                }
            }
            else
            {
                _incidentSpawnCooldown--;
                if (_incidentSpawnCooldown <= 0)
                {
                    _incidentSpawnCooldown = _rand.Next(25, 55);
                    int maxUnlocked = 0;
                    for (int i = 0; i < _state.Servers.Count; i++)
                    {
                        if (_state.Servers[i].Level > 0) maxUnlocked = i;
                    }
                    if (_state.GetTotalServerCount() > 0)
                    {
                        _state.CurrentIncident = TycoonIncident.GenerateRandom(maxUnlocked, _rand, rps);
                    }
                }
            }

            // 4. 10초마다 자동 영구 저장
            if (_frameCounter % 10 == 0)
            {
                _state.Save();
            }
        }

        private void RenderTimer_Tick(object sender, EventArgs e)
        {
            _frameCounter++;
            _fanAngle = (_fanAngle + 12f) % 360f;

            // 1. 패킷 파티클 업데이트
            UpdateParticles();

            // 2. 플로팅 텍스트 업데이트
            for (int i = _floatingTexts.Count - 1; i >= 0; i--)
            {
                var ft = _floatingTexts[i];
                ft.Y += ft.VelocityY;
                ft.Alpha -= 0.035f;
                if (ft.Alpha <= 0f)
                {
                    _floatingTexts.RemoveAt(i);
                }
            }

            // 3. 충격파 링 업데이트
            for (int i = _shockwaves.Count - 1; i >= 0; i--)
            {
                var sw = _shockwaves[i];
                sw.Radius += 3.5f;
                sw.Alpha -= 0.05f;
                if (sw.Alpha <= 0f || sw.Radius >= sw.MaxRadius)
                {
                    _shockwaves.RemoveAt(i);
                }
            }

            Invalidate();
        }

        private void UpdateParticles()
        {
            if (_frameCounter % 5 == 0)
            {
                float hubX = Width / 2f;
                float hubY = 195f;

                for (int i = 0; i < _state.Servers.Count; i++)
                {
                    var s = _state.Servers[i];
                    if (s.Level > 0 && i < _rackBounds.Length)
                    {
                        var rect = _rackBounds[i];
                        float startX = rect.X + rect.Width / 2f;
                        float startY = rect.Bottom - 6;

                        if (_rand.Next(0, 100) < Math.Min(95, 30 + s.Level * 4))
                        {
                            _particles.Add(new PacketParticle
                            {
                                StartX = startX,
                                StartY = startY,
                                X = startX,
                                Y = startY,
                                TargetX = hubX,
                                TargetY = hubY,
                                Progress = 0f,
                                Speed = 0.035f + (float)_rand.NextDouble() * 0.025f,
                                ParticleColor = s.AccentColor,
                                ServerIndex = i
                            });
                        }
                    }
                }
            }

            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var p = _particles[i];
                p.Progress += p.Speed;
                p.X = p.StartX + (p.TargetX - p.StartX) * p.Progress;
                p.Y = p.StartY + (p.TargetY - p.StartY) * p.Progress;

                if (p.Progress >= 1f)
                {
                    _particles.RemoveAt(i);
                }
            }

            if (_particles.Count > 100)
            {
                _particles.RemoveRange(0, _particles.Count - 100);
            }
        }

        #endregion
        public string FormatCompactNumber(double value)
        {
            if (value >= 1_000_000_000_000) return $"{value / 1_000_000_000_000:F1}T";
            if (value >= 1_000_000_000) return $"{value / 1_000_000_000:F1}B";
            if (value >= 1_000_000) return $"{value / 1_000_000:F1}M";
            if (value >= 10_000) return $"{value / 1_000:F1}K";
            return $"{value:N0}";
        }

        public void AddFloatingText(string text, float x, float y, Color col, bool isCrit = false)
        {
            if (_floatingTexts.Count > 12)
            {
                _floatingTexts.RemoveAt(0);
            }

            _floatingTexts.Add(new FloatingText
            {
                Text = text,
                X = x - 20,
                Y = y - 10,
                VelocityY = -1.8f,
                Alpha = 1.0f,
                TextColor = col,
                IsCrit = isCrit
            });
        }
    }
}
