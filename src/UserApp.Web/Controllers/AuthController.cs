using Microsoft.AspNetCore.Mvc;
using UserApp.Application.Users.DTOs;
using UserApp.Application.Users.Interfaces;
using UserApp.Web.ViewModels;

namespace UserApp.Web.Controllers;

public class AuthController : Controller
{
    private readonly IAuthService _service;

    public AuthController(IAuthService service)
    {
        _service = service;
    }

    public IActionResult Login() => View();

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        var result = await _service.LoginAsync(new LoginDto(vm.Email, vm.Password));

        if (result == null)
        {
            ModelState.AddModelError("", "Invalid login");
            return View(vm);
        }

        return RedirectToAction("Index", "Users");
    }

    public IActionResult Register() => View();

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel vm)
    {
        await _service.RegisterAsync(new RegisterDto(vm.Email, vm.FullName, vm.Password));
        return RedirectToAction("Login");
    }
}