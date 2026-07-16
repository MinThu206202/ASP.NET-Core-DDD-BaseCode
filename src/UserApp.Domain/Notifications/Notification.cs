using UserApp.Domain.Common;

namespace UserApp.Domain.Notifications;

public class Notification : Entity<Guid>
{
    // Required by EF Core
    private Notification()
    {
    }

    public Notification(
        Guid recipientId,
        NotificationType type,
        string title,
        string message,
        Guid? senderId = null,
        NotificationPriority priority = NotificationPriority.Normal,
        string? actionUrl = null,
        string? metadata = null,
        DateTime? expiresAt = null)
    {
        if (recipientId == Guid.Empty)
            throw new ArgumentException("Recipient is required.", nameof(recipientId));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message is required.", nameof(message));

        RecipientId = recipientId;
        SenderId = senderId;
        Title = title.Trim();
        Message = message.Trim();
        Type = type;
        Priority = priority;
        Status = NotificationStatus.Created;
        ActionUrl = actionUrl;
        Metadata = metadata;
        ExpiresAt = expiresAt;

        SeenAt = null;
        ReadAt = null;
    }

    public Guid RecipientId { get; private set; }

    public Guid? SenderId { get; private set; }

    public string Title { get; private set; } = default!;

    public string Message { get; private set; } = default!;

    public NotificationType Type { get; private set; }

    public NotificationPriority Priority { get; private set; }

    public NotificationStatus Status { get; private set; }

    public string? ActionUrl { get; private set; }

    public string? Metadata { get; private set; }

    public DateTime? SeenAt { get; private set; }

    public DateTime? ReadAt { get; private set; }

    public DateTime? ExpiresAt { get; private set; }

    public bool IsSeen => SeenAt.HasValue;

    public bool IsRead => ReadAt.HasValue;

    public bool IsExpired =>
        ExpiresAt.HasValue &&
        ExpiresAt.Value <= DateTime.UtcNow;

    public void MarkAsQueued()
    {
        EnsureNotArchived();

        Status = NotificationStatus.Queued;
    }

    public void MarkAsDelivered()
    {
        EnsureNotArchived();

        Status = NotificationStatus.Delivered;
    }

    public void MarkAsFailed()
    {
        EnsureNotArchived();

        Status = NotificationStatus.Failed;
    }

    public void MarkAsSeen()
    {
        EnsureNotArchived();

        if (SeenAt.HasValue)
            return;

        SeenAt = DateTime.UtcNow;
    }

    public void MarkAsRead()
    {
        EnsureNotArchived();

        if (ReadAt.HasValue)
            return;

        MarkAsSeen();

        ReadAt = DateTime.UtcNow;
    }

    public void Archive()
    {
        Status = NotificationStatus.Archived;
    }

    private void EnsureNotArchived()
    {
        if (Status == NotificationStatus.Archived)
            throw new InvalidOperationException(
                "Archived notifications cannot be modified.");
    }
}