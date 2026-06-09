using UserApp.Domain.Media;

namespace UserApp.Domain.Media;

public interface IMediaRepository
{
    Task AddAsync(MediaFile entity);

    Task<MediaFile?> GetByIdAsync(Guid id);

    Task<List<MediaFile>> GetByEntityAsync(string entityName, Guid entityId);

    void Remove(MediaFile entity);

    Task SaveChangesAsync();
}