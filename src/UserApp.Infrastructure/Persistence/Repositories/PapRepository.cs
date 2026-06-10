using UserApp.Domain.Paps;
using UserApp.Infrastructure.Persistence;

namespace UserApp.Infrastructure.Persistence.Repositories;

public class PapRepository : BaseRepository<Pap>, IPapRepository
{
    public PapRepository(AppDbContext db) : base(db)
    {
    }
}
