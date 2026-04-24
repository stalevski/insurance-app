using InsuranceIntegration.Api.Persistence;
using InsuranceIntegration.Api.Services.Clearance;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InsuranceIntegration.Api.Tests.Clearance;

public sealed class EfCoreSubmissionRegistryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<IntegrationDbContext> _options;

    public EfCoreSubmissionRegistryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new IntegrationDbContext(_options);
        context.Database.EnsureCreated();
    }

    [Test]
    public void RegisterAndRetrieve_PersistsSubmissionRecord()
    {
        using var context = new IntegrationDbContext(_options);
        var registry = new EfCoreSubmissionRegistry(context, TimeProvider.System);

        registry.Register(new KnownSubmissionRecord
        {
            ExternalReference = "EXT-1",
            InsuredName = "Northwind Storage Ltd",
            ProductCode = "COMMERCIAL_PROPERTY",
            UnderwritingYear = 2026,
            BrokerCode = "BRK-1"
        });

        var records = registry.GetKnownSubmissions();

        Assert.That(records, Has.Count.EqualTo(1));
        var first = records.First();
        Assert.That(first.ExternalReference, Is.EqualTo("EXT-1"));
        Assert.That(first.ProductCode, Is.EqualTo("COMMERCIAL_PROPERTY"));
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
