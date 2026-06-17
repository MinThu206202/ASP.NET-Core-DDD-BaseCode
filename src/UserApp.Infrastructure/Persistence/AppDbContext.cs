using Microsoft.EntityFrameworkCore;
using UserApp.Domain.Users;
using UserApp.Infrastructure.Persistence.Configurations;
using MediaEntity = UserApp.Domain.Media.MediaFile;
using UserApp.Domain.Paps;
using UserApp.Domain.Milks;
using UserApp.Domain.Ais;
using UserApp.Domain.Cocos;
using UserApp.Domain.Roles;
using UserApp.Domain.Categorys;
using UserApp.Domain.Payments;
using UserApp.Domain.CommonTables;
using UserApp.Domain.Humans;
using UserApp.Domain.Messengers;

using UserApp.Domain.Cars;
using UserApp.Domain.Notifications;
using UserApp.Domain.Products;

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
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    // ================= AUTO DBSets =================
    // <AUTO-DBSETS-START>
    public DbSet<Pap> Paps => Set<Pap>();
    public DbSet<Milk> Milks => Set<Milk>();
    public DbSet<Ai> Ais => Set<Ai>();
    public DbSet<Coco> Cocos => Set<Coco>();
public DbSet<Category> Categorys => Set<Category>();
public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<CommonTable> CommonTables => Set<CommonTable>();
public DbSet<Human> Humans => Set<Human>();
public DbSet<Messenger> Messengers => Set<Messenger>();
public DbSet<Car> Cars => Set<Car>();
public DbSet<Notification> Notifications => Set<Notification>();
public DbSet<Product> Products => Set<Product>();
    // <AUTO-DBSETS-END>



    // NEW MODULE (Payment)
    // ==================== MODEL CONFIG ====================
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // <AUTO-CONFIG-START>
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
        // <AUTO-CONFIG-END>

        modelBuilder.ApplyConfiguration(new UserConfiguration());

        modelBuilder.Entity<UserRole>()
            .HasKey(x => new { x.UserId, x.RoleId });

        modelBuilder.Entity<UserRole>()
            .HasOne(x => x.User)
            .WithMany(x => x.UserRoles)
            .HasForeignKey(x => x.UserId);

        modelBuilder.Entity<UserRole>()
            .HasOne(x => x.Role)
            .WithMany(x => x.UserRoles)
            .HasForeignKey(x => x.RoleId);

        modelBuilder.Entity<RolePermission>()
    .HasKey(x => new { x.RoleId, x.PermissionId });

        modelBuilder.Entity<RolePermission>()
            .HasOne(x => x.Role)
            .WithMany(x => x.RolePermissions)
            .HasForeignKey(x => x.RoleId);

        modelBuilder.Entity<RolePermission>()
            .HasOne(x => x.Permission)
            .WithMany(x => x.RolePermissions)
            .HasForeignKey(x => x.PermissionId);
    }
}
