using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using UserApp.Application.Products.Interfaces;
using UserApp.Domain.Products;
using UserApp.Web.ViewModels;

namespace UserApp.Web.Controllers;

public class ProductController : BaseController<Product, ProductViewModel>
{
    public ProductController(IProductService service, IMapper mapper) : base(service, mapper)
    {
    }
}
