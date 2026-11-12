namespace InsuranceIntegration.Api.Services.Billing;

public static class RefundTransactionType
{
    public const string Cancellation = "Cancellation";

    public const string Endorsement = "Endorsement";

    public static bool IsValid(string? value) =>
        string.Equals(value, Cancellation, StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, Endorsement, StringComparison.OrdinalIgnoreCase);

    /// <summary>Short, stable prefix used to build a deterministic refund reference.</summary>
    public static string PrefixFor(string value) =>
        string.Equals(value, Endorsement, StringComparison.OrdinalIgnoreCase) ? "END" : "CAN";
}

public static class RefundOutcome
{
    /// <summary>A positive net amount is owed back to the insured.</summary>
    public const string Payable = "Payable";

    /// <summary>Nothing is owed (zero return premium, or withheld fees/arrears absorb it).</summary>
    public const string NoRefund = "NoRefund";

    /// <summary>Every premium unit collected has already been refunded.</summary>
    public const string Exhausted = "Exhausted";
}

public static class RefundPayoutMethod
{
    public const string BankTransfer = "BankTransfer";

    public const string Card = "Card";

    public const string Credit = "Credit";

    public static bool IsValid(string? value) =>
        string.Equals(value, BankTransfer, StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, Card, StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, Credit, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Clearing lead time per channel: a bank transfer needs the longest settlement window, a card
    /// reversal is quicker, and a credit sits on the account immediately.
    /// </summary>
    public static int LeadDaysFor(string value) => value.ToUpperInvariant() switch
    {
        "CARD" => 3,
        "CREDIT" => 0,
        _ => 5
    };
}
