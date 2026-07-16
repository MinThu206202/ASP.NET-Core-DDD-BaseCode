using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UserApp.Application.Common.Interfaces;
using UserApp.Application.Notifications.Interfaces;
using UserApp.Application.Notifications.Services;
using UserApp.Application.Users;
using UserApp.Application.Users.Interfaces;
using UserApp.Domain.Notifications;
using UserApp.Domain.Users;
using UserApp.Infrastructure.Background;
using UserApp.Infrastructure.Identity;
using UserApp.Infrastructure.Notifications.Channels;
using UserApp.Infrastructure.Notifications.Dispatchers;
using UserApp.Infrastructure.Notifications.Repositories;
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

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<UserService>();

        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
        services.AddScoped<INotificationService, NotificationService>();

        services.AddScoped<INotificationChannel, SignalRNotificationChannel>();

        services.AddScoped<IPermissionRepository, PermissionRepository>();

        services.AddSingleton<IEmailTaskQueue, EmailTaskQueue>();
        services.AddSingleton<EmailTaskQueue>(sp => (EmailTaskQueue)sp.GetRequiredService<IEmailTaskQueue>());
        services.AddHostedService<EmailBackgroundService>();

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
