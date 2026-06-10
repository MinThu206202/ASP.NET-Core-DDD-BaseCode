using System.ComponentModel.DataAnnotations;
namespace UserApp.Web.ViewModels;

public class PapViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Name is required")] 
    [StringLength(10, MinimumLength = 1, ErrorMessage = "Name length must be between 1 and 10")] 
    public string Name { get; set; } = string.Empty;


    public string? ImageUrl { get; set; }
    public Guid? MediaId { get; set; }

}
