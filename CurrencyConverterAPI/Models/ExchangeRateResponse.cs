using System.Text.Json.Serialization;

public class ExchangeRateResponse
{
    [JsonPropertyName("base")]
    public string Base { get; set; } = "EUR";
    [JsonPropertyName("date")]
    public string Date { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");
    [JsonPropertyName("rates")]
    public Dictionary<string, decimal> Rates { get; set; } = new();
}
