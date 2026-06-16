using InsuranceIntegration.Api.CanonicalContracts.Risks;
using InsuranceIntegration.Api.Mappers.Risks;
using InsuranceIntegration.Api.SourceContracts.Ingest;

namespace InsuranceIntegration.Api.Tests.Mappers.Risks;

internal sealed class StubSourceRiskMapper : ISourceRiskMapper
{
    private readonly bool _canMap;
    private readonly CanonicalRiskRequest _response;

    public StubSourceRiskMapper(bool canMap, CanonicalRiskRequest response)
    {
        _canMap = canMap;
        _response = response;
    }

    public bool CanMap(SourceIngestRequest request)
    {
        return _canMap;
    }

    public CanonicalRiskRequest Map(SourceIngestRequest request)
    {
        return _response;
    }
}
