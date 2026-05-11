using System.Collections.Concurrent;
using DocIntake.Api.Services.IService;

namespace DocIntake.Api.Services;

public class InMemoryBlobStorageService : IBlobStorageService
{
    private readonly ConcurrentDictionary<string, byte[]> _blobs = new();

    public Task<string> UploadAsync(string blobName, Stream content, string contentType)
    {
        using var ms = new MemoryStream();
        content.CopyTo(ms);
        _blobs[blobName] = ms.ToArray();
        return Task.FromResult($"http://localhost:5000/inmem/{blobName}");
    }

    public Task<Stream> DownloadAsync(string blobName)
    {
        if (_blobs.TryGetValue(blobName, out var data))
            return Task.FromResult<Stream>(new MemoryStream(data));
        throw new FileNotFoundException("Blob not found", blobName);
    }

    public Task DeleteAsync(string blobName)
    {
        _blobs.TryRemove(blobName, out _);
        return Task.CompletedTask;
    }
}
