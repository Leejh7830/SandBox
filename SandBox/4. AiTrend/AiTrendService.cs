using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GateHelper.AiTrend
{
    public class AiTrendService
    {
        private static readonly string CacheDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_meta", "cache");
        private static DateTime _lastRequestTime = DateTime.MinValue;
        private const int MinRequestIntervalSeconds = 8; // 새로고침 연타 방지 쿨타임 (초)

        private static readonly string FavoritesFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_meta", "favorite_repos.json");
        private static readonly string CustomKeywordsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_meta", "custom_keywords.json");

        public bool HasApiKey => !string.IsNullOrWhiteSpace(GetGeminiApiKey());

        public string GetGeminiApiKey()
        {
            try
            {
                return Util_Option.LoadUserOptions()?.GeminiApiKey ?? "";
            }
            catch
            {
                return "";
            }
        }

        #region [Cache Housekeeping & Auto-Purge]

        /// <summary>
        /// _meta/cache/ 디렉토리 내의 오래된 임시 캐시 파일(ai_*.json)을 안전하게 자동 정리합니다.
        /// (설정 파일, 즐겨찾기, 커스텀 키워드 등 사용자 영구 데이터는 절대 건드리지 않습니다.)
        /// </summary>
        public static int PurgeStaleCaches(int days = 3)
        {
            try
            {
                if (!Directory.Exists(CacheDir)) return 0;

                int deletedCount = 0;
                var dirInfo = new DirectoryInfo(CacheDir);
                var files = dirInfo.GetFiles("ai_*.json");

                DateTime threshold = DateTime.Now.AddDays(-days);

                foreach (var file in files)
                {
                    try
                    {
                        // 3일 이상 경과한 캐시 파일만 삭제
                        if (file.LastWriteTime < threshold)
                        {
                            file.Delete();
                            deletedCount++;
                        }
                    }
                    catch
                    {
                        // 다른 스레드가 읽기/쓰기 중인 파일은 에러 없이 조용히 패스 (FileLock 방어)
                    }
                }

                if (deletedCount > 0)
                {
                    LogManager.LogMessage($"[AiTrend] 3일 이상 경과한 오래된 캐시 파일 {deletedCount}개 자동 정리 완료", LogManager.Level.Info);
                }

                return deletedCount;
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "AiTrend PurgeStaleCaches Failed");
                return 0;
            }
        }

        #endregion

        #region [Sequential Background Preload Queue]

        private static bool _isPreloadRunning = false;
        private static readonly object _preloadLock = new object();
        private static CancellationTokenSource _globalPreloadCts = new CancellationTokenSource();

        static AiTrendService()
        {
            try
            {
                // 프로그램 종료 시 백그라운드 프리로딩 큐를 즉각 취소(Cancel)하여 좀비 프로세스 방지
                System.Windows.Forms.Application.ApplicationExit += (s, e) => CancelAllPreloads();
            }
            catch { }
        }

        /// <summary>
        /// 실행 중인 모든 백그라운드 AI 프리로딩 작업을 즉각 안전하게 중단합니다.
        /// (프로그램 종료 시 호출되어 좀비 프로세스 잔류를 100% 방지)
        /// </summary>
        public static void CancelAllPreloads()
        {
            try
            {
                _globalPreloadCts?.Cancel();
                _globalPreloadCts = new CancellationTokenSource();
            }
            catch { }
        }

        /// <summary>
        /// 모든 AI 트렌드 카테고리를 순차적 백그라운드 유휴 큐로 안전하게 미리 분석/캐싱합니다.
        /// (관심 키워드 -> 뉴스 -> 오픈소스 순서, Gemini 15 RPM 한도 방어, 캐시 존재 시 API 호출 100% 스킵)
        /// </summary>
        public async Task PreloadAllCategoriesAsync(CancellationToken ct, Func<bool> canProceed = null, Action<string, int, int> onProgress = null)
        {
            // 1. 중복 실행 방지
            lock (_preloadLock)
            {
                if (_isPreloadRunning) return;
                _isPreloadRunning = true;
            }

            // 외부 토큰과 전역 종료 토큰을 연결
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _globalPreloadCts.Token))
            {
                var effectiveToken = linkedCts.Token;

                try
                {
                    // 2. Gemini API Key 미등록 시 백그라운드 프리로딩 즉시 스킵 (캐시 오염 방지)
                    if (!HasApiKey) return;

                var allCategories = GetCategories();
                var targets = new List<AiTrendCategory>();

                // 우선순위 정렬: 1순위 커스텀 관심 키워드
                targets.AddRange(allCategories.FindAll(c => c.Kind == CategoryKind.CustomKeyword));
                // 2순위: 실시간 뉴스 (즐겨찾기 보관함 제외)
                targets.AddRange(allCategories.FindAll(c => c.Kind == CategoryKind.NewsRss && c.Id != "favorites_news"));
                // 3순위: GitHub 오픈소스 트렌드 (즐겨찾기 보관함 제외)
                targets.AddRange(allCategories.FindAll(c => c.Kind == CategoryKind.GitHub && c.Id != "favorites_github"));

                int totalCount = targets.Count;
                int processedCount = 0;
                int consecutiveFailures = 0;

                foreach (var cat in targets)
                {
                    if (effectiveToken.IsCancellationRequested) break;

                    // 외부 조건 검사 (서버 접속, 자동 로그인, UI 조작 등 메인 작업이 끝날 때까지 1초씩 안전하게 대기/양보)
                    while (canProceed != null && !canProceed())
                    {
                        if (effectiveToken.IsCancellationRequested) break;
                        await Task.Delay(1000, effectiveToken);
                    }

                    if (effectiveToken.IsCancellationRequested) break;

                    processedCount++;
                    onProgress?.Invoke(cat.DisplayName, processedCount, totalCount);

                    // Zero-Request Cache Skip 검사
                    string queryStr = cat.GetEffectiveQuery();
                    string queryHash = GetShortHash(queryStr);
                    string filePrefix = cat.Kind == CategoryKind.GitHub ? "ai_trend" : "ai_news";
                    string cacheFilePath = Path.Combine(CacheDir, $"{filePrefix}_{cat.Id}_{queryHash}.json");
                    double validHours = cat.Kind == CategoryKind.GitHub ? 3.0 : 1.0;

                    if (File.Exists(cacheFilePath))
                    {
                        var (cachedItems, fTime) = TryReadCache(cacheFilePath);
                        if (cachedItems != null && cachedItems.Count > 0 && (DateTime.Now - fTime).TotalHours < validHours)
                        {
                            bool allEnriched = cachedItems.TrueForAll(x => !string.IsNullOrWhiteSpace(x.PracticalTip) && !string.IsNullOrWhiteSpace(x.DetailStory));
                            if (allEnriched)
                            {
                                // 이미 3단 심층 브리핑까지 완비된 유효 캐시가 있으므로 네트워크 요청 0건, 즉시 통과!
                                continue;
                            }
                        }
                    }

                    // 캐시가 없거나 불완전한 경우 수집 및 AI 요약 비동기 진행
                    try
                    {
                        var fetchResult = await FetchRepositoriesAsync(cat, forceRefresh: false);

                        if (fetchResult != null && fetchResult.Items != null && fetchResult.Items.Count > 0)
                        {
                            consecutiveFailures = 0; // 성공 시 실패 카운트 리셋

                            // 만약 아직 Gemini 3단 브리핑이 안 채워져 있다면 생성
                            if (!fetchResult.IsFullyEnriched)
                            {
                                if (effectiveToken.IsCancellationRequested) break;

                                var status = await EnrichRepositoriesAsync(cat, fetchResult.Items);
                                if (status == AiEnrichStatus.QuotaExceeded)
                                {
                                    // 쿼터 초과 시 15초 추가 휴식
                                    await Task.Delay(15000, effectiveToken);
                                }
                                else
                                {
                                    // Gemini 15 RPM 한도 완벽 방어를 위한 5초 안전 쿨다운
                                    await Task.Delay(5000, effectiveToken);
                                }
                            }
                        }
                        else
                        {
                            consecutiveFailures++;
                        }
                    }
                    catch (WebException)
                    {
                        consecutiveFailures++;
                    }
                    catch (Exception)
                    {
                        consecutiveFailures++;
                    }

                    // 서킷 브레이커: 연속 2회 이상 네트워크 실패 시 외부 인터넷 불가 환경으로 판단하고 즉시 중단
                    if (consecutiveFailures >= 2)
                    {
                        LogManager.LogMessage("[AiTrend] 네트워크 연결 불가로 백그라운드 프리로딩 큐를 안전하게 조기 종료합니다.", LogManager.Level.Warning);
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 취소 요청은 정상 종료 처리
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "AiTrend PreloadAllCategoriesAsync Error");
            }
            finally
            {
                lock (_preloadLock)
                {
                    _isPreloadRunning = false;
                }
            }
            }
        }

        #endregion

        #region [Favorites Management]

        public List<AiRepoItem> LoadFavorites()
        {
            try
            {
                if (!File.Exists(FavoritesFilePath)) return new List<AiRepoItem>();
                string json = File.ReadAllText(FavoritesFilePath, Encoding.UTF8);
                var items = JsonConvert.DeserializeObject<List<AiRepoItem>>(json) ?? new List<AiRepoItem>();
                int rank = 1;
                foreach (var it in items)
                {
                    it.Rank = rank++;
                    it.IsFavorite = true;
                }
                return items;
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "AiTrend LoadFavorites Failed");
                return new List<AiRepoItem>();
            }
        }

        public bool IsFavorite(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return false;
            var list = LoadFavorites();
            return list.Exists(x => string.Equals(x.FullName, fullName, StringComparison.OrdinalIgnoreCase));
        }

        public bool ToggleFavorite(AiRepoItem item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.FullName)) return false;

            try
            {
                var list = LoadFavorites();
                int idx = list.FindIndex(x => string.Equals(x.FullName, item.FullName, StringComparison.OrdinalIgnoreCase));

                bool nowFavorite;
                if (idx >= 0)
                {
                    list.RemoveAt(idx);
                    nowFavorite = false;
                }
                else
                {
                    item.IsFavorite = true;
                    list.Insert(0, item);
                    nowFavorite = true;
                }

                string dir = Path.GetDirectoryName(FavoritesFilePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                string json = JsonConvert.SerializeObject(list, Formatting.Indented);
                File.WriteAllText(FavoritesFilePath, json, Encoding.UTF8);

                item.IsFavorite = nowFavorite;
                return nowFavorite;
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "AiTrend ToggleFavorite Failed");
                return false;
            }
        }

        #endregion

        #region [Custom Keywords Management]

        /// <summary>
        /// 사용자가 입력한 자연어/조합 키워드를 구글 뉴스 RSS가 가장 정확하게 이해하는 최적의 쿼리로 스마트 변환
        /// (지나치게 좁은 날짜 제한 when:3d를 강제하지 않아 5일 전~수주 전 주요 보도 기사도 온전히 수집)
        /// </summary>
        public static string BuildSmartNewsQuery(string rawInput)
        {
            if (string.IsNullOrWhiteSpace(rawInput)) return "";
            string clean = rawInput.Trim();

            // 1. 쉼표(,) 구분: 여러 프로그램/키워드 중 하나라도 해당되는 OR 묶음 (예: "피지컬100, 데블스플랜")
            if (clean.Contains(","))
            {
                var parts = clean.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                var tokens = new List<string>();
                foreach (var p in parts)
                {
                    string t = p.Trim();
                    if (string.IsNullOrEmpty(t)) continue;
                    tokens.Add(t.Contains(" ") ? $"\"{t}\"" : t);
                }
                if (tokens.Count > 0)
                {
                    return string.Join(" OR ", tokens);
                }
            }

            // 2. 더하기(+) 구분: 필수 포함 조합 검색 (예: 서바이벌+예능)
            if (clean.Contains("+"))
            {
                var parts = clean.Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
                var tokens = new List<string>();
                foreach (var p in parts)
                {
                    string t = p.Trim();
                    if (!string.IsNullOrEmpty(t)) tokens.Add(t);
                }
                if (tokens.Count > 0)
                {
                    return string.Join("+", tokens);
                }
            }

            // 3. '데블스플랜' 등 띄어쓰기 여부에 따른 누락 방지 (붙여쓰기 / 띄어쓰기 동시 검색)
            if (clean.Equals("데블스플랜", StringComparison.OrdinalIgnoreCase))
            {
                return "데블스플랜 OR \"데블스 플랜\"";
            }

            // 4. 공백 및 일반 단어: 구글 뉴스의 형태소 분석 엔진이 가장 자연스럽게 연관 기사를 찾도록 그대로 전달 (예: "서바이벌 예능")
            return clean;
        }

        public List<CustomKeywordItem> LoadCustomKeywords()
        {
            try
            {
                if (!File.Exists(CustomKeywordsFilePath)) return new List<CustomKeywordItem>();
                string json = File.ReadAllText(CustomKeywordsFilePath, Encoding.UTF8);
                return JsonConvert.DeserializeObject<List<CustomKeywordItem>>(json) ?? new List<CustomKeywordItem>();
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "AiTrend LoadCustomKeywords Failed");
                return new List<CustomKeywordItem>();
            }
        }

        public bool SaveCustomKeywords(List<CustomKeywordItem> list)
        {
            try
            {
                string dir = Path.GetDirectoryName(CustomKeywordsFilePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                string json = JsonConvert.SerializeObject(list ?? new List<CustomKeywordItem>(), Formatting.Indented);
                File.WriteAllText(CustomKeywordsFilePath, json, Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "AiTrend SaveCustomKeywords Failed");
                return false;
            }
        }

        public CustomKeywordItem AddCustomKeyword(string displayName, string rawKeyword)
        {
            if (string.IsNullOrWhiteSpace(rawKeyword)) return null;

            string cleanKeyword = rawKeyword.Trim();
            string cleanName = string.IsNullOrWhiteSpace(displayName) ? cleanKeyword : displayName.Trim();

            var list = LoadCustomKeywords();
            var existing = list.Find(x => string.Equals(x.RawKeyword, cleanKeyword, StringComparison.OrdinalIgnoreCase)
                                       || string.Equals(x.DisplayName, cleanName, StringComparison.OrdinalIgnoreCase));
            if (existing != null) return existing;

            var newItem = new CustomKeywordItem
            {
                Id = Guid.NewGuid().ToString("N").Substring(0, 8),
                DisplayName = cleanName,
                RawKeyword = cleanKeyword,
                CreatedAt = DateTime.Now
            };

            list.Add(newItem);
            SaveCustomKeywords(list);
            return newItem;
        }

        public bool DeleteCustomKeyword(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;

            try
            {
                var list = LoadCustomKeywords();
                int idx = list.FindIndex(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0)
                {
                    list.RemoveAt(idx);
                    SaveCustomKeywords(list);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "AiTrend DeleteCustomKeyword Failed");
                return false;
            }
        }

        #endregion

        public List<AiTrendCategory> GetCategories()
        {
            var list = new List<AiTrendCategory>
            {
                // --- [오픈소스 AI & 개발 트렌드 (GitHub)] ---
                new AiTrendCategory
                {
                    Id = "trending",
                    DisplayName = "🔥 실시간 전체 급상승 (최근 30일)",
                    Kind = CategoryKind.GitHub,
                    DynamicQuery = () => $"created:>{DateTime.UtcNow.AddDays(-30):yyyy-MM-dd}&sort=stars&order=desc"
                },
                new AiTrendCategory
                {
                    Id = "ai_trending",
                    DisplayName = "🤖 최신 AI & 에이전트 핫 트렌드",
                    Kind = CategoryKind.GitHub,
                    DynamicQuery = () => $"ai+created:>{DateTime.UtcNow.AddMonths(-6):yyyy-MM-dd}&sort=stars&order=desc"
                },
                new AiTrendCategory
                {
                    Id = "productivity",
                    DisplayName = "💻 직장인 생산성 & 업무 자동화 꿀도구",
                    Kind = CategoryKind.GitHub,
                    Query = "productivity+OR+developer-tools+OR+workflow+stars:>2000&sort=stars&order=desc"
                },
                new AiTrendCategory
                {
                    Id = "local_ai",
                    DisplayName = "🏠 내 PC 로컬 AI (오프라인/사내보안)",
                    Kind = CategoryKind.GitHub,
                    Query = "ollama+OR+llama.cpp+OR+local-ai+OR+open-webui+stars:>1000&sort=stars&order=desc"
                },
                new AiTrendCategory
                {
                    Id = "speech_ai",
                    DisplayName = "🎙️ 음성인식 & 회의록 자동화 AI (STT)",
                    Kind = CategoryKind.GitHub,
                    Query = "whisper+OR+speech-to-text+OR+speech-recognition+stars:>1000&sort=stars&order=desc"
                },
                new AiTrendCategory
                {
                    Id = "top_stars",
                    DisplayName = "⭐ 역대 최고 스타 (All-Time Legends)",
                    Kind = CategoryKind.GitHub,
                    Query = "ai+stars:>10000&sort=stars&order=desc"
                },
                new AiTrendCategory
                {
                    Id = "favorites_github",
                    DisplayName = "⭐ [보관] 내가 찜한 오픈소스",
                    Kind = CategoryKind.GitHub,
                    Query = ""
                },

                // --- [실시간 핵심 뉴스 브리핑 (Google News RSS)] ---
                new AiTrendCategory
                {
                    Id = "news_tech",
                    DisplayName = "🌐 [뉴스] 글로벌 IT 테크 핵심 뉴스",
                    Kind = CategoryKind.NewsRss,
                    Query = "IT OR 테크 OR 인공지능 OR 빅테크 OR 반도체 when:2d"
                },
                new AiTrendCategory
                {
                    Id = "news_world_econ",
                    DisplayName = "🌍 [뉴스] 세계 경제 & 글로벌 마켓",
                    Kind = CategoryKind.NewsRss,
                    Query = "세계경제 OR 미국증시 OR 나스닥 OR 연준 OR 환율 when:2d"
                },
                new AiTrendCategory
                {
                    Id = "news_kr_econ",
                    DisplayName = "📈 [뉴스] 국내 경제 & 증시 브리핑",
                    Kind = CategoryKind.NewsRss,
                    Query = "국내경제 OR 코스피 OR 한국은행 OR 부동산 OR 금리 when:2d"
                },
                new AiTrendCategory
                {
                    Id = "news_sports",
                    DisplayName = "⚽ [뉴스] 스포츠 핫이슈 & 주요 경기",
                    Kind = CategoryKind.NewsRss,
                    Query = "손흥민 OR 프로야구 OR KBO OR 프리미어리그 OR 챔피언스리그 when:2d"
                },
                new AiTrendCategory
                {
                    Id = "news_esports",
                    DisplayName = "🎮 [뉴스] E스포츠 (LCK / 롤드컵)",
                    Kind = CategoryKind.NewsRss,
                    Query = "LCK OR 롤드컵 OR T1 OR 페이커 OR 젠지 OR 오버워치 when:2d"
                },
                new AiTrendCategory
                {
                    Id = "favorites_news",
                    DisplayName = "⭐ [보관] 내가 찜한 뉴스 (스크랩)",
                    Kind = CategoryKind.NewsRss,
                    Query = ""
                }
            };

            // --- [사용자 관심 키워드 (Custom Keywords)] ---
            var customs = LoadCustomKeywords();
            foreach (var kw in customs)
            {
                list.Add(new AiTrendCategory
                {
                    Id = $"custom_{kw.Id}",
                    DisplayName = $"📌 [관심] {kw.DisplayName}",
                    Kind = CategoryKind.CustomKeyword,
                    IsCustom = true,
                    RawKeyword = kw.RawKeyword,
                    Query = BuildSmartNewsQuery(kw.RawKeyword)
                });
            }

            return list;
        }

        public async Task<AiTrendFetchResult> FetchRepositoriesAsync(AiTrendCategory category, bool forceRefresh = false)
        {
            if (category == null)
            {
                return new AiTrendFetchResult
                {
                    DisplayStatus = "카테고리가 올바르지 않습니다.",
                    IsLiveSuccess = false
                };
            }

            // [오픈소스 찜 (즐겨찾기) 전용 분기 - GitHub 컬럼 헤더 자동 매핑]
            if (category.Id == "favorites_github")
            {
                var allFavs = LoadFavorites();
                var githubFavs = allFavs.FindAll(x => x.ItemType == TrendItemType.GitHubRepo);
                int rank = 1;
                foreach (var it in githubFavs) it.Rank = rank++;
                return new AiTrendFetchResult
                {
                    Items = githubFavs,
                    IsLiveSuccess = true,
                    IsFallbackCache = false,
                    DataTimestamp = DateTime.Now,
                    DisplayStatus = githubFavs.Count > 0
                        ? $"⭐ 내가 찜한 오픈소스 프로젝트 ({githubFavs.Count}개 보관 중)"
                        : "⭐ 찜한 오픈소스가 없습니다. 프로젝트 세부 정보창에서 '⭐ 찜하기'를 눌러 저장해보세요!"
                };
            }

            // [뉴스 찜/스크랩 (즐겨찾기) 전용 분기 - 뉴스 컬럼 헤더 자동 매핑]
            if (category.Id == "favorites_news" || category.Id == "favorites")
            {
                var allFavs = LoadFavorites();
                var newsFavs = allFavs.FindAll(x => x.ItemType == TrendItemType.NewsArticle);
                int rank = 1;
                foreach (var it in newsFavs) it.Rank = rank++;
                return new AiTrendFetchResult
                {
                    Items = newsFavs,
                    IsLiveSuccess = true,
                    IsFallbackCache = false,
                    DataTimestamp = DateTime.Now,
                    DisplayStatus = newsFavs.Count > 0
                        ? $"⭐ 내가 찜한 뉴스 기사 ({newsFavs.Count}건 보관 중)"
                        : "⭐ 찜한 뉴스가 없습니다. 기사 세부 정보창이나 카드뉴스에서 '⭐ 찜하기'를 눌러 저장해보세요!"
                };
            }

            // [실시간 뉴스 (Google News RSS) 및 사용자 커스텀 키워드 분기]
            if (category.Kind == CategoryKind.NewsRss || category.Kind == CategoryKind.CustomKeyword)
            {
                return await FetchNewsRssAsync(category, forceRefresh);
            }

            string queryStr = category.GetEffectiveQuery();
            string queryHash = GetShortHash(queryStr);
            string cacheFilePath = Path.Combine(CacheDir, $"ai_trend_{category.Id}_{queryHash}.json");

            // 1. 연타 방지 쿨타임 검사 (강제 새로고침 시 시간당 60회 쿼터 방어)
            if (forceRefresh)
            {
                double elapsedSeconds = (DateTime.Now - _lastRequestTime).TotalSeconds;
                if (elapsedSeconds < MinRequestIntervalSeconds)
                {
                    int remainSec = (int)Math.Ceiling(MinRequestIntervalSeconds - elapsedSeconds);
                    if (File.Exists(cacheFilePath))
                    {
                        var (cachedItems, fTime) = TryReadCache(cacheFilePath);
                        if (cachedItems != null && cachedItems.Count > 0)
                        {
                            EnsureAiSummaries(cachedItems);
                            SyncFavoriteStatus(cachedItems);
                            return new AiTrendFetchResult
                            {
                                Items = cachedItems,
                                IsLiveSuccess = false,
                                IsFallbackCache = false,
                                DataTimestamp = fTime,
                                DisplayStatus = $"요청 간격이 너무 빠릅니다. ({remainSec}초 후 가능) - 기존 데이터 표시"
                            };
                        }
                    }
                    return new AiTrendFetchResult
                    {
                        DisplayStatus = $"요청 간격이 너무 빠릅니다. ({remainSec}초 후 시도 가능)",
                        IsLiveSuccess = false
                    };
                }
            }

            // 2. 캐시가 유효하고 강제 새로고침이 아닌 경우 로컬 캐시 반환 (3시간 유효)
            if (!forceRefresh && File.Exists(cacheFilePath))
            {
                var (cachedItems, fTime) = TryReadCache(cacheFilePath);
                if (cachedItems != null && cachedItems.Count > 0 && (DateTime.Now - fTime).TotalHours < 3)
                {
                    EnsureAiSummaries(cachedItems);
                    SyncFavoriteStatus(cachedItems);
                    bool allEnriched = cachedItems.TrueForAll(x => (!string.IsNullOrWhiteSpace(x.PracticalTip) && !string.IsNullOrWhiteSpace(x.DetailStory)) || !HasApiKey);
                    return new AiTrendFetchResult
                    {
                        Items = cachedItems,
                        IsLiveSuccess = true,
                        IsFallbackCache = false,
                        IsFullyEnriched = allEnriched,
                        DataTimestamp = fTime,
                        DisplayStatus = $"📦 저장된 캐시 데이터 ({fTime:yyyy-MM-dd HH:mm} 저장본)"
                    };
                }
            }

            // 3. GitHub Search API 호출
            try
            {
                _lastRequestTime = DateTime.Now;
                string apiUrl = $"https://api.github.com/search/repositories?q={queryStr}&per_page=15";

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(apiUrl);
                request.Method = "GET";
                request.UserAgent = "GateHelper-DesktopApp";
                request.Accept = "application/vnd.github.v3+json";
                request.Timeout = 10000;

                string responseText = "";
                using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    responseText = await reader.ReadToEndAsync();
                }

                var rootObj = JObject.Parse(responseText);
                var itemsArray = rootObj["items"] as JArray;

                var resultList = new List<AiRepoItem>();
                if (itemsArray != null)
                {
                    var favSet = new HashSet<string>(LoadFavorites().ConvertAll(x => x.FullName), StringComparer.OrdinalIgnoreCase);
                    int rank = 1;
                    foreach (var item in itemsArray)
                    {
                        var repo = new AiRepoItem
                        {
                            Rank = rank++,
                            FullName = item["full_name"]?.ToString() ?? "",
                            RepoName = item["name"]?.ToString() ?? "",
                            Owner = item["owner"]?["login"]?.ToString() ?? "",
                            HtmlUrl = item["html_url"]?.ToString() ?? "",
                            Description = item["description"]?.ToString() ?? "설명이 제공되지 않은 저장소입니다.",
                            Stars = item["stargazers_count"]?.ToObject<int>() ?? 0,
                            Forks = item["forks_count"]?.ToObject<int>() ?? 0,
                            Language = item["language"]?.ToString() ?? "기타",
                            AiSummary = "",
                            PracticalTip = ""
                        };

                        // [최근 커밋(pushed_at) 파싱]
                        if (DateTime.TryParse(item["pushed_at"]?.ToString(), out DateTime dtPushed))
                        {
                            repo.PushedAt = dtPushed;
                        }

                        if (item["topics"] is JArray topicsArr)
                        {
                            foreach (var t in topicsArr)
                            {
                                if (t != null) repo.Topics.Add(t.ToString());
                            }
                        }

                        repo.IsFavorite = favSet.Contains(repo.FullName);
                        resultList.Add(repo);
                    }
                }

                // 4. 기존 캐시에서 이미 생성된 AI 요약이 있다면 먼저 채워넣음
                MergeExistingAiSummaries(resultList, cacheFilePath);

                string apiKey = GetGeminiApiKey();
                bool hasApiKey = !string.IsNullOrWhiteSpace(apiKey);
                if (!hasApiKey)
                {
                    foreach (var item in resultList)
                    {
                        if (string.IsNullOrWhiteSpace(item.AiSummary))
                        {
                            item.AiSummary = "[API Key 미등록] '🔑 API 설정'을 통해 키를 등록하면 한국어 핵심 요약이 제공됩니다.";
                        }
                    }
                }

                bool isFullyEnriched = resultList.Count > 0 && resultList.TrueForAll(x => (!string.IsNullOrWhiteSpace(x.PracticalTip) && !string.IsNullOrWhiteSpace(x.DetailStory)) || (!hasApiKey && !string.IsNullOrWhiteSpace(x.AiSummary)));

                // 5. 1단계 로컬 캐시 파일 저장 (AI 요약 전이라도 최신 저장소 목록 보존)
                SaveCache(cacheFilePath, resultList);

                string displayStatus = isFullyEnriched
                    ? $"✔ GitHub 실시간 최신 데이터 ({DateTime.Now:yyyy-MM-dd HH:mm})"
                    : $"✔ 저장소 {resultList.Count}개 수집 완료 (한국어 AI 요약 생성 중... ⏳)";

                return new AiTrendFetchResult
                {
                    Items = resultList,
                    IsLiveSuccess = true,
                    IsFallbackCache = false,
                    IsFullyEnriched = isFullyEnriched,
                    DataTimestamp = DateTime.Now,
                    DisplayStatus = displayStatus
                };
            }
            catch (WebException webEx)
            {
                LogManager.LogException(webEx, LogManager.Level.Warning, "AiTrend GitHub API Call Failed");
                string errorReason = AnalyzeWebException(webEx);

                // 네트워크/사내망/한도 오류 발생 시 기존 캐시가 있다면 오프라인 데이터로 안전 복구
                if (File.Exists(cacheFilePath))
                {
                    var (cachedItems, fTime) = TryReadCache(cacheFilePath);
                    if (cachedItems != null && cachedItems.Count > 0)
                    {
                        EnsureAiSummaries(cachedItems);
                        SyncFavoriteStatus(cachedItems);
                        return new AiTrendFetchResult
                        {
                            Items = cachedItems,
                            IsLiveSuccess = false,
                            IsFallbackCache = true,
                            DataTimestamp = fTime,
                            FailureReason = errorReason,
                            DisplayStatus = $"⚠️ [이전 저장본] {fTime:yyyy-MM-dd HH:mm} 기준 (실시간 갱신 실패: {errorReason})"
                        };
                    }
                }

                // 캐시조차 없는 경우 (최초 실행 + 오프라인/사내망 차단) -> 내장 스타터 팩(대표 AI 오픈소스 5선) 안전망 반환!
                var starterItems = GetStarterPackItems();
                SyncFavoriteStatus(starterItems);
                return new AiTrendFetchResult
                {
                    Items = starterItems,
                    IsLiveSuccess = false,
                    IsFallbackCache = false,
                    IsStarterPack = true,
                    DataTimestamp = DateTime.Now,
                    FailureReason = errorReason,
                    DisplayStatus = $"📡 [오프라인 스타터 팩] 인터넷 연결 대기 중 - 대표 AI 오픈소스 가이드 표시 (사유: {errorReason})"
                };
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Error, "AiTrend Fetch General Error");
                
                if (File.Exists(cacheFilePath))
                {
                    var (cachedItems, fTime) = TryReadCache(cacheFilePath);
                    if (cachedItems != null && cachedItems.Count > 0)
                    {
                        EnsureAiSummaries(cachedItems);
                        SyncFavoriteStatus(cachedItems);
                        return new AiTrendFetchResult
                        {
                            Items = cachedItems,
                            IsLiveSuccess = false,
                            IsFallbackCache = true,
                            DataTimestamp = fTime,
                            FailureReason = ex.Message,
                            DisplayStatus = $"⚠️ [이전 저장본] {fTime:yyyy-MM-dd HH:mm} 기준 (오류 발생: {ex.Message})"
                        };
                    }
                }

                var starterItems = GetStarterPackItems();
                SyncFavoriteStatus(starterItems);
                return new AiTrendFetchResult
                {
                    Items = starterItems,
                    IsLiveSuccess = false,
                    IsFallbackCache = false,
                    IsStarterPack = true,
                    DataTimestamp = DateTime.Now,
                    FailureReason = ex.Message,
                    DisplayStatus = $"📡 [오프라인 스타터 팩] 대표 AI 오픈소스 가이드 표시 (사유: {ex.Message})"
                };
            }
        }

        /// <summary>
        /// 인터넷이 불가하고 캐시도 없는 환경에서 사용자에게 흥미와 읽을거리를 제공하는 대표 AI 오픈소스 5선 기본 데이터
        /// </summary>
        public static List<AiRepoItem> GetStarterPackItems()
        {
            return new List<AiRepoItem>
            {
                new AiRepoItem
                {
                    Rank = 1,
                    FullName = "ollama / ollama",
                    RepoName = "ollama",
                    Owner = "ollama",
                    HtmlUrl = "https://github.com/ollama/ollama",
                    Stars = 98500,
                    Forks = 8200,
                    Language = "Go",
                    Topics = new List<string> { "llm", "local-ai", "llama3", "cli" },
                    PushedAt = DateTime.UtcNow.AddDays(-1),
                    Description = "Get up and running with Llama 3.3, Mistral, Gemma 2, and other large language models locally.",
                    AiSummary = "인터넷 없이 내 PC/서버에서 Llama 3 등 최신 AI를 1초 만에 실행하는 로컬 LLM의 제왕",
                    PracticalTip = "외부 데이터 유출 걱정 없는 사내 폐쇄망 보안 챗봇 및 업무 도우미 구축에 최적",
                    DetailStory = "메타의 Llama 3.3, 구글 Gemma 2, Mistral 등 최신 오픈가중치 모델을 명령어 한 줄로 즉시 다운로드하여 구동할 수 있습니다. GPU 하드웨어 가속(CUDA/Metal)을 자동 감지하며, OpenAI 호환 REST API를 제공하므로 기존 사내 시스템이나 웹 앱에 프라이빗 로컬 AI를 10분 만에 손쉽게 연동할 수 있습니다."
                },
                new AiRepoItem
                {
                    Rank = 2,
                    FullName = "openai / whisper",
                    RepoName = "whisper",
                    Owner = "openai",
                    HtmlUrl = "https://github.com/openai/whisper",
                    Stars = 73200,
                    Forks = 8900,
                    Language = "Python",
                    Topics = new List<string> { "speech-recognition", "audio", "transcription", "deep-learning" },
                    PushedAt = DateTime.UtcNow.AddDays(-3),
                    Description = "Robust Speech Recognition via Large-Scale Weak Supervision.",
                    AiSummary = "전 세계 100개 언어 음성을 사람 귀 수준으로 정밀하게 텍스트로 변환하는 최고 성능 오픈소스 STT",
                    PracticalTip = "사내 회의 녹음 파일, 세미나 영상, 고객 통화 음성을 자동으로 텍스트 회의록으로 변환",
                    DetailStory = "68만 시간 이상의 방대한 다국어 웹 오디오 데이터로 사전 학습된 최고 수준의 범용 음성인식 인공지능 모델입니다. 주변 소음이나 다양한 억양, 전문 기술 용어에도 놀라울 정도로 강건하며, 한국어 음성을 곧바로 영어로 번역하거나 문장 단위 타임스탬프를 자동 생성하는 기능을 갖추고 있습니다."
                },
                new AiRepoItem
                {
                    Rank = 3,
                    FullName = "AUTOMATIC1111 / stable-diffusion-webui",
                    RepoName = "stable-diffusion-webui",
                    Owner = "AUTOMATIC1111",
                    HtmlUrl = "https://github.com/AUTOMATIC1111/stable-diffusion-webui",
                    Stars = 142000,
                    Forks = 26500,
                    Language = "Python",
                    Topics = new List<string> { "image-generation", "stable-diffusion", "gradio", "webui" },
                    PushedAt = DateTime.UtcNow.AddDays(-4),
                    Description = "Stable Diffusion web UI, a browser interface based on Gradio library for Stable Diffusion.",
                    AiSummary = "텍스트 몇 줄로 고화질 일러스트와 실사 이미지를 생성하는 브라우저 기반 GUI 오픈소스",
                    PracticalTip = "사내 보고서 삽화, 프레젠테이션 디자인, 웹 배너 제작 시 이미지 외주 비용 없이 자체 제작",
                    DetailStory = "Gradio를 기반으로 구축된 브라우저 인터페이스로, 복잡한 파이썬 코딩 없이도 누구나 웹 화면에서 텍스트 프롬프트를 입력해 초고화질 이미지를 즉시 생성할 수 있습니다. ControlNet(자세 제어), LoRA(스타일 학습), 고해상도 업스케일러 등 수백 가지 확장 기능을 원클릭으로 결합할 수 있어 전 세계 디지털 크리에이터들의 표준 도구로 자리잡았습니다."
                },
                new AiRepoItem
                {
                    Rank = 4,
                    FullName = "langchain-ai / langchain",
                    RepoName = "langchain",
                    Owner = "langchain-ai",
                    HtmlUrl = "https://github.com/langchain-ai/langchain",
                    Stars = 99100,
                    Forks = 16200,
                    Language = "Python",
                    Topics = new List<string> { "llm", "agents", "rag", "framework" },
                    PushedAt = DateTime.UtcNow.AddHours(-12),
                    Description = "Build context-aware reasoning applications with LangChain's flexible abstractions and AI components.",
                    AiSummary = "대형 언어 모델(LLM)에 사내 문서(PDF/Excel), DB, 외부 검색 엔진을 손쉽게 연결하는 표준 프레임워크",
                    PracticalTip = "사내 업무 매뉴얼, 설계 가이드, 규정집을 기반으로 정확한 답변을 찾아주는 사내 RAG 시스템 구축",
                    DetailStory = "단순한 챗봇을 넘어 스스로 생각하고 외부 툴을 사용하는 자율형 AI 에이전트를 만들기 위한 종합 프레임워크입니다. 다양한 LLM 제공자, 수십 종의 벡터 데이터베이스, 웹 크롤러를 레고 블록처럼 손쉽게 조립할 수 있으며, 기업 내부 지식 저장소를 연동하는 RAG(검색 증강 생성) 구축에 필수적인 표준 라이브러리입니다."
                },
                new AiRepoItem
                {
                    Rank = 5,
                    FullName = "vllm-project / vllm",
                    RepoName = "vllm",
                    Owner = "vllm-project",
                    HtmlUrl = "https://github.com/vllm-project/vllm",
                    Stars = 33400,
                    Forks = 5100,
                    Language = "Python",
                    Topics = new List<string> { "llm-serving", "inference", "pagedattention", "gpu" },
                    PushedAt = DateTime.UtcNow.AddHours(-6),
                    Description = "A high-throughput and memory-efficient inference and serving engine for LLMs.",
                    AiSummary = "PagedAttention 메모리 기법으로 LLM의 응답 속도를 최대 24배 가속하는 최고속도 서빙 엔진",
                    PracticalTip = "고가의 GPU 서버 증설 없이도 사내 AI 서비스의 동시 접속자 수용량을 10배 이상 확대하여 비용 절감",
                    DetailStory = "운영체제의 가상 메모리 페이징 기법에서 착안한 혁신적인 'PagedAttention' 알고리즘을 도입하여 GPU VRAM 낭비를 거의 0% 수준으로 최소화했습니다. 기존 허깅페이스 트랜스포머 대비 추론 처리량(Throughput)을 최대 24배까지 끌어올려, 대규모 사내 LLM 배포 시 GPU 인프라 서버 비용을 획기적으로 줄여줍니다."
                }
            };
        }

        #region [Google News RSS Fetcher]

        private async Task<AiTrendFetchResult> FetchNewsRssAsync(AiTrendCategory category, bool forceRefresh)
        {
            string queryStr = category.GetEffectiveQuery();
            string queryHash = GetShortHash(queryStr);
            string cacheFilePath = Path.Combine(CacheDir, $"ai_news_{category.Id}_{queryHash}.json");

            // 1. 연타 방지 쿨타임 (5초)
            if (forceRefresh)
            {
                double elapsedSeconds = (DateTime.Now - _lastRequestTime).TotalSeconds;
                if (elapsedSeconds < 5)
                {
                    int remainSec = (int)Math.Ceiling(5 - elapsedSeconds);
                    if (File.Exists(cacheFilePath))
                    {
                        var (cachedItems, fTime) = TryReadCache(cacheFilePath);
                        if (cachedItems != null && cachedItems.Count > 0)
                        {
                            EnsureAiSummaries(cachedItems);
                            SyncFavoriteStatus(cachedItems);
                            return new AiTrendFetchResult
                            {
                                Items = cachedItems,
                                IsLiveSuccess = false,
                                IsFallbackCache = false,
                                DataTimestamp = fTime,
                                DisplayStatus = $"요청 간격이 너무 빠릅니다. ({remainSec}초 후 가능) - 기존 뉴스 표시"
                            };
                        }
                    }
                }
            }

            // 2. 캐시 유효성 검사 (뉴스는 신선도가 중요하므로 1시간 유효)
            if (!forceRefresh && File.Exists(cacheFilePath))
            {
                var (cachedItems, fTime) = TryReadCache(cacheFilePath);
                if (cachedItems != null && cachedItems.Count > 0 && (DateTime.Now - fTime).TotalHours < 1)
                {
                    EnsureAiSummaries(cachedItems);
                    SyncFavoriteStatus(cachedItems);
                    bool allEnriched = cachedItems.TrueForAll(x => (!string.IsNullOrWhiteSpace(x.PracticalTip) && !string.IsNullOrWhiteSpace(x.DetailStory)) || !HasApiKey);
                    return new AiTrendFetchResult
                    {
                        Items = cachedItems,
                        IsLiveSuccess = true,
                        IsFallbackCache = false,
                        IsFullyEnriched = allEnriched,
                        DataTimestamp = fTime,
                        DisplayStatus = $"📦 저장된 뉴스 캐시 ({fTime:yyyy-MM-dd HH:mm} 저장본)"
                    };
                }
            }

            // 3. Google News RSS 호출 및 XML 파싱
            try
            {
                _lastRequestTime = DateTime.Now;
                string encodedQuery = Uri.EscapeDataString(queryStr);
                string rssUrl = $"https://news.google.com/rss/search?q={encodedQuery}&hl=ko&gl=KR&ceid=KR:ko";

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(rssUrl);
                request.Method = "GET";
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
                request.Accept = "application/rss+xml, application/xml, text/xml; q=0.9, */*; q=0.8";
                request.Timeout = 10000;

                string xmlContent = "";
                using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    xmlContent = await reader.ReadToEndAsync();
                }

                XDocument xdoc = XDocument.Parse(xmlContent);
                var items = xdoc.Descendants("item");

                var resultList = new List<AiRepoItem>();
                var favSet = new HashSet<string>(LoadFavorites().ConvertAll(x => x.FullName), StringComparer.OrdinalIgnoreCase);
                int rank = 1;

                foreach (var el in items)
                {
                    if (rank > 15) break;

                    string rawTitle = el.Element("title")?.Value?.Trim() ?? "";
                    string link = el.Element("link")?.Value?.Trim() ?? "";
                    string pubDateStr = el.Element("pubDate")?.Value?.Trim() ?? "";
                    string rawDesc = el.Element("description")?.Value?.Trim() ?? "";
                    string source = el.Element("source")?.Value?.Trim() ?? "";

                    if (string.IsNullOrWhiteSpace(rawTitle)) continue;

                    // 언론사 분리 및 타이틀 정제
                    string cleanTitle = rawTitle;
                    if (string.IsNullOrWhiteSpace(source))
                    {
                        int lastHyphen = rawTitle.LastIndexOf(" - ");
                        if (lastHyphen > 0)
                        {
                            source = rawTitle.Substring(lastHyphen + 3).Trim();
                            cleanTitle = rawTitle.Substring(0, lastHyphen).Trim();
                        }
                        else
                        {
                            source = "주요 언론";
                        }
                    }
                    else
                    {
                        int lastHyphen = rawTitle.LastIndexOf(" - ");
                        if (lastHyphen > 0)
                        {
                            cleanTitle = rawTitle.Substring(0, lastHyphen).Trim();
                        }
                    }

                    // 설명 HTML 태그 정제
                    string cleanDesc = Regex.Replace(rawDesc, "<.*?>", string.Empty);
                    cleanDesc = WebUtility.HtmlDecode(cleanDesc).Trim();

                    DateTime? dtPub = null;
                    if (DateTime.TryParse(pubDateStr, out DateTime dtParsed))
                    {
                        dtPub = dtParsed;
                    }

                    var newsItem = new AiRepoItem
                    {
                        ItemType = TrendItemType.NewsArticle,
                        Rank = rank++,
                        FullName = cleanTitle,
                        RepoName = cleanTitle,
                        Owner = source,
                        HtmlUrl = link,
                        Description = string.IsNullOrWhiteSpace(cleanDesc) ? cleanTitle : cleanDesc,
                        Stars = 0,
                        Forks = 0,
                        Language = "뉴스",
                        PushedAt = dtPub,
                        UpdatedAt = dtPub ?? DateTime.Now,
                        AiSummary = "",
                        PracticalTip = ""
                    };

                    newsItem.IsFavorite = favSet.Contains(newsItem.FullName);
                    resultList.Add(newsItem);
                }

                // 4. 기존 캐시에서 이미 생성된 AI 요약 병합
                MergeExistingAiSummaries(resultList, cacheFilePath);

                string apiKey = GetGeminiApiKey();
                bool hasApiKey = !string.IsNullOrWhiteSpace(apiKey);
                if (!hasApiKey)
                {
                    foreach (var item in resultList)
                    {
                        if (string.IsNullOrWhiteSpace(item.AiSummary))
                        {
                            item.AiSummary = "[API Key 미등록] '🔑 API 설정'을 통해 키를 등록하면 AI 핵심 1줄 브리핑과 시사점이 제공됩니다.";
                        }
                    }
                }

                bool isFullyEnriched = resultList.Count > 0 && resultList.TrueForAll(x => (!string.IsNullOrWhiteSpace(x.PracticalTip) && !string.IsNullOrWhiteSpace(x.DetailStory)) || (!hasApiKey && !string.IsNullOrWhiteSpace(x.AiSummary)));

                // 5. 1단계 로컬 캐시 파일 저장
                SaveCache(cacheFilePath, resultList);

                string displayStatus = isFullyEnriched
                    ? $"✔ 최신 실시간 뉴스 ({DateTime.Now:yyyy-MM-dd HH:mm})"
                    : $"✔ 기사 {resultList.Count}건 수집 완료 (AI 핵심 브리핑 생성 중... ⏳)";

                return new AiTrendFetchResult
                {
                    Items = resultList,
                    IsLiveSuccess = true,
                    IsFallbackCache = false,
                    IsFullyEnriched = isFullyEnriched,
                    DataTimestamp = DateTime.Now,
                    DisplayStatus = displayStatus
                };
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, $"AiTrend News RSS Call Failed ({category.DisplayName})");

                if (File.Exists(cacheFilePath))
                {
                    var (cachedItems, fTime) = TryReadCache(cacheFilePath);
                    if (cachedItems != null && cachedItems.Count > 0)
                    {
                        EnsureAiSummaries(cachedItems);
                        SyncFavoriteStatus(cachedItems);
                        return new AiTrendFetchResult
                        {
                            Items = cachedItems,
                            IsLiveSuccess = false,
                            IsFallbackCache = true,
                            DataTimestamp = fTime,
                            FailureReason = ex.Message,
                            DisplayStatus = $"⚠️ [이전 뉴스 캐시] {fTime:yyyy-MM-dd HH:mm} 기준 (실시간 수집 실패: {ex.Message})"
                        };
                    }
                }

                return new AiTrendFetchResult
                {
                    Items = new List<AiRepoItem>(),
                    IsLiveSuccess = false,
                    IsFallbackCache = false,
                    DataTimestamp = DateTime.Now,
                    FailureReason = ex.Message,
                    DisplayStatus = $"⚠️ 뉴스 수집 실패: {ex.Message}"
                };
            }
        }

        #endregion

        public static string GetShortHash(string input)
        {
            if (string.IsNullOrEmpty(input)) return "default";
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(hash).Replace("-", "").Substring(0, 8).ToLowerInvariant();
            }
        }

        /// <summary>
        /// [Stage 2] 백그라운드에서 Gemini API를 호출하여 AI 핵심 요약 및 실무 팁(시사점)을 비동기로 생성하고 캐시를 갱신합니다.
        /// </summary>
        public async Task<AiEnrichStatus> EnrichRepositoriesAsync(AiTrendCategory category, List<AiRepoItem> items)
        {
            if (category == null || items == null || items.Count == 0) return AiEnrichStatus.Success;

            string apiKey = GetGeminiApiKey();
            if (string.IsNullOrWhiteSpace(apiKey)) return AiEnrichStatus.Success;

            // 이미 모든 아이템에 실무 팁/시사점 및 심층 스토리가 채워져 있다면 스킵
            bool allEnriched = items.TrueForAll(x => !string.IsNullOrWhiteSpace(x.PracticalTip) && !string.IsNullOrWhiteSpace(x.DetailStory));
            if (allEnriched) return AiEnrichStatus.Success;

            string queryStr = category.GetEffectiveQuery();
            string queryHash = GetShortHash(queryStr);
            string filePrefix = category.Kind == CategoryKind.GitHub ? "ai_trend" : "ai_news";
            string cacheFilePath = Path.Combine(CacheDir, $"{filePrefix}_{category.Id}_{queryHash}.json");

            bool isNews = category.Kind != CategoryKind.GitHub || items[0].ItemType == TrendItemType.NewsArticle;
            var status = isNews
                ? await TryEnrichNewsWithAiSummariesAsync(items, apiKey)
                : await TryEnrichWithAiSummariesAsync(items, apiKey);

            if (status == AiEnrichStatus.QuotaExceeded)
            {
                foreach (var item in items)
                {
                    if (string.IsNullOrWhiteSpace(item.AiSummary))
                    {
                        item.AiSummary = "[할당량 초과] Gemini 분당 호출 한도(15회)에 도달하여 원문 설명을 임시 표시합니다. (잠시 후 새로고침 시 요약 생성)";
                    }
                }
            }

            // AI 요약이 반영된 최신 상태로 캐시 업데이트 저장
            SaveCache(cacheFilePath, items);
            return status;
        }

        /// <summary>
        /// API Key 등록 후 화면의 기존 리스트를 즉시 한국어 AI 요약으로 채워주는 기능
        /// </summary>
        public async Task<bool> ReSummarizeItemsAsync(List<AiRepoItem> items, string categoryId)
        {
            string apiKey = GetGeminiApiKey();
            if (string.IsNullOrWhiteSpace(apiKey) || items == null || items.Count == 0) return false;

            try
            {
                var status = await TryEnrichWithAiSummariesAsync(items, apiKey);
                if (Directory.Exists(CacheDir))
                {
                    string[] existingCaches = Directory.GetFiles(CacheDir, $"ai_trend_{categoryId}*.json");
                    foreach (var cf in existingCaches)
                    {
                        SaveCache(cf, items);
                    }
                }
                return status == AiEnrichStatus.Success;
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "AiTrend ReSummarizeItemsAsync Failed");
                return false;
            }
        }

        private static readonly string[] GeminiModelsToTry = new[]
        {
            "gemini-2.5-flash",
            "gemini-flash-latest",
            "gemini-3.5-flash-lite",
            "gemini-3.5-flash",
            "gemini-pro-latest"
        };

        private async Task<(string jsonResponse, bool had429)> CallGeminiWithFallbackAsync(string promptText, string apiKey)
        {
            bool had429 = false;
            string jsonResponse = "";

            foreach (var modelName in GeminiModelsToTry)
            {
                try
                {
                    string url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={apiKey}";
                    string jsonBody = new JObject
                    {
                        ["contents"] = new JArray
                        {
                            new JObject
                            {
                                ["role"] = "user",
                                ["parts"] = new JArray
                                {
                                    new JObject { ["text"] = promptText }
                                }
                            }
                        }
                    }.ToString(Formatting.None);

                    byte[] bytes = Encoding.UTF8.GetBytes(jsonBody);
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                    req.Method = "POST";
                    req.ContentType = "application/json";
                    req.ContentLength = bytes.Length;
                    req.Timeout = 14000;

                    using (Stream s = await req.GetRequestStreamAsync())
                    {
                        await s.WriteAsync(bytes, 0, bytes.Length);
                    }

                    using (HttpWebResponse resp = (HttpWebResponse)await req.GetResponseAsync())
                    using (Stream rs = resp.GetResponseStream())
                    using (StreamReader r = new StreamReader(rs, Encoding.UTF8))
                    {
                        string aiRawResp = await r.ReadToEndAsync();
                        var jsonObj = JObject.Parse(aiRawResp);
                        var textToken = jsonObj["candidates"]?[0]?["content"]?["parts"]?[0]?["text"];
                        if (textToken != null)
                        {
                            jsonResponse = textToken.ToString().Trim();
                            if (!string.IsNullOrWhiteSpace(jsonResponse)) break;
                        }
                    }
                }
                catch (WebException webEx)
                {
                    if (webEx.Response is HttpWebResponse httpResp)
                    {
                        int statusCode = (int)httpResp.StatusCode;
                        if (statusCode == 429)
                        {
                            had429 = true;
                            LogManager.LogMessage($"[AiTrend Gemini] 모델 '{modelName}' HTTP 429(분당 한도). 다음 모델 시도.", LogManager.Level.Warning);
                            await Task.Delay(500);
                            continue;
                        }
                        if (statusCode == 404)
                        {
                            LogManager.LogMessage($"[AiTrend Gemini] 모델 '{modelName}' HTTP 404 발생. 다음 모델 시도.", LogManager.Level.Warning);
                            await Task.Delay(300);
                            continue;
                        }
                    }
                    LogManager.LogException(webEx, LogManager.Level.Warning, $"[AiTrend Gemini] 모델 '{modelName}' 호출 통신 오류");
                }
            }

            return (jsonResponse, had429);
        }

        private async Task<AiEnrichStatus> TryEnrichWithAiSummariesAsync(List<AiRepoItem> items, string apiKey)
        {
            bool had429 = false;
            try
            {
                var sbPrompt = new StringBuilder();
                sbPrompt.AppendLine("당신은 전세계 최신 AI 오픈소스 트렌드를 분석하는 전문 기술 분석가입니다.");
                sbPrompt.AppendLine("다음 GitHub 저장소 목록을 보고, 각 도구에 대해 다음 3가지 정보를 반드시 한국어로 작성하세요:");
                sbPrompt.AppendLine("1) summary: 이 도구가 무엇인지, 어떤 문제를 해결하는지 명쾌한 1줄 정의 (30~60자)");
                sbPrompt.AppendLine("2) practicalTip: 실무 개발/사내 업무에 어떻게 써먹을 수 있는지 구체적인 활용 포인트 1줄 (30~60자)");
                sbPrompt.AppendLine("3) story: 이 오픈소스의 핵심 아키텍처 원리, 기존 도구와의 차별점, 어떤 환경에서 도입하면 좋은지 3~4문장으로 서술한 심층 기술 해설 (100~200자)");
                sbPrompt.AppendLine("반드시 아래 JSON 배열 형식으로만 응답하세요. 백틱이나 마크다운 없이 순수 JSON 문자열만 출력하세요:");
                sbPrompt.AppendLine("[ {\"repo\": \"레포명\", \"summary\": \"1줄 핵심 요약\", \"practicalTip\": \"2줄 실무/업무 활용 포인트\", \"story\": \"3~4줄 심층 기술 해설\"}, ... ]");
                sbPrompt.AppendLine();

                int limit = Math.Min(items.Count, 15);
                for (int i = 0; i < limit; i++)
                {
                    string desc = string.IsNullOrWhiteSpace(items[i].Description) ? "No description" : items[i].Description;
                    sbPrompt.AppendLine($"- {items[i].FullName}: {desc} (Topics: {string.Join(", ", items[i].Topics)})");
                }

                string promptText = sbPrompt.ToString();
                var geminiResult = await CallGeminiWithFallbackAsync(promptText, apiKey);
                string jsonResponse = geminiResult.jsonResponse;
                had429 = geminiResult.had429;

                if (string.IsNullOrWhiteSpace(jsonResponse))
                {
                    return had429 ? AiEnrichStatus.QuotaExceeded : AiEnrichStatus.Failed;
                }

                // [견고한 파싱] 백틱 제거 및 다단계 파싱
                if (jsonResponse.StartsWith("```json", StringComparison.OrdinalIgnoreCase)) jsonResponse = jsonResponse.Substring(7);
                if (jsonResponse.StartsWith("```")) jsonResponse = jsonResponse.Substring(3);
                if (jsonResponse.EndsWith("```")) jsonResponse = jsonResponse.Substring(0, jsonResponse.Length - 3);
                jsonResponse = jsonResponse.Trim();

                var dict = ParseSummariesSafely(jsonResponse);
                if (dict.Count > 0)
                {
                    foreach (var item in items)
                    {
                        if (dict.TryGetValue(item.FullName, out var pair) || dict.TryGetValue(item.RepoName, out pair))
                        {
                            if (!string.IsNullOrWhiteSpace(pair.summary)) item.AiSummary = pair.summary;
                            if (!string.IsNullOrWhiteSpace(pair.practicalTip)) item.PracticalTip = pair.practicalTip;
                            if (!string.IsNullOrWhiteSpace(pair.story)) item.DetailStory = pair.story;
                        }
                    }
                    return AiEnrichStatus.Success;
                }
                return had429 ? AiEnrichStatus.QuotaExceeded : AiEnrichStatus.Failed;
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "AiTrend Gemini Enrichment Failed");
                return had429 ? AiEnrichStatus.QuotaExceeded : AiEnrichStatus.Failed;
            }
        }

        private async Task<AiEnrichStatus> TryEnrichNewsWithAiSummariesAsync(List<AiRepoItem> items, string apiKey)
        {
            bool had429 = false;
            try
            {
                var sbPrompt = new StringBuilder();
                sbPrompt.AppendLine("당신은 바쁜 직장인을 위해 전세계 및 국내 주요 뉴스의 핵심을 명쾌하게 브리핑하는 수석 뉴스 에디터입니다.");
                sbPrompt.AppendLine("아래 제공된 뉴스 기사 목록을 읽고, 각 기사에 대해 다음 3가지 정보를 반드시 한국어로 작성하세요:");
                sbPrompt.AppendLine("1) summary: 기사의 핵심 사건/팩트를 육하원칙에 맞게 명쾌하게 설명한 1줄 요약 (30~60자, ~습니다 / ~함 체)");
                sbPrompt.AppendLine("2) practicalTip: 이 뉴스가 갖는 경제/사회적 파급효과나 독자가 눈여겨볼 핵심 시사점 1줄 (30~60자, ~전망 / ~주목 체)");
                sbPrompt.AppendLine("3) story: 기사의 구체적 배경 맥락, 인물/기업 간의 전개 상황, 화제가 된 이유와 향후 쟁점을 3~4문장으로 알기 쉽게 서술한 [심층 스토리 브리핑] (120~220자)");
                sbPrompt.AppendLine("반드시 아래 JSON 배열 형식으로만 응답하세요. 백틱이나 마크다운 없이 순수 JSON 문자열만 출력하세요:");
                sbPrompt.AppendLine("[ {\"rank\": 1, \"title\": \"기사 제목 일부\", \"summary\": \"1줄 팩트 브리핑\", \"practicalTip\": \"핵심 시사점 및 관전 포인트\", \"story\": \"3~4문장 심층 스토리 브리핑\"}, ... ]");
                sbPrompt.AppendLine();

                int limit = Math.Min(items.Count, 15);
                for (int i = 0; i < limit; i++)
                {
                    string desc = string.IsNullOrWhiteSpace(items[i].Description) ? items[i].FullName : items[i].Description;
                    sbPrompt.AppendLine($"- [#{items[i].Rank}] {items[i].FullName} (출처: {items[i].Owner}): {desc}");
                }

                string promptText = sbPrompt.ToString();
                var geminiResult = await CallGeminiWithFallbackAsync(promptText, apiKey);
                string jsonResponse = geminiResult.jsonResponse;
                had429 = geminiResult.had429;

                if (string.IsNullOrWhiteSpace(jsonResponse))
                {
                    return had429 ? AiEnrichStatus.QuotaExceeded : AiEnrichStatus.Failed;
                }

                if (jsonResponse.StartsWith("```json", StringComparison.OrdinalIgnoreCase)) jsonResponse = jsonResponse.Substring(7);
                if (jsonResponse.StartsWith("```")) jsonResponse = jsonResponse.Substring(3);
                if (jsonResponse.EndsWith("```")) jsonResponse = jsonResponse.Substring(0, jsonResponse.Length - 3);
                jsonResponse = jsonResponse.Trim();

                var (byRank, byTitle) = ParseNewsSummariesSafely(jsonResponse);
                if (byRank.Count > 0 || byTitle.Count > 0)
                {
                    foreach (var item in items)
                    {
                        if (byRank.TryGetValue(item.Rank, out var pair) || byTitle.TryGetValue(item.FullName, out pair))
                        {
                            if (!string.IsNullOrWhiteSpace(pair.summary)) item.AiSummary = pair.summary;
                            if (!string.IsNullOrWhiteSpace(pair.practicalTip)) item.PracticalTip = pair.practicalTip;
                            if (!string.IsNullOrWhiteSpace(pair.story)) item.DetailStory = pair.story;
                        }
                    }
                    return AiEnrichStatus.Success;
                }
                return had429 ? AiEnrichStatus.QuotaExceeded : AiEnrichStatus.Failed;
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "AiTrend Gemini News Enrichment Failed");
                return had429 ? AiEnrichStatus.QuotaExceeded : AiEnrichStatus.Failed;
            }
        }

        private (Dictionary<int, (string summary, string practicalTip, string story)> byRank, Dictionary<string, (string summary, string practicalTip, string story)> byTitle) ParseNewsSummariesSafely(string rawJson)
        {
            var byRank = new Dictionary<int, (string summary, string practicalTip, string story)>();
            var byTitle = new Dictionary<string, (string summary, string practicalTip, string story)>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var arr = JArray.Parse(rawJson);
                foreach (var row in arr)
                {
                    int? rank = row["rank"]?.ToObject<int>();
                    string title = row["title"]?.ToString() ?? row["repo"]?.ToString() ?? "";
                    string summary = row["summary"]?.ToString() ?? "";
                    string tip = row["practicalTip"]?.ToString() ?? row["tip"]?.ToString() ?? "";
                    string story = row["story"]?.ToString() ?? row["detail"]?.ToString() ?? "";

                    if (!string.IsNullOrEmpty(summary) || !string.IsNullOrEmpty(tip) || !string.IsNullOrEmpty(story))
                    {
                        if (rank.HasValue && rank.Value > 0) byRank[rank.Value] = (summary, tip, story);
                        if (!string.IsNullOrEmpty(title)) byTitle[title] = (summary, tip, story);
                    }
                }
            }
            catch { }

            return (byRank, byTitle);
        }

        /// <summary>
        /// Gemini JSON 파싱 오류 방어 로직 (JArray 파싱 실패 시 잘림 복구 및 정규식 추출 fallback)
        /// </summary>
        private Dictionary<string, (string summary, string practicalTip, string story)> ParseSummariesSafely(string rawJson)
        {
            var dict = new Dictionary<string, (string summary, string practicalTip, string story)>(StringComparer.OrdinalIgnoreCase);

            // 1차 시도: 표준 JArray 파싱
            try
            {
                var summaryArray = JArray.Parse(rawJson);
                foreach (var row in summaryArray)
                {
                    string repo = row["repo"]?.ToString() ?? row["name"]?.ToString() ?? "";
                    string summary = row["summary"]?.ToString() ?? "";
                    string tip = row["practicalTip"]?.ToString() ?? row["tip"]?.ToString() ?? "";
                    string story = row["story"]?.ToString() ?? row["detail"]?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(repo) && (!string.IsNullOrEmpty(summary) || !string.IsNullOrEmpty(tip) || !string.IsNullOrEmpty(story)))
                    {
                        dict[repo] = (summary, tip, story);
                    }
                }
                if (dict.Count > 0) return dict;
            }
            catch { }

            // 2차 시도: 토큰 잘림으로 끝이 안 닫힌 경우 (']' 복구 시도)
            try
            {
                string fixedJson = rawJson;
                int lastBrace = fixedJson.LastIndexOf('}');
                if (lastBrace > 0)
                {
                    fixedJson = fixedJson.Substring(0, lastBrace + 1) + "]";
                    var summaryArray = JArray.Parse(fixedJson);
                    foreach (var row in summaryArray)
                    {
                        string repo = row["repo"]?.ToString() ?? row["name"]?.ToString() ?? "";
                        string summary = row["summary"]?.ToString() ?? "";
                        string tip = row["practicalTip"]?.ToString() ?? row["tip"]?.ToString() ?? "";
                        string story = row["story"]?.ToString() ?? row["detail"]?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(repo) && (!string.IsNullOrEmpty(summary) || !string.IsNullOrEmpty(tip) || !string.IsNullOrEmpty(story)))
                        {
                            dict[repo] = (summary, tip, story);
                        }
                    }
                    if (dict.Count > 0) return dict;
                }
            }
            catch { }

            // 3차 시도: 정규식 기반 부분 추출 (가능한 요약문은 1개라도 전부 구출)
            try
            {
                var matches = Regex.Matches(rawJson, @"[""'](?:repo|name)[""']\s*:\s*[""'](?<repo>[^""']+)[""']\s*,\s*[""']summary[""']\s*:\s*[""'](?<sum>[^""']+)[""'](?:\s*,\s*[""'](?:practicalTip|tip)[""']\s*:\s*[""'](?<tip>[^""']+)[""'])?(?:\s*,\s*[""'](?:story|detail)[""']\s*:\s*[""'](?<sty>[^""']+)[""'])?", RegexOptions.IgnoreCase);
                foreach (Match m in matches)
                {
                    string repo = m.Groups["repo"].Value.Trim();
                    string sum = m.Groups["sum"].Value.Trim();
                    string tip = m.Groups["tip"].Success ? m.Groups["tip"].Value.Trim() : "";
                    string story = m.Groups["sty"].Success ? m.Groups["sty"].Value.Trim() : "";
                    if (!string.IsNullOrEmpty(repo) && (!string.IsNullOrEmpty(sum) || !string.IsNullOrEmpty(tip) || !string.IsNullOrEmpty(story)))
                    {
                        dict[repo] = (sum, tip, story);
                    }
                }
            }
            catch { }

            return dict;
        }

        private string AnalyzeWebException(WebException webEx)
        {
            if (webEx.Response is HttpWebResponse httpResp)
            {
                int statusCode = (int)httpResp.StatusCode;
                if (statusCode == 403)
                {
                    return "GitHub 시간당 요청 한도(60회) 초과 (잠시 후 다시 시도해 주세요)";
                }
                if (statusCode == 407)
                {
                    return "사내 프록시 인증 필요 (HTTP 407)";
                }
                if (statusCode == 429)
                {
                    return "요청 빈도 제한 도달 (HTTP 429)";
                }
                return $"GitHub 서버 응답 오류 (HTTP {statusCode})";
            }

            switch (webEx.Status)
            {
                case WebExceptionStatus.NameResolutionFailure:
                    return "인터넷 미연결 또는 사내망 DNS 차단";
                case WebExceptionStatus.Timeout:
                    return "GitHub 서버 응답 시간 초과 (네트워크 지연)";
                case WebExceptionStatus.TrustFailure:
                case WebExceptionStatus.SecureChannelFailure:
                    return "사내망 보안 인증서(SSL) 제한";
                case WebExceptionStatus.ConnectFailure:
                    return "GitHub 서버에 연결할 수 없음 (방화벽/네트워크 차단)";
                default:
                    return webEx.Message;
            }
        }

        private (List<AiRepoItem> items, DateTime lastWriteTime) TryReadCache(string cacheFilePath)
        {
            try
            {
                var fileInfo = new FileInfo(cacheFilePath);
                string cachedJson = File.ReadAllText(cacheFilePath, Encoding.UTF8);
                var cachedItems = JsonConvert.DeserializeObject<List<AiRepoItem>>(cachedJson);
                return (cachedItems, fileInfo.LastWriteTime);
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "AiTrend TryReadCache Failed");
                return (null, DateTime.MinValue);
            }
        }

        private void SaveCache(string cacheFilePath, List<AiRepoItem> items)
        {
            try
            {
                if (!Directory.Exists(CacheDir)) Directory.CreateDirectory(CacheDir);
                string jsonToSave = JsonConvert.SerializeObject(items, Formatting.Indented);
                File.WriteAllText(cacheFilePath, jsonToSave, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "AiTrend SaveCache Failed");
            }
        }

        private void MergeExistingAiSummaries(List<AiRepoItem> newItems, string cacheFilePath)
        {
            if (!File.Exists(cacheFilePath)) return;
            try
            {
                var (oldItems, _) = TryReadCache(cacheFilePath);
                if (oldItems == null) return;

                var dict = new Dictionary<string, (string summary, string tip, string story)>(StringComparer.OrdinalIgnoreCase);
                foreach (var old in oldItems)
                {
                    if (!string.IsNullOrWhiteSpace(old.AiSummary) && !old.AiSummary.StartsWith("[API Key 미등록]"))
                    {
                        dict[old.FullName] = (old.AiSummary, old.PracticalTip ?? "", old.DetailStory ?? "");
                    }
                }

                foreach (var item in newItems)
                {
                    if (dict.TryGetValue(item.FullName, out var oldPair))
                    {
                        if (string.IsNullOrWhiteSpace(item.AiSummary)) item.AiSummary = oldPair.summary;
                        if (string.IsNullOrWhiteSpace(item.PracticalTip)) item.PracticalTip = oldPair.tip;
                        if (string.IsNullOrWhiteSpace(item.DetailStory)) item.DetailStory = oldPair.story;
                    }
                }
            }
            catch { }
        }

        private void EnsureAiSummaries(List<AiRepoItem> items)
        {
            bool hasKey = HasApiKey;
            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.AiSummary))
                {
                    item.AiSummary = hasKey
                        ? item.Description
                        : "[API Key 미등록] '🔑 API 설정'을 통해 키를 등록하면 한국어 핵심 요약이 제공됩니다.";
                }
            }
        }

        private void SyncFavoriteStatus(List<AiRepoItem> items)
        {
            if (items == null) return;
            try
            {
                var favs = LoadFavorites();
                var set = new HashSet<string>(favs.ConvertAll(x => x.FullName), StringComparer.OrdinalIgnoreCase);
                foreach (var item in items)
                {
                    item.IsFavorite = set.Contains(item.FullName);
                }
            }
            catch { }
        }
    }
}
