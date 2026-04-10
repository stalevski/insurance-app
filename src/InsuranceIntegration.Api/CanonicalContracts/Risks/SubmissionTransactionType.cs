namespace InsuranceIntegration.Api.CanonicalContracts.Risks;

public static class SubmissionTransactionType
{
    public const string Submission = "Submission";

    public const string Clearance = "Clearance";

    public static IReadOnlyCollection<string> All { get; } = [Submission, Clearance];

    public static bool IsSubmissionTransaction(string transactionType)
    {
        return All.Any(value => string.Equals(value, transactionType, StringComparison.OrdinalIgnoreCase));
    }
}
