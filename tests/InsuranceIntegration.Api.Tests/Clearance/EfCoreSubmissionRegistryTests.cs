using InsuranceIntegration.Api.Persistence;
using InsuranceIntegration.Api.Services.Clearance;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InsuranceIntegration.Api.Tests.Clearance;

public sealed class EfCoreSubmissionRegistryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly IDbContextFactory<IntegrationDbContext> _factory;

    public EfCoreSubmissionRegistryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _factory = new TestDbContextFactory(options);

        using var context = _factory.CreateDbContext();
        context.Database.EnsureCreated();
    }

    [Test]
    public void RegisterAndRetrieve_PersistsSubmissionRecord()
    {
        var registry = new EfCoreSubmissionRegistry(_factory);

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

    private sealed class TestDbContextFactory : IDbContextFactory<IntegrationDbContext>
    {
        private readonly DbContextOptions<IntegrationDbContext> _options;

        public TestDbContextFactory(DbContextOptions<IntegrationDbContext> options)
        {
            _options = options;
        }

        public IntegrationDbContext CreateDbContext()
        {
            return new IntegrationDbContext(_options);
        }
    }
}
