using InsuranceIntegration.Api.Responses.Ingest;

namespace InsuranceIntegration.Api.Services.Ingest;

public interface IIdempotencyStore
{
    Task<IngestReceipt?> FindAsync(string source, string envelopeId, CancellationToken cancellationToken = default);

    Task StoreAsync(string source, string envelopeId, IngestReceipt receipt, CancellationToken cancellationToken = default);
}
