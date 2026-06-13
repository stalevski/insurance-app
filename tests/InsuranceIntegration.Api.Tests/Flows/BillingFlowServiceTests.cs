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

    [Test]
    public void Process_DerivesQuarterlyNextDueDateFromBillingFrequency_WhenScheduleNotProvided()
    {
        var service = new BillingFlowService();
        var request = new CanonicalBillingRequest
        {
            PolicyReference = "POL-FREQ",
            SourceSystem = "PAYMENTRAIL",
            InstallmentCount = 4,                       // quarterly over a 12-month term
            TotalAmount = 2000m,
            PaidToDate = 500m,                          // one installment (500) settled
            MissedPayments = 2,                         // missed count must NOT drive the next due date
            FirstDueDate = new DateOnly(2026, 1, 1)
        };

        var result = service.Process(request);

        // Installment #1 was due Jan 1 (settled); the next quarterly installment falls on Apr 1,
        // regardless of how many payments were missed.
        Assert.That(result.NextDueDate, Is.EqualTo(new DateOnly(2026, 4, 1)));
    }

    [Test]
    public void Process_DerivesMonthlyNextDueDateFromBillingFrequency_WhenScheduleNotProvided()
    {
        var service = new BillingFlowService();
        var request = new CanonicalBillingRequest
        {
            PolicyReference = "POL-MONTHLY",
            SourceSystem = "PAYMENTRAIL",
            InstallmentCount = 12,                      // monthly
            TotalAmount = 1200m,
            PaidToDate = 300m,                          // three installments (100 each) settled
            MissedPayments = 1,
            FirstDueDate = new DateOnly(2026, 1, 1)
        };

        var result = service.Process(request);

        // 100 per installment, 3 settled -> the next monthly installment is due Apr 1.
        Assert.That(result.NextDueDate, Is.EqualTo(new DateOnly(2026, 4, 1)));
    }
}
