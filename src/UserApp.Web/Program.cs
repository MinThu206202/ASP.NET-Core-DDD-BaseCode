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
using UserApp.Application.Common;
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
using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;


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
builder.Services.AddSingleton<ICacheService, RedisCacheService>();

// ------------------------------------------------
// Health Checks
// ------------------------------------------------
var mysqlConn = builder.Configuration.GetConnectionString("MySql");
builder.Services.AddHealthChecks()
    .AddMySql(mysqlConn!, name: "mysql", tags: new[] { "db", "mysql" })
    .AddRedis(redisConn, name: "redis", tags: new[] { "cache", "redis" });

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

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<UserApp.Web.Validators.LoginViewModelValidator>();

// ------------------------------------------------
// Swagger / OpenAPI
// ------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "UserApp API",
        Version = "v1",
        Description = "ASP.NET Core Clean Architecture API"
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your JWT token"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured. Set it in appsettings.Development.json or User Secrets.");

var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

if (keyBytes.Length < 32)
    throw new InvalidOperationException($"Jwt:Key must be at least 32 bytes (256 bits). Current length: {keyBytes.Length} bytes.");

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
// Rate Limiting (IP-based sliding window for auth endpoints)
// ------------------------------------------------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("AuthPolicy", context =>
    {
        var ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                 ?? context.Connection.RemoteIpAddress?.ToString()
                 ?? "unknown";

        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

        int permitLimit = path switch
        {
            var p when p.Contains("forgot-password") || p.Contains("resend-otp") => 3,
            var p when p.Contains("login") || p.Contains("register") => 5,
            _ => 10
        };

        return RateLimitPartition.GetSlidingWindowLimiter(ip, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 1
        });
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
    builder.Services.AddMiniProfiler(options =>
    {
        options.RouteBasePath = "/profiler";
        options.PopupShowTimeWithChildren = true;
        options.PopupShowTrivial = false;
    }).AddEntityFramework();
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Sync migration history: mark auto-generated migrations as applied if their table already exists
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await SyncMigrationHistoryAsync(db, logger);

    await UserApp.Infrastructure.Persistence.Seed.RbacSeeder.SeedRolesAsync(db);
    await UserApp.Infrastructure.Persistence.Seed.RbacSeeder.SeedPermissionsAsync(db);
    await UserApp.Infrastructure.Persistence.Seed.RbacSeeder.SeedAdminRolePermissionsAsync(db);
    await UserApp.Infrastructure.Persistence.Seed.UserSeeder.SeedUsersAsync(scope.ServiceProvider);
}

static async Task SyncMigrationHistoryAsync(AppDbContext db, ILogger logger)
{
    // Sync auto-generated table migrations (create if table already exists)
    try
    {
        await db.Database.ExecuteSqlRawAsync(@"
        ");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to sync auto-generated table migrations");
    }

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
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to mark DropCategoryPriceAndDescription migration as applied");
    }
}

await app.InitializeDatabaseAsync();

// ... (database migrations and environments check)

app.Use(async (context, next) =>
{
    UserApp.Application.Common.ServiceProviderAccessor.Current = context.RequestServices;
    await next();
});

// Global exception handler (must be early in pipeline)
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

        logger.LogError(exception, "Unhandled exception: {Message}", exception?.Message);

        var isApiRequest = context.Request.Path.StartsWithSegments("/api") ||
                          context.Request.Headers.Accept.ToString().Contains("application/json");

        if (isApiRequest)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new ApiResponse<object>
            {
                Success = false,
                Message = "An unexpected error occurred. Please try again later."
            });
        }
        else
        {
            context.Response.Redirect("/Error/500");
        }
    });
});

app.UseStaticFiles();

// Swagger / OpenAPI
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "UserApp API v1");
    options.RoutePrefix = "swagger";
});

app.UseRouting();


// 2. 🔥 ACTIVATE CORS MIDDLEWARE (MUST BE PLACED IN THIS EXACT ORDER)
app.UseCors(myAllowSpecificOrigins);

app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseMiniProfiler();
}

app.UseAuthentication();
app.UseAuthorization();

// ------------------------------------------------
// ROUTES
// ------------------------------------------------

// Health check endpoints
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.ToString()
            }),
            totalDuration = report.TotalDuration.ToString()
        };
        await context.Response.WriteAsJsonAsync(result);
    }
});

// Health check for Docker (simple 200 OK)
app.MapHealthChecks("/health/ready");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}"
);

app.Run();

