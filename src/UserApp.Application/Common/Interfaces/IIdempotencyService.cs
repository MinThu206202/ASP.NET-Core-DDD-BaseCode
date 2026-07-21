namespace UserApp.Application.Common.Interfaces;

public interface IIdempotencyService
{
    Task<IdempotencyResult?> GetCachedResponseAsync(string key);
    Task StoreResponseAsync(string key, IdempotencyResult result, TimeSpan? expiry = null);
    Task<bool> TryAcquireLockAsync(string key, TimeSpan lockExpiry);
    Task ReleaseLockAsync(string key);
}

public class IdempotencyResult
{
    public int StatusCode { get; set; }
    public string ContentType { get; set; } = "application/json";
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
}
