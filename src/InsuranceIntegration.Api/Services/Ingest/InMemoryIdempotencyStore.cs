using System.Collections.Concurrent;
using InsuranceIntegration.Api.FinalMessages.Ingest;

namespace InsuranceIntegration.Api.Services.Ingest;

public sealed class InMemoryIdempotencyStore : IIdempotencyStore
{
    private readonly ConcurrentDictionary<string, IngestAcceptedResult> _entries = new();

    public bool TryGet(string source, string envelopeId, out IngestAcceptedResult? existingResult)
    {
        if (_entries.TryGetValue(BuildKey(source, envelopeId), out var found))
        {
            existingResult = found;
            return true;
        }

        existingResult = null;
        return false;
    }

    public void Store(string source, string envelopeId, IngestAcceptedResult result)
    {
        _entries[BuildKey(source, envelopeId)] = result;
    }

    private static string BuildKey(string source, string envelopeId)
    {
        return $"{source}::{envelopeId}";
    }
}
