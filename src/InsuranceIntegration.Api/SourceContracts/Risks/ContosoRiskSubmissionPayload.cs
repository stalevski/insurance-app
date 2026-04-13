using System.ComponentModel.DataAnnotations;

namespace InsuranceIntegration.Api.SourceContracts.Risks;

public sealed class ContosoRiskSubmissionPayload
{
    [Required]
    public required string QuoteId { get; init; }

    [Required]
    public required string InsuredName { get; init; }

    [Required]
    public required string Trade { get; init; }

    public decimal EstimatedPremium { get; init; }
}
