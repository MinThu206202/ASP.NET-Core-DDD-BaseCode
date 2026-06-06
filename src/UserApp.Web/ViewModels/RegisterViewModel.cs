using System.ComponentModel.DataAnnotations;

namespace UserApp.Web.ViewModels;

public class RegisterViewModel
{
    [Required]
    public string Email { get; set; } = "";

    [Required]
    public string FullName { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";
}