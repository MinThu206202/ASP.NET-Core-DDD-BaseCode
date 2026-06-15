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
using UserApp.Application.Users.Interfaces;

using UserApp.Application.Users;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using UserApp.Application.Common.Interfaces;
using UserApp.Infrastructure.Media;
using UserApp.Domain.Media;

using UserApp.Domain.Media;
using UserApp.Domain.Paps;
using UserApp.Application.Paps;
using UserApp.Application.Paps.Interfaces;
using UserApp.Domain.Milks;
using UserApp.Application.Milks;
using UserApp.Application.Milks.Interfaces;
using UserApp.Domain.Ais;
using UserApp.Application.Ais;
using UserApp.Application.Ais.Interfaces;
using UserApp.Domain.Cocos;
using UserApp.Application.Cocos;
using UserApp.Application.Cocos.Interfaces;
using UserApp.Domain.Roles;
using UserApp.Infrastructure.Persistence.Repositories;
using System.Security.Claims;
using UserApp.Infrastructure.Security;
using UserApp.Web.Common;
using Microsoft.AspNetCore.Authentication.Cookies;
using UserApp.Application.Roles.Interfaces;
using UserApp.Application.Roles;
using UserApp.Application.Permissions.Interfaces;
using UserApp.Application.Permissions;
using UserApp.Domain.Categorys;
using UserApp.Application.Categorys;
using UserApp.Application.Categorys.Interfaces;
using UserApp.Domain.Payments;
using UserApp.Application.Payments;
using UserApp.Application.Payments.Interfaces;
using UserApp.Domain.CommonTables;
using UserApp.Application.CommonTables;
using UserApp.Application.CommonTables.Interfaces;
using UserApp.Domain.Products;
using UserApp.Application.Products;
using UserApp.Application.Products.Interfaces;
using UserApp.Domain.Humans;
using UserApp.Application.Humans;
using UserApp.Application.Humans.Interfaces;

// ================= AUTO MODULE IMPORTS =================
// <AUTO-USINGS-START>
// <AUTO-USINGS-END>


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

// ------------------------------------------------
// REPOSITORIES
// ------------------------------------------------
builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));

// ================= AUTO REPOSITORIES =================
// <AUTO-REPOSITORIES-START>
builder.Services.AddScoped<IPapRepository, PapRepository>();
builder.Services.AddScoped<IMilkRepository, MilkRepository>();
builder.Services.AddScoped<IAiRepository, AiRepository>();
builder.Services.AddScoped<ICocoRepository, CocoRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<ICommonTableRepository, CommonTableRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IHumanRepository, HumanRepository>();
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
builder.Services.AddScoped<IPapService, PapService>();
builder.Services.AddScoped<IMilkService, MilkService>();
builder.Services.AddScoped<IAiService, AiService>();
builder.Services.AddScoped<ICocoService, CocoService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ICommonTableService, CommonTableService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IHumanService, HumanService>();
// <AUTO-SERVICES-END>

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IPermissionChecker, PermissionChecker>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<
    UserApp.Application.Common.Interfaces.IModuleGeneratorService,
    UserApp.Infrastructure.Services.ModuleGeneratorService>();
builder.Services.AddScoped<MediaStorage>();
builder.Services.AddScoped<IMediaPipeline, MediaPipeline>();


builder.Services.AddScoped<PermissionFilter>();
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.AddService<PermissionFilter>();
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

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    await db.Database.MigrateAsync();
    await UserApp.Infrastructure.Persistence.Seed.RbacSeeder.SeedRolesAsync(db);
    await UserApp.Infrastructure.Persistence.Seed.RbacSeeder.SeedPermissionsAsync(db);
    await UserApp.Infrastructure.Persistence.Seed.RbacSeeder.SeedAdminRolePermissionsAsync(db);
    await SeedFlashMessages(db);
}

static async Task SeedFlashMessages(AppDbContext db)
{
    var existing = db.Set<UserApp.Domain.CommonTables.CommonTable>()
        .Where(x => x.Type == "FlashMessage")
        .Select(x => x.Code)
        .ToHashSet();

    var modules = new[] { "Category", "Milk", "Ai", "Pap", "Coco" };
    var actions = new[] { "Create", "Edit", "Delete" };

    foreach (var module in modules)
    {
        foreach (var action in actions)
        {
            var code = $"{module}{action}";
            if (existing.Contains(code)) continue;

            db.Set<UserApp.Domain.CommonTables.CommonTable>().Add(new()
            {
                Type = "FlashMessage",
                Code = code,
                Name = $"{module} {action.ToLower()} successfully"
            });
        }
    }

    await db.SaveChangesAsync();
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
    pattern: "{controller=Auth}/{action=Login}"
);

app.Run();

