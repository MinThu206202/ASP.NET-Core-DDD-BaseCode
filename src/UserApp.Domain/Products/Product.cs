using UserApp.Domain.Common;

namespace UserApp.Domain.Products;

public class Product : Entity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}