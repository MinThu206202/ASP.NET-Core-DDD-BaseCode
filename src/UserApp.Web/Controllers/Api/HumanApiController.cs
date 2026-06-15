using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserApp.Application.Humans.Interfaces;
using UserApp.Domain.Humans;
using UserApp.Web.ViewModels;

namespace UserApp.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HumanApiController : BaseApiController<Human, HumanViewModel>
{
    public HumanApiController(IHumanService service, IMapper mapper) : base(service, mapper)
    {
    }
}
