using UserApp.Domain.Cars;
using UserApp.Infrastructure.Persistence;

namespace UserApp.Infrastructure.Persistence.Repositories;

public class CarRepository : BaseRepository<Car>, ICarRepository
{
    public CarRepository(AppDbContext db) : base(db)
    {
    }
}
