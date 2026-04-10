namespace InsuranceIntegration.Api.CanonicalContracts.Risks;

public sealed class SectionOperation
{
    public string SectionCode { get; init; } = string.Empty;

    public string OperationType { get; init; } = string.Empty;

    public string? SubcoverCode { get; init; }

    public bool RemoveAllSubcovers { get; init; }
}
