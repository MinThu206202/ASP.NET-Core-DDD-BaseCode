using System.ComponentModel.DataAnnotations;
using UserApp.Application.Media;
namespace UserApp.Web.ViewModels;

public class HumanViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "SystemCode is required")]
    public string SystemCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Name is required")] 
    [StringLength(225, MinimumLength = 0, ErrorMessage = "Name length must be between 0 and 225")] 
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Age is required")] 
    public int Age { get; set; }

    [Required(ErrorMessage = "Email is required")] 
    [StringLength(225, MinimumLength = 0, ErrorMessage = "Email length must be between 0 and 225")] 
    public string Email { get; set; } = string.Empty;

    public string Gender { get; set; } = string.Empty;


}
