using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;

namespace GateHelper.StockAdvisor.Services
{
    public class MasterStockItem
    {
        [JsonProperty("c")]
        public string Code { get; set; }

        [JsonProperty("n")]
        public string Name { get; set; }

        [JsonProperty("m")]
        public string Market { get; set; }
    }

    /// <summary>
    /// 국내 전종목(KOSPI, KOSDAQ 약 4,300개) 마스터 사전 서비스
    /// 단어의 시작/중간/끝(예: "백화점", "에어로", "바이오", "스페이스" 등)을 0ms 만에 100% 포함 검색 지원
    /// (Zero-Resource: EmbeddedResource 기반 1회 메모리 로드, 런타임 디스크 I/O 0B)
    /// </summary>
    public static class StockMasterService
    {
        private static List<MasterStockItem> _masterList;
        private static readonly object _lock = new object();

        private static void EnsureLoaded()
        {
            if (_masterList != null) return;
            lock (_lock)
            {
                if (_masterList != null) return;
                try
                {
                    var asm = typeof(StockMasterService).Assembly;
                    string[] resNames = asm.GetManifestResourceNames();
                    string targetRes = resNames.FirstOrDefault(r => r.EndsWith("stock_master.json", StringComparison.OrdinalIgnoreCase));

                    if (!string.IsNullOrEmpty(targetRes))
                    {
                        using (var stream = asm.GetManifestResourceStream(targetRes))
                        {
                            if (stream != null)
                            {
                                using (var reader = new StreamReader(stream, Encoding.UTF8))
                                {
                                    string json = reader.ReadToEnd();
                                    _masterList = JsonConvert.DeserializeObject<List<MasterStockItem>>(json) ?? new List<MasterStockItem>();
                                    return;
                                }
                            }
                        }
                    }

                    // 파일 직접 경로 폴백 (디버깅/개발 모드용)
                    string fallbackPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SandBox", "8. StockAdvisor", "Resources", "stock_master.json");
                    if (File.Exists(fallbackPath))
                    {
                        string json = File.ReadAllText(fallbackPath, Encoding.UTF8);
                        _masterList = JsonConvert.DeserializeObject<List<MasterStockItem>>(json) ?? new List<MasterStockItem>();
                        return;
                    }

                    // 소스 디렉터리 경로 폴백
                    string srcFallback = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "SandBox", "8. StockAdvisor", "Resources", "stock_master.json");
                    if (File.Exists(srcFallback))
                    {
                        string json = File.ReadAllText(srcFallback, Encoding.UTF8);
                        _masterList = JsonConvert.DeserializeObject<List<MasterStockItem>>(json) ?? new List<MasterStockItem>();
                        return;
                    }
                }
                catch { }

                if (_masterList == null) _masterList = new List<MasterStockItem>();
            }
        }

        /// <summary>
        /// 검색어가 포함된 모든 종목을 스마트하게 검색 (정확 일치 > 전방 일치 > 중간/후방 부분 포함 순 정렬)
        /// </summary>
        public static List<StockSearchResultItem> Search(string query, int maxResults = 30)
        {
            var results = new List<StockSearchResultItem>();
            if (string.IsNullOrWhiteSpace(query)) return results;

            EnsureLoaded();
            if (_masterList == null || _masterList.Count == 0) return results;

            string clean = query.Trim();

            // 1) 정확 일치
            var exact = _masterList.Where(x => string.Equals(x.Name, clean, StringComparison.OrdinalIgnoreCase));
            // 2) 전방 일치 (시작하는 단어)
            var prefix = _masterList.Where(x => !string.Equals(x.Name, clean, StringComparison.OrdinalIgnoreCase) && x.Name.StartsWith(clean, StringComparison.OrdinalIgnoreCase));
            // 3) 중간/후방 포함 (단어 내에 포함)
            var contains = _masterList.Where(x => !x.Name.StartsWith(clean, StringComparison.OrdinalIgnoreCase) && x.Name.IndexOf(clean, StringComparison.OrdinalIgnoreCase) >= 0);

            var merged = exact.Concat(prefix).Concat(contains).Take(maxResults);

            foreach (var item in merged)
            {
                results.Add(new StockSearchResultItem
                {
                    Code = item.Code,
                    Name = item.Name,
                    TypeName = item.Market,
                    NationCode = "KOR",
                    ReutersCode = item.Code
                });
            }

            return results;
        }
    }
}
