using UserApp.Domain.Common;

namespace UserApp.Domain.Paps;

public class Pap : Entity<Guid>, IHasMedia
{
    public string Name { get; set; } = string.Empty;

}