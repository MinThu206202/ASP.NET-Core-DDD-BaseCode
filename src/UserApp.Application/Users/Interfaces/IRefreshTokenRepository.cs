using UserApp.Domain.Users;

namespace UserApp.Application.Users.Interfaces;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token);
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task SaveAsync();
}