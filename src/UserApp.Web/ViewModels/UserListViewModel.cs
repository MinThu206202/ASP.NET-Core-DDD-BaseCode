namespace UserApp.Web.ViewModels;

public class UserListViewModel
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public IReadOnlyList<UserViewModel> Users { get; set; } = Array.Empty<UserViewModel>();

    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
