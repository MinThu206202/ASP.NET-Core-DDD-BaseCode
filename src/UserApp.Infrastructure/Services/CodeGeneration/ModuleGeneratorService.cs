using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UserApp.Application.Common.DTOs;
using UserApp.Application.Common.Interfaces;
using UserApp.Infrastructure.Services.CodeGeneration;
using UserApp.Infrastructure.Services.CodeGeneration.Shared;

namespace UserApp.Infrastructure.Services;

public class ModuleGeneratorService : IModuleGeneratorService
{
    private readonly PathProvider _paths;
    private readonly FileManager _files;
    private readonly TemplateEngine _templates;
    private readonly DomainGenerator _domain;
    private readonly ApplicationGenerator _application;
    private readonly InfrastructureGenerator _infrastructure;
    private readonly WebGenerator _web;
    private readonly DbContextUpdater _dbContextUpdater;
    private readonly MappingUpdater _mappingUpdater;
    private readonly ProgramUpdater _programUpdater;
    private readonly MigrationRunner _migrationRunner;

    public ModuleGeneratorService()
    {
        _paths = new PathProvider();
        _files = new FileManager();
        _templates = new TemplateEngine(_paths);

        _domain = new DomainGenerator(_paths, _files, _templates);
        _application = new ApplicationGenerator(_paths, _files, _templates);
        _infrastructure = new InfrastructureGenerator(_paths, _files, _templates);
        _web = new WebGenerator(_paths, _files, _templates);
        _dbContextUpdater = new DbContextUpdater(_files, _paths);
        _mappingUpdater = new MappingUpdater(_files, _paths);
        _programUpdater = new ProgramUpdater(_files, _paths);
        _migrationRunner = new MigrationRunner(_paths);
    }

    public Task GenerateModuleAsync(
        string moduleName,
        List<ModuleFieldDto> fields,
        bool runMigration = false,
        bool hasImage = false,
        bool runDbUpdate = false
    )
    {
        if (string.IsNullOrWhiteSpace(moduleName))
            throw new ArgumentException("Module name is required.", nameof(moduleName));

        var name = Capitalize(moduleName.Trim());

        Console.WriteLine($"Generating module: {name}");

        _domain.Generate(name, fields, hasImage);
        _application.Generate(name);
        _infrastructure.Generate(name);
        _web.Generate(name, fields, hasImage);

        _mappingUpdater.Update(name);
        _dbContextUpdater.Update(name);
        _programUpdater.Update(name);

        if (runMigration)
            _migrationRunner.AddMigration(name);

        if (runDbUpdate)
            _migrationRunner.UpdateDatabase();

        Console.WriteLine($"Module {name} generated successfully");

        return Task.CompletedTask;
    }

    private static string Capitalize(string input)
        => char.ToUpperInvariant(input[0]) + input[1..];
}
