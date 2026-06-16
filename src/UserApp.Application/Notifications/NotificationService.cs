using UserApp.Application.Common;
using UserApp.Domain.Notifications;
using UserApp.Application.Notifications.Interfaces;

namespace UserApp.Application.Notifications;

public class NotificationService : BaseService<Notification>, INotificationService
{
    public NotificationService(INotificationRepository repo) : base(repo)
    {
    }
}
