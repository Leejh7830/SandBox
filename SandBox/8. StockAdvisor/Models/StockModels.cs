using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace GateHelper.StockAdvisor.Models
{
    /// <summary>
    /// 차트 렌더링 및 기술적 분석용 일봉 캔들 DTO
    /// </summary>
    public class DailyCandle
    {
        public DateTime Date { get; set; }
        public decimal Close { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public long Volume { get; set; }
    }

    /// <summary>
    /// AI 4대 종합 등급 (A, B, C, F)
    /// - 🟢 A (8, 9, 10점): 초록색 (우수/안전)
    /// - 🟡 B (6, 7점): 노란색 (주의/관망)
    /// - 🟠 C (3, 4, 5점): 주황색 (경고/경계)
    /// - 🔴 F (0, 1, 2점): 빨간색 (위험/탈출)
    /// </summary>
    public enum PositionGrade
    {
        A, // 8, 9, 10점: 우수/안전 (초록)
        B, // 6, 7점: 양호/관망 (노랑)
        C, // 3, 4, 5점: 주의/경계 (주황)
        F  // 0, 1, 2점: 위험/손절 (빨강)
    }

    public static class GradeHelper
    {
        public static PositionGrade CalculateGrade(int finalScore, decimal rsi = 50m, decimal profitRate = 0m)
        {
            if (finalScore >= 8) return PositionGrade.A; // 8, 9, 10점
            if (finalScore >= 6) return PositionGrade.B; // 6, 7점
            if (finalScore >= 3) return PositionGrade.C; // 3, 4, 5점
            return PositionGrade.F; // 0, 1, 2점
        }

        public static Color GetGradeColor(PositionGrade grade)
        {
            switch (grade)
            {
                case PositionGrade.A:
                    return Color.FromArgb(0, 230, 118); // 초록색 (8,9,10점)
                case PositionGrade.B:
                    return Color.FromArgb(255, 214, 0); // 노란색 (6,7점)
                case PositionGrade.C:
                    return Color.FromArgb(255, 145, 0); // 주황색 (3,4,5점)
                case PositionGrade.F:
                default:
                    return Color.FromArgb(255, 68, 68); // 빨간색 (0,1,2점)
            }
        }

        public static string GetGradeDescription(PositionGrade grade)
        {
            switch (grade)
            {
                case PositionGrade.A: return "A (우수/안전: 8~10점)";
                case PositionGrade.B: return "B (양호/주의: 6~7점)";
                case PositionGrade.C: return "C (경계/경고: 3~5점)";
                case PositionGrade.F: return "F (위험/탈출: 0~2점)";
                default: return "-";
            }
        }
    }

    /// <summary>
    /// 메인 테이블 지뢰검증 간결 4단계 뱃지 & 컬러 헬퍼
    /// - 우수 (주주환원 친화 공시 등 호재): 연초록 (0, 230, 118)
    /// - 안전 (부채 건전 + 악재 지뢰 0건 기본 클린): 연초록 (0, 230, 118)
    /// - 주의 (부채비율 250% 이상): 주황색 (255, 170, 50)
    /// - 위험 (CB/BW, 유상증자, 감사의견거절, 3년적자, 횡령 등): 빨간색 (255, 75, 75)
    /// </summary>
    public static class LandmineHelper
    {
        public static (string Text, Color Color, bool IsBold) GetSimplifiedBadge(StockHoldingItem item)
        {
            if (item == null) return ("안전", Color.FromArgb(0, 230, 118), false);

            string raw = item.LandmineBadge ?? string.Empty;
            if (item.HasCriticalLandmine || raw.Contains("적자") || raw.Contains("CB") || raw.Contains("BW") || raw.Contains("유상") || raw.Contains("위기") || raw.Contains("횡령") || raw.Contains("감사"))
            {
                return ("위험", Color.FromArgb(255, 75, 75), true);
            }
            if (raw.Contains("부채") || item.DebtRatio >= 250m)
            {
                return ("주의", Color.FromArgb(255, 170, 50), true);
            }
            if (raw.Contains("환원") || raw.Contains("우수"))
            {
                return ("우수", Color.FromArgb(0, 230, 118), true);
            }
            return ("안전", Color.FromArgb(0, 230, 118), false);
        }

        public static int GetLandminePriority(StockHoldingItem item)
        {
            var badge = GetSimplifiedBadge(item).Text;
            switch (badge)
            {
                case "우수": return 4;
                case "안전": return 3;
                case "주의": return 2;
                case "위험": return 1;
                default: return 0;
            }
        }
    }

    public enum PositionGroup
    {
        GroupA, // 반등 시 손절 / 비중 축소 권고 (적색/주황)
        GroupB, // 관망 및 현상 유지 / 지지선 확인 (황색/회색)
        GroupC  // 분할 매수 검토 / 실적 모멘텀 유효 (녹색/청색)
    }

    /// <summary>
    /// 메인 종목 리스트뷰 표시 필터 모드 (전체 / 보유 종목 / 관심 종목)
    /// </summary>
    public enum StockFilterMode
    {
        All,          // 전체 종목
        HoldingOnly,  // 보유 종목 (수량 > 0)
        WatchlistOnly // 🎯 관심 종목 (수량 == 0, 매수 타점 대기)
    }

    public enum OverallRiskLevel
    {
        Safe,    // 🟢 안전 (거시 지표 안정 및 리스크 낮음)
        Caution, // 🟡 주의 (변동성 확대 또는 주요 정책 발표 임박)
        Panic    // 🔴 비상 (돌발 악재, 글로벌 수출통제 또는 폭락 국면)
    }

    /// <summary>
    /// 사용자의 개별 보유/관심 종목 DTO
    /// </summary>
    public class StockHoldingItem
    {
        public string StockCode { get; set; } = string.Empty;
        public string StockName { get; set; } = string.Empty;
        public int Quantity { get; set; } = 0;
        public decimal BuyPrice { get; set; } = 0m;
        public decimal CurrentPrice { get; set; } = 0m;
        public decimal ChangeRate { get; set; } = 0m;
        public decimal ChangePrice { get; set; } = 0m;
        public string Memo { get; set; } = string.Empty;

        /// <summary>
        /// 실제 증권사 앱(MTS/HTS)에 자동청산(보유종목) 또는 신규예약매수(관심종목) 주문이 설정되었는지 체크 여부
        /// </summary>
        public bool IsBrokerOrderSet { get; set; } = false;

        // --- 객관적 정량 지표 & 4대 점수 체계 ---
        /// <summary>
        /// 차트 점수 (1~10점): 20일 일봉 정배열, 거래량 수급, 추세 기반 (가중치 30%)
        /// </summary>
        public int ChartScore { get; set; } = 5;

        /// <summary>
        /// 수급·재무 점수 (1~10점): 외인/기관 순매수 수급 + 부채비율/배당 건전성 (가중치 25%)
        /// </summary>
        public int SupplyFinanceScore { get; set; } = 5;

        /// <summary>
        /// 뉴스·공시 점수 (1~10점): 공시, 수출통제, 실적 팩트 기반 호악재 감성 (가중치 25%)
        /// </summary>
        public int NewsScore { get; set; } = 5;

        /// <summary>
        /// 손익비/안전 점수 (1~10점): 20일 지지선까지의 거리(손절폭 %) (가중치 20%)
        /// </summary>
        public int RiskScore { get; set; } = 5;

        /// <summary>
        /// 최종 종목 점수 (1~10점): [차트 30% + 수급재무 25% + 뉴스 25% + 손익비 20%]
        /// </summary>
        public int FinalScore { get; set; } = 5;

        /// <summary>
        /// 14일 RSI 상대강도지수 (0~100)
        /// </summary>
        public decimal Rsi { get; set; } = 50m;

        /// <summary>
        /// 최근 5일 평균 대비 당일 거래량 비율 (%)
        /// </summary>
        public decimal VolumeRatio { get; set; } = 100m;

        /// <summary>
        /// 최근 20일 지지선 (최저가 / 1차 손절 기준선)
        /// </summary>
        public decimal SupportPrice { get; set; } = 0m;

        /// <summary>
        /// 최근 20일 저항선 (최고가 / 1차 목표가)
        /// </summary>
        public decimal ResistancePrice { get; set; } = 0m;

        /// <summary>
        /// 지지선까지의 하방 위험률 (%)
        /// </summary>
        public decimal DownsideRiskPercent { get; set; } = 0m;

        /// <summary>
        /// 타이밍 상태 뱃지 (🔴 과열(매수금지), 🟢 골든 눌림목, 🟡 눌림목 대기, 🔵 과매도반등, ⚪ 관망유지, ❌ 보유불필요)
        /// </summary>
        public string TimingBadge { get; set; } = "관망유지";

        /// <summary>
        /// 추격매수 방지 경고 및 타이밍 가이드 코멘트
        /// </summary>
        public string TimingWarning { get; set; } = string.Empty;

        // --- 수급 및 스마트 머니(외인/기관) 지표 ---
        /// <summary>
        /// 외인/기관 수급 뱃지 (예: 쌍끌이매수, 외인매수, 기관매수, 개미매수, 쌍끌이매도, 수급대기)
        /// </summary>
        public string MajorSupplyBadge { get; set; } = "수급대기";
        public string MajorSupplyDetail { get; set; } = string.Empty;

        // --- 재무 및 공시 지뢰 탐지 지표 ---
        /// <summary>
        /// 재무/공시 지뢰 탐지 뱃지 (예: 클린안전, 3년적자, CB발행, 부채과다, 주주환원)
        /// </summary>
        public string LandmineBadge { get; set; } = "클린안전";
        public string LandmineDetail { get; set; } = string.Empty;
        public bool HasCriticalLandmine { get; set; } = false;
        public decimal DebtRatio { get; set; } = 0m;
        public int ConsecutiveLossYears { get; set; } = 0;

        // --- 실적 발표(어닝) D-Day 레이더 지표 ---
        public int? EarningsDaysRemaining { get; set; } = null;
        public string EarningsPeriodTitle { get; set; } = string.Empty;
        public DateTime? EarningsAnnouncementDate { get; set; } = null;

        /// <summary>
        /// 보유 수량이 0 이하인 경우 관심 종목(매수 대기)으로 자동 판정
        /// </summary>
        [JsonIgnore]
        public bool IsWatchlist => Quantity <= 0;

        // --- 목표가/손절가/매수가 실시간 터치 알림 상태 추적 (중복 도배 방지용 세션 상태) ---
        [JsonIgnore]
        public bool WasTargetAlerted { get; set; } = false;

        [JsonIgnore]
        public bool WasStopAlerted { get; set; } = false;

        [JsonIgnore]
        public bool WasBuyAlerted { get; set; } = false;

        [JsonIgnore]
        public bool WasSecondaryBuyAlerted { get; set; } = false;

        /// <summary>
        /// 미니 캔들 차트용 최근 20일 일봉 데이터 캐시 (런타임 메모리 유지, 0-IO)
        /// </summary>
        [JsonIgnore]
        public List<DailyCandle> RecentCandles { get; set; }

        // --- 네이버 스크래핑 무결성 검증 필터 ---
        public bool IsPriceScrapeSuccess { get; set; } = true;
        public string ScrapeErrorMessage { get; set; } = string.Empty;
        public DateTime? LastScrapedTime { get; set; } = null;

        // --- AI 퀀트 권고 매수가 및 진입 타점 ---
        public decimal RecommendedBuyPrice { get; set; } = 0m;  // 1차 눌림목 권고 매수가
        public decimal SecondaryBuyPrice { get; set; } = 0m;    // 2차 분할/눌림목 권고 매수가 (물타기/추매 지지선)
        public decimal TargetStopPrice { get; set; } = 0m;       // 2차 방어선 / 손절 기준가
        public string EntryTimingBadge { get; set; } = "관망대기"; // 진입 판정 뱃지
        public string EntryTimingGuide { get; set; } = string.Empty; // 구체적 매수 가이드 문구

        // --- AI 퀀트 권고 매도(익절) 타점 & 청산 시나리오 ---
        public decimal RecommendedTargetPrice { get; set; } = 0m; // 1차 권고 목표가 (분할 익절선)
        public decimal SecondaryTargetPrice { get; set; } = 0m;   // 2차 최종 목표가 (슈팅/과열 청산선)
        public string ExitTimingBadge { get; set; } = "추세보유"; // 매도 판정 뱃지
        public string ExitTimingGuide { get; set; } = string.Empty; // 구체적 익절 가이드 문구

        // --- 정통 퀀트 우량주 8대 필터 요약 ---
        public string QuantBadge { get; set; } = "퀀트대기";
        public string QuantSummary { get; set; } = string.Empty;

        // 최종 AI 진단 결과
        public StockRiskDiagnosis LastDiagnosis { get; set; }

        [JsonIgnore]
        public decimal TotalBuyAmount => Quantity * BuyPrice;

        [JsonIgnore]
        public decimal TotalEvalAmount => Quantity * CurrentPrice;

        [JsonIgnore]
        public decimal ProfitAmount => (Quantity > 0 && BuyPrice > 0) ? (TotalEvalAmount - TotalBuyAmount) : 0m;

        [JsonIgnore]
        public decimal ProfitRate => (BuyPrice > 0) ? ((CurrentPrice - BuyPrice) / BuyPrice * 100m) : 0m;

        [JsonIgnore]
        public decimal WeightPercent { get; set; } = 0m;

        [JsonIgnore]
        public PositionGrade Grade
        {
            get
            {
                if (LastDiagnosis != null && LastDiagnosis.Grade.HasValue)
                    return LastDiagnosis.Grade.Value;
                return GradeHelper.CalculateGrade(FinalScore, Rsi, ChangeRate);
            }
        }
    }

    /// <summary>
    /// LLM 3축 리스크 평가 및 진단 DTO
    /// </summary>
    public class StockRiskDiagnosis
    {
        public string StockCode { get; set; } = string.Empty;
        public string StockName { get; set; } = string.Empty;

        /// <summary>
        /// AI 6대 종합 등급 (S, A, B, C, D, F)
        /// </summary>
        public PositionGrade? Grade { get; set; }

        /// <summary>
        /// 1. 출처 신뢰도 (1~5점)
        /// </summary>
        public int SourceReliability { get; set; } = 3;

        /// <summary>
        /// 2. 정책/실적 직결도 (-5 ~ +5점)
        /// </summary>
        public int PolicyImpact { get; set; } = 0;

        /// <summary>
        /// 뉴스 팩트 감성 점수 (1~10점)
        /// </summary>
        public int NewsScore { get; set; } = 5;

        /// <summary>
        /// 차트 기술적 점수 (1~10점)
        /// </summary>
        public int ChartScore { get; set; } = 5;

        /// <summary>
        /// 수급·재무 점수 (1~10점)
        /// </summary>
        public int SupplyFinanceScore { get; set; } = 5;

        /// <summary>
        /// 손익비 안전 점수 (1~10점)
        /// </summary>
        public int RiskScore { get; set; } = 5;

        /// <summary>
        /// 최종 종합 점수 (1~10점)
        /// </summary>
        public int FinalScore { get; set; } = 5;

        /// <summary>
        /// 타이밍 가이드 코멘트
        /// </summary>
        public string TimingWarning { get; set; } = string.Empty;

        /// <summary>
        /// 3. 포지션 분류 (Group A: 반등시 손절, Group B: 관망/유지, Group C: 분할매수)
        /// </summary>
        public PositionGroup Group { get; set; } = PositionGroup.GroupB;

        /// <summary>
        /// 구체적 실행 권고 (예: "비중 30% 축소", "손절가 182,000원 설정", "관망")
        /// </summary>
        public string RecommendedAction { get; set; } = string.Empty;

        /// <summary>
        /// 추천 손절/방어선 기준가
        /// </summary>
        public decimal TargetCutPrice { get; set; } = 0m;

        /// <summary>
        /// 진단 상세 사유 및 대응 논리
        /// </summary>
        public string ReasonSummary { get; set; } = string.Empty;

        public DateTime DiagnosedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 장전/장중 종합 시장 리스크 브리핑
    /// </summary>
    public class MarketBriefing
    {
        public OverallRiskLevel RiskLevel { get; set; } = OverallRiskLevel.Safe;
        public string Line1 { get; set; } = "시장 지표 및 글로벌 동향 수집 대기 중입니다.";
        public string Line2 { get; set; } = "AI 리스크 진단을 실행하여 맞춤 포지션 분석을 받아보세요.";
        public string Line3 { get; set; } = "비상 상황 발생 시 언제든 리스크 참모의 권고를 확인하세요.";
        public string GlobalIssuesSummary { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 암호화되어 로컬에 저장되는 포트폴리오 루트 데이터
    /// </summary>
    public class PortfolioData
    {
        public List<StockHoldingItem> Items { get; set; } = new List<StockHoldingItem>();
        public MarketBriefing LastBriefing { get; set; } = new MarketBriefing();

        // 2단계 KB증권 Open API 대비 인증 필드 (DPAPI 암호화 보관)
        public string KbAppKey { get; set; } = string.Empty;
        public string KbAppSecret { get; set; } = string.Empty;
        public string KbAccountNumber { get; set; } = string.Empty;

        // 자동 동기화 옵션
        public bool AutoPriceSyncEnabled { get; set; } = false;
        public int PriceSyncIntervalMinutes { get; set; } = 5;

        public bool AutoAiDiagnosisEnabled { get; set; } = false;
        public int AiDiagnosisIntervalMinutes { get; set; } = 5;

        // 보유 종목 목표가/손절가 도달 시 실시간 토스트 알림 활성화 (기본 true)
        public bool EnablePriceTouchAlert { get; set; } = true;

        // 주도주 매수 레이더 종목(관심 미등록 포함)이 AI 권고 매수가 도달 시 자동 토스트 알림 활성화 (기본 true)
        public bool EnableLeaderRadarAutoAlert { get; set; } = true;
    }

    public enum ToastAlertType
    {
        TargetHit, // 목표가 도달 (익절 권고)
        StopHit,   // 손절 방어선 터치/이탈 (리스크 관리 권고)
        BuyHit     // ★ 적정 매수가 도달 (관심종목 1차 진입 / 보유종목 추가매수)
    }

    /// <summary>
    /// 목표가/손절가/적정매수가 도달 알림 이력 항목 (메모리 전용, 0B 디스크 I/O)
    /// </summary>
    public class StockAlertLogItem
    {
        public DateTime AlertTime { get; set; } = DateTime.Now;
        public string StockCode { get; set; } = string.Empty;
        public string StockName { get; set; } = string.Empty;
        public ToastAlertType AlertType { get; set; } = ToastAlertType.TargetHit;
        public decimal CurrentPrice { get; set; } = 0m;
        public decimal TriggerPrice { get; set; } = 0m;
        public decimal ChangeRate { get; set; } = 0m;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
    }

    /// <summary>
    /// 정통 퀀트 조건식 개별 검증 항목
    /// </summary>
    public class QuantFilterItem
    {
        public string Id { get; set; } = string.Empty;       // E, C, D, F, G, H, I, B
        public string Name { get; set; } = string.Empty;     // 시가총액, ROE, 부채비율 등
        public string Criteria { get; set; } = string.Empty; // 10% 이상, 150% 이하 등
        public string ActualValue { get; set; } = "-";       // 19.09% 등
        public bool IsPassed { get; set; } = false;
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// 정통 퀀트 8대 황금 필터 종합 분석 결과
    /// </summary>
    public class QuantAnalysisResult
    {
        public string StockCode { get; set; } = string.Empty;
        public string StockName { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; } = 0m;
        public string MarketCapText { get; set; } = "-";
        public decimal Roe { get; set; } = 0m;
        public decimal OperatingMargin { get; set; } = 0m;
        public decimal DebtRatio { get; set; } = 0m;
        public decimal Per { get; set; } = 0m;
        public decimal Pbr { get; set; } = 0m;
        public decimal DividendYield { get; set; } = 0m;
        public decimal ForeignRate { get; set; } = 0m;
        public decimal Rsi { get; set; } = 0m;

        public int PassCount { get; set; } = 0;
        public int TotalCount { get; set; } = 8;
        public string QuantGradeBadge { get; set; } = "보통";
        public List<QuantFilterItem> Items { get; set; } = new List<QuantFilterItem>();

        public decimal PassPercent => (TotalCount > 0) ? ((decimal)PassCount / TotalCount * 100m) : 0m;
    }

    /// <summary>
    /// UI 표시 전 모든 이모지 및 특수기호 난립을 차단하고 단정하게 정돈하는 헬퍼
    /// </summary>
    public static class TextCleanHelper
    {
        private static readonly Regex EmojiRegex = new Regex(@"[\uD800-\uDBFF][\uDC00-\uDFFF]|[\u2600-\u27BF]|[\u2300-\u23FF]|[\uFE00-\uFE0F]", RegexOptions.Compiled);

        public static string StripEmoji(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return EmojiRegex.Replace(input, "").Trim();
        }
    }

    /// <summary>
    /// AI 참모가 내리는 최종 투자 행동 판정 열거형
    /// </summary>
    public enum ActionVerdictType
    {
        AvoidOrReduce,   // 🔴 매수 금지 / 비중 축소 (역배열, C/F등급, 지뢰 악재)
        SharpDropAlert,  // 🟠 급락 주의 (당일 -4% 초과 급락, 떨어지는 칼날 진입 보류)
        TakeProfit,      // 🔵 추격매수 금지 / 분할 익절 (RSI 68+ 과열 슈팅)
        HoldOrWatch,     // 🟡 관망 대기 / 지지선 확인 대기 (중립 B등급, 방향성 탐색)
        OversoldBounce,  // 🟢 과매도 반등 (RSI 36.5 이하 + 20일 지지선 안착 역발상 분할 매수)
        BuyEntry         // 🟢 적극 분할 매수 진입 유효 (A등급/우량 B등급, 20일선 눌림목 안착)
    }

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
                    BorderColor = Color.FromArgb(60, 75, 100),
                    ListBadgeColor = Color.FromArgb(170, 185, 205),
                    CanBuy = false,
                    IsEmergency = false
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

            // [매수 승인 판정 우선 계산] 종합 6점 이상(B등급 이상)에 수급 양호하고 지뢰가 없으면 객관적 우량 매수 승인
            // (RSI 58 이상 상승 구간은 뒤쪽에서 [보유홀딩]으로 분리되므로 isBuy 자체는 6~7점 우량 팩터 전체에 대해 true 유지)
            bool isBuy = (item.Grade == PositionGrade.A || item.FinalScore >= 7 ||
                         (item.Grade == PositionGrade.B && item.FinalScore >= 6 && item.ChartScore >= 6 && item.SupplyFinanceScore >= 5))
                         && !item.HasCriticalLandmine && !isSharpDrop;

            // [회피/비중축소 인터락] C/F등급, 종합 4점 이하, 또는 치명적 지뢰 발생 시.
            // (종합 6점 이상 B등급 우량 종목은 과거 텍스트나 일시적 RSI 상승으로 인해 절대 비중축소/매수금지로 왜곡되지 않음)
            bool isAvoid = (item.Grade == PositionGrade.C || item.Grade == PositionGrade.F ||
                           item.FinalScore <= 4 || item.HasCriticalLandmine) ||
                           (item.FinalScore < 6 && item.LastDiagnosis != null &&
                           (item.LastDiagnosis.RecommendedAction.Contains("손절") || item.LastDiagnosis.RecommendedAction.Contains("축소") || item.LastDiagnosis.RecommendedAction.Contains("경보")));

            bool isOverheat = item.Rsi >= 68.0m || (item.TimingBadge != null && item.TimingBadge.Contains("과열"));

            // 과매도 반등: RSI 36.5 이하 침체권 + 손익비/안전 우수 + 악재/지뢰 없음 + 단기 급락(-4% 초과) 아님
            bool isOversold = item.Rsi <= 36.5m && item.Rsi > 0m && item.RiskScore >= 7 && !item.HasCriticalLandmine && !isSharpDrop;
            bool isHeld = item.Quantity > 0;

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
                info.Type = ActionVerdictType.TakeProfit;
                if (isHeld)
                {
                    info.BadgeText = "과열주의";
                    info.TitleText = "[ AI 참모 최종 결론 : 단기 과열 구간 / 분할 익절 및 수익 확정 권고 ]";
                    info.GuideText = "단기 과열 슈팅 구간! 욕심을 줄이고 보유 물량의 30~50%를 분할 매도하여 수익을 확정하세요.";
                    info.BuyPriceText = $"• 매수가: [추격 금지] (20일 눌림목 대기선: {buy1Str} 도달 시에만 검토)";
                }
                else
                {
                    info.BadgeText = "과열주의";
                    info.TitleText = "[ AI 참모 최종 결론 : 단기 과열 주의 / 추격 매수 절대 금지 ]";
                    info.GuideText = "단기 과열 슈팅 구간! 지금 사면 상투 물림 위험. 20일선 눌림목 도달 시까지 매수 금지";
                    info.BuyPriceText = $"• 매수가: [추격 금지] (20일 눌림목 대기선: {buy1Str} 도달 시에만 검토)";
                }
                info.BackColor = Color.FromArgb(16, 32, 54);
                info.ForeColor = Color.FromArgb(100, 215, 255);
                info.BorderColor = Color.FromArgb(50, 130, 210);
                info.ListBadgeColor = Color.FromArgb(70, 190, 255);
                info.CanBuy = false;
            }
            else if (isBuy)
            {
                if (isOversold)
                {
                    info.Type = ActionVerdictType.OversoldBounce;
                    if (isHeld)
                    {
                        info.BadgeText = "매수추천";
                        info.TitleText = "[ AI 참모 최종 결론 : RSI 과매도 구간 지지 & 추매 추천 ]";
                        info.GuideText = $"종합 우량(점수 {item.FinalScore}점) 및 RSI({item.Rsi:F1}) 과매도 침체권. 20일 지지선 안착 확인 후 역발상 추매 가능";
                        info.BuyPriceText = $"• 매수가: {buy1Str}{secBuyAppend}";
                    }
                    else
                    {
                        info.BadgeText = "매수추천";
                        info.TitleText = "[ AI 참모 최종 결론 : RSI 과매도 바닥 구간 분할 매수 추천 ]";
                        info.GuideText = $"종합 우량(점수 {item.FinalScore}점) 및 RSI({item.Rsi:F1}) 과매도 침체권. 20일 지지선 안착 확인 후 역발상 진입 권장";
                        info.BuyPriceText = $"• 매수가: {buy1Str}";
                    }
                    // 과매도반등도 매수 승인 신호이므로 통일된 안전 초록색 테마 적용
                    info.BackColor = Color.FromArgb(16, 44, 28);
                    info.ForeColor = Color.FromArgb(90, 255, 170);
                    info.BorderColor = Color.FromArgb(40, 170, 90);
                    info.ListBadgeColor = Color.FromArgb(0, 230, 118);
                    info.CanBuy = true;
                }
                else
                {
                    info.Type = ActionVerdictType.BuyEntry;
                    if (isHeld)
                    {
                        // [RSI 58 이상 과열 진입 구간 인터락] 주가가 올라선 상태이므로 추가 매수는 금지하고 기존 물량만 안전 홀딩
                        if (item.Rsi >= 58.0m)
                        {
                            info.BadgeText = "상승추세";
                            info.TitleText = "[ AI 참모 최종 결론 : 우량 상승추세 지속 / 수익 극대화 홀딩 ]";
                            info.GuideText = $"종합 우량(점수 {item.FinalScore}점) 및 상승 추세 유지. 기존 물량은 수익 극대화 홀딩하되, RSI({item.Rsi:F1}) 상승권으로 추가 매수는 자제 권고";
                            info.BuyPriceText = $"• 매수가: [추매 보류] (상승 추세 홀딩 중 / 추가 매수는 20일선 눌림목까지 대기)";
                        }
                        else
                        {
                            info.BadgeText = "매수추천";
                            info.TitleText = "[ AI 참모 최종 결론 : 눌림목 지지 안착 / 분할 추매 추천 ]";
                            info.GuideText = $"종합 우량(점수 {item.FinalScore}점) 및 지지선 안착 확인. 기존 물량 홀딩 및 매수가 부근 분할 추매 유효";
                            info.BuyPriceText = $"• 매수가: {buy1Str}{secBuyAppend}";
                        }
                    }
                    else
                    {
                        info.BadgeText = "매수추천";
                        info.TitleText = "[ AI 참모 최종 결론 : 20일 눌림목 분할 매수 추천 ]";
                        info.GuideText = $"종합 우량(점수 {item.FinalScore}점) 및 지지선 안착 확인. 권고 매수가 부근 분할 진입 권장";
                        info.BuyPriceText = $"• 매수가: {buy1Str}";
                    }
                    info.BackColor = Color.FromArgb(16, 44, 28);
                    info.ForeColor = Color.FromArgb(90, 255, 170);
                    info.BorderColor = Color.FromArgb(40, 170, 90);
                    info.ListBadgeColor = Color.FromArgb(0, 230, 118);
                    info.CanBuy = true;
                }
            }
            else
            {
                info.Type = ActionVerdictType.HoldOrWatch;
                if (isHeld)
                {
                    info.BadgeText = "관망";
                    info.TitleText = "[ AI 참모 최종 결론 : 기존 물량 유지 및 지지선 관망 ]";
                    info.GuideText = "추세 방향성 탐색 중. 20일 지지선 안착 확인 시까지 섣부른 물타기를 자제하고 관망 권고";
                    info.BuyPriceText = $"• 매수가: [관망 대기] (기준 지지선: {buy1Str})";
                }
                else
                {
                    info.BadgeText = "관망";
                    info.TitleText = "[ AI 참모 최종 결론 : 관망 및 지지선 확인 대기 ]";
                    info.GuideText = "추세 방향성 탐색 중. 20일 지지선 안착 및 추가 거래량 수급 확인 전까지 신규 진입 보류";
                    info.BuyPriceText = $"• 매수가: [관망 대기] (기준 지지선: {buy1Str})";
                }
                info.BackColor = Color.FromArgb(36, 32, 20);
                info.ForeColor = Color.FromArgb(255, 220, 90);
                info.BorderColor = Color.FromArgb(170, 140, 40);
                info.ListBadgeColor = Color.FromArgb(240, 200, 80);
                info.CanBuy = false;
            }

            return info;
        }

        /// <summary>
        /// 최종 액션 뱃지 우선순위 정렬 가중치 (매수추천 > 상승추세 > 과열주의 > 관망 > 급락주의 > 비중축소/매수금지)
        /// </summary>
        public static int GetActionPriority(string badgeText)
        {
            if (string.IsNullOrEmpty(badgeText)) return 0;
            if (badgeText.Contains("매수추천")) return 6;
            if (badgeText.Contains("상승추세")) return 5;
            if (badgeText.Contains("과열주의")) return 4;
            if (badgeText.Contains("관망")) return 3;
            if (badgeText.Contains("급락주의")) return 2;
            if (badgeText.Contains("비중축소") || badgeText.Contains("매수금지")) return 1;
            return 0;
        }
    }
}

