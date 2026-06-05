using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UserApp.Application.Users;
using UserApp.Application.Users.DTOs;

namespace UserApp.Web.Pages.Users;

public class DeleteModel : PageModel
{
    private readonly UserService _svc;
    public DeleteModel(UserService svc) => _svc = svc;

    public UserDto? User { get; private set; }
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        User = await _svc.GetAsync(Id);
        return User is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await _svc.DeleteAsync(Id);
        return RedirectToPage("Index");
    }
}
