
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using UserApp.Application.Mobiles.Interfaces;
using UserApp.Domain.Mobiles;
using UserApp.Web.ViewModels;

namespace UserApp.Web.Controllers;

public class MobileController : BaseController<Mobile, MobileViewModel>
{
    public MobileController(IMobileService service, IMapper mapper)
        : base(service, mapper)
    {
    }
}