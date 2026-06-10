using System.ComponentModel.DataAnnotations;
namespace UserApp.Web.ViewModels;

public class MilkViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Name is required")] 
    [StringLength(15, MinimumLength = 1, ErrorMessage = "Name length must be between 1 and 15")] 
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Price is required")] 
    [Range(5, 10000000, ErrorMessage = "Price must be between 5 and 10000000")] 
    public int Price { get; set; }


    public string? ImageUrl { get; set; }
    public Guid? MediaId { get; set; }

}
