using Microsoft.AspNetCore.Http;
using UserApp.Application.Common.Interfaces;
using UserApp.Domain.Media;
using MediaEntity = UserApp.Domain.Media.MediaFile;


namespace UserApp.Infrastructure.Media;

public class MediaPipeline : IMediaPipeline
{
    private readonly IMediaRepository _repo;
    private readonly MediaStorage _storage;

    public MediaPipeline(IMediaRepository repo, MediaStorage storage)
    {
        _repo = repo;
        _storage = storage;
    }

    public async Task HandleCreateAsync(string entityName, object entity, object? file)
    {
        if (file is not IFormFile f) return;

        var path = await _storage.SaveAsync(f);

        await _repo.AddAsync(new MediaEntity
        {
            EntityName = entityName,
            EntityId = GetId(entity),
            Url = path,
            OriginalName = f.FileName,
            MimeType = f.ContentType
        });
    }

    public Task HandleUpdateAsync(string entityName, object entity, object? file)
        => HandleCreateAsync(entityName, entity, file);

    public async Task HandleDeleteAsync(string entityName, object entity)
    {
        var id = GetId(entity);

        var items = await _repo.GetByEntityAsync(entityName, id);

        foreach (var item in items)
        {
            _repo.Remove(item);
        }

        await _repo.SaveChangesAsync();
    }

    private Guid GetId(object entity)
    {
        var prop = entity.GetType().GetProperty("Id");
        return (Guid)(prop?.GetValue(entity) ?? Guid.Empty);
    }
}