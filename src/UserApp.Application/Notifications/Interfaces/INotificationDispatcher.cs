using UserApp.Domain.Notifications;

namespace UserApp.Application.Notifications.Interfaces;

public interface INotificationDispatcher
{
    Task DispatchAsync(
        Notification notification,
        CancellationToken cancellationToken = default);
}