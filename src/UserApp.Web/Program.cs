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


// ================= AUTO MODULE IMPORTS =================
// <AUTO-USINGS-START>
// <AUTO-USINGS-END>


var builder = WebApplication.CreateBuilder(args);

// ------------------------------------------------
// MVC (Controllers + Views + API)
// ------------------------------------------------
builder.Services.AddControllersWithViews();

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
// <AUTO-REPOSITORIES-END>

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IMediaRepository, MediaRepository>();


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
// <AUTO-SERVICES-END>

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<
    UserApp.Application.Common.Interfaces.IModuleGeneratorService,
    UserApp.Infrastructure.Services.ModuleGeneratorService>();
builder.Services.AddScoped<MediaStorage>();
builder.Services.AddScoped<IMediaPipeline, MediaPipeline>();


// ------------------------------------------------
// JWT AUTHENTICATION (IMPORTANT FIX)
// ------------------------------------------------
var jwtKey = builder.Configuration["Jwt:Key"] ?? "THIS_IS_DEMO_SECRET_KEY_123456";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
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
// MVC (Controllers + Views + API)
// ------------------------------------------------
builder.Services.AddControllersWithViews();

// ... (rest of your service registrations)

var app = builder.Build();

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
    pattern: "{controller=Users}/{action=Index}/{id?}"
);

app.Run();

