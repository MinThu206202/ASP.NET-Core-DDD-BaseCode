using System.ComponentModel.DataAnnotations;
using UserApp.Domain.Notifications;

namespace UserApp.Application.Notifications.DTOs;

public sealed class CreateNotificationRequest
{
    [Required]
    public Guid RecipientId { get; set; }

    public Guid? SenderId { get; set; }

    [Required]
    public NotificationType Type { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Message { get; set; } = string.Empty;

    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    public string? ActionUrl { get; set; }

    public string? Metadata { get; set; }

    public DateTime? ExpiresAt { get; set; }
}