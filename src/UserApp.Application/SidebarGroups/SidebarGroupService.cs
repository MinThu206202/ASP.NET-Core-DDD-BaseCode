using UserApp.Application.Common;
using UserApp.Domain.SidebarGroups;
using UserApp.Application.SidebarGroups.Interfaces;

namespace UserApp.Application.SidebarGroups;

public class SidebarGroupService : BaseService<SidebarGroup>, ISidebarGroupService
{
    private readonly ISidebarGroupRepository _groupRepo;

    public SidebarGroupService(ISidebarGroupRepository repo) : base(repo)
    {
        _groupRepo = repo;
    }

    public Task<List<SidebarGroup>> GetAllOrderedAsync()
    {
        return _groupRepo.GetAllOrderedAsync();
    }
}
