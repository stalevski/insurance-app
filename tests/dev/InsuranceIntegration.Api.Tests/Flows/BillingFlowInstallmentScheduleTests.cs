using InsuranceIntegration.Api.CanonicalContracts.Billing;
using InsuranceIntegration.Api.Services.Flows;

namespace InsuranceIntegration.Api.Tests.Flows;

public sealed class BillingFlowInstallmentScheduleTests
{
    [Test]
    public void Process_DerivesPaidAmountAndOverdueListFromSchedule()
    {
        var service = new BillingFlowService();
        var request = new CanonicalBillingRequest
        {
            PolicyReference = "POL-SCH-1",
            SourceSystem = "TEST",
            InstallmentCount = 0,
            TotalAmount = 0m,
            Installments =
            [
                new BillingInstallment { SequenceNumber = 1, DueDate = new DateOnly(2026, 1, 1), Amount = 500m, Status = BillingInstallmentStatus.Paid, PaidDate = new DateOnly(2026, 1, 2) },
                new BillingInstallment { SequenceNumber = 2, DueDate = new DateOnly(2026, 2, 1), Amount = 500m, Status = BillingInstallmentStatus.Paid, PaidDate = new DateOnly(2026, 2, 3) },
                new BillingInstallment { SequenceNumber = 3, DueDate = new DateOnly(2026, 3, 1), Amount = 500m, Status = BillingInstallmentStatus.Overdue },
                new BillingInstallment { SequenceNumber = 4, DueDate = new DateOnly(2026, 4, 1), Amount = 500m, Status = BillingInstallmentStatus.Issued }
            ]
        };

        var result = service.Process(request);

        Assert.That(result.OverdueInstallmentCount, Is.EqualTo(1));
        Assert.That(result.OverdueInstallmentNumbers, Is.EqualTo(new[] { 3 }));
        Assert.That(result.OutstandingBalance, Is.EqualTo(1000m));
        Assert.That(result.NextDueDate, Is.EqualTo(new DateOnly(2026, 3, 1)));
        Assert.That(result.NextInstallmentAmount, Is.EqualTo(500m));
        Assert.That(result.BillingStatus, Is.EqualTo("Delinquent"));
        Assert.That(result.DunningTriggered, Is.True);
    }

    [Test]
    public void Process_FlagsNonPaymentCancellationWhenThreeOrMoreOverdue()
    {
        var service = new BillingFlowService();
        var request = new CanonicalBillingRequest
        {
            PolicyReference = "POL-SCH-2",
            SourceSystem = "TEST",
            Installments =
            [
                new BillingInstallment { SequenceNumber = 1, DueDate = new DateOnly(2026, 1, 1), Amount = 500m, Status = BillingInstallmentStatus.Overdue },
                new BillingInstallment { SequenceNumber = 2, DueDate = new DateOnly(2026, 2, 1), Amount = 500m, Status = BillingInstallmentStatus.Overdue },
                new BillingInstallment { SequenceNumber = 3, DueDate = new DateOnly(2026, 3, 1), Amount = 500m, Status = BillingInstallmentStatus.Overdue },
                new BillingInstallment { SequenceNumber = 4, DueDate = new DateOnly(2026, 4, 1), Amount = 500m, Status = BillingInstallmentStatus.Issued }
            ]
        };

        var result = service.Process(request);

        Assert.That(result.NonPaymentCancellationRecommended, Is.True);
        Assert.That(result.FinalStatus, Is.EqualTo("PendingNonPaymentCancellation"));
        Assert.That(result.BillingStatus, Is.EqualTo("SeverelyDelinquent"));
    }

    [Test]
    public void Process_TreatsAllPaidScheduleAsPaidInFull()
    {
        var service = new BillingFlowService();
        var request = new CanonicalBillingRequest
        {
            PolicyReference = "POL-SCH-3",
            SourceSystem = "TEST",
            Installments =
            [
                new BillingInstallment { SequenceNumber = 1, DueDate = new DateOnly(2026, 1, 1), Amount = 500m, Status = BillingInstallmentStatus.Paid },
                new BillingInstallment { SequenceNumber = 2, DueDate = new DateOnly(2026, 2, 1), Amount = 500m, Status = BillingInstallmentStatus.Paid }
            ]
        };

        var result = service.Process(request);

        Assert.That(result.OutstandingBalance, Is.EqualTo(0m));
        Assert.That(result.BillingStatus, Is.EqualTo("PaidInFull"));
        Assert.That(result.NextDueDate, Is.Null);
        Assert.That(result.NonPaymentCancellationRecommended, Is.False);
    }

    [Test]
    public void Process_IgnoresCancelledInstallmentsFromTotalBillable()
    {
        var service = new BillingFlowService();
        var request = new CanonicalBillingRequest
        {
            PolicyReference = "POL-SCH-4",
            SourceSystem = "TEST",
            Installments =
            [
                new BillingInstallment { SequenceNumber = 1, DueDate = new DateOnly(2026, 1, 1), Amount = 500m, Status = BillingInstallmentStatus.Paid },
                new BillingInstallment { SequenceNumber = 2, DueDate = new DateOnly(2026, 2, 1), Amount = 500m, Status = BillingInstallmentStatus.Cancelled },
                new BillingInstallment { SequenceNumber = 3, DueDate = new DateOnly(2026, 3, 1), Amount = 500m, Status = BillingInstallmentStatus.Issued }
            ]
        };

        var result = service.Process(request);

        Assert.That(result.OutstandingBalance, Is.EqualTo(500m));
        Assert.That(result.NextDueDate, Is.EqualTo(new DateOnly(2026, 3, 1)));
    }

    [Test]
    public void Process_PreservesLegacyBehaviorWhenScheduleNotProvided()
    {
        var service = new BillingFlowService();
        var request = new CanonicalBillingRequest
        {
            PolicyReference = "POL-LEGACY",
            SourceSystem = "TEST",
            InstallmentCount = 4,
            TotalAmount = 2000m,
            PaidToDate = 500m,
            MissedPayments = 1,
            FirstDueDate = new DateOnly(2026, 1, 1)
        };

        var result = service.Process(request);

        Assert.That(result.OutstandingBalance, Is.EqualTo(1500m));
        Assert.That(result.DunningTriggered, Is.True);
        Assert.That(result.NonPaymentCancellationRecommended, Is.False);
        Assert.That(result.OverdueInstallmentCount, Is.EqualTo(0));
    }
}
