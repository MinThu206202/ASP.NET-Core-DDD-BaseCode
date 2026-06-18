using Microsoft.EntityFrameworkCore;
using UserApp.Domain.SidebarGroups;

namespace UserApp.Infrastructure.Persistence.Repositories;

public class SidebarGroupRepository : BaseRepository<SidebarGroup>, ISidebarGroupRepository
{
    public SidebarGroupRepository(AppDbContext db) : base(db)
    {
    }

    public async Task<List<SidebarGroup>> GetAllOrderedAsync()
    {
        return await _set
            .Where(x => x.DeletedAt == null)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync();
    }
}
