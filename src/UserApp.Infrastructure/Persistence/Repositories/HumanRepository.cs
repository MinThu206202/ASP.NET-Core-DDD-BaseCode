using UserApp.Domain.Humans;
using UserApp.Infrastructure.Persistence;

namespace UserApp.Infrastructure.Persistence.Repositories;

public class HumanRepository : BaseRepository<Human>, IHumanRepository
{
    public HumanRepository(AppDbContext db) : base(db)
    {
    }
}
