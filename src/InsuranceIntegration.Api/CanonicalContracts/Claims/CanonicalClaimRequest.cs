using System.ComponentModel.DataAnnotations;

namespace InsuranceIntegration.Api.CanonicalContracts.Claims;

public sealed class CanonicalClaimRequest
{
    public Guid EntityId { get; init; }

    [Required]
    public required string ClaimReference { get; init; }

    [Required]
    public required string PolicyReference { get; init; }

    [Required]
    public required string ClaimantName { get; init; }

    [Required]
    public required string SourceSystem { get; init; }

    public DateOnly LossDate { get; init; }

    public DateTime ReportedAtUtc { get; init; }

    public string LossCause { get; init; } = string.Empty;

    public decimal IncurredAmount { get; init; }

    public decimal ReservedAmount { get; init; }

    public decimal PaidAmount { get; init; }

    public string CurrencyCode { get; init; } = "USD";

    public bool FraudIndicator { get; init; }
}
