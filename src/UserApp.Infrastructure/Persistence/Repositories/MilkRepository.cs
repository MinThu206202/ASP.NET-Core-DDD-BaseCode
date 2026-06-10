using UserApp.Domain.Milks;
using UserApp.Infrastructure.Persistence;

namespace UserApp.Infrastructure.Persistence.Repositories;

public class MilkRepository : BaseRepository<Milk>, IMilkRepository
{
    public MilkRepository(AppDbContext db) : base(db)
    {
    }
}
