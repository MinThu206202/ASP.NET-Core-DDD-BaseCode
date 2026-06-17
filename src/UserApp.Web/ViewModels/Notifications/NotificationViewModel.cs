using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using UserApp.Application.Media;
namespace UserApp.Web.ViewModels;

public class NotificationViewModel
{
    public Guid Id { get; set; }

    [StringLength(255, MinimumLength = 0, ErrorMessage = "Name length must be between 0 and 255")] 
    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;


    public List<string> ImageUrls { get; set; } = [];
    public List<MediaDto> MediaList { get; set; } = [];

}
