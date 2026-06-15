namespace UserApp.Web.ViewModels.ModuleGenerator;

public class GenerateModuleViewModel
{
    public string ModuleName { get; set; } = string.Empty;

    public bool RunMigration { get; set; }   // NEW
    public bool RunDbUpdate { get; set; }    // NEW
    public bool HasImage { get; set; }       // NEW

    public List<ModuleFieldViewModel> Fields { get; set; } = new();
}