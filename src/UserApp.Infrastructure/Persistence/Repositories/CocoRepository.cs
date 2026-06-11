using UserApp.Domain.Cocos;
using UserApp.Infrastructure.Persistence;

namespace UserApp.Infrastructure.Persistence.Repositories;

public class CocoRepository : BaseRepository<Coco>, ICocoRepository
{
    public CocoRepository(AppDbContext db) : base(db)
    {
    }
}
