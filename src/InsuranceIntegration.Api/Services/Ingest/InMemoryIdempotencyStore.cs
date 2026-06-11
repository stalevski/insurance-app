using System.Collections.Concurrent;
using InsuranceIntegration.Api.Responses.Ingest;

namespace InsuranceIntegration.Api.Services.Ingest;

public sealed class InMemoryIdempotencyStore : IIdempotencyStore
{
    private readonly ConcurrentDictionary<string, IngestReceipt> _entries = new();

    public Task<IngestReceipt?> FindAsync(string source, string envelopeId, CancellationToken cancellationToken = default)
    {
        _entries.TryGetValue(BuildKey(source, envelopeId), out var found);
        return Task.FromResult<IngestReceipt?>(found);
    }

    public Task<IngestReceipt> StoreAsync(string source, string envelopeId, IngestReceipt receipt, CancellationToken cancellationToken = default)
    {
        var stored = _entries.GetOrAdd(BuildKey(source, envelopeId), receipt);
        return Task.FromResult(stored);
    }

    private static string BuildKey(string source, string envelopeId)
    {
        return $"{source}::{envelopeId}";
    }
}
