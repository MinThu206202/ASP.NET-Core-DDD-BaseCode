using Microsoft.EntityFrameworkCore;
using UserApp.Domain.Users;
using UserApp.Domain.Products;
using UserApp.Infrastructure.Persistence.Configurations;
using UserApp.Domain.Payments;

namespace UserApp.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // ==================== DBSets ====================

    public DbSet<Product> Products => Set<Product>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // ==================== AUTO GENERATED DBSets ====================
    // <AUTO-DBSETS-START>
    public DbSet<Payment> Payments => Set<Payment>();

    // <AUTO-DBSETS-END>



    // NEW MODULE (Payment)
    // ==================== MODEL CONFIG ====================
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new UserConfiguration());

        // Product config
        modelBuilder.Entity<Product>()
            .Property(p => p.Price)
            .HasPrecision(18, 2);

        // (Optional future configs)
        // modelBuilder.ApplyConfiguration(new ProductConfiguration());
        // modelBuilder.ApplyConfiguration(new PaymentConfiguration());
    }
}
