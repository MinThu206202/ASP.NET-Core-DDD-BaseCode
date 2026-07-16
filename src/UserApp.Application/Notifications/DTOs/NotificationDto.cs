using UserApp.Domain.Notifications;

namespace UserApp.Application.Notifications.DTOs;

public sealed class NotificationDto
{
    public Guid Id { get; set; }

    public Guid RecipientId { get; set; }

    public Guid? SenderId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public NotificationType Type { get; set; }

    public NotificationPriority Priority { get; set; }

    public NotificationStatus Status { get; set; }

    public string? ActionUrl { get; set; }

    public string? Metadata { get; set; }

    public DateTime? SeenAt { get; set; }

    public DateTime? ReadAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsRead => ReadAt.HasValue;

    public bool IsSeen => SeenAt.HasValue;

    public bool IsExpired =>
        ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;
}