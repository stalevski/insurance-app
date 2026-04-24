using InsuranceIntegration.Api.Mappers.Risks;
using InsuranceIntegration.Api.SourceContracts.Ingest;
using System.Text.Json;

namespace InsuranceIntegration.Api.Tests.Mappers.Risks;

public sealed class BindPointRiskMapperTests
{
    [Test]
    public void CanMap_ReturnsTrueForPolicyBindRequestFromBindPoint()
    {
        var mapper = new BindPointRiskMapper();

        var result = mapper.CanMap(CreateRequest());

        Assert.That(result, Is.True);
    }

    [Test]
    public void Map_TransformsBindPointPayloadIntoCanonicalPolicyBindRequest()
    {
        var mapper = new BindPointRiskMapper();

        var result = mapper.Map(CreateRequest());

        Assert.That(result.ExternalReference, Is.EqualTo("POL-7781"));
        Assert.That(result.SourceSystem, Is.EqualTo("BINDPOINT"));
        Assert.That(result.TransactionType, Is.EqualTo("PolicyBind"));
        Assert.That(result.ProductCode, Is.EqualTo("LIABILITY"));
        Assert.That(result.Policy.PolicyReference, Is.EqualTo("POL-7781"));
        Assert.That(result.Quote.QuoteReference, Is.EqualTo("QT-2001"));
        Assert.That(result.Installments.Count, Is.EqualTo(4));
        Assert.That(result.Broker.IsPreferredPartner, Is.True);
    }

    private static SourceIngestRequest CreateRequest()
    {
        return new SourceIngestRequest
        {
            SourceSystem = "BINDPOINT",
            MessageType = "PolicyBindRequest",
            Payload = JsonSerializer.SerializeToElement(new
            {
                policyReference = "POL-7781",
                quoteReference = "QT-2001",
                insuredName = "Harborline Services",
                productCode = "LIABILITY",
                brokerCode = "BRK-44",
                brokerName = "Summit Risk Partners",
                brokerHasDelegatedAuthority = false,
                brokerIsPreferredPartner = true,
                boundPremium = 8500m,
                currencyCode = "USD",
                inceptionDate = "2026-05-01",
                expiryDate = "2027-04-30",
                boundDate = "2026-04-28",
                paymentMethod = "Invoice",
                installmentCount = 4
            })
        };
    }
}
