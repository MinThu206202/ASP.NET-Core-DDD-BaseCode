namespace UserApp.Domain.Common;

public abstract class Entity<TId> where TId : notnull
{
    public TId Id { get; protected set; } = default!;
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }

    // 🔥 SOFT DELETE
    public DateTime? DeletedAt { get; private set; }

    public bool IsDeleted => DeletedAt != null;

    // 🔥 Domain behavior (DDD way)
    public void Delete()
    {
        DeletedAt = DateTime.UtcNow;
    }

    public void Restore()
    {
        DeletedAt = null;
    }
}