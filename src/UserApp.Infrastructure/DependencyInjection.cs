using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UserApp.Application.Users;
using UserApp.Application.Users.Interfaces;
using UserApp.Domain.Users;
using UserApp.Infrastructure.Identity;
using UserApp.Infrastructure.Persistence;
using UserApp.Infrastructure.Persistence.Repositories;
using UserApp.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Builder;
using UserApp.Domain.Roles;

namespace UserApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connStr = config.GetConnectionString("MySql")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:MySql");

        services.AddDbContext<AppDbContext>(opt =>
            opt.UseMySql(connStr, ServerVersion.AutoDetect(connStr)));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<UserService>();
        // Inside your Infrastructure's service registration method
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        return services;
    }

    public static async Task InitializeDatabaseAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();

        await RbacSeeder.SeedRolesAsync(db);
    }
}
