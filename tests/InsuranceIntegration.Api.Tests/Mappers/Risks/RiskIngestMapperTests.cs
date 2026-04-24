using InsuranceIntegration.Api.CanonicalContracts.Risks;
using InsuranceIntegration.Api.Mappers.Risks;
using InsuranceIntegration.Api.SourceContracts.Ingest;
using InsuranceIntegration.Api.Tests.Flows;
using System.Text.Json;

namespace InsuranceIntegration.Api.Tests.Mappers.Risks;

public sealed class RiskIngestMapperTests
{
    [Test]
    public void Map_UsesFirstCompatibleMapper()
    {
        var expected = TestRiskRequestFactory.Create();
        var mapper = new RiskIngestMapper([
            new StubSourceRiskMapper(false, TestRiskRequestFactory.Create()),
            new StubSourceRiskMapper(true, expected)
        ]);

        var result = mapper.Map(CreateRequest());

        Assert.That(result, Is.SameAs(expected));
    }

    [Test]
    public void Map_ThrowsWhenNoMapperCanHandleRequest()
    {
        var mapper = new RiskIngestMapper([
            new StubSourceRiskMapper(false, TestRiskRequestFactory.Create())
        ]);

        var exception = Assert.Throws<InvalidOperationException>(() => mapper.Map(CreateRequest()));
        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Message, Does.Contain("No risk mapper registered"));
    }

    private static SourceIngestRequest CreateRequest()
    {
        return new SourceIngestRequest
        {
            SourceSystem = "POLARIS_UW",
            MessageType = "RiskSubmission",
            Payload = JsonSerializer.SerializeToElement(new { quoteId = "Q-1" })
        };
    }
}
