using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserApp.Application.Cars.Interfaces;
using UserApp.Domain.Cars;
using UserApp.Web.ViewModels;

namespace UserApp.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CarApiController : BaseApiController<Car, CarViewModel>
{
    public CarApiController(ICarService service, IMapper mapper) : base(service, mapper)
    {
    }
}
