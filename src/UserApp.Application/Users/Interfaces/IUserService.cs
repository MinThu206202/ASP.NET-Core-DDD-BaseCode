using UserApp.Domain.Users;
using UserApp.Application.Common;
using UserApp.Application.Users.DTOs;


namespace UserApp.Application.Users.Interfaces
{
    public interface IUserService : IBaseService<User>
    {
        // You can add user-specific methods if needed
        Task<UserDto> CreateAsync(string email, string fullName, string password);


        Task<User?> GetByEmailAsync(string email);
        Task<User?> ValidateUserAsync(string email, string password);
    }
}