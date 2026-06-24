using UserApp.Domain.Common;
using UserApp.Domain.Roles;

namespace UserApp.Domain.Users;

public class User : Entity<Guid>, IAggregateRoot
{
    public Email Email { get; private set; } = default!;
    public string FullName { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public UserStatus Status { get; private set; }
    private readonly List<UserRole> _userRoles = new();
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles;
    private User() { }

    public static User Create(Email email, string fullName, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name is required", nameof(fullName));
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required", nameof(passwordHash));

        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FullName = fullName.Trim(),
            PasswordHash = passwordHash,
            Status = UserStatus.Active,
            CreatedAt = TimeHelper.Now
        };
    }

    public void UpdateProfile(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name is required", nameof(fullName));
        FullName = fullName.Trim();
        UpdatedAt = TimeHelper.Now;
    }

    public void ChangeEmail(Email email)
    {
        Email = email;
        UpdatedAt = TimeHelper.Now;
    }

    public void Deactivate()
    {
        Status = UserStatus.Inactive;
        UpdatedAt = TimeHelper.Now;
    }

    public void Activate()
    {
        Status = UserStatus.Active;
        UpdatedAt = TimeHelper.Now;
    }
}
