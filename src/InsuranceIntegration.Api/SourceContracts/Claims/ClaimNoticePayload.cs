using System.ComponentModel.DataAnnotations;

namespace InsuranceIntegration.Api.SourceContracts.Claims;

public sealed class ClaimNoticePayload
{
    [Required]
    public required string ClaimReference { get; init; }

    [Required]
    public required string PolicyReference { get; init; }

    [Required]
    public required string ClaimantName { get; init; }

    public DateOnly LossDate { get; init; }

    public string LossCause { get; init; } = string.Empty;

    public decimal EstimatedIncurred { get; init; }

    public decimal EstimatedReserved { get; init; }

    public decimal PaidAmount { get; init; }

    public string CurrencyCode { get; init; } = "USD";

    public bool FraudIndicator { get; init; }
}
