using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UserApp.Application.Common;
using UserApp.Application.SidebarGroups.Interfaces;
using UserApp.Domain.SidebarGroups;

namespace UserApp.Web.Controllers.Api;

[ApiController]
[Route("api/sidebar-groups")]
[AllowAnonymous]
public class SidebarGroupApiController : ControllerBase
{
    private readonly ISidebarGroupService _service;

    public SidebarGroupApiController(ISidebarGroupService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<SidebarGroup>>>> GetAll()
    {
        var groups = await _service.GetAllOrderedAsync();
        return Ok(ApiResponse<List<SidebarGroup>>.Ok(groups));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> Create(
        [FromBody] SidebarGroupDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(ApiResponse<object>.Fail("Group name is required"));

        var group = new SidebarGroup
        {
            Name = dto.Name.Trim(),
            DisplayOrder = dto.DisplayOrder
        };

        await _service.AddAsync(group);
        return Ok(ApiResponse<object>.Ok(null, "Group created"));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Update(
        Guid id, [FromBody] SidebarGroupDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(ApiResponse<object>.Fail("Group name is required"));

        var group = await _service.GetByIdAsync(id);
        if (group == null)
            return NotFound(ApiResponse<object>.Fail("Group not found"));

        group.Name = dto.Name.Trim();
        group.DisplayOrder = dto.DisplayOrder;

        await _service.UpdateAsync(group);
        return Ok(ApiResponse<object>.Ok(null, "Group updated"));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id)
    {
        var group = await _service.GetByIdAsync(id);
        if (group == null)
            return NotFound(ApiResponse<object>.Fail("Group not found"));

        await _service.RemoveAsync(group);
        return Ok(ApiResponse<object>.Ok(null, "Group deleted"));
    }
}

public class SidebarGroupDto
{
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}
