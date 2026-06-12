using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserApp.Application.Common;
using UserApp.Application.Users.Interfaces;
using UserApp.Application.Users.DTOs;
using UserApp.Domain.Common;
using UserApp.Domain.Roles;
using UserApp.Domain.Users;

namespace UserApp.Web.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersApiController : BaseApiController<User, UserDto>
    {
        private readonly IBaseRepository<Role> _roleRepo;
        private readonly IBaseRepository<UserRole> _userRoleRepo;

        public UsersApiController(
            IUserService service,
            IMapper mapper,
            IBaseRepository<Role> roleRepo,
            IBaseRepository<UserRole> userRoleRepo)
            : base(service, mapper)
        {
            _roleRepo = roleRepo;
            _userRoleRepo = userRoleRepo;
        }

        // ---------------- GET USER ROLES ----------------
        [HttpGet("{userId}/roles")]
        public async Task<ActionResult<ApiResponse<object>>> GetRoles(Guid userId)
        {
            var user = await _service.GetByIdAsync(userId);
            if (user == null)
                return NotFound(ApiResponse<object>.Fail("User not found"));

            var allRoles = await _roleRepo.ListAsync(0, 10000);
            var assigned = await _userRoleRepo.ListAsync(0, 10000);
            var assignedIds = assigned
                .Where(x => x.UserId == userId)
                .Select(x => x.RoleId)
                .ToHashSet();

            var result = allRoles.Select(r => new
            {
                roleId = r.Id,
                roleName = r.Name,
                isAssigned = assignedIds.Contains(r.Id)
            }).ToList();

            return Ok(ApiResponse<object>.Ok(result, "User roles retrieved successfully"));
        }

        // ---------------- UPDATE USER ROLES ----------------
        [HttpPost("{userId}/roles")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateRoles(Guid userId, List<Guid> roleIds)
        {
            var user = await _service.GetByIdAsync(userId);
            if (user == null)
                return NotFound(ApiResponse<object>.Fail("User not found"));

            roleIds ??= new List<Guid>();

            var existing = await _userRoleRepo.ListAsync(0, 10000);
            var toRemove = existing.Where(x => x.UserId == userId).ToList();

            foreach (var ur in toRemove)
                _userRoleRepo.Remove(ur);

            foreach (var rid in roleIds)
                await _userRoleRepo.AddAsync(new UserRole { UserId = userId, RoleId = rid });

            await _userRoleRepo.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(null, "Roles updated successfully"));
        }
    }
}