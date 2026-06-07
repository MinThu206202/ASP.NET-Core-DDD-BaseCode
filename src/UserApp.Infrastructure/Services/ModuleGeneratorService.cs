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

    // ========================= ENTRY =========================
    public Task GenerateModuleAsync(string moduleName)
    {
        var name = Capitalize(moduleName);

        GenerateDomain(name);
        GenerateApplication(name);
        GenerateInfrastructure(name);
        GenerateWeb(name);

        UpdateMappingProfile(name);
        UpdateDbContext(name);

        return Task.CompletedTask;
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

    // ========================= DOMAIN =========================
    private void GenerateDomain(string name)
    {
        var path = Path.Combine(_srcPath, $"UserApp.Domain/{name}s");
        Directory.CreateDirectory(path);

        File.WriteAllText(Path.Combine(path, $"{name}.cs"),
        $@"using UserApp.Domain.Common;

        namespace UserApp.Domain.{name}s;

        public class {name} : Entity<Guid>
        {{
            public string Name {{ get; set; }} = string.Empty;
        }}");

        File.WriteAllText(Path.Combine(path, $"I{name}Repository.cs"),
        $@"using UserApp.Domain.Common;

        namespace UserApp.Domain.{name}s;

        public interface I{name}Repository : IBaseRepository<{name}>
        {{
        }}");
    }

    // ========================= APPLICATION (FIXED HERE) =========================
    private void GenerateApplication(string name)
    {
        var path = Path.Combine(_srcPath, $"UserApp.Application/{name}s");
        var interfaces = Path.Combine(path, "Interfaces");

        Directory.CreateDirectory(path);
        Directory.CreateDirectory(interfaces);

        // SERVICE
        File.WriteAllText(Path.Combine(path, $"{name}Service.cs"),
        $@"using UserApp.Domain.{name}s;
        using UserApp.Application.Common;
        using UserApp.Application.{name}s.Interfaces;

        namespace UserApp.Application.{name}s;

        public class {name}Service : BaseService<{name}>, I{name}Service
        {{
            public {name}Service(I{name}Repository repo) : base(repo)
            {{
            }}
        }}");

        // INTERFACE (🔥 FIXED: Domain added)
        File.WriteAllText(Path.Combine(interfaces, $"I{name}Service.cs"),
        $@"using UserApp.Application.Common;
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

    // ========================= WEB =========================
    private void GenerateWeb(string name)
    {
        var mvc = Path.Combine(_srcPath, "UserApp.Web/Controllers");
        var api = Path.Combine(_srcPath, "UserApp.Web/Controllers/Api");
        var vm = Path.Combine(_srcPath, $"UserApp.Web/ViewModels/{name}s");

        Directory.CreateDirectory(mvc);
        Directory.CreateDirectory(api);
        Directory.CreateDirectory(vm);

        // MVC
        File.WriteAllText(Path.Combine(mvc, $"{name}Controller.cs"),
        $@"using AutoMapper;
        using Microsoft.AspNetCore.Mvc;
        using UserApp.Application.{name}s.Interfaces;
        using UserApp.Domain.{name}s;
        using UserApp.Web.ViewModels.{name}s;

        namespace UserApp.Web.Controllers;

        public class {name}Controller : BaseController<{name}, {name}ViewModel>
        {{
            public {name}Controller(I{name}Service service, IMapper mapper)
                : base(service, mapper)
            {{
            }}
        }}");

        // API
        File.WriteAllText(Path.Combine(api, $"{name}ApiController.cs"),
        $@"using AutoMapper;
        using Microsoft.AspNetCore.Authorization;
        using Microsoft.AspNetCore.Mvc;
        using UserApp.Application.{name}s.Interfaces;
        using UserApp.Domain.{name}s;
        using UserApp.Web.ViewModels.{name}s;

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

        // ViewModel
        File.WriteAllText(Path.Combine(vm, $"{name}ViewModel.cs"),
        $@"namespace UserApp.Web.ViewModels.{name}s;

        public class {name}ViewModel
        {{
            public Guid Id {{ get; set; }}
            public string Name {{ get; set; }} = string.Empty;
        }}");
    }

    // ========================= MAPPING =========================
    private void UpdateMappingProfile(string name)
    {
        var file = Path.Combine(_srcPath, "UserApp.Web/Mapping/MappingProfile.cs");

        EnsureUsing(file, $"using UserApp.Domain.{name}s;");
        EnsureUsing(file, $"using UserApp.Web.ViewModels.{name}s;");

        var inject =
        $@"
        CreateMap<{name}, {name}ViewModel>();
        CreateMap<{name}ViewModel, {name}>();
        ";

        CodeInjector.InjectBetween(
            file,
            "// <AUTO-MAPPINGS-START>",
            "// <AUTO-MAPPINGS-END>",
            inject
        );
    }

    // ========================= DB CONTEXT =========================
    private void UpdateDbContext(string name)
    {
        var file = Path.Combine(_srcPath, "UserApp.Infrastructure/Persistence/AppDbContext.cs");

        EnsureUsing(file, $"using UserApp.Domain.{name}s;");

        var inject =
        $@"
            public DbSet<{name}> {name}s => Set<{name}>();
        ";

        CodeInjector.InjectBetween(
            file,
            "// <AUTO-DBSETS-START>",
            "// <AUTO-DBSETS-END>",
            inject
        );
    }

    // ========================= USING HELPER =========================
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