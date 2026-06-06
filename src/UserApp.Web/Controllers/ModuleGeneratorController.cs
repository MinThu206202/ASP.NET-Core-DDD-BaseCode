using Microsoft.AspNetCore.Mvc;
using UserApp.Application.Common.Interfaces;
using UserApp.Web.ViewModels.ModuleGenerator;

namespace UserApp.Web.Controllers;

public class ModuleGeneratorController : Controller
{
    private readonly IModuleGeneratorService _service;

    public ModuleGeneratorController(IModuleGeneratorService service)
    {
        _service = service;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Index(GenerateModuleViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        await _service.GenerateModuleAsync(vm.ModuleName);

        ViewBag.Message = $"{vm.ModuleName} module generated!";
        return View(vm);
    }
}