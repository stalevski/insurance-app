using InsuranceIntegration.Api.IntegrationTests.Infrastructure;

namespace InsuranceIntegration.Api.IntegrationTests.Api;

/// <summary>
/// Read coverage for the source-system catalog (<c>GET /api/v1/source-systems</c>), which advertises
/// the upstream/downstream systems the integration layer understands.
/// </summary>
public sealed class SourceSystemEndpointsTests : ApiTestBase
{
    private static readonly string[] ExpectedSystems =
    [
        "CONTOSO_UW", "QUOTEFORGE", "BINDPOINT", "POLICYCORE", "RENEWALPILOT", "CLAIMFORGE",
        "BROKERHUB", "SANCTIONSCAN", "PAYMENTRAIL", "REINS_LINK", "FRAUDLENS", "DOCVAULT",
        "ENDORSE_PRO", "LOSSWATCH",
    ];

    [Test]
    public async Task GetSourceSystems_ReturnsTheFullCatalog()
    {
        using var response = await GetAsync("/api/v1/source-systems");

        var body = await response.ShouldReturnJsonAsync();
        Assert.That(body.ValueKind, Is.EqualTo(System.Text.Json.JsonValueKind.Array));
        Assert.That(body.GetArrayLength(), Is.EqualTo(ExpectedSystems.Length));

        var rawText = body.GetRawText();
        Assert.Multiple(() =>
        {
            foreach (var system in ExpectedSystems)
            {
                Assert.That(rawText, Does.Contain(system), $"Catalog should advertise {system}.");
            }
        });
    }
}
