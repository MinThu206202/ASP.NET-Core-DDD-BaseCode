namespace UserApp.Web.ViewModels.ModuleGenerator;

public class GenerateModuleViewModel
{
    public string ModuleName { get; set; } = string.Empty;

    public bool RunMigration { get; set; }
    public bool RunDbUpdate { get; set; }
    public bool HasImage { get; set; }
    public bool HasRelation { get; set; }

    public List<ModuleFieldViewModel> Fields { get; set; } = new();
    public List<string> AvailableTables { get; set; } = new();
}