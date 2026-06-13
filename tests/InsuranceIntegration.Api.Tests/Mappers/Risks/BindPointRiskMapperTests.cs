using InsuranceIntegration.Api.Mappers.Risks;
using InsuranceIntegration.Api.SourceContracts.Ingest;
using Microsoft.Extensions.Time.Testing;
using System.Text.Json;

namespace InsuranceIntegration.Api.Tests.Mappers.Risks;

public sealed class BindPointRiskMapperTests
{
    private static readonly FakeTimeProvider Time = new(new DateTimeOffset(2026, 1, 15, 8, 30, 0, TimeSpan.Zero));

    [Test]
    public void CanMap_ReturnsTrueForPolicyBindRequestFromBindPoint()
    {
        var mapper = new BindPointRiskMapper(Time);

        var result = mapper.CanMap(CreateRequest());

        Assert.That(result, Is.True);
    }

    [Test]
    public void Map_TransformsBindPointPayloadIntoCanonicalPolicyBindRequest()
    {
        var mapper = new BindPointRiskMapper(Time);

        var result = mapper.Map(CreateRequest());

        Assert.That(result.TransactionTimestampUtc, Is.EqualTo(Time.GetUtcNow().UtcDateTime));
        Assert.That(result.ExternalReference, Is.EqualTo("POL-7781"));
        Assert.That(result.SourceSystem, Is.EqualTo("BINDPOINT"));
        Assert.That(result.TransactionType, Is.EqualTo("PolicyBind"));
        Assert.That(result.ProductCode, Is.EqualTo("LIABILITY"));
        Assert.That(result.Policy.PolicyReference, Is.EqualTo("POL-7781"));
        Assert.That(result.Quote.QuoteReference, Is.EqualTo("QT-2001"));
        Assert.That(result.Installments.Count, Is.EqualTo(4));
        Assert.That(result.Broker.IsPreferredPartner, Is.True);
    }

    [Test]
    public void Map_InstallmentsSumExactlyToBoundPremium_WhenDivisionDoesNotRoundEvenly()
    {
        var mapper = new BindPointRiskMapper(Time);

        // 1000 / 3 = 333.33 rounded; naive equal installments would sum to 999.99.
        var result = mapper.Map(CreateRequest(boundPremium: 1000m, installmentCount: 3));

        Assert.Multiple(() =>
        {
            Assert.That(result.Installments.Sum(installment => installment.Amount), Is.EqualTo(1000m));
            Assert.That(result.Installments[0].Amount, Is.EqualTo(333.33m));
            Assert.That(result.Installments[1].Amount, Is.EqualTo(333.33m));
            Assert.That(result.Installments[2].Amount, Is.EqualTo(333.34m), "last installment absorbs the rounding residual");
        });
    }

    [Test]
    public void Map_InstallmentsRemainEqual_WhenDivisionRoundsEvenly()
    {
        var mapper = new BindPointRiskMapper(Time);

        var result = mapper.Map(CreateRequest(boundPremium: 8500m, installmentCount: 4));

        Assert.Multiple(() =>
        {
            Assert.That(result.Installments.Sum(installment => installment.Amount), Is.EqualTo(8500m));
            Assert.That(result.Installments.Select(installment => installment.Amount), Is.All.EqualTo(2125m));
        });
    }

    private static SourceIngestRequest CreateRequest(decimal boundPremium = 8500m, int installmentCount = 4)
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
                boundPremium,
                currencyCode = "USD",
                inceptionDate = "2026-05-01",
                expiryDate = "2027-04-30",
                boundDate = "2026-04-28",
                paymentMethod = "Invoice",
                installmentCount
            })
        };
    }
}
