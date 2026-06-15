using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using UserApp.Application.Messengers.Interfaces;
using UserApp.Domain.Messengers;
using UserApp.Web.ViewModels;

namespace UserApp.Web.Controllers;

public class MessengerController : BaseController<Messenger, MessengerViewModel>
{
    public MessengerController(IMessengerService service, IMapper mapper) : base(service, mapper)
    {
    }
}
