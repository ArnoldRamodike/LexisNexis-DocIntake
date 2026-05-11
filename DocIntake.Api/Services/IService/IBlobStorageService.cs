namespace DocIntake.Api.Services.IService;

public interface IBlobStorageService
{
    Task<string> UploadAsync(string blobName, Stream content, string contentType);
    Task<Stream> DownloadAsync(string blobName);
    Task DeleteAsync(string blobName);
}