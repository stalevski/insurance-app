using System.ComponentModel.DataAnnotations;

namespace InsuranceIntegration.Api.CanonicalContracts.Risks;

public sealed class CanonicalRiskRequest
{
    public Guid EntityId { get; init; }

    [Required]
    public required string ExternalReference { get; init; }

    [Required]
    public required string ProductCode { get; init; }

    [Required]
    public required string SourceSystem { get; init; }

    [Required]
    public required string TransactionType { get; init; }

    public string? SchemeCode { get; init; }

    public DateTime TransactionTimestampUtc { get; init; }

    public DateOnly? BoundDate { get; init; }

    public string? LifecycleStatus { get; init; }

    public decimal? AnnualizedGrossPremium { get; init; }

    public string CurrencyCode { get; init; } = "USD";

    public string? UnderwriterName { get; init; }

    public string? PaymentMethod { get; init; }

    public SubmissionData Submission { get; init; } = new();

    public BrokerData Broker { get; init; } = new();

    public InsuredData Insured { get; init; } = new();

    public QuoteData Quote { get; init; } = new();

    public PolicyData Policy { get; init; } = new();

    public ClearanceData Clearance { get; init; } = new();

    public List<EnrichmentItem> Enrichments { get; init; } = [];

    public List<ContractCheck> ContractChecks { get; init; } = [];

    public List<ComplianceCheck> ComplianceChecks { get; init; } = [];

    public List<PartyData> Parties { get; init; } = [];

    public List<ClaimData> Claims { get; init; } = [];

    public List<SectionData> Sections { get; init; } = [];

    public List<SectionOperation> SectionOperations { get; init; } = [];

    public List<InstallmentData> Installments { get; init; } = [];
}
