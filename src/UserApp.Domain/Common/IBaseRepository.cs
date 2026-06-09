using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UserApp.Domain.Common;

public interface IBaseRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);

    Task<List<T>> ListAsync(int skip, int take);

    Task<int> CountAsync();

    Task AddAsync(T entity);

    void Update(T entity);

    void Remove(T entity);

    Task SaveChangesAsync();
}