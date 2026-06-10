using Microsoft.EntityFrameworkCore;
using UserApp.Domain.Users;
using UserApp.Infrastructure.Persistence.Configurations;
using MediaEntity = UserApp.Domain.Media.MediaFile;


namespace UserApp.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // ==================== DBSets ====================
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<MediaEntity> Media => Set<MediaEntity>();

    // ================= AUTO DBSets =================
    // <AUTO-DBSETS-START>
    // <AUTO-DBSETS-END>



    // NEW MODULE (Payment)
    // ==================== MODEL CONFIG ====================
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new UserConfiguration());

        // (Optional future configs)
        // modelBuilder.ApplyConfiguration(new ProductConfiguration());
        // modelBuilder.ApplyConfiguration(new PaymentConfiguration());
    }
}
