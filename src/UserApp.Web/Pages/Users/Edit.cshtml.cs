using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UserApp.Application.Users;
using UserApp.Domain.Users;
using UserApp.Application.Users.DTOs;

namespace UserApp.Web.Pages.Users
{
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
            var user = await _svc.GetByIdAsync(id);
            if (user is null) return NotFound();

            Input = new InputModel
            {
                Id = user.Id,
                Email = user.Email.Value,
                FullName = user.FullName
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            try
            {
                var user = await _svc.GetByIdAsync(Input.Id);
                if (user is null) return NotFound();

                // Update properties
                user.UpdateProfile(Input.FullName);
                if (user.Email.Value != Input.Email)
                    user.ChangeEmail(UserApp.Domain.Users.Email.Create(Input.Email));

                await _svc.UpdateAsync(user);
                await _svc.SaveAsync();

                return RedirectToPage("Index");
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                return Page();
            }
        }
    }
}