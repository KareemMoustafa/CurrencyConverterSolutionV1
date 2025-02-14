namespace CurrencyConverterAPI.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class HistoricalExchangeRateResponse
    {
        public int TotalCount { get; set; } = 0;  // Total number of dates available
        public int Page { get; set; } = 1;        // Current page
        public int PageSize { get; set; } = 10;    // Number of results per page

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; } = 1.0m;

        [JsonPropertyName("base")]
        public string BaseCurrency { get; set; } = string.Empty;

        [JsonPropertyName("start_date")]
        public string StartDate { get; set; } = System.DateTime.UtcNow.ToString("yyyy-MM-dd");

        [JsonPropertyName("end_date")]
        public string EndDate { get; set; } = System.DateTime.UtcNow.ToString("yyyy-MM-dd");

        [JsonPropertyName("rates")]
        public Dictionary<string, Dictionary<string, decimal>> Rates { get; set; } = new();

        public string ConvertedCurrency { get; set; } = string.Empty;
        public decimal ConvertedAmount { get; set; } = 0.0m;
    }
}
