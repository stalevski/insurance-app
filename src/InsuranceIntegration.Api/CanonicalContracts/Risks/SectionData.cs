namespace InsuranceIntegration.Api.CanonicalContracts.Risks;

public sealed class SectionData
{
    public string SectionCode { get; init; } = string.Empty;

    public string SectionName { get; init; } = string.Empty;

    public List<SubcoverData> Subcovers { get; init; } = [];
}
