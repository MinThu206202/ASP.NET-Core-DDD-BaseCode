using UserApp.Domain.Common;

namespace UserApp.Domain.Messengers;

public class Messenger : Entity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

}