using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UserApp.Application.Users.Interfaces;
using UserApp.Domain.Roles;
using UserApp.Domain.Users;
using UserApp.Infrastructure.Persistence;

namespace UserApp.Infrastructure.Persistence.Seed;

public static class UserSeeder
{
    public static async Task SeedUsersAsync(IServiceProvider services)
    {
        var userService = services.GetRequiredService<IUserService>();
        var roleRepo = services.GetRequiredService<IRoleRepository>();
        var userRoleRepo = services.GetRequiredService<IUserRoleRepository>();
        var db = services.GetRequiredService<AppDbContext>();

        const string adminEmail = "admin@local.com";
        const string userEmail = "user@local.com";

        if (!await db.Set<User>().AnyAsync(u => u.Email.Value == adminEmail))
        {
            await userService.CreateAsync(adminEmail, "Administrator", "Admin123!");

            var admin = await db.Set<User>().FirstOrDefaultAsync(u => u.Email.Value == adminEmail);
            var adminRole = await roleRepo.GetByNameAsync("Admin");
            if (admin != null && adminRole != null)
            {
                await userRoleRepo.AddAsync(new UserRole
                {
                    UserId = admin.Id,
                    RoleId = adminRole.Id
                });
                await userRoleRepo.SaveAsync();
            }
        }

        if (!await db.Set<User>().AnyAsync(u => u.Email.Value == userEmail))
        {
            await userService.CreateAsync(userEmail, "Demo User", "User123!");

            var user = await db.Set<User>().FirstOrDefaultAsync(u => u.Email.Value == userEmail);
            var userRole = await roleRepo.GetByNameAsync("User");
            if (user != null && userRole != null)
            {
                await userRoleRepo.AddAsync(new UserRole
                {
                    UserId = user.Id,
                    RoleId = userRole.Id
                });
                await userRoleRepo.SaveAsync();
            }
        }
    }
}
