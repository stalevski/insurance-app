namespace InsuranceIntegration.Api.Services.Claims;

public sealed class ClaimFinancialService : IClaimFinancialService
{
    public ClaimFinancialResult Apply(ClaimFinancialRequest request)
    {
        if (!ClaimFinancialOperation.IsKnown(request.Operation))
        {
            throw new ArgumentException($"Unknown claim financial operation '{request.Operation}'.");
        }

        if (request.CurrentReserve < 0m)
        {
            throw new ArgumentException("Current reserve cannot be negative.");
        }

        if (request.PaidIndemnityToDate < 0m || request.PaidExpenseToDate < 0m)
        {
            throw new ArgumentException("Paid-to-date amounts cannot be negative.");
        }

        var operation = ClaimFinancialOperation.All
            .First(value => string.Equals(value, request.Operation, StringComparison.OrdinalIgnoreCase));

        var reserve = request.CurrentReserve;
        var paidIndemnity = request.PaidIndemnityToDate;
        var paidExpense = request.PaidExpenseToDate;
        var narrative = new List<string>();

        switch (operation)
        {
            case ClaimFinancialOperation.SetReserve:
                if (request.Amount < 0m)
                {
                    throw new ArgumentException("Reserve amount cannot be negative.");
                }

                reserve = request.Amount;
                narrative.Add($"Reserve set to {reserve:0.##} (was {request.CurrentReserve:0.##})");
                break;

            case ClaimFinancialOperation.AdjustReserve:
                reserve = request.CurrentReserve + request.Amount;
                if (reserve < 0m)
                {
                    throw new ArgumentException("Adjustment would drive the reserve below zero.");
                }

                narrative.Add($"Reserve adjusted by {request.Amount:0.##} to {reserve:0.##}");
                break;

            case ClaimFinancialOperation.RecordIndemnityPayment:
                if (request.Amount <= 0m)
                {
                    throw new ArgumentException("A payment amount must be greater than zero.");
                }

                paidIndemnity = request.PaidIndemnityToDate + request.Amount;
                reserve = Math.Max(0m, request.CurrentReserve - request.Amount);
                narrative.Add($"Indemnity payment of {request.Amount:0.##} recorded; reserve drawn down to {reserve:0.##}");
                break;

            case ClaimFinancialOperation.RecordExpensePayment:
                if (request.Amount <= 0m)
                {
                    throw new ArgumentException("A payment amount must be greater than zero.");
                }

                paidExpense = request.PaidExpenseToDate + request.Amount;
                narrative.Add($"Expense payment of {request.Amount:0.##} recorded");
                break;
        }

        var totalPaid = paidIndemnity + paidExpense;
        var incurred = totalPaid + reserve;

        narrative.Add($"Outstanding reserve: {reserve:0.##}");
        narrative.Add($"Total paid (indemnity {paidIndemnity:0.##} + expense {paidExpense:0.##}): {totalPaid:0.##}");
        narrative.Add($"Incurred (paid + outstanding): {incurred:0.##}");

        if (!string.IsNullOrWhiteSpace(request.Reason))
        {
            narrative.Add($"Note: {request.Reason}");
        }

        return new ClaimFinancialResult
        {
            ClaimReference = request.ClaimReference,
            PolicyReference = request.PolicyReference,
            Operation = operation,
            OutstandingReserve = reserve,
            PaidIndemnity = paidIndemnity,
            PaidExpense = paidExpense,
            TotalPaid = totalPaid,
            Incurred = incurred,
            Reasons = narrative
        };
    }
}
