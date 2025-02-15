using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Identity.Data;
using CurrencyConverterAPI.Models;

[ApiController]
[Route("api/exchange-rates")]
public class ExchangeRatesController : ControllerBase
{
    private readonly ExchangeRateProviderFactory _providerFactory;
    private readonly List<string> _excludedCurrencies = new() { "TRY", "PLN", "THB", "MXN" };
    private readonly JwtService _jwtService;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="providerFactory"></param>
    /// <param name="jwtService"></param>
    public ExchangeRatesController(ExchangeRateProviderFactory providerFactory, JwtService jwtService)
    {
        _jwtService = jwtService;

        _providerFactory = providerFactory;
    }


    [HttpPost("login")]
    public IActionResult Login([FromBody] Login request)
    {
        // Dummy user validation (Replace with real authentication logic)
        if (request.Username == "admin" && request.Password == "password")
        {
            var token = _jwtService.GenerateToken("1", "Admin");
            return Ok(new { Token = token });
        }

        return Unauthorized("Invalid credentials");
    }
    /// <summary>
    /// Get all available currencies.
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetCurrencies([FromQuery] string provider = "FrankfurterExchangeRateProvider")
    {
        var exchangeRateProvider = _providerFactory.GetProvider(provider);
        var currencies = await exchangeRateProvider.GetCurrenciesAsync();
        return Ok(currencies);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="baseCurrency"></param>
    /// <param name="provider"></param>
    /// <returns></returns>
    [Authorize]
    [HttpGet("latest")]
    [EnableRateLimiting("StrictPolicy")]
    public async Task<IActionResult> GetLatestRates([FromQuery] string baseCurrency = "EUR", [FromQuery] string provider = "FrankfurterExchangeRateProvider")
    {
        var exchangeRateProvider = _providerFactory.GetProvider(provider);
        var rates = await exchangeRateProvider.GetLatestRatesAsync(baseCurrency);
        return rates != null ? Ok(rates) : BadRequest("Failed to retrieve exchange rates.");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="from"></param>
    /// <param name="provider"></param>
    /// <param name="to"></param>
    /// <returns></returns>
 
    [HttpGet("convert")]
    [EnableRateLimiting("StrictPolicy")]
    [Authorize]
    public async Task<IActionResult> ConvertCurrency(
        [FromQuery] string from = "EUR",
        [FromQuery] string provider = "FrankfurterExchangeRateProvider",
        [FromQuery] string to = "USD")
    {
        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
        {
            Log.Warning("Invalid conversion request: from={From}, to={To}", from, to);
            return BadRequest("Invalid input. Ensure 'from' and 'to' are provided.");
        }
        try
        {
            var exchangeRateProvider = _providerFactory.GetProvider(provider);
            var rates = await exchangeRateProvider.ConvertRateAsync(from, to);
            return rates != null ? Ok(rates) : BadRequest("Conversion failed.");
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "Error fetching exchange rate data from API");
            return StatusCode(500, "An error occurred while processing the request.");
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="startDate"></param>
    /// <param name="endDate"></param>
    /// <param name="page"></param>
    /// <param name="pageSize"></param>
    /// <param name="baseCurrency"></param>
    /// <param name="provider"></param>
    /// <returns></returns>
    ///[Authorize(Roles = "Admin")]
    [EnableRateLimiting("StrictPolicy")]
    [HttpGet("historical")]
    [Authorize]
    public async Task<IActionResult> GetHistoricalRates(
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null,
        int page = 1, int pageSize = 5,
        [FromQuery] string baseCurrency = "EUR",
        [FromQuery] string provider = "FrankfurterExchangeRateProvider")
    {
        if (string.IsNullOrEmpty(startDate) || string.IsNullOrEmpty(endDate))
        {
            return BadRequest("StartDate and EndDate are required.");
        }

        var exchangeRateProvider = _providerFactory.GetProvider(provider);
        var rates = await exchangeRateProvider.GetHistoricalRatesAsync(baseCurrency, startDate, endDate, page, pageSize);
        return rates != null ? Ok(rates) : BadRequest("Failed to retrieve historical exchange rates.");
    }
}