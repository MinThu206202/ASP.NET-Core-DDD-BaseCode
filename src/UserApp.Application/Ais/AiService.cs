using UserApp.Application.Common;
using UserApp.Domain.Ais;
using UserApp.Application.Ais.Interfaces;

namespace UserApp.Application.Ais;

public class AiService : BaseService<Ai>, IAiService
{
    public AiService(IAiRepository repo) : base(repo)
    {
    }
}
