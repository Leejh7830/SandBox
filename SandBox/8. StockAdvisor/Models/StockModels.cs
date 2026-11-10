using System;
using System.Collections.Generic;
using System.Drawing;
using Newtonsoft.Json;

namespace GateHelper.StockAdvisor.Models
{
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

        // --- 객관적 정량 지표 & 3대 점수 체계 ---
        /// <summary>
        /// 차트 점수 (1~10점): 20일 일봉 정배열, 거래량 수급, 추세 기반
        /// </summary>
        public int ChartScore { get; set; } = 5;

        /// <summary>
        /// 뉴스 점수 (1~10점): 공시, 수출통제, 실적 팩트 기반 호악재 감성
        /// </summary>
        public int NewsScore { get; set; } = 5;

        /// <summary>
        /// 손익비/안전 점수 (1~10점): 20일 지지선까지의 거리(손절폭 %)
        /// </summary>
        public int RiskScore { get; set; } = 5;

        /// <summary>
        /// 최종 종목 점수 (1~10점): [차트 40% + 뉴스 30% + 손익비 30%]
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
        /// 타이밍 상태 뱃지 (🔴 과열(매수금지), 🟢 골든진입, 🟡 눌림목대기, 🔵 과매도반등, ⚪ 관망유지, ❌ 보유불필요)
        /// </summary>
        public string TimingBadge { get; set; } = "⚪ 관망유지";

        /// <summary>
        /// 추격매수 방지 경고 및 타이밍 가이드 코멘트
        /// </summary>
        public string TimingWarning { get; set; } = string.Empty;

        // --- 🌊 수급 및 스마트 머니(외인/기관) 지표 ---
        /// <summary>
        /// 외인/기관 수급 뱃지 (예: 🌊쌍끌이매수, 🦅외인매수, 🏦기관매수, 🐜개미매수, 🌧️쌍끌이매도, ⚪수급대기)
        /// </summary>
        public string MajorSupplyBadge { get; set; } = "⚪ 수급대기";
        public string MajorSupplyDetail { get; set; } = string.Empty;

        // --- 🛡️ 재무 및 공시 지뢰 탐지 지표 ---
        /// <summary>
        /// 재무/공시 지뢰 탐지 뱃지 (예: 🛡️클린안전, 💀3년적자, 🚨CB발행, ⚠️부채과다, 💎주주환원)
        /// </summary>
        public string LandmineBadge { get; set; } = "🛡️ 클린안전";
        public string LandmineDetail { get; set; } = string.Empty;
        public bool HasCriticalLandmine { get; set; } = false;
        public decimal DebtRatio { get; set; } = 0m;
        public int ConsecutiveLossYears { get; set; } = 0;

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
                return GradeHelper.CalculateGrade(FinalScore, Rsi, ProfitRate);
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
    }
}

