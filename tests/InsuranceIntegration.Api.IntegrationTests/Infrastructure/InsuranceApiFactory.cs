using InsuranceIntegration.Api.Security;
using InsuranceIntegration.Api.Services.Outbox;
using InsuranceIntegration.Api.Services.Seeding;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace InsuranceIntegration.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Boots the real ASP.NET Core host in-process (via <see cref="WebApplicationFactory{TEntryPoint}"/>)
/// against an isolated, in-memory SQLite database. Each factory instance owns a uniquely named
/// shared-cache database; a single keep-alive connection is held open for the factory's lifetime so
/// the schema survives between requests (an in-memory SQLite database is dropped once its last
/// connection closes).
/// </summary>
/// <remarks>
/// The host runs in the <c>Testing</c> environment, which means the development data seeder does not
/// run at startup and the database begins empty. Tests that need the read models populated call
/// <see cref="SeedDevelopmentDataAsync"/> explicitly. Passing an <paramref name="apiKey"/> turns on
/// API-key enforcement for mutating requests, mirroring a locked-down deployment.
/// </remarks>
public sealed class InsuranceApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly SqliteConnection _keepAliveConnection;
    private readonly string? _apiKey;

    public InsuranceApiFactory(string? apiKey = null)
    {
        _apiKey = apiKey;
        var databaseName = $"insurance-tests-{Guid.NewGuid():N}";
        _connectionString = $"Data Source={databaseName};Mode=Memory;Cache=Shared";

        // Open before the host starts so the shared in-memory database exists when EF Core
        // migrates the schema during application startup, and stays alive afterwards.
        _keepAliveConnection = new SqliteConnection(_connectionString);
        _keepAliveConnection.Open();
    }

    /// <summary>The API key the factory accepts, or <c>null</c> when enforcement is disabled.</summary>
    public string? ApiKey => _apiKey;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["ConnectionStrings:Integration"] = _connectionString,
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Remove the polling outbox dispatcher so background writes never contend with the
            // request-scoped writes under test on the shared in-memory connection. Outbox dispatch
            // behaviour is covered separately by the unit-level OutboxDispatcher tests.
            var hostedDispatcher = services.SingleOrDefault(descriptor =>
                descriptor.ServiceType == typeof(IHostedService) &&
                descriptor.ImplementationType == typeof(OutboxDispatcher));

            if (hostedDispatcher is not null)
            {
                services.Remove(hostedDispatcher);
            }

            // API-key options are bound eagerly during service registration, before this factory's
            // configuration overrides are layered in, so a configured key is injected by replacing
            // the validator singleton the gate middleware resolves.
            if (_apiKey is not null)
            {
                services.RemoveAll<ApiKeyValidator>();
                services.RemoveAll<ApiKeyOptions>();

                var options = new ApiKeyOptions { Keys = new List<string> { _apiKey } };
                services.AddSingleton(options);
                services.AddSingleton(new ApiKeyValidator(options));
            }
        });
    }

    /// <summary>
    /// Runs the development data seeder against this factory's database, populating the 32 quote and
    /// 8 policy read models used by the read-side endpoint and UI tests.
    /// </summary>
    public async Task SeedDevelopmentDataAsync()
    {
        using var scope = Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IDevelopmentDataSeeder>();
        await seeder.SeedAsync();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _keepAliveConnection.Dispose();
        }
    }
}
