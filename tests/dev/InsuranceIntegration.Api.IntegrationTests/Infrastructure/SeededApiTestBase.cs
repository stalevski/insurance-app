namespace InsuranceIntegration.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Base fixture for read-side tests that need the development data present. The seeder runs once,
/// before any test in the derived class, populating the 28 quote and 8 policy read models.
/// </summary>
public abstract class SeededApiTestBase : ApiTestBase
{
    [OneTimeSetUp]
    public async Task SeedAsync()
    {
        await Factory.SeedDevelopmentDataAsync();
    }

    /// <summary>
    /// Returns the reference of the first seeded policy. References are discovered at runtime rather
    /// than hard-coded so the suite stays resilient to changes in the seeding routine.
    /// </summary>
    protected async Task<string> GetFirstSeededPolicyReferenceAsync()
    {
        using var response = await GetAsync("/api/v1/policies?take=1");
        var body = await response.ReadAsJsonAsync();
        var items = body.GetProperty("items");
        Assert.That(items.GetArrayLength(), Is.GreaterThan(0), "Expected at least one seeded policy.");
        return items[0].GetProperty("policyReference").GetString()!;
    }
}
