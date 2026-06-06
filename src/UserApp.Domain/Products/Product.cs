using UserApp.Domain.Common;

namespace UserApp.Domain.Products;

public class Product: Entity<Guid>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}