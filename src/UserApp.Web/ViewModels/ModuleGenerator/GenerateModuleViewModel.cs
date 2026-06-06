using System.ComponentModel.DataAnnotations;

namespace UserApp.Web.ViewModels.ModuleGenerator;

public class GenerateModuleViewModel
{
    [Required]
    public string ModuleName { get; set; } = string.Empty;
}