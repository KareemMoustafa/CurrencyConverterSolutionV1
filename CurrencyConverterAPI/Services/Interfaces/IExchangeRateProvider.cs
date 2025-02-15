using CurrencyConverterAPI.Models;

public interface IExchangeRateProvider
{
    Task<ExchangeRateResponse> GetLatestRatesAsync(string baseCurrency);
    Task<ExchangeRateResponse> ConvertRateAsync(string baseCurrency, string to);
    Task<HistoricalExchangeRateResponse> GetHistoricalRatesAsync(string baseCurrency, string startDate, string endDate, int page = 1, int pageSize = 10);
    Task<Dictionary<string, string>> GetCurrenciesAsync();
}
