using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using UserApp.Application.Ais.Interfaces;
using UserApp.Domain.Ais;
using UserApp.Web.ViewModels;

namespace UserApp.Web.Controllers;

public class AiController : BaseController<Ai, AiViewModel>
{
    public AiController(IAiService service, IMapper mapper) : base(service, mapper)
    {
    }
}
