using System.ComponentModel.DataAnnotations;

namespace InsuranceIntegration.Api.SourceContracts.Risks;

public sealed class BindPointPolicyBindPayload
{
    [Required]
    public required string PolicyReference { get; init; }

    [Required]
    public required string QuoteReference { get; init; }

    [Required]
    public required string InsuredName { get; init; }

    [Required]
    public required string ProductCode { get; init; }

    public string? BrokerCode { get; init; }

    public string? BrokerName { get; init; }

    public bool BrokerHasDelegatedAuthority { get; init; }

    public bool BrokerIsPreferredPartner { get; init; }

    public decimal BoundPremium { get; init; }

    public string CurrencyCode { get; init; } = "USD";

    public DateOnly InceptionDate { get; init; }

    public DateOnly ExpiryDate { get; init; }

    public DateOnly? BoundDate { get; init; }

    public string? PaymentMethod { get; init; }

    public int InstallmentCount { get; init; }
}
