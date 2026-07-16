public sealed class RoleAssignedEvent : DomainEvent
{
    public RoleAssignedEvent(
        Guid userId,
        Guid roleId,
        Guid assignedBy)
    {
        UserId = userId;
        RoleId = roleId;
        AssignedBy = assignedBy;
    }

    public Guid UserId { get; }

    public Guid RoleId { get; }

    public Guid AssignedBy { get; }
}