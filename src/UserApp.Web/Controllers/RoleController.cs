using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UserApp.Application.Notifications.DTOs;
using UserApp.Application.Notifications.Interfaces;
using UserApp.Application.Roles.Interfaces;
using UserApp.Domain.Common;
using UserApp.Domain.Notifications;
using UserApp.Domain.Roles;
using UserApp.Web.ViewModels.Roles;

namespace UserApp.Web.Controllers;

public class RolesController : BaseController<Role, RoleViewModel>
{
    private readonly IBaseRepository<Permission> _permRepo;
    private readonly IBaseRepository<RolePermission> _rpRepo;
    private readonly INotificationService _notificationService;

    public RolesController(
        IRoleService service,
        IMapper mapper,
        IBaseRepository<Permission> permRepo,
        IBaseRepository<RolePermission> rpRepo,
        INotificationService notificationService) : base(service, mapper)
    {
        _permRepo = permRepo;
        _rpRepo = rpRepo;
        _notificationService = notificationService;
    }

    public async Task<IActionResult> ManagePermissions(Guid roleId)
    {
        var role = await _service.GetByIdAsync(roleId);
        if (role == null) return NotFound();

        var allPermissions = await _permRepo.ListAsync(0, 10000);
        var assigned = await _rpRepo.ListAsync(0, 10000);
        var assignedIds = assigned
            .Where(x => x.RoleId == roleId)
            .Select(x => x.PermissionId)
            .ToHashSet();

        var vm = new RolePermissionAssignmentViewModel
        {
            RoleId = role.Id,
            RoleName = role.Name,
            Permissions = allPermissions.Select(p => new PermissionCheckItem
            {
                PermissionId = p.Id,
                PermissionName = p.Name,
                IsAssigned = assignedIds.Contains(p.Id)
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> ManagePermissions(Guid roleId, List<Guid> permissionIds)
    {
        var role = await _service.GetByIdAsync(roleId);
        if (role == null) return NotFound();

        permissionIds ??= new List<Guid>();

        var existing = await _rpRepo.ListAsync(0, 10000);
        var oldPermissionIds = existing.Where(x => x.RoleId == roleId).Select(x => x.PermissionId).ToHashSet();
        var toRemove = existing.Where(x => x.RoleId == roleId).ToList();

        foreach (var rp in toRemove)
            _rpRepo.Remove(rp);

        foreach (var pid in permissionIds)
            await _rpRepo.AddAsync(new RolePermission { RoleId = roleId, PermissionId = pid });

        await _rpRepo.SaveChangesAsync();

        var addedPerms = permissionIds.Where(p => !oldPermissionIds.Contains(p)).ToList();
        var removedPerms = oldPermissionIds.Where(p => !permissionIds.Contains(p)).ToList();
        var allPerms = await _permRepo.ListAsync(0, 10000);
        var permLookup = allPerms.ToDictionary(p => p.Id, p => p.Name);
        var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var currentUserId = currentUserIdStr != null ? Guid.Parse(currentUserIdStr) : Guid.Empty;

        if (addedPerms.Count > 0)
        {
            var names = string.Join(", ", addedPerms.Select(id => permLookup.GetValueOrDefault(id)).Where(n => n != null));
            await _notificationService.SendAsync(new CreateNotificationRequest
            {
                RecipientId = currentUserId,
                Title = "Permissions Granted",
                Message = $"Permissions granted to role \"{role.Name}\": {names}",
                Type = NotificationType.PermissionGranted
            });
        }

        if (removedPerms.Count > 0)
        {
            var names = string.Join(", ", removedPerms.Select(id => permLookup.GetValueOrDefault(id)).Where(n => n != null));
            await _notificationService.SendAsync(new CreateNotificationRequest
            {
                RecipientId = currentUserId,
                Title = "Permissions Revoked",
                Message = $"Permissions revoked from role \"{role.Name}\": {names}",
                Type = NotificationType.PermissionRevoked
            });
        }

        TempData["Success"] = "Permissions updated successfully.";
        return RedirectToAction(nameof(ManagePermissions), new { roleId });
    }
}
