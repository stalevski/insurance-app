namespace InsuranceIntegration.Api.FinalMessages.Billing;

public sealed class FinalBillingResponse
{
    public Guid EntityId { get; init; }

    public string PolicyReference { get; init; } = string.Empty;

    public string SourceSystem { get; init; } = string.Empty;

    public decimal InstallmentAmount { get; init; }

    public decimal OutstandingBalance { get; init; }

    public int InstallmentCount { get; init; }

    public string BillingStatus { get; init; } = string.Empty;

    public bool DunningTriggered { get; init; }

    public bool NonPaymentCancellationRecommended { get; init; }

    public DateOnly? NextDueDate { get; init; }

    public List<string> DecisionReasons { get; init; } = [];

    public string FinalStatus { get; init; } = string.Empty;
}
