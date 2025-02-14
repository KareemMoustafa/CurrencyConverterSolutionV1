namespace CurrencyConverterAPI.Models
{
    public class ConvertExchangeRateResonse
    {
        public decimal Amount { get; set; } = 1.0m;
        public string Base { get; set; } = string.Empty;
        public string Date { get; set; } = System.DateTime.UtcNow.ToString("yyyy-MM-dd");
        public Dictionary<string, decimal> Rates { get; set; } = new();
        public string ConvertedCurrency { get; set; } = string.Empty;
        public decimal ConvertedAmount { get; set; } = 0.0m;

    }
}