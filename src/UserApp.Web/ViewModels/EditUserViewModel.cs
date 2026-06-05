using System.ComponentModel.DataAnnotations;

namespace UserApp.Web.ViewModels;

public class EditUserViewModel
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    [EmailAddress]
    public string? Email { get; set; }

    [Required]
    public string? FullName { get; set; }
}
