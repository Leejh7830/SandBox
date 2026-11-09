using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace GateHelper.StockAdvisor.Models
{
    public enum PositionGroup
    {
        GroupA, // 반등 시 손절 / 비중 축소 권고 (적색/주황)
        GroupB, // 관망 및 현상 유지 / 지지선 확인 (황색/회색)
        GroupC  // 분할 매수 검토 / 실적 모멘텀 유효 (녹색/청색)
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
    }

    /// <summary>
    /// LLM 3축 리스크 평가 및 진단 DTO
    /// </summary>
    public class StockRiskDiagnosis
    {
        public string StockCode { get; set; } = string.Empty;
        public string StockName { get; set; } = string.Empty;

        /// <summary>
        /// 1. 출처 신뢰도 (1~5점)
        /// </summary>
        public int SourceReliability { get; set; } = 3;

        /// <summary>
        /// 2. 정책/실적 직결도 (-5 ~ +5점)
        /// </summary>
        public int PolicyImpact { get; set; } = 0;

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
    }
}

