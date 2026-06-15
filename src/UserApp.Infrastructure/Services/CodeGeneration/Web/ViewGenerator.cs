using System.Text;
using UserApp.Application.Common.DTOs;
using UserApp.Infrastructure.Services.CodeGeneration.Shared;

namespace UserApp.Infrastructure.Services.CodeGeneration;

public class ViewGenerator
{
    private readonly PathProvider _paths;
    private readonly FileManager _files;
    private readonly TemplateEngine _templates;

    public ViewGenerator(PathProvider paths, FileManager files, TemplateEngine templates)
    {
        _paths = paths;
        _files = files;
        _templates = templates;
    }

    public void GenerateViews(string name, List<ModuleFieldDto> fields, bool hasImage)
    {
        var viewFolder = Path.Combine(_paths.SrcRoot, "UserApp.Web", "Views", name);
        _files.EnsureDirectory(viewFolder);

        var indexContent = _templates.RenderFile(
            new[] { "Web", "Templates", "Index.tpl" },
            new Dictionary<string, string>
            {
                ["Name"] = name,
                ["Columns"] = BuildTableColumns(fields, hasImage),
                ["Rows"] = BuildTableRows(fields, hasImage)
            });

        var createContent = _templates.RenderFile(
            new[] { "Web", "Templates", "Create.tpl" },
            new Dictionary<string, string>
            {
                ["Name"] = name,
                ["Inputs"] = BuildFormInputs(fields, hasImage)
            });

        var editContent = _templates.RenderFile(
            new[] { "Web", "Templates", "Edit.tpl" },
            new Dictionary<string, string>
            {
                ["Name"] = name,
                ["CurrentImages"] = BuildCurrentImages(hasImage),
                ["Inputs"] = BuildFormInputs(fields, hasImage),
                ["Scripts"] = BuildMediaScripts(hasImage)
            });

        _files.WriteFile(Path.Combine(viewFolder, "Index.cshtml"), indexContent);
        _files.WriteFile(Path.Combine(viewFolder, "Create.cshtml"), createContent);
        _files.WriteFile(Path.Combine(viewFolder, "Edit.cshtml"), editContent);
    }

    // =========================
    // ENUM CHECK
    // =========================
    private static bool IsEnumType(string type)
        => type.Equals("enum", StringComparison.OrdinalIgnoreCase);

    private static bool IsBooleanType(string type)
        => type.Equals("bool", StringComparison.OrdinalIgnoreCase);

    // =========================
    // TABLE
    // =========================
    private static string BuildTableColumns(List<ModuleFieldDto> fields, bool hasImage)
    {
        var sb = new StringBuilder();

        foreach (var field in fields)
        {
            if (field.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                continue;
            if (field.Name.Equals("Name", StringComparison.OrdinalIgnoreCase))
                continue;

            sb.AppendLine($@"<th class=""text-left px-6 py-4 text-xs font-bold text-slate-500 uppercase"">
                {field.Name}
            </th>");
        }

        if (hasImage)
            sb.AppendLine(@"<th class=""text-left px-6 py-4 text-xs font-bold text-slate-500 uppercase"">Images</th>");

        return sb.ToString();
    }

    private static string BuildTableRows(List<ModuleFieldDto> fields, bool hasImage)
    {
        var sb = new StringBuilder();

        foreach (var field in fields)
        {
            if (field.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                continue;
            if (field.Name.Equals("Name", StringComparison.OrdinalIgnoreCase))
                continue;

            var displayValue = field.IsRelation
                ? $"@p.{field.Name}Name"
                : field.UseCommonTable
                    ? $"@p.{field.Name}Name"
                    : $"@p.{field.Name}";

            sb.AppendLine($@"<td class=""px-6 py-4 text-slate-600"">{displayValue}</td>");
        }

        if (hasImage)
        {
            sb.AppendLine(@"
<td class=""px-6 py-4"">
    @if (p.ImageUrls.Count > 0)
    {
        <div class=""flex gap-1.5"">
        @foreach (var img in p.ImageUrls.Take(3))
        {
            <img src=""@img"" class=""w-10 h-10 rounded-lg object-cover"" />
        }
        </div>
    }
</td>");
        }

        return sb.ToString();
    }

    // =========================
    // FORM INPUTS (CREATE + EDIT)
    // =========================
    private static string BuildFormInputs(List<ModuleFieldDto> fields, bool hasImage)
    {
        var sb = new StringBuilder();

        foreach (var field in fields)
        {
            if (field.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                continue;

            // =========================
            // RELATION → DROPDOWN (only when type is "relation")
            // =========================
            if (field.IsRelation && field.Type.Equals("relation", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine($@"
<div>
    <label asp-for=""{field.Name}Id"" class=""block text-sm font-bold text-slate-700 mb-1.5"">
        {field.Name}
    </label>

    <select asp-for=""{field.Name}Id"" asp-items=""Model.{field.Name}Options""
            class=""w-full px-4 py-2.5 rounded-xl border border-slate-200 bg-slate-50"">
        <option value="""">-- Select {field.Name} --</option>
    </select>

    <span asp-validation-for=""{field.Name}Id"" class=""text-xs text-rose-500 mt-1""></span>
</div>");
            }
            // =========================
            // ENUM FIELD
            // =========================
            else if (IsEnumType(field.Type))
            {
                if (field.UseCommonTable)
                {
                    // CommonTable → dropdown with asp-items
                    sb.AppendLine($@"
<div>
    <label asp-for=""{field.Name}"" class=""block text-sm font-bold text-slate-700 mb-1.5"">
        {field.Name}
    </label>

    <select asp-for=""{field.Name}"" asp-items=""Model.{field.Name}Options""
            class=""w-full px-4 py-2.5 rounded-xl border border-slate-200 bg-slate-50"">
        <option value="""">-- Select {field.Name} --</option>
    </select>

    <span asp-validation-for=""{field.Name}"" class=""text-xs text-rose-500 mt-1""></span>
</div>");
                }
                else
                {
                    // Hardcoded options → radio buttons
                    sb.AppendLine($@"
<div>
    <label class=""block text-sm font-bold text-slate-700 mb-1.5"">{field.Name}</label>
    <div class=""flex flex-wrap gap-4"">");

                    if (!string.IsNullOrWhiteSpace(field.EnumValues))
                    {
                        var values = field.EnumValues.Split(',', System.StringSplitOptions.TrimEntries | System.StringSplitOptions.RemoveEmptyEntries);
                        foreach (var val in values)
                        {
                            var code = val.Trim().Replace(' ', '_').ToUpperInvariant();
                            sb.AppendLine($@"
        <label class=""inline-flex items-center gap-2 cursor-pointer"">
            <input type=""radio"" asp-for=""{field.Name}"" value=""{code}"" class=""text-indigo-600"" />
            <span class=""text-sm text-slate-700"">{val}</span>
        </label>");
                        }
                    }

                    sb.AppendLine($@"
    </div>
    <span asp-validation-for=""{field.Name}"" class=""text-xs text-rose-500 mt-1""></span>
</div>");
                }
            }
            else if (IsBooleanType(field.Type))
            {
                // =========================
                // BOOLEAN → SINGLE CHECKBOX
                // =========================
                sb.AppendLine($@"
<div>
    <label class=""inline-flex items-center gap-2.5 cursor-pointer"">
        <input type=""checkbox"" asp-for=""{field.Name}""
               class=""w-4 h-4 rounded border-slate-300 text-indigo-600 focus:ring-indigo-500"" />
        <span class=""text-sm font-medium text-slate-700"">{field.Name}</span>
    </label>
    <span asp-validation-for=""{field.Name}"" class=""text-xs text-rose-500 mt-1""></span>
</div>");
            }
            else
            {
                // =========================
                // NORMAL INPUT
                // =========================
                sb.AppendLine($@"
<div>
    <label asp-for=""{field.Name}"" class=""block text-sm font-bold text-slate-700 mb-1.5"">
        {field.Name}
    </label>

    <input asp-for=""{field.Name}""
           class=""w-full px-4 py-2.5 rounded-xl border border-slate-200 bg-slate-50"" />

    <span asp-validation-for=""{field.Name}"" class=""text-xs text-rose-500 mt-1""></span>
</div>");
            }
        }

        // IMAGE FIELD
        if (hasImage)
        {
            sb.AppendLine(@"
<div>
    <label class=""block text-sm font-bold text-slate-700 mb-1.5"">Images</label>
    <input type=""file"" name=""files"" multiple class=""w-full"" />
    @Html.ValidationMessage(""files"", new { @class = ""text-xs text-rose-500 mt-1"" })
</div>");
        }

        return sb.ToString();
    }

    // =========================
    // EDIT: CURRENT IMAGES
    // =========================
    private static string BuildCurrentImages(bool hasImage)
    {
        if (!hasImage) return string.Empty;

        return @"
@if (Model.MediaList.Count > 0)
{
    <div>
        <label class=""block text-sm font-bold"">Current Images</label>

        @foreach (var media in Model.MediaList)
        {
            <img src=""@media.Url"" class=""w-20 h-20"" />
        }
    </div>
}";
    }

    // =========================
    // SCRIPTS
    // =========================
    private static string BuildMediaScripts(bool hasImage)
    {
        if (!hasImage) return string.Empty;

        return @"
<script>
function updateFileLabel(input) {
    console.log(input.files.length);
}
</script>";
    }
}