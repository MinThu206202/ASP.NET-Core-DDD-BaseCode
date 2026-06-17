using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using UserApp.Application.Cars.Interfaces;
using UserApp.Domain.Cars;
using UserApp.Web.ViewModels;

namespace UserApp.Web.Controllers;

public class CarController : BaseController<Car, CarViewModel>
{
    public CarController(ICarService service, IMapper mapper) : base(service, mapper)
    {
    }
}
