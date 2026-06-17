using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using UserApp.Application.Notifications.Interfaces;
using UserApp.Domain.Notifications;
using UserApp.Web.ViewModels;

namespace UserApp.Web.Controllers;

public class NotificationController : BaseController<Notification, NotificationViewModel>
{
    public NotificationController(INotificationService service, IMapper mapper) : base(service, mapper)
    {
    }
}
