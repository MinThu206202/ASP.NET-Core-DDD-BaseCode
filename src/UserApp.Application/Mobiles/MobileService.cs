
using UserApp.Domain.Mobiles;
using UserApp.Application.Common;
using UserApp.Application.Mobiles.Interfaces;

namespace UserApp.Application.Mobiles;

public class MobileService : BaseService<Mobile>, IMobileService
{
    public MobileService(IMobileRepository repo) : base(repo)
    {
    }
}