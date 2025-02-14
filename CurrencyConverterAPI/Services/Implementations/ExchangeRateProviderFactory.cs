public class ExchangeRateProviderFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ExchangeRateProviderFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IExchangeRateProvider GetProvider(string providerName)
    {
        return providerName switch
        {
            "FrankfurterExchangeRateProvider" => _serviceProvider.GetRequiredService<FrankfurterExchangeRateProvider>(),
            _ => throw new InvalidOperationException($"Exchange rate provider '{providerName}' is not registered.")
        };
    }
}
