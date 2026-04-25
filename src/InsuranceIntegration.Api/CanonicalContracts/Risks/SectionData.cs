namespace InsuranceIntegration.Api.CanonicalContracts.Risks;

public sealed class SectionData
{
    public string SectionCode { get; init; } = string.Empty;

    public string SectionName { get; init; } = string.Empty;

    public string Status { get; init; } = SectionStatus.Active;

    public decimal SumInsured { get; init; }

    public decimal SectionPremium { get; init; }

    public List<SubcoverData> Subcovers { get; init; } = [];

    public List<string> Warranties { get; init; } = [];

    public List<string> SpecialConditions { get; init; } = [];
}
