namespace InsuranceIntegration.Api.CanonicalContracts.Risks;

public static class DeductibleType
{
    public const string Flat = "Flat";

    public const string PercentageOfLoss = "PercentageOfLoss";

    public const string PercentageOfSumInsured = "PercentageOfSumInsured";

    public static bool IsRecognized(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        return string.Equals(value, Flat, StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, PercentageOfLoss, StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, PercentageOfSumInsured, StringComparison.OrdinalIgnoreCase);
    }
}
