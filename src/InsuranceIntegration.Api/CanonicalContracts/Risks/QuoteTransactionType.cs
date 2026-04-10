namespace InsuranceIntegration.Api.CanonicalContracts.Risks;

public static class QuoteTransactionType
{
    public const string Quotable = "Quotable";

    public const string Quoted = "Quoted";

    public const string Bind = "Bind";

    public static IReadOnlyCollection<string> All { get; } = [Quotable, Quoted, Bind];

    public static bool IsQuoteTransaction(string transactionType)
    {
        return All.Any(value => string.Equals(value, transactionType, StringComparison.OrdinalIgnoreCase));
    }
}
