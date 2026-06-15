using System.Collections.Generic;
using System.IO;
using System.Text;
using UserApp.Application.Common.DTOs;
using UserApp.Infrastructure.Services.CodeGeneration.Shared;

namespace UserApp.Infrastructure.Services.CodeGeneration;

public class WebGenerator
{
    private readonly PathProvider _paths;
    private readonly FileManager _files;
    private readonly TemplateEngine _templates;
    private readonly ViewGenerator _views;


    public WebGenerator(PathProvider paths, FileManager files, TemplateEngine templates)
    {
        _paths = paths;
        _files = files;
        _templates = templates;
        _views = new ViewGenerator(_paths, _files, _templates);
    }

    public void Generate(string name, List<ModuleFieldDto> fields, bool hasImage)
    {
        var controllersFolder = Path.Combine(_paths.SrcRoot, "UserApp.Web", "Controllers");
        var apiControllersFolder = Path.Combine(controllersFolder, "Api");
        var viewModelsFolder = Path.Combine(_paths.SrcRoot, "UserApp.Web", "ViewModels", $"{name}s");

        _files.EnsureDirectory(controllersFolder);
        _files.EnsureDirectory(apiControllersFolder);
        _files.EnsureDirectory(viewModelsFolder);

        var controllerContent = _templates.RenderFile(
            new[] { "Web", "Templates", "Controller.tpl" },
            new Dictionary<string, string>
            {
                ["Name"] = name
            });

        var apiControllerContent = _templates.RenderFile(
            new[] { "Web", "Templates", "ApiController.tpl" },
            new Dictionary<string, string>
            {
                ["Name"] = name
            });

        var viewModelContent = _templates.RenderFile(
            new[] { "Web", "Templates", "ViewModel.tpl" },
            new Dictionary<string, string>
            {
                ["Name"] = name,
                ["Properties"] = BuildViewModelProperties(fields, hasImage)
            });

        _files.WriteFile(Path.Combine(controllersFolder, $"{name}Controller.cs"), controllerContent);
        _files.WriteFile(Path.Combine(apiControllersFolder, $"{name}ApiController.cs"), apiControllerContent);
        _files.WriteFile(Path.Combine(viewModelsFolder, $"{name}ViewModel.cs"), viewModelContent);

        _views.GenerateViews(name, fields, hasImage);
    }

    private static string BuildViewModelProperties(List<ModuleFieldDto> fields, bool hasImage)
    {
        var sb = new StringBuilder();


        foreach (var field in fields)
        {
            if (field.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                continue;

            if (field.IsRequired)
                sb.AppendLine($"    [Required(ErrorMessage = \"{field.Name} is required\")] ");

            if (IsStringType(field.Type) && HasStringLengthValidation(field))
            {
                var minLength = field.MinLength ?? ToLength(field.MinValue) ?? 0;
                var maxLength = field.MaxLength ?? ToLength(field.MaxValue) ?? field.Length ?? 500;

                sb.AppendLine(
                    "    [StringLength(" + maxLength + ", " +
                    "MinimumLength = " + minLength + ", " +
                    "ErrorMessage = \"" + field.Name + " length must be between " +
                    minLength + " and " + maxLength + "\")] ");
            }

            if (IsNumericType(field.Type) && (field.MinValue.HasValue || field.MaxValue.HasValue))
            {
                var minValue = FormatDecimal(field.MinValue ?? 0);
                var maxValue = FormatDecimal(field.MaxValue ?? 999999999);

                sb.AppendLine(
                    "    [Range(" + minValue + ", " +
                    maxValue + ", " +
                    "ErrorMessage = \"" + field.Name + " must be between " +
                    minValue + " and " + maxValue + "\")] ");
            }

            var type = field.Type;
            var name = field.Name;

            if (IsStringType(type) ||
                type.Equals("enum",
                StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine(
                    $"    public string {name} {{ get; set; }} = string.Empty;");
            }
            else if (field.IsRelation)
            {
                sb.AppendLine(
                    $"    [Required(ErrorMessage = \"{name} is required\")]");
                sb.AppendLine(
                    $"    public Guid {name}Id {{ get; set; }}");
                sb.AppendLine(
                    $"    public List<SelectListItem> {name}Options {{ get; set; }} = [];");
                sb.AppendLine(
                    $"    public string {name}Name {{ get; set; }} = string.Empty;");
            }
            else
            {
                var nullable = field.IsNullable ? "?" : string.Empty;
                sb.AppendLine($"    public {type}{nullable} {name} {{ get; set; }}");
            }

            if (field.UseCommonTable)
            {
                sb.AppendLine(
                    $"    public List<SelectListItem> {name}Options {{ get; set; }} = [];");
                sb.AppendLine(
                    $"    public string {name}Name {{ get; set; }} = string.Empty;");
            }

            sb.AppendLine();
        }

        // ✅ ADD IMAGE FIELDS (from Media table, not DB column)
        if (hasImage)
        {
            sb.AppendLine();
            sb.AppendLine("    public List<string> ImageUrls { get; set; } = [];");
            sb.AppendLine("    public List<MediaDto> MediaList { get; set; } = [];");
        }

        return sb.ToString();
    }

    private static bool IsStringType(string type)
        => type.Equals("string", StringComparison.OrdinalIgnoreCase);

    private static bool IsNumericType(string type)
        => type.Equals("int", StringComparison.OrdinalIgnoreCase)
            || type.Equals("decimal", StringComparison.OrdinalIgnoreCase)
            || type.Equals("double", StringComparison.OrdinalIgnoreCase)
            || type.Equals("float", StringComparison.OrdinalIgnoreCase)
            || type.Equals("long", StringComparison.OrdinalIgnoreCase);

    private static bool IsBooleanType(string type)
        => type.Equals("bool", StringComparison.OrdinalIgnoreCase);

    private static bool HasStringLengthValidation(ModuleFieldDto field)
        => field.MinLength.HasValue
            || field.MaxLength.HasValue
            || field.Length.HasValue
            || field.MinValue.HasValue
            || field.MaxValue.HasValue;

    private static int? ToLength(decimal? value)
        => value.HasValue ? (int)value.Value : null;

    private static string FormatDecimal(decimal value)
        => value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
