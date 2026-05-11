using System;

namespace DocIntake.Api.Models;

public record StatusEvent
{
    public string Status { get; init; }
    public DateTime Timestamp { get; init; }
    public string? Message { get; init; }
}
