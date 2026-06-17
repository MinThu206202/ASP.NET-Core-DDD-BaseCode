using UserApp.Application.Common;
using UserApp.Domain.Cars;
using UserApp.Application.Cars.Interfaces;

namespace UserApp.Application.Cars;

public class CarService : BaseService<Car>, ICarService
{
    public CarService(ICarRepository repo) : base(repo)
    {
    }
}
