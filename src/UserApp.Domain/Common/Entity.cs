using System.Text.Json.Serialization;

namespace UserApp.Domain.Common;

public abstract class Entity<TId> where TId : notnull
{
    [JsonInclude]
    public TId Id { get; protected set; } = default!;
    [JsonInclude]
    public DateTime CreatedAt { get; protected set; } = TimeHelper.Now;
    [JsonInclude]
    public DateTime? UpdatedAt { get; protected set; }

    [JsonInclude]
    public DateTime? DeletedAt { get; private set; }

    public bool IsDeleted => DeletedAt != null;

    public void Delete()
    {
        DeletedAt = TimeHelper.Now;
    }

    public void Restore()
    {
        DeletedAt = null;
    }
}