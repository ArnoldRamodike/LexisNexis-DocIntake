using DocIntake.Api.Models;

public record DocumentEntity
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Provider { get; init; }
    public string SourceDocumentId { get; init; }
    public string Title { get; init; }
    public string Jurisdiction { get; init; }
    public List<string> Categories { get; init; } = new();
    public List<string> Tags { get; init; } = new();
    public DateTime ReceivedAt { get; init; }
    public string ContentType { get; init; }
    public string FileName { get; init; }
    public string BlobName { get; set; }
    public string Status { get; set; } = "received";
    public string? Preview { get; set; }
    public int? PreviewSize { get; set; }
    public List<StatusEvent> AuditTrail { get; init; } = new();
}