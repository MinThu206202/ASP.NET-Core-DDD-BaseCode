using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using UserApp.Application.Common.Interfaces;
using UserApp.Application.CommonTables.Interfaces;
using UserApp.Domain.CommonTables;
using UserApp.Web.Common;
using System.Security.Claims;

namespace UserApp.Web.Controllers;

public abstract partial class BaseController<TEntity, TViewModel>
    where TEntity : class
    where TViewModel : class, new()
{
    protected bool ValidateModel<T>(T model)
    {
        var results = DynamicValidator.Validate(model);

        foreach (var error in results)
        {
            foreach (var member in error.MemberNames)
            {
                ModelState.AddModelError(member, error.ErrorMessage!);
            }
        }

        return ModelState.IsValid;
    }

    private async Task SetFlashMessageAsync(string action)
    {
        var entityName = typeof(TEntity).Name;
        var service = CommonTableService;
        if (service != null)
        {
            var all = await service.ListAsync(0, 999);
            var entry = all.FirstOrDefault(x => x.Type == "FlashMessage" && x.Code == $"{entityName}{action}");
            if (entry != null)
            {
                TempData["Success"] = entry.Name;
                return;
            }
        }
        TempData["Success"] = $"{entityName} {action.ToLower()} successfully";
    }

    private bool ValidateFiles(IEnumerable<IFormFile>? files)
    {
        if (files == null || !files.Any()) return true;

        const int maxSize = 5 * 1024 * 1024;
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };

        foreach (var file in files)
        {
            if (file.Length > maxSize)
            {
                ModelState.AddModelError("files", "Image size must be 5 MB or smaller.");
                return false;
            }

            if (!allowedTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                ModelState.AddModelError("files", "Only JPG, PNG, and WEBP files are allowed.");
                return false;
            }
        }

        return true;
    }

    protected async Task<bool> HasPermission(string permission)
    {
        if (_permissionService == null)
            return true;

        var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return false;

        return await _permissionService.HasPermissionAsync(
            Guid.Parse(userId),
            permission
        );
    }
}
