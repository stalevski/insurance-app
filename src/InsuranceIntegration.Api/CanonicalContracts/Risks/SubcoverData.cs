namespace InsuranceIntegration.Api.CanonicalContracts.Risks;

public sealed class SubcoverData
{
    public string SubcoverCode { get; init; } = string.Empty;

    public string SubcoverName { get; init; } = string.Empty;

    public decimal SumInsured { get; init; }

    public decimal? PerOccurrenceLimit { get; init; }

    public decimal? AggregateLimit { get; init; }

    public decimal Deductible { get; init; }

    public string? DeductibleType { get; init; }

    public decimal Premium { get; init; }

    public List<PerilData> Perils { get; init; } = [];

    public List<string> Exclusions { get; init; } = [];
}
