using UserApp.Domain.Notifications;

namespace UserApp.Infrastructure.Notifications.Channels;

public interface INotificationChannel
{
    string Name { get; }

    bool CanHandle(Notification notification);

    Task SendAsync(
        Notification notification,
        CancellationToken cancellationToken = default);
}