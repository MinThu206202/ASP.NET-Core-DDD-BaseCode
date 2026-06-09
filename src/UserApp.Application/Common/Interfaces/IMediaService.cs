using UserApp.Application.Common.Media;
using UserApp.Application.Media;

namespace UserApp.Application.Common.Interfaces;

public interface IMediaService
{
    Task<List<MediaDto>> GetAsync(string entityName, Guid entityId);

    Task UploadAsync(string entityName, Guid entityId, MediaFileInput file);

    Task DeleteAsync(Guid mediaId);
}