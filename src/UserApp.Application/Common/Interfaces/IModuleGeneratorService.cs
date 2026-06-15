using UserApp.Application.Common.DTOs;

namespace UserApp.Application.Common.Interfaces;

public interface IModuleGeneratorService
{
    Task GenerateModuleAsync(
        string moduleName,
        List<ModuleFieldDto> fields,
        bool runMigration = false,
        bool hasImage = false,
        bool runDbUpdate = false
    );
}