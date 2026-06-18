using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UserApp.Application.Common;
using UserApp.Application.SidebarGroups.Interfaces;
using UserApp.Application.SidebarItems.Interfaces;
using UserApp.Domain.SidebarItems;

namespace UserApp.Web.Controllers.Api;

[ApiController]
[Route("api/sidebar-items")]
[AllowAnonymous]
public class SidebarItemApiController : ControllerBase
{
    private readonly ISidebarItemService _service;
    private readonly ISidebarGroupService _groupService;

    public SidebarItemApiController(ISidebarItemService service, ISidebarGroupService groupService)
    {
        _service = service;
        _groupService = groupService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<SidebarItem>>>> GetAll()
    {
        var items = await _service.GetActiveAsync();
        return Ok(ApiResponse<List<SidebarItem>>.Ok(items));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> Create(
        [FromBody] SidebarItemCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ModuleName))
            return BadRequest(ApiResponse<object>.Fail("Module name is required"));

        var group = await _groupService.GetByIdAsync(dto.GroupId);
        if (group == null)
            return BadRequest(ApiResponse<object>.Fail("Group not found"));

        var item = new SidebarItem
        {
            ModuleName = dto.ModuleName.Trim(),
            ControllerName = dto.ModuleName.Trim(),
            GroupId = group.Id,
            AreaName = "",
            DisplayOrder = dto.DisplayOrder
        };

        await _service.AddAsync(item);
        return Ok(ApiResponse<object>.Ok(null, "Module added to sidebar"));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Update(
        Guid id, [FromBody] SidebarItemUpdate dto)
    {
        var item = await _service.GetByIdAsync(id);
        if (item == null)
            return NotFound(ApiResponse<object>.Fail("Item not found"));

        item.ModuleName = dto.ModuleName?.Trim() ?? item.ModuleName;
        if (dto.GroupId.HasValue && dto.GroupId.Value != Guid.Empty)
            item.GroupId = dto.GroupId.Value;
        item.DisplayOrder = dto.DisplayOrder;

        await _service.UpdateAsync(item);
        return Ok(ApiResponse<object>.Ok(null, "Item updated"));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item == null)
            return NotFound(ApiResponse<object>.Fail("Item not found"));

        await _service.RemoveAsync(item);
        return Ok(ApiResponse<object>.Ok(null, "Item deleted"));
    }

    [HttpPut("reorder")]
    public async Task<ActionResult<ApiResponse<object>>> Reorder(
        [FromBody] SidebarReorderDto dto)
    {
        if (dto.Items == null || dto.Items.Count == 0)
            return BadRequest(ApiResponse<object>.Fail("No items to reorder"));

        foreach (var update in dto.Items)
        {
            var item = await _service.GetByIdAsync(update.Id);
            if (item == null) continue;

            item.GroupId = update.GroupId ?? item.GroupId;
            item.DisplayOrder = update.DisplayOrder;
            await _service.UpdateAsync(item);
        }

        return Ok(ApiResponse<object>.Ok(null, "Reordered successfully"));
    }

    [HttpPut("reorder-groups")]
    public async Task<ActionResult<ApiResponse<object>>> ReorderGroups(
        [FromBody] GroupReorderDto dto)
    {
        if (dto.Groups == null || dto.Groups.Count == 0)
            return BadRequest(ApiResponse<object>.Fail("No groups to reorder"));

        var groupService = HttpContext.RequestServices
            .GetService(typeof(UserApp.Application.SidebarGroups.Interfaces.ISidebarGroupService))
            as UserApp.Application.SidebarGroups.Interfaces.ISidebarGroupService;

        if (groupService == null)
            return BadRequest(ApiResponse<object>.Fail("Group service unavailable"));

        foreach (var update in dto.Groups)
        {
            var group = await groupService.GetByIdAsync(update.Id);
            if (group == null) continue;

            group.DisplayOrder = update.DisplayOrder;
            await groupService.UpdateAsync(group);
        }

        return Ok(ApiResponse<object>.Ok(null, "Groups reordered"));
    }
}

public class SidebarReorderDto
{
    public List<SidebarItemUpdate> Items { get; set; } = new();
}

public class SidebarItemUpdate
{
    public Guid Id { get; set; }
    public string? ModuleName { get; set; }
    public Guid? GroupId { get; set; }
    public int DisplayOrder { get; set; }
}

public class GroupReorderDto
{
    public List<GroupUpdate> Groups { get; set; } = new();
}

public class GroupUpdate
{
    public Guid Id { get; set; }
    public int DisplayOrder { get; set; }
}

public class SidebarItemCreateDto
{
    public string ModuleName { get; set; } = "";
    public Guid GroupId { get; set; }
    public int DisplayOrder { get; set; }
}
