using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using UserApp.Application.Users.Interfaces;
using UserApp.Domain.Common;
using UserApp.Domain.Roles;
using UserApp.Domain.Users;
using UserApp.Web.ViewModels;

namespace UserApp.Web.Controllers;

public class UsersController : BaseController<User, UserViewModel>
{
    private readonly IBaseRepository<Role> _roleRepo;
    private readonly IBaseRepository<UserRole> _userRoleRepo;

    public UsersController(
        IUserService service,
        IMapper mapper,
        IBaseRepository<Role> roleRepo,
        IBaseRepository<UserRole> userRoleRepo)
        : base(service, mapper)
    {
        _roleRepo = roleRepo;
        _userRoleRepo = userRoleRepo;
    }

    public override async Task<IActionResult> Index(int page = 1, int size = 10)
    {
        var users = await _service.ListAsync((page - 1) * size, size);
        var totalCount = await _service.CountAsync();

        var allUserRoles = await _userRoleRepo.ListAsync(0, 10000);
        var allRoles = await _roleRepo.ListAsync(0, 10000);
        var roleLookup = allRoles.ToDictionary(r => r.Id, r => r.Name);

        var userIds = users.Select(u => u.Id).ToHashSet();
        var userRolesMap = allUserRoles
            .Where(ur => userIds.Contains(ur.UserId))
            .GroupBy(ur => ur.UserId)
            .ToDictionary(g => g.Key, g => g.Select(ur => roleLookup.GetValueOrDefault(ur.RoleId, "")).Where(n => n != "").ToList());

        var items = _mapper.Map<List<UserViewModel>>(users);
        foreach (var item in items)
        {
            if (userRolesMap.TryGetValue(item.Id, out var roles))
                item.Roles = roles;
        }

        return View("Index", new ListViewModel<UserViewModel>
        {
            Page = page,
            PageSize = size,
            TotalCount = totalCount,
            Items = items
        });
    }

    public override Task<IActionResult> Create() => Task.FromResult<IActionResult>(RedirectToAction(nameof(Index)));

    [HttpPost, ValidateAntiForgeryToken]
    public override async Task<IActionResult> Create(UserViewModel vm, List<IFormFile>? files = null)
    {
        await Task.CompletedTask;
        return RedirectToAction(nameof(Index));
    }

    public override Task<IActionResult> Edit(Guid id) => Task.FromResult<IActionResult>(RedirectToAction(nameof(Index)));

    [HttpPost, ValidateAntiForgeryToken]
    public override async Task<IActionResult> Edit(Guid id, UserViewModel vm, List<IFormFile>? files = null)
    {
        await Task.CompletedTask;
        return RedirectToAction(nameof(Index));
    }

    public new Task<IActionResult> Delete(Guid id) => Task.FromResult<IActionResult>(RedirectToAction(nameof(Index)));

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public new Task<IActionResult> DeleteConfirmed(Guid id) => Task.FromResult<IActionResult>(RedirectToAction(nameof(Index)));

    public async Task<IActionResult> ManageRoles(Guid userId)
    {
        var user = await _service.GetByIdAsync(userId);
        if (user == null) return NotFound();

        var allRoles = await _roleRepo.ListAsync(0, 10000);
        var assigned = await _userRoleRepo.ListAsync(0, 10000);
        var assignedIds = assigned
            .Where(x => x.UserId == userId)
            .Select(x => x.RoleId)
            .ToHashSet();

        var vm = new UserRoleAssignmentViewModel
        {
            UserId = user.Id,
            UserEmail = user.Email.Value,
            UserName = user.FullName,
            Roles = allRoles.Select(r => new RoleCheckItem
            {
                RoleId = r.Id,
                RoleName = r.Name,
                IsAssigned = assignedIds.Contains(r.Id)
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> ManageRoles(Guid userId, List<Guid> roleIds)
    {
        var user = await _service.GetByIdAsync(userId);
        if (user == null) return NotFound();

        roleIds ??= new List<Guid>();

        var existing = await _userRoleRepo.ListAsync(0, 10000);
        var toRemove = existing.Where(x => x.UserId == userId).ToList();

        foreach (var ur in toRemove)
            _userRoleRepo.Remove(ur);

        foreach (var rid in roleIds)
            await _userRoleRepo.AddAsync(new UserRole { UserId = userId, RoleId = rid });

        await _userRoleRepo.SaveChangesAsync();

        TempData["Success"] = "Roles updated successfully.";
        return RedirectToAction(nameof(ManageRoles), new { userId });
    }
}