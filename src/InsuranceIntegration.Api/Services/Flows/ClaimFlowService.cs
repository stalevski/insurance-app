using InsuranceIntegration.Api.CanonicalContracts.Claims;
using InsuranceIntegration.Api.FinalMessages.Claims;

namespace InsuranceIntegration.Api.Services.Flows;

public sealed class ClaimFlowService : IClaimFlowService
{
    public FinalClaimResponse Process(CanonicalClaimRequest request)
    {
        var severity = ResolveSeverity(request.IncurredAmount);
        var fraudFlag = request.FraudIndicator || severity == "High";
        var triage = ResolveTriage(severity, fraudFlag);
        var reserveStatus = request.ReservedAmount >= request.IncurredAmount ? "FullyReserved" : "UnderReserved";
        var autoClosed = severity == "Low" && !fraudFlag && request.PaidAmount >= request.IncurredAmount;
        var outstanding = Math.Max(0m, request.IncurredAmount - request.PaidAmount);

        var reasons = new List<string>
        {
            $"Severity resolved as {severity} from incurred {request.IncurredAmount:0.##}",
            $"Reserve status: {reserveStatus}",
            $"Fraud flag: {fraudFlag}",
            $"Triage decision: {triage}",
            autoClosed ? "Claim auto-closed due to low severity and full payment" : "Claim remains open"
        };

        return new FinalClaimResponse
        {
            EntityId = request.EntityId,
            ClaimReference = request.ClaimReference,
            PolicyReference = request.PolicyReference,
            ClaimantName = request.ClaimantName,
            SourceSystem = request.SourceSystem,
            Severity = severity,
            TriageDecision = triage,
            ReserveStatus = reserveStatus,
            FraudFlagRaised = fraudFlag,
            AutoClosed = autoClosed,
            IncurredAmount = request.IncurredAmount,
            ReservedAmount = request.ReservedAmount,
            OutstandingAmount = outstanding,
            DecisionReasons = reasons,
            FinalStatus = autoClosed ? "Closed" : "Open"
        };
    }

    private static string ResolveSeverity(decimal incurred)
    {
        if (incurred >= 50000m)
        {
            return "High";
        }

        if (incurred >= 5000m)
        {
            return "Medium";
        }

        return "Low";
    }

    private static string ResolveTriage(string severity, bool fraudFlag)
    {
        if (fraudFlag)
        {
            return "SuspectedFraud";
        }

        return severity switch
        {
            "High" => "Escalate",
            "Medium" => "ManualReview",
            _ => "AutoProcess"
        };
    }
}
