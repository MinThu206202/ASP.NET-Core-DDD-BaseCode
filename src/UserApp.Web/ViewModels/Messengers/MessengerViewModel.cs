using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using UserApp.Application.Media;
namespace UserApp.Web.ViewModels;

public class MessengerViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Milk is required")]
    public Guid MilkId { get; set; }
    public List<SelectListItem> MilkOptions { get; set; } = [];
    public string MilkName { get; set; } = string.Empty;

    [StringLength(225, MinimumLength = 0, ErrorMessage = "Name length must be between 0 and 225")] 
    public string Name { get; set; } = string.Empty;


    public List<string> ImageUrls { get; set; } = [];
    public List<MediaDto> MediaList { get; set; } = [];

}
