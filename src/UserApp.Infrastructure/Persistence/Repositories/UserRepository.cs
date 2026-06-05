using Microsoft.EntityFrameworkCore;
using UserApp.Domain.Users;

namespace UserApp.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;
    public UserRepository(AppDbContext db) => _db = db;

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.Email.Value == email, ct);

    public async Task<IReadOnlyList<User>> ListAsync(int skip, int take, CancellationToken ct = default) =>
        await _db.Users.OrderByDescending(u => u.CreatedAt).Skip(skip).Take(take).ToListAsync(ct);

    public Task<int> CountAsync(CancellationToken ct = default) => _db.Users.CountAsync(ct);

    public async Task AddAsync(User user, CancellationToken ct = default) =>
        await _db.Users.AddAsync(user, ct);

    public void Update(User user) => _db.Users.Update(user);
    public void Remove(User user) => _db.Users.Remove(user);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
