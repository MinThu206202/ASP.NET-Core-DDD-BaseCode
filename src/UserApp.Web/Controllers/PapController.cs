using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using UserApp.Application.Paps.Interfaces;
using UserApp.Domain.Paps;
using UserApp.Web.ViewModels;

namespace UserApp.Web.Controllers;

public class PapController : BaseController<Pap, PapViewModel>
{
    public PapController(IPapService service, IMapper mapper) : base(service, mapper)
    {
    }
}
