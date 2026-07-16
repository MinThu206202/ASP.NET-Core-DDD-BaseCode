using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UserApp.Application.Notifications.DTOs;
using UserApp.Application.Notifications.Interfaces;
using UserApp.Application.Users.Interfaces;
using UserApp.Domain.Common;
using UserApp.Domain.Notifications;
using UserApp.Domain.Roles;
using UserApp.Domain.Users;
using UserApp.Web.ViewModels;

namespace UserApp.Web.Controllers;

public class UsersController : BaseController<User, UserViewModel>
{
    private readonly IBaseRepository<Role> _roleRepo;
    private readonly IBaseRepository<UserRole> _userRoleRepo;
    private readonly IUserService _userService;
    private readonly INotificationService _notificationService;

    public UsersController(
        IUserService service,
        IMapper mapper,
        IBaseRepository<Role> roleRepo,
        IBaseRepository<UserRole> userRoleRepo,
        INotificationService notificationService)
        : base(service, mapper)
    {
        _userService = service;
        _roleRepo = roleRepo;
        _userRoleRepo = userRoleRepo;
        _notificationService = notificationService;
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

    public new IActionResult Create()
    {
        return View(new CreateUserViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        try
        {
            var created = await _userService.CreateAsync(vm.Email!, vm.FullName!, vm.Password!);
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            await _notificationService.SendAsync(new CreateNotificationRequest
            {
                RecipientId = created.Id,
                SenderId = currentUserId != null ? Guid.Parse(currentUserId) : null,
                Title = "Welcome to UserApp",
                Message = $"Your account has been created. Welcome, {vm.FullName}!",
                Type = NotificationType.UserCreated,
                ActionUrl = "/Auth/Login"
            });

            TempData["Success"] = "User created successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(vm);
        }
    }

    public override async Task<IActionResult> Edit(Guid id)
    {
        var user = await _service.GetByIdAsync(id);
        if (user == null) return NotFound();

        var vm = new EditUserViewModel
        {
            Id = user.Id,
            Email = user.Email.Value,
            FullName = user.FullName
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, EditUserViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var user = await _service.GetByIdAsync(id);
        if (user == null) return NotFound();

        try
        {
            user.ChangeEmail(Email.Create(vm.Email!));
            user.UpdateProfile(vm.FullName!);
            await _service.UpdateAsync(user);
            await _service.SaveAsync();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _notificationService.SendAsync(new CreateNotificationRequest
            {
                RecipientId = id,
                SenderId = currentUserId != null ? Guid.Parse(currentUserId) : null,
                Title = "Profile Updated",
                Message = "Your profile information has been updated.",
                Type = NotificationType.UserUpdated,
                ActionUrl = "/Users"
            });

            TempData["Success"] = "User updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(vm);
        }
    }

    public new async Task<IActionResult> Delete(Guid id)
    {
        var user = await _service.GetByIdAsync(id);
        if (user == null) return NotFound();

        var vm = new DeleteUserViewModel
        {
            Id = user.Id,
            Email = user.Email.Value,
            FullName = user.FullName
        };
        return View(vm);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public new async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var user = await _service.GetByIdAsync(id);
        if (user == null) return NotFound();

        var name = user.FullName;
        await _service.RemoveAsync(user);
        await _service.SaveAsync();

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId != null && Guid.TryParse(currentUserId, out var adminId))
        {
            await _notificationService.SendAsync(new CreateNotificationRequest
            {
                RecipientId = adminId,
                Title = "User Deleted",
                Message = $"User \"{name}\" has been deleted from the system.",
                Type = NotificationType.UserDeleted
            });
        }

        TempData["Success"] = "User deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

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
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManageRoles(Guid userId, List<Guid> roleIds)
    {
        var user = await _service.GetByIdAsync(userId);
        if (user == null) return NotFound();

        roleIds ??= new List<Guid>();

        var allRoles = await _roleRepo.ListAsync(0, 10000);
        var roleLookup = allRoles.ToDictionary(r => r.Id, r => r.Name);

        var existing = await _userRoleRepo.ListAsync(0, 10000);
        var oldRoleIds = existing.Where(x => x.UserId == userId).Select(x => x.RoleId).ToHashSet();
        var toRemove = existing.Where(x => x.UserId == userId).ToList();

        foreach (var ur in toRemove)
            _userRoleRepo.Remove(ur);

        foreach (var rid in roleIds)
            await _userRoleRepo.AddAsync(new UserRole { UserId = userId, RoleId = rid });

        await _userRoleRepo.SaveChangesAsync();

        var addedRoles = roleIds.Where(r => !oldRoleIds.Contains(r)).ToList();
        var removedRoles = oldRoleIds.Where(r => !roleIds.Contains(r)).ToList();

        if (addedRoles.Count > 0)
        {
            var names = string.Join(", ", addedRoles.Select(id => roleLookup.GetValueOrDefault(id)).Where(n => n != null));
            await _notificationService.SendAsync(new CreateNotificationRequest
            {
                RecipientId = userId,
                Title = "Role Assigned",
                Message = $"You have been assigned the role(s): {names}",
                Type = NotificationType.RoleAssigned
            });
        }

        if (removedRoles.Count > 0)
        {
            var names = string.Join(", ", removedRoles.Select(id => roleLookup.GetValueOrDefault(id)).Where(n => n != null));
            await _notificationService.SendAsync(new CreateNotificationRequest
            {
                RecipientId = userId,
                Title = "Role Removed",
                Message = $"The role(s) have been removed: {names}",
                Type = NotificationType.RoleRemoved
            });
        }

        TempData["Success"] = "Roles updated successfully.";
        return RedirectToAction(nameof(ManageRoles), new { userId });
    }
}