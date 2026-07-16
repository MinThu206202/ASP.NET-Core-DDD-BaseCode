using System.Linq.Expressions;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using UserApp.Domain.Users;
using UserApp.Infrastructure.Persistence.Configurations;
using UserApp.Infrastructure.Notifications.Configurations;
using MediaEntity = UserApp.Domain.Media.MediaFile;
using UserApp.Domain.Roles;
using UserApp.Domain.CommonTables;
using UserApp.Domain.AuditLogs;
using UserApp.Application.Common;
using UserApp.Domain.Common;
using UserApp.Domain.Notifications;





namespace UserApp.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var auditEntries = new List<AuditLog>();

        var userName = "System";
        var httpContext = (ServiceProviderAccessor.Current?.GetService(typeof(IHttpContextAccessor)) as IHttpContextAccessor)?.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            userName = httpContext.User.FindFirstValue(ClaimTypes.Name)
                       ?? httpContext.User.Identity.Name
                       ?? "System";
        }

        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is not AuditLog
                && e.Entity is not AuditLogArchive
                && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        var pageName = httpContext?.Items["AuditPageName"]?.ToString() ?? string.Empty;
        var functionName = httpContext?.Items["AuditFunctionName"]?.ToString() ?? string.Empty;

        foreach (var entry in entries)
        {
            var entityType = entry.Entity.GetType();
            var isSoftDelete = entry.State == EntityState.Modified
                && entry.Properties.Any(p => p.Metadata.Name == "DeletedAt" && p.OriginalValue == null && p.CurrentValue != null);

            var action = entry.State switch
            {
                EntityState.Added => "Create",
                EntityState.Deleted => "Delete",
                _ when isSoftDelete => "Delete",
                _ => "Update"
            };

            var pk = entry.Metadata.FindPrimaryKey();
            var entityId = pk != null
                ? string.Join(",", pk.Properties.Select(p => entry.Property(p.Name).CurrentValue?.ToString()))
                : string.Empty;

            var audit = new AuditLog
            {
                UserName = userName,
                Action = action,
                EntityName = entityType.Name,
                EntityId = entityId,
                PageName = pageName,
                FunctionName = functionName
            };

            if (entry.State == EntityState.Modified && !isSoftDelete)
            {
                var affectedCols = new List<string>();
                var oldVals = new Dictionary<string, object?>();
                var newVals = new Dictionary<string, object?>();
                var skipProps = new[] { "Id", "CreatedAt", "UpdatedAt", "DeletedAt" };

                foreach (var prop in entry.Properties)
                {
                    if (prop.IsModified && !skipProps.Contains(prop.Metadata.Name))
                    {
                        affectedCols.Add(prop.Metadata.Name);
                        oldVals[prop.Metadata.Name] = prop.OriginalValue;
                        newVals[prop.Metadata.Name] = prop.CurrentValue;
                    }
                }

                if (affectedCols.Count > 0)
                {
                    audit.AffectedColumns = JsonSerializer.Serialize(affectedCols);
                    audit.OldValues = JsonSerializer.Serialize(oldVals);
                    audit.NewValues = JsonSerializer.Serialize(newVals);
                }
            }
            else if (entry.State == EntityState.Added)
            {
                var newVals = new Dictionary<string, object?>();
                var skipProps = new[] { "Id", "CreatedAt", "UpdatedAt", "DeletedAt" };

                foreach (var prop in entry.Properties)
                {
                    if (!skipProps.Contains(prop.Metadata.Name))
                    {
                        newVals[prop.Metadata.Name] = prop.CurrentValue;
                    }
                }

                if (newVals.Count > 0)
                {
                    audit.AffectedColumns = JsonSerializer.Serialize(newVals.Keys.ToList());
                    audit.NewValues = JsonSerializer.Serialize(newVals);
                }
            }
            else if (entry.State == EntityState.Deleted || isSoftDelete)
            {
                var oldVals = new Dictionary<string, object?>();
                var skipProps = new[] { "Id", "CreatedAt", "UpdatedAt", "DeletedAt" };

                foreach (var prop in entry.Properties)
                {
                    if (!skipProps.Contains(prop.Metadata.Name))
                    {
                        oldVals[prop.Metadata.Name] = entry.State == EntityState.Deleted
                            ? prop.OriginalValue
                            : prop.OriginalValue;
                    }
                }

                if (oldVals.Count > 0)
                {
                    audit.AffectedColumns = JsonSerializer.Serialize(oldVals.Keys.ToList());
                    audit.OldValues = JsonSerializer.Serialize(oldVals);
                }
            }

            auditEntries.Add(audit);
        }

        if (auditEntries.Count > 0)
        {
            AuditLogs.AddRange(auditEntries);
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    // ==================== DBSets ====================
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<MediaEntity> Media => Set<MediaEntity>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    // ================= AUTO DBSets =================
    // <AUTO-DBSETS-START>
    public DbSet<CommonTable> CommonTables => Set<CommonTable>();

    // ==================== SYSTEM DBSets ====================
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AuditLogArchive> AuditLogsArchive => Set<AuditLogArchive>();



    // NEW MODULE (Payment)
    // ==================== MODEL CONFIG ====================
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // <AUTO-CONFIG-START>
        // <AUTO-CONFIG-END>

        modelBuilder.ApplyConfiguration(new AuditLogArchiveConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new NotificationConfiguration());

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

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(Entity<Guid>).IsAssignableFrom(entityType.ClrType))
            {
                var param = Expression.Parameter(entityType.ClrType, "e");
                var body = Expression.Equal(
                    Expression.Property(param, "DeletedAt"),
                    Expression.Constant(null, typeof(DateTime?)));
                var lambda = Expression.Lambda(body, param);
                entityType.SetQueryFilter(lambda);
            }
        }
    }
}
