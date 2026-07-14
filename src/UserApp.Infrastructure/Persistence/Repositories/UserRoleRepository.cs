using Microsoft.EntityFrameworkCore;
using UserApp.Domain.Roles;

namespace UserApp.Infrastructure.Persistence.Repositories;

public class UserRoleRepository : IUserRoleRepository
{
    private readonly AppDbContext _db;

    public UserRoleRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(UserRole userRole)
    {
        await _db.Set<UserRole>().AddAsync(userRole);
    }

    public async Task SaveAsync()
    {
        await _db.SaveChangesAsync();
    }

}