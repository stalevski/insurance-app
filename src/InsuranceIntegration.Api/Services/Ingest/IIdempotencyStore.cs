using InsuranceIntegration.Api.Responses.Ingest;

namespace InsuranceIntegration.Api.Services.Ingest;

public interface IIdempotencyStore
{
    Task<IngestReceipt?> FindAsync(string source, string envelopeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically stores the receipt for <paramref name="source"/> + <paramref name="envelopeId"/>.
    /// If a concurrent request already stored a receipt for the same key (TOCTOU race between
    /// <see cref="FindAsync"/> and this call), the first writer wins and the existing receipt is
    /// returned. Callers must use the returned receipt as the canonical outcome.
    /// </summary>
    Task<IngestReceipt> StoreAsync(string source, string envelopeId, IngestReceipt receipt, CancellationToken cancellationToken = default);
}
