using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using CurrencyConverterAPI.Models;


public class ExchangeRatesControllerTests
{
    private readonly Mock<IExchangeRateProvider> _mockProvider;
    private readonly Mock<ExchangeRateProviderFactory> _mockFactory;
    private readonly ExchangeRatesController _controller;

    public ExchangeRatesControllerTests()
    {
        _mockProvider = new Mock<IExchangeRateProvider>();
        _mockFactory = new Mock<ExchangeRateProviderFactory>();
        _mockFactory.Setup(f => f.GetProvider(It.IsAny<string>())).Returns(_mockProvider.Object);

        _controller = new ExchangeRatesController(_mockFactory.Object);
    }

    // GetCurrencies returns list
    [Fact]
    public async Task GetCurrencies_ReturnsListOfCurrencies()
    {
        var currencies = new Dictionary<string, string>
        {
            { "USD", "United States Dollar" },
            { "EUR", "Euro" }
        };

        _mockProvider.Setup(p => p.GetCurrenciesAsync()).ReturnsAsync(currencies);

        var result = await _controller.GetCurrencies();
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedCurrencies = Assert.IsType<Dictionary<string, string>>(okResult.Value);

        Assert.Equal(2, returnedCurrencies.Count);
    }

    //  GetCurrencies returns empty
    [Fact]
    public async Task GetCurrencies_ReturnsEmptyList_WhenNoData()
    {
        _mockProvider.Setup(p => p.GetCurrenciesAsync()).ReturnsAsync(new Dictionary<string, string>());

        var result = await _controller.GetCurrencies();
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedCurrencies = Assert.IsType<Dictionary<string, string>>(okResult.Value);

        Assert.Empty(returnedCurrencies);
    }

    //  GetLatestRates returns rates
    [Fact]
    public async Task GetLatestRates_ReturnsExchangeRates()
    {
        var rates = new ExchangeRateResponse
        {
            Base = "EUR",
            Rates = new Dictionary<string, decimal>
                {
                    { "USD", 1.10m },
                    { "GBP", 0.85m }
                }
        };
        _mockProvider.Setup(p => p.GetLatestRatesAsync("EUR")).ReturnsAsync(rates);

        var result = await _controller.GetLatestRates();
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedRates = Assert.IsType<object>(okResult.Value);

        Assert.NotNull(returnedRates);
    }

    // GetLatestRates fails
    [Fact]
    public async Task GetLatestRates_ReturnsBadRequest_WhenFail()
    {
        _mockProvider.Setup(p => p.GetLatestRatesAsync("EUR"))
         .ReturnsAsync((ExchangeRateResponse?)null);

        var result = await _controller.GetLatestRates();

        Assert.IsType<BadRequestObjectResult>(result);
    }

    //  ConvertCurrency fails with invalid input
    [Fact]
    public async Task ConvertCurrency_ReturnsBadRequest_WhenInputIsInvalid()
    {
        var result = await _controller.ConvertCurrency(from: "", to: "");
        Assert.IsType<BadRequestObjectResult>(result);
    }

    //  ConvertCurrency fails due to API issue
    [Fact]
    public async Task ConvertCurrency_ReturnsServerError_WhenApiFails()
    {
        _mockProvider.Setup(p => p.ConvertRateAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException());

        var result = await _controller.ConvertCurrency(from: "EUR", to: "USD");
        Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, (result as ObjectResult)!.StatusCode);
    }

    //  GetHistoricalRates returns data
    [Fact]
    public async Task GetHistoricalRates_ReturnsData()
    {
        var rates = new HistoricalExchangeRateResponse
        {
            Rates = new Dictionary<string, Dictionary<string, decimal>>
        {
            { "2024-01-01", new Dictionary<string, decimal> { { "USD", 1.10m }, { "GBP", 0.85m } } },
            { "2024-01-02", new Dictionary<string, decimal> { { "USD", 1.12m }, { "GBP", 0.86m } } }
        }
        };


        _mockProvider.Setup(p => p.GetHistoricalRatesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(rates); // Must match `Task<HistoricalExchangeRateResponse>`

        var result = await _controller.GetHistoricalRates("2024-01-01", "2024-01-10");
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedRates = Assert.IsType<HistoricalExchangeRateResponse>(okResult.Value);

        Assert.NotNull(returnedRates);
        Assert.True(returnedRates.Rates.Count > 0);
    }

    // GetHistoricalRates fails due to missing dates
    [Fact]
    public async Task GetHistoricalRates_ReturnsBadRequest_WhenDatesMissing()
    {
        var result = await _controller.GetHistoricalRates(null, null);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    //  GetHistoricalRates fails due to provider failure
    [Fact]
    public async Task GetHistoricalRates_ReturnsBadRequest_WhenProviderFails()
    {

        _mockProvider.Setup(p => p.GetHistoricalRatesAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((HistoricalExchangeRateResponse?)null); // ✅ Correct return type

        var result = await _controller.GetHistoricalRates("2024-01-01", "2024-01-10");

        Assert.IsType<BadRequestObjectResult>(result);
    }

}
