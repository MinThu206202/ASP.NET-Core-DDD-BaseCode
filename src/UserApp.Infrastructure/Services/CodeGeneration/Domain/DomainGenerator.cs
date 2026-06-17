using System.Linq;
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

        var navigationUsings = BuildNavigationUsings(fields);

        var entityContent = _templates.RenderFile(
            new[] { "Domain", "Templates", "Entity.tpl" },
            new Dictionary<string, string>
            {
                ["Name"] = name,
                ["Properties"] = GenerateProperties(fields),
                ["HasImageInterface"] = hasImage ? ", IHasMedia" : "",
                ["NavigationUsings"] = navigationUsings
            });

        _files.WriteFile(Path.Combine(domainFolder, $"{name}.cs"), entityContent);

        var repositoryContent = _templates.RenderFile(
            new[] { "Domain", "Templates", "Repository.tpl" },
            new Dictionary<string, string>
            {
                ["Name"] = name
            });

        _files.WriteFile(Path.Combine(domainFolder, $"I{name}Repository.cs"), repositoryContent);

        // Generate configuration if there are non-pivot relation fields
        var relationFields = fields.Where(f => f.IsRelation && !f.IsPivot).ToList();
        if (relationFields.Count > 0)
        {
            GenerateConfiguration(name, relationFields);
        }
    }

    private static string BuildNavigationUsings(List<ModuleFieldDto> fields)
    {
        var sb = new StringBuilder();
        foreach (var field in fields.Where(f => f.IsRelation && !f.IsPivot && !string.IsNullOrWhiteSpace(f.RelatedEntityName)))
        {
            sb.AppendLine($"using UserApp.Domain.{field.RelatedEntityName}s;");
        }
        return sb.ToString();
    }

    public void GenerateConfiguration(string entityName, List<ModuleFieldDto> relationFields)
    {
        var configFolder = Path.Combine(_paths.SrcRoot, "UserApp.Infrastructure", "Persistence", "Configurations");
        _files.EnsureDirectory(configFolder);

        var sb = new StringBuilder();
        sb.AppendLine($@"using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserApp.Domain.{entityName}s;

namespace UserApp.Infrastructure.Persistence.Configurations;

public class {entityName}Configuration : IEntityTypeConfiguration<{entityName}>
{{
    public void Configure(EntityTypeBuilder<{entityName}> b)
    {{");

        foreach (var field in relationFields)
        {
            var behavior = GetDeleteBehavior(field.DeleteBehavior);
            sb.AppendLine($@"
        b.HasOne(x => x.{field.Name})
            .WithMany()
            .HasForeignKey(x => x.{field.Name}Id)
            .OnDelete(DeleteBehavior.{behavior});");
        }

        sb.AppendLine(@"    }
}");

        _files.WriteFile(Path.Combine(configFolder, $"{entityName}Configuration.cs"), sb.ToString());
    }

    private static string GetDeleteBehavior(string? deleteBehavior)
    {
        return deleteBehavior switch
        {
            "Restrict" => "Restrict",
            "SetNull" => "SetNull",
            _ => "Cascade"
        };
    }

    public void GeneratePivot(string moduleName, string relatedEntityName)
    {
        var pivotName = $"{moduleName}{relatedEntityName}";
        var domainFolder = Path.Combine(_paths.SrcRoot, "UserApp.Domain", $"{pivotName}s");
        _files.EnsureDirectory(domainFolder);

        var entity = $@"using System.ComponentModel.DataAnnotations.Schema;
using UserApp.Domain.Common;
using UserApp.Domain.{moduleName}s;
using UserApp.Domain.{relatedEntityName}s;

namespace UserApp.Domain.{pivotName}s;

[Table(""{moduleName}_{relatedEntityName}"")]
public class {pivotName} : Entity<Guid>
{{
    [Column(""{moduleName}_id"")]
    public Guid {moduleName}Id {{ get; set; }}
    public {moduleName} {moduleName} {{ get; set; }}

    [Column(""{relatedEntityName}_id"")]
    public Guid {relatedEntityName}Id {{ get; set; }}
    public {relatedEntityName} {relatedEntityName} {{ get; set; }}
}}";
        _files.WriteFile(Path.Combine(domainFolder, $"{pivotName}.cs"), entity);
    }

    private static string GenerateProperties(List<ModuleFieldDto> fields)
    {
        var sb = new StringBuilder();

        foreach (var field in fields)
        {
            if (field.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                continue;

            // Pivot fields don't generate entity properties — the FK lives in the pivot table
            if (field.IsPivot)
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
            else if (field.IsRelation && !field.IsPivot)
            {
                var fkNullable = field.DeleteBehavior == "SetNull" ? "?" : "";
                sb.AppendLine(
                    $"    [Column(\"{field.Name}_id\")]");
                sb.AppendLine(
                    $"    public Guid{fkNullable} {field.Name}Id {{ get; set; }}");
                sb.AppendLine(
                    $"    public {field.RelatedEntityName} {field.Name} {{ get; set; }}");
            }
            else
            {
                sb.AppendLine($"    public {field.Type}{nullable} {field.Name} {{ get; set; }}");
            }
        }

        return sb.ToString();
    }


}