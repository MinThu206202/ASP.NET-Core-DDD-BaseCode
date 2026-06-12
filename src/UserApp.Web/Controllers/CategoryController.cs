using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using UserApp.Application.Categorys.Interfaces;
using UserApp.Domain.Categorys;
using UserApp.Web.ViewModels;

namespace UserApp.Web.Controllers;

public class CategoryController : BaseController<Category, CategoryViewModel>
{
    public CategoryController(ICategoryService service, IMapper mapper) : base(service, mapper)
    {
    }
}
