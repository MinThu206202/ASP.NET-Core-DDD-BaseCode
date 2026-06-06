using System.ComponentModel.DataAnnotations;

namespace UserApp.Web.ViewModels;

public class LoginViewModel
{
    [Required]
    public string Email { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";
}