using UserApp.Domain.Common;

namespace UserApp.Domain.Users;

public class RefreshToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Token { get; set; } = default!;

    public DateTime CreatedAt { get; set; } = TimeHelper.Now;

    public DateTime ExpiresAt { get; set; }

    public bool IsRevoked { get; set; }

    public string? CreatedByIp { get; set; }
}