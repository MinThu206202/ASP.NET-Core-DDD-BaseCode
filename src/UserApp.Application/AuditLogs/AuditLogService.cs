using UserApp.Application.Common;
using UserApp.Application.AuditLogs.Interfaces;
using UserApp.Domain.AuditLogs;

namespace UserApp.Application.AuditLogs;

public class AuditLogService : BaseService<AuditLog>, IAuditLogService
{
    public AuditLogService(IAuditLogRepository repo) : base(repo) { }

    public async Task LogAsync(string userName, string action, string entityName, string entityId,
        string? pageName = null, string? functionName = null,
        string? affectedColumns = null, string? oldValues = null, string? newValues = null)
    {
        var audit = new AuditLog
        {
            UserName = userName,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            PageName = pageName ?? string.Empty,
            FunctionName = functionName ?? string.Empty,
            AffectedColumns = affectedColumns,
            OldValues = oldValues,
            NewValues = newValues
        };

        await _repo.AddAsync(audit);
        await _repo.SaveChangesAsync();
    }
}
