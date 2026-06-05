using Microsoft.AspNetCore.Mvc;
using UserApp.Application.Users;
using UserApp.Web.ViewModels;

namespace UserApp.Web.Controllers;

public class UsersController : BaseController
{
    private readonly UserService _userService;
    private const int PageSize = 10;

    public UsersController(UserService userService)
    {
        _userService = userService;
    }

    public async Task<IActionResult> Index(int page = 1)
    {
        var (items, total) = await _userService.ListAsync(page, PageSize);

        var model = new UserListViewModel
        {
            Page = page,
            PageSize = PageSize,
            TotalCount = total,
            Users = items.Select(u => new UserViewModel
            {
                Id = u.Id,
                Email = u.Email,
                FullName = u.FullName,
                Status = u.Status,
                CreatedAt = u.CreatedAt
            }).ToList()
        };

        return View(model);
    }

    public IActionResult Create() => View(new CreateUserViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel input)
    {
        if (!ModelState.IsValid)
            return View(input);

        try
        {
            await _userService.CreateAsync(input.Email!, input.FullName!, input.Password!);
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(input);
        }
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var user = await _userService.GetAsync(id);
        if (user is null)
            return NotFound();

        var model = new EditUserViewModel
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditUserViewModel input)
    {
        if (!ModelState.IsValid)
            return View(input);

        try
        {
            await _userService.UpdateAsync(input.Id, input.FullName!, input.Email!);
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(input);
        }
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await _userService.GetAsync(id);
        if (user is null)
            return NotFound();

        var model = new DeleteUserViewModel
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        try
        {
            await _userService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            return RenderError(ex.Message);
        }
    }
}
