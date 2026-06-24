using UserApp.Domain.Common;

namespace UserApp.Domain.AuditLogs;

public interface IAuditLogRepository : IBaseRepository<AuditLog>
{
}
