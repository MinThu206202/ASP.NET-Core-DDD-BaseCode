using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserApp.Application.Products.Interfaces;
using UserApp.Domain.Products;
using UserApp.Web.ViewModels;

namespace UserApp.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductApiController : BaseApiController<Product, ProductViewModel>
{
    public ProductApiController(IProductService service, IMapper mapper) : base(service, mapper)
    {
    }
}
