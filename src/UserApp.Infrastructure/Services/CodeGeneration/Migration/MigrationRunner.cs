using UserApp.Infrastructure.Services.CodeGeneration.Shared;

namespace UserApp.Infrastructure.Services.CodeGeneration;

public class MigrationRunner
{
    private readonly PathProvider _paths;

    public MigrationRunner(PathProvider paths)
    {
        _paths = paths;
    }

    public void AddMigration(string name)
    {
        var migrationName = $"{name}_Auto";
        CommandRunner.Run("dotnet", $"ef migrations add {migrationName} --project {_paths.InfrastructureProject} --startup-project {_paths.WebProject}");
    }

    public void UpdateDatabase()
    {
        CommandRunner.Run("dotnet", $"ef database update --project {_paths.InfrastructureProject} --startup-project {_paths.WebProject}");
    }
}
