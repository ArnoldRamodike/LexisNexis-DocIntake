using DocIntake.Api.Models;

namespace DocIntake.Api.Services.IService;

public interface IQueueService
{
    ValueTask EnqueueAsync(ProcessMessage message);
    IAsyncEnumerable<ProcessMessage> ReadAllAsync(CancellationToken cancellationToken);
}