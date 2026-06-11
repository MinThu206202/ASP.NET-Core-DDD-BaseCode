using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserApp.Application.Cocos.Interfaces;
using UserApp.Domain.Cocos;
using UserApp.Web.ViewModels;

namespace UserApp.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CocoApiController : BaseApiController<Coco, CocoViewModel>
{
    public CocoApiController(ICocoService service, IMapper mapper) : base(service, mapper)
    {
    }
}
