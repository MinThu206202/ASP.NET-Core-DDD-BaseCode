namespace UserApp.Application.Common.Interfaces;

public interface IModuleGeneratorService
{
    Task GenerateModuleAsync(string moduleName);
}