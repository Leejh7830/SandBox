using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace GateHelper.StockAdvisor.Models
{
    /// <summary>
    /// 독립 매매 복기 및 전략 승률 통계 기록 항목 (AI 진단평가 로직과 100% 분리)
    /// </summary>
    public class TradeJournalEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public DateTime TradeDate { get; set; } = DateTime.Now;
        public string StockCode { get; set; } = string.Empty;
        public string StockName { get; set; } = string.Empty;
        public string EntryStrategy { get; set; } = "수동/기타";
        public string ExitReason { get; set; } = "수동 청산";
        public decimal BuyPrice { get; set; } = 0m;
        public decimal SellPrice { get; set; } = 0m;
        public int Quantity { get; set; } = 0;
        public string Memo { get; set; } = string.Empty;

        [JsonIgnore]
        public decimal RealizedProfit => (SellPrice - BuyPrice) * Quantity;

        [JsonIgnore]
        public decimal ProfitRate => BuyPrice > 0m ? Math.Round((SellPrice - BuyPrice) / BuyPrice * 100m, 2) : 0m;

        [JsonIgnore]
        public bool IsWin => SellPrice > BuyPrice;
    }

    /// <summary>
    /// 매매 복기 전용 독립 JSON 저장소 (평상시 0B 디스크 I/O, 기록/삭제 시에만 저장)
    /// </summary>
    public static class TradeJournalStorage
    {
        private static readonly string JournalFilePath = Path.Combine(Util.MetaFolder, "stock_trade_journal.json");

        public static List<TradeJournalEntry> LoadEntries()
        {
            try
            {
                if (!File.Exists(JournalFilePath)) return new List<TradeJournalEntry>();
                string json = File.ReadAllText(JournalFilePath);
                if (string.IsNullOrWhiteSpace(json)) return new List<TradeJournalEntry>();
                return JsonConvert.DeserializeObject<List<TradeJournalEntry>>(json) ?? new List<TradeJournalEntry>();
            }
            catch
            {
                return new List<TradeJournalEntry>();
            }
        }

        public static void SaveEntries(List<TradeJournalEntry> entries)
        {
            try
            {
                string dir = Path.GetDirectoryName(JournalFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                string json = JsonConvert.SerializeObject(entries ?? new List<TradeJournalEntry>(), Formatting.Indented);
                File.WriteAllText(JournalFilePath, json);
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "TradeJournalStorage.SaveEntries");
            }
        }
    }
}
