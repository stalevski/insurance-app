using InsuranceIntegration.Api.CanonicalContracts.Billing;
using InsuranceIntegration.Api.Services.Flows;

namespace InsuranceIntegration.Api.Services.Billing;

public sealed class PaymentApplicationService : IPaymentApplicationService
{
    private readonly IBillingFlowService _billingFlowService;
    private readonly TimeProvider _timeProvider;

    public PaymentApplicationService(IBillingFlowService billingFlowService, TimeProvider timeProvider)
    {
        _billingFlowService = billingFlowService;
        _timeProvider = timeProvider;
    }

    public PaymentRecordResult RecordPayment(PaymentRecordRequest request)
    {
        if (request.Amount <= 0m)
        {
            throw new ArgumentException("Payment amount must be greater than zero.");
        }

        if (request.Installments.Count == 0)
        {
            throw new ArgumentException("A payment requires at least one installment in the schedule.");
        }

        var paidDate = request.PaidDate ?? DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);

        var ordered = request.Installments
            .OrderBy(installment => installment.SequenceNumber)
            .ThenBy(installment => installment.DueDate)
            .ToList();

        if (request.InstallmentNumber is int target)
        {
            var targetInstallment = ordered.FirstOrDefault(installment => installment.SequenceNumber == target)
                ?? throw new ArgumentException($"Installment {target} was not found in the schedule.");

            if (!BillingInstallmentStatus.IsOpen(targetInstallment.Status))
            {
                throw new ArgumentException($"Installment {target} is '{targetInstallment.Status}' and cannot accept a payment.");
            }
        }

        var remaining = request.Amount;
        var settled = new List<int>();
        var updated = new List<BillingInstallment>(ordered.Count);

        foreach (var installment in ordered)
        {
            var isCandidate = BillingInstallmentStatus.IsOpen(installment.Status)
                && installment.Amount > 0m
                && (request.InstallmentNumber is null || installment.SequenceNumber >= request.InstallmentNumber);

            if (isCandidate && remaining >= installment.Amount)
            {
                remaining -= installment.Amount;
                settled.Add(installment.SequenceNumber);
                updated.Add(MarkPaid(installment, paidDate, request.PaymentReference));
            }
            else
            {
                updated.Add(installment);
            }
        }

        var amountApplied = request.Amount - remaining;

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
            $"Payment received: {request.Amount:0.##}",
            $"Installments settled: {(settled.Count > 0 ? string.Join(", ", settled) : "none")}",
            $"Amount applied to installments: {amountApplied:0.##}",
            $"Unapplied credit: {remaining:0.##}",
            $"Outstanding balance after payment: {recomputed.OutstandingBalance:0.##}",
            $"Billing status after payment: {recomputed.BillingStatus}"
        };

        return new PaymentRecordResult
        {
            PolicyReference = request.PolicyReference,
            AmountApplied = amountApplied,
            UnappliedCredit = remaining,
            SettledInstallmentNumbers = settled,
            Installments = updated,
            Billing = recomputed,
            Reasons = reasons
        };
    }

    private static BillingInstallment MarkPaid(BillingInstallment source, DateOnly paidDate, string? paymentReference)
    {
        return new BillingInstallment
        {
            SequenceNumber = source.SequenceNumber,
            DueDate = source.DueDate,
            Amount = source.Amount,
            Status = BillingInstallmentStatus.Paid,
            IssuedDate = source.IssuedDate,
            PaidDate = paidDate,
            PaymentReference = paymentReference ?? source.PaymentReference
        };
    }
}
