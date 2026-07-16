namespace UserApp.Domain.Notifications;

public interface INotificationRepository
{
    Task AddAsync(
        Notification notification,
        CancellationToken cancellationToken = default);

    Task<Notification?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notification>> GetByRecipientAsync(
        Guid recipientId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notification>> GetUnreadByRecipientAsync(
        Guid recipientId,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(
        Guid recipientId,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Notification notification,
        CancellationToken cancellationToken = default);
}