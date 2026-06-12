using UserApp.Application.Common;
using UserApp.Domain.Categorys;
using UserApp.Application.Categorys.Interfaces;

namespace UserApp.Application.Categorys;

public class CategoryService : BaseService<Category>, ICategoryService
{
    public CategoryService(ICategoryRepository repo) : base(repo)
    {
    }
}
