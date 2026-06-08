
using UserApp.Domain.Common;

namespace UserApp.Domain.Mobiles;

public class Mobile : Entity<Guid>
{

    public string Name { get; set; } = string.Empty;


    public int Price { get; set; }


    public int stock { get; set; }


}
