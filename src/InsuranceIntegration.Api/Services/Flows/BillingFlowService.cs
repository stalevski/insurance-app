using InsuranceIntegration.Api.CanonicalContracts.Billing;
using InsuranceIntegration.Api.FinalMessages.Billing;

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
            : request.FirstDueDate?.AddMonths(Math.Max(request.MissedPayments, 0));

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
}
