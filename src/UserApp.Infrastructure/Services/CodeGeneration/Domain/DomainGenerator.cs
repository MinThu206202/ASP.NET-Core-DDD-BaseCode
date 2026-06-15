using System.Text;
using UserApp.Application.Common.DTOs;
using UserApp.Infrastructure.Services.CodeGeneration.Shared;

namespace UserApp.Infrastructure.Services.CodeGeneration;

public class DomainGenerator
{
    private readonly PathProvider _paths;
    private readonly FileManager _files;
    private readonly TemplateEngine _templates;

    public DomainGenerator(PathProvider paths, FileManager files, TemplateEngine templates)
    {
        _paths = paths;
        _files = files;
        _templates = templates;
    }

    public void Generate(string name, List<ModuleFieldDto> fields, bool hasImage)
    {
        var domainFolder = Path.Combine(_paths.SrcRoot, "UserApp.Domain", $"{name}s");
        _files.EnsureDirectory(domainFolder);

        var entityContent = _templates.RenderFile(
            new[] { "Domain", "Templates", "Entity.tpl" },
            new Dictionary<string, string>
            {
                ["Name"] = name,
                ["Properties"] = GenerateProperties(fields),
                ["HasImageInterface"] = hasImage ? ", IHasMedia" : ""
            });

        _files.WriteFile(Path.Combine(domainFolder, $"{name}.cs"), entityContent);

        var repositoryContent = _templates.RenderFile(
            new[] { "Domain", "Templates", "Repository.tpl" },
            new Dictionary<string, string>
            {
                ["Name"] = name
            });

        _files.WriteFile(Path.Combine(domainFolder, $"I{name}Repository.cs"), repositoryContent);
    }

    private static string GenerateProperties(List<ModuleFieldDto> fields)
    {
        var sb = new StringBuilder();

        foreach (var field in fields)
        {
            if (field.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                continue;

            var nullable = field.IsNullable && field.Type != "string" ? "?" : "";

            if (field.Type == "enum")
            {
                sb.AppendLine(
                    $"    public string {field.Name} {{ get; set; }} = string.Empty;");
            }
            else if (field.Type == "string")
            {
                sb.AppendLine(
                    $"    public string {field.Name} {{ get; set; }} = string.Empty;");
            }
            else if (field.IsRelation)
            {
                sb.AppendLine(
                    $"    public Guid {field.Name}Id {{ get; set; }}");
            }
            else
            {
                sb.AppendLine($"    public {field.Type}{nullable} {field.Name} {{ get; set; }}");
            }
        }

        return sb.ToString();
    }


}