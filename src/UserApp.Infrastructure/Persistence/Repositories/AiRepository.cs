using UserApp.Domain.Ais;
using UserApp.Infrastructure.Persistence;

namespace UserApp.Infrastructure.Persistence.Repositories;

public class AiRepository : BaseRepository<Ai>, IAiRepository
{
    public AiRepository(AppDbContext db) : base(db)
    {
    }
}
