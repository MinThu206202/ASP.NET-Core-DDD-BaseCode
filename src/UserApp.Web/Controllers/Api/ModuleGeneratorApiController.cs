using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UserApp.Application.Common;
using UserApp.Application.Common.DTOs;
using UserApp.Application.Common.Interfaces;
using UserApp.Web.ViewModels.ModuleGenerator;

namespace UserApp.Web.Controllers.Api;

[ApiController]
[Route("api/module-generator")]
[AllowAnonymous]
public class ModuleGeneratorApiController : ControllerBase
{
    private readonly IModuleGeneratorService _service;

    public ModuleGeneratorApiController(
        IModuleGeneratorService service)
    {
        _service = service;
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
                EnumRenderAsCheckbox = x.EnumRenderAsCheckbox
            })
            .ToList();

        await _service.GenerateModuleAsync(
            moduleName,
            fields,
            vm.RunMigration,
            vm.HasImage,
            vm.RunDbUpdate);

        return Ok(
            ApiResponse<object>.Ok(
                null,
                $"{moduleName} module generated successfully"));
    }
}