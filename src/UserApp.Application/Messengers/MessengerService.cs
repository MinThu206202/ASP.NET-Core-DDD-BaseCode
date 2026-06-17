using UserApp.Application.Common;
using UserApp.Domain.Messengers;
using UserApp.Application.Messengers.Interfaces;

namespace UserApp.Application.Messengers;

public class MessengerService : BaseService<Messenger>, IMessengerService
{
    public MessengerService(IMessengerRepository repo) : base(repo)
    {
    }
}
