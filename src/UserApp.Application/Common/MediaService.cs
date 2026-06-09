using UserApp.Application.Common.Interfaces;
using UserApp.Application.Common.Media;
using UserApp.Application.Media;
using UserApp.Domain.Media;

namespace UserApp.Infrastructure.Media;

public class MediaService : IMediaService
{
    private readonly IMediaRepository _repo;

    public MediaService(IMediaRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<MediaDto>> GetAsync(string entityName, Guid entityId)
    {
        var media = await _repo.GetByEntityAsync(entityName, entityId);

        return media.Select(x => new MediaDto
        {
            Url = x.Url,
            OriginalName = x.OriginalName
        }).ToList();
    }

    public async Task UploadAsync(string entityName, Guid entityId, MediaFileInput file)
    {
        if (file == null || file.Data.Length == 0)
            return;

        var fileName = $"{Guid.NewGuid()}.webp";
        var path = $"uploads/{fileName}";

        await File.WriteAllBytesAsync(path, file.Data);

        await _repo.AddAsync(new MediaFile
        {
            EntityName = entityName,
            EntityId = entityId,
            Url = path,
            OriginalName = file.FileName
        });

        await _repo.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid mediaId)
    {
        var media = await _repo.GetByIdAsync(mediaId);
        if (media == null) return;

        _repo.Remove(media);
        await _repo.SaveChangesAsync();
    }
}