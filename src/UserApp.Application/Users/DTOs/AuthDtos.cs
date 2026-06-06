namespace UserApp.Application.Users.DTOs;

public record RegisterDto(string Email, string FullName, string Password);
public record LoginDto(string Email, string Password);

public record AuthResponseDto(
    string AccessToken,
    string Email,
    string FullName,
    string? RefreshToken = null
);