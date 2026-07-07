using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using UserApp.Application.Permissions.Interfaces;
using UserApp.Domain.Roles;
using UserApp.Infrastructure.Persistence;
using UserApp.Infrastructure.Persistence.Seed;
using UserApp.Web.ViewModels.Permissions;

namespace UserApp.Web.Controllers;

public class PermissionsController
    : BaseController<Permission, PermissionViewModel>
{
    private readonly IServiceProvider _serviceProvider;

    public PermissionsController(
        IPermissionService service,
        IMapper mapper,
        IServiceProvider serviceProvider)
        : base(service, mapper)
    {
        _serviceProvider = serviceProvider;
    }

    public override Task<IActionResult> Create() => Task.FromResult<IActionResult>(RedirectToAction(nameof(Index)));

    [HttpPost, ValidateAntiForgeryToken]
    public override async Task<IActionResult> Create(PermissionViewModel vm, List<IFormFile>? files = null)
    {
        await Task.CompletedTask;
        return RedirectToAction(nameof(Index));
    }

    public override Task<IActionResult> Edit(Guid id) => Task.FromResult<IActionResult>(RedirectToAction(nameof(Index)));

    [HttpPost, ValidateAntiForgeryToken]
    public override async Task<IActionResult> Edit(Guid id, PermissionViewModel vm, List<IFormFile>? files = null)
    {
        await Task.CompletedTask;
        return RedirectToAction(nameof(Index));
    }

    public new Task<IActionResult> Delete(Guid id) => Task.FromResult<IActionResult>(RedirectToAction(nameof(Index)));

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public new Task<IActionResult> DeleteConfirmed(Guid id) => Task.FromResult<IActionResult>(RedirectToAction(nameof(Index)));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sync()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await RbacSeeder.SeedRolesAsync(db);
        await RbacSeeder.SeedPermissionsAsync(db);
        await RbacSeeder.SeedAdminRolePermissionsAsync(db);

        TempData["Success"] = "Permissions synced successfully from all controllers.";
        return RedirectToAction(nameof(Index));
    }
}