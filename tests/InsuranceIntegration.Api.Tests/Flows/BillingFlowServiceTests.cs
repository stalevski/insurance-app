using InsuranceIntegration.Api.CanonicalContracts.Billing;
using InsuranceIntegration.Api.Services.Flows;

namespace InsuranceIntegration.Api.Tests.Flows;

public sealed class BillingFlowServiceTests
{
    [Test]
    public void Process_MarksPaidInFullWhenPaymentCoversTotal()
    {
        var service = new BillingFlowService();
        var request = new CanonicalBillingRequest
        {
            PolicyReference = "POL-1",
            SourceSystem = "PAYMENTRAIL",
            InstallmentCount = 4,
            TotalAmount = 1000m,
            PaidToDate = 1000m
        };

        var result = service.Process(request);

        Assert.That(result.BillingStatus, Is.EqualTo("PaidInFull"));
        Assert.That(result.OutstandingBalance, Is.EqualTo(0m));
    }

    [Test]
    public void Process_TriggersDunningAfterMissedPayment()
    {
        var service = new BillingFlowService();
        var request = new CanonicalBillingRequest
        {
            PolicyReference = "POL-2",
            SourceSystem = "PAYMENTRAIL",
            InstallmentCount = 4,
            TotalAmount = 1000m,
            PaidToDate = 250m,
            MissedPayments = 1
        };

        var result = service.Process(request);

        Assert.That(result.DunningTriggered, Is.True);
        Assert.That(result.BillingStatus, Is.EqualTo("Delinquent"));
        Assert.That(result.NonPaymentCancellationRecommended, Is.False);
    }

    [Test]
    public void Process_RecommendsNonPaymentCancellationAfterRepeatedMisses()
    {
        var service = new BillingFlowService();
        var request = new CanonicalBillingRequest
        {
            PolicyReference = "POL-3",
            SourceSystem = "PAYMENTRAIL",
            InstallmentCount = 4,
            TotalAmount = 1000m,
            PaidToDate = 250m,
            MissedPayments = 3
        };

        var result = service.Process(request);

        Assert.That(result.NonPaymentCancellationRecommended, Is.True);
        Assert.That(result.BillingStatus, Is.EqualTo("SeverelyDelinquent"));
        Assert.That(result.FinalStatus, Is.EqualTo("PendingNonPaymentCancellation"));
    }
}
