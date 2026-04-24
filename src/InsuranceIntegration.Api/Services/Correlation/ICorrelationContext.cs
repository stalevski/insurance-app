namespace InsuranceIntegration.Api.Services.Correlation;

public interface ICorrelationContext
{
    Guid CorrelationId { get; }

    Guid? CausationId { get; }

    void Set(Guid correlationId, Guid? causationId = null);
}
