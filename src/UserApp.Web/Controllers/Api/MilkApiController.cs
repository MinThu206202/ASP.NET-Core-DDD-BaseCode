using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserApp.Application.Milks.Interfaces;
using UserApp.Domain.Milks;
using UserApp.Web.ViewModels;

namespace UserApp.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MilkApiController : BaseApiController<Milk, MilkViewModel>
{
    public MilkApiController(IMilkService service, IMapper mapper) : base(service, mapper)
    {
    }
}
