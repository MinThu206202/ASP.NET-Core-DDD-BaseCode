using UserApp.Domain.Common;

namespace UserApp.Domain.Categorys;

public class Category : Entity<Guid>, IHasMedia
{
    public string Name { get; set; } = string.Empty;

}