using System.Threading.Channels;
using UserApp.Application.Common.Interfaces;

namespace UserApp.Infrastructure.Background;

public sealed class EmailTaskQueue : IEmailTaskQueue
{
    private readonly Channel<Func<IServiceProvider, CancellationToken, Task>> _channel;

    public EmailTaskQueue(int capacity = 100)
    {
        _channel = Channel.CreateBounded<Func<IServiceProvider, CancellationToken, Task>>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });
    }

    public async ValueTask EnqueueAsync(Func<IServiceProvider, CancellationToken, Task> task, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(task, cancellationToken);
    }

    public async ValueTask<Func<IServiceProvider, CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _channel.Reader.ReadAsync(cancellationToken);
    }
}
