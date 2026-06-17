using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using UserApp.Application.Media;
namespace UserApp.Web.ViewModels;

public class ProductViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Name is required")] 
    [StringLength(225, MinimumLength = 0, ErrorMessage = "Name length must be between 0 and 225")] 
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Price is required")] 
    public int Price { get; set; }

    [Required(ErrorMessage = "Description is required")] 
    [StringLength(500, MinimumLength = 0, ErrorMessage = "Description length must be between 0 and 500")] 
    public string Description { get; set; } = string.Empty;

    public int Quantity { get; set; }

    [Required(ErrorMessage = "Category is required")]
    public Guid CategoryId { get; set; }
    public List<SelectListItem> CategoryOptions { get; set; } = [];
    public string CategoryName { get; set; } = string.Empty;


    public List<string> ImageUrls { get; set; } = [];
    public List<MediaDto> MediaList { get; set; } = [];

}
