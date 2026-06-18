using UserApp.Domain.Common;
using UserApp.Domain.SidebarItems;

namespace UserApp.Domain.SidebarGroups;

public class SidebarGroup : Entity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string? IconSvg { get; set; }
    public bool IsActive { get; set; } = true;
    public List<SidebarItem> Items { get; set; } = new();
}
