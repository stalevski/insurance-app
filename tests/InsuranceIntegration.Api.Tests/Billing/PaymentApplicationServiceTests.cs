using InsuranceIntegration.Api.CanonicalContracts.Billing;
using InsuranceIntegration.Api.Services.Billing;
using InsuranceIntegration.Api.Services.Flows;
using Microsoft.Extensions.Time.Testing;

namespace InsuranceIntegration.Api.Tests.Billing;

public sealed class PaymentApplicationServiceTests
{
    private static readonly FakeTimeProvider Time = new(new DateTimeOffset(2026, 6, 13, 0, 0, 0, TimeSpan.Zero));

    private static readonly int[] SingleSecond = [2];
    private static readonly int[] FirstTwo = [1, 2];
    private static readonly int[] AllFour = [1, 2, 3, 4];

    private static PaymentApplicationService CreateService() =>
        new(new BillingFlowService(), Time);

    private static List<BillingInstallment> BuildSchedule(params string[] statuses)
    {
        return statuses
            .Select((status, index) => new BillingInstallment
            {
                SequenceNumber = index + 1,
                DueDate = new DateOnly(2026, 1, 1).AddMonths(index * 3),
                Amount = 250m,
                Status = status
            })
            .ToList();
    }

    [Test]
    public void RecordPayment_SettlesTargetedInstallment_AndRecomputesOutstanding()
    {
        var service = CreateService();
        var request = new PaymentRecordRequest
        {
            PolicyReference = "POL-PAY-001",
            SourceSystem = "PAYMENTRAIL",
            Installments = BuildSchedule(
                BillingInstallmentStatus.Paid,
                BillingInstallmentStatus.Overdue,
                BillingInstallmentStatus.Issued,
                BillingInstallmentStatus.Planned),
            Amount = 250m,
            InstallmentNumber = 2,
            PaymentReference = "PMT-22"
        };

        var result = service.RecordPayment(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.SettledInstallmentNumbers, Is.EqualTo(SingleSecond));
            Assert.That(result.AmountApplied, Is.EqualTo(250m));
            Assert.That(result.UnappliedCredit, Is.EqualTo(0m));
            Assert.That(result.Installments.Single(i => i.SequenceNumber == 2).Status, Is.EqualTo(BillingInstallmentStatus.Paid));
            Assert.That(result.Installments.Single(i => i.SequenceNumber == 2).PaymentReference, Is.EqualTo("PMT-22"));
            Assert.That(result.Billing.OutstandingBalance, Is.EqualTo(500m));
            Assert.That(result.Billing.OverdueInstallmentCount, Is.EqualTo(0));
        });
    }

    [Test]
    public void RecordPayment_SweepsEarliestOpenInstallments_WhenNoInstallmentNumber()
    {
        var service = CreateService();
        var request = new PaymentRecordRequest
        {
            PolicyReference = "POL-PAY-002",
            SourceSystem = "PAYMENTRAIL",
            Installments = BuildSchedule(
                BillingInstallmentStatus.Issued,
                BillingInstallmentStatus.Issued,
                BillingInstallmentStatus.Issued,
                BillingInstallmentStatus.Issued),
            Amount = 500m
        };

        var result = service.RecordPayment(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.SettledInstallmentNumbers, Is.EqualTo(FirstTwo));
            Assert.That(result.AmountApplied, Is.EqualTo(500m));
            Assert.That(result.UnappliedCredit, Is.EqualTo(0m));
            Assert.That(result.Billing.OutstandingBalance, Is.EqualTo(500m));
        });
    }

    [Test]
    public void RecordPayment_PaysEntireSchedule_MarksPaidInFull()
    {
        var service = CreateService();
        var request = new PaymentRecordRequest
        {
            PolicyReference = "POL-PAY-003",
            SourceSystem = "PAYMENTRAIL",
            Installments = BuildSchedule(
                BillingInstallmentStatus.Issued,
                BillingInstallmentStatus.Issued,
                BillingInstallmentStatus.Issued,
                BillingInstallmentStatus.Issued),
            Amount = 1000m
        };

        var result = service.RecordPayment(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.SettledInstallmentNumbers, Is.EqualTo(AllFour));
            Assert.That(result.Billing.OutstandingBalance, Is.EqualTo(0m));
            Assert.That(result.Billing.BillingStatus, Is.EqualTo("PaidInFull"));
        });
    }

    [Test]
    public void RecordPayment_OverpaymentLeavesUnappliedCredit()
    {
        var service = CreateService();
        var request = new PaymentRecordRequest
        {
            PolicyReference = "POL-PAY-004",
            SourceSystem = "PAYMENTRAIL",
            Installments = BuildSchedule(
                BillingInstallmentStatus.Issued,
                BillingInstallmentStatus.Issued),
            Amount = 600m
        };

        var result = service.RecordPayment(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.SettledInstallmentNumbers, Is.EqualTo(FirstTwo));
            Assert.That(result.AmountApplied, Is.EqualTo(500m));
            Assert.That(result.UnappliedCredit, Is.EqualTo(100m));
            Assert.That(result.Billing.OutstandingBalance, Is.EqualTo(0m));
        });
    }

    [Test]
    public void RecordPayment_AmountBelowInstallment_SettlesNothingAndReturnsCredit()
    {
        var service = CreateService();
        var request = new PaymentRecordRequest
        {
            PolicyReference = "POL-PAY-005",
            SourceSystem = "PAYMENTRAIL",
            Installments = BuildSchedule(BillingInstallmentStatus.Issued, BillingInstallmentStatus.Issued),
            Amount = 100m
        };

        var result = service.RecordPayment(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.SettledInstallmentNumbers, Is.Empty);
            Assert.That(result.AmountApplied, Is.EqualTo(0m));
            Assert.That(result.UnappliedCredit, Is.EqualTo(100m));
            Assert.That(result.Billing.OutstandingBalance, Is.EqualTo(500m));
        });
    }

    [Test]
    public void RecordPayment_DefaultsPaidDateFromTimeProvider()
    {
        var service = CreateService();
        var request = new PaymentRecordRequest
        {
            PolicyReference = "POL-PAY-006",
            SourceSystem = "PAYMENTRAIL",
            Installments = BuildSchedule(BillingInstallmentStatus.Issued),
            Amount = 250m
        };

        var result = service.RecordPayment(request);

        Assert.That(result.Installments.Single().PaidDate, Is.EqualTo(new DateOnly(2026, 6, 13)));
    }

    [Test]
    public void RecordPayment_ZeroAmount_Throws()
    {
        var service = CreateService();
        var request = new PaymentRecordRequest
        {
            PolicyReference = "POL-PAY-007",
            SourceSystem = "PAYMENTRAIL",
            Installments = BuildSchedule(BillingInstallmentStatus.Issued),
            Amount = 0m
        };

        Assert.Throws<ArgumentException>(() => service.RecordPayment(request));
    }

    [Test]
    public void RecordPayment_TargetingAlreadyPaidInstallment_Throws()
    {
        var service = CreateService();
        var request = new PaymentRecordRequest
        {
            PolicyReference = "POL-PAY-008",
            SourceSystem = "PAYMENTRAIL",
            Installments = BuildSchedule(BillingInstallmentStatus.Paid, BillingInstallmentStatus.Issued),
            Amount = 250m,
            InstallmentNumber = 1
        };

        Assert.Throws<ArgumentException>(() => service.RecordPayment(request));
    }
}
