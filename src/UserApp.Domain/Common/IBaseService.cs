namespace UserApp.Application.Common;

public interface IBaseService<T>
{
    Task<T?> GetByIdAsync(Guid id);
    Task<List<T>> ListAsync(int skip, int take);
    Task<int> CountAsync();

    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task RemoveAsync(T entity);

    Task SaveAsync();
}