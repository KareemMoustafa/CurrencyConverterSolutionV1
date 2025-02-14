public class ExchangeRateProviderFactory
{
    private readonly Dictionary<string, IExchangeRateProvider> _providers;

    public ExchangeRateProviderFactory(IEnumerable<IExchangeRateProvider> providers)
    {
        _providers = new Dictionary<string, IExchangeRateProvider>();

        foreach (var provider in providers)
        {
            _providers[provider.GetType().Name] = provider;
        }
    }

    public IExchangeRateProvider GetProvider(string providerName)
    {
        return _providers.TryGetValue(providerName, out var provider)
            ? provider
            : throw new InvalidOperationException($"Exchange rate provider '{providerName}' is not registered.");
    }
}
