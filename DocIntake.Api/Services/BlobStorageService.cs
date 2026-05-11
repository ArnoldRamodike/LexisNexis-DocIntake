using Azure.Storage.Blobs;
using DocIntake.Api.Services.IService;

namespace DocIntake.Api.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _container;

    public BlobStorageService(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("BlobStorage")
                               ?? "UseDevelopmentStorage=true";
        var containerName = configuration["BlobStorage:ContainerName"] ?? "documents";
        var blobServiceClient = new BlobServiceClient(connectionString);
        _container = blobServiceClient.GetBlobContainerClient(containerName);
        _container.CreateIfNotExists();
    }

    public async Task<string> UploadAsync(string blobName, Stream content, string contentType)
    {
        var blobClient = _container.GetBlobClient(blobName);
        await blobClient.UploadAsync(content, new Azure.Storage.Blobs.Models.BlobUploadOptions
        {
            HttpHeaders = new Azure.Storage.Blobs.Models.BlobHttpHeaders
            {
                ContentType = contentType
            }
        });
        return blobClient.Uri.ToString();
    }

    public async Task<Stream> DownloadAsync(string blobName)
    {
        var blobClient = _container.GetBlobClient(blobName);
        var response = await blobClient.DownloadAsync();
        return response.Value.Content;
    }

    public Task DeleteAsync(string blobName)
    {
        var blobClient = _container.GetBlobClient(blobName);
        return blobClient.DeleteIfExistsAsync();
    }
}