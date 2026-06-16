using System.Collections.Generic;
using System.IO;
using UserApp.Infrastructure.Services.CodeGeneration.Shared;

namespace UserApp.Infrastructure.Services.CodeGeneration;

public class InfrastructureGenerator
{
    private readonly PathProvider _paths;
    private readonly FileManager _files;
    private readonly TemplateEngine _templates;

    public InfrastructureGenerator(PathProvider paths, FileManager files, TemplateEngine templates)
    {
        _paths = paths;
        _files = files;
        _templates = templates;
    }

    public void Generate(string name)
    {
        var repositoryFolder = Path.Combine(_paths.SrcRoot, "UserApp.Infrastructure", "Persistence", "Repositories");
        _files.EnsureDirectory(repositoryFolder);

        var repositoryContent = _templates.RenderFile(
            new[] { "Infrastructure", "Templates", "Repository.tpl" },
            new Dictionary<string, string>
            {
                ["Name"] = name
            });

        _files.WriteFile(Path.Combine(repositoryFolder, $"{name}Repository.cs"), repositoryContent);
    }

}
