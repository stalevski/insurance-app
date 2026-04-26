using InsuranceIntegration.Api.CanonicalContracts.Compliance;
using InsuranceIntegration.Api.Responses.Compliance;

namespace InsuranceIntegration.Api.Services.Flows;

public sealed class ComplianceFlowService : IComplianceFlowService
{
    public FinalComplianceResponse Process(CanonicalComplianceRequest request)
    {
        var reasons = new List<string>();
        var decision = "Clear";
        var blocks = false;
        var edd = false;

        if (request.HasSanctionsHit)
        {
            decision = "SanctionsBlock";
            blocks = true;
            reasons.Add("Sanctions hit detected; submission cannot be bound");
        }
        else if (request.IsPoliticallyExposed)
        {
            decision = "EnhancedDueDiligence";
            edd = true;
            reasons.Add("Politically exposed person detected; enhanced due diligence required");
        }
        else if (string.Equals(request.ScreeningResult, "Flagged", StringComparison.OrdinalIgnoreCase))
        {
            decision = "ManualReview";
            reasons.Add("Screening result flagged; manual compliance review required");
        }
        else if (request.Score >= 50)
        {
            decision = "EnhancedDueDiligence";
            edd = true;
            reasons.Add($"Screening score {request.Score} exceeds enhanced due-diligence threshold");
        }
        else
        {
            reasons.Add("No compliance concerns identified");
        }

        return new FinalComplianceResponse
        {
            EntityId = request.EntityId,
            PartyName = request.PartyName,
            SourceSystem = request.SourceSystem,
            EntityReference = request.EntityReference,
            Decision = decision,
            BlocksBind = blocks,
            RequiresEnhancedDueDiligence = edd,
            DecisionReasons = reasons,
            FinalStatus = blocks ? "Blocked" : decision
        };
    }
}
