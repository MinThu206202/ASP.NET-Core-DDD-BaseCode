using Microsoft.EntityFrameworkCore;
using UserApp.Domain.Media;
using UserApp.Infrastructure.Persistence;

namespace UserApp.Infrastructure.Media;

public class MediaRepository : IMediaRepository
{
    private readonly AppDbContext _db;

    public MediaRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(MediaFile entity)
    {
        await _db.Set<MediaFile>().AddAsync(entity);
    }

    public async Task<MediaFile?> GetByIdAsync(Guid id)
    {
        return await _db.Set<MediaFile>().FindAsync(id);
    }

    public async Task<List<MediaFile>> GetByEntityAsync(string entityName, Guid entityId)
    {
        return await _db.Set<MediaFile>()
            .Where(x => x.EntityName == entityName && x.EntityId == entityId)
            .ToListAsync();
    }

    public void Remove(MediaFile entity)
    {
        _db.Set<MediaFile>().Remove(entity);
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}