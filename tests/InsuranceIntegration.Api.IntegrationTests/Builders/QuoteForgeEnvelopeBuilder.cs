using System.Text.Json;
using InsuranceIntegration.Api.SourceContracts.Ingest;

namespace InsuranceIntegration.Api.IntegrationTests.Builders;

/// <summary>
/// Fluent builder for a QuoteForge <see cref="SourceIngestEnvelope"/> (<c>Source=QUOTEFORGE</c>,
/// <c>Type=QuoteRequest</c>) — the shape accepted by <c>POST /api/v1/ingest</c>. The envelope wraps
/// a QuoteForge-native JSON payload that the QuoteForge mapper translates into the canonical model.
/// </summary>
public sealed class QuoteForgeEnvelopeBuilder
{
    private string _envelopeId = $"qf-{Guid.NewGuid():N}";
    private string _quoteReference = "QT-9001";
    private string _insuredName = "Harborline Services";
    private string _productLine = "Liability";
    private string _brokerCode = "BRK-44";
    private string _brokerName = "Summit Risk Partners";
    private decimal _technicalPremium = 8_200m;
    private decimal _brokerPremium = 8_500m;
    private string _currencyCode = "USD";
    private string _effectiveDate = "2026-05-01";
    private string _expiryDate = "2027-04-30";
    private int _underwritingYear = 2026;
    private decimal _insuredRevenue = 1_500_000m;
    private int _insuredEmployeeCount = 25;
    private int _insuredYearsInBusiness = 8;
    private string? _correlationId = "corr-quoteforge";

    public QuoteForgeEnvelopeBuilder WithEnvelopeId(string envelopeId)
    {
        _envelopeId = envelopeId;
        return this;
    }

    public QuoteForgeEnvelopeBuilder WithQuoteReference(string quoteReference)
    {
        _quoteReference = quoteReference;
        return this;
    }

    public QuoteForgeEnvelopeBuilder WithInsuredName(string insuredName)
    {
        _insuredName = insuredName;
        return this;
    }

    public QuoteForgeEnvelopeBuilder WithProductLine(string productLine)
    {
        _productLine = productLine;
        return this;
    }

    public QuoteForgeEnvelopeBuilder WithPremiums(decimal technicalPremium, decimal brokerPremium)
    {
        _technicalPremium = technicalPremium;
        _brokerPremium = brokerPremium;
        return this;
    }

    public QuoteForgeEnvelopeBuilder WithCorrelationId(string? correlationId)
    {
        _correlationId = correlationId;
        return this;
    }

    /// <summary>The envelope id, which doubles as the idempotency key for this source.</summary>
    public string EnvelopeId => _envelopeId;

    /// <summary>The quote reference carried in the payload (the canonical external reference).</summary>
    public string QuoteReference => _quoteReference;

    public SourceIngestEnvelope Build()
    {
        var payload = JsonSerializer.SerializeToElement(new
        {
            quoteReference = _quoteReference,
            insuredName = _insuredName,
            productLine = _productLine,
            brokerCode = _brokerCode,
            brokerName = _brokerName,
            technicalPremium = _technicalPremium,
            brokerPremium = _brokerPremium,
            currencyCode = _currencyCode,
            effectiveDate = _effectiveDate,
            expiryDate = _expiryDate,
            underwritingYear = _underwritingYear,
            insuredRevenue = _insuredRevenue,
            insuredEmployeeCount = _insuredEmployeeCount,
            insuredYearsInBusiness = _insuredYearsInBusiness,
        });

        return new SourceIngestEnvelope
        {
            Id = _envelopeId,
            Source = "QUOTEFORGE",
            Type = "QuoteRequest",
            SchemaVersion = "1.0",
            OccurredAtUtc = new DateTime(2026, 4, 24, 9, 0, 0, DateTimeKind.Utc),
            CorrelationId = _correlationId,
            Data = payload,
        };
    }
}
