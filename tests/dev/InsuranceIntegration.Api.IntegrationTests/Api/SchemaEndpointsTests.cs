using System.Text.Json;
using InsuranceIntegration.Api.IntegrationTests.Infrastructure;

namespace InsuranceIntegration.Api.IntegrationTests.Api;

/// <summary>
/// Read coverage for the JSON-schema discovery endpoints. Each route publishes the schema a client
/// can validate a payload against before posting it to the integration layer.
/// </summary>
public sealed class SchemaEndpointsTests : ApiTestBase
{
    [TestCase("/api/v1/schemas/ingest/envelope")]
    [TestCase("/api/v1/schemas/ingest/risk-request")]
    [TestCase("/api/v1/schemas/canonical/risk-request")]
    [TestCase("/api/v1/schemas/final/risk-response")]
    public async Task GetSchema_ReturnsAJsonSchemaObject(string path)
    {
        using var response = await GetAsync(path);

        var body = await response.ShouldReturnJsonAsync();
        Assert.Multiple(() =>
        {
            Assert.That(body.ValueKind, Is.EqualTo(JsonValueKind.Object));
            Assert.That(body.GetProperty("type").GetString(), Is.EqualTo("object"));
            Assert.That(body.TryGetProperty("properties", out var properties), Is.True, "A schema should describe its properties.");
            Assert.That(properties.ValueKind, Is.EqualTo(JsonValueKind.Object));
        });
    }
}
