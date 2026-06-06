using UserApp.Application.Common.Interfaces;

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

    // =========================
    // ENTRY POINT
    // =========================
    public Task GenerateModuleAsync(string moduleName)
    {
        var name = Capitalize(moduleName);

        GenerateDomain(_srcPath, name);
        GenerateApplication(_srcPath, name);
        GenerateInfrastructure(_srcPath, name);
        GenerateWeb(_srcPath, name);

        return Task.CompletedTask;
    }

    // =========================
    // SOLUTION ROOT FIX
    // =========================
    private string GetSolutionRoot()
    {
        var dir = AppContext.BaseDirectory;

        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir, "UserApp.sln")))
                return dir;

            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new Exception("Could not find solution root (UserApp.sln)");
    }

    // =========================
    // DOMAIN
    // =========================
    private void GenerateDomain(string src, string name)
    {
        var path = Path.Combine(src, "UserApp.Domain", $"{name}s");
        Directory.CreateDirectory(path);

        File.WriteAllText(
            Path.Combine(path, $"{name}.cs"),
$@"using UserApp.Domain.Common;

namespace UserApp.Domain.{name}s;

public class {name} : Entity<Guid>
{{
    public Guid Id {{ get; set; }}
}}");

        File.WriteAllText(
            Path.Combine(path, $"I{name}Repository.cs"),
$@"using UserApp.Domain.Common;

namespace UserApp.Domain.{name}s;

public interface I{name}Repository : IBaseRepository<{name}>
{{
}}");
    }

    // =========================
    // APPLICATION
    // =========================
    private void GenerateApplication(string src, string name)
    {
        var path = Path.Combine(src, "UserApp.Application", $"{name}s");
        var interfacePath = Path.Combine(path, "Interfaces");

        Directory.CreateDirectory(path);
        Directory.CreateDirectory(interfacePath);

        File.WriteAllText(
            Path.Combine(path, $"{name}Service.cs"),
$@"using UserApp.Domain.{name}s;
using UserApp.Application.Common;

namespace UserApp.Application.{name}s;

public class {name}Service : BaseService<{name}>, I{name}Service
{{
    public {name}Service(I{name}Repository repo) : base(repo)
    {{
    }}
}}");

        File.WriteAllText(
            Path.Combine(interfacePath, $"I{name}Service.cs"),
$@"using UserApp.Domain.{name}s;
using UserApp.Application.Common;

namespace UserApp.Application.{name}s.Interfaces;

public interface I{name}Service : IBaseService<{name}>
{{
}}");
    }

    // =========================
    // INFRASTRUCTURE
    // =========================
    private void GenerateInfrastructure(string src, string name)
    {
        var path = Path.Combine(src, "UserApp.Infrastructure", "Persistence", "Repositories");
        Directory.CreateDirectory(path);

        File.WriteAllText(
            Path.Combine(path, $"{name}Repository.cs"),
$@"using UserApp.Domain.{name}s;
using UserApp.Infrastructure.Persistence;

namespace UserApp.Infrastructure.Persistence.Repositories;

public class {name}Repository : BaseRepository<{name}>, I{name}Repository
{{
    public {name}Repository(AppDbContext db) : base(db)
    {{
    }}
}}");
    }

    // =========================
    // WEB
    // =========================
    private void GenerateWeb(string src, string name)
    {
        var controllerPath = Path.Combine(src, "UserApp.Web", "Controllers");
        var vmPath = Path.Combine(src, "UserApp.Web", "ViewModels", $"{name}s");

        Directory.CreateDirectory(controllerPath);
        Directory.CreateDirectory(vmPath);

        File.WriteAllText(
            Path.Combine(controllerPath, $"{name}Controller.cs"),
$@"using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using UserApp.Application.{name}s.Interfaces;
using UserApp.Domain.{name}s;
using UserApp.Web.ViewModels.{name}s;

namespace UserApp.Web.Controllers;

public class {name}Controller : Controller
{{
    private readonly I{name}Service _service;
    private readonly IMapper _mapper;

    public {name}Controller(I{name}Service service, IMapper mapper)
    {{
        _service = service;
        _mapper = mapper;
    }}
}}");

        File.WriteAllText(
            Path.Combine(vmPath, $"{name}ViewModel.cs"),
$@"namespace UserApp.Web.ViewModels.{name}s;

public class {name}ViewModel
{{
    public Guid Id {{ get; set; }}
}}");
    }

    // =========================
    // HELPERS
    // =========================
    private string Capitalize(string input)
        => char.ToUpper(input[0]) + input.Substring(1);
}