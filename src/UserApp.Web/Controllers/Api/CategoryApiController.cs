using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserApp.Application.Categorys.Interfaces;
using UserApp.Domain.Categorys;
using UserApp.Web.ViewModels;

namespace UserApp.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoryApiController : BaseApiController<Category, CategoryViewModel>
{
    public CategoryApiController(ICategoryService service, IMapper mapper) : base(service, mapper)
    {
    }
}
