using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserApp.Application.Paps.Interfaces;
using UserApp.Domain.Paps;
using UserApp.Web.ViewModels;

namespace UserApp.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PapApiController : BaseApiController<Pap, PapViewModel>
{
    public PapApiController(IPapService service, IMapper mapper) : base(service, mapper)
    {
    }
}
