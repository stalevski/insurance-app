using InsuranceIntegration.Api.CanonicalContracts.Risks;
using InsuranceIntegration.Api.SourceContracts.Ingest;

namespace InsuranceIntegration.Api.Mappers.Risks;

public interface IRiskIngestMapper
{
    CanonicalRiskRequest Map(SourceIngestRequest request);
}
