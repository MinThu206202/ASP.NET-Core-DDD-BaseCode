using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using UserApp.Application.Common;
using UserApp.Application.Common.DTOs;
using UserApp.Application.Common.Interfaces;
using UserApp.Infrastructure.Persistence;
using UserApp.Application.SidebarGroups.Interfaces;
using UserApp.Web.ViewModels.ModuleGenerator;

namespace UserApp.Web.Controllers.Api;

[ApiController]
[Route("api/module-generator")]
[AllowAnonymous]
public class ModuleGeneratorApiController : ControllerBase
{
    private readonly IModuleGeneratorService _service;
    private readonly AppDbContext _db;
    private readonly ISidebarGroupService _sidebarGroupService;

    public ModuleGeneratorApiController(
        IModuleGeneratorService service,
        AppDbContext db,
        ISidebarGroupService sidebarGroupService)
    {
        _service = service;
        _db = db;
        _sidebarGroupService = sidebarGroupService;
    }

    [HttpGet("tables")]
    public ActionResult<ApiResponse<List<string>>> GetTables()
    {
        var tables = _db.Model.GetEntityTypes()
            .Select(e => e.ClrType.Name)
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        return Ok(ApiResponse<List<string>>.Ok(tables, "Tables retrieved successfully"));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> Generate(
        [FromBody] GenerateModuleViewModel vm)
    {
        if (!ModelState.IsValid)
            return BadRequest(
                ApiResponse<object>.Fail("Invalid request"));

        var moduleName = vm.ModuleName?.Trim();

        if (string.IsNullOrWhiteSpace(moduleName))
            return BadRequest(
                ApiResponse<object>.Fail("Module name is required"));

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
            })
            .ToList();

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
            sidebarGroupId);

        return Ok(
            ApiResponse<object>.Ok(
                null,
                $"{moduleName} module generated successfully"));
    }
}
