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

        var hasPivot = fields.Any(f => f.IsPivot);
        var hasCheckbox = fields.Any(f => f.EnumRenderAsCheckbox);

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
                ["Inputs"] = BuildFormInputs(fields, hasImage),
                ["Scripts"] = BuildCheckboxScripts(hasCheckbox) + BuildPivotScripts(hasPivot)
            });

        var editContent = _templates.RenderFile(
            new[] { "Web", "Templates", "Edit.tpl" },
            new Dictionary<string, string>
            {
                ["Name"] = name,
                ["CurrentImages"] = BuildCurrentImages(hasImage),
                ["Inputs"] = BuildFormInputs(fields, hasImage),
                ["Scripts"] = BuildMediaScripts(hasImage) + BuildCheckboxScripts(hasCheckbox) + BuildPivotScripts(hasPivot)
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

            var displayValue = field.IsPivot
                ? $"@(!string.IsNullOrEmpty(p.{field.Name}Display) ? p.{field.Name}Display : \"None\")"
                : field.IsRelation
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
            // RELATION → DROPDOWN or PIVOT CHECKBOXES
            // =========================
            if (field.IsRelation && field.Type.Equals("relation", StringComparison.OrdinalIgnoreCase))
            {
                if (field.IsPivot)
                {
                    sb.AppendLine($@"
<div>
    <label class=""block text-sm font-bold text-slate-700 mb-1.5"">{field.Name}</label>
    <div class=""relative"">
        <button type=""button"" onclick=""togglePivotDropdown(this)"" data-label=""{field.Name}""
                class=""w-full px-4 py-2.5 rounded-xl border border-slate-200 bg-slate-50 text-left text-sm text-slate-500 flex items-center justify-between gap-2"">
            <span class=""truncate"">Select {field.Name}</span>
            <svg class=""w-4 h-4 shrink-0 text-slate-400"" fill=""none"" stroke=""currentColor"" viewBox=""0 0 24 24"">
                <path stroke-linecap=""round"" stroke-linejoin=""round"" stroke-width=""2"" d=""M19 9l-7 7-7-7""/>
            </svg>
        </button>
        <div class=""hidden absolute z-10 mt-1 w-full bg-white border border-slate-200 rounded-xl shadow-lg max-h-48 overflow-y-auto"">
            @foreach (var option in Model.{field.Name}Options)
            {{
                <label class=""flex items-center px-3 py-2 hover:bg-slate-50 cursor-pointer"">
                    <input type=""checkbox"" name=""Selected{field.Name}Ids"" value=""@option.Value""
                           class=""w-4 h-4 rounded border-slate-300 text-indigo-600 focus:ring-indigo-500""
                           onchange=""updatePivotLabel(this)""
                           @(Model.Selected{field.Name}Ids.Contains(Guid.Parse(option.Value)) ? ""checked"" : """") />
                    <span class=""ml-2.5 text-sm text-slate-700"">@option.Text</span>
                </label>
            }}
        </div>
    </div>
</div>");
                }
                else
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
            }
            // =========================
            // ENUM FIELD (radio/checkbox)
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
                else if (field.EnumRenderAsCheckbox)
                {
                    // Checkboxes (multiple select)
                    sb.AppendLine($@"
<div class=""checkbox-group-{field.Name}"">
    <label class=""block text-sm font-bold text-slate-700 mb-1.5"">{field.Name}</label>
    <div class=""flex flex-wrap gap-4"">");

                    if (!string.IsNullOrWhiteSpace(field.EnumValues))
                    {
                        var items = field.EnumValues.Split(',', System.StringSplitOptions.TrimEntries | System.StringSplitOptions.RemoveEmptyEntries)
                            .Select(v => (code: v.Trim().Replace(' ', '_').ToUpperInvariant(), text: v.Trim()))
                            .ToList();

                        sb.AppendLine($@"        @{{ var _opts = new[] {{");
                        for (int i = 0; i < items.Count; i++)
                        {
                            var comma = i < items.Count - 1 ? "," : "";
                            sb.AppendLine($"            (\"{items[i].code}\", \"{items[i].text}\"){comma}");
                        }
                        sb.AppendLine($@"        }}; }}
        @foreach (var opt in _opts)
        {{
            var _checked = Model.{field.Name} != null && Model.{field.Name}.Split(',').Contains(opt.Item1);
            <label class=""inline-flex items-center gap-2 cursor-pointer"">
                <input type=""checkbox"" value=""@opt.Item1""
                       onchange=""updateCheckboxHidden(this, '{field.Name}')""
                       class=""w-4 h-4 rounded border-slate-300 text-indigo-600 focus:ring-indigo-500""
                       @(_checked ? ""checked"" : """") />
                <span class=""text-sm text-slate-700"">@opt.Item2</span>
            </label>
        }}");
                    }

                    sb.AppendLine($@"
    </div>
    <input type=""hidden"" asp-for=""{field.Name}"" id=""hf_{field.Name}"" />
    <span asp-validation-for=""{field.Name}"" class=""text-xs text-rose-500 mt-1""></span>
</div>");
                }
                else
                {
                    // Radio buttons (single select)
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

    private static string BuildCheckboxScripts(bool hasCheckbox)
    {
        if (!hasCheckbox) return string.Empty;

        return @"
<script>
function updateCheckboxHidden(checkbox, fieldName) {
    var div = checkbox.closest('div[class^=""checkbox-group-""]');
    if (!div) div = checkbox.closest('div');
    var hidden = document.getElementById('hf_' + fieldName);
    if (!hidden) return;
    var checked = div.querySelectorAll('input[type=""checkbox""]:checked');
    var values = [];
    checked.forEach(function(cb) { values.push(cb.value); });
    hidden.value = values.join(',');
}
</script>";
    }

    private static string BuildPivotScripts(bool hasPivot)
    {
        if (!hasPivot) return string.Empty;

        return @"
<script>
function togglePivotDropdown(btn) {
    var panel = btn.nextElementSibling;
    var isHidden = panel.classList.contains('hidden');
    document.querySelectorAll('.relative > div.absolute.z-10').forEach(function(p) {
        if (p !== panel) p.classList.add('hidden');
    });
    panel.classList.toggle('hidden', !isHidden);
}

function updatePivotLabel(checkbox) {
    var container = checkbox.closest('.relative');
    var btn = container.querySelector('button');
    var checked = container.querySelectorAll('input:checked');
    var label = btn.querySelector('span');
    label.textContent = checked.length > 0 ? checked.length + ' selected' : 'Select ' + btn.getAttribute('data-label');
}

document.addEventListener('click', function(e) {
    if (!e.target.closest('.relative')) {
        document.querySelectorAll('.relative > div.absolute.z-10').forEach(function(p) {
            p.classList.add('hidden');
        });
    }
});

document.addEventListener('DOMContentLoaded', function() {
    document.querySelectorAll('.relative').forEach(function(container) {
        var btn = container.querySelector('button');
        var checked = container.querySelectorAll('input:checked');
        var label = btn.querySelector('span');
        label.textContent = checked.length > 0 ? checked.length + ' selected' : 'Select ' + btn.getAttribute('data-label');
    });
});
</script>";
    }
}