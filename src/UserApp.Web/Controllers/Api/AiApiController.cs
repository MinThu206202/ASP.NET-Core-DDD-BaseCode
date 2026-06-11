using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserApp.Application.Ais.Interfaces;
using UserApp.Domain.Ais;
using UserApp.Web.ViewModels;

namespace UserApp.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AiApiController : BaseApiController<Ai, AiViewModel>
{
    public AiApiController(IAiService service, IMapper mapper) : base(service, mapper)
    {
    }
}
