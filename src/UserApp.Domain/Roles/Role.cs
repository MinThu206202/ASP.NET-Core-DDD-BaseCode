using UserApp.Domain.Common;
using UserApp.Domain.Users;

namespace UserApp.Domain.Roles;

public class Role : Entity<Guid>
{
    public string Name { get; private set; } = default!;

    private readonly List<UserRole> _userRoles = new();
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles;

    private readonly List<RolePermission> _rolePermissions = new();
    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions;

    private Role() { }

    public static Role Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Role name is required");

        return new Role
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            CreatedAt = TimeHelper.Now
        };
    }

    // Add this domain method for the Update CRUD operation
    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Role name cannot be empty");

        Name = newName.Trim();
        UpdatedAt = TimeHelper.Now;
    }
}