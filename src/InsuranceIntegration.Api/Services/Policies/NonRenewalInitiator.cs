namespace InsuranceIntegration.Api.Services.Policies;

public static class NonRenewalInitiator
{
    public const string Insurer = "Insurer";

    public const string Insured = "Insured";

    public static bool IsValid(string initiator)
    {
        return string.Equals(initiator, Insurer, StringComparison.OrdinalIgnoreCase)
            || string.Equals(initiator, Insured, StringComparison.OrdinalIgnoreCase);
    }
}
