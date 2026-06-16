using System.Net;
using InsuranceIntegration.Api.IntegrationTests.Infrastructure;
using InsuranceIntegration.Api.Services.Policies;

namespace InsuranceIntegration.Api.IntegrationTests.Api;

/// <summary>
/// Coverage for the policy lifecycle endpoints. The "unknown policy" paths assert the documented
/// error contract (<c>KeyNotFoundException</c> → 404) for every operation, and a happy-path
/// cancellation against a seeded policy proves the success contract end to end.
/// </summary>
public sealed class PolicyLifecycleEndpointsTests : SeededApiTestBase
{
    private static readonly DateOnly Inception = new(2026, 1, 1);
    private static readonly DateOnly Expiry = new(2026, 12, 31);

    [Test]
    public async Task Cancellation_Returns404_ForAnUnknownPolicy()
    {
        var request = new CancellationRequest
        {
            PolicyReference = "POL-DOES-NOT-EXIST",
            AnnualPremium = 10_000m,
            InceptionDate = Inception,
            ExpiryDate = Expiry,
            CancellationDate = new DateOnly(2026, 7, 1),
        };

        using var response = await PostAsync("/api/v1/policies/cancellations", request);

        response.ShouldHaveStatus(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Endorsement_Returns404_ForAnUnknownPolicy()
    {
        var request = new EndorsementRequest
        {
            PolicyReference = "POL-DOES-NOT-EXIST",
            CurrentAnnualPremium = 10_000m,
            NewAnnualPremium = 12_000m,
            InceptionDate = Inception,
            ExpiryDate = Expiry,
            EffectiveDate = new DateOnly(2026, 6, 1),
        };

        using var response = await PostAsync("/api/v1/policies/endorsements", request);

        response.ShouldHaveStatus(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Renewal_Returns404_ForAnUnknownPolicy()
    {
        var request = new RenewalRequest
        {
            PolicyReference = "POL-DOES-NOT-EXIST",
            NewQuoteReference = "QT-RENEW-404",
            NewInceptionDate = new DateOnly(2027, 1, 1),
            NewExpiryDate = new DateOnly(2027, 12, 31),
            PriorAnnualPremium = 10_000m,
            PriorClaimsPaid = 1_000m,
        };

        using var response = await PostAsync("/api/v1/policies/renewals", request);

        response.ShouldHaveStatus(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Reinstatement_Returns404_ForAnUnknownPolicy()
    {
        var request = new ReinstatementRequest
        {
            PolicyReference = "POL-DOES-NOT-EXIST",
            AnnualPremium = 10_000m,
            InceptionDate = Inception,
            ExpiryDate = Expiry,
            CancellationDate = new DateOnly(2026, 4, 1),
            ReinstatementDate = new DateOnly(2026, 5, 1),
        };

        using var response = await PostAsync("/api/v1/policies/reinstatements", request);

        response.ShouldHaveStatus(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Lapse_Returns404_ForAnUnknownPolicy()
    {
        var request = new LapseRequest
        {
            PolicyReference = "POL-DOES-NOT-EXIST",
            AnnualPremium = 10_000m,
            InceptionDate = Inception,
            ExpiryDate = Expiry,
            LapseDate = new DateOnly(2026, 7, 1),
        };

        using var response = await PostAsync("/api/v1/policies/lapses", request);

        response.ShouldHaveStatus(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task NonRenewal_Returns404_ForAnUnknownPolicy()
    {
        var request = new NonRenewalRequest
        {
            PolicyReference = "POL-DOES-NOT-EXIST",
            AnnualPremium = 10_000m,
            InceptionDate = Inception,
            ExpiryDate = Expiry,
            NoticeDays = 30,
        };

        using var response = await PostAsync("/api/v1/policies/non-renewals", request);

        response.ShouldHaveStatus(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Cancellation_MovesASeededPolicyToCancelled()
    {
        var reference = await GetFirstSeededPolicyReferenceAsync();
        var request = new CancellationRequest
        {
            PolicyReference = reference,
            AnnualPremium = 10_000m,
            InceptionDate = Inception,
            ExpiryDate = Expiry,
            CancellationDate = new DateOnly(2026, 7, 1),
            Basis = CancellationBasis.ProRata,
        };

        using var response = await PostAsync("/api/v1/policies/cancellations", request);

        var body = await response.ShouldReturnJsonAsync();
        Assert.Multiple(() =>
        {
            Assert.That(body.GetProperty("policyStatus").GetString(), Is.EqualTo("Cancelled"));
            Assert.That(body.GetProperty("currentPhase").GetString(), Is.EqualTo("Cancelled"));
        });
    }
}
