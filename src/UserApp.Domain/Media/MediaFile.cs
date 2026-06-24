using UserApp.Domain.Common;

namespace UserApp.Domain.Media;

public class MediaFile
{
    public Guid Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public Guid EntityId { get; set; }

    public string Url { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = TimeHelper.Now;
}