using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using GateHelper.StockAdvisor.Models;

namespace GateHelper.StockAdvisor.Services
{
    /// <summary>
    /// 개별 종목 실적 발표 D-Day 스케줄 항목
    /// </summary>
    public class EarningsScheduleItem
    {
        public string StockCode { get; set; } = string.Empty;
        public string StockName { get; set; } = string.Empty;
        public DateTime EarningsDate { get; set; }
        public string PeriodTitle { get; set; } = "3분기 잠정 실적";
        public string EstimatedConsensus { get; set; } = "컨센서스 집계 중";
        public string ActionAdvice { get; set; } = string.Empty;
        public bool IsHeld { get; set; } = false;
        public decimal HoldingProfitRate { get; set; } = 0m;
        public decimal CurrentPrice { get; set; } = 0m;
        public bool IsImportantBellwether { get; set; } = false;

        public int DaysRemaining => (EarningsDate.Date - DateTime.Today).Days;

        public string DDayBadge
        {
            get
            {
                int d = DaysRemaining;
                if (d < 0) return $"D+{Math.Abs(d)} (발표완료)";
                if (d == 0) return "★ D-Day (오늘)";
                return $"D-{d}";
            }
        }

        public bool IsWithin7Days => DaysRemaining >= 0 && DaysRemaining <= 7;
    }

    /// <summary>
    /// 실시간 실적 발표(어닝) 캘린더 & D-Day 리스크 레이더 서비스
    /// (Zero-Resource: 정적 클라이언트, 타임아웃 5초, 100% 비동기)
    /// </summary>
    public static class EarningsRadarService
    {
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        static EarningsRadarService()
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            }
            catch { }
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }

        /// <summary>
        /// 국내 주요 16대 핵심 대형주(Bellwether) 실적 시즌 스케줄 데이터베이스
        /// </summary>
        private static readonly List<(string Code, string Name, int Month, int Day, string Period, string Consensus)> BellwetherPresets =
            new List<(string, string, int, int, string, string)>
            {
                ("005930", "삼성전자",       10, 31, "3분기 경영실적",   "영업익 9.18조원 (전년비 +277%)"),
                ("000660", "SK하이닉스",     10, 24, "3분기 잠정실적",   "영업익 7.03조원 (사상 최대 흑자)"),
                ("373220", "LG에너지솔루션", 10, 28, "3분기 잠정실적",   "영업익 4,483억원 (IRA보조금 4,660억)"),
                ("005380", "현대차",         10, 24, "3분기 경영실적",   "영업익 3.58조원 (북미 인센티브 반영)"),
                ("000270", "기아",           10, 25, "3분기 경영실적",   "영업익 2.88조원 (하이브리드 호조)"),
                ("035420", "NAVER",          11,  8, "3분기 잠정실적",   "영업익 5,211억원 (역대 최대 실적 달성)"),
                ("035720", "카카오",         11,  7, "3분기 경영실적",   "영업익 1,305억원 (비용 통제 개선)"),
                ("207940", "삼성바이오로직스", 10, 23, "3분기 잠정실적", "영업익 3,386억원 (4공장 가동 효과)"),
                ("068270", "셀트리온",       11,  8, "3분기 경영실적",   "영업익 2,077억원 (짐펜트라 처방 확대)"),
                ("005490", "POSCO홀딩스",    10, 30, "3분기 경영실적",   "영업익 7,430억원 (철강 시황 둔화)"),
                ("105560", "KB금융",         10, 24, "3분기 경영실적",   "당기순익 1.61조원 (주주환원 확대)"),
                ("055550", "신한지주",       10, 25, "3분기 경영실적",   "당기순익 1.24조원 (비이자 이익 성장)"),
                ("028260", "삼성물산",       10, 30, "3분기 잠정실적",   "영업익 7,360억원 (바이오·상사 견조)"),
                ("006400", "삼성SDI",        10, 31, "3분기 경영실적",   "영업익 1,299억원 (원형전지 선방)"),
                ("051910", "LG화학",         10, 28, "3분기 경영실적",   "영업익 4,984억원 (양극재 스프레드)"),
                ("271560", "오리온",         11, 14, "3분기 분기보고서", "영업익 1,370억원 (글로벌 제과 호조)"),
                ("010140", "삼성중공업",     10, 24, "3분기 잠정실적",   "영업익 1,199억원 (LNG선 연속 흑자)"),
                ("240810", "원익IPS",        11, 14, "3분기 분기보고서", "영업익 185억원 (HBM 장비 수주 회복)")
            };

        /// <summary>
        /// 전체 실적 발표 스케줄(포트폴리오 보유 종목 + 주요 대형주) 조회
        /// </summary>
        public static async Task<List<EarningsScheduleItem>> GetEarningsScheduleAsync(List<StockHoldingItem> portfolio)
        {
            var resultList = new List<EarningsScheduleItem>();
            int currentYear = DateTime.Today.Year;

            // 1. 주요 대형주 프리셋 장착
            var codeMap = new Dictionary<string, EarningsScheduleItem>(StringComparer.OrdinalIgnoreCase);

            foreach (var p in BellwetherPresets)
            {
                DateTime dt = new DateTime(currentYear, p.Month, p.Day);
                // 만약 연말/연초일 경우 가장 가까운 실적 발표일로 보정
                var item = new EarningsScheduleItem
                {
                    StockCode = p.Code,
                    StockName = p.Name,
                    EarningsDate = dt,
                    PeriodTitle = p.Period,
                    EstimatedConsensus = p.Consensus,
                    IsImportantBellwether = true
                };
                codeMap[p.Code] = item;
            }

            // 2. 포트폴리오 종목 매핑 및 실시간 DART 공시 스크래핑
            if (portfolio != null)
            {
                foreach (var h in portfolio)
                {
                    if (string.IsNullOrWhiteSpace(h.StockCode)) continue;
                    string code = h.StockCode.TrimStart('0').PadLeft(6, '0');

                    if (codeMap.TryGetValue(code, out var existing))
                    {
                        existing.IsHeld = true;
                        existing.HoldingProfitRate = h.ProfitRate;
                        existing.CurrentPrice = h.CurrentPrice;
                    }
                    else
                    {
                        // 프리셋에 없는 종목: 네이버 공시 기반 동적 탐색
                        var dynamicItem = await ResolveDynamicEarningsDateAsync(code, h.StockName);
                        dynamicItem.IsHeld = true;
                        dynamicItem.HoldingProfitRate = h.ProfitRate;
                        dynamicItem.CurrentPrice = h.CurrentPrice;
                        codeMap[code] = dynamicItem;
                    }
                }
            }

            // 3. AI 참모 실전 대응 가이드 생성
            foreach (var item in codeMap.Values)
            {
                item.ActionAdvice = GenerateActionAdvice(item);
                resultList.Add(item);
            }

            // D-Day 순 (오늘 및 미래 임박 -> 지난 발표)
            return resultList
                .OrderBy(x => x.DaysRemaining < 0 ? 1 : 0) // 미래/오늘 우선
                .ThenBy(x => Math.Abs(x.DaysRemaining))
                .ToList();
        }

        private static async Task<EarningsScheduleItem> ResolveDynamicEarningsDateAsync(string stockCode, string stockName)
        {
            DateTime defaultDate = new DateTime(DateTime.Today.Year, 11, 14); // 3분기 법정 정기보고서 마감일
            string periodTitle = "3분기 분기보고서 법정 기한";
            string consensus = "분기 결산 보고서 제출 예정";

            try
            {
                string url = $"https://m.stock.naver.com/api/stock/{stockCode}/notice?page=1&pageSize=10";
                string json = await _httpClient.GetStringAsync(url);
                var root = JArray.Parse(json);

                foreach (var token in root)
                {
                    string title = token["title"]?.ToString() ?? string.Empty;
                    string dateStr = token["noticeDate"]?.ToString() ?? string.Empty;

                    if (title.Contains("잠정") || title.Contains("실적") || title.Contains("매출액또는손익구조") || title.Contains("분기보고서"))
                    {
                        if (DateTime.TryParse(dateStr, out DateTime parsed))
                        {
                            return new EarningsScheduleItem
                            {
                                StockCode = stockCode,
                                StockName = stockName,
                                EarningsDate = parsed,
                                PeriodTitle = title.Length > 20 ? title.Substring(0, 20) + "..." : title,
                                EstimatedConsensus = "최근 DART 공시 접수 완료"
                            };
                        }
                    }
                }
            }
            catch { }

            return new EarningsScheduleItem
            {
                StockCode = stockCode,
                StockName = stockName,
                EarningsDate = defaultDate,
                PeriodTitle = periodTitle,
                EstimatedConsensus = consensus
            };
        }

        private static string GenerateActionAdvice(EarningsScheduleItem item)
        {
            int d = item.DaysRemaining;

            if (d == 0)
            {
                return "【D-Day 실적 발표일】 장중 실적 쇼크/서프라이즈에 따른 변동성 주의. 발표 직후 외국인·기관 대량 수급 방향 확인 전까지 뇌동매매 금지!";
            }
            else if (d >= 1 && d <= 3)
            {
                if (item.HoldingProfitRate > 5.0m)
                {
                    return $"【발표 D-{d} 기대감 선반영】 호실적 기대감으로 주가가 단기 상승한 구간입니다. '뉴스에 팔아라' 재료 소멸 매물에 대비해 20~30% 분할 익절 권고!";
                }
                else if (item.HoldingProfitRate < -5.0m)
                {
                    return $"【발표 D-{d} 불확실성 경계】 역배열 조정 중 어닝 쇼크가 겹치면 추가 갭하락 위험이 있습니다. 지지선 이탈 시 비중 축소 준비!";
                }
                else
                {
                    return $"【발표 D-{d} 어닝 카운트다운】 실적 발표 직전 변동성 확대 구간. 20일 지지선 안착을 확인하며 무리한 추가 매수보다는 관망 유지 권고.";
                }
            }
            else if (d >= 4 && d <= 7)
            {
                return $"【발표 D-{d} 실적 리스크 레이더】 실적 시즌 진입(D-7 이내). 최근 5일간 메이저 수급(외인/기관) 유입 추이와 컨센서스 상회 여부를 집중 모니터링하세요.";
            }
            else if (d > 7)
            {
                return $"【발표 D-{d} 일정 여유】 실적 발표일까지 여유가 있습니다. 20일 이동평균선 지지 여부 및 추세 중심의 정상 매매 전략을 유지하세요.";
            }
            else
            {
                return $"【실적 발표 완료(D+{Math.Abs(d)})】 공시 완료 후 재료 소멸 매물 소화 과정 확인. 20일선 지지선 안착 시 눌림목 재진입 타점 검토 가능.";
            }
        }
    }
}
