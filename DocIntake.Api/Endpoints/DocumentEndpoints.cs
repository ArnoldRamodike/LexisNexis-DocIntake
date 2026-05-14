using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using DocIntake.Api.Models;
using DocIntake.Api.Services;
using DocIntake.Api.Services.IService;

namespace DocIntake.Api.Endpoints;

public static class DocumentEndpoints
{
    public static void MapDocumentEndpoints(this WebApplication app)
    {
        var apiGroup = app.MapGroup("/api/");
        // POST /documents
        apiGroup.MapPost("documents", async (
            [FromForm] string? provider,
            [FromForm] string? sourceDocumentId,
            [FromForm] string? title,
            [FromForm] string? jurisdiction,
            [FromForm] string? categories,
            [FromForm] string? tags,
            [FromForm] DateTime? receivedAt,
            IFormFile? file,
            IMetadataStore store,
            IBlobStorageService blobStorage,
            IQueueService queue) =>
        {
            if (file is null || file.Length == 0)
                return Results.BadRequest(new { error = "File is required." });
            if (string.IsNullOrWhiteSpace(provider))
                return Results.BadRequest(new { error = "Provider is required." });
            if (string.IsNullOrWhiteSpace(sourceDocumentId))
                return Results.BadRequest(new { error = "SourceDocumentId is required." });
            if (string.IsNullOrWhiteSpace(title))
                return Results.BadRequest(new { error = "Title is required." });
            if (string.IsNullOrWhiteSpace(jurisdiction))
                return Results.BadRequest(new { error = "Jurisdiction is required." });

            // Dedup check
            var existingId = store.GetExistingId(provider, sourceDocumentId);
            if (existingId is not null && store.TryGet(existingId, out var existingDoc))
            {
                // Duplicate submission – just return existing
                return Results.Ok(MapToDto(existingDoc));
            }

            // Create new document record
            var doc = new DocumentEntity
            {
                Provider = provider,
                SourceDocumentId = sourceDocumentId,
                Title = title,
                Jurisdiction = jurisdiction,
                Categories = categories?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList() ?? new(),
                Tags = tags?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList() ?? new(),
                ReceivedAt = receivedAt ?? DateTime.UtcNow,
                ContentType = file.ContentType,
                FileName = file.FileName,
                BlobName = $"{Guid.NewGuid()}/{file.FileName}"
            };

            // Audit: received
            doc.AuditTrail.Add(new StatusEvent
            {
                Status = "received",
                Timestamp = DateTime.UtcNow,
                Message = "Document received"
            });

            // Store raw file
            using var stream = file.OpenReadStream();
            await blobStorage.UploadAsync(doc.BlobName, stream, doc.ContentType);
            doc.AuditTrail.Add(new StatusEvent
            {
                Status = "stored",
                Timestamp = DateTime.UtcNow,
                Message = "Stored in blob storage"
            });

            // Persist metadata (handles dedup finally)
            doc = store.AddOrGetExisting(doc);

            // Audit: queued (before enqueue to avoid race with background processor)
            doc.AuditTrail.Add(new StatusEvent
            {
                Status = "queued",
                Timestamp = DateTime.UtcNow,
                Message = "Queued for processing"
            });
            store.Update(doc);

            // Enqueue processing message
            var processMsg = new ProcessMessage
            {
                DocumentId = doc.Id,
                SourceDocumentId = doc.SourceDocumentId,
                Action = "process",
                SubmittedAt = DateTime.UtcNow
            };
            await queue.EnqueueAsync(processMsg);

            return Results.Created($"/documents/{doc.Id}", MapToDto(doc));
        }).DisableAntiforgery(); // for form file uploads in development

        // GET /documents/{id}
        apiGroup.MapGet("documents/{id}", (string id, IMetadataStore store) =>
        {
            if (store.TryGet(id, out var doc))
                return Results.Ok(MapToDto(doc));
            return Results.NotFound();
        });

        // GET /documents/{id}/content
        apiGroup.MapGet("documents/{id}/content", async (string id, IMetadataStore store, IBlobStorageService blobStorage) =>
        {
            if (!store.TryGet(id, out var doc))
                return Results.NotFound();
            var stream = await blobStorage.DownloadAsync(doc.BlobName);
            return Results.File(stream, doc.ContentType, doc.FileName);
        });

        // GET /documents/{id}/preview
        apiGroup.MapGet("documents/{id}/preview", (string id, IMetadataStore store) =>
        {
            if (store.TryGet(id, out var doc))
            {
                if (doc.Status == "processed" && doc.Preview is not null)
                    return Results.Text(doc.Preview);
                if (doc.Status == "failed")
                    return Results.Problem("Processing failed.", statusCode: 500);
                return Results.Ok(new { status = doc.Status, message = "Preview not yet available" });
            }
            return Results.NotFound();
        });

        // GET /documents?provider=...&tag=... (simple listing)
        apiGroup.MapGet("documents", (string? provider, string? tag, IMetadataStore store) =>
        {
            var docs = store.List(provider, tag)
                           .Select(MapToDto);
            return Results.Ok(docs);
        });
    }

    // DTO to hide internal details
    private static object MapToDto(DocumentEntity doc) => new
    {
        doc.Id,
        doc.Provider,
        doc.SourceDocumentId,
        doc.Title,
        doc.Jurisdiction,
        doc.Categories,
        doc.Tags,
        doc.ReceivedAt,
        doc.ContentType,
        doc.FileName,
        doc.Status,
        PreviewSize = doc.PreviewSize,
        AuditTrail = doc.AuditTrail.Select(a => new { a.Status, a.Timestamp, a.Message })
    };
}
