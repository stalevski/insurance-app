using InsuranceIntegration.Api.Services.Claims;

namespace InsuranceIntegration.Api.Tests.Claims;

public sealed class ClaimFinancialServiceTests
{
    private static ClaimFinancialRequest Request(string operation, decimal amount, decimal reserve = 0m, decimal indemnity = 0m, decimal expense = 0m) => new()
    {
        ClaimReference = "CLM-1",
        PolicyReference = "POL-1",
        Operation = operation,
        Amount = amount,
        CurrentReserve = reserve,
        PaidIndemnityToDate = indemnity,
        PaidExpenseToDate = expense
    };

    [Test]
    public void Apply_SetReserve_ReplacesReserveAndComputesIncurred()
    {
        var service = new ClaimFinancialService();

        var result = service.Apply(Request(ClaimFinancialOperation.SetReserve, 20000m, reserve: 5000m, indemnity: 1000m, expense: 500m));

        Assert.Multiple(() =>
        {
            Assert.That(result.OutstandingReserve, Is.EqualTo(20000m));
            Assert.That(result.TotalPaid, Is.EqualTo(1500m));
            Assert.That(result.Incurred, Is.EqualTo(21500m));
        });
    }

    [Test]
    public void Apply_AdjustReserve_AppliesSignedDelta()
    {
        var service = new ClaimFinancialService();

        var result = service.Apply(Request(ClaimFinancialOperation.AdjustReserve, -3000m, reserve: 8000m));

        Assert.Multiple(() =>
        {
            Assert.That(result.OutstandingReserve, Is.EqualTo(5000m));
            Assert.That(result.Incurred, Is.EqualTo(5000m));
        });
    }

    [Test]
    public void Apply_RecordIndemnityPayment_DrawsDownReserveAndAddsToPaid()
    {
        var service = new ClaimFinancialService();

        var result = service.Apply(Request(ClaimFinancialOperation.RecordIndemnityPayment, 4000m, reserve: 10000m, indemnity: 2000m));

        Assert.Multiple(() =>
        {
            Assert.That(result.PaidIndemnity, Is.EqualTo(6000m));
            Assert.That(result.OutstandingReserve, Is.EqualTo(6000m));
            // incurred = paid (6000) + reserve (6000) = 12000, unchanged from pre-payment incurred.
            Assert.That(result.Incurred, Is.EqualTo(12000m));
        });
    }

    [Test]
    public void Apply_RecordIndemnityPayment_NeverDrivesReserveNegative()
    {
        var service = new ClaimFinancialService();

        var result = service.Apply(Request(ClaimFinancialOperation.RecordIndemnityPayment, 9000m, reserve: 5000m, indemnity: 1000m));

        Assert.Multiple(() =>
        {
            Assert.That(result.PaidIndemnity, Is.EqualTo(10000m));
            Assert.That(result.OutstandingReserve, Is.EqualTo(0m));
            Assert.That(result.Incurred, Is.EqualTo(10000m));
        });
    }

    [Test]
    public void Apply_RecordExpensePayment_DoesNotDrawDownReserve()
    {
        var service = new ClaimFinancialService();

        var result = service.Apply(Request(ClaimFinancialOperation.RecordExpensePayment, 750m, reserve: 4000m, indemnity: 1000m, expense: 250m));

        Assert.Multiple(() =>
        {
            Assert.That(result.PaidExpense, Is.EqualTo(1000m));
            Assert.That(result.OutstandingReserve, Is.EqualTo(4000m));
            Assert.That(result.TotalPaid, Is.EqualTo(2000m));
            Assert.That(result.Incurred, Is.EqualTo(6000m));
        });
    }

    [Test]
    public void Apply_UnknownOperation_Throws()
    {
        var service = new ClaimFinancialService();

        Assert.Throws<ArgumentException>(() => service.Apply(Request("Reopen", 100m)));
    }

    [Test]
    public void Apply_SetNegativeReserve_Throws()
    {
        var service = new ClaimFinancialService();

        Assert.Throws<ArgumentException>(() => service.Apply(Request(ClaimFinancialOperation.SetReserve, -1m)));
    }

    [Test]
    public void Apply_AdjustReserveBelowZero_Throws()
    {
        var service = new ClaimFinancialService();

        Assert.Throws<ArgumentException>(() => service.Apply(Request(ClaimFinancialOperation.AdjustReserve, -6000m, reserve: 5000m)));
    }

    [Test]
    public void Apply_ZeroIndemnityPayment_Throws()
    {
        var service = new ClaimFinancialService();

        Assert.Throws<ArgumentException>(() => service.Apply(Request(ClaimFinancialOperation.RecordIndemnityPayment, 0m, reserve: 5000m)));
    }

    [Test]
    public void Apply_NegativePaidToDate_Throws()
    {
        var service = new ClaimFinancialService();

        Assert.Throws<ArgumentException>(() => service.Apply(Request(ClaimFinancialOperation.SetReserve, 100m, indemnity: -1m)));
    }
}
