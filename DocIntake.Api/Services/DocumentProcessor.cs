using System.Text;
using DocIntake.Api.Models;
using DocIntake.Api.Services.IService;

namespace DocIntake.Api.Services;

public class DocumentProcessor : BackgroundService
{
    private readonly IQueueService _queue;
    private readonly IBlobStorageService _blobStorage;
    private readonly IMetadataStore _store;
    private readonly ILogger<DocumentProcessor> _logger;

    public DocumentProcessor(
        IQueueService queue,
        IBlobStorageService blobStorage,
        IMetadataStore store,
        ILogger<DocumentProcessor> logger)
    {
        _queue = queue;
        _blobStorage = blobStorage;
        _store = store;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in _queue.ReadAllAsync(stoppingToken))
        {
            if (!_store.TryGet(message.DocumentId, out var doc))
            {
                _logger.LogWarning("Document {Id} not found for processing", message.DocumentId);
                continue;
            }

            doc.Status = "processing";
            doc.AuditTrail.Add(new StatusEvent
            {
                Status = "processing",
                Timestamp = DateTime.UtcNow,
                Message = "Started processing"
            });
            _store.Update(doc);

            try
            {
                await using var stream = await _blobStorage.DownloadAsync(doc.BlobName);
                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
                var content = await reader.ReadToEndAsync();

                // Generate simple preview: first 500 characters
                var preview = content.Length <= 500 ? content : content[..500] + "...";
                doc.Preview = preview;
                doc.PreviewSize = Encoding.UTF8.GetByteCount(preview);
                doc.Status = "processed";
                doc.AuditTrail.Add(new StatusEvent
                {
                    Status = "processed",
                    Timestamp = DateTime.UtcNow,
                    Message = "Preview generated"
                });
                _logger.LogInformation("Processed document {Id}", doc.Id);
            }
            catch (Exception ex)
            {
                doc.Status = "failed";
                doc.AuditTrail.Add(new StatusEvent
                {
                    Status = "failed",
                    Timestamp = DateTime.UtcNow,
                    Message = $"Processing failed: {ex.Message}"
                });
                _logger.LogError(ex, "Failed to process document {Id}", doc.Id);
            }
            finally
            {
                _store.Update(doc);
            }
        }
    }
}