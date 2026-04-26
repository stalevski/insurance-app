namespace InsuranceIntegration.Api.Responses.Compliance;

public sealed class FinalComplianceResponse
{
    public Guid EntityId { get; init; }

    public string PartyName { get; init; } = string.Empty;

    public string SourceSystem { get; init; } = string.Empty;

    public string? EntityReference { get; init; }

    public string Decision { get; init; } = string.Empty;

    public bool BlocksBind { get; init; }

    public bool RequiresEnhancedDueDiligence { get; init; }

    public List<string> DecisionReasons { get; init; } = [];

    public string FinalStatus { get; init; } = string.Empty;
}
