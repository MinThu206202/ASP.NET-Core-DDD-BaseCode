using UserApp.Application.Users.DTOs;
using UserApp.Application.Users.Interfaces;
using UserApp.Domain.Users;
using UserApp.Domain.Common;
using UserApp.Domain.Roles;


namespace UserApp.Application.Users;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IBaseRepository<User> _baseRepository;
    private readonly IPasswordHasher _hasher;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRoleRepository _userRoleRepository;

    public AuthService(
    IUserRepository userRepository,
    IBaseRepository<User> baseRepository,
    IPasswordHasher hasher,
    IRefreshTokenRepository refreshTokenRepository,
    IRoleRepository roleRepository,
    IUserRoleRepository userRoleRepository)
    {
        _userRepository = userRepository;
        _baseRepository = baseRepository;
        _hasher = hasher;
        _refreshTokenRepository = refreshTokenRepository;
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
    }

    // ---------------- REGISTER ----------------
    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        var email = Email.Create(dto.Email);

        var exists = await _userRepository.GetByEmailAsync(dto.Email);
        if (exists != null)
            throw new Exception("Email already exists");

        var user = User.Create(email, dto.FullName, _hasher.Hash(dto.Password));

        // 1. Save user first
        await _baseRepository.AddAsync(user);
        await _baseRepository.SaveChangesAsync();

        // 2. Get default "User" role
        var role = await _roleRepository.GetByNameAsync("User");
        if (role == null)
            throw new Exception("Default role 'User' not found");

        // 3. Assign role (pivot table)
        var userRole = new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id
        };

        await _userRoleRepository.AddAsync(userRole);
        await _userRoleRepository.SaveAsync();

        return new AuthResponseDto(
            AccessToken: "NO_LOGIN_TOKEN",
            Email: user.Email.Value,
            FullName: user.FullName
        );
    }

    // ---------------- LOGIN ----------------
    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email);
        if (user == null) return null;

        var isValid = _hasher.Verify(dto.Password, user.PasswordHash);
        if (!isValid) return null;

        var accessToken = "JWT_TOKEN_HERE"; // you will replace later

        var refreshToken = await CreateRefreshTokenAsync(user.Id);

        return new AuthResponseDto(
    AccessToken: accessToken,
    Email: user.Email.Value,
    FullName: user.FullName,
    RefreshToken: refreshToken.Token
);
    }

    // ---------------- CREATE REFRESH TOKEN ----------------
    public async Task<RefreshToken> CreateRefreshTokenAsync(Guid userId)
    {
        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = token,
            ExpiresAt = TimeHelper.Now.AddDays(7),
            IsRevoked = false,
            CreatedAt = TimeHelper.Now
        };

        await _refreshTokenRepository.AddAsync(refreshToken);
        await _refreshTokenRepository.SaveAsync();

        return refreshToken;
    }

    // ---------------- GET REFRESH TOKEN ----------------
    public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
    {
        return await _refreshTokenRepository.GetByTokenAsync(token);
    }

    // ---------------- REVOKE REFRESH TOKEN ----------------
    public async Task RevokeRefreshTokenAsync(string token)
    {
        var entity = await _refreshTokenRepository.GetByTokenAsync(token);

        if (entity != null)
        {
            entity.IsRevoked = true;
            await _refreshTokenRepository.SaveAsync();
        }
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _userRepository.GetByEmailAsync(email);
    }
}