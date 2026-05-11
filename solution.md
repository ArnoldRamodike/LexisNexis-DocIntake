### **SOLUTION.md**

```markdown
# Solution Design & Trade-offs

## Architecture

- **Minimal API** (.NET 8) for simplicity; no controllers or heavy layers.
- **Cloud abstraction**: Interfaces for blob storage and queue. Current implementations use Azure.Storage.Blobs (against Azurite) and an in‑memory `Channel` for the queue.
- **In‑memory metadata store** (`ConcurrentDictionary`) – keeps state for the exercise, easily replaceable by a database.
- **Deduplication** by composite key (`provider` + `sourceDocumentId`). Repeated submissions update the audit trail but do not create new records.
- **Background processing**: `DocumentProcessor` hosted service reads from the channel, downloads the blob, generates a preview (first 500 chars).

## Trade-offs

- **Queue**: Using a local channel instead of Azure Service Bus. This removes an external dependency for local runs, yet swapping to Service Bus is trivial because of the `IQueueService` abstraction.
- **Storage**: Azurite emulator keeps the design 100% Azure‑compatible. For true local testing no cloud account is needed.
- **Preview generation**: Simple string truncation; in a real system we’d use a proper document parser (e.g. Azure Cognitive Services for richer summaries).
- **Error handling**: Basic try/catch with audit events. A production service would use a dead‑letter queue and retry policies.
- **Testing**: Two focused unit tests; integration with actual Azurite is not required.

## CI/CD

GitHub Actions builds and runs unit tests on every push. Docker Compose provides a complete local environment.
```

## What I Would Add Next

1. **Database persistence** — replace `InMemoryDocumentRepository` with an EF Core implementation (PostgreSQL/SQL Server) or a DynamoDB adapter.
2. **Real cloud storage** — implement `IObjectStorage` with `BlobServiceClient`.
3. **Real queue** — implement `IMessageQueue` with `ServiceBusClient`, including dead-letter handling.
4. **Authentication** — API key middleware or JWT bearer tokens.
5. **Idempotency on processing** — version/ETag checking before re-storing to avoid unnecessary writes on rapid duplicate submissions.
6. **Richer preview** — integrate a document intelligence API for true content summarisation.
7. **Outbound status webhook** — post a `DocumentProcessed` event to a caller-registered webhook URL after processing completes (stub exists as an optional extension point in the worker).
8. **Pagination** — cursor-based pagination on the list endpoint.
9. **Data Tranfer Object** — To safely transfer only the required data between application layers or APIs without exposing internal domain models..
10. **Health checks** — `AddHealthChecks()` with storage and queue probes for Kubernetes readiness/liveness.
