using System.ComponentModel.DataAnnotations;
using UserApp.Application.Media;
namespace UserApp.Web.ViewModels;

public class AiViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Name is required")] 
    [StringLength(10, MinimumLength = 1, ErrorMessage = "Name length must be between 1 and 10")] 
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "price is required")] 
    [Range(1, 10000000, ErrorMessage = "price must be between 1 and 10000000")] 
    public int price { get; set; }

    [Required(ErrorMessage = "stock is required")] 
    [Range(1, 10009000, ErrorMessage = "stock must be between 1 and 10009000")] 
    public decimal stock { get; set; }


    public List<string> ImageUrls { get; set; } = [];
    public List<MediaDto> MediaList { get; set; } = [];

}
