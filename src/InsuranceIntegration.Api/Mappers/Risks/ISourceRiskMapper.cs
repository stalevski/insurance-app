using InsuranceIntegration.Api.CanonicalContracts.Risks;
using InsuranceIntegration.Api.SourceContracts.Ingest;

namespace InsuranceIntegration.Api.Mappers.Risks;

public interface ISourceRiskMapper
{
    bool CanMap(SourceIngestRequest request);

    CanonicalRiskRequest Map(SourceIngestRequest request);
}
