using InsuranceIntegration.Api.Persistence;

namespace InsuranceIntegration.Api.Services.Events;

public interface IDomainEventLog
{
    void Append(DomainEventEntity entity);

    IReadOnlyList<DomainEventEntity> GetByAggregate(string aggregateKind, string aggregateKey);

    IReadOnlyList<DomainEventEntity> List(string? aggregateKind = null, string? eventType = null, int skip = 0, int take = 100);
}
