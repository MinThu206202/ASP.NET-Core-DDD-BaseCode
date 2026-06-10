using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserApp.Application.Common;
using UserApp.Application.Common.DTOs;
using UserApp.Application.Common.Interfaces;
using UserApp.Web.ViewModels.ModuleGenerator;

namespace UserApp.Web.Controllers.Api;

[ApiController]
[Route("api/module-generator")]
[Authorize]
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
                MaxValue = x.MaxValue
            })
            .ToList();

        await _service.GenerateModuleAsync(
            vm.ModuleName,
            fields,
            vm.RunMigration,
            vm.HasImage,
            vm.RunDbUpdate);

        return Ok(
            ApiResponse<object>.Ok(
                null,
                $"{vm.ModuleName} module generated successfully"));
    }
}