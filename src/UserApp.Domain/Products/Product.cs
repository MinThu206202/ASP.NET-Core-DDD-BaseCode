using System.ComponentModel.DataAnnotations.Schema;
using UserApp.Domain.Common;
using UserApp.Domain.Categorys;

namespace UserApp.Domain.Products;

public class Product : Entity<Guid>, IHasMedia
{
    public string Name { get; set; } = string.Empty;
    public int Price { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    [Column("Category_id")]
    public Guid CategoryId { get; set; }
    public Category Category { get; set; }

}