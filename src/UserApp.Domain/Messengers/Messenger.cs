using UserApp.Domain.Common;

namespace UserApp.Domain.Messengers;

public class Messenger : Entity<Guid>, IHasMedia
{
    public Guid MilkId { get; set; }
    public string Name { get; set; } = string.Empty;

}