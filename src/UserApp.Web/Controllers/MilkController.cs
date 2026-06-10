using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using UserApp.Application.Milks.Interfaces;
using UserApp.Domain.Milks;
using UserApp.Web.ViewModels;

namespace UserApp.Web.Controllers;

public class MilkController : BaseController<Milk, MilkViewModel>
{
    public MilkController(IMilkService service, IMapper mapper) : base(service, mapper)
    {
    }
}
