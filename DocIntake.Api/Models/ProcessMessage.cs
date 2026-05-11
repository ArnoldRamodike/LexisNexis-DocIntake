namespace DocIntake.Api.Models;

public record ProcessMessage
{
    public string DocumentId { get; init; } = default!;
    public string SourceDocumentId { get; init; } = default!;
    public string Action { get; init; } = "process";
    public DateTime SubmittedAt { get; init; }
}