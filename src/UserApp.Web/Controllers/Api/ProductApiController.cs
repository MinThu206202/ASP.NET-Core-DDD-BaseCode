using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserApp.Domain.Products;
using UserApp.Web.ViewModels;
using UserApp.Application.Products.Interfaces;


namespace UserApp.Web.Controllers.Api;

[Route("api/[controller]")]
[ApiController]
[Authorize] // JWT required
public class ProductApiController : BaseApiController<Product, ProductViewModel>
{
    public ProductApiController(IProductService service, IMapper mapper)
        : base(service, mapper)
    {
    }
}