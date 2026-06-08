using UserApp.Application.Common.Interfaces;
using UserApp.Application.Common.DTOs;
using System.Text;

namespace UserApp.Infrastructure.Services;

public class ModuleGeneratorService : IModuleGeneratorService
{
    private readonly string _solutionRoot;
    private readonly string _srcPath;

    public ModuleGeneratorService()
    {
        _solutionRoot = GetSolutionRoot();
        _srcPath = Path.Combine(_solutionRoot, "src");
    }

    private string InfraProject =>
    Path.Combine(_solutionRoot, "src/UserApp.Infrastructure/UserApp.Infrastructure.csproj");

    private string WebProject =>
        Path.Combine(_solutionRoot, "src/UserApp.Web/UserApp.Web.csproj");

    // ========================= ENTRY =========================
    public Task GenerateModuleAsync(
        string moduleName,
        List<ModuleFieldDto> fields,
        bool runMigration,
        bool runDbUpdate
    )
    {
        var name = Capitalize(moduleName);

        GenerateDomain(name, fields);
        GenerateDomainRepository(name);
        GenerateApplication(name);
        GenerateInfrastructure(name);
        GenerateWeb(name, fields);
        GenerateViews(name, fields);

        UpdateMappingProfile(name);
        UpdateDbContext(name);
        UpdateProgramCs(name);

        // EF MIGRATION
        if (runMigration)
        {
            var migrationName = $"{name}_{DateTime.Now:yyyyMMddHHmmss}";

            if (!MigrationExists(migrationName))
            {
                RunCommand("dotnet",
                    $"ef migrations add {migrationName} " +
                    $"--project {InfraProject} " +
                    $"--startup-project {WebProject}");
            }
        }

        // DB UPDATE
        if (runDbUpdate)
        {
            if (HasPendingMigrations())
            {
                RunCommand("dotnet",
                    $"ef database update " +
                    $"--project {InfraProject} " +
                    $"--startup-project {WebProject}");
            }
        }

        return Task.CompletedTask;
    }
    // ========================= Migrations =========================

    private bool MigrationExists(string migrationName)
    {
        var migrationsPath = Path.Combine(
            _srcPath,
            "UserApp.Infrastructure/Persistence/Migrations"
        );

        if (!Directory.Exists(migrationsPath))
            return false;

        return Directory.GetFiles(migrationsPath)
            .Any(x => Path.GetFileName(x).Contains(migrationName));
    }

    private string? GetLastMigration()
    {
        var migrationsPath = Path.Combine(
            _srcPath,
            "UserApp.Infrastructure/Persistence/Migrations"
        );

        if (!Directory.Exists(migrationsPath))
            return null;

        var migrationFiles = Directory.GetFiles(migrationsPath, "*.cs")
            .Select(Path.GetFileNameWithoutExtension)
            .OrderByDescending(x => x)
            .ToList();

        return migrationFiles.FirstOrDefault();
    }

    private bool HasPendingMigrations()
    {
        var result = RunCommandCapture(
            "dotnet",
            $"ef migrations list --project {InfraProject} --startup-project {WebProject}"
        );

        // crude but effective check
        return result.Contains("(Pending)");
    }

    private string RunCommandCapture(string fileName, string arguments)
    {
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = _solutionRoot
            }
        };

        process.Start();

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();

        process.WaitForExit();

        return output + "\n" + error;
    }

    // ========================= SOLUTION ROOT =========================
    private string GetSolutionRoot()
    {
        var dir = AppContext.BaseDirectory;

        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir, "UserApp.sln")))
                return dir;

            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new Exception("Solution root not found");
    }

    // ========================= DOMAIN ENTITY =========================
    private void GenerateDomain(string name, List<ModuleFieldDto> fields)
    {
        var path = Path.Combine(_srcPath, $"UserApp.Domain/{name}s");
        Directory.CreateDirectory(path);

        var props = new StringBuilder();

        foreach (var f in fields)
        {
            // ❌ NEVER generate Id (already in Entity<Guid>)
            if (f.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                continue;

            var nullable = f.IsNullable && f.Type != "string" ? "?" : "";

            if (f.Type == "string")
            {
                props.AppendLine($@"
                public string {f.Name} {{ get; set; }} = string.Empty;
            ");
            }
            else
            {
                props.AppendLine($@"
                public {f.Type}{nullable} {f.Name} {{ get; set; }}
            ");
            }
        }

        var entity = $@"
        using UserApp.Domain.Common;

        namespace UserApp.Domain.{name}s;

        public class {name} : Entity<Guid>
        {{
        {props}
        }}
        ";

        File.WriteAllText(Path.Combine(path, $"{name}.cs"), entity);
    }

    // ========================= DOMAIN REPOSITORY (IMPORTANT FIX) =========================
    private void GenerateDomainRepository(string name)
    {
        var path = Path.Combine(_srcPath, $"UserApp.Domain/{name}s");
        Directory.CreateDirectory(path);

        var file = Path.Combine(path, $"I{name}Repository.cs");

        var content = $@"
        using UserApp.Domain.Common;

        namespace UserApp.Domain.{name}s;

        public interface I{name}Repository : IBaseRepository<{name}>
        {{
        }}
        ";

        File.WriteAllText(file, content);
    }

    // ========================= APPLICATION =========================
    private void GenerateApplication(string name)
    {
        var path = Path.Combine(_srcPath, $"UserApp.Application/{name}s");
        var interfaces = Path.Combine(path, "Interfaces");

        Directory.CreateDirectory(path);
        Directory.CreateDirectory(interfaces);

        File.WriteAllText(Path.Combine(path, $"{name}Service.cs"),
        $@"
        using UserApp.Domain.{name}s;
        using UserApp.Application.Common;
        using UserApp.Application.{name}s.Interfaces;

        namespace UserApp.Application.{name}s;

        public class {name}Service : BaseService<{name}>, I{name}Service
        {{
            public {name}Service(I{name}Repository repo) : base(repo)
            {{
            }}
        }}");

        File.WriteAllText(Path.Combine(interfaces, $"I{name}Service.cs"),
        $@"
        using UserApp.Application.Common;
        using UserApp.Domain.{name}s;

        namespace UserApp.Application.{name}s.Interfaces;

        public interface I{name}Service : IBaseService<{name}>
        {{
        }}");
    }

    // ========================= INFRASTRUCTURE =========================
    private void GenerateInfrastructure(string name)
    {
        var path = Path.Combine(_srcPath, "UserApp.Infrastructure/Persistence/Repositories");
        Directory.CreateDirectory(path);

        File.WriteAllText(Path.Combine(path, $"{name}Repository.cs"),
        $@"
        using UserApp.Domain.{name}s;
        using UserApp.Infrastructure.Persistence;

        namespace UserApp.Infrastructure.Persistence.Repositories;

        public class {name}Repository : BaseRepository<{name}>, I{name}Repository
        {{
            public {name}Repository(AppDbContext db) : base(db)
            {{
            }}
        }}");
    }

    // ========================= WEB =========================
    private void GenerateWeb(string name, List<ModuleFieldDto> fields)
    {
        var mvc = Path.Combine(_srcPath, "UserApp.Web/Controllers");
        var api = Path.Combine(_srcPath, "UserApp.Web/Controllers/Api");
        var vm = Path.Combine(_srcPath, $"UserApp.Web/ViewModels/{name}s");

        Directory.CreateDirectory(mvc);
        Directory.CreateDirectory(api);
        Directory.CreateDirectory(vm);

        File.WriteAllText(Path.Combine(mvc, $"{name}Controller.cs"),
        $@"
        using AutoMapper;
        using Microsoft.AspNetCore.Mvc;
        using UserApp.Application.{name}s.Interfaces;
        using UserApp.Domain.{name}s;
        using UserApp.Web.ViewModels;

        namespace UserApp.Web.Controllers;

        public class {name}Controller : BaseController<{name}, {name}ViewModel>
        {{
            public {name}Controller(I{name}Service service, IMapper mapper)
                : base(service, mapper)
            {{
            }}
        }}");

        File.WriteAllText(Path.Combine(api, $"{name}ApiController.cs"),
        $@"
        using AutoMapper;
        using Microsoft.AspNetCore.Authorization;
        using Microsoft.AspNetCore.Mvc;
        using UserApp.Application.{name}s.Interfaces;
        using UserApp.Domain.{name}s;
        using UserApp.Web.ViewModels;

        namespace UserApp.Web.Controllers.Api;

        [ApiController]
        [Route(""api/[controller]"")]
        [Authorize]
        public class {name}ApiController : BaseApiController<{name}, {name}ViewModel>
        {{
            public {name}ApiController(I{name}Service service, IMapper mapper)
                : base(service, mapper)
            {{
            }}
        }}");

        File.WriteAllText(Path.Combine(vm, $"{name}ViewModel.cs"),
        $@"
        namespace UserApp.Web.ViewModels;

        public class {name}ViewModel
        {{
            public Guid Id {{ get; set; }}

        {GenerateViewModelFields(fields)}
        }}
        ");
    }

    private string GenerateViewModelFields(List<ModuleFieldDto> fields)
    {
        var sb = new StringBuilder();

        foreach (var f in fields)
        {
            if (f.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                continue;

            var nullable = f.IsNullable && f.Type != "string" ? "?" : "";

            if (f.Type == "string")
            {
                sb.AppendLine($"    public string {f.Name} {{ get; set; }} = string.Empty;");
            }
            else
            {
                sb.AppendLine($"    public {f.Type}{nullable} {f.Name} {{ get; set; }}");
            }
        }

        return sb.ToString();
    }


    // ========================= VIEWS GENERATION =========================
    private void GenerateViews(string name, List<ModuleFieldDto> fields)
    {
        var viewPath = Path.Combine(_srcPath, $"UserApp.Web/Views/{name}");
        Directory.CreateDirectory(viewPath);

        GenerateIndexView(name, viewPath, fields);
        GenerateCreateView(name, viewPath, fields);
        GenerateEditView(name, viewPath, fields);
    }


    private void GenerateIndexView(string name, string path, List<ModuleFieldDto> fields)
    {
        var file = Path.Combine(path, "Index.cshtml");

        var columns = new StringBuilder();
        var rows = new StringBuilder();

        foreach (var f in fields)
        {
            if (f.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                continue;

            columns.AppendLine($"<th>{f.Name}</th>");
            rows.AppendLine($"<td>@p.{f.Name}</td>");
        }

        var content = $@"
        @model UserApp.Web.ViewModels.ListViewModel<UserApp.Web.ViewModels.{name}ViewModel>

        <h2>{name}s</h2>

        <a asp-action=""Create"" class=""btn btn-success"">Create {name}</a>

        <table class=""table"">
            <thead>
                <tr>
                    <th>Id</th>
        {columns}
                    <th>Actions</th>
                </tr>
            </thead>

            <tbody>
        @foreach (var p in Model.Items)
        {{
            <tr>
                <td>@p.Id</td>
                {rows}
                <td>
                    <a asp-action=""Edit""
                    asp-route-id=""@p.Id""
                    class=""btn btn-sm btn-primary"">
                        Edit
                    </a>

                    <form asp-action=""Delete""
                        asp-route-id=""@p.Id""
                        method=""post""
                        style=""display:inline""
                        onsubmit=""return confirm('Are you sure you want to delete this?');"">
                        <button type=""submit"" class=""btn btn-sm btn-danger"">
                            Delete
                        </button>
                    </form>
                </td>
            </tr>
        }}
            </tbody>
        </table>
        ";

        File.WriteAllText(file, content);
    }
    private void GenerateCreateView(string name, string path, List<ModuleFieldDto> fields)
    {
        var file = Path.Combine(path, "Create.cshtml");

        var inputs = new StringBuilder();

        foreach (var f in fields)
        {
            if (f.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                continue;

            inputs.AppendLine($@"
        <div class=""form-group"">
            <label>{f.Name}</label>
            <input asp-for=""{f.Name}"" class=""form-control"" />
        </div>
        ");
        }

        var content = $@"
        @model UserApp.Web.ViewModels.{name}ViewModel

        <h2>Create {name}</h2>

        <form asp-action=""Create"" method=""post"">

        {inputs}

            <button type=""submit"" class=""btn btn-success"">Save</button>
        </form>
        ";

        File.WriteAllText(file, content);
    }

    private void GenerateEditView(string name, string path, List<ModuleFieldDto> fields)
    {
        var file = Path.Combine(path, "Edit.cshtml");

        var inputs = new StringBuilder();

        foreach (var f in fields)
        {
            if (f.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                continue;

            inputs.AppendLine($@"
        <div class=""form-group"">
            <label>{f.Name}</label>
            <input asp-for=""{f.Name}"" class=""form-control"" />
        </div>
        ");
        }

        var content = $@"
        @model UserApp.Web.ViewModels.{name}ViewModel

        <h2>Edit {name}</h2>

        <form asp-action=""Edit"" method=""post"">

            <input type=""hidden"" asp-for=""Id"" />

        {inputs}

            <button type=""submit"" class=""btn btn-primary"">Update</button>
        </form>
        ";

        File.WriteAllText(file, content);
    }
    // ========================= DB CONTEXT =========================
    private void UpdateDbContext(string name)
    {
        var file = Path.Combine(_srcPath, "UserApp.Infrastructure/Persistence/AppDbContext.cs");

        EnsureUsing(file, $"using UserApp.Domain.{name}s;");

        var inject = $@"
        public DbSet<{name}> {name}s => Set<{name}>();
        ";

        CodeInjector.InjectBetween(file,
            "// <AUTO-DBSETS-START>",
            "// <AUTO-DBSETS-END>",
            inject);
    }
    // ========================= PROGRAM CS =========================
    private void UpdateProgramCs(string name)
    {
        var file = Path.Combine(_srcPath, "UserApp.Web/Program.cs");

        EnsureUsing(file, $"using UserApp.Domain.{name}s;");
        EnsureUsing(file, $"using UserApp.Application.{name}s;");
        EnsureUsing(file, $"using UserApp.Application.{name}s.Interfaces;");
        EnsureUsing(file, $"using UserApp.Infrastructure.Persistence.Repositories;");

        CodeInjector.InjectBetween(file,
            "// <AUTO-REPOSITORIES-START>",
            "// <AUTO-REPOSITORIES-END>",
            $@"builder.Services.AddScoped<I{name}Repository, {name}Repository>();");

        CodeInjector.InjectBetween(file,
            "// <AUTO-SERVICES-START>",
            "// <AUTO-SERVICES-END>",
            $@"builder.Services.AddScoped<I{name}Service, {name}Service>();");
    }

    // ========================= MAPPING =========================
    private void UpdateMappingProfile(string name)
    {
        var file = Path.Combine(_srcPath, "UserApp.Web/Mapping/MappingProfile.cs");

        EnsureUsing(file, $"using UserApp.Domain.{name}s;");
        // EnsureUsing(file, $"using UserApp.Web.ViewModels.{name}s;");

        CodeInjector.InjectBetween(file,
            "// <AUTO-MAPPINGS-START>",
            "// <AUTO-MAPPINGS-END>",
            $@"
        CreateMap<{name}, {name}ViewModel>();
        CreateMap<{name}ViewModel, {name}>();
        ");
    }

    private void RunCommand(string fileName, string arguments)
    {
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = _solutionRoot
            }
        };

        process.Start();

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();

        process.WaitForExit();



        if (process.ExitCode != 0)
        {
            throw new Exception($"""
        ❌ EF COMMAND FAILED

        COMMAND:
        {fileName} {arguments}

        EXIT CODE:
        {process.ExitCode}

        STDOUT:
        {output}

        STDERR:
        {error}
        """);
        }

        Console.WriteLine(output);
    }
    // ========================= HELPER =========================
    private void EnsureUsing(string filePath, string usingLine)
    {
        var lines = File.ReadAllLines(filePath).ToList();

        if (lines.Any(x => x.Trim() == usingLine))
            return;

        var lastUsing = lines.FindLastIndex(x => x.Trim().StartsWith("using"));

        if (lastUsing == -1)
            throw new Exception("No using found");

        lines.Insert(lastUsing + 1, usingLine);
        File.WriteAllLines(filePath, lines);
    }

    private string Capitalize(string input)
        => char.ToUpper(input[0]) + input.Substring(1);
}