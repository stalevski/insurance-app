using InsuranceIntegration.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InsuranceIntegration.Api.Services.Events;

public sealed class DomainEventLog : IDomainEventLog
{
    private readonly IntegrationDbContext _context;

    public DomainEventLog(IntegrationDbContext context)
    {
        _context = context;
    }

    public void Append(DomainEventEntity entity)
    {
        _context.DomainEvents.Add(entity);
        // Intentionally not calling SaveChanges. The caller (snapshot service / endpoint)
        // saves the surrounding work in the same EF transaction so the event row and the
        // snapshot row are flushed together.
    }

    public IReadOnlyList<DomainEventEntity> GetByAggregate(string aggregateKind, string aggregateKey)
    {
        return _context.DomainEvents
            .AsNoTracking()
            .Where(record => record.AggregateKind == aggregateKind && record.AggregateKey == aggregateKey)
            .OrderBy(record => record.OccurredAtUtc)
            .ThenBy(record => record.RecordedAtUtc)
            .ToList();
    }

    public IReadOnlyList<DomainEventEntity> List(string? aggregateKind = null, string? eventType = null, int skip = 0, int take = 100)
    {
        if (take <= 0) take = 100;
        if (take > 500) take = 500;
        if (skip < 0) skip = 0;

        var query = _context.DomainEvents.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(aggregateKind))
        {
            query = query.Where(record => record.AggregateKind == aggregateKind);
        }

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            query = query.Where(record => record.EventType == eventType);
        }

        return query
            .OrderByDescending(record => record.OccurredAtUtc)
            .ThenByDescending(record => record.RecordedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToList();
    }
}
