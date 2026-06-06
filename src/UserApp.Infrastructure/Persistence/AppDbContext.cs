using Microsoft.EntityFrameworkCore;
using UserApp.Domain.Users;
using UserApp.Domain.Products;
using UserApp.Infrastructure.Persistence.Configurations;

namespace UserApp.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>()
        .Property(p => p.Price)
        .HasPrecision(18, 2);
    }
}
