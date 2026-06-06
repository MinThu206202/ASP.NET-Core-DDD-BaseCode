using Microsoft.EntityFrameworkCore;
using UserApp.Infrastructure;
using UserApp.Infrastructure.Persistence;
using UserApp.Web.Mapping;
using UserApp.Infrastructure.Persistence.Repositories;
using UserApp.Domain.Common;
using UserApp.Domain.Users;
using UserApp.Domain.Products;
using UserApp.Application.Products;
using UserApp.Application.Common;
using UserApp.Domain.Users;
using UserApp.Application.Products.Interfaces;
using UserApp.Application.Users.Interfaces;
using UserApp.Application.Users;



var builder = WebApplication.CreateBuilder(args);

// Add MVC
builder.Services.AddControllersWithViews();

// Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);

// AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// ----------------------
// REPOSITORIES
// ----------------------
builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();

// ----------------------
// SERVICES
// ----------------------
builder.Services.AddScoped(typeof(IBaseService<>), typeof(BaseService<>));

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProductService, ProductService>();

var app = builder.Build();

// Apply database migrations on startup (for dev/testing)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Configure middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Users}/{action=Index}/{id?}");

app.Run();