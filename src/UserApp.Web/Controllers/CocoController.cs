using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using UserApp.Application.Cocos.Interfaces;
using UserApp.Domain.Cocos;
using UserApp.Web.ViewModels;

namespace UserApp.Web.Controllers;

public class CocoController : BaseController<Coco, CocoViewModel>
{
    public CocoController(ICocoService service, IMapper mapper) : base(service, mapper)
    {
    }
}
