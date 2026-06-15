using System.Net;
using System.Net.Http.Json;
using InsuranceIntegration.Api.CanonicalContracts.Claims;
using InsuranceIntegration.Api.IntegrationTests.Infrastructure;
using InsuranceIntegration.Api.Services.Claims;

namespace InsuranceIntegration.Api.IntegrationTests.Api;

/// <summary>
/// Coverage for the API-key gate. The fixture boots a host with a configured key, so mutating
/// requests must carry a valid <c>X-Api-Key</c> header while read requests stay open.
/// </summary>
public sealed class SecurityEndpointsTests : ApiTestBase
{
    private const string HeaderName = "X-Api-Key";

    public SecurityEndpointsTests()
        : base("integration-test-key")
    {
    }

    [Test]
    public async Task MutatingRequest_WithoutAKey_IsRejected()
    {
        using var request = BuildTransitionRequest(apiKey: null);

        using var response = await Client.SendAsync(request);

        response.ShouldHaveStatus(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task MutatingRequest_WithAnInvalidKey_IsRejected()
    {
        using var request = BuildTransitionRequest(apiKey: "wrong-key");

        using var response = await Client.SendAsync(request);

        response.ShouldHaveStatus(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task MutatingRequest_WithAValidKey_IsAccepted()
    {
        using var request = BuildTransitionRequest(apiKey: Factory.ApiKey);

        using var response = await Client.SendAsync(request);

        response.ShouldHaveStatus(HttpStatusCode.OK);
    }

    [Test]
    public async Task ReadRequest_WithoutAKey_IsAllowed()
    {
        using var response = await GetAsync("/api/v1/products");

        response.ShouldHaveStatus(HttpStatusCode.OK);
    }

    private static HttpRequestMessage BuildTransitionRequest(string? apiKey)
    {
        var payload = new ClaimTransitionRequest
        {
            ClaimReference = "CLM-SEC-1",
            PolicyReference = "POL-SEC-1",
            CurrentStatus = ClaimStatusValue.Notified,
            TargetStatus = ClaimStatusValue.Open,
        };

        var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/api/v1/claims/transitions", UriKind.Relative))
        {
            Content = JsonContent.Create(payload, options: HttpJsonExtensions.JsonOptions),
        };

        if (apiKey is not null)
        {
            request.Headers.Add(HeaderName, apiKey);
        }

        return request;
    }
}
