using Microsoft.EntityFrameworkCore;
using UserApp.Domain.Users;
using UserApp.Infrastructure.Persistence;



namespace UserApp.Infrastructure.Persistence.Repositories;

public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext db) : base(db) { }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _db.Set<User>()
            .FirstOrDefaultAsync(x => x.Email.Value == email);
    }
}