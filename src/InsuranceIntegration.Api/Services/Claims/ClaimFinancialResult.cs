namespace InsuranceIntegration.Api.Services.Claims;

public sealed class ClaimFinancialResult
{
    public string ClaimReference { get; init; } = string.Empty;

    public string PolicyReference { get; init; } = string.Empty;

    public string Operation { get; init; } = string.Empty;

    /// <summary>Outstanding case reserve after the operation.</summary>
    public decimal OutstandingReserve { get; init; }

    /// <summary>Indemnity paid to date after the operation.</summary>
    public decimal PaidIndemnity { get; init; }

    /// <summary>Expense paid to date after the operation.</summary>
    public decimal PaidExpense { get; init; }

    /// <summary>Total paid after the operation (<c>indemnity + expense</c>).</summary>
    public decimal TotalPaid { get; init; }

    /// <summary>Incurred total after the operation (<c>total paid + outstanding reserve</c>).</summary>
    public decimal Incurred { get; init; }

    public List<string> Reasons { get; init; } = [];
}
