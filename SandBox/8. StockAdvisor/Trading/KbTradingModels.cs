using System;
using System.Collections.Generic;

namespace GateHelper.StockAdvisor.Trading
{
    public class KbAuthTokenResponse
    {
        public bool IsSuccess { get; set; } = false;
        public string AccessToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = "Bearer";
        public int ExpiresIn { get; set; } = 86400;
        public DateTime IssuedAt { get; set; } = DateTime.Now;
        public string ErrorMessage { get; set; } = string.Empty;

        public bool IsExpired => DateTime.Now >= IssuedAt.AddSeconds(ExpiresIn - 60);
    }

    public class KbHoldingBalanceDto
    {
        public string StockCode { get; set; } = string.Empty;
        public string StockName { get; set; } = string.Empty;
        public int HoldingQuantity { get; set; } = 0;
        public decimal BuyAveragePrice { get; set; } = 0m;
        public decimal CurrentPrice { get; set; } = 0m;
        public decimal TotalBuyAmount { get; set; } = 0m;
        public decimal TotalEvalAmount { get; set; } = 0m;
        public decimal ProfitAmount { get; set; } = 0m;
        public decimal ProfitRate { get; set; } = 0m;
    }

    public class KbAccountBalanceResponse
    {
        public bool IsSuccess { get; set; } = false;
        public string AccountNumber { get; set; } = string.Empty;
        public decimal TotalDeposit { get; set; } = 0m;        // 예수금
        public decimal TotalEvalAmount { get; set; } = 0m;      // 총 평가금
        public decimal TotalProfitAmount { get; set; } = 0m;    // 총 손익금
        public List<KbHoldingBalanceDto> Holdings { get; set; } = new List<KbHoldingBalanceDto>();
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class KbOrderRequest
    {
        public string StockCode { get; set; } = string.Empty;
        public int OrderQuantity { get; set; } = 0;
        public decimal OrderPrice { get; set; } = 0m;
        public string OrderType { get; set; } = "01"; // 01: 시장가(Market), 00: 지정가(Limit), 03: 최유리
        public bool IsBuy { get; set; } = false;      // false: 매도 (Exit)
    }

    public class KbOrderResponse
    {
        public bool IsSuccess { get; set; } = false;
        public string OrderNumber { get; set; } = string.Empty;
        public string StockCode { get; set; } = string.Empty;
        public int OrderedQuantity { get; set; } = 0;
        public decimal ExecutedPrice { get; set; } = 0m;
        public string StatusMessage { get; set; } = string.Empty;
        public DateTime OrderedAt { get; set; } = DateTime.Now;
    }

    public interface IKbTradingClient
    {
        bool IsConfigured { get; }
        bool IsSimulationMode { get; }
        System.Threading.Tasks.Task<KbAuthTokenResponse> GetAccessTokenAsync();
        System.Threading.Tasks.Task<KbAccountBalanceResponse> FetchBalanceAsync(string accountNumber);
        System.Threading.Tasks.Task<KbOrderResponse> SendOrderAsync(KbOrderRequest request);
    }
}

