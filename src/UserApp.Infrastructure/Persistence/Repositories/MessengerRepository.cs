using UserApp.Domain.Messengers;
using UserApp.Infrastructure.Persistence;

namespace UserApp.Infrastructure.Persistence.Repositories;

public class MessengerRepository : BaseRepository<Messenger>, IMessengerRepository
{
    public MessengerRepository(AppDbContext db) : base(db)
    {
    }
}
