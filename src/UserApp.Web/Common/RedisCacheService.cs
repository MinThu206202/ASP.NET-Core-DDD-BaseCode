using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Distributed;
using UserApp.Application.Common.Interfaces;

namespace UserApp.Web.Common;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    public RedisCacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var bytes = await _cache.GetAsync(key);
        if (bytes == null) return default;
        return JsonSerializer.Deserialize<T>(bytes, JsonOptions);
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null)
    {
        var bytes = await _cache.GetAsync(key);
        if (bytes != null)
        {
            var value = JsonSerializer.Deserialize<T>(bytes, JsonOptions);
            return value!;
        }

        var result = await factory();
        await SetAsync(key, result, expiry);
        return result;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
        var options = new DistributedCacheEntryOptions();
        if (expiry.HasValue)
            options.AbsoluteExpirationRelativeToNow = expiry;
        await _cache.SetAsync(key, bytes, options);
    }

    public Task RemoveAsync(string key) => _cache.RemoveAsync(key);
}
