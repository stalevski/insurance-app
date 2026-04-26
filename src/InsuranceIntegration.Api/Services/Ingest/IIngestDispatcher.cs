using InsuranceIntegration.Api.Responses.Ingest;
using InsuranceIntegration.Api.SourceContracts.Ingest;

namespace InsuranceIntegration.Api.Services.Ingest;

public interface IIngestDispatcher
{
    IngestReceipt Dispatch(SourceIngestEnvelope envelope);
}
