using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using UserApp.Infrastructure;
using UserApp.Infrastructure.Persistence;
using UserApp.Web.Mapping;
using UserApp.Infrastructure.Persistence.Repositories;
using UserApp.Domain.Common;
using UserApp.Domain.Users;
using UserApp.Application.Common;
using UserApp.Application.Users;
using UserApp.Application.Users.Interfaces;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using UserApp.Application.Common.Interfaces;
using UserApp.Infrastructure.Media;
using UserApp.Infrastructure.Services;
using UserApp.Domain.Media;
using UserApp.Domain.Roles;
using System.Security.Claims;
using UserApp.Infrastructure.Security;
using UserApp.Web.Common;
using Microsoft.AspNetCore.Authentication.Cookies;
using UserApp.Application.Roles.Interfaces;
using UserApp.Application.Roles;
using UserApp.Application.Permissions.Interfaces;
using UserApp.Application.Permissions;
using UserApp.Domain.CommonTables;
using UserApp.Application.CommonTables;
using UserApp.Application.CommonTables.Interfaces;
// ================= AUTO MODULE IMPORTS =================
// <AUTO-USINGS-START>
// <AUTO-USINGS-END>
using UserApp.Domain.AuditLogs;
using UserApp.Application.AuditLogs;
using UserApp.Application.AuditLogs.Interfaces;
using Quartz;
using UserApp.Web.Jobs;


var builder = WebApplication.CreateBuilder(args);

// ------------------------------------------------
// Infrastructure (DbContext)
// ------------------------------------------------
builder.Services.AddInfrastructure(builder.Configuration);

// ------------------------------------------------
// AutoMapper
// ------------------------------------------------
var mapperConfiguration = new MapperConfiguration(
    cfg =>
    {
        cfg.AddProfile<MappingProfile>();
    },
    NullLoggerFactory.Instance);

builder.Services.AddSingleton(mapperConfiguration.CreateMapper());
// Sidebar provider removed per request

// ------------------------------------------------
// REPOSITORIES
// ------------------------------------------------
builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));

// ================= AUTO REPOSITORIES =================
// <AUTO-REPOSITORIES-START>
builder.Services.AddScoped<ICommonTableRepository, CommonTableRepository>();
// ================= AUTO REPOSITORIES =================
// <AUTO-REPOSITORIES-START>
builder.Services.AddScoped<ICommonTableRepository, CommonTableRepository>();

// <AUTO-REPOSITORIES-END>
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IMediaRepository, MediaRepository>();
builder.Services.AddScoped<IUserRoleRepository, UserRoleRepository>();


// ------------------------------------------------
// SERVICES (Application Layer)
// ------------------------------------------------
builder.Services.AddScoped(typeof(IBaseService<>), typeof(BaseService<>));

// ================= AUTO SERVICES =================
// <AUTO-SERVICES-START>
builder.Services.AddScoped<ICommonTableService, CommonTableService>();

// <AUTO-SERVICES-END>

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IPermissionChecker, PermissionChecker>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IAuditLogArchiveRepository, AuditLogArchiveRepository>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IAuditLogArchiveService, AuditLogArchiveService>();
builder.Services.AddHttpContextAccessor();

var redisConn = builder.Configuration.GetConnectionString("Redis") ?? "127.0.0.1:6379";
builder.Services.AddStackExchangeRedisCache(options => options.Configuration = redisConn);
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<MediaStorage>();
builder.Services.AddScoped<IMediaPipeline, MediaPipeline>();

builder.Services.AddScoped<PermissionFilter>();
builder.Services.AddScoped<AuditContextActionFilter>();
var mvcBuilder = builder.Services.AddControllersWithViews(options =>
{
    options.Filters.AddService<PermissionFilter>();
    options.Filters.AddService<AuditContextActionFilter>();
}).AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

var jwtKey = builder.Configuration["Jwt:Key"] ?? "THIS_IS_DEMO_SECRET_KEY_123456";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/Denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            RoleClaimType = ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync(
                    """{"success":false,"message":"Unauthorized"}"""
                );
            },
            OnForbidden = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync(
                    """{"success":false,"message":"Forbidden"}"""
                );
            }
        };
    });

builder.Services.AddAuthorization();

// 1. ADD CORS POLICY HERE 
var myAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: myAllowSpecificOrigins,
                      policy =>
                      {
                          policy.WithOrigins("http://localhost:5173") // Your Vite Dev Server
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                      });
});

// ------------------------------------------------
// Quartz Scheduled Job: AuditLog Archive at 09:00 daily
// ------------------------------------------------
builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey("AuditLogArchiveJob");

    q.AddJob<AuditLogArchiveJob>(opts => opts.WithIdentity(jobKey));

    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("AuditLogArchiveTrigger")
        .WithCronSchedule("0 0 9 * * ?", x => x.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Asia/Yangon"))));
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

if (builder.Environment.IsDevelopment())
{
    mvcBuilder.AddRazorRuntimeCompilation();
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Sync migration history: mark auto-generated migrations as applied if their table already exists
    await SyncMigrationHistoryAsync(db);

    await db.Database.MigrateAsync();
    await UserApp.Infrastructure.Persistence.Seed.RbacSeeder.SeedRolesAsync(db);
    await UserApp.Infrastructure.Persistence.Seed.RbacSeeder.SeedPermissionsAsync(db);
    await UserApp.Infrastructure.Persistence.Seed.RbacSeeder.SeedAdminRolePermissionsAsync(db);
    await UserApp.Infrastructure.Persistence.Seed.UserSeeder.SeedUsersAsync(scope.ServiceProvider);
}

static async Task SyncMigrationHistoryAsync(AppDbContext db)
{
    // Sync auto-generated table migrations (create if table already exists)
    try
    {
        await db.Database.ExecuteSqlRawAsync(@"
        ");
    }
    catch { }

    // Mark DropCategoryPriceAndDescription as applied (no-op if columns already adjusted)
    try
    {
        var applied = (await db.Database.GetAppliedMigrationsAsync()).ToHashSet();
        if (!applied.Contains("20260617163210_DropCategoryPriceAndDescription"))
        {
            await db.Database.ExecuteSqlRawAsync(
                "INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`) VALUES ('20260617163210_DropCategoryPriceAndDescription', '8.0.0')");
        }
    }
    catch { }
}

await app.InitializeDatabaseAsync();

// ... (database migrations and environments check)

app.Use(async (context, next) =>
{
    UserApp.Application.Common.ServiceProviderAccessor.Current = context.RequestServices;
    await next();
});

app.UseStaticFiles();

app.UseRouting();


// 2. 🔥 ACTIVATE CORS MIDDLEWARE (MUST BE PLACED IN THIS EXACT ORDER)
app.UseCors(myAllowSpecificOrigins);

app.UseAuthentication();
app.UseAuthorization();

// ------------------------------------------------
// ROUTES
// ------------------------------------------------
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}"
);

app.Run();

