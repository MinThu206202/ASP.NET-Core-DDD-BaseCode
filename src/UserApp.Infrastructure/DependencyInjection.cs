using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UserApp.Application.Users;
using UserApp.Application.Users.Interfaces;
using UserApp.Domain.Users;
using UserApp.Infrastructure.Identity;
using UserApp.Infrastructure.Persistence;
using UserApp.Infrastructure.Persistence.Repositories;

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
        return services;
    }
}
