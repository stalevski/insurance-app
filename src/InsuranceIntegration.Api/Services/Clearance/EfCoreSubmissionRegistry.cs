using InsuranceIntegration.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InsuranceIntegration.Api.Services.Clearance;

public sealed class EfCoreSubmissionRegistry : ISubmissionRegistry
{
    private readonly IntegrationDbContext _context;
    private readonly TimeProvider _timeProvider;

    public EfCoreSubmissionRegistry(IntegrationDbContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public IReadOnlyCollection<KnownSubmissionRecord> GetKnownSubmissions()
    {
        return _context.KnownSubmissions
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
        _context.KnownSubmissions.Add(new KnownSubmissionEntity
        {
            Id = Guid.CreateVersion7(),
            ExternalReference = record.ExternalReference,
            InsuredName = record.InsuredName,
            ProductCode = record.ProductCode,
            UnderwritingYear = record.UnderwritingYear,
            BrokerCode = record.BrokerCode,
            RegisteredAtUtc = _timeProvider.GetUtcNow().UtcDateTime
        });
        _context.SaveChanges();
    }
}
