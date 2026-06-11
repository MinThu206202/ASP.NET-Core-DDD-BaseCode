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

    private static string BuildTableColumns(List<ModuleFieldDto> fields, bool hasImage)
    {
        var sb = new StringBuilder();

        foreach (var field in fields)
        {
            if (field.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                continue;

            sb.AppendLine($"                        <th class=\"text-left px-6 py-4 text-xs font-bold text-slate-500 uppercase tracking-wider\">{field.Name}</th>");
        }

        if (hasImage)
            sb.AppendLine("                        <th class=\"text-left px-6 py-4 text-xs font-bold text-slate-500 uppercase tracking-wider\">Images</th>");

        return sb.ToString();
    }

    private static string BuildTableRows(List<ModuleFieldDto> fields, bool hasImage)
    {
        var sb = new StringBuilder();

        foreach (var field in fields)
        {
            if (field.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                continue;

            sb.AppendLine($"                        <td class=\"px-6 py-4 text-slate-600\">@p.{field.Name}</td>");
        }

        if (hasImage)
        {
            sb.AppendLine(@"                        <td class=""px-6 py-4"">
                            @if (p.ImageUrls.Count > 0)
                            {
                                <div class=""flex items-center gap-1.5"">
                                @foreach (var img in p.ImageUrls.Take(3))
                                {
                                    <div class=""w-10 h-10 rounded-lg overflow-hidden ring-2 ring-slate-100"">
                                        <img src=""@img"" alt="""" class=""w-full h-full object-cover"" />
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

    private static string BuildFormInputs(List<ModuleFieldDto> fields, bool hasImage)
    {
        var sb = new StringBuilder();

        foreach (var field in fields)
        {
            if (field.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                continue;

            sb.AppendLine($@"
                <div>
                    <label asp-for=""{field.Name}"" class=""block text-sm font-bold text-slate-700 mb-1.5"">{field.Name}</label>
                    <input asp-for=""{field.Name}""
                           class=""w-full px-4 py-2.5 rounded-xl border border-slate-200 bg-slate-50 text-slate-800 font-medium placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-500/30 focus:border-indigo-500 transition-all duration-200"" />
                    <span asp-validation-for=""{field.Name}"" class=""text-xs text-rose-500 mt-1""></span>
                </div>");
        }

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
                                <path stroke-linecap=""round"" stroke-linejoin=""round"" stroke-width=""1.5"" d=""M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z""/>
                            </svg>
                            <span id=""fileLabel"" class=""text-sm font-medium"">Choose images or drag here</span>
                            <span class=""text-xs text-slate-400"">Supports JPG, PNG, WEBP &bull; Max 5MB each</span>
                        </div>
                    </div>
                </div>");
        }

        return sb.ToString();
    }

    internal void GenerateViews(string name, List<ModuleFieldDto> fields, object hasImage)
    {
        throw new NotImplementedException();
    }

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

    private static string BuildMediaScripts(bool hasImage)
    {
        if (!hasImage) return string.Empty;

        return @"
    <script>
        function updateFileLabel(input) {
            const label = document.getElementById('fileLabel');
            label.textContent = input.files.length > 0
                ? input.files.length + ' file(s) selected'
                : 'Choose images or drag here';
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
    </script>
";
    }

    private string BuildProperty(ModuleFieldDto field)
    {
        var sb = new StringBuilder();

        if (field.IsRequired)
            sb.AppendLine("[Required(ErrorMessage = \"" + field.Name + " is required\")]");

        if (field.MinLength.HasValue || field.MaxLength.HasValue)
        {
            sb.AppendLine(
                $"[StringLength({field.MaxLength ?? 500}," +
                $" MinimumLength = {field.MinLength ?? 0}," +
                $" ErrorMessage = \"{field.Name} length must be between {field.MinLength ?? 0} and {field.MaxLength ?? 500}\")]");
        }

        if (field.Type == "decimal" ||
            field.Type == "double" ||
            field.Type == "int")
        {
            sb.AppendLine(
                $"[Range({field.MinValue ?? 0}," +
                $"{field.MaxValue ?? 999999999}," +
                $"ErrorMessage = \"{field.Name} must be between {field.MinValue ?? 0} and {field.MaxValue ?? 999999999}\")]");
        }

        sb.AppendLine(
            $"public {field.Type}{(field.IsNullable ? "?" : "")} {field.Name} {{ get; set; }}");

        return sb.ToString();
    }
}
