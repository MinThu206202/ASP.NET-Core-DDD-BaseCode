namespace UserApp.Domain.Notifications;

public enum NotificationStatus
{
    Created = 1,

    Queued = 2,

    Delivered = 3,

    Failed = 4,

    Archived = 5
}