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
    [Test]
    public async Task RecordPayment_SettlesEveryOpenInstallment()
    {
        var request = new PaymentRecordRequest
        {
            PolicyReference = "POL-BILL-001",
            SourceSystem = "PAYMENTRAIL",
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
            SourceSystem = "PAYMENTRAIL",
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
            SourceSystem = "PAYMENTRAIL",
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
            SourceSystem = "PAYMENTRAIL",
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
            SourceSystem = "PAYMENTRAIL",
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
            SourceSystem = "PAYMENTRAIL",
            Installments = BillingScheduleBuilder.WithStatuses(250m, BillingInstallmentStatus.Issued),
            GracePeriodDays = -5,
        };

        using var response = await PostAsync("/api/v1/billing/delinquency", request);

        response.ShouldHaveStatus(HttpStatusCode.BadRequest);
    }
}
