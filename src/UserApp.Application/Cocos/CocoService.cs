using UserApp.Application.Common;
using UserApp.Domain.Cocos;
using UserApp.Application.Cocos.Interfaces;

namespace UserApp.Application.Cocos;

public class CocoService : BaseService<Coco>, ICocoService
{
    public CocoService(ICocoRepository repo) : base(repo)
    {
    }
}
