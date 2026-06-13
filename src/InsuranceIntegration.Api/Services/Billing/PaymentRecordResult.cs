using InsuranceIntegration.Api.CanonicalContracts.Billing;
using InsuranceIntegration.Api.Responses.Billing;

namespace InsuranceIntegration.Api.Services.Billing;

public sealed class PaymentRecordResult
{
    public string PolicyReference { get; init; } = string.Empty;

    /// <summary>Portion of the payment that fully settled one or more installments.</summary>
    public decimal AmountApplied { get; init; }

    /// <summary>Remainder of the payment that did not fully cover a further installment (a credit).</summary>
    public decimal UnappliedCredit { get; init; }

    /// <summary>Sequence numbers of the installments settled by this payment.</summary>
    public IReadOnlyList<int> SettledInstallmentNumbers { get; init; } = [];

    /// <summary>The installment schedule after applying the payment.</summary>
    public List<BillingInstallment> Installments { get; init; } = [];

    /// <summary>The recomputed billing position after applying the payment.</summary>
    public FinalBillingResponse Billing { get; init; } = new();

    public List<string> Reasons { get; init; } = [];
}
