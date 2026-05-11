using System;

namespace DocIntake.Api.Models;

public record SubmissionRequest
{
    public string Provider { get; init; }
    public string SourceDocumentId { get; init; }
    public string Title { get; init; }
    public string Jurisdiction { get; init; }
    public string Categories { get; init; }
    public string Tags { get; init; }
    public DateTime? ReceivedAt { get; init; }
    public IFormFile File { get; init; }
}
