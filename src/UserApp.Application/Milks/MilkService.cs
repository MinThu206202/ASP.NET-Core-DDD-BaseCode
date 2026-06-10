using UserApp.Application.Common;
using UserApp.Domain.Milks;
using UserApp.Application.Milks.Interfaces;

namespace UserApp.Application.Milks;

public class MilkService : BaseService<Milk>, IMilkService
{
    public MilkService(IMilkRepository repo) : base(repo)
    {
    }
}
