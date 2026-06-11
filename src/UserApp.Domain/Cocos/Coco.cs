using UserApp.Domain.Common;

namespace UserApp.Domain.Cocos;

public class Coco : Entity<Guid>, IHasMedia
{
    public string Name { get; set; } = string.Empty;
    public int Price { get; set; }

}