using InsuranceIntegration.Api.CanonicalContracts.Billing;
using InsuranceIntegration.Api.Services.Billing;
using InsuranceIntegration.Api.Services.Flows;
using Microsoft.Extensions.Time.Testing;

namespace InsuranceIntegration.Api.Tests.Billing;

public sealed class DelinquencyAssessmentServiceTests
{
    private static readonly FakeTimeProvider Time = new(new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero));

    private static readonly int[] First = [1];
    private static readonly int[] Third = [3];
    private static readonly int[] FirstTwo = [1, 2];
    private static readonly int[] FirstThree = [1, 2, 3];
    private static readonly int[] None = [];

    private static DelinquencyAssessmentService CreateService() =>
        new(new BillingFlowService(), Time);

    private static List<BillingInstallment> BuildSchedule(params (int Seq, DateOnly Due, string Status)[] rows)
    {
        return rows
            .Select(row => new BillingInstallment
            {
                SequenceNumber = row.Seq,
                DueDate = row.Due,
                Amount = 200m,
                Status = row.Status
            })
            .ToList();
    }

    [Test]
    public void Assess_FlagsOpenInstallmentsPastDue_AsOverdue()
    {
        var service = CreateService();
        var request = new DelinquencyAssessmentRequest
        {
            PolicyReference = "POL-DLQ-001",
            SourceSystem = "BILLINGENGINE",
            AsOfDate = new DateOnly(2026, 7, 1),
            Installments = BuildSchedule(
                (1, new DateOnly(2026, 4, 1), BillingInstallmentStatus.Issued),
                (2, new DateOnly(2026, 5, 1), BillingInstallmentStatus.Issued),
                (3, new DateOnly(2026, 8, 1), BillingInstallmentStatus.Issued))
        };

        var result = service.Assess(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.NewlyOverdueInstallmentNumbers, Is.EqualTo(FirstTwo));
            Assert.That(result.OverdueInstallmentNumbers, Is.EqualTo(FirstTwo));
            Assert.That(result.Installments.Single(i => i.SequenceNumber == 3).Status, Is.EqualTo(BillingInstallmentStatus.Issued));
            Assert.That(result.DunningTriggered, Is.True);
        });
    }

    [Test]
    public void Assess_RespectsGracePeriod()
    {
        var service = CreateService();
        var request = new DelinquencyAssessmentRequest
        {
            PolicyReference = "POL-DLQ-002",
            SourceSystem = "BILLINGENGINE",
            AsOfDate = new DateOnly(2026, 7, 1),
            GracePeriodDays = 45,
            Installments = BuildSchedule(
                (1, new DateOnly(2026, 5, 1), BillingInstallmentStatus.Issued),
                (2, new DateOnly(2026, 6, 1), BillingInstallmentStatus.Issued))
        };

        var result = service.Assess(request);

        // Cutoff = 2026-07-01 minus 45 days = 2026-05-17: only installment 1 (due 2026-05-01) is past it.
        Assert.Multiple(() =>
        {
            Assert.That(result.NewlyOverdueInstallmentNumbers, Is.EqualTo(First));
            Assert.That(result.Installments.Single(i => i.SequenceNumber == 2).Status, Is.EqualTo(BillingInstallmentStatus.Issued));
        });
    }

    [Test]
    public void Assess_RecommendsCancellation_WhenThreeOrMoreOverdue()
    {
        var service = CreateService();
        var request = new DelinquencyAssessmentRequest
        {
            PolicyReference = "POL-DLQ-003",
            SourceSystem = "BILLINGENGINE",
            AsOfDate = new DateOnly(2026, 7, 1),
            Installments = BuildSchedule(
                (1, new DateOnly(2026, 2, 1), BillingInstallmentStatus.Issued),
                (2, new DateOnly(2026, 3, 1), BillingInstallmentStatus.Issued),
                (3, new DateOnly(2026, 4, 1), BillingInstallmentStatus.Issued))
        };

        var result = service.Assess(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.NewlyOverdueInstallmentNumbers, Is.EqualTo(FirstThree));
            Assert.That(result.NonPaymentCancellationRecommended, Is.True);
            Assert.That(result.Billing.BillingStatus, Is.EqualTo("SeverelyDelinquent"));
            Assert.That(result.Billing.FinalStatus, Is.EqualTo("PendingNonPaymentCancellation"));
        });
    }

    [Test]
    public void Assess_IgnoresPaidAndCancelledInstallments()
    {
        var service = CreateService();
        var request = new DelinquencyAssessmentRequest
        {
            PolicyReference = "POL-DLQ-004",
            SourceSystem = "BILLINGENGINE",
            AsOfDate = new DateOnly(2026, 7, 1),
            Installments = BuildSchedule(
                (1, new DateOnly(2026, 2, 1), BillingInstallmentStatus.Paid),
                (2, new DateOnly(2026, 3, 1), BillingInstallmentStatus.Cancelled),
                (3, new DateOnly(2026, 4, 1), BillingInstallmentStatus.Issued))
        };

        var result = service.Assess(request);

        Assert.That(result.NewlyOverdueInstallmentNumbers, Is.EqualTo(Third));
    }

    [Test]
    public void Assess_AlreadyOverdueInstallment_IsNotReflagged()
    {
        var service = CreateService();
        var request = new DelinquencyAssessmentRequest
        {
            PolicyReference = "POL-DLQ-005",
            SourceSystem = "BILLINGENGINE",
            AsOfDate = new DateOnly(2026, 7, 1),
            Installments = BuildSchedule(
                (1, new DateOnly(2026, 2, 1), BillingInstallmentStatus.Overdue),
                (2, new DateOnly(2026, 8, 1), BillingInstallmentStatus.Issued))
        };

        var result = service.Assess(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.NewlyOverdueInstallmentNumbers, Is.EqualTo(None));
            Assert.That(result.OverdueInstallmentNumbers, Is.EqualTo(First));
        });
    }

    [Test]
    public void Assess_DefaultsAsOfDateFromTimeProvider()
    {
        var service = CreateService();
        var request = new DelinquencyAssessmentRequest
        {
            PolicyReference = "POL-DLQ-006",
            SourceSystem = "BILLINGENGINE",
            Installments = BuildSchedule((1, new DateOnly(2026, 1, 1), BillingInstallmentStatus.Issued))
        };

        var result = service.Assess(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.AsOfDate, Is.EqualTo(new DateOnly(2026, 7, 1)));
            Assert.That(result.NewlyOverdueInstallmentNumbers, Is.EqualTo(First));
        });
    }

    [Test]
    public void Assess_EmptySchedule_Throws()
    {
        var service = CreateService();
        var request = new DelinquencyAssessmentRequest
        {
            PolicyReference = "POL-DLQ-007",
            SourceSystem = "BILLINGENGINE",
            Installments = []
        };

        Assert.Throws<ArgumentException>(() => service.Assess(request));
    }

    [Test]
    public void Assess_NegativeGracePeriod_Throws()
    {
        var service = CreateService();
        var request = new DelinquencyAssessmentRequest
        {
            PolicyReference = "POL-DLQ-008",
            SourceSystem = "BILLINGENGINE",
            GracePeriodDays = -1,
            Installments = BuildSchedule((1, new DateOnly(2026, 1, 1), BillingInstallmentStatus.Issued))
        };

        Assert.Throws<ArgumentException>(() => service.Assess(request));
    }
}
