using UserApp.Domain.Roles;

namespace UserApp.Domain.Roles;

public interface IUserRoleRepository
{
    Task AddAsync(UserRole userRole);
    Task SaveAsync();

}