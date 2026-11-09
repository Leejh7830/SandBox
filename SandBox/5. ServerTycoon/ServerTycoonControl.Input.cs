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
        #region User Interaction (Mouse Events)

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Point pt = e.Location;

            int prevRack = _hoveredRackIndex;
            _hoveredRackIndex = -1;
            for (int i = 0; i < _rackBounds.Length; i++)
            {
                if (_rackBounds[i].Contains(pt))
                {
                    _hoveredRackIndex = i;
                    break;
                }
            }

            int prevCard = _hoveredCardIndex;
            _hoveredCardIndex = -1;
            for (int i = 0; i < _cardBounds.Length; i++)
            {
                if (_cardBounds[i].Contains(pt))
                {
                    _hoveredCardIndex = i;
                    break;
                }
            }

            bool prevDeploy = _isDeployHovered;
            _isDeployHovered = _deployButtonRect.Contains(pt);

            if (prevRack != _hoveredRackIndex || prevCard != _hoveredCardIndex || prevDeploy != _isDeployHovered)
            {
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Point pt = e.Location;

            // 1. 오프라인 보상 수령
            if (_showOfflineBanner && _offlineClaimButtonRect.Contains(pt))
            {
                _showOfflineBanner = false;
                Invalidate();
                return;
            }

            // 2. 뒤로가기 버튼 [◀ SandBox]
            if (_backButtonRect.Contains(pt))
            {
                if (this.ParentForm is SandBox sb)
                {
                    sb.BackToList();
                }
                return;
            }

            // 3. 서브 탭 전환 [서버실, 전력실, 냉각실, 연구/환생]
            for (int i = 0; i < _subTabBounds.Length; i++)
            {
                if (_subTabBounds[i].Contains(pt))
                {
                    _activeSubTab = i;
                    Invalidate();
                    return;
                }
            }

            // 4. 구매 배수 토글 버튼 [x1], [x10], [MAX] (탭 0, 1, 2)
            if (_activeSubTab < 3)
            {
                if (_buy1Rect.Contains(pt)) { _buyMultiplier = 1; Invalidate(); return; }
                if (_buy10Rect.Contains(pt)) { _buyMultiplier = 10; Invalidate(); return; }
                if (_buyMaxRect.Contains(pt)) { _buyMultiplier = -1; Invalidate(); return; }
            }

            // 5. 돌발 이벤트 배너/랙 클릭
            if (_state.CurrentIncident != null)
            {
                int target = _state.CurrentIncident.TargetServerIndex;
                if (target >= 0 && target < _rackBounds.Length)
                {
                    var rackRect = _rackBounds[target];
                    Rectangle clickArea = new Rectangle(rackRect.X - 10, rackRect.Y - 40, rackRect.Width + 20, rackRect.Height + 50);
                    if (clickArea.Contains(pt))
                    {
                        double reward = _state.CurrentIncident.RewardCash;
                        int aiLvl = _state.GetSkillLevel("skill_ai_devops");
                        if (aiLvl > 0) reward = Math.Round(reward * (1.0 + (aiLvl * 0.30)), 0);
                        _state.Money += reward;
                        _state.TotalEarned += reward;
                        _state.CurrentRunEarned += reward;
                        AddFloatingText($"장애 조치 완료! +${reward:N0}", pt.X, pt.Y, Color.FromArgb(0, 255, 128), true);
                        _state.CurrentIncident = null;
                        Invalidate();
                        return;
                    }
                }
            }

            // 6. 메인 [ 🚀 PING / DEPLOY ] 버튼 클릭
            if (_deployButtonRect.Contains(pt))
            {
                PerformClickAction(pt.X, pt.Y);
                return;
            }

            // 7. 하단 탭별 아이템 구매 및 연구 액션
            switch (_activeSubTab)
            {
                case 0: // 서버 구매
                    for (int i = 0; i < _cardBuyButtonBounds.Length; i++)
                    {
                        if (_cardBuyButtonBounds[i].Contains(pt))
                        {
                            PerformBuyServer(i, pt);
                            return;
                        }
                    }
                    break;
                case 1: // 전력 설비 구매
                    for (int i = 0; i < _cardBuyButtonBounds.Length; i++)
                    {
                        if (_cardBuyButtonBounds[i].Contains(pt))
                        {
                            PerformBuyPower(i, pt);
                            return;
                        }
                    }
                    break;
                case 2: // 냉각 설비 구매
                    for (int i = 0; i < _cardBuyButtonBounds.Length; i++)
                    {
                        if (_cardBuyButtonBounds[i].Contains(pt))
                        {
                            PerformBuyCooling(i, pt);
                            return;
                        }
                    }
                    break;
                case 3: // S/W 스튜디오 인터랙션
                    HandleSoftwareStudioClick(pt);
                    break;
                case 4: // B2B 수주 인터랙션
                    HandleB2BContractsClick(pt);
                    break;
                case 5: // 코인 거래소 인터랙션
                    HandleCryptoMarketClick(pt);
                    break;
                case 6: // 기술 연구 및 환생
                    if (_prestigeActionBtnRect.Contains(pt))
                    {
                        PerformPrestige();
                        return;
                    }
                    var skills = TycoonGameState.GetSkillDefinitions();
                    for (int i = 0; i < skills.Count && i < _cardBuyButtonBounds.Length; i++)
                    {
                        if (_cardBuyButtonBounds[i].Contains(pt))
                        {
                            PerformUnlockSkill(skills[i], pt);
                            return;
                        }
                    }
                    break;
            }

            // 8. 서버실 랙 클릭 시에도 가벼운 핑 생성
            for (int i = 0; i < _rackBounds.Length; i++)
            {
                if (_rackBounds[i].Contains(pt))
                {
                    PerformClickAction(pt.X, pt.Y);
                    return;
                }
            }
        }

        private void PerformClickAction(int clickX, int clickY)
        {
            _state.TotalClicks++;
            double power = _state.GetClickPower();

            bool isJackpot = false;
            int critLvl = _state.GetSkillLevel("skill_crit_packet");
            if (critLvl > 0)
            {
                int jackpotChance = 5 + (critLvl * 4); // Lv1=9%, Lv2=13%, Lv3=17%, Lv4=21%, Lv5=25%
                if (_rand.Next(0, 100) < jackpotChance)
                {
                    double mult = 3.0 + (critLvl * 1.0); // Lv1=4x, Lv2=5x, Lv3=6x, Lv4=7x, Lv5=8x
                    power *= mult;
                    isJackpot = true;
                }
                else if (_rand.Next(0, 100) < 15)
                {
                    power *= 2.0;
                }
            }
            else if (_rand.Next(0, 100) < 15)
            {
                power *= 2.0;
            }

            _state.Money += power;
            _state.TotalEarned += power;
            _state.CurrentRunEarned += power;

            if (!_state.IsFeverActive)
            {
                _state.FeverMeter += 4.5f;
                if (_state.FeverMeter >= 100f)
                {
                    _state.FeverMeter = 100f;
                    _state.IsFeverActive = true;
                    _state.FeverRemainingSeconds = 10f;
                    AddFloatingText("★ FEVER OVERDRIVE!", Width / 2f, 150, Color.FromArgb(255, 60, 0), true);
                }
            }

            string text = isJackpot ? $"★ JACKPOT! +${power:N1}" : (power > _state.GetClickPower() ? $"CRIT! +${power:N1}" : $"+${power:N1}");
            Color col = isJackpot ? Color.FromArgb(255, 80, 220) : (power > _state.GetClickPower() ? Color.FromArgb(255, 230, 0) : Color.FromArgb(0, 229, 255));
            AddFloatingText(text, clickX, clickY, col, isJackpot || power > _state.GetClickPower());

            _shockwaves.Add(new Shockwave
            {
                X = clickX,
                Y = clickY,
                Radius = 5f,
                MaxRadius = isJackpot ? 60f : 45f,
                Alpha = 1.0f,
                WaveColor = col
            });

            Invalidate();
        }

        private void PerformBuyServer(int index, Point clickPt)
        {
            if (index < 0 || index >= _state.Servers.Count) return;
            var server = _state.Servers[index];

            int count = _buyMultiplier == -1 ? server.GetMaxAffordable(_state.Money) : _buyMultiplier;
            double cost = server.GetNextCost(count);

            if (_state.Money >= cost && count > 0)
            {
                _state.Money -= cost;
                server.Level += count;
                _state.Save();

                AddFloatingText($"+{count} {server.Name}!", clickPt.X, clickPt.Y, server.AccentColor, true);

                _shockwaves.Add(new Shockwave
                {
                    X = clickPt.X,
                    Y = clickPt.Y,
                    Radius = 6f,
                    MaxRadius = 45f,
                    Alpha = 1.0f,
                    WaveColor = server.AccentColor
                });

                Invalidate();
            }
            else
            {
                AddFloatingText("자금 부족!", clickPt.X, clickPt.Y, Color.FromArgb(255, 80, 80), false);
            }
        }

        private void PerformBuyPower(int index, Point clickPt)
        {
            if (index < 0 || index >= _state.PowerFacilities.Count) return;
            var power = _state.PowerFacilities[index];
            if (power.BaseCost <= 0) return;

            int count = _buyMultiplier == -1 ? power.GetMaxAffordable(_state.Money) : _buyMultiplier;
            double cost = power.GetNextCost(count);

            if (_state.Money >= cost && count > 0)
            {
                _state.Money -= cost;
                power.Level += count;
                _state.Save();

                AddFloatingText($"+{count} {power.Name} 증설!", clickPt.X, clickPt.Y, power.AccentColor, true);
                Invalidate();
            }
            else
            {
                AddFloatingText("자금 부족!", clickPt.X, clickPt.Y, Color.FromArgb(255, 80, 80), false);
            }
        }

        private void PerformBuyCooling(int index, Point clickPt)
        {
            if (index < 0 || index >= _state.CoolingFacilities.Count) return;
            var cooling = _state.CoolingFacilities[index];
            if (cooling.BaseCost <= 0) return;

            int count = _buyMultiplier == -1 ? cooling.GetMaxAffordable(_state.Money) : _buyMultiplier;
            double cost = cooling.GetNextCost(count);

            if (_state.Money >= cost && count > 0)
            {
                _state.Money -= cost;
                cooling.Level += count;
                _state.Save();

                AddFloatingText($"+{count} {cooling.Name} 가동!", clickPt.X, clickPt.Y, cooling.AccentColor, true);
                Invalidate();
            }
            else
            {
                AddFloatingText("자금 부족!", clickPt.X, clickPt.Y, Color.FromArgb(255, 80, 80), false);
            }
        }

        private void PerformUnlockSkill(SkillNode skill, Point clickPt)
        {
            int currentLvl = _state.GetSkillLevel(skill.Id);
            if (currentLvl >= skill.MaxLevel) return;

            int cost = skill.GetCostForLevel(currentLvl);
            if (_state.ResearchTokens >= cost)
            {
                _state.ResearchTokens -= cost;
                _state.SetSkillLevel(skill.Id, currentLvl + 1);
                _state.Save();

                AddFloatingText($"◈ {skill.Name} Lv.{currentLvl + 1} 업그레이드!", clickPt.X, clickPt.Y, Color.FromArgb(0, 230, 118), true);
                Invalidate();
            }
            else
            {
                AddFloatingText($"연구 토큰 부족! (◈ {cost}개 필요)", clickPt.X, clickPt.Y, Color.FromArgb(255, 80, 80), false);
            }
        }

        private void PerformPrestige()
        {
            int tokens = _state.CalculatePrestigeTokens();
            if (tokens < 1)
            {
                double remain = Math.Max(0, 1000000.0 - _state.CurrentRunEarned);
                MessageBox.Show(
                    $"이번 회차 수익이 부족하여 클라우드 마이그레이션이 불가합니다.\n\n" +
                    $"• 현재 회차 수익: ${FormatCompactNumber(_state.CurrentRunEarned)}\n" +
                    $"• 첫 연구 토큰 필요액: $1,000,000 (남은 금액: ${FormatCompactNumber(remain)})\n\n" +
                    $"IDC 서버 인프라를 더 육성한 후 다시 시도하세요!", 
                    "Server Farm Tycoon", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            double nextMult = 1.0 + ((_state.PrestigeCount + 1) * 0.15) + ((_state.TotalTokensEarned + tokens) * 0.015);
            var res = MessageBox.Show(
                $"[클라우드 마이그레이션 - 환생]\n\n" +
                $"현재 보유 자금과 서버/전력/냉각 인프라가 초기화되는 대신,\n" +
                $"다음 혜택을 영구히 획득합니다:\n\n" +
                $"1. 영구 글로벌 생산 배율: x{_state.GetPrestigeMultiplier():F2} -> x{nextMult:F2}\n" +
                $"2. 신규 IDC 연구 토큰: ◈ +{tokens}개 영구 획득\n\n" +
                $"※ 연구실의 스킬 레벨은 환생해도 영구 보존됩니다.\n" +
                $"마이그레이션을 진행하시겠습니까?",
                "Server Farm Tycoon", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (res == DialogResult.Yes)
            {
                _state.PrestigeCount++;
                _state.ResearchTokens += tokens;
                _state.TotalTokensEarned += tokens;
                _state.Money = 50.0;
                _state.CurrentRunEarned = 50.0;
                _state.Servers = TycoonGameState.CreateDefaultServers();
                _state.PowerFacilities = TycoonGameState.CreateDefaultPower();
                _state.CoolingFacilities = TycoonGameState.CreateDefaultCooling();
                _state.Save();

                AddFloatingText($"[마이그레이션 완료] ◈ +{tokens} 토큰 획득!", Width / 2f, 150, Color.FromArgb(179, 136, 255), true);
                Invalidate();
            }
        }

        #endregion
    }
}
