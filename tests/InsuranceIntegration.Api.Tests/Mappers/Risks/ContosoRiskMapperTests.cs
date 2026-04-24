using InsuranceIntegration.Api.Mappers.Risks;
using InsuranceIntegration.Api.SourceContracts.Ingest;
using System.Text.Json;

namespace InsuranceIntegration.Api.Tests.Mappers.Risks;

public sealed class ContosoRiskMapperTests
{
    [Test]
    public void CanMap_ReturnsTrueForSupportedSourceAndMessageType()
    {
        var mapper = new ContosoRiskMapper();

        var result = mapper.CanMap(CreateRequest());

        Assert.That(result, Is.True);
    }

    [Test]
    public void Map_TransformsContosoPayloadIntoCanonicalRiskRequest()
    {
        var mapper = new ContosoRiskMapper();

        var result = mapper.Map(CreateRequest());

        Assert.That(result.ExternalReference, Is.EqualTo("Q-100045"));
        Assert.That(result.ProductCode, Is.EqualTo("COMMERCIALPROPERTY"));
        Assert.That(result.SourceSystem, Is.EqualTo("CONTOSO_UW"));
        Assert.That(result.TransactionType, Is.EqualTo("Submission"));
        Assert.That(result.Insured.FullName, Is.EqualTo("Northwind Storage Ltd"));
        Assert.That(result.AnnualizedGrossPremium, Is.EqualTo(12500m));
        Assert.That(result.Installments.Count, Is.EqualTo(2));
        Assert.That(result.SectionOperations.Count, Is.EqualTo(3));
    }

    private static SourceIngestRequest CreateRequest()
    {
        return new SourceIngestRequest
        {
            SourceSystem = "CONTOSO_UW",
            MessageType = "RiskSubmission",
            Payload = JsonSerializer.SerializeToElement(new
            {
                quoteId = "Q-100045",
                insuredName = "Northwind Storage Ltd",
                trade = "CommercialProperty",
                estimatedPremium = 12500m
            })
        };
    }
}
