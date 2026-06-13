using InsuranceIntegration.Api.CanonicalContracts.Billing;
using InsuranceIntegration.Api.Responses.Billing;

namespace InsuranceIntegration.Api.Services.Billing;

public sealed class DelinquencyAssessmentResult
{
    public string PolicyReference { get; init; } = string.Empty;

    public DateOnly AsOfDate { get; init; }

    /// <summary>Sequence numbers transitioned from open to <c>Overdue</c> by this assessment.</summary>
    public IReadOnlyList<int> NewlyOverdueInstallmentNumbers { get; init; } = [];

    /// <summary>All overdue sequence numbers after the assessment (newly + previously flagged).</summary>
    public IReadOnlyList<int> OverdueInstallmentNumbers { get; init; } = [];

    public bool DunningTriggered { get; init; }

    public bool NonPaymentCancellationRecommended { get; init; }

    /// <summary>The installment schedule after delinquency flagging.</summary>
    public List<BillingInstallment> Installments { get; init; } = [];

    /// <summary>The recomputed billing position after delinquency flagging.</summary>
    public FinalBillingResponse Billing { get; init; } = new();

    public List<string> Reasons { get; init; } = [];
}
