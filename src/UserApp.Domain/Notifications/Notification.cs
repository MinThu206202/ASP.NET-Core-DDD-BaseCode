using System.ComponentModel.DataAnnotations.Schema;
using UserApp.Domain.Common;

namespace UserApp.Domain.Notifications;

public class Notification : Entity<Guid>, IHasMedia
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;

}