namespace UserApp.Application.Common.Interfaces;

public interface IMediaPipeline
{
    Task HandleCreateAsync(string entityName, object entity, object? file);
    Task HandleUpdateAsync(string entityName, object entity, object? file);
    Task HandleDeleteAsync(string entityName, object entity);
}