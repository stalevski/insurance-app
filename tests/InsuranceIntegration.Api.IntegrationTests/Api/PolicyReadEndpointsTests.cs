using System.Net;
using InsuranceIntegration.Api.IntegrationTests.Infrastructure;

namespace InsuranceIntegration.Api.IntegrationTests.Api;

/// <summary>
/// Read coverage for the policy snapshot endpoints against the seeded data set. Seeded policy
/// references are discovered at runtime (rather than hard-coded) so the suite stays resilient to
/// changes in the seeding routine.
/// </summary>
public sealed class PolicyReadEndpointsTests : SeededApiTestBase
{
    [Test]
    public async Task ListPolicies_ReturnsSeededPolicies()
    {
        using var response = await GetAsync("/api/v1/policies?take=100");

        var body = await response.ShouldReturnJsonAsync();
        var count = body.GetProperty("count").GetInt32();
        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(8), "The seeder binds two policies in each of the four product families.");
            Assert.That(body.GetProperty("items").GetArrayLength(), Is.EqualTo(count));
        });
    }

    [Test]
    public async Task GetPolicy_ReturnsSnapshot_ForASeededReference()
    {
        var reference = await GetFirstSeededPolicyReferenceAsync();

        using var response = await GetAsync($"/api/v1/policies/{reference}");

        var snapshot = await response.ShouldReturnJsonAsync();
        Assert.That(snapshot.GetProperty("policyReference").GetString(), Is.EqualTo(reference));
    }

    [Test]
    public async Task GetPolicy_Returns404_ForAnUnknownReference()
    {
        using var response = await GetAsync("/api/v1/policies/POL-DOES-NOT-EXIST");

        response.ShouldHaveStatus(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetSchedulePdf_ReturnsAPdf_ForASeededReference()
    {
        var reference = await GetFirstSeededPolicyReferenceAsync();

        using var response = await GetAsync($"/api/v1/policies/{reference}/schedule.pdf");

        Assert.Multiple(() =>
        {
            response.ShouldHaveStatus(HttpStatusCode.OK);
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/pdf"));
        });

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.That(bytes, Is.Not.Empty, "A generated schedule should contain bytes.");
    }

    [Test]
    public async Task GetSchedulePdf_Returns404_ForAnUnknownReference()
    {
        using var response = await GetAsync("/api/v1/policies/POL-DOES-NOT-EXIST/schedule.pdf");

        response.ShouldHaveStatus(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task RebuildPolicySnapshot_Returns404_ForAnUnknownReference()
    {
        using var response = await PostAsync("/api/v1/snapshots/policies/POL-DOES-NOT-EXIST/rebuild");

        response.ShouldHaveStatus(HttpStatusCode.NotFound);
    }
}
