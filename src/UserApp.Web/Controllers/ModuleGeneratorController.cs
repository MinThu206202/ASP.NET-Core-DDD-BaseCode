using Microsoft.AspNetCore.Mvc;
using UserApp.Application.Common.Interfaces;
using UserApp.Application.SidebarGroups.Interfaces;
using UserApp.Application.SidebarItems.Interfaces;
using UserApp.Web.ViewModels.ModuleGenerator;
using UserApp.Application.Common.DTOs;
using System.Reflection;

namespace UserApp.Web.Controllers;

public class ModuleGeneratorController : Controller
{
    private readonly IModuleGeneratorService _service;
    private readonly ISidebarGroupService _sidebarGroupService;
    private readonly ISidebarItemService _sidebarItemService;
    private static readonly HashSet<string> ExcludedTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "CommonTable", "RefreshToken", "Permission", "RolePermission", "UserRole", "Media"
    };

    public ModuleGeneratorController(
        IModuleGeneratorService service,
        ISidebarGroupService sidebarGroupService,
        ISidebarItemService sidebarItemService)
    {
        _service = service;
        _sidebarGroupService = sidebarGroupService;
        _sidebarItemService = sidebarItemService;
    }

    // =========================
    // GET
    // =========================
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var vm = new GenerateModuleViewModel
        {
            AvailableTables = GetAvailableTables(),
            SidebarGroups = await _sidebarGroupService.GetAllOrderedAsync(),
            SidebarItems = await _sidebarItemService.GetActiveAsync()
        };
        return View(vm);
    }

    // =========================
    // POST
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(GenerateModuleViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.AvailableTables = GetAvailableTables();
            vm.SidebarGroups = await _sidebarGroupService.GetAllOrderedAsync();
            vm.SidebarItems = await _sidebarItemService.GetActiveAsync();
            return View(vm);
        }

        var moduleName = vm.ModuleName?.Trim();

        var fields = vm.Fields
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .Select(x => new ModuleFieldDto
            {
                Name = x.Name,
                Type = x.Type,
                Length = x.Length,
                IsRequired = x.IsRequired,
                IsNullable = x.IsNullable,
                MinLength = x.MinLength,
                MaxLength = x.MaxLength,
                MinValue = x.MinValue,
                MaxValue = x.MaxValue,
                EnumValues = x.EnumValues,
                UseCommonTable = x.UseCommonTable,
                EnumRenderAsCheckbox = x.EnumRenderAsCheckbox,
                IsRelation = x.IsRelation,
                RelatedEntityName = x.RelatedEntityName,
                IsPivot = x.IsPivot,
                DeleteBehavior = x.DeleteBehavior
            }).ToList();

        // Read relation data from raw form (bypasses List<T> gap-index limitation)
        var relName = HttpContext.Request.Form["_rel_Name"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(relName))
        {
            var relIsPivot = HttpContext.Request.Form["_rel_IsPivot"].FirstOrDefault() == "true";
            var relDeleteBehavior = HttpContext.Request.Form["_rel_DeleteBehavior"].FirstOrDefault() ?? "Cascade";
            fields.Add(new ModuleFieldDto
            {
                Name = relName,
                Type = "relation",
                IsRelation = true,
                RelatedEntityName = relName,
                IsPivot = relIsPivot,
                DeleteBehavior = relDeleteBehavior
            });
        }

        // Determine sidebar group when sidebar is enabled
        Guid? sidebarGroupId = null;
        if (vm.EnableSidebar)
        {
            var groups = await _sidebarGroupService.GetAllOrderedAsync();
            sidebarGroupId = groups.FirstOrDefault()?.Id;
        }

        await _service.GenerateModuleAsync(
            moduleName,
            fields,
            vm.RunMigration,
            vm.HasImage,
            vm.RunDbUpdate,
            sidebarGroupId
        );

        TempData["Success"] = $"{moduleName} module generated successfully!";
        return RedirectToAction(nameof(Index));
    }

    private static List<string> GetAvailableTables()
    {
        var domainAssembly = Assembly.Load("UserApp.Domain");
        var entityTypes = domainAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                && t.Namespace != null
                && t.Namespace.StartsWith("UserApp.Domain")
                && t.BaseType != null
                && t.BaseType.IsGenericType
                && t.BaseType.GetGenericTypeDefinition().Name == "Entity`1")
            .Select(t => t.Name)
            .Where(name => !ExcludedTables.Contains(name))
            .OrderBy(name => name)
            .ToList();

        return entityTypes;
    }
}
