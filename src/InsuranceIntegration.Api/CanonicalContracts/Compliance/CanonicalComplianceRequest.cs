using System.ComponentModel.DataAnnotations;

namespace InsuranceIntegration.Api.CanonicalContracts.Compliance;

public sealed class CanonicalComplianceRequest
{
    public Guid EntityId { get; init; }

    [Required]
    public required string PartyName { get; init; }

    [Required]
    public required string SourceSystem { get; init; }

    public string? EntityReference { get; init; }

    public string ScreeningResult { get; init; } = string.Empty;

    public int Score { get; init; }

    public bool IsPoliticallyExposed { get; init; }

    public bool HasSanctionsHit { get; init; }
}
