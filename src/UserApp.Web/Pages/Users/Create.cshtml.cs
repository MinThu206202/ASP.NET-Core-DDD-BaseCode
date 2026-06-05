using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UserApp.Application.Users;

namespace UserApp.Web.Pages.Users;

public class CreateModel : PageModel
{
    private readonly UserService _svc;
    public CreateModel(UserService svc) => _svc = svc;

    [BindProperty] public InputModel Input { get; set; } = new();
    public string? Error { get; private set; }

    public class InputModel
    {
        [Required, EmailAddress] public string Email { get; set; } = "";
        [Required, StringLength(200)] public string FullName { get; set; } = "";
        [Required, StringLength(100, MinimumLength = 6)] public string Password { get; set; } = "";
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        try
        {
            await _svc.CreateAsync(Input.Email, Input.FullName, Input.Password);
            return RedirectToPage("Index");
        }
        catch (Exception ex) { Error = ex.Message; return Page(); }
    }
}
