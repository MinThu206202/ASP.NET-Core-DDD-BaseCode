using UserApp.Application.Users.Interfaces;
using UserApp.Domain.Users;
using UserApp.Application.Common;
using UserApp.Application.Users.DTOs;
using System.Linq.Expressions;

namespace UserApp.Application.Users
{
    public class UserService : BaseService<User>, IUserService
    {
        private readonly IUserRepository _userRepo;
        private readonly IPasswordHasher _hasher;

        public UserService(IUserRepository repo, IPasswordHasher hasher)
            : base(repo)
        {
            _userRepo = repo;
            _hasher = hasher;
        }

        public async Task<UserDto> CreateAsync(string email, string fullName, string password)
        {
            var emailVo = Email.Create(email);
            var existingUser = await _userRepo.GetByEmailAsync(emailVo.Value);
            if (existingUser != null)
                throw new InvalidOperationException("Email already in use");

            var user = User.Create(emailVo, fullName, _hasher.Hash(password));
            await AddAsync(user);
            await SaveAsync();

            return new UserDto(user.Id, user.Email.Value, user.FullName, user.Status.ToString(), user.CreatedAt);
        }

        // ✅ FIX: Call repository method instead of _repo.Entities
        public Task<User?> GetByEmailAsync(string email)
        {
            return _userRepo.GetByEmailAsync(email);
        }

        public async Task<User?> ValidateUserAsync(string email, string password)
        {
            var user = await GetByEmailAsync(email);
            if (user == null) return null;

            // Compare hashed password
            return _hasher.Verify(password, user.PasswordHash) ? user : null;
        }
    }
}