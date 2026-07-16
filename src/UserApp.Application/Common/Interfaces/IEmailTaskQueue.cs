namespace UserApp.Application.Common.Interfaces;

public interface IEmailTaskQueue
{
    ValueTask EnqueueAsync(Func<IServiceProvider, CancellationToken, Task> task, CancellationToken cancellationToken = default);
}
