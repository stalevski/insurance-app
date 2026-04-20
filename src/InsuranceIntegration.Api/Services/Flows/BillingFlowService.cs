using InsuranceIntegration.Api.CanonicalContracts.Billing;
using InsuranceIntegration.Api.FinalMessages.Billing;

namespace InsuranceIntegration.Api.Services.Flows;

public sealed class BillingFlowService : IBillingFlowService
{
    private const int MissedPaymentDunningThreshold = 1;
    private const int MissedPaymentCancellationThreshold = 3;

    public FinalBillingResponse Process(CanonicalBillingRequest request)
    {
        var installmentAmount = request.InstallmentCount > 0
            ? Math.Round(request.TotalAmount / request.InstallmentCount, 2, MidpointRounding.AwayFromZero)
            : 0m;
        var outstanding = Math.Max(0m, request.TotalAmount - request.PaidToDate);
        var billingStatus = ResolveStatus(outstanding, request.MissedPayments);
        var dunningTriggered = request.MissedPayments >= MissedPaymentDunningThreshold;
        var nonPaymentCancellation = request.MissedPayments >= MissedPaymentCancellationThreshold;
        var nextDueDate = request.FirstDueDate?.AddMonths(Math.Max(request.MissedPayments, 0));

        var reasons = new List<string>
        {
            $"Installment amount: {installmentAmount:0.##}",
            $"Outstanding balance: {outstanding:0.##}",
            $"Missed payments: {request.MissedPayments}",
            $"Dunning triggered: {dunningTriggered}",
            $"Non-payment cancellation recommended: {nonPaymentCancellation}"
        };

        return new FinalBillingResponse
        {
            EntityId = request.EntityId,
            PolicyReference = request.PolicyReference,
            SourceSystem = request.SourceSystem,
            InstallmentAmount = installmentAmount,
            OutstandingBalance = outstanding,
            InstallmentCount = request.InstallmentCount,
            BillingStatus = billingStatus,
            DunningTriggered = dunningTriggered,
            NonPaymentCancellationRecommended = nonPaymentCancellation,
            NextDueDate = nextDueDate,
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
