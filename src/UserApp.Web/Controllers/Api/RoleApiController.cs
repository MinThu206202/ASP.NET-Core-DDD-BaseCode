using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserApp.Application.Common;
using UserApp.Application.Roles.Interfaces;
using UserApp.Domain.Common;
using UserApp.Domain.Roles;
using UserApp.Web.ViewModels.Roles;
namespace UserApp.Web.Controllers.Api;

[ApiController]
[Route("api/roles")]
public class RolesApiController : BaseApiController<Role, RoleViewModel>
{
    private readonly IBaseRepository<Permission> _permRepo;
    private readonly IBaseRepository<RolePermission> _rpRepo;

    public RolesApiController(
        IRoleService service,
        IMapper mapper,
        IBaseRepository<Permission> permRepo,
        IBaseRepository<RolePermission> rpRepo) : base(service, mapper)
    {
        _permRepo = permRepo;
        _rpRepo = rpRepo;
    }

    [HttpGet("{id}/permissions")]
    [ActionName(nameof(ManagePermissions))]
    public async Task<ActionResult<ApiResponse<object>>> GetPermissions(Guid id)
    {
        var role = await _service.GetByIdAsync(id);
        if (role == null)
            return NotFound(ApiResponse<object>.Fail("Role not found"));

        var existing = await _rpRepo.ListAsync(0, 10000);

        var permissionIds = existing
            .Where(x => x.RoleId == id)
            .Select(x => x.PermissionId)
            .ToList();

        return Ok(ApiResponse<object>.Ok(
            new
            {
                roleId = id,
                permissionIds
            },
            "Role permissions retrieved successfully"
        ));
    }

    [HttpPost("{id}/permissions")]
    public async Task<ActionResult<ApiResponse<object>>> ManagePermissions(Guid id, [FromBody] List<Guid> permissionIds)
    {
        var role = await _service.GetByIdAsync(id);
        if (role == null)
            return NotFound(ApiResponse<object>.Fail("Role not found"));

        permissionIds ??= new List<Guid>();

        var existing = await _rpRepo.ListAsync(0, 10000);
        var toRemove = existing.Where(x => x.RoleId == id).ToList();

        foreach (var rp in toRemove)
            _rpRepo.Remove(rp);

        foreach (var pid in permissionIds)
            await _rpRepo.AddAsync(new RolePermission { RoleId = id, PermissionId = pid });

        await _rpRepo.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(null, "Permissions updated successfully"));
    }

}
