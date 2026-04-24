namespace InsuranceIntegration.Api.Services.Correlation;

public sealed class CorrelationContext : ICorrelationContext
{
    public Guid CorrelationId { get; private set; } = Guid.CreateVersion7();

    public Guid? CausationId { get; private set; }

    public void Set(Guid correlationId, Guid? causationId = null)
    {
        CorrelationId = correlationId;
        CausationId = causationId;
    }
}
