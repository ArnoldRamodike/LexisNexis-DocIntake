using System.Collections.Concurrent;
using DocIntake.Api.Models;
using DocIntake.Api.Services.IService;

namespace DocIntake.Api.Services;

public class InMemoryMetadataStore : IMetadataStore
{
    private readonly ConcurrentDictionary<string, DocumentEntity> _documents = new();
    private readonly ConcurrentDictionary<(string Provider, string SourceDocumentId), string> _dedupMap = new();

    public bool TryGet(string documentId, out DocumentEntity doc)
        => _documents.TryGetValue(documentId, out doc!);

    public DocumentEntity AddOrGetExisting(DocumentEntity doc)
    {
        var key = (doc.Provider, doc.SourceDocumentId);
        var existingId = _dedupMap.GetOrAdd(key, doc.Id);
        if (existingId != doc.Id)
        {
            // duplicate submission – add audit event and return existing
            if (_documents.TryGetValue(existingId, out var existing))
            {
                existing.AuditTrail.Add(new StatusEvent
                {
                    Status = "duplicate_submission",
                    Timestamp = DateTime.UtcNow,
                    Message = $"Duplicate submission received at {DateTime.UtcNow:O}"
                });
                return existing;
            }
        }

        _documents[doc.Id] = doc;
        return doc;
    }

    public void Update(DocumentEntity doc)
    {
        _documents[doc.Id] = doc;
    }

    public IEnumerable<DocumentEntity> List(string? provider, string? tag)
    {
        var query = _documents.Values.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(provider))
            query = query.Where(d => d.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(tag))
            query = query.Where(d => d.Tags.Any(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase)));
        return query;
    }

    public string? GetExistingId(string provider, string sourceDocumentId)
    {
        var key = (provider, sourceDocumentId);
        return _dedupMap.TryGetValue(key, out var id) ? id : null;
    }
}