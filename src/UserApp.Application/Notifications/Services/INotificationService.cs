using UserApp.Application.Notifications.DTOs;

public interface INotificationService
{
    Task SendAsync(
        CreateNotificationRequest request,
        CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default);

    Task MarkAsSeenAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationDto>>
        GetNotificationsAsync(
            Guid recipientId,
            CancellationToken cancellationToken = default);

    Task<int>
        GetUnreadCountAsync(
            Guid recipientId,
            CancellationToken cancellationToken = default);
}