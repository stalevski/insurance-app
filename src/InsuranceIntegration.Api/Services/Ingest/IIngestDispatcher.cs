using InsuranceIntegration.Api.Responses.Ingest;
using InsuranceIntegration.Api.SourceContracts.Ingest;

namespace InsuranceIntegration.Api.Services.Ingest;

public interface IIngestDispatcher
{
    Task<IngestReceipt> DispatchAsync(SourceIngestEnvelope envelope, CancellationToken cancellationToken = default);
}
