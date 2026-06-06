using UserApp.Domain.Products;
using UserApp.Application.Common;

namespace UserApp.Application.Products.Interfaces
{
    // This inherits your generic IBaseService
    public interface IProductService : IBaseService<Product>
    {
        // You can add product-specific methods here later
        // e.g. Task<List<Product>> GetProductsByCategoryAsync(Guid categoryId);
    }
}