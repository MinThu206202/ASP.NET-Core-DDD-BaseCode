using System.ComponentModel.DataAnnotations.Schema;
using UserApp.Domain.Common;

namespace UserApp.Domain.Cars;

public class Car : Entity<Guid>, IHasMedia
{
    public string Name { get; set; } = string.Empty;
    public string Gender  { get; set; } = string.Empty;

}