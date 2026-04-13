using System.ComponentModel.DataAnnotations;

namespace InsuranceIntegration.Api.SourceContracts.Risks;

public sealed class QuoteForgeQuoteRequestPayload
{
    [Required]
    public required string QuoteReference { get; init; }

    [Required]
    public required string InsuredName { get; init; }

    [Required]
    public required string ProductLine { get; init; }

    public string? BrokerCode { get; init; }

    public string? BrokerName { get; init; }

    public decimal TechnicalPremium { get; init; }

    public decimal? BrokerPremium { get; init; }

    public string CurrencyCode { get; init; } = "USD";

    public DateOnly? EffectiveDate { get; init; }

    public DateOnly? ExpiryDate { get; init; }

    public int UnderwritingYear { get; init; }

    public decimal? InsuredRevenue { get; init; }

    public int InsuredEmployeeCount { get; init; }

    public int InsuredYearsInBusiness { get; init; }
}
