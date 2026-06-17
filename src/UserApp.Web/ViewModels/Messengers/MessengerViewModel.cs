using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using UserApp.Application.Media;
namespace UserApp.Web.ViewModels;

public class MessengerViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Name is required")] 
    [StringLength(225, MinimumLength = 0, ErrorMessage = "Name length must be between 0 and 225")] 
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Status is required")] 
    public string Status { get; set; } = string.Empty;
    public List<SelectListItem> StatusOptions { get; set; } = [];
    public string StatusName { get; set; } = string.Empty;


}
