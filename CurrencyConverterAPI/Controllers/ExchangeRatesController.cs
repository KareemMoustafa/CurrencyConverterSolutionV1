using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Microsoft.AspNetCore.RateLimiting;
using CurrencyConverterAPI.Models;
using System.ComponentModel.DataAnnotations;

[ApiController]
[Route("api/exchange-rates")]
[Produces("application/json")]
public class ExchangeRatesController : ControllerBase
{
    private const string DEFAULT_PROVIDER = "FrankfurterExchangeRateProvider";
    private const string DEFAULT_BASE_CURRENCY = "EUR";
    private const string DEFAULT_TARGET_CURRENCY = "USD";
    private const int DEFAULT_PAGE_SIZE = 5;
    private const int MAX_PAGE_SIZE = 50;

    private readonly ExchangeRateProviderFactory _providerFactory;
    private readonly JwtService _jwtService;
    private readonly ISet<string> _excludedCurrencies = new HashSet<string> { "TRY", "PLN", "THB", "MXN" };

    /// <summary>
    /// Initializes a new instance of the ExchangeRatesController
    /// </summary>
    /// <param name="providerFactory">Factory for creating exchange rate providers</param>
    /// <param name="jwtService">Service for JWT token operations</param>
    /// <exception cref="ArgumentNullException">Thrown when any required dependency is null</exception>
    public ExchangeRatesController(
        ExchangeRateProviderFactory providerFactory,
        JwtService jwtService)
    {
        _providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
        _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>JWT token on successful authentication</returns>
    /// <response code="200">Returns the JWT token</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="401">If credentials are invalid</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Login([FromBody] Login request)
    {
        if (request == null || string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new ErrorResponse("Username and password are required"));
        }

        // TODO: Replace with proper authentication
        if (request.Username == "admin" && request.Password == "password")
        {
            var token = _jwtService.GenerateToken("1", "Admin");
            return Ok(new TokenResponse(token));
        }

        Log.Warning("Failed login attempt for user: {Username}", request.Username);
        return Unauthorized(new ErrorResponse("Invalid credentials"));
    }

    /// <summary>
    /// Get all available currencies
    /// </summary>
    /// <param name="provider">The exchange rate provider to use</param>
    /// <returns>List of available currencies</returns>
    /// <response code="200">Returns the list of currencies</response>
    /// <response code="400">If the provider is invalid</response>
    /// <response code="401">If unauthorized</response>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrencies(
        [FromQuery] string provider = DEFAULT_PROVIDER)
    {
        try
        {
            var exchangeRateProvider = _providerFactory.GetProvider(provider);
            var currencies = await exchangeRateProvider.GetCurrenciesAsync();

            return Ok(currencies.Keys.Except(_excludedCurrencies));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving currencies from provider {Provider}", provider);
            return BadRequest(new ErrorResponse($"Failed to retrieve currencies from provider {provider}"));
        }
    }

    /// <summary>
    /// Get latest exchange rates for a base currency
    /// </summary>
    /// <param name="baseCurrency">Base currency code</param>
    /// <param name="provider">Exchange rate provider name</param>
    /// <returns>Latest exchange rates</returns>
    [Authorize]
    [HttpGet("latest")]
    [EnableRateLimiting("StrictPolicy")]
    [ProducesResponseType(typeof(ExchangeRateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetLatestRates(
        [FromQuery] string baseCurrency = DEFAULT_BASE_CURRENCY,
        [FromQuery] string provider = DEFAULT_PROVIDER)
    {
        if (_excludedCurrencies.Contains(baseCurrency))
        {
            return BadRequest(new ErrorResponse($"Currency {baseCurrency} is not supported"));
        }

        try
        {
            var exchangeRateProvider = _providerFactory.GetProvider(provider);
            var rates = await exchangeRateProvider.GetLatestRatesAsync(baseCurrency.ToUpperInvariant());

            return rates != null
                ? Ok(rates)
                : BadRequest(new ErrorResponse("Failed to retrieve exchange rates"));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving latest rates for {BaseCurrency}", baseCurrency);
            return StatusCode(500, new ErrorResponse("An unexpected error occurred"));
        }
    }

    /// <summary>
    /// Converts currency from one denomination to another
    /// </summary>
    /// <param name="from">Source currency code</param>
    /// <param name="to">Target currency code</param>
    /// <param name="provider">Exchange rate provider name</param>
    /// <returns>Converted currency rate</returns>
    [HttpGet("convert")]
    [EnableRateLimiting("StrictPolicy")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ConvertCurrency(
        [FromQuery] string from = DEFAULT_BASE_CURRENCY,
        [FromQuery] string to = DEFAULT_TARGET_CURRENCY,
        [FromQuery] string provider = DEFAULT_PROVIDER)
    {
        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
        {
            Log.Warning("Invalid conversion request: from={From}, to={To}", from, to);
            return BadRequest(new ErrorResponse("Invalid input. Ensure 'from' and 'to' are provided."));
        }

        if (_excludedCurrencies.Contains(from) || _excludedCurrencies.Contains(to))
        {
            return BadRequest(new ErrorResponse("One or more specified currencies are not supported."));
        }

        try
        {
            var exchangeRateProvider = _providerFactory.GetProvider(provider);
            var rates = await exchangeRateProvider.ConvertRateAsync(
                from.ToUpperInvariant(),
                to.ToUpperInvariant());

            if (rates == null)
            {
                Log.Warning("Conversion failed for {From} to {To}", from, to);
                return BadRequest(new ErrorResponse("Conversion failed."));
            }

            return Ok(rates);
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "Error fetching exchange rate data from API");
            return StatusCode(500, new ErrorResponse("An error occurred while processing the request."));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error during currency conversion");
            return StatusCode(500, new ErrorResponse("An unexpected error occurred."));
        }
    }

    /// <summary>
    /// Get historical exchange rates for a date range
    /// </summary>
    /// <param name="startDate">Start date for historical data</param>
    /// <param name="endDate">End date for historical data</param>
    /// <param name="page">Page number for pagination</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="baseCurrency">Base currency code</param>
    /// <param name="provider">Exchange rate provider name</param>
    /// <returns>Historical exchange rates</returns>
    [EnableRateLimiting("StrictPolicy")]
    [HttpGet("historical")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetHistoricalRates(
        [Required][FromQuery] string startDate,
        [Required][FromQuery] string endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = DEFAULT_PAGE_SIZE,
        [FromQuery] string baseCurrency = DEFAULT_BASE_CURRENCY,
        [FromQuery] string provider = DEFAULT_PROVIDER)
    {
        if (string.IsNullOrEmpty(startDate) || string.IsNullOrEmpty(endDate))
        {
            return BadRequest(new ErrorResponse("StartDate and EndDate are required."));
        }

        if (!DateTime.TryParse(startDate, out _) || !DateTime.TryParse(endDate, out _))
        {
            return BadRequest(new ErrorResponse("Invalid date format."));
        }

        if (_excludedCurrencies.Contains(baseCurrency))
        {
            return BadRequest(new ErrorResponse($"Currency {baseCurrency} is not supported"));
        }

        pageSize = Math.Min(pageSize, MAX_PAGE_SIZE);

        try
        {
            var exchangeRateProvider = _providerFactory.GetProvider(provider);
            var rates = await exchangeRateProvider.GetHistoricalRatesAsync(
                baseCurrency.ToUpperInvariant(),
                startDate,
                endDate,
                page,
                pageSize);

            return rates != null
                ? Ok(rates)
                : BadRequest(new ErrorResponse("Failed to retrieve historical exchange rates."));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving historical rates");
            return StatusCode(500, new ErrorResponse("An unexpected error occurred"));
        }
    }
}

public record TokenResponse(string Token);
public record ErrorResponse(string Message);