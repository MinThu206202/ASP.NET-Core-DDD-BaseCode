using System.Text.Json;
using UserApp.Domain.Common;
using UserApp.Application.Common.Interfaces;
using UserApp.Application.AuditLogs.Interfaces;

namespace UserApp.Application.Common;

public class BaseService<T> : IBaseService<T> where T : class
{
    protected readonly IBaseRepository<T> _repo;
    protected readonly IMediaPipeline? _mediaPipeline;
    protected readonly IMediaService? _mediaService;

    public BaseService(IBaseRepository<T> repo)
    {
        _repo = repo;
        _mediaPipeline = ServiceProviderAccessor.Current?.GetService(typeof(IMediaPipeline)) as IMediaPipeline;
        _mediaService = ServiceProviderAccessor.Current?.GetService(typeof(IMediaService)) as IMediaService;
    }

    public Task<T?> GetByIdAsync(Guid id) => _repo.GetByIdAsync(id);
    public Task<List<T>> ListAsync(int skip, int take) => _repo.ListAsync(skip, take);
    public Task<int> CountAsync() => _repo.CountAsync();

    public virtual async Task AddAsync(T entity, object? file = null)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        await _repo.AddAsync(entity);

        await _repo.SaveChangesAsync();

        if (entity is IHasMedia && _mediaPipeline != null && file != null)
        {
            await _mediaPipeline.HandleCreateAsync(typeof(T).Name, entity, file);
            await _repo.SaveChangesAsync();
        }
    }

    public virtual async Task UpdateAsync(T entity, object? file = null)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        _repo.Update(entity);

        if (entity is IHasMedia && _mediaPipeline != null && file != null)
        {
            await _mediaPipeline.HandleUpdateAsync(typeof(T).Name, entity, file);
        }

        await _repo.SaveChangesAsync();
    }

    public async Task RemoveAsync(T entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        if (entity is Entity<Guid> softDeletable)
        {
            softDeletable.Delete();
        }
        else
        {
            if (entity is IHasMedia && _mediaPipeline != null)
            {
                await _mediaPipeline.HandleDeleteAsync(typeof(T).Name, entity);
            }
            _repo.Remove(entity);
        }
        await _repo.SaveChangesAsync();
    }

    public async Task RestoreAsync(Guid id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is Entity<Guid> softDeletable)
        {
            softDeletable.Restore();
            await _repo.SaveChangesAsync();
        }
    }

    public async Task RevertFromAuditAsync(Guid auditLogId)
    {
        var auditLogService = ServiceProviderAccessor.Current?.GetService(typeof(IAuditLogService)) as IAuditLogService;
        if (auditLogService == null) return;

        var auditLog = await auditLogService.GetByIdAsync(auditLogId);
        if (auditLog == null || string.IsNullOrEmpty(auditLog.OldValues))
            throw new InvalidOperationException("Audit log not found or has no old values to revert");

        var entity = await _repo.GetByIdAsync(Guid.Parse(auditLog.EntityId));
        if (entity == null)
            throw new InvalidOperationException("Entity not found");

        var oldValues = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(auditLog.OldValues);
        if (oldValues == null) return;

        foreach (var (key, value) in oldValues)
        {
            var prop = typeof(T).GetProperty(key);
            if (prop == null || !prop.CanWrite) continue;

            if (value.ValueKind == JsonValueKind.Null)
            {
                prop.SetValue(entity, null);
            }
            else
            {
                var converted = JsonSerializer.Deserialize(value.GetRawText(), prop.PropertyType);
                prop.SetValue(entity, converted);
            }
        }

        await _repo.SaveChangesAsync();
    }

    public virtual async Task<List<string>> GetImageUrlsAsync(Guid id)
    {
        if (_mediaService == null)
            return [];

        var media = await _mediaService.GetAsync(typeof(T).Name, id);
        return media.Select(x => x.Url).ToList();
    }

    public Task SaveAsync() => _repo.SaveChangesAsync();
}