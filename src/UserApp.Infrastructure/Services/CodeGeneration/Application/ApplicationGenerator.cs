using System.Collections.Generic;
using System.IO;
using UserApp.Infrastructure.Services.CodeGeneration.Shared;

namespace UserApp.Infrastructure.Services.CodeGeneration;

public class ApplicationGenerator
{
    private readonly PathProvider _paths;
    private readonly FileManager _files;
    private readonly TemplateEngine _templates;

    public ApplicationGenerator(PathProvider paths, FileManager files, TemplateEngine templates)
    {
        _paths = paths;
        _files = files;
        _templates = templates;
    }

    public void Generate(string name)
    {
        var applicationFolder = Path.Combine(_paths.SrcRoot, "UserApp.Application", $"{name}s");
        var interfacesFolder = Path.Combine(applicationFolder, "Interfaces");

        _files.EnsureDirectory(applicationFolder);
        _files.EnsureDirectory(interfacesFolder);

        var serviceContent = _templates.RenderFile(
            new[] { "Application", "Templates", "Service.tpl" },
            new Dictionary<string, string>
            {
                ["Name"] = name
            });

        var interfaceContent = _templates.RenderFile(
            new[] { "Application", "Templates", "Interface.tpl" },
            new Dictionary<string, string>
            {
                ["Name"] = name
            });

        _files.WriteFile(Path.Combine(applicationFolder, $"{name}Service.cs"), serviceContent);
        _files.WriteFile(Path.Combine(interfacesFolder, $"I{name}Service.cs"), interfaceContent);
    }

}
