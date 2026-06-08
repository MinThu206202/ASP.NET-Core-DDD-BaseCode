
using UserApp.Domain.Mobiles;
using UserApp.Infrastructure.Persistence;

namespace UserApp.Infrastructure.Persistence.Repositories;

public class MobileRepository : BaseRepository<Mobile>, IMobileRepository
{
    public MobileRepository(AppDbContext db) : base(db)
    {
    }
}