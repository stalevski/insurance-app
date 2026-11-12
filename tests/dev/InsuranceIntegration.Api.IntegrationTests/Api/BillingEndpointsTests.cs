using System.Net;
using InsuranceIntegration.Api.CanonicalContracts.Billing;
using InsuranceIntegration.Api.IntegrationTests.Builders;
using InsuranceIntegration.Api.IntegrationTests.Infrastructure;
using InsuranceIntegration.Api.Services.Billing;

namespace InsuranceIntegration.Api.IntegrationTests.Api;

/// <summary>
/// Coverage for the billing endpoints. Payment application and delinquency assessment both run as
/// pure calculations over a supplied installment schedule, so the tests assert the financial outcome
/// and the documented validation contract (<c>ArgumentException</c> → 400).
/// </summary>
public sealed class BillingEndpointsTests : ApiTestBase
{
    private const string SourceSystemCode = "PAYMENTRAIL";

    [Test]
    public async Task RecordPayment_SettlesEveryOpenInstallment()
    {
        var request = new PaymentRecordRequest
        {
            PolicyReference = "POL-BILL-001",
            SourceSystem = SourceSystemCode,
            Installments = BillingScheduleBuilder.WithStatuses(
                250m,
                BillingInstallmentStatus.Issued,
                BillingInstallmentStatus.Issued,
                BillingInstallmentStatus.Issued,
                BillingInstallmentStatus.Issued),
            Amount = 1_000m,
        };

        using var response = await PostAsync("/api/v1/billing/payments", request);

        var body = await response.ShouldReturnJsonAsync();
        Assert.Multiple(() =>
        {
            Assert.That(body.GetProperty("amountApplied").GetDecimal(), Is.EqualTo(1_000m));
            Assert.That(body.GetProperty("unappliedCredit").GetDecimal(), Is.EqualTo(0m));
            Assert.That(body.GetProperty("settledInstallmentNumbers").GetArrayLength(), Is.EqualTo(4));
        });
    }

    [Test]
    public async Task RecordPayment_LeavesUnappliedCredit_WhenAmountExceedsSchedule()
    {
        var request = new PaymentRecordRequest
        {
            PolicyReference = "POL-BILL-002",
            SourceSystem = SourceSystemCode,
            Installments = BillingScheduleBuilder.WithStatuses(250m, BillingInstallmentStatus.Issued),
            Amount = 400m,
        };

        using var response = await PostAsync("/api/v1/billing/payments", request);

        var body = await response.ShouldReturnJsonAsync();
        Assert.Multiple(() =>
        {
            Assert.That(body.GetProperty("amountApplied").GetDecimal(), Is.EqualTo(250m));
            Assert.That(body.GetProperty("unappliedCredit").GetDecimal(), Is.EqualTo(150m));
        });
    }

    [Test]
    public async Task RecordPayment_Returns400_ForAZeroAmount()
    {
        var request = new PaymentRecordRequest
        {
            PolicyReference = "POL-BILL-003",
            SourceSystem = SourceSystemCode,
            Installments = BillingScheduleBuilder.WithStatuses(250m, BillingInstallmentStatus.Issued),
            Amount = 0m,
        };

        using var response = await PostAsync("/api/v1/billing/payments", request);

        response.ShouldHaveStatus(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task RecordPayment_Returns400_ForAnEmptySchedule()
    {
        var request = new PaymentRecordRequest
        {
            PolicyReference = "POL-BILL-004",
            SourceSystem = SourceSystemCode,
            Installments = [],
            Amount = 250m,
        };

        using var response = await PostAsync("/api/v1/billing/payments", request);

        response.ShouldHaveStatus(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task AssessDelinquency_FlagsPastDueInstallments()
    {
        var request = new DelinquencyAssessmentRequest
        {
            PolicyReference = "POL-BILL-005",
            SourceSystem = SourceSystemCode,
            Installments = BillingScheduleBuilder.WithStatuses(
                250m,
                BillingInstallmentStatus.Issued,
                BillingInstallmentStatus.Issued,
                BillingInstallmentStatus.Issued),
            AsOfDate = new DateOnly(2027, 6, 1),
            GracePeriodDays = 0,
        };

        using var response = await PostAsync("/api/v1/billing/delinquency", request);

        var body = await response.ShouldReturnJsonAsync();
        Assert.That(body.GetProperty("newlyOverdueInstallmentNumbers").GetArrayLength(), Is.EqualTo(3));
    }

    [Test]
    public async Task AssessDelinquency_Returns400_ForANegativeGracePeriod()
    {
        var request = new DelinquencyAssessmentRequest
        {
            PolicyReference = "POL-BILL-006",
            SourceSystem = SourceSystemCode,
            Installments = BillingScheduleBuilder.WithStatuses(250m, BillingInstallmentStatus.Issued),
            GracePeriodDays = -5,
        };

        using var response = await PostAsync("/api/v1/billing/delinquency", request);

        response.ShouldHaveStatus(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ProcessRefund_PaysTheReturnPremium_ForACancellation()
    {
        var request = new RefundRequest
        {
            PolicyReference = "POL-BILL-007",
            TransactionType = RefundTransactionType.Cancellation,
            TransactionReference = "CAN-7001",
            ReturnPremium = 420.5m,
            CollectedPremium = 1_200m,
            EffectiveDate = new DateOnly(2026, 3, 1)
        };

        using var response = await PostAsync("/api/v1/billing/refunds", request);

        var body = await response.ShouldReturnJsonAsync();
        Assert.Multiple(() =>
        {
            Assert.That(body.GetProperty("outcome").GetString(), Is.EqualTo(RefundOutcome.Payable));
            Assert.That(body.GetProperty("netRefundAmount").GetDecimal(), Is.EqualTo(420.5m));
            Assert.That(body.GetProperty("refundReference").GetString(), Is.EqualTo("RFD-CAN-CAN-7001"));
            Assert.That(body.GetProperty("payableDate").GetString(), Is.EqualTo("2026-03-06"));
        });
    }

    [Test]
    public async Task ProcessRefund_WithholdsTheAdminFee_AndOffsetsArrears()
    {
        var request = new RefundRequest
        {
            PolicyReference = "POL-BILL-008",
            TransactionType = RefundTransactionType.Endorsement,
            TransactionReference = "END-8001",
            ReturnPremium = 500m,
            CollectedPremium = 500m,
            AdminFeeWithheld = 50m,
            OutstandingPremium = 100m,
            PayoutMethod = RefundPayoutMethod.Card,
            EffectiveDate = new DateOnly(2026, 4, 1)
        };

        using var response = await PostAsync("/api/v1/billing/refunds", request);

        var body = await response.ShouldReturnJsonAsync();
        Assert.Multiple(() =>
        {
            Assert.That(body.GetProperty("adminFeeWithheld").GetDecimal(), Is.EqualTo(50m));
            Assert.That(body.GetProperty("offsetAgainstArrears").GetDecimal(), Is.EqualTo(100m));
            Assert.That(body.GetProperty("netRefundAmount").GetDecimal(), Is.EqualTo(350m));
            Assert.That(body.GetProperty("payoutMethod").GetString(), Is.EqualTo(RefundPayoutMethod.Card));
            Assert.That(body.GetProperty("payableDate").GetString(), Is.EqualTo("2026-04-04"));
        });
    }

    [Test]
    public async Task ProcessRefund_CapsAtPremiumCollected()
    {
        var request = new RefundRequest
        {
            PolicyReference = "POL-BILL-009",
            TransactionType = RefundTransactionType.Cancellation,
            TransactionReference = "CAN-9001",
            ReturnPremium = 900m,
            CollectedPremium = 300m
        };

        using var response = await PostAsync("/api/v1/billing/refunds", request);

        var body = await response.ShouldReturnJsonAsync();
        Assert.Multiple(() =>
        {
            Assert.That(body.GetProperty("refundableBase").GetDecimal(), Is.EqualTo(300m));
            Assert.That(body.GetProperty("uncollectedPortion").GetDecimal(), Is.EqualTo(600m));
            Assert.That(body.GetProperty("cappedByCollectedPremium").GetBoolean(), Is.True);
        });
    }

    [Test]
    public async Task ProcessRefund_Returns400_ForAnUnsupportedTransactionType()
    {
        var request = new RefundRequest
        {
            PolicyReference = "POL-BILL-010",
            TransactionType = "Refund",
            TransactionReference = "REF-1",
            ReturnPremium = 100m,
            CollectedPremium = 100m
        };

        using var response = await PostAsync("/api/v1/billing/refunds", request);

        response.ShouldHaveStatus(HttpStatusCode.BadRequest);
    }
}
