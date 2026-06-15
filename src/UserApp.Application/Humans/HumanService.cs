using UserApp.Application.Common;
using UserApp.Domain.Humans;
using UserApp.Application.Humans.Interfaces;

namespace UserApp.Application.Humans;

public class HumanService : BaseService<Human>, IHumanService
{
    public HumanService(IHumanRepository repo) : base(repo)
    {
    }
}
