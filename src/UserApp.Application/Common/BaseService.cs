using UserApp.Domain.Common;
using UserApp.Application.Common.Interfaces;


namespace UserApp.Application.Common;

public class BaseService<T> : IBaseService<T> where T : class
{
    protected readonly IBaseRepository<T> _repo;
    protected readonly IMediaPipeline? _mediaPipeline;

    public BaseService(IBaseRepository<T> repo, IMediaPipeline? mediaPipeline = null)
    {
        _repo = repo;
        _mediaPipeline = mediaPipeline;
    }

    public Task<T?> GetByIdAsync(Guid id) => _repo.GetByIdAsync(id);
    public Task<List<T>> ListAsync(int skip, int take) => _repo.ListAsync(skip, take);
    public Task<int> CountAsync() => _repo.CountAsync();

    public async Task AddAsync(T entity, object? file = null)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        await _repo.AddAsync(entity);

        // 🔥 FIRST SAVE so ID exists
        await _repo.SaveChangesAsync();

        // THEN media
        if (_mediaPipeline != null && file != null)
        {
            await _mediaPipeline.HandleCreateAsync(typeof(T).Name, entity, file);
            // 🔥 SAVE MEDIA
            await _repo.SaveChangesAsync();
        }


    }

    public async Task UpdateAsync(T entity, object? file = null)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        _repo.Update(entity);
        await _repo.SaveChangesAsync();

        if (_mediaPipeline != null && file != null)
        {
            await _mediaPipeline.HandleUpdateAsync(typeof(T).Name, entity, file);
        }
    }

    public async Task RemoveAsync(T entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        if (_mediaPipeline != null)
        {
            await _mediaPipeline.HandleDeleteAsync(typeof(T).Name, entity);
        }

        _repo.Remove(entity);
        await _repo.SaveChangesAsync();
    }

    public Task SaveAsync() => _repo.SaveChangesAsync();
}