using Microsoft.AspNetCore.SignalR;
using UserApp.Domain.Notifications;
using UserApp.Infrastructure.Notifications.Hubs;

namespace UserApp.Infrastructure.Notifications.Channels;

public sealed class SignalRNotificationChannel
    : INotificationChannel
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotificationChannel(
        IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public string Name => "SignalR";

    public bool CanHandle(Notification notification)
    {
        return true;
    }

    public async Task SendAsync(
        Notification notification,
        CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients
            .Group($"user:{notification.RecipientId}")
            .SendAsync(
                "ReceiveNotification",
                new
                {
                    notification.Id,
                    notification.Title,
                    notification.Message,
                    notification.Type,
                    notification.Priority,
                    notification.ActionUrl,
                    notification.CreatedAt
                },
                cancellationToken);
    }
}