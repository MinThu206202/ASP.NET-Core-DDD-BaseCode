using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using UserApp.Application.Notifications.DTOs;
using UserApp.Application.Notifications.Interfaces;
using UserApp.Application.Users.DTOs;
using UserApp.Application.Users.Interfaces;
using UserApp.Web.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using UserApp.Domain.Notifications;
using UserApp.Domain.Roles;
using UserApp.Domain.Users;
using UserApp.Domain.Common;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using UserApp.Application.Common.Interfaces;

namespace UserApp.Web.Controllers;

[AllowAnonymous]
[EnableRateLimiting("AuthPolicy")]
public class AuthController : Controller
{
    private const int MaxOtpAttempts = 5;
    private static readonly TimeSpan OtpTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan BlockTtl = TimeSpan.FromHours(1);

    private readonly IAuthService _authService;
    private readonly IBaseRepository<UserRole> _userRoleBaseRepository;
    private readonly IBaseRepository<Role> _roleBaseRepository;
    private readonly IDistributedCache _cache;
    private readonly IEmailService _emailService;
    private readonly IWebHostEnvironment _env;
    private readonly INotificationService _notificationService;
    private readonly IUserRepository _userRepository;
    private readonly IEmailTaskQueue _emailTaskQueue;

    private static string OtpCodeKey(string email) => $"otp:{email.ToLower()}";
    private static string OtpAttemptsKey(string email) => $"otp_attempts:{email.ToLower()}";
    private static string OtpBlockedKey(string email) => $"otp_blocked:{email.ToLower()}";

    public AuthController(
        IAuthService authService,
        IBaseRepository<UserRole> userRoleBaseRepository,
        IBaseRepository<Role> roleBaseRepository,
        IDistributedCache cache,
        IEmailService emailService,
        IWebHostEnvironment env,
        INotificationService notificationService,
        IUserRepository userRepository,
        IEmailTaskQueue emailTaskQueue)
    {
        _authService = authService;
        _userRoleBaseRepository = userRoleBaseRepository;
        _roleBaseRepository = roleBaseRepository;
        _cache = cache;
        _emailService = emailService;
        _env = env;
        _notificationService = notificationService;
        _userRepository = userRepository;
        _emailTaskQueue = emailTaskQueue;
    }

    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        // 1. Authenticate via application layer
        var authResult = await _authService.LoginAsync(new LoginDto(vm.Email, vm.Password));

        if (authResult == null)
        {
            ModelState.AddModelError("", "Invalid email or password.");
            return View(vm);
        }

        // 2. Fetch the full domain entity to access the Guid Id
        var userEntity = await _authService.GetUserByEmailAsync(authResult.Email);
        if (userEntity == null)
        {
            ModelState.AddModelError("", "User profile synchronization failed.");
            return View(vm);
        }

        // 3. Build basic identity claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userEntity.Id.ToString()),
            new Claim(ClaimTypes.Name, authResult.FullName),
            new Claim(ClaimTypes.Email, authResult.Email)
        };

        // 4. Use generic base repository to get user roles securely
        var allUserRoles = await _userRoleBaseRepository.ListAsync(0, 1000);
        var assignedRoleIds = allUserRoles
            .Where(ur => ur.UserId == userEntity.Id)
            .Select(ur => ur.RoleId)
            .ToList();

        // 5. Fetch Role names based on the assigned IDs
        foreach (var roleId in assignedRoleIds)
        {
            var role = await _roleBaseRepository.GetByIdAsync(roleId);
            if (role != null)
            {
                claims.Add(new Claim(ClaimTypes.Role, role.Name));
            }
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        // 6. Issue the browser cookie
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        await _notificationService.SendAsync(new CreateNotificationRequest
        {
            RecipientId = userEntity.Id,
            Title = "New Login Detected",
            Message = $"A new login was detected on your account at {DateTime.Now:HH:mm}.",
            Type = NotificationType.LoginDetected,
            Priority = NotificationPriority.Low
        });

        return RedirectToAction("Index", "Users");
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        try
        {
            await _authService.RegisterAsync(new RegisterDto(vm.Email, vm.FullName, vm.Password));

            var registeredUser = await _authService.GetUserByEmailAsync(vm.Email);
            if (registeredUser != null)
            {
                await _notificationService.SendAsync(new CreateNotificationRequest
                {
                    RecipientId = registeredUser.Id,
                    Title = "Welcome to UserApp",
                    Message = $"Your account has been created successfully. Welcome, {vm.FullName}!",
                    Type = NotificationType.UserCreated,
                    ActionUrl = "/Auth/Login"
                });
            }

            var loginUrl = Url.Action("Login", "Auth", null, Request.Scheme) ?? "/Auth/Login";

            // Enqueue welcome email (background, non-blocking)
            var capturedEmail = vm.Email;
            var capturedName = vm.FullName;
            var capturedLoginUrl = loginUrl;
            await _emailTaskQueue.EnqueueAsync(async (sp, ct) =>
            {
                var emailService = sp.GetRequiredService<IEmailService>();
                await emailService.SendTemplateAsync(
                    capturedEmail,
                    "Welcome to UserApp",
                    "RegistrationWelcome.md",
                    new Dictionary<string, string>
                    {
                        ["FULLNAME"] = capturedName,
                        ["LOGIN_URL"] = capturedLoginUrl,
                        ["YEAR"] = DateTime.UtcNow.Year.ToString()
                    });
            });

            await NotifyAdminsOnRegisterAsync(vm.Email, vm.FullName);

            return RedirectToAction("Login", "Auth");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(vm);
        }
    }

    [HttpGet]
    public IActionResult ForgotPassword() => View();

    [HttpPost]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var user = await _authService.GetUserByEmailAsync(vm.Email);
        if (user == null)
        {
            ModelState.AddModelError("", "If this email is registered, you will receive an OTP.");
            return View(vm);
        }

        var email = vm.Email.ToLower();
        var otp = Random.Shared.Next(100000, 999999).ToString();

        await Task.WhenAll(
            _cache.SetStringAsync(OtpCodeKey(email), otp, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = OtpTtl }),
            _cache.SetStringAsync(OtpAttemptsKey(email), "0", new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = OtpTtl }),
            _cache.RemoveAsync(OtpBlockedKey(email))
        );

        try
        {
            await _emailService.SendTemplateAsync(
                vm.Email,
                "Your Password Reset OTP",
                "ForgotPasswordOtp.md",
                new Dictionary<string, string>
                {
                    ["OTP"] = otp,
                    ["YEAR"] = DateTime.UtcNow.Year.ToString()
                });

        }
        catch (Exception ex)
        {
            if (_env.IsDevelopment())
                TempData["DevOtp"] = otp;
        }
        return RedirectToAction("VerifyOtp", new { email, sent = true });
    }

    [HttpGet]
    public IActionResult VerifyOtp(string email, bool sent = false)
    {
        if (string.IsNullOrWhiteSpace(email))
            return RedirectToAction("ForgotPassword");

        if (sent)
        {
            var devOtp = TempData["DevOtp"] as string;
            TempData["Success"] = devOtp != null
                ? $"DEV MODE: OTP is {devOtp} (email sending failed)"
                : "OTP sent to your email.";
        }

        return View(new VerifyOtpViewModel { Email = email });
    }

    [HttpPost]
    public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var email = vm.Email.ToLower();

        // 1. Check if blocked
        var blocked = await _cache.GetStringAsync(OtpBlockedKey(email));
        if (blocked != null)
        {
            ModelState.AddModelError("", "Too many attempts. Please try again later.");
            return View(vm);
        }

        // 2. Verify OTP
        var storedOtp = await _cache.GetStringAsync(OtpCodeKey(email));

        if (string.IsNullOrEmpty(storedOtp))
        {
            ModelState.AddModelError("", "This code has expired. Please request a new one.");
            return View(vm);
        }

        if (storedOtp != vm.Otp)
        {
            // 3. Track failed attempt
            var attemptsStr = await _cache.GetStringAsync(OtpAttemptsKey(email));
            var attempts = string.IsNullOrEmpty(attemptsStr) ? 1 : int.Parse(attemptsStr) + 1;

            if (attempts >= MaxOtpAttempts)
            {
                await Task.WhenAll(
                    _cache.SetStringAsync(OtpBlockedKey(email), "1", new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = BlockTtl }),
                    _cache.RemoveAsync(OtpCodeKey(email)),
                    _cache.RemoveAsync(OtpAttemptsKey(email))
                );
                ModelState.AddModelError("", "Too many attempts. Please try again later.");
            }
            else
            {
                await _cache.SetStringAsync(OtpAttemptsKey(email), attempts.ToString(), new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = OtpTtl });
                ModelState.AddModelError("", "Wrong code. Please try again.");
            }

            return View(vm);
        }

        // 4. Success — clean up and issue reset token
        var resetToken = Guid.NewGuid().ToString("N");

        await Task.WhenAll(
            _cache.RemoveAsync(OtpCodeKey(email)),
            _cache.RemoveAsync(OtpAttemptsKey(email)),
            _cache.SetStringAsync($"reset_token:{resetToken}", email, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = OtpTtl })
        );

        return RedirectToAction("ChangePassword", new { token = resetToken });
    }

    [HttpGet]
    public IActionResult ChangePassword(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("ForgotPassword");

        return View(new ChangePasswordViewModel { Token = token });
    }

    [HttpPost]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var tokenKey = $"reset_token:{vm.Token}";
        var email = await _cache.GetStringAsync(tokenKey);

        if (string.IsNullOrEmpty(email))
        {
            ModelState.AddModelError("", "Invalid or expired reset link.");
            return View(vm);
        }

        try
        {
            await _authService.UpdatePasswordAsync(email, vm.NewPassword);

            var user = await _authService.GetUserByEmailAsync(email);
            if (user != null)
            {
                await _notificationService.SendAsync(new CreateNotificationRequest
                {
                    RecipientId = user.Id,
                    Title = "Password Changed",
                    Message = "Your password has been changed successfully.",
                    Type = NotificationType.PasswordChanged,
                    Priority = NotificationPriority.High
                });
            }
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(vm);
        }

        await _cache.RemoveAsync(tokenKey);

        TempData["Success"] = "Password has been reset. Please sign in.";
        return RedirectToAction("Login");
    }

    [HttpPost]
    public async Task<IActionResult> ResendOtp(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return RedirectToAction("ForgotPassword");

        var user = await _authService.GetUserByEmailAsync(email);
        if (user == null)
            return RedirectToAction("ForgotPassword");

        var normalizedEmail = email.ToLower();
        var otp = Random.Shared.Next(100000, 999999).ToString();

        await Task.WhenAll(
            _cache.SetStringAsync(OtpCodeKey(normalizedEmail), otp, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = OtpTtl }),
            _cache.SetStringAsync(OtpAttemptsKey(normalizedEmail), "0", new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = OtpTtl }),
            _cache.RemoveAsync(OtpBlockedKey(normalizedEmail))
        );

        try
        {
            await _emailService.SendTemplateAsync(
                email,
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
            if (_env.IsDevelopment())
                TempData["DevOtp"] = otp;
        }

        TempData["Success"] = "OTP code is resend to your mail";
        return RedirectToAction("VerifyOtp", new { email = normalizedEmail, sent = true });
    }

    [HttpPost]
    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    private async Task NotifyAdminsOnRegisterAsync(string email, string fullName)
    {
        try
        {
            var roles = await _roleBaseRepository.ListAsync(0, 10000);
            var adminRole = roles.FirstOrDefault(r => r.Name == "Admin");
            if (adminRole == null) return;

            var allUserRoles = await _userRoleBaseRepository.ListAsync(0, 10000);
            var adminUserIds = allUserRoles
                .Where(ur => ur.RoleId == adminRole.Id)
                .Select(ur => ur.UserId)
                .ToHashSet();

            if (adminUserIds.Count == 0) return;

            var allUsers = await _userRepository.ListAsync(0, 10000);
            var admins = allUsers.Where(u => adminUserIds.Contains(u.Id)).ToList();

            foreach (var admin in admins)
            {
                await _notificationService.SendAsync(new CreateNotificationRequest
                {
                    RecipientId = admin.Id,
                    Title = "New User Registered",
                    Message = $"A new user has registered: {fullName} ({email})",
                    Type = NotificationType.SystemAnnouncement,
                    ActionUrl = "/Users"
                });
            }

            // Enqueue admin notification emails (background, non-blocking)
            var adminEmails = admins.Select(a => a.Email.ToString()).ToList();
            var capturedEmail = email;
            var capturedName = fullName;
            await _emailTaskQueue.EnqueueAsync(async (sp, ct) =>
            {
                var emailService = sp.GetRequiredService<IEmailService>();
                foreach (var adminEmail in adminEmails)
                {
                    try
                    {
                        await emailService.SendAsync(
                            adminEmail,
                            "New User Registration",
                            $"<p>A new user has registered on the system:</p>" +
                            $"<ul><li><strong>Name:</strong> {capturedName}</li>" +
                            $"<li><strong>Email:</strong> {capturedEmail}</li></ul>");
                    }
                    catch
                    {
                        // Per-admin email failure does not block others
                    }
                }
            });
        }
        catch
        {
            // Failure to notify admins does not block registration
        }
    }

    [HttpGet]
    public IActionResult Denied()
    {
        return View();
    }
}