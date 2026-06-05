using Microsoft.EntityFrameworkCore;
using UserApp.Domain.Common;
using UserApp.Domain.Users;

namespace UserApp.Infrastructure.Persistence.Repositories;

public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext db) : base(db) { }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        await Entities.FirstOrDefaultAsync(u => u.Email.Value == email, ct);

    public override async Task<IReadOnlyList<User>> ListAsync(int skip, int take, CancellationToken ct = default) =>
        await Entities.OrderByDescending(u => u.CreatedAt).Skip(skip).Take(take).ToListAsync(ct);
}
