using InsuranceIntegration.Api.CanonicalContracts.Risks;
using InsuranceIntegration.Api.SourceContracts.Ingest;

namespace InsuranceIntegration.Api.Mappers.Risks;

public sealed class RiskIngestMapper : IRiskIngestMapper
{
    private readonly IReadOnlyCollection<ISourceRiskMapper> _mappers;

    public RiskIngestMapper(IEnumerable<ISourceRiskMapper> mappers)
    {
        _mappers = mappers.ToArray();
    }

    public CanonicalRiskRequest Map(SourceIngestRequest request)
    {
        var mapper = _mappers.FirstOrDefault(candidate => candidate.CanMap(request));

        if (mapper is null)
        {
            throw new InvalidOperationException($"No risk mapper registered for source '{request.SourceSystem}' and message type '{request.MessageType}'.");
        }

        return mapper.Map(request);
    }
}
