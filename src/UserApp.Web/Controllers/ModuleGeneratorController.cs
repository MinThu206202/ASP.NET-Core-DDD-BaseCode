using Microsoft.AspNetCore.Mvc;
using UserApp.Application.Common.Interfaces;
using UserApp.Web.ViewModels.ModuleGenerator;
using UserApp.Application.Common.DTOs;

namespace UserApp.Web.Controllers;

public class ModuleGeneratorController : Controller
{
    private readonly IModuleGeneratorService _service;

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
        return View(new GenerateModuleViewModel());
    }

    // =========================
    // POST
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(GenerateModuleViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

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
                UseCommonTable = x.UseCommonTable
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
}