using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserApp.Application.Notifications.Interfaces;
using UserApp.Domain.Notifications;
using UserApp.Web.ViewModels;

namespace UserApp.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationApiController : BaseApiController<Notification, NotificationViewModel>
{
    public NotificationApiController(INotificationService service, IMapper mapper) : base(service, mapper)
    {
    }
}
