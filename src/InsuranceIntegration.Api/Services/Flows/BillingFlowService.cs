using InsuranceIntegration.Api.CanonicalContracts.Billing;
using InsuranceIntegration.Api.Responses.Billing;

namespace InsuranceIntegration.Api.Services.Flows;

public sealed class BillingFlowService : IBillingFlowService
{
    private const int MissedPaymentDunningThreshold = 1;
    private const int MissedPaymentCancellationThreshold = 3;

    public FinalBillingResponse Process(CanonicalBillingRequest request)
    {
        var hasSchedule = request.Installments.Count > 0;
        var orderedInstallments = request.Installments
            .OrderBy(installment => installment.SequenceNumber)
            .ThenBy(installment => installment.DueDate)
            .ToList();

        var installmentCount = hasSchedule ? orderedInstallments.Count : request.InstallmentCount;
        var paidFromSchedule = orderedInstallments
            .Where(installment => BillingInstallmentStatus.IsPaid(installment.Status))
            .Sum(installment => installment.Amount);
        var paidToDate = hasSchedule ? paidFromSchedule : request.PaidToDate;
        var totalAmount = hasSchedule
            ? orderedInstallments
                .Where(installment => !BillingInstallmentStatus.IsCancelled(installment.Status))
                .Sum(installment => installment.Amount)
            : request.TotalAmount;
        var overdueInstallments = orderedInstallments
            .Where(installment => BillingInstallmentStatus.IsOverdue(installment.Status))
            .ToList();
        var missedPayments = hasSchedule ? overdueInstallments.Count : request.MissedPayments;
        var outstanding = Math.Max(0m, totalAmount - paidToDate);

        var openInstallments = orderedInstallments
            .Where(installment => BillingInstallmentStatus.IsOpen(installment.Status))
            .ToList();
        var nextInstallment = openInstallments.FirstOrDefault();

        decimal installmentAmount;
        if (hasSchedule)
        {
            installmentAmount = nextInstallment?.Amount
                ?? orderedInstallments.LastOrDefault()?.Amount
                ?? 0m;
        }
        else
        {
            installmentAmount = installmentCount > 0
                ? Math.Round(totalAmount / installmentCount, 2, MidpointRounding.AwayFromZero)
                : 0m;
        }

        var billingStatus = ResolveStatus(outstanding, missedPayments);
        var dunningTriggered = missedPayments >= MissedPaymentDunningThreshold;
        var nonPaymentCancellation = missedPayments >= MissedPaymentCancellationThreshold;
        var nextDueDate = hasSchedule
            ? nextInstallment?.DueDate
            : ResolveScheduleFreeNextDueDate(request.FirstDueDate, installmentCount, paidToDate, installmentAmount);

        var reasons = new List<string>
        {
            $"Installment amount: {installmentAmount:0.##}",
            $"Outstanding balance: {outstanding:0.##}",
            $"Missed payments: {missedPayments}",
            $"Dunning triggered: {dunningTriggered}",
            $"Non-payment cancellation recommended: {nonPaymentCancellation}"
        };

        if (hasSchedule)
        {
            reasons.Add($"Schedule provided: {orderedInstallments.Count} installments; total billable {totalAmount:0.##}, paid {paidToDate:0.##}");
            if (overdueInstallments.Count > 0)
            {
                reasons.Add($"Overdue installments: {string.Join(", ", overdueInstallments.Select(item => item.SequenceNumber))}");
            }
        }

        return new FinalBillingResponse
        {
            EntityId = request.EntityId,
            PolicyReference = request.PolicyReference,
            SourceSystem = request.SourceSystem,
            InstallmentAmount = installmentAmount,
            OutstandingBalance = outstanding,
            InstallmentCount = installmentCount,
            BillingStatus = billingStatus,
            DunningTriggered = dunningTriggered,
            NonPaymentCancellationRecommended = nonPaymentCancellation,
            NextDueDate = nextDueDate,
            OverdueInstallmentCount = overdueInstallments.Count,
            OverdueInstallmentNumbers = overdueInstallments.Select(item => item.SequenceNumber).ToList(),
            NextInstallmentAmount = nextInstallment?.Amount ?? installmentAmount,
            DecisionReasons = reasons,
            FinalStatus = nonPaymentCancellation ? "PendingNonPaymentCancellation" : billingStatus
        };
    }

    private static string ResolveStatus(decimal outstanding, int missedPayments)
    {
        if (outstanding <= 0m)
        {
            return "PaidInFull";
        }

        if (missedPayments >= MissedPaymentCancellationThreshold)
        {
            return "SeverelyDelinquent";
        }

        if (missedPayments >= MissedPaymentDunningThreshold)
        {
            return "Delinquent";
        }

        return "Current";
    }

    // Without an explicit schedule the next due date is derived from the billing
    // frequency implied by the installment count (12 -> monthly, 4 -> quarterly,
    // 2 -> semi-annual, 1 -> annual) advanced by the number of settled installments.
    // It must not be driven by the count of missed payments.
    private static DateOnly? ResolveScheduleFreeNextDueDate(DateOnly? firstDueDate, int installmentCount, decimal paidToDate, decimal installmentAmount)
    {
        if (firstDueDate is null)
        {
            return null;
        }

        var settledInstallments = installmentAmount > 0m
            ? (int)Math.Floor(paidToDate / installmentAmount)
            : 0;

        if (installmentCount > 0)
        {
            settledInstallments = Math.Clamp(settledInstallments, 0, installmentCount);

            // Every installment is settled -> nothing further is due.
            if (settledInstallments >= installmentCount)
            {
                return null;
            }
        }
        else
        {
            settledInstallments = Math.Max(settledInstallments, 0);
        }

        var billingPeriodMonths = ResolveBillingPeriodMonths(installmentCount);
        return firstDueDate.Value.AddMonths(settledInstallments * billingPeriodMonths);
    }

    private static int ResolveBillingPeriodMonths(int installmentCount)
    {
        if (installmentCount <= 1)
        {
            return 12;
        }

        // Assume a 12-month billing term.
        return Math.Max(1, 12 / installmentCount);
    }
}
