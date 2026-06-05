using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UserApp.Application.Users;

namespace UserApp.Web.Pages.Users;

public class EditModel : PageModel
{
    private readonly UserService _svc;
    public EditModel(UserService svc) => _svc = svc;

    [BindProperty] public InputModel Input { get; set; } = new();
    public string? Error { get; private set; }

    public class InputModel
    {
        public Guid Id { get; set; }
        [Required, EmailAddress] public string Email { get; set; } = "";
        [Required, StringLength(200)] public string FullName { get; set; } = "";
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var u = await _svc.GetAsync(id);
        if (u is null) return NotFound();
        Input = new InputModel { Id = u.Id, Email = u.Email, FullName = u.FullName };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        try
        {
            await _svc.UpdateAsync(Input.Id, Input.FullName, Input.Email);
            return RedirectToPage("Index");
        }
        catch (Exception ex) { Error = ex.Message; return Page(); }
    }
}
