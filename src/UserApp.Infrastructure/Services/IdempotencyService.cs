using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Distributed;
using UserApp.Application.Common.Interfaces;

namespace UserApp.Infrastructure.Services;

public class IdempotencyService : IIdempotencyService
{
    private readonly IDistributedCache _cache;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    private const string ResponsePrefix = "idempotency:response:";
    private const string LockPrefix = "idempotency:lock:";

    public IdempotencyService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<IdempotencyResult?> GetCachedResponseAsync(string key)
    {
        var bytes = await _cache.GetAsync($"{ResponsePrefix}{key}");
        if (bytes == null) return null;
        return JsonSerializer.Deserialize<IdempotencyResult>(bytes, JsonOptions);
    }

    public async Task StoreResponseAsync(string key, IdempotencyResult result, TimeSpan? expiry = null)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(result, JsonOptions);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromMinutes(60)
        };
        await _cache.SetAsync($"{ResponsePrefix}{key}", bytes, options);
    }

    public async Task<bool> TryAcquireLockAsync(string key, TimeSpan lockExpiry)
    {
        var lockKey = $"{LockPrefix}{key}";
        var existing = await _cache.GetAsync(lockKey);
        if (existing != null) return false;

        await _cache.SetAsync(lockKey, new byte[] { 1 }, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = lockExpiry
        });
        return true;
    }

    public async Task ReleaseLockAsync(string key)
    {
        await _cache.RemoveAsync($"{LockPrefix}{key}");
    }
}
