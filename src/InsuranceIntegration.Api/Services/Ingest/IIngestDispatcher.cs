using InsuranceIntegration.Api.FinalMessages.Ingest;
using InsuranceIntegration.Api.SourceContracts.Ingest;

namespace InsuranceIntegration.Api.Services.Ingest;

public interface IIngestDispatcher
{
    IngestAcceptedResult Dispatch(SourceIngestEnvelope envelope);
}
