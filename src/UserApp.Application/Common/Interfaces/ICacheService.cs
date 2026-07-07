namespace UserApp.Application.Common.Interfaces;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task RemoveAsync(string key);
}
