using InsuranceIntegration.Api.IntegrationTests.Infrastructure;

namespace InsuranceIntegration.Api.IntegrationTests.Api;

/// <summary>Smoke coverage for the unauthenticated health probe.</summary>
[Category("Smoke")]
public sealed class HealthEndpointsTests : ApiTestBase
{
    [Test]
    public async Task Health_ReturnsHealthyStatus()
    {
        using var response = await GetAsync("/health");

        var body = await response.ShouldReturnJsonAsync();
        Assert.Multiple(() =>
        {
            Assert.That(body.GetProperty("status").GetString(), Is.EqualTo("Healthy"));
            Assert.That(body.GetProperty("service").GetString(), Is.EqualTo("InsuranceIntegration.Api"));
        });
    }
}
