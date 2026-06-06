using System.ComponentModel.DataAnnotations;

namespace UserApp.Web.ViewModels;

public class CreateUserViewModel
{
    [Required]
    [EmailAddress]
    public string? Email { get; set; }

    [Required]
    public string? FullName { get; set; }

    [Required]
    [DataType(DataType.Password)]
    public string? Password { get; set; }
}
