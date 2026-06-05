using Microsoft.EntityFrameworkCore;
using UserApp.Domain.Common;

namespace UserApp.Infrastructure.Persistence.Repositories;

public abstract class BaseRepository<T> : IBaseRepository<T> where T : class
{
    protected readonly AppDbContext Db;
    protected readonly DbSet<T> Entities;

    protected BaseRepository(AppDbContext db)
    {
        Db = db;
        Entities = db.Set<T>();
    }

    public virtual Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Entities.FindAsync(new object[] { id }, ct).AsTask();

    public virtual async Task<IReadOnlyList<T>> ListAsync(int skip, int take, CancellationToken ct = default) =>
        await Entities.Skip(skip).Take(take).ToListAsync(ct);

    public virtual Task<int> CountAsync(CancellationToken ct = default) =>
        Entities.CountAsync(ct);

    public virtual async Task AddAsync(T entity, CancellationToken ct = default) =>
        await Entities.AddAsync(entity, ct);

    public virtual void Update(T entity) => Entities.Update(entity);

    public virtual void Remove(T entity) => Entities.Remove(entity);

    public virtual Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        Db.SaveChangesAsync(ct);
}
