using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UserApp.Application.Users;
using UserApp.Application.Users.DTOs;
using UserApp.Domain.Users;

namespace UserApp.Web.Pages.Users
{
    public class DeleteModel : PageModel
    {
        private readonly UserService _svc;
        public DeleteModel(UserService svc) => _svc = svc;

        public UserDto? User { get; private set; }
        [BindProperty(SupportsGet = true)] public Guid Id { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Use BaseService method
            var entity = await _svc.GetByIdAsync(Id);
            if (entity is null) return NotFound();

            User = new UserDto(
                entity.Id,
                entity.Email.Value,
                entity.FullName,
                entity.Status.ToString(),
                entity.CreatedAt
            );
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var entity = await _svc.GetByIdAsync(Id);
            if (entity is null) return NotFound();

            await _svc.RemoveAsync(entity);
            await _svc.SaveAsync();

            return RedirectToPage("Index");
        }
    }
}