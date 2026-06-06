using Microsoft.EntityFrameworkCore;
using UserApp.Domain.Common;

namespace UserApp.Infrastructure.Persistence;

public class BaseRepository<T> : IBaseRepository<T> where T : class
{
    protected readonly AppDbContext _db;
    protected readonly DbSet<T> Entities; // rename _set -> Entities

    public BaseRepository(AppDbContext db)
    {
        _db = db;
        Entities = db.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await Entities.FindAsync(new object[] { id }, ct);

    public virtual async Task<List<T>> ListAsync(int skip, int take, CancellationToken ct = default)
    {
        return await Entities
            .Where(x => EF.Property<DateTime?>(x, "DeletedAt") == null)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
    }

    public virtual async Task<int> CountAsync(CancellationToken ct = default)
        => await Entities.CountAsync(ct);

    public virtual async Task AddAsync(T entity, CancellationToken ct = default)
        => await Entities.AddAsync(entity, ct);

    public virtual void Update(T entity) => Entities.Update(entity);

    public virtual void Remove(T entity) => Entities.Remove(entity);

    public virtual Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);


}