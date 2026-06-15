using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserApp.Application.CommonTables.Interfaces;
using UserApp.Domain.CommonTables;
using UserApp.Web.ViewModels.CommonTables;

namespace UserApp.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class CommonTableApiController : BaseApiController<CommonTable, CommonTableViewModel>
{
    public CommonTableApiController(ICommonTableService service, IMapper mapper) : base(service, mapper)
    {
    }
}
