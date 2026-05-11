using System.Threading.Channels;
using DocIntake.Api.Models;
using DocIntake.Api.Services.IService;

namespace DocIntake.Api.Services;

public class InMemoryQueueService : IQueueService
{
    private readonly Channel<ProcessMessage> _channel = Channel.CreateBounded<ProcessMessage>(100);

    public ValueTask EnqueueAsync(ProcessMessage message)
        => _channel.Writer.WriteAsync(message);

    public IAsyncEnumerable<ProcessMessage> ReadAllAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAllAsync(cancellationToken);
}