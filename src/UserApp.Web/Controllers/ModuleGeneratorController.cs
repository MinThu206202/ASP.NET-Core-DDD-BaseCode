using Microsoft.AspNetCore.Mvc;
using UserApp.Application.Common.Interfaces;
using UserApp.Web.ViewModels.ModuleGenerator;
using UserApp.Application.Common.DTOs;
using System.Reflection;

namespace UserApp.Web.Controllers;

public class ModuleGeneratorController : Controller
{
    private readonly IModuleGeneratorService _service;
    private static readonly HashSet<string> ExcludedTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "CommonTable", "RefreshToken", "Permission", "RolePermission", "UserRole", "Media"
    };

    public ModuleGeneratorController(IModuleGeneratorService service)
    {
        _service = service;
    }

    // =========================
    // GET
    // =========================
    [HttpGet]
    public IActionResult Index()
    {
        var vm = new GenerateModuleViewModel
        {
            AvailableTables = GetAvailableTables()
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
            return View(vm);
        }

        var moduleName = vm.ModuleName?.Trim();

        var fields = vm.Fields
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
                IsPivot = x.IsPivot
            }).ToList();

        await _service.GenerateModuleAsync(
            moduleName,
            fields,
            vm.RunMigration,
            vm.HasImage,
            vm.RunDbUpdate
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