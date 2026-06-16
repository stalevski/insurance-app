using System.Net;
using InsuranceIntegration.Api.CanonicalContracts.Claims;
using InsuranceIntegration.Api.IntegrationTests.Infrastructure;
using InsuranceIntegration.Api.Services.Claims;

namespace InsuranceIntegration.Api.IntegrationTests.Api;

/// <summary>
/// Coverage for the claim endpoints: the state-machine transition guard
/// (<c>POST /api/v1/claims/transitions</c>) and the financial movements
/// (<c>POST /api/v1/claims/financials</c>). Both validate input and surface
/// <c>ArgumentException</c> as a 400.
/// </summary>
public sealed class ClaimEndpointsTests : ApiTestBase
{
    [Test]
    public async Task Transition_MovesNotifiedToOpen()
    {
        var request = new ClaimTransitionRequest
        {
            ClaimReference = "CLM-100",
            PolicyReference = "POL-CLM-100",
            CurrentStatus = ClaimStatusValue.Notified,
            TargetStatus = ClaimStatusValue.Open,
        };

        using var response = await PostAsync("/api/v1/claims/transitions", request);

        var body = await response.ShouldReturnJsonAsync();
        Assert.Multiple(() =>
        {
            Assert.That(body.GetProperty("previousStatus").GetString(), Is.EqualTo(ClaimStatusValue.Notified));
            Assert.That(body.GetProperty("status").GetString(), Is.EqualTo(ClaimStatusValue.Open));
        });
    }

    [Test]
    public async Task Transition_Returns400_FromATerminalStatus()
    {
        var request = new ClaimTransitionRequest
        {
            ClaimReference = "CLM-101",
            PolicyReference = "POL-CLM-101",
            CurrentStatus = ClaimStatusValue.Closed,
            TargetStatus = ClaimStatusValue.Open,
        };

        using var response = await PostAsync("/api/v1/claims/transitions", request);

        response.ShouldHaveStatus(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Transition_Returns400_ForANonPermittedMove()
    {
        var request = new ClaimTransitionRequest
        {
            ClaimReference = "CLM-102",
            PolicyReference = "POL-CLM-102",
            CurrentStatus = ClaimStatusValue.Notified,
            TargetStatus = ClaimStatusValue.Settled,
        };

        using var response = await PostAsync("/api/v1/claims/transitions", request);

        response.ShouldHaveStatus(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ApplyFinancial_SetsTheOutstandingReserve()
    {
        var request = new ClaimFinancialRequest
        {
            ClaimReference = "CLM-200",
            PolicyReference = "POL-CLM-200",
            Operation = ClaimFinancialOperation.SetReserve,
            Amount = 5_000m,
            CurrentReserve = 0m,
        };

        using var response = await PostAsync("/api/v1/claims/financials", request);

        var body = await response.ShouldReturnJsonAsync();
        Assert.That(body.GetProperty("outstandingReserve").GetDecimal(), Is.EqualTo(5_000m));
    }

    [Test]
    public async Task ApplyFinancial_Returns400_ForAnUnknownOperation()
    {
        var request = new ClaimFinancialRequest
        {
            ClaimReference = "CLM-201",
            PolicyReference = "POL-CLM-201",
            Operation = "Teleport",
            Amount = 100m,
        };

        using var response = await PostAsync("/api/v1/claims/financials", request);

        response.ShouldHaveStatus(HttpStatusCode.BadRequest);
    }
}
