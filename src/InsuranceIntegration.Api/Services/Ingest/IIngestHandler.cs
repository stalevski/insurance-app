using InsuranceIntegration.Api.SourceContracts.Ingest;

namespace InsuranceIntegration.Api.Services.Ingest;

public interface IIngestHandler
{
    string Name { get; }

    bool CanHandle(SourceIngestEnvelope envelope);

    Task<object> HandleAsync(SourceIngestEnvelope envelope, CancellationToken cancellationToken = default);
}
