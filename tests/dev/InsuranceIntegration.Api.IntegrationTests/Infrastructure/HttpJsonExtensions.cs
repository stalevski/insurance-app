using System.Net.Http.Json;
using System.Text.Json;

namespace InsuranceIntegration.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Small JSON conveniences over <see cref="HttpResponseMessage"/> for the API tests. The API
/// serializes with <see cref="JsonSerializerDefaults.Web"/> (camelCase, case-insensitive), so the
/// helpers mirror that to keep round-tripping faithful.
/// </summary>
internal static class HttpJsonExtensions
{
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Reads the response body as a strongly-typed value, asserting it is non-null.</summary>
    internal static async Task<T> ReadAsAsync<T>(this HttpResponseMessage response)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        Assert.That(value, Is.Not.Null, "Expected a non-null JSON body.");
        return value!;
    }

    /// <summary>Reads the response body as a detached <see cref="JsonElement"/> for loose assertions.</summary>
    internal static async Task<JsonElement> ReadAsJsonAsync(this HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        return document.RootElement.Clone();
    }
}
