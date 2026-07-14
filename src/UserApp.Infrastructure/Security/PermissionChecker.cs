using Microsoft.EntityFrameworkCore;
using UserApp.Application.Common.Interfaces;
using UserApp.Domain.Roles;
using UserApp.Infrastructure.Persistence;

namespace UserApp.Infrastructure.Security;

public class PermissionChecker : IPermissionChecker
{
    private readonly AppDbContext _context;

    public PermissionChecker(AppDbContext context)
    {
        _context = context;
    }

    public async Task<HashSet<string>> GetUserPermissionsAsync(Guid userId)
    {
        var permissions = await (
            from ur in _context.Set<UserRole>()
            join rp in _context.Set<RolePermission>()
                on ur.RoleId equals rp.RoleId
            join p in _context.Set<Permission>()
                on rp.PermissionId equals p.Id
            where ur.UserId == userId
            select p.Name
        ).Distinct().ToListAsync();

        return permissions.ToHashSet();
    }

    public async Task<bool> HasPermissionAsync(Guid userId, string permission)
    {
        var permissions = await GetUserPermissionsAsync(userId);

        return permissions.Contains(permission);
    }
}