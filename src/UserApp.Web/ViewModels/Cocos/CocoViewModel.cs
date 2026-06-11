using System.ComponentModel.DataAnnotations;
using UserApp.Application.Media;
namespace UserApp.Web.ViewModels;

public class CocoViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Name is required")] 
    [StringLength(15, MinimumLength = 2, ErrorMessage = "Name length must be between 2 and 15")] 
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Price is required")] 
    [Range(10, 1000000, ErrorMessage = "Price must be between 10 and 1000000")] 
    public int Price { get; set; }


    public List<string> ImageUrls { get; set; } = [];
    public List<MediaDto> MediaList { get; set; } = [];

}
