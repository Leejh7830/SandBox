using System;
using System.Collections.Generic;

namespace GateHelper.Dashboard
{
    /// <summary>
    /// _meta/dashboard/dashboard_stats.dat (AES-256 암호화)에 영구 보존되는 통계 모델
    /// </summary>
    public class DashboardStats
    {
        // 1. 실행 및 세션
        public int TotalLaunchCount { get; set; } = 0;
        public double TotalUptimeMinutes { get; set; } = 0;
        public DateTime FirstLaunchDate { get; set; } = DateTime.Now;
        public DateTime LastLaunchDate { get; set; } = DateTime.Now;

        // 2. 업무 및 서버 접속
        public int TotalConnectAttempts { get; set; } = 0;
        public int AutoLoginSuccessCount { get; set; } = 0;
        public int AutoLoginFailureCount { get; set; } = 0;
        public int ScreenUnlockCount { get; set; } = 0;
        public Dictionary<string, int> ServerAccessCounts { get; set; } = new Dictionary<string, int>();
        public int[] HourlyAccessCounts { get; set; } = new int[24];
        public Dictionary<string, int[]> DailyHourlyAccessCounts { get; set; } = new Dictionary<string, int[]>();

        /// <summary>
        /// GateHelper가 절약해준 예상 총 업무 시간 (초)
        /// (OTP 자동 로그인 1회당 25초, 서버 직접 접속 1회당 15초, 화면 잠금 해제 1회당 15초 절약 기준)
        /// </summary>
        public long GetEstimatedSavedSeconds()
        {
            return (AutoLoginSuccessCount * 25L) + (TotalConnectAttempts * 15L) + (ScreenUnlockCount * 15L);
        }

        public string GetEstimatedSavedFormatted()
        {
            long sec = GetEstimatedSavedSeconds();
            if (sec <= 0) return "0분";
            long hours = sec / 3600;
            long mins = (sec % 3600) / 60;
            if (hours > 0)
            {
                return $"{hours}시간 {mins:D2}분";
            }
            return $"{Math.Max(1, mins)}분";
        }

        // 3. 샌드박스 클리어 기록
        public int BitFlipWins { get; set; } = 0;
        public int SignalLinkWins { get; set; } = 0;
        public int MysterySolves { get; set; } = 0;
        public int MysteryAttempts { get; set; } = 0;
        public int AiTrendScraps { get; set; } = 0;
        public int CoreFrontWins { get; set; } = 0;
        public List<string> CoreFrontUnlockedAchievements { get; set; } = new List<string>();

        // 4. 에러 카운터 및 확인(Acknowledge) 상태 관리
        public int TotalErrorCount { get; set; } = 0;
        public int TotalCriticalCount { get; set; } = 0;
        public DateTime? LastErrorAcknowledgedTime { get; set; } = null;
        public int AcknowledgedErrorCount { get; set; } = 0;
        public int AcknowledgedCriticalCount { get; set; } = 0;

        /// <summary>
        /// 사용자가 아직 확인(클리어)하지 않은 신규 에러/크리티컬 발생 건수
        /// </summary>
        public int GetUnacknowledgedErrorCount()
        {
            int total = TotalErrorCount + TotalCriticalCount;
            int acked = AcknowledgedErrorCount + AcknowledgedCriticalCount;
            return Math.Max(0, total - acked);
        }
    }

    /// <summary>
    /// 실시간 프로세스 및 하드웨어 점유율 스냅샷
    /// </summary>
    public class SystemResourceSnapshot
    {
        public double ProcessRamMb { get; set; }
        public double ProcessPrivateRamMb { get; set; }
        public double ProcessCpuPercent { get; set; }
        public int ThreadCount { get; set; }
        public int HandleCount { get; set; }

        public long NetworkPingMs { get; set; } = -1;
        public string NetworkStatusText { get; set; } = "측정 대기";

        public bool ChromeDriverRunning { get; set; }
        public bool IsSeleniumDriverAlive { get; set; }

        public TimeSpan SessionUptime { get; set; }
    }

    /// <summary>
    /// 최근 크리티컬/에러 로그 항목
    /// </summary>
    public class DashboardLogItem
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; }
        public string Caller { get; set; }
        public string Message { get; set; }
        public bool IsAcknowledged { get; set; } = false;
    }

    /// <summary>
    /// 최근 7일 주간 활동 리듬 및 오늘 타임라인 렌더링용 모델
    /// </summary>
    public class DailyActivityItem
    {
        public DateTime Date { get; set; }
        public string DateLabel { get; set; }
        public int[] HourlyCounts { get; set; } = new int[24];
        public int TotalCount { get; set; }
        public bool IsToday { get; set; }
    }

    #region [ Steam-Style Achievement Models ]

    public enum AchievementCategory
    {
        BitFlip,
        SignalLink,
        MysteryTime,
        AiTrend,
        CoreFront,
        Special
    }

    public class AchievementItem
    {
        public string Id { get; set; }
        public AchievementCategory Category { get; set; }
        public string CategoryName { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int Points { get; set; }
        public int RequiredCount { get; set; }
        public int CurrentCount { get; set; }
        public bool IsUnlocked => CurrentCount >= RequiredCount;
        public float ProgressPercent => RequiredCount <= 0 ? 100f : Math.Min(100f, (float)CurrentCount / RequiredCount * 100f);
        public string IconSymbol { get; set; } = "🏆";
    }

    public class MasteryTierInfo
    {
        public int TotalEarnedPoints { get; set; }
        public int MaxPoints { get; set; }
        public int TierLevel { get; set; } // 1 ~ 5
        public string TierName { get; set; }
        public string TierTitle { get; set; }
        public System.Drawing.Color AccentColor { get; set; }
        public float ProgressPercent => MaxPoints <= 0 ? 0f : Math.Min(100f, (float)TotalEarnedPoints / MaxPoints * 100f);
        public int UnlockedCount { get; set; }
        public int TotalCount { get; set; }
    }

    public class CategoryTierInfo
    {
        public AchievementCategory Category { get; set; }
        public string CategoryName { get; set; }
        public string CurrentTierName { get; set; }
        public int CurrentLevel { get; set; } // 0 ~ 4
        public int CurrentCount { get; set; }
        public System.Drawing.Color TierColor { get; set; }
    }

    #endregion
}
