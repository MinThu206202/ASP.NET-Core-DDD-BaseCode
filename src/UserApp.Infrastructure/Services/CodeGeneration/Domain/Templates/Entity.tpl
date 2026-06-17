using System.ComponentModel.DataAnnotations.Schema;
using UserApp.Domain.Common;
{{NavigationUsings}}
namespace UserApp.Domain.{{Name}}s;

public class {{Name}} : Entity<Guid>{{HasImageInterface}}
{
{{Properties}}
}