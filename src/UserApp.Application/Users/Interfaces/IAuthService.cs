using UserApp.Application.Users.DTOs;
using UserApp.Domain.Users;


namespace UserApp.Application.Users.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto?> LoginAsync(LoginDto dto);

    Task<RefreshToken> CreateRefreshTokenAsync(Guid userId);
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task RevokeRefreshTokenAsync(string token);
}