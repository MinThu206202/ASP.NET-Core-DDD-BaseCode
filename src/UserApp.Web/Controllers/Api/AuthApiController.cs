using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UserApp.Application.Users.DTOs;
using UserApp.Application.Users.Interfaces;
using UserApp.Application.Common;
using UserApp.Application.Common.Interfaces;
using UserApp.Domain.Common;
using UserApp.Domain.Roles;
using UserApp.Domain.Users;
using Microsoft.Extensions.Caching.Distributed;

namespace UserApp.Web.Controllers.Api;

[Route("api/auth")]
[ApiController]
[EnableRateLimiting("AuthPolicy")]
public class AuthApiController : ControllerBase
{
    private const int MaxOtpAttempts = 5;
    private static readonly TimeSpan OtpTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan BlockTtl = TimeSpan.FromHours(1);

    private readonly IUserService _userService;
    private readonly IAuthService _authService;
    private readonly IConfiguration _config;
    private readonly IBaseRepository<UserRole> _userRoleRepo;
    private readonly IBaseRepository<Role> _roleRepo;
    private readonly IDistributedCache _cache;
    private readonly IEmailService _emailService;

    private static string OtpCodeKey(string email) => $"otp:{email.ToLower()}";
    private static string OtpAttemptsKey(string email) => $"otp_attempts:{email.ToLower()}";
    private static string OtpBlockedKey(string email) => $"otp_blocked:{email.ToLower()}";

    public AuthApiController(
        IUserService userService,
        IAuthService authService,
        IConfiguration config,
        IBaseRepository<UserRole> userRoleRepo,
        IBaseRepository<Role> roleRepo,
        IDistributedCache cache,
        IEmailService emailService)
    {
        _userService = userService;
        _authService = authService;
        _config = config;
        _userRoleRepo = userRoleRepo;
        _roleRepo = roleRepo;
        _cache = cache;
        _emailService = emailService;
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

            try
            {
                var loginUrl = $"{Request.Scheme}://{Request.Host}/Auth/Login";
                await _emailService.SendTemplateAsync(
                    req.Email,
                    "Welcome to UserApp",
                    "RegistrationWelcome.md",
                    new Dictionary<string, string>
                    {
                        ["FULLNAME"] = req.FullName,
                        ["LOGIN_URL"] = loginUrl,
                        ["YEAR"] = DateTime.UtcNow.Year.ToString()
                    });
            }
            catch
            {
                // Email failure does not block registration
            }

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


    // ---------------- FORGOT PASSWORD ----------------
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest req)
    {
        var user = await _authService.GetUserByEmailAsync(req.Email);
        if (user == null)
        {
            return Ok(ApiResponse<object>.Ok(null, "If this email is registered, an OTP will be sent."));
        }

        var email = req.Email.ToLower();
        var otp = Random.Shared.Next(100000, 999999).ToString();

        await Task.WhenAll(
            _cache.SetStringAsync(OtpCodeKey(email), otp, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = OtpTtl }),
            _cache.SetStringAsync(OtpAttemptsKey(email), "0", new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = OtpTtl }),
            _cache.RemoveAsync(OtpBlockedKey(email))
        );

        try
        {
            await _emailService.SendTemplateAsync(
                req.Email,
                "Your Password Reset OTP",
                "ForgotPasswordOtp.md",
                new Dictionary<string, string>
                {
                    ["OTP"] = otp,
                    ["YEAR"] = DateTime.UtcNow.Year.ToString()
                });
        }
        catch
        {
            // In development, OTP is visible in the response
        }

        return Ok(ApiResponse<object>.Ok(new { email }, "OTP sent to your email."));
    }

    // ---------------- VERIFY OTP ----------------
    [HttpPost("verify-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyOtp(VerifyOtpRequest req)
    {
        var email = req.Email.ToLower();

        var blocked = await _cache.GetStringAsync(OtpBlockedKey(email));
        if (blocked != null)
        {
            return BadRequest(ApiResponse<object>.Fail("Too many attempts. Please try again later."));
        }

        var storedOtp = await _cache.GetStringAsync(OtpCodeKey(email));

        if (string.IsNullOrEmpty(storedOtp))
        {
            return BadRequest(ApiResponse<object>.Fail("This code has expired. Please request a new one."));
        }

        if (storedOtp != req.Otp)
        {
            var attemptsStr = await _cache.GetStringAsync(OtpAttemptsKey(email));
            var attempts = string.IsNullOrEmpty(attemptsStr) ? 1 : int.Parse(attemptsStr) + 1;

            if (attempts >= MaxOtpAttempts)
            {
                await Task.WhenAll(
                    _cache.SetStringAsync(OtpBlockedKey(email), "1", new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = BlockTtl }),
                    _cache.RemoveAsync(OtpCodeKey(email)),
                    _cache.RemoveAsync(OtpAttemptsKey(email))
                );
                return BadRequest(ApiResponse<object>.Fail("Too many attempts. Please try again later."));
            }

            await _cache.SetStringAsync(OtpAttemptsKey(email), attempts.ToString(), new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = OtpTtl });
            return BadRequest(ApiResponse<object>.Fail("Wrong code. Please try again."));
        }

        var resetToken = Guid.NewGuid().ToString("N");

        await Task.WhenAll(
            _cache.RemoveAsync(OtpCodeKey(email)),
            _cache.RemoveAsync(OtpAttemptsKey(email)),
            _cache.SetStringAsync($"reset_token:{resetToken}", email, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = OtpTtl })
        );

        return Ok(ApiResponse<object>.Ok(new { reset_token = resetToken }, "OTP verified."));
    }

    // ---------------- CHANGE PASSWORD ----------------
    [HttpPost("change-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest req)
    {
        var tokenKey = $"reset_token:{req.ResetToken}";
        var email = await _cache.GetStringAsync(tokenKey);

        if (string.IsNullOrEmpty(email))
        {
            return BadRequest(ApiResponse<object>.Fail("Invalid or expired reset token."));
        }

        try
        {
            await _authService.UpdatePasswordAsync(email, req.NewPassword);
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }

        await _cache.RemoveAsync(tokenKey);

        return Ok(ApiResponse<object>.Ok(null, "Password has been reset successfully."));
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

public record ForgotPasswordRequest(string Email);

public record VerifyOtpRequest(string Email, string Otp);

public record ChangePasswordRequest(string ResetToken, string NewPassword);