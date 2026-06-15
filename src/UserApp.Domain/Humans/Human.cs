using UserApp.Domain.Common;

namespace UserApp.Domain.Humans;

public class Human : Entity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string SystemCode { get; set; } = "HUMAN";
}