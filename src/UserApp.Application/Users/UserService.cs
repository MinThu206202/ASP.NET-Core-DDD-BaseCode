using UserApp.Application.Users.DTOs;
using UserApp.Application.Users.Interfaces;
using UserApp.Domain.Users;

namespace UserApp.Application.Users;

public class UserService
{
    private readonly IUserRepository _repo;
    private readonly IPasswordHasher _hasher;

    public UserService(IUserRepository repo, IPasswordHasher hasher)
    {
        _repo = repo;
        _hasher = hasher;
    }

    public async Task<UserDto> CreateAsync(string email, string fullName, string password, CancellationToken ct = default)
    {
        var emailVo = Email.Create(email);
        var existing = await _repo.GetByEmailAsync(emailVo.Value, ct);
        if (existing is not null)
            throw new InvalidOperationException("Email already in use");

        var user = User.Create(emailVo, fullName, _hasher.Hash(password));
        await _repo.AddAsync(user, ct);
        await _repo.SaveChangesAsync(ct);
        return ToDto(user);
    }

    public async Task<UserDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var u = await _repo.GetByIdAsync(id, ct);
        return u is null ? null : ToDto(u);
    }

    public async Task<(IReadOnlyList<UserDto> Items, int Total)> ListAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var skip = (page - 1) * pageSize;
        var items = await _repo.ListAsync(skip, pageSize, ct);
        var total = await _repo.CountAsync(ct);
        return (items.Select(ToDto).ToList(), total);
    }

    public async Task UpdateAsync(Guid id, string fullName, string email, CancellationToken ct = default)
    {
        var user = await _repo.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("User not found");
        user.UpdateProfile(fullName);
        var emailVo = Email.Create(email);
        if (user.Email.Value != emailVo.Value)
        {
            var existing = await _repo.GetByEmailAsync(emailVo.Value, ct);
            if (existing is not null) throw new InvalidOperationException("Email already in use");
            user.ChangeEmail(emailVo);
        }
        _repo.Update(user);
        await _repo.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _repo.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("User not found");
        _repo.Remove(user);
        await _repo.SaveChangesAsync(ct);
    }

    private static UserDto ToDto(User u) =>
        new(u.Id, u.Email.Value, u.FullName, u.Status.ToString(), u.CreatedAt);
}
