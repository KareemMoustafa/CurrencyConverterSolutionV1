using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using System.Threading.Tasks;

public class RedisCacheService
{
    private readonly IDistributedCache _cache;

    public RedisCacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var data = await _cache.GetStringAsync(key);
        if (string.IsNullOrEmpty(data)) return default;

        return JsonSerializer.Deserialize<T>(data, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        try
        {
            Console.WriteLine($"Cache Data for {key}: {data}"); // Log the raw JSON
            return JsonSerializer.Deserialize<T>(data, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true // Allows case-insensitive mapping
            });
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"JSON Deserialization Error: {ex.Message}");
            Console.WriteLine($"JSON Data: {data}");
            throw; // Re-throw the exception for debugging
        }
    }


    public async Task SetAsync<T>(string key, T value, int expirationInMinutes = 10)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(expirationInMinutes)
        };
        await _cache.SetStringAsync(key, JsonSerializer.Serialize(value), options);
    }
}
