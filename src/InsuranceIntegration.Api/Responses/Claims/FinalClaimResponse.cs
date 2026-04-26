namespace InsuranceIntegration.Api.Responses.Claims;

public sealed class FinalClaimResponse
{
    public Guid EntityId { get; init; }

    public string ClaimReference { get; init; } = string.Empty;

    public string PolicyReference { get; init; } = string.Empty;

    public string ClaimantName { get; init; } = string.Empty;

    public string SourceSystem { get; init; } = string.Empty;

    public string Severity { get; init; } = string.Empty;

    public string TriageDecision { get; init; } = string.Empty;

    public string ReserveStatus { get; init; } = string.Empty;

    public bool FraudFlagRaised { get; init; }

    public bool AutoClosed { get; init; }

    public decimal IncurredAmount { get; init; }

    public decimal ReservedAmount { get; init; }

    public decimal OutstandingAmount { get; init; }

    public decimal DeductibleAmount { get; init; }

    public decimal IndemnityAmount { get; init; }

    public bool LimitBreached { get; init; }

    public string? AffectedSectionCode { get; init; }

    public string? AffectedSubcoverCode { get; init; }

    public string? AffectedPerilCode { get; init; }

    public List<string> DecisionReasons { get; init; } = [];

    public string FinalStatus { get; init; } = string.Empty;
}
