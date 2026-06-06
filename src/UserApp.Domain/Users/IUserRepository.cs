using UserApp.Domain.Common;

namespace UserApp.Domain.Users;

public interface IUserRepository : IBaseRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
}
