using System.Net;
using InsuranceIntegration.Api.IntegrationTests.Infrastructure;
using InsuranceIntegration.Api.Middleware;

namespace InsuranceIntegration.Api.IntegrationTests.Api;

/// <summary>
/// End-to-end coverage for the cross-cutting request pipeline: the correlation-id echo / generation
/// performed by <see cref="CorrelationIdMiddleware"/>, and the environment gate that keeps the
/// read-only database browser (<c>/database</c>) returning 404 outside Development.
/// </summary>
public sealed class PipelineEndpointsTests : ApiTestBase
{
    [Test]
    public async Task CorrelationId_IsEchoed_WhenSuppliedOnTheRequest()
    {
        var correlationId = Guid.CreateVersion7().ToString();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/health", UriKind.Relative));
        request.Headers.TryAddWithoutValidation(CorrelationIdMiddleware.HeaderName, correlationId);

        using var response = await Client.SendAsync(request);

        Assert.That(
            response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single(),
            Is.EqualTo(correlationId));
    }

    [Test]
    public async Task CorrelationId_IsGenerated_WhenAbsentFromTheRequest()
    {
        using var response = await GetAsync("/health");

        var header = response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single();
        Assert.That(
            Guid.TryParse(header, out _),
            Is.True,
            "An absent correlation id should be replaced with a generated GUID.");
    }

    [Test]
    public async Task CorrelationId_IsRegenerated_WhenTheSuppliedValueIsNotAGuid()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/health", UriKind.Relative));
        request.Headers.TryAddWithoutValidation(CorrelationIdMiddleware.HeaderName, "not-a-guid");

        using var response = await Client.SendAsync(request);

        var header = response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single();
        Assert.Multiple(() =>
        {
            Assert.That(header, Is.Not.EqualTo("not-a-guid"));
            Assert.That(Guid.TryParse(header, out _), Is.True);
        });
    }

    [Test]
    public async Task DatabaseBrowser_Returns404_OutsideDevelopment()
    {
        using var response = await GetAsync("/database");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
