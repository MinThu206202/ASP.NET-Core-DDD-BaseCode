using Microsoft.EntityFrameworkCore;
using UserApp.Domain.Notifications;
using UserApp.Infrastructure.Persistence;

namespace UserApp.Infrastructure.Notifications.Repositories;

public sealed class NotificationRepository 
    : INotificationRepository
{
    private readonly AppDbContext _context;


    public NotificationRepository(AppDbContext context)
    {
        _context = context;
    }


    public async Task AddAsync(
        Notification notification,
        CancellationToken cancellationToken = default)
    {
        await _context.Notifications
            .AddAsync(notification, cancellationToken);
    }


    public async Task<Notification?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }


    public async Task<IReadOnlyList<Notification>> 
        GetByRecipientAsync(
            Guid recipientId,
            CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .Where(x => x.RecipientId == recipientId)
            .OrderByDescending(x => x.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }


    public async Task<IReadOnlyList<Notification>>
        GetUnreadByRecipientAsync(
            Guid recipientId,
            CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .Where(x =>
                x.RecipientId == recipientId &&
                x.ReadAt == null)
            .OrderByDescending(x => x.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }


    public async Task<int> GetUnreadCountAsync(
        Guid recipientId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .CountAsync(
                x =>
                    x.RecipientId == recipientId &&
                    x.ReadAt == null,
                cancellationToken);
    }


    public Task UpdateAsync(
        Notification notification,
        CancellationToken cancellationToken = default)
    {
        _context.Notifications.Update(notification);

        return Task.CompletedTask;
    }
}