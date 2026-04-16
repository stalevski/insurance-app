using InsuranceIntegration.Api.SourceContracts.Ingest;

namespace InsuranceIntegration.Api.Services.Ingest;

public interface IIngestHandler
{
    string Name { get; }

    bool CanHandle(SourceIngestEnvelope envelope);

    object Handle(SourceIngestEnvelope envelope);
}
