using InsuranceIntegration.Api.Mappers.Risks;
using InsuranceIntegration.Api.SourceContracts.Ingest;
using System.Text.Json;

namespace InsuranceIntegration.Api.Tests.Mappers.Risks;

public sealed class QuoteForgeRiskMapperTests
{
    [Test]
    public void CanMap_ReturnsTrueForQuoteRequestFromQuoteForge()
    {
        var mapper = new QuoteForgeRiskMapper();

        var result = mapper.CanMap(CreateRequest());

        Assert.That(result, Is.True);
    }

    [Test]
    public void Map_TransformsQuoteForgePayloadIntoCanonicalQuoteRequest()
    {
        var mapper = new QuoteForgeRiskMapper();

        var result = mapper.Map(CreateRequest());

        Assert.That(result.ExternalReference, Is.EqualTo("QT-2001"));
        Assert.That(result.SourceSystem, Is.EqualTo("QUOTEFORGE"));
        Assert.That(result.TransactionType, Is.EqualTo("Quote"));
        Assert.That(result.ProductCode, Is.EqualTo("LIABILITY"));
        Assert.That(result.Submission.TechnicalPremium, Is.EqualTo(8200m));
        Assert.That(result.Submission.BrokerPremium, Is.EqualTo(8500m));
        Assert.That(result.Quote.QuoteReference, Is.EqualTo("QT-2001"));
        Assert.That(result.Insured.FullName, Is.EqualTo("Harborline Services"));
    }

    private static SourceIngestRequest CreateRequest()
    {
        return new SourceIngestRequest
        {
            SourceSystem = "QUOTEFORGE",
            MessageType = "QuoteRequest",
            Payload = JsonSerializer.SerializeToElement(new
            {
                quoteReference = "QT-2001",
                insuredName = "Harborline Services",
                productLine = "Liability",
                brokerCode = "BRK-44",
                brokerName = "Summit Risk Partners",
                technicalPremium = 8200m,
                brokerPremium = 8500m,
                currencyCode = "USD",
                effectiveDate = "2026-05-01",
                expiryDate = "2027-04-30",
                underwritingYear = 2026,
                insuredRevenue = 1500000m,
                insuredEmployeeCount = 25,
                insuredYearsInBusiness = 8
            })
        };
    }
}
