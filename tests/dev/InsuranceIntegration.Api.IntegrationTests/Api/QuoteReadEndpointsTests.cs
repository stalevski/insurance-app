using System.Net;
using InsuranceIntegration.Api.IntegrationTests.Infrastructure;
using InsuranceIntegration.Api.Services.Snapshots;

namespace InsuranceIntegration.Api.IntegrationTests.Api;

/// <summary>
/// Read coverage for the quote snapshot endpoints against the seeded data set (28 quotes across the
/// four product families, references <c>QF-{PROP,LIAB,CYB,MOT}-01..07</c>).
/// </summary>
public sealed class QuoteReadEndpointsTests : SeededApiTestBase
{
    [Test]
    public async Task ListQuotes_ReturnsSeededQuotesWithCount()
    {
        using var response = await GetAsync("/api/v1/quotes?take=100");

        var body = await response.ShouldReturnJsonAsync();
        var items = body.GetProperty("items");
        Assert.Multiple(() =>
        {
            Assert.That(body.GetProperty("count").GetInt32(), Is.EqualTo(28));
            Assert.That(items.GetArrayLength(), Is.EqualTo(28));
        });
    }

    [Test]
    public async Task ListQuotes_HonoursPaging()
    {
        using var response = await GetAsync("/api/v1/quotes?skip=0&take=10");

        var body = await response.ShouldReturnJsonAsync();
        Assert.That(body.GetProperty("count").GetInt32(), Is.EqualTo(10));
    }

    [Test]
    public async Task GetQuote_ReturnsSnapshot_ForKnownReference()
    {
        using var response = await GetAsync("/api/v1/quotes/QF-LIAB-01");

        var snapshot = await response.ShouldReturnAsync<QuoteSnapshotSummary>();
        Assert.Multiple(() =>
        {
            Assert.That(snapshot.QuoteReference, Is.EqualTo("QF-LIAB-01"));
            Assert.That(snapshot.ProductCode, Is.EqualTo("LIABILITY"));
        });
    }

    [Test]
    public async Task GetQuote_Returns404_ForUnknownReference()
    {
        using var response = await GetAsync("/api/v1/quotes/QF-DOES-NOT-EXIST");

        response.ShouldHaveStatus(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task RebuildQuoteSnapshot_Returns404_ForUnknownReference()
    {
        using var response = await PostAsync("/api/v1/snapshots/quotes/QF-DOES-NOT-EXIST/rebuild");

        response.ShouldHaveStatus(HttpStatusCode.NotFound);
    }
}
