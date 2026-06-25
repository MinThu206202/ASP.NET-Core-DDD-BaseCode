using Microsoft.EntityFrameworkCore;
using UserApp.Domain.Common;

namespace UserApp.Infrastructure.Persistence.Repositories;

public class BaseRepository<T> : IBaseRepository<T> where T : class
{
    protected readonly AppDbContext _db;
    protected readonly DbSet<T> _set;

    public BaseRepository(AppDbContext db)
    {
        _db = db;
        _set = db.Set<T>();
    }

    public async Task<T?> GetByIdAsync(Guid id)
    {
        return await _set.FindAsync(id);
    }

    public virtual async Task<List<T>> ListAsync(int skip, int take)
    {
        var query = _set.AsQueryable();

        if (typeof(T).GetProperty("DeletedAt")?.PropertyType == typeof(DateTime?))
        {
            query = query.Where(e => EF.Property<DateTime?>(e, "DeletedAt") == null);
        }

        return await query.Skip(skip).Take(take).ToListAsync();
    }

    public async Task<int> CountAsync()
    {
        var query = _set.AsQueryable();

        if (typeof(T).GetProperty("DeletedAt")?.PropertyType == typeof(DateTime?))
        {
            query = query.Where(e => EF.Property<DateTime?>(e, "DeletedAt") == null);
        }

        return await query.CountAsync();
    }

    public async Task AddAsync(T entity)
    {
        await _set.AddAsync(entity);
    }

    public void Update(T entity)
    {
        _set.Update(entity);
    }

    public void Remove(T entity)
    {
        _set.Remove(entity);
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }

    
}