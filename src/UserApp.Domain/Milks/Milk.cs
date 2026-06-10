using UserApp.Domain.Common;

namespace UserApp.Domain.Milks;

public class Milk : Entity<Guid>, IHasMedia
{
    public string Name { get; set; } = string.Empty;
    public int Price { get; set; }

}