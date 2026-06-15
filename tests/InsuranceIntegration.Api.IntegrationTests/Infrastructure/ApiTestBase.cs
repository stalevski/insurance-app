using System.Net.Http.Json;

namespace InsuranceIntegration.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Base fixture for API tests that exercise the HTTP surface against an empty database. NUnit
/// instantiates the fixture once, so a single host and <see cref="HttpClient"/> are reused by every
/// test in the derived class and torn down afterwards.
/// </summary>
public abstract class ApiTestBase : IDisposable
{
    private bool _disposed;

    protected ApiTestBase(string? apiKey = null)
    {
        Factory = new InsuranceApiFactory(apiKey);
        Client = Factory.CreateClient();
    }

    protected InsuranceApiFactory Factory { get; }

    protected HttpClient Client { get; }

    /// <summary>Issues a GET against a relative API path.</summary>
    protected Task<HttpResponseMessage> GetAsync(string path) =>
        Client.GetAsync(new Uri(path, UriKind.Relative));

    /// <summary>POSTs <paramref name="request"/> as a JSON body using the API's web serializer settings.</summary>
    protected Task<HttpResponseMessage> PostAsync<T>(string path, T request) =>
        Client.PostAsJsonAsync(new Uri(path, UriKind.Relative), request, HttpJsonExtensions.JsonOptions);

    /// <summary>POSTs to a relative API path with no request body (e.g. snapshot rebuild commands).</summary>
    protected Task<HttpResponseMessage> PostAsync(string path) =>
        Client.PostAsync(new Uri(path, UriKind.Relative), content: null);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            Client.Dispose();
            Factory.Dispose();
        }

        _disposed = true;
    }
}
