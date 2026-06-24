using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UserApp.Application.Users.DTOs;
using UserApp.Application.Users.Interfaces;
using UserApp.Application.Common;
using UserApp.Domain.Common;
using UserApp.Domain.Roles;
using UserApp.Domain.Users;

namespace UserApp.Web.Controllers.Api;

[Route("api/auth")]
[ApiController]
public class AuthApiController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IAuthService _authService;
    private readonly IConfiguration _config;
    private readonly IBaseRepository<UserRole> _userRoleRepo;
    private readonly IBaseRepository<Role> _roleRepo;

    public AuthApiController(
        IUserService userService,
        IAuthService authService,
        IConfiguration config,
        IBaseRepository<UserRole> userRoleRepo,
        IBaseRepository<Role> roleRepo)
    {
        _userService = userService;
        _authService = authService;
        _config = config;
        _userRoleRepo = userRoleRepo;
        _roleRepo = roleRepo;
    }

    // ---------------- LOGIN ----------------
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        try
        {
            var user = await _userService.ValidateUserAsync(
            req.Email,
            req.Password
            );
            if (user == null)
            {
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid credentials.",
                    Data = new { field = "credentials" }
                });
            }

            var accessToken = await GenerateToken(user);

            var refreshToken =
                await _authService.CreateRefreshTokenAsync(user.Id);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Login successful",
                Data = new
                {
                    access_token = accessToken,
                    refresh_token = refreshToken.Token,
                    user = new
                    {
                        id = user.Id,
                        email = user.Email.Value,
                        fullName = user.FullName
                    }
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message,
                Data = null
            });
        }

    }



    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        try
        {
            var result = await _authService.RegisterAsync(
                new RegisterDto(req.Email, req.FullName, req.Password)
            );

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "User registered successfully",
                Data = result
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }


    // ---------------- REFRESH TOKEN ----------------
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] string refreshToken)
    {
        var storedToken = await _authService.GetRefreshTokenAsync(refreshToken);

        if (storedToken == null ||
            storedToken.IsRevoked ||
            storedToken.ExpiresAt < TimeHelper.Now)
        {
            return Unauthorized(ApiResponse<object>.Fail("Invalid refresh token"));
        }

        var user = await _userService.GetByIdAsync(storedToken.UserId);

        var newAccessToken = await GenerateToken(user);

        return Ok(ApiResponse<object>.Ok(new
        {
            access_token = newAccessToken
        }, "Token refreshed"));
    }

    // ---------------- LOGOUT ----------------
    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout([FromBody] string refreshToken)
    {
        await _authService.RevokeRefreshTokenAsync(refreshToken);

        return Ok(ApiResponse<object>.Ok(null, "Logged out"));
    }

    // ---------------- JWT GENERATION ----------------
    private async Task<string> GenerateToken(User user)
    {
        var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]!);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email.Value ?? user.Email.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var allUserRoles = await _userRoleRepo.ListAsync(0, 1000);
        var assignedRoleIds = allUserRoles
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => ur.RoleId)
            .ToList();

        foreach (var roleId in assignedRoleIds)
        {
            var role = await _roleRepo.GetByIdAsync(roleId);
            if (role != null)
            {
                claims.Add(new(ClaimTypes.Role, role.Name));
            }
        }

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: TimeHelper.Now.AddHours(1),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256
            )
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public record LoginRequest(string Email, string Password);

public record RegisterRequest(string Email, string FullName, string Password);