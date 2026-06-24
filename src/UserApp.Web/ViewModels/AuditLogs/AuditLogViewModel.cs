namespace UserApp.Web.ViewModels.AuditLogs;

public class AuditLogViewModel
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string PageName { get; set; } = string.Empty;
    public string FunctionName { get; set; } = string.Empty;
    public string? AffectedColumns { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public DateTime CreatedAt { get; set; }
}
