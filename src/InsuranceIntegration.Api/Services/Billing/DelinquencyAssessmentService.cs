using InsuranceIntegration.Api.CanonicalContracts.Billing;
using InsuranceIntegration.Api.Services.Flows;

namespace InsuranceIntegration.Api.Services.Billing;

public sealed class DelinquencyAssessmentService : IDelinquencyAssessmentService
{
    private readonly IBillingFlowService _billingFlowService;
    private readonly TimeProvider _timeProvider;

    public DelinquencyAssessmentService(IBillingFlowService billingFlowService, TimeProvider timeProvider)
    {
        _billingFlowService = billingFlowService;
        _timeProvider = timeProvider;
    }

    public DelinquencyAssessmentResult Assess(DelinquencyAssessmentRequest request)
    {
        if (request.Installments.Count == 0)
        {
            throw new ArgumentException("A delinquency assessment requires at least one installment in the schedule.");
        }

        if (request.GracePeriodDays < 0)
        {
            throw new ArgumentException("Grace period days cannot be negative.");
        }

        var asOfDate = request.AsOfDate ?? DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        var cutoff = asOfDate.AddDays(-request.GracePeriodDays);

        var ordered = request.Installments
            .OrderBy(installment => installment.SequenceNumber)
            .ThenBy(installment => installment.DueDate)
            .ToList();

        var newlyOverdue = new List<int>();
        var updated = new List<BillingInstallment>(ordered.Count);

        foreach (var installment in ordered)
        {
            var isNowOverdue = BillingInstallmentStatus.IsOpen(installment.Status)
                && !BillingInstallmentStatus.IsOverdue(installment.Status)
                && installment.DueDate < cutoff;

            if (isNowOverdue)
            {
                newlyOverdue.Add(installment.SequenceNumber);
                updated.Add(MarkOverdue(installment));
            }
            else
            {
                updated.Add(installment);
            }
        }

        var recomputed = _billingFlowService.Process(new CanonicalBillingRequest
        {
            EntityId = Guid.NewGuid(),
            PolicyReference = request.PolicyReference,
            SourceSystem = request.SourceSystem,
            CurrencyCode = request.CurrencyCode,
            Installments = updated
        });

        var reasons = new List<string>
        {
            $"Assessed as of {asOfDate:yyyy-MM-dd} (grace period {request.GracePeriodDays} day(s); cutoff {cutoff:yyyy-MM-dd})",
            $"Newly overdue installments: {(newlyOverdue.Count > 0 ? string.Join(", ", newlyOverdue) : "none")}",
            $"Total overdue installments: {recomputed.OverdueInstallmentCount}",
            $"Dunning triggered: {recomputed.DunningTriggered}",
            $"Non-payment cancellation recommended: {recomputed.NonPaymentCancellationRecommended}",
            $"Billing status after assessment: {recomputed.BillingStatus}"
        };

        return new DelinquencyAssessmentResult
        {
            PolicyReference = request.PolicyReference,
            AsOfDate = asOfDate,
            NewlyOverdueInstallmentNumbers = newlyOverdue,
            OverdueInstallmentNumbers = recomputed.OverdueInstallmentNumbers,
            DunningTriggered = recomputed.DunningTriggered,
            NonPaymentCancellationRecommended = recomputed.NonPaymentCancellationRecommended,
            Installments = updated,
            Billing = recomputed,
            Reasons = reasons
        };
    }

    private static BillingInstallment MarkOverdue(BillingInstallment source)
    {
        return new BillingInstallment
        {
            SequenceNumber = source.SequenceNumber,
            DueDate = source.DueDate,
            Amount = source.Amount,
            Status = BillingInstallmentStatus.Overdue,
            IssuedDate = source.IssuedDate,
            PaidDate = source.PaidDate,
            PaymentReference = source.PaymentReference
        };
    }
}
