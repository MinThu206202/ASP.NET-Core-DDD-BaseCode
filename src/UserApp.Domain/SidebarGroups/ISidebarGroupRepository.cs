using UserApp.Domain.Common;

namespace UserApp.Domain.SidebarGroups;

public interface ISidebarGroupRepository : IBaseRepository<SidebarGroup>
{
    Task<List<SidebarGroup>> GetAllOrderedAsync();
}
