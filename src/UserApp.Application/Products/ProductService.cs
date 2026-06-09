using UserApp.Domain.Products;
using UserApp.Application.Common;
using UserApp.Application.Common.Interfaces;
using UserApp.Application.Products.Interfaces;

namespace UserApp.Application.Products;

public class ProductService : BaseService<Product>, IProductService
{
    public ProductService(
        IProductRepository repo,
        IMediaPipeline mediaPipeline)
        : base(repo, mediaPipeline)
    {
    }
}