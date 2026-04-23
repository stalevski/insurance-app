using InsuranceIntegration.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InsuranceIntegration.Api.Services.Clearance;

public sealed class EfCoreSubmissionRegistry : ISubmissionRegistry
{
    private readonly IDbContextFactory<IntegrationDbContext> _contextFactory;

    public EfCoreSubmissionRegistry(IDbContextFactory<IntegrationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public IReadOnlyCollection<KnownSubmissionRecord> GetKnownSubmissions()
    {
        using var context = _contextFactory.CreateDbContext();
        return context.KnownSubmissions
            .AsNoTracking()
            .Select(entity => new KnownSubmissionRecord
            {
                ExternalReference = entity.ExternalReference,
                InsuredName = entity.InsuredName,
                ProductCode = entity.ProductCode,
                UnderwritingYear = entity.UnderwritingYear,
                BrokerCode = entity.BrokerCode
            })
            .ToArray();
    }

    public void Register(KnownSubmissionRecord record)
    {
        using var context = _contextFactory.CreateDbContext();
        context.KnownSubmissions.Add(new KnownSubmissionEntity
        {
            ExternalReference = record.ExternalReference,
            InsuredName = record.InsuredName,
            ProductCode = record.ProductCode,
            UnderwritingYear = record.UnderwritingYear,
            BrokerCode = record.BrokerCode
        });
        context.SaveChanges();
    }
}
