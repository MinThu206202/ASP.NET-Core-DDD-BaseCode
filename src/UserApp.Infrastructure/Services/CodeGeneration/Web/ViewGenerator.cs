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
                ["Scripts"] = BuildMediaScripts(hasImage) + BuildCheckboxScripts(hasCheckbox) + BuildPivotScripts(hasPivot)
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

        var detailsContent = _templates.RenderFile(
            new[] { "Web", "Templates", "Details.tpl" },
            new Dictionary<string, string>
            {
                ["Name"] = name,
                ["DetailFields"] = BuildDetailFields(fields),
                ["DetailImages"] = BuildDetailImages(hasImage)
            });

        _files.WriteFile(Path.Combine(viewFolder, "Index.cshtml"), indexContent);
        _files.WriteFile(Path.Combine(viewFolder, "Create.cshtml"), createContent);
        _files.WriteFile(Path.Combine(viewFolder, "Edit.cshtml"), editContent);
        _files.WriteFile(Path.Combine(viewFolder, "Details.cshtml"), detailsContent);
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

            var displayValue = field.IsPivot
                ? $"@Html.Raw(string.IsNullOrEmpty(p.{field.Name}Display) ? \"<span class='px-2 py-0.5 rounded-full text-xs font-bold bg-slate-100 text-slate-500'>OFF</span>\" : \"<span class='px-2 py-0.5 rounded-full text-xs font-bold bg-green-100 text-green-700'>ON</span>\")"
                : field.IsRelation
                    ? $"@(!string.IsNullOrEmpty(p.{field.Name}Name) ? p.{field.Name}Name : \"—\")"
                    : field.UseCommonTable
                        ? $"@p.{field.Name}Name"
                        : IsBooleanType(field.Type)
                            ? $"@(p.{field.Name} ? \"On\" : \"Off\")"
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
            <div class=""w-10 h-10 rounded-lg overflow-hidden ring-2 ring-slate-100"">
                <img src=""@img"" class=""w-full h-full object-cover"" />
            </div>
        }
        @if (p.ImageUrls.Count > 3)
        {
            <span class=""text-xs font-bold text-slate-400 ml-1"">+@(p.ImageUrls.Count - 3)</span>
        }
        </div>
    }
    else
    {
        <span class=""text-xs text-slate-400 italic"">No images</span>
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
                <div class=""relative"">
                    <input type=""file"" name=""files"" multiple
                           class=""peer absolute inset-0 w-full h-full opacity-0 cursor-pointer z-10""
                           onchange=""updateFileLabel(this)"" />
                    <div class=""flex flex-col items-center justify-center gap-2 px-6 py-8 rounded-xl border-2 border-dashed border-slate-200 bg-slate-50 text-slate-400 peer-hover:border-indigo-400 peer-hover:bg-indigo-50/50 transition-all duration-200"">
                        <svg class=""w-10 h-10 text-slate-300 peer-hover:text-indigo-400 transition-colors duration-200"" fill=""none"" stroke=""currentColor"" viewBox=""0 0 24 24"">
                            <path stroke-linecap=""round"" stroke-linejoin=""round"" stroke-width=""1.5"" d=""M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z"");
                        </svg>
                        <span id=""fileLabel"" class=""text-sm font-medium"">Choose images or drag here</span>
                        <span class=""text-xs text-slate-400"">Supports JPG, PNG, WEBP &bull; Max 5MB each</span>
                    </div>
                </div>
                <div id=""filePreview"" class=""grid grid-cols-3 gap-2 mt-2""></div>
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
                        <label class=""block text-sm font-bold text-slate-700 mb-3"">Current Images</label>
                        <div class=""grid grid-cols-2 sm:grid-cols-3 gap-4"">
                        @foreach (var media in Model.MediaList)
                        {
                            <div class=""media-item group relative bg-slate-50 rounded-xl border border-slate-200 p-2 hover:border-indigo-300 hover:shadow-md transition-all duration-200"">
                                <div class=""relative aspect-square rounded-lg overflow-hidden bg-slate-100"">
                                    <img src=""@media.Url"" alt=""@media.OriginalName""
                                         class=""w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"" />
                                    <button type=""button""
                                            onclick=""deleteMedia('@media.Id', this)""
                                            class=""absolute top-1.5 right-1.5 w-7 h-7 flex items-center justify-center rounded-full bg-rose-500 shadow-sm text-white hover:bg-rose-600 hover:scale-110 transition-all duration-200"">
                                        <svg class=""w-4 h-4"" fill=""none"" stroke=""currentColor"" viewBox=""0 0 24 24"">
                                            <path stroke-linecap=""round"" stroke-linejoin=""round"" stroke-width=""2"" d=""M6 18L18 6M6 6l12 12""/>
                                        </svg>
                                    </button>
                                </div>
                                <div class=""mt-2"">
                                    <p class=""text-xs text-slate-500 truncate px-1"">@media.OriginalName</p>
                                    <div class=""mt-1.5"">
                                        <input type=""file"" name=""replace_@media.Id""
                                               class=""block w-full text-xs text-slate-500 file:mr-2 file:py-1 file:px-3 file:rounded-lg file:border-0 file:text-xs file:font-bold file:bg-indigo-50 file:text-indigo-600 hover:file:bg-indigo-100 transition-colors""
                                               accept=""image/*"" />
                                    </div>
                                </div>
                            </div>
                        }
                        </div>
                    </div>
                }";
    }

    // =========================
    // DETAIL VIEW
    // =========================
    private static string BuildDetailFields(List<ModuleFieldDto> fields)
    {
        var sb = new StringBuilder();

        foreach (var field in fields)
        {
            if (field.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                continue;

            // Skip FK/relation fields on parent detail (they show data from other entities)
            if (field.IsRelation && !field.IsPivot)
                continue;

            // PIVOT — show comma-separated display
            if (field.IsPivot)
            {
                sb.AppendLine($@"
<div>
    <label class=""block text-sm font-bold text-slate-700 mb-1.5"">{field.Name}</label>
    <div class=""px-4 py-2.5 rounded-xl border border-slate-200 bg-slate-50 text-slate-800"">
        @(!string.IsNullOrEmpty(Model.{field.Name}Display) ? Model.{field.Name}Display : ""None"")
    </div>
</div>");
                continue;
            }

            // COMMON TABLE (lookup) — show resolved name
            if (field.UseCommonTable)
            {
                sb.AppendLine($@"
<div>
    <label class=""block text-sm font-bold text-slate-700 mb-1.5"">{field.Name}</label>
    <div class=""px-4 py-2.5 rounded-xl border border-slate-200 bg-slate-50 text-slate-800"">
        @Model.{field.Name}Name
    </div>
</div>");
                continue;
            }

            // BOOLEAN
            if (IsBooleanType(field.Type))
            {
                sb.AppendLine($@"
<div>
    <label class=""block text-sm font-bold text-slate-700 mb-1.5"">{field.Name}</label>
    <div class=""px-4 py-2.5 rounded-xl border border-slate-200 bg-slate-50 text-slate-800"">
        @(Model.{field.Name} ? ""Yes"" : ""No"")
    </div>
</div>");
                continue;
            }

            // DEFAULT (string, int, decimal, etc.)
            sb.AppendLine($@"
<div>
    <label class=""block text-sm font-bold text-slate-700 mb-1.5"">{field.Name}</label>
    <div class=""px-4 py-2.5 rounded-xl border border-slate-200 bg-slate-50 text-slate-800"">
        @Model.{field.Name}
    </div>
</div>");
        }

        return sb.ToString();
    }

    private static string BuildDetailImages(bool hasImage)
    {
        if (!hasImage) return string.Empty;

        return @"
<div>
    <label class=""block text-sm font-bold text-slate-700 mb-1.5"">Images</label>
    @if (Model.ImageUrls.Count > 0)
    {
        <div class=""grid grid-cols-3 gap-3"">
        @foreach (var img in Model.ImageUrls)
        {
            <img src=""@img"" class=""w-full h-32 object-cover rounded-xl border border-slate-200"" />
        }
        </div>
    }
    else
    {
        <div class=""px-4 py-2.5 rounded-xl border border-slate-200 bg-slate-50 text-slate-400"">
            No images
        </div>
    }
</div>";
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
            const label = document.getElementById('fileLabel');
            const preview = document.getElementById('filePreview');
            const count = input.files.length;
            label.textContent = count > 0 ? count + ' file(s) selected' : 'Choose images or drag here';
            preview.innerHTML = '';
            Array.from(input.files).forEach(file => {
                const img = document.createElement('img');
                img.src = URL.createObjectURL(file);
                img.className = 'w-full h-20 object-cover rounded-lg border border-slate-200';
                preview.appendChild(img);
            });
        }
        function deleteMedia(mediaId, btn) {
            if (!confirm('Delete this image?')) return;
            fetch('@Url.Action(""Delete"", ""Media"")', {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: 'mediaId=' + mediaId
            }).then(r => {
                if (r.ok) {
                    const item = btn.closest('.media-item');
                    item.style.transition = 'all 0.3s ease';
                    item.style.opacity = '0';
                    item.style.transform = 'scale(0.8)';
                    setTimeout(() => item.remove(), 300);
                } else {
                    alert('Failed to delete image');
                }
            }).catch(() => alert('Failed to delete image'));
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