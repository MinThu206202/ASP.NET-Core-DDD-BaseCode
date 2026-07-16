using UserApp.Application.Notifications.DTOs;
using UserApp.Application.Common.Interfaces;
using UserApp.Application.Notifications.Interfaces;
using UserApp.Domain.Notifications;
using AutoMapper;

namespace UserApp.Application.Notifications.Services;

public sealed class NotificationService
    : INotificationService
{
    private readonly INotificationRepository _repository;
    private readonly INotificationDispatcher _dispatcher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public NotificationService(
        INotificationRepository repository,
        INotificationDispatcher dispatcher,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _repository = repository;
        _dispatcher = dispatcher;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task SendAsync(
        CreateNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        var notification = new Notification(
            recipientId: request.RecipientId,
            type: request.Type,
            title: request.Title,
            message: request.Message,
            senderId: request.SenderId,
            priority: request.Priority,
            actionUrl: request.ActionUrl,
            metadata: request.Metadata,
            expiresAt: request.ExpiresAt);

        notification.MarkAsQueued();

        await _repository.AddAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _dispatcher.DispatchAsync(notification, cancellationToken);

        notification.MarkAsDelivered();
        await _repository.UpdateAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAsReadAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        var notification = await _repository.GetByIdAsync(notificationId, cancellationToken)
            ?? throw new InvalidOperationException("Notification not found");

        notification.MarkAsRead();
        await _repository.UpdateAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAsSeenAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        var notification = await _repository.GetByIdAsync(notificationId, cancellationToken)
            ?? throw new InvalidOperationException("Notification not found");

        notification.MarkAsSeen();
        await _repository.UpdateAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationDto>>
        GetNotificationsAsync(
            Guid recipientId,
            CancellationToken cancellationToken = default)
    {
        var notifications = await _repository.GetByRecipientAsync(recipientId, cancellationToken);
        return _mapper.Map<IReadOnlyList<NotificationDto>>(notifications);
    }

    public async Task<int> GetUnreadCountAsync(
        Guid recipientId,
        CancellationToken cancellationToken = default)
    {
        return await _repository.GetUnreadCountAsync(recipientId, cancellationToken);
    }
}