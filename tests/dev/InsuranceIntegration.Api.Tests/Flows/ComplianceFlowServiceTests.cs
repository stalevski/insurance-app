using InsuranceIntegration.Api.CanonicalContracts.Compliance;
using InsuranceIntegration.Api.Services.Flows;

namespace InsuranceIntegration.Api.Tests.Flows;

public sealed class ComplianceFlowServiceTests
{
    [Test]
    public void Process_BlocksBindWhenSanctionsHitDetected()
    {
        var service = new ComplianceFlowService();
        var request = new CanonicalComplianceRequest
        {
            PartyName = "Northwind Storage Ltd",
            SourceSystem = "SANCTIONSCAN",
            ScreeningResult = "Clear",
            HasSanctionsHit = true
        };

        var result = service.Process(request);

        Assert.That(result.Decision, Is.EqualTo("SanctionsBlock"));
        Assert.That(result.BlocksBind, Is.True);
        Assert.That(result.FinalStatus, Is.EqualTo("Blocked"));
    }

    [Test]
    public void Process_RequiresEnhancedDueDiligenceForPoliticallyExposedPerson()
    {
        var service = new ComplianceFlowService();
        var request = new CanonicalComplianceRequest
        {
            PartyName = "Alex Doe",
            SourceSystem = "SANCTIONSCAN",
            ScreeningResult = "Clear",
            IsPoliticallyExposed = true
        };

        var result = service.Process(request);

        Assert.That(result.Decision, Is.EqualTo("EnhancedDueDiligence"));
        Assert.That(result.RequiresEnhancedDueDiligence, Is.True);
        Assert.That(result.BlocksBind, Is.False);
    }

    [Test]
    public void Process_ReturnsClearWhenNoConcerns()
    {
        var service = new ComplianceFlowService();
        var request = new CanonicalComplianceRequest
        {
            PartyName = "Alex Doe",
            SourceSystem = "SANCTIONSCAN",
            ScreeningResult = "Clear",
            Score = 0
        };

        var result = service.Process(request);

        Assert.That(result.Decision, Is.EqualTo("Clear"));
        Assert.That(result.BlocksBind, Is.False);
    }
}
