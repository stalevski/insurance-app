using System.Collections.Concurrent;
using InsuranceIntegration.Api.Responses.Ingest;

namespace InsuranceIntegration.Api.Services.Ingest;

public sealed class InMemoryIdempotencyStore : IIdempotencyStore
{
    private readonly ConcurrentDictionary<string, IngestReceipt> _entries = new();

    public bool TryGet(string source, string envelopeId, out IngestReceipt? existingReceipt)
    {
        if (_entries.TryGetValue(BuildKey(source, envelopeId), out var found))
        {
            existingReceipt = found;
            return true;
        }

        existingReceipt = null;
        return false;
    }

    public IngestReceipt? Find(string source, string envelopeId)
    {
        return _entries.TryGetValue(BuildKey(source, envelopeId), out var found) ? found : null;
    }

    public void Store(string source, string envelopeId, IngestReceipt receipt)
    {
        _entries[BuildKey(source, envelopeId)] = receipt;
    }

    private static string BuildKey(string source, string envelopeId)
    {
        return $"{source}::{envelopeId}";
    }
}
