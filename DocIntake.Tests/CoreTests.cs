using DocIntake.Api.Models;
using DocIntake.Api.Services;
using DocIntake.Api.Services.IService;
using Microsoft.Extensions.Logging;

namespace DocIntake.Tests;

public class CoreTests
{
    [Fact]
    public void Dedup_Should_Return_Same_Document_On_Repeated_Submission()
    {
        var store = new InMemoryMetadataStore();
        var doc1 = new DocumentEntity
        {
            Provider = "Lexis",
            SourceDocumentId = "12345",
            Title = "Test Doc"
        };

        var first = store.AddOrGetExisting(doc1);
        var second = store.AddOrGetExisting(new DocumentEntity
        {
            Provider = "Lexis",
            SourceDocumentId = "12345",
            Title = "Duplicate"
        });

        Assert.Equal(first.Id, second.Id);
        Assert.Contains(second.AuditTrail, e => e.Status == "duplicate_submission");
    }

    [Fact]
    public async Task HappyPath_Processing_Generates_Preview()
    {
        var store = new InMemoryMetadataStore();
        var queue = new InMemoryQueueService();
        var blobStorage = new FakeBlobStorage();
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<DocumentProcessor>();
        var processor = new DocumentProcessor(queue, blobStorage, store, logger);

        var doc = new DocumentEntity
        {
            Provider = "Test",
            SourceDocumentId = "src1",
            BlobName = "testblob",
            ContentType = "text/plain"
        };
        doc = store.AddOrGetExisting(doc);
        await blobStorage.UploadAsync(doc.BlobName, new MemoryStream("Hello world content"u8.ToArray()), "text/plain");

        var msg = new ProcessMessage
        {
            DocumentId = doc.Id,
            SourceDocumentId = doc.SourceDocumentId,
            Action = "process",
            SubmittedAt = DateTime.UtcNow
        };
        await queue.EnqueueAsync(msg);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await processor.StartAsync(cts.Token);
        await Task.Delay(500);
        cts.Cancel();

        Assert.True(store.TryGet(doc.Id, out var updated));
        Assert.Equal("processed", updated.Status);
        Assert.NotNull(updated.Preview);
        Assert.Contains("Hello world", updated.Preview);
    }

    private class FakeBlobStorage : IBlobStorageService
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public Task<string> UploadAsync(string blobName, Stream content, string contentType)
        {
            using var ms = new MemoryStream();
            content.CopyTo(ms);
            _store[blobName] = ms.ToArray();
            return Task.FromResult($"fake://{blobName}");
        }

        public Task<Stream> DownloadAsync(string blobName)
        {
            if (_store.TryGetValue(blobName, out var data))
                return Task.FromResult<Stream>(new MemoryStream(data));
            throw new FileNotFoundException("Blob not found");
        }

        public Task DeleteAsync(string blobName)
        {
            _store.Remove(blobName);
            return Task.CompletedTask;
        }
    }
}