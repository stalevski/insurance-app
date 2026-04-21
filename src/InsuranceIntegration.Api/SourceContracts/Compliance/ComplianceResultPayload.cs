using System.ComponentModel.DataAnnotations;

namespace InsuranceIntegration.Api.SourceContracts.Compliance;

public sealed class ComplianceResultPayload
{
    [Required]
    public required string PartyName { get; init; }

    [Required]
    public required string ScreeningResult { get; init; }

    public int Score { get; init; }

    public bool IsPoliticallyExposed { get; init; }

    public bool HasSanctionsHit { get; init; }

    public string? EntityReference { get; init; }
}
