using UserApp.Application.Notifications.Interfaces;
using UserApp.Domain.Notifications;
using UserApp.Infrastructure.Notifications.Channels;

namespace UserApp.Infrastructure.Notifications.Dispatchers;

public sealed class NotificationDispatcher
    : INotificationDispatcher
{
    private readonly IEnumerable<INotificationChannel> _channels;

    public NotificationDispatcher(
        IEnumerable<INotificationChannel> channels)
    {
        _channels = channels;
    }

    public async Task DispatchAsync(
        Notification notification,
        CancellationToken cancellationToken = default)
    {
        foreach (var channel in _channels)
        {
            if (!channel.CanHandle(notification))
                continue;

            await channel.SendAsync(
                notification,
                cancellationToken);
        }
    }
}