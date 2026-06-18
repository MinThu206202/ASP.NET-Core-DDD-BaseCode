using Microsoft.EntityFrameworkCore;
using UserApp.Domain.SidebarItems;

namespace UserApp.Infrastructure.Persistence.Repositories;

public class SidebarItemRepository : BaseRepository<SidebarItem>, ISidebarItemRepository
{
    public SidebarItemRepository(AppDbContext db) : base(db)
    {
    }

    public async Task<List<SidebarItem>> GetActiveAsync()
    {
        return await _set
            .Include(x => x.Group)
            .Where(x => x.IsActive && x.DeletedAt == null)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync();
    }
}
