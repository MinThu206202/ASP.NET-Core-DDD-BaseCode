namespace UserApp.Domain.Notifications;

public enum NotificationType
{
    // User
    UserCreated,
    UserUpdated,
    UserDeleted,

    // Role
    RoleAssigned,
    RoleRemoved,

    // Permission
    PermissionGranted,
    PermissionRevoked,

    // Authentication
    PasswordChanged,
    LoginDetected,

    // Collaboration
    CommentAdded,
    Mentioned,

    // Task
    TaskAssigned,
    TaskCompleted,

    // System
    SystemAnnouncement,
    SystemMaintenance
}