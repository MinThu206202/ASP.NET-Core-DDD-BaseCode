using UserApp.Domain.Common;

namespace UserApp.Domain.Ais;

public class Ai : Entity<Guid>, IHasMedia
{
    public string Name { get; set; } = string.Empty;
    public int price { get; set; }
    public decimal stock { get; set; }

}