using UserApp.Domain.Products;
using UserApp.Domain.Common;
using UserApp.Application.Common;
using UserApp.Application.Products.Interfaces;


namespace UserApp.Application.Products;

public class ProductService : BaseService<Product>, IProductService
{
    public ProductService(IProductRepository repo) : base(repo)
    {
    }


}