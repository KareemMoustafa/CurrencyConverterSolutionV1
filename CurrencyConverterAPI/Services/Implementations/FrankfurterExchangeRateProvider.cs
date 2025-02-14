using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;
using CurrencyConverterAPI.Models;
using Microsoft.Extensions.Caching.Distributed;

public class FrankfurterExchangeRateProvider : IExchangeRateProvider
{
    private readonly HttpClient _httpClient;
    private readonly RedisCacheService _cache;
    private readonly ILogger<FrankfurterExchangeRateProvider> _logger;
    /// <summary>
    /// 
    /// </summary>
    /// <param name="httpClient"></param>
    /// <param name="cache"></param>
    /// <param name="logger"></param>
    public FrankfurterExchangeRateProvider(HttpClient httpClient, RedisCacheService cache, ILogger<FrankfurterExchangeRateProvider> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="baseCurrency"></param>
    /// <returns></returns>
    public async Task<ExchangeRateResponse> GetLatestRatesAsync(string baseCurrency)
    {
        string cacheKey = $"exchange_rates_{baseCurrency}";
        var cachedData = await _cache.GetAsync<ExchangeRateResponse>(cacheKey);

        if (cachedData != null) return cachedData;

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var response = await _httpClient.GetStringAsync($"https://api.frankfurter.dev/v1/latest?base={baseCurrency}");
        var rates = JsonSerializer.Deserialize<ExchangeRateResponse>(response, options);


        if (rates != null)
            await _cache.SetAsync(cacheKey, rates, 60);

        return rates ?? new ExchangeRateResponse();
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="baseCurrency"></param>
    /// <param name="to"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<ConvertExchangeRateResonse> ConvertRateAsync(string baseCurrency, string to)
    {
        string cacheKey = $"convert_rates_{baseCurrency}";
        var cachedData = await _cache.GetAsync<ConvertExchangeRateResonse>(cacheKey);

        if (cachedData != null) return cachedData;

        string url = $"https://api.frankfurter.dev/v1/latest?base={baseCurrency}&symbols={to}";

        try
        {
            Log.Information("Fetching exchange rate from {Url}", url);

            var response = await _httpClient.GetStringAsync(url);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var exchangeData = JsonSerializer.Deserialize<ConvertExchangeRateResonse>(response, options);

            if (exchangeData == null || exchangeData.Rates == null || !exchangeData.Rates.ContainsKey(to))
            {
                Log.Warning("API response is invalid for conversion: {Response}", response);
                throw new Exception("Invalid API response.");
            }
            await _cache.SetAsync(cacheKey, JsonSerializer.Serialize(exchangeData));

            return exchangeData;
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "HTTP request error when calling exchange rate API at {Url}", url);
            throw new Exception("Error fetching exchange rate data from API.", ex);
        }
        catch (JsonException ex)
        {
            Log.Error(ex, "JSON deserialization error for response: {Url}", url);
            throw new Exception("Error processing exchange rate data.", ex);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "An unexpected error occurred while processing exchange rate data.");
            throw;
        }
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="baseCurrency"></param>
    /// <param name="startDate"></param>
    /// <param name="endDate"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<HistoricalExchangeRateResponse> GetHistoricalRatesAsync(string baseCurrency, string startDate, string endDate, int page = 1, int pageSize = 10)
    {
        try
        {
            string url = $"https://api.frankfurter.app/{startDate}..{endDate}?base={baseCurrency}";

            var response = await _httpClient.GetStringAsync(url);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals
            };

            var ratesResponse = JsonSerializer.Deserialize<HistoricalExchangeRateResponse>(response, options);

            if (ratesResponse == null || ratesResponse.Rates == null)
            {
                throw new JsonException("Invalid API response format.");
            }

            // Sort by date (latest first) and apply pagination
            var sortedRates = ratesResponse.Rates.OrderByDescending(kvp => kvp.Key);
            int totalCount = sortedRates.Count(); // Total available historical dates

            var paginatedRates = sortedRates
                .Skip((page - 1) * pageSize)  // Skip previous pages
                .Take(pageSize)               // Take only the required number of results
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            return new HistoricalExchangeRateResponse
            {
                Amount = ratesResponse.Amount,
                BaseCurrency = ratesResponse.BaseCurrency,
                StartDate = ratesResponse.StartDate,
                EndDate = ratesResponse.EndDate,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Rates = paginatedRates
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error fetching exchange rate data from API.");
            throw new Exception("Error fetching exchange rate data from API.", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error processing exchange rate data.");
            throw new Exception("Error processing exchange rate data.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred.");
            throw new Exception("An unexpected error occurred.", ex);
        }
    }



    public async Task<Dictionary<string, string>> GetCurrenciesAsync()
    {
        const string CurrencyApiUrl = "https://api.frankfurter.dev/v1/currencies";
        const string CacheKey = "CurrencyList";
        var cachedData = await _cache.GetAsync<Dictionary<string, string>>(CacheKey);

        if (cachedData != null) return cachedData;
        try
        {
            var response = await _httpClient.GetAsync(CurrencyApiUrl);
            response.EnsureSuccessStatusCode();
            var jsonString = await response.Content.ReadAsStringAsync();
            var currencies = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);

            if (currencies != null)
            {
                await _cache.SetAsync(CacheKey, currencies);
                return currencies;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Error fetching currencies: {Message}", ex.Message);
        }

        return new Dictionary<string, string>(); // Return empty if API fails
    }
}
