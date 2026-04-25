namespace InsuranceIntegration.Api.Services.Policies;

public sealed class SectionEndorsementOperation
{
    public string OperationType { get; init; } = string.Empty;

    public string SectionCode { get; init; } = string.Empty;

    public string? SubcoverCode { get; init; }

    public decimal SumInsuredDelta { get; init; }

    public decimal DeductibleDelta { get; init; }

    public decimal PremiumDelta { get; init; }

    public string? Reason { get; init; }
}

public static class SectionEndorsementOperationType
{
    public const string AddSection = "AddSection";
    public const string RemoveSection = "RemoveSection";
    public const string AddSubcover = "AddSubcover";
    public const string RemoveSubcover = "RemoveSubcover";
    public const string UpdateLimit = "UpdateLimit";
    public const string UpdateDeductible = "UpdateDeductible";
}
