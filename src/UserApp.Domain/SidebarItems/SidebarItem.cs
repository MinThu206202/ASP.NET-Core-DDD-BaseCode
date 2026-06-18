using UserApp.Domain.Common;
using UserApp.Domain.SidebarGroups;

namespace UserApp.Domain.SidebarItems;

public class SidebarItem : Entity<Guid>
{
    public string ModuleName { get; set; } = string.Empty;
    public string ControllerName { get; set; } = string.Empty;
    public string? AreaName { get; set; }
    public Guid GroupId { get; set; }
    public SidebarGroup Group { get; set; } = null!;
    public string? IconSvg { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
