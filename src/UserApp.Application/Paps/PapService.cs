using UserApp.Application.Common;
using UserApp.Domain.Paps;
using UserApp.Application.Paps.Interfaces;

namespace UserApp.Application.Paps;

public class PapService : BaseService<Pap>, IPapService
{
    public PapService(IPapRepository repo) : base(repo)
    {
    }
}
