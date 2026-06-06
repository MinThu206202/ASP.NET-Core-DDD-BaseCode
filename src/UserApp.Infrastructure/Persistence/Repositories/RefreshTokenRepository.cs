using Microsoft.EntityFrameworkCore;
using UserApp.Application.Users.Interfaces;
using UserApp.Domain.Users;
using UserApp.Infrastructure.Persistence;

namespace UserApp.Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _context;

    public RefreshTokenRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RefreshToken token)
    {
        await _context.RefreshTokens.AddAsync(token);
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == token);
    }

    public async Task SaveAsync()
    {
        await _context.SaveChangesAsync();
    }
}