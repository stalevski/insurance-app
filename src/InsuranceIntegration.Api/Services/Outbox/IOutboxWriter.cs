namespace InsuranceIntegration.Api.Services.Outbox;

public interface IOutboxWriter
{
    void Enqueue<TPayload>(string aggregateType, Guid aggregateId, string eventType, TPayload payload);
}
