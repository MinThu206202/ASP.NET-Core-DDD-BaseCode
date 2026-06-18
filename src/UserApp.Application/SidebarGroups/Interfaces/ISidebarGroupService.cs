using UserApp.Application.Common;
using UserApp.Domain.SidebarGroups;

namespace UserApp.Application.SidebarGroups.Interfaces;

public interface ISidebarGroupService : IBaseService<SidebarGroup>
{
    Task<List<SidebarGroup>> GetAllOrderedAsync();
}
