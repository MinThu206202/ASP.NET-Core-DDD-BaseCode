using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using UserApp.Application.Humans.Interfaces;
using UserApp.Domain.Humans;
using UserApp.Web.ViewModels;

namespace UserApp.Web.Controllers;

public class HumanController : BaseController<Human, HumanViewModel>
{
    public HumanController(IHumanService service, IMapper mapper) : base(service, mapper)
    {
    }
}
