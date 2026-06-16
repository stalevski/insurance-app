using InsuranceIntegration.Api.IntegrationTests.Infrastructure;

namespace InsuranceIntegration.Api.IntegrationTests.Api;

/// <summary>
/// Coverage for the development data seeder's idempotency contract: it populates the read models on
/// the first run and is a no-op on subsequent runs, so repeated application startup never duplicates
/// the sample quotes and policies.
/// </summary>
public sealed class DevelopmentDataSeederTests : ApiTestBase
{
    [Test]
    public async Task Seed_IsIdempotent_AcrossRepeatedRuns()
    {
        await Factory.SeedDevelopmentDataAsync();
        await Factory.SeedDevelopmentDataAsync();

        using var quotes = await GetAsync("/api/v1/quotes?take=100");
        var quoteBody = await quotes.ShouldReturnJsonAsync();

        using var policies = await GetAsync("/api/v1/policies?take=100");
        var policyBody = await policies.ShouldReturnJsonAsync();

        Assert.Multiple(() =>
        {
            Assert.That(
                quoteBody.GetProperty("count").GetInt32(),
                Is.EqualTo(28),
                "Seeding twice must not duplicate quotes.");
            Assert.That(
                policyBody.GetProperty("count").GetInt32(),
                Is.EqualTo(8),
                "Seeding twice must not duplicate policies.");
        });
    }
}
