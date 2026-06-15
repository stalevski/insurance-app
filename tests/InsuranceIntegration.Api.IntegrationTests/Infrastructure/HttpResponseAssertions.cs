using System.Net;
using System.Text.Json;

namespace InsuranceIntegration.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Assertion conveniences that fold the two repeated steps — verify the status code, then read the
/// body — into a single call. Keeping them together means a failing status is reported before an
/// attempt is made to deserialize an error payload, which gives a clearer failure message.
/// </summary>
internal static class HttpResponseAssertions
{
    /// <summary>Asserts the response carries the expected status code.</summary>
    internal static void ShouldHaveStatus(this HttpResponseMessage response, HttpStatusCode expected) =>
        Assert.That(response.StatusCode, Is.EqualTo(expected));

    /// <summary>Asserts the status code, then returns the body as a detached <see cref="JsonElement"/>.</summary>
    internal static async Task<JsonElement> ShouldReturnJsonAsync(
        this HttpResponseMessage response,
        HttpStatusCode expected = HttpStatusCode.OK)
    {
        response.ShouldHaveStatus(expected);
        return await response.ReadAsJsonAsync();
    }

    /// <summary>Asserts the status code, then returns the body as a non-null value of <typeparamref name="T"/>.</summary>
    internal static async Task<T> ShouldReturnAsync<T>(
        this HttpResponseMessage response,
        HttpStatusCode expected = HttpStatusCode.OK)
    {
        response.ShouldHaveStatus(expected);
        return await response.ReadAsAsync<T>();
    }
}
