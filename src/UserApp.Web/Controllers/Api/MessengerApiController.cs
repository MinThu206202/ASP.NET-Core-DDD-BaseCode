using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserApp.Application.Messengers.Interfaces;
using UserApp.Domain.Messengers;
using UserApp.Web.ViewModels;

namespace UserApp.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessengerApiController : BaseApiController<Messenger, MessengerViewModel>
{
    public MessengerApiController(IMessengerService service, IMapper mapper) : base(service, mapper)
    {
    }
}
