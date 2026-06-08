
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserApp.Application.Mobiles.Interfaces;
using UserApp.Domain.Mobiles;
using UserApp.Web.ViewModels;

namespace UserApp.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MobileApiController : BaseApiController<Mobile, MobileViewModel>
{
    public MobileApiController(IMobileService service, IMapper mapper)
        : base(service, mapper)
    {
    }
}