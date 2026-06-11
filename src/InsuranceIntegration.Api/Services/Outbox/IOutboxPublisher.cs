using InsuranceIntegration.Api.Persistence;

namespace InsuranceIntegration.Api.Services.Outbox;

/// <summary>
/// Sends an outbox message to its downstream destination. Implementations must throw on failure
/// so the dispatcher can record the error and retry on a later poll.
/// </summary>
public interface IOutboxPublisher
{
    Task PublishAsync(OutboxMessageEntity message, CancellationToken cancellationToken = default);
}
