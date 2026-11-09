using System;
using System.Collections.Generic;

namespace GateHelper.AiTrend
{
    public enum TrendItemType
    {
        GitHubRepo,
        NewsArticle
    }

    public enum CategoryKind
    {
        GitHub,
        NewsRss,
        CustomKeyword
    }

    public enum AiEnrichStatus
    {
        Success,
        QuotaExceeded,
        Failed
    }

    public class AiRepoItem
    {
        public TrendItemType ItemType { get; set; } = TrendItemType.GitHubRepo;
        public int Rank { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string RepoName { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty; // Repo Owner or News Publisher
        public string HtmlUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string AiSummary { get; set; } = string.Empty; // 1줄 핵심 요약 / 핵심 팩트
        public string PracticalTip { get; set; } = string.Empty; // 2줄 실무 활용 팁 / 시사점
        public string DetailStory { get; set; } = string.Empty; // 3~4줄 상세 배경 및 심층 스토리 브리핑
        public int Stars { get; set; }
        public int Forks { get; set; }
        public string Language { get; set; } = string.Empty;
        public List<string> Topics { get; set; } = new List<string>();
        public DateTime? PushedAt { get; set; } // 최근 커밋/업데이트 일시
        public DateTime UpdatedAt { get; set; }
        public bool IsFavorite { get; set; } // 관심 저장소(즐겨찾기) 여부

        public string StarsFormatted => Stars >= 1000 ? $"{Stars / 1000.0:F1}k" : Stars.ToString();

        /// <summary>
        /// 최근 커밋 일시를 기준으로 프로젝트의 생존/활성 상태를 친절한 한국어로 표시
        /// </summary>
        public string PushedAtFormatted
        {
            get
            {
                if (!PushedAt.HasValue) return "정보 없음";
                var span = DateTime.UtcNow - PushedAt.Value.ToUniversalTime();
                if (span.TotalDays < 0) return "방금 전";
                if (span.TotalDays < 1) return span.TotalHours < 1 ? "방금 전 (🔥)" : $"{(int)span.TotalHours}시간 전 (🔥)";
                if (span.TotalDays < 7) return $"{(int)span.TotalDays}일 전 (활발 🔥)";
                if (span.TotalDays < 30) return $"{(int)(span.TotalDays / 7)}주 전 (활발 🟢)";
                if (span.TotalDays < 90) return $"{(int)(span.TotalDays / 30)}달 전 (보통 🟡)";
                if (span.TotalDays < 365) return $"{(int)(span.TotalDays / 30)}달 전 (유지보수 ⚪)";
                return $"{(int)(span.TotalDays / 365)}년 전 (개발 중단 💤)";
            }
        }

        /// <summary>
        /// 24시간 이내 발행된 최신 뉴스 여부 (주황색 불꽃 🔥 뱃지 표시용)
        /// </summary>
        public bool IsRecentNews
        {
            get
            {
                if (!PushedAt.HasValue) return false;
                var span = DateTime.UtcNow - PushedAt.Value.ToUniversalTime();
                return span.TotalDays < 1 && span.TotalDays >= 0;
            }
        }

        /// <summary>
        /// 뉴스 기사용 간결하고 깔끔한 발행 시간 표기
        /// </summary>
        public string TimeFormatted
        {
            get
            {
                if (!PushedAt.HasValue) return "정보 없음";
                var span = DateTime.UtcNow - PushedAt.Value.ToUniversalTime();
                if (span.TotalMinutes < 1) return "방금 전";
                if (span.TotalHours < 1) return $"{(int)span.TotalMinutes}분 전";
                if (span.TotalDays < 1) return $"{(int)span.TotalHours}시간 전";
                if (span.TotalDays < 2) return "어제";
                if (span.TotalDays < 7) return $"{(int)span.TotalDays}일 전";
                return PushedAt.Value.ToString("yyyy-MM-dd");
            }
        }

        /// <summary>
        /// 1줄 요약과 2줄 활용 포인트를 종합한 전체 해설 문구
        /// </summary>
        public string DisplayFullSummary
        {
            get
            {
                if (string.IsNullOrWhiteSpace(AiSummary) && string.IsNullOrWhiteSpace(PracticalTip))
                    return Description;

                if (string.IsNullOrWhiteSpace(PracticalTip))
                    return AiSummary;

                string tipPrefix = ItemType == TrendItemType.NewsArticle ? "💡 [시사점]" : "🚀 [활용]";
                return $"📌 {AiSummary}\r\n\r\n{tipPrefix} {PracticalTip}";
            }
        }
    }

    public class CustomKeywordItem
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty; // 화면 표시명 (예: "서바이벌 예능")
        public string RawKeyword { get; set; } = string.Empty;  // 사용자가 입력한 검색어 (예: "서바이벌+국내")
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public override string ToString() => $"{DisplayName} ({RawKeyword})";
    }

    public class AiTrendCategory
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public CategoryKind Kind { get; set; } = CategoryKind.GitHub;
        public bool IsCustom { get; set; } = false;
        public string RawKeyword { get; set; } = string.Empty;
        public string Query { get; set; } = string.Empty;
        public Func<string> DynamicQuery { get; set; }

        public string GetEffectiveQuery()
        {
            if (DynamicQuery != null) return DynamicQuery();
            return Query;
        }

        public override string ToString() => DisplayName;
    }

    public class AiTrendFetchResult
    {
        public List<AiRepoItem> Items { get; set; } = new List<AiRepoItem>();
        public bool IsLiveSuccess { get; set; } // GitHub 실시간 통신 성공 여부
        public bool IsFallbackCache { get; set; } // 통신 실패로 이전 캐시를 대체 표시 중인지 여부
        public bool IsStarterPack { get; set; } // 인터넷 불가/캐시 없음으로 내장 스타터 팩 표시 여부
        public bool IsAiQuotaExceeded { get; set; } // Gemini 429 무료 쿼터 초과 여부
        public bool IsFullyEnriched { get; set; } // AI 2줄 요약이 이미 모두 완성되었는지 여부 (캐시 히트 등)
        public DateTime DataTimestamp { get; set; } // 표시 데이터 기준 일시 (캐시 생성 시점 또는 현재)
        public string FailureReason { get; set; } = string.Empty; // 실패 원인 설명
        public string DisplayStatus { get; set; } = string.Empty; // 화면 표시 문구
    }
}
