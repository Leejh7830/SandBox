using System;
using System.Drawing;

namespace GateHelper.StockAdvisor.Models
{
    /// <summary>
    /// 최종 투자 행동 판정 상세 모델
    /// </summary>
    public class StockVerdictInfo
    {
        public ActionVerdictType Type { get; set; } = ActionVerdictType.HoldOrWatch;
        public string BadgeText { get; set; } = "관망";
        public string TitleText { get; set; } = "[ AI 참모 최종 결론 : 관망 및 지지선 확인 대기 ]";
        public string GuideText { get; set; } = "추세 방향성 탐색 중. 20일 지지선 안착 확인 전까지 진입 보류";
        public string BuyPriceText { get; set; } = "• 매수가: [관망 대기] (1차 -원 | 2차 -원)";
        public Color BackColor { get; set; } = Color.FromArgb(36, 32, 20);
        public Color ForeColor { get; set; } = Color.FromArgb(255, 220, 90);
        public Color BorderColor { get; set; } = Color.FromArgb(160, 130, 40);
        public Color ListBadgeColor { get; set; } = Color.FromArgb(240, 200, 80);
        public bool CanBuy { get; set; } = false;
        public bool IsEmergency { get; set; } = false;
        public string EmergencyReason { get; set; } = string.Empty;
    }

    /// <summary>
    /// 객관적 정량 지표와 AI 진단을 종합하여 단 1초 만에 명쾌한 결론을 내려주는 헬퍼
    /// </summary>
    public static class StockVerdictHelper
    {
        public static StockVerdictInfo Evaluate(StockHoldingItem item)
        {
            if (item == null)
            {
                return new StockVerdictInfo
                {
                    Type = ActionVerdictType.HoldOrWatch,
                    BadgeText = "선택대기",
                    TitleText = "[ AI 참모 최종 결론 : 좌측 종목을 선택하세요 ]",
                    GuideText = "종목을 선택하시면 단일화된 최종 행동 결론이 표시됩니다.",
                    BuyPriceText = "• 매수가: - 원",
                    BackColor = Color.FromArgb(24, 28, 38),
                    ForeColor = Color.FromArgb(170, 185, 205),
                    BorderColor = Color.FromArgb(48, 56, 74),
                    ListBadgeColor = Color.FromArgb(140, 155, 175),
                    CanBuy = false
                };
            }

            var info = new StockVerdictInfo();

            // 1. 위급 종목 판단 (당일 -5% 급락 또는 손절가 이탈)
            decimal stopPrice = item.TargetStopPrice > 0 ? item.TargetStopPrice : (item.SupportPrice > 0 ? item.SupportPrice * 0.97m : item.CurrentPrice * 0.93m);
            if (item.ChangeRate <= -5.0m)
            {
                info.IsEmergency = true;
                info.EmergencyReason = $"당일 급락({item.ChangeRate:+0.00;-0.00}%)";
            }
            else if (stopPrice > 0 && item.CurrentPrice > 0 && item.CurrentPrice <= stopPrice)
            {
                info.IsEmergency = true;
                info.EmergencyReason = $"손절가({stopPrice:N0}원) 터치/이탈";
            }

            // 단일 권고 매수가 기초 문자열 구성 (3단 구조 심플화: 불필요한 상시 2차 매수가 제거)
            string buy1Str = item.RecommendedBuyPrice > 0 ? $"{item.RecommendedBuyPrice:N0}원" : (item.CurrentPrice > 0 ? $"{item.CurrentPrice:N0}원" : "-");

            // [추매 이벤트 조건] 보유 중이고 현재가가 지지선에서 하락을 멈추고 지지 반등(수급 양호, 지뢰 없음, 당일 양봉/보합)할 때만 2차 추매 정보 활성화
            bool isSecondaryBuyTriggered = item.Quantity > 0 && !item.HasCriticalLandmine && item.ChangeRate >= -0.5m &&
                                           item.CurrentPrice <= item.BuyPrice * 0.96m && item.CurrentPrice > stopPrice;
            string secBuyAppend = isSecondaryBuyTriggered ? $"   [★지지선 반등 확인! 2차추매 가능: {item.CurrentPrice:N0}원]" : "";

            // 2. 판정 로직 분기
            // [급락 경보 인터락] 당일 -4.0%를 초과하여 급락 중인 종목은 지지선 붕괴 위험이 크므로 매수 배제 및 급락주의 경보
            bool isSharpDrop = item.ChangeRate <= -4.0m;
            bool isHeld = item.Quantity > 0;
            bool isStopBreached = isHeld && stopPrice > 0 && item.CurrentPrice > 0 && item.CurrentPrice <= stopPrice;

            // 과매도 반등: RSI 36.5 이하 침체권 + 손익비/안전 우수(RiskScore >= 7) + 악재/지뢰 없음 + 단기 급락(-4% 초과) 아님
            bool isOversold = item.Rsi <= 36.5m && item.Rsi > 0m && item.RiskScore >= 7 && !item.HasCriticalLandmine && !isSharpDrop;

            // [매수 승인 판정 우선 계산] 종합 6점 이상(B등급 이상)에 지뢰가 없고 급락이 아닐 때
            // (과매도 반등 요건 충족 시 역배열로 인한 차트 5점 캡 예외 인정하여 레이더와 100% 일치)
            bool isBuy = (item.Grade == PositionGrade.A || item.FinalScore >= 7 ||
                         (item.FinalScore >= 6 && ((item.ChartScore >= 6 && item.SupplyFinanceScore >= 5) || isOversold)))
                         && !item.HasCriticalLandmine && !isSharpDrop;

            // [회피/비중축소 인터락]
            // 1. 종합 4점 이하(F등급 및 하위 C등급), 치명적 지뢰 발생, 또는 보유 종목 손절 방어선 실제 이탈 시
            // 2. 6점 미만 종목에서 AI 심층진단이 명시적 손절/축소/경보를 권고한 경우
            // (지뢰가 없는 5점 중립 C등급 종목은 빨간 경고 대신 노란색 [관망]으로 합리적 분류)
            bool isAvoid = (item.Grade == PositionGrade.F || item.FinalScore <= 4 || item.HasCriticalLandmine || isStopBreached) ||
                           (item.FinalScore < 6 && item.LastDiagnosis != null &&
                           (item.LastDiagnosis.RecommendedAction.Contains("손절") || item.LastDiagnosis.RecommendedAction.Contains("축소") || item.LastDiagnosis.RecommendedAction.Contains("경보")));

            bool isOverheat = item.Rsi >= 68.0m || (item.TimingBadge != null && item.TimingBadge.Contains("과열"));

            if (isSharpDrop)
            {
                info.Type = ActionVerdictType.SharpDropAlert;
                if (isHeld)
                {
                    info.BadgeText = "급락주의";
                    info.TitleText = "[ AI 참모 최종 결론 : 당일 급락 발생! 성급한 물타기 금지 ]";
                    info.GuideText = $"당일 등락률({item.ChangeRate:+0.00;-0.00}%) 급락 발생. 하방 지지선 안착 확인 전까지 추가 매수 보류 및 관망 권고";
                    info.BuyPriceText = $"• 매수가: [진입 보류] (당일 급락 {item.ChangeRate:+0.00;-0.00}%로 섣부른 물타기 금지)";
                }
                else
                {
                    info.BadgeText = "급락주의";
                    info.TitleText = "[ AI 참모 최종 결론 : 당일 급락! 떨어지는 칼날 진입 금지 ]";
                    info.GuideText = $"당일 {item.ChangeRate:+0.00;-0.00}% 급락 중. 바닥 지지 확인 전까지 섣부른 역발상 매수를 엄격히 금지합니다.";
                    info.BuyPriceText = $"• 매수가: [진입 보류] (당일 -4% 초과 급락으로 하방 지지선 확인 전 진입 불가)";
                }
                info.BackColor = Color.FromArgb(46, 26, 16);
                info.ForeColor = Color.FromArgb(255, 175, 75);
                info.BorderColor = Color.FromArgb(220, 120, 30);
                info.ListBadgeColor = Color.FromArgb(255, 160, 60);
                info.CanBuy = false;
            }
            else if (isAvoid)
            {
                info.Type = ActionVerdictType.AvoidOrReduce;
                if (isHeld)
                {
                    info.BadgeText = "비중축소";
                    info.TitleText = "[ AI 참모 최종 결론 : 반등 시 비중 축소 / 손절 권고 ]";
                    if (item.LastDiagnosis?.PolicyImpact < 0)
                    {
                        info.GuideText = $"정책/규제 악재({item.LastDiagnosis.PolicyImpact}점) 및 역배열 지속. 추가 손실 방지를 위해 반등 시 비중 40~50% 축소 권고";
                    }
                    else if (item.HasCriticalLandmine)
                    {
                        info.GuideText = "재무/공시 지뢰(적자/CB 등) 탐지! 손실 고착화 방지를 위해 조속한 비중 축소 및 리스크 정리 권고";
                    }
                    else if (isStopBreached)
                    {
                        info.GuideText = $"현재가가 손절 방어선({stopPrice:N0}원)을 이탈했습니다! 추가 하락 방지를 위해 원칙에 따른 비중 축소/손절 권고";
                    }
                    else
                    {
                        info.GuideText = "하락 추세 지속. 추가 하락에 대비하여 기술적 반등 구간에서 단계적 비중 축소 권고";
                    }
                    info.BuyPriceText = $"• 매수가: [매수 불가] (종합 {item.Grade}등급으로 추가 매수 절대 금지)";
                }
                else
                {
                    info.BadgeText = "매수금지";
                    info.TitleText = "[ AI 참모 최종 결론 : 신규 매수 금지 (리스크 경보) ]";
                    if (item.LastDiagnosis?.PolicyImpact < 0)
                    {
                        info.GuideText = $"정책/규제 악재({item.LastDiagnosis.PolicyImpact}점) 및 역배열 지속. 신규 진입 절대 금지";
                    }
                    else if (item.HasCriticalLandmine)
                    {
                        info.GuideText = "재무/공시 지뢰(적자/CB 등) 탐지! 신규 진입 절대 금지 및 리스크 주의";
                    }
                    else
                    {
                        info.GuideText = "하락 추세 및 불확실성 지속. 신규 매수를 금지하며 안전한 지지선 형성 대기";
                    }
                    info.BuyPriceText = $"• 매수가: [진입 금지] (종합 {item.Grade}등급으로 매수 불가)";
                }

                info.BackColor = Color.FromArgb(48, 18, 26);
                info.ForeColor = Color.FromArgb(255, 120, 130);
                info.BorderColor = Color.FromArgb(200, 50, 70);
                info.ListBadgeColor = Color.FromArgb(255, 95, 110);
                info.CanBuy = false;
            }
            else if (isOverheat)
            {
                if (isHeld)
                {
                    info.Type = ActionVerdictType.TakeProfit;
                    info.BadgeText = "과열주의";
                    info.TitleText = "[ AI 참모 최종 결론 : 단기 과열 구간 / 분할 익절 및 수익 확정 권고 ]";
                    info.GuideText = "단기 과열 슈팅 구간! 욕심을 줄이고 보유 물량의 30~50%를 분할 매도하여 수익을 확정하세요.";
                    info.BuyPriceText = $"• 매수가: [추격 금지] (20일 눌림목 대기선: {buy1Str} 도달 시에만 검토)";
                    info.BackColor = Color.FromArgb(16, 32, 54);
                    info.ForeColor = Color.FromArgb(100, 215, 255);
                    info.BorderColor = Color.FromArgb(50, 130, 210);
                    info.ListBadgeColor = Color.FromArgb(70, 190, 255);
                }
                else
                {
                    // [사용자 요청] 미보유 종목의 단기 과열 시 타이밍 안전벨트는 '과열(추격금지)', 최종 액션은 '관망'으로 통일
                    info.Type = ActionVerdictType.HoldOrWatch;
                    info.BadgeText = "관망";
                    info.TitleText = "[ AI 참모 최종 결론 : 단기 과열 구간 / 추격 매수 금지 및 관망 ]";
                    info.GuideText = "단기 과열 슈팅 구간! 지금 사면 상투 물림 위험. 20일선 눌림목 도달 시까지 관망 유지";
                    info.BuyPriceText = $"• 매수가: [추격 금지] (20일 눌림목 대기선: {buy1Str} 도달 시에만 검토)";
                    info.BackColor = Color.FromArgb(36, 32, 20);
                    info.ForeColor = Color.FromArgb(255, 220, 90);
                    info.BorderColor = Color.FromArgb(170, 140, 40);
                    info.ListBadgeColor = Color.FromArgb(240, 200, 80);
                }
                info.CanBuy = false;
            }
            else if (isBuy)
            {
                if (isOversold)
                {
                    info.Type = ActionVerdictType.OversoldBounce;
                    if (isHeld)
                    {
                        // [보유 종목 과매도 추매 인터락]
                        // RSI 과매도 구간이라도 당일 주가가 음봉 하락 중(ChangeRate < -0.5%)이면 바닥 확인 전이므로
                        // 떨어지는 칼날 물타기를 방지하고 [관망]으로 유지
                        bool isHoldingSupportConfirmed = item.ChangeRate >= -0.5m && item.CurrentPrice > stopPrice;

                        if (!isHoldingSupportConfirmed)
                        {
                            info.Type = ActionVerdictType.HoldOrWatch;
                            info.BadgeText = "관망";
                            info.TitleText = "[ AI 참모 최종 결론 : 과매도 구간이나 당일 하락 중 / 지지선 안착 대기 ]";
                            info.GuideText = $"종합 우량(점수 {item.FinalScore}점) 및 RSI({item.Rsi:F1}) 과매도이나 당일 {item.ChangeRate:+0.00;-0.00}% 약세 지속 중. 성급한 물타기를 자제하고 하락 멈춤(양봉/보합 지지) 확인 후 추매 권고";
                            info.BuyPriceText = $"• 매수가: [지지 확인 대기] (1차 지지선: {buy1Str})";
                            info.BackColor = Color.FromArgb(36, 32, 20);
                            info.ForeColor = Color.FromArgb(255, 220, 90);
                            info.BorderColor = Color.FromArgb(170, 140, 40);
                            info.ListBadgeColor = Color.FromArgb(240, 200, 80);
                            info.CanBuy = false;
                        }
                        else
                        {
                            info.BadgeText = "매수추천";
                            info.TitleText = "[ AI 참모 최종 결론 : RSI 과매도 구간 지지 & 추매 추천 ]";
                            info.GuideText = $"종합 우량(점수 {item.FinalScore}점) 및 RSI({item.Rsi:F1}) 과매도 침체권. 20일 지지선 안착 확인 후 역발상 추매 가능";
                            info.BuyPriceText = $"• 매수가: {buy1Str}{secBuyAppend}";
                            info.BackColor = Color.FromArgb(16, 44, 28);
                            info.ForeColor = Color.FromArgb(90, 255, 170);
                            info.BorderColor = Color.FromArgb(40, 170, 90);
                            info.ListBadgeColor = Color.FromArgb(0, 230, 118);
                            info.CanBuy = true;
                        }
                    }
                    else
                    {
                        // 미보유 관심 종목의 과매도 진입
                        // 당일 급락 중이거나 지지선 안착 전이면 관망 유도
                        bool isEntrySupportConfirmed = item.ChangeRate >= -1.0m;
                        if (!isEntrySupportConfirmed)
                        {
                            info.Type = ActionVerdictType.HoldOrWatch;
                            info.BadgeText = "관망";
                            info.TitleText = "[ AI 참모 최종 결론 : 과매도 침체권이나 추가 하락 대기 / 지지선 관망 ]";
                            info.GuideText = $"종합 우량(점수 {item.FinalScore}점) 및 RSI({item.Rsi:F1}) 침체권이나 당일 하락세 지속 중. 하방 지지선 안착 확인 전까지 신규 진입 보류";
                            info.BuyPriceText = $"• 매수가: [바닥 확인 대기] (희망 진입가: {buy1Str})";
                            info.BackColor = Color.FromArgb(36, 32, 20);
                            info.ForeColor = Color.FromArgb(255, 220, 90);
                            info.BorderColor = Color.FromArgb(170, 140, 40);
                            info.ListBadgeColor = Color.FromArgb(240, 200, 80);
                            info.CanBuy = false;
                        }
                        else
                        {
                            info.BadgeText = "매수추천";
                            info.TitleText = "[ AI 참모 최종 결론 : RSI 과매도 바닥 구간 분할 매수 추천 ]";
                            info.GuideText = $"종합 우량(점수 {item.FinalScore}점) 및 RSI({item.Rsi:F1}) 과매도 침체권. 20일 지지선 안착 확인 후 역발상 진입 권장";
                            info.BuyPriceText = $"• 매수가: {buy1Str}";
                            info.BackColor = Color.FromArgb(16, 44, 28);
                            info.ForeColor = Color.FromArgb(90, 255, 170);
                            info.BorderColor = Color.FromArgb(40, 170, 90);
                            info.ListBadgeColor = Color.FromArgb(0, 230, 118);
                            info.CanBuy = true;
                        }
                    }
                }
                else
                {
                    info.Type = ActionVerdictType.BuyEntry;
                    if (isHeld)
                    {
                        // [보유 종목 추가 매수(불타기/추매) 철벽 인터락]
                        // 1. RSI 58 이상 과열 진입 구간: 추가 매수 금지, 기존 물량만 수익 극대화 홀딩
                        // 2. 타이밍 안전벨트가 '눌림목 대기'이거나 현재가가 권고 매수가보다 2% 이상 높을 때:
                        //    이미 수익 중이거나 주가가 위로 튀어있는 상태에서 평단을 높이는 불타기를 막고 [관망]으로 유지
                        bool isHeldWaitingPullback = (item.RecommendedBuyPrice > 0 && item.CurrentPrice > item.RecommendedBuyPrice * 1.02m) ||
                                                     (item.TimingBadge != null && (item.TimingBadge.Contains("눌림목 대기") || item.TimingBadge.Contains("과열")));

                        if (item.Rsi >= 58.0m)
                        {
                            info.Type = ActionVerdictType.HoldOrWatch;
                            info.BadgeText = "관망";
                            info.TitleText = "[ AI 참모 최종 결론 : 우량 상승추세 지속 / 불타기 금지 및 관망 ]";
                            info.GuideText = $"종합 우량(점수 {item.FinalScore}점) 및 상승 추세 유지. 기존 물량은 수익 극대화 홀딩하되, RSI({item.Rsi:F1}) 상승권으로 추가 매수는 자제하고 관망 권고";
                            info.BuyPriceText = $"• 매수가: [추매 보류] (상승 추세 홀딩 중 / 추가 매수는 20일선 눌림목까지 대기)";
                            info.BackColor = Color.FromArgb(36, 32, 20);
                            info.ForeColor = Color.FromArgb(255, 220, 90);
                            info.BorderColor = Color.FromArgb(170, 140, 40);
                            info.ListBadgeColor = Color.FromArgb(240, 200, 80);
                            info.CanBuy = false;
                        }
                        else if (isHeldWaitingPullback)
                        {
                            info.Type = ActionVerdictType.HoldOrWatch;
                            info.BadgeText = "관망";
                            info.TitleText = "[ AI 참모 최종 결론 : 우량 보유 중이나 타점 이격 / 불타기 금지 및 관망 ]";
                            info.GuideText = $"종합 우량(점수 {item.FinalScore}점)이나 현재 주가가 눌림목 권고선보다 높아 추가 매수(불타기) 시 평단 상승 위험. 기존 물량 홀딩 및 지지선 관망 권고";
                            info.BuyPriceText = $"• 매수가: [추매 대기] (눌림목 대기선: {buy1Str})";
                            info.BackColor = Color.FromArgb(36, 32, 20);
                            info.ForeColor = Color.FromArgb(255, 220, 90);
                            info.BorderColor = Color.FromArgb(170, 140, 40);
                            info.ListBadgeColor = Color.FromArgb(240, 200, 80);
                            info.CanBuy = false;
                        }
                        else
                        {
                            info.BadgeText = "매수추천";
                            info.TitleText = "[ AI 참모 최종 결론 : 눌림목 지지 안착 / 분할 추매 추천 ]";
                            info.GuideText = $"종합 우량(점수 {item.FinalScore}점) 및 지지선 안착 확인. 기존 물량 홀딩 및 매수가 부근 분할 추매 유효";
                            info.BuyPriceText = $"• 매수가: {buy1Str}{secBuyAppend}";
                            info.BackColor = Color.FromArgb(16, 44, 28);
                            info.ForeColor = Color.FromArgb(90, 255, 170);
                            info.BorderColor = Color.FromArgb(40, 170, 90);
                            info.ListBadgeColor = Color.FromArgb(0, 230, 118);
                            info.CanBuy = true;
                        }
                    }
                    else
                    {
                        // [관심 종목 진입 타점 인터락]
                        // 우량주(isBuy)라도 현재가가 권고 매수가보다 2.0% 이상 높거나 타이밍 안전벨트가 '눌림목 대기/과열'이면
                        // 시장가 추격 매수 방지를 위해 최종 액션을 '관망'으로 전환하고 눌림목 안착 대기 유도
                        bool isWaitingPullback = (item.RecommendedBuyPrice > 0 && item.CurrentPrice > item.RecommendedBuyPrice * 1.02m) ||
                                                 (item.TimingBadge != null && (item.TimingBadge.Contains("눌림목 대기") || item.TimingBadge.Contains("과열")));

                        if (isWaitingPullback)
                        {
                            info.Type = ActionVerdictType.HoldOrWatch;
                            info.BadgeText = "관망";
                            info.TitleText = "[ AI 참모 최종 결론 : 우량주이나 현재 타점 이격 / 눌림목 관망 대기 ]";
                            info.GuideText = $"종합 우량(점수 {item.FinalScore}점, {item.Grade}등급) 종목이나 현재 단기 슈팅으로 권고 매수가({buy1Str})와 이격 발생. 20일선 눌림목 안착 시까지 추격 금지 및 관망 권고";
                            info.BuyPriceText = $"• 매수가: [눌림목 대기] (희망 진입가: {buy1Str})";
                            info.BackColor = Color.FromArgb(36, 32, 20);
                            info.ForeColor = Color.FromArgb(255, 220, 90);
                            info.BorderColor = Color.FromArgb(170, 140, 40);
                            info.ListBadgeColor = Color.FromArgb(240, 200, 80);
                            info.CanBuy = false;
                        }
                        else
                        {
                            info.BadgeText = "매수추천";
                            info.TitleText = "[ AI 참모 최종 결론 : 20일 눌림목 분할 매수 추천 ]";
                            info.GuideText = $"종합 우량(점수 {item.FinalScore}점) 및 지지선 안착 확인. 권고 매수가 부근 분할 진입 권장";
                            info.BuyPriceText = $"• 매수가: {buy1Str}";
                            info.BackColor = Color.FromArgb(16, 44, 28);
                            info.ForeColor = Color.FromArgb(90, 255, 170);
                            info.BorderColor = Color.FromArgb(40, 170, 90);
                            info.ListBadgeColor = Color.FromArgb(0, 230, 118);
                            info.CanBuy = true;
                        }
                    }
                }
            }
            else
            {
                info.Type = ActionVerdictType.HoldOrWatch;
                info.BadgeText = "관망";
                info.TitleText = "[ AI 참모 최종 결론 : 관망 및 지지선 확인 대기 ]";
                info.GuideText = $"종합 {item.FinalScore}점({item.Grade}등급). 추세 방향성 탐색 중으로 무리한 진입을 자제하고 지지선 안착 대기";
                info.BuyPriceText = $"• 매수가: [관망 대기] (희망 진입가: {buy1Str})";
                info.BackColor = Color.FromArgb(36, 32, 20);
                info.ForeColor = Color.FromArgb(255, 220, 90);
                info.BorderColor = Color.FromArgb(170, 140, 40);
                info.ListBadgeColor = Color.FromArgb(240, 200, 80);
                info.CanBuy = false;
            }

            return info;
        }

        /// <summary>
        /// 최종 액션 뱃지 우선순위 정렬 가중치 (매수추천 > 과열주의 > 관망 > 급락주의 > 비중축소/매수금지)
        /// </summary>
        public static int GetActionPriority(string badgeText)
        {
            if (string.IsNullOrEmpty(badgeText)) return 0;
            if (badgeText.Contains("매수추천")) return 5;
            if (badgeText.Contains("과열주의")) return 4;
            if (badgeText.Contains("관망")) return 3;
            if (badgeText.Contains("급락주의")) return 2;
            if (badgeText.Contains("비중축소") || badgeText.Contains("매수금지")) return 1;
            return 0;
        }
    }
}
