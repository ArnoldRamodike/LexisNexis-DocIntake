using DocIntake.Api.Models;

namespace DocIntake.Api.Services.IService;

public interface IMetadataStore
{
    bool TryGet(string documentId, out DocumentEntity doc);
    DocumentEntity AddOrGetExisting(DocumentEntity doc);  // handles dedup
    void Update(DocumentEntity doc);
    IEnumerable<DocumentEntity> List(string? provider, string? tag);
    string? GetExistingId(string provider, string sourceDocumentId);
}