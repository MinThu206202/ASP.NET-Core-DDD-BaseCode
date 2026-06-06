using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UserApp.Application.Users;
using UserApp.Application.Users.DTOs;

namespace UserApp.Web.Pages.Users
{
    public class IndexModel : PageModel
    {
        private readonly UserService _svc;
        public IndexModel(UserService svc) => _svc = svc;

        public IReadOnlyList<UserDto> Users { get; private set; } = Array.Empty<UserDto>();
        [BindProperty(SupportsGet = true)] public int Page { get; set; } = 1;
        public int PageSize { get; } = 10;
        public int TotalPages { get; private set; }

        public async Task OnGetAsync()
        {
            var skip = (Page < 1 ? 0 : (Page - 1)) * PageSize;
            var users = await _svc.ListAsync(skip, PageSize);
            var total = await _svc.CountAsync();

            Users = users.ConvertAll(u => new UserDto(
                u.Id,
                u.Email.Value,
                u.FullName,
                u.Status.ToString(),
                u.CreatedAt
            ));

            TotalPages = (int)Math.Ceiling(total / (double)PageSize);
        }
    }
}