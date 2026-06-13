namespace InsuranceIntegration.Api.Events;

public static class DomainEventType
{
    public const string RiskSubmissionReceived = "RiskSubmissionReceived";

    public const string QuoteIssued = "QuoteIssued";

    public const string QuoteBound = "QuoteBound";

    public const string PolicyBound = "PolicyBound";

    public const string PolicyEndorsed = "PolicyEndorsed";

    public const string PolicyCancelled = "PolicyCancelled";

    public const string PolicyRenewed = "PolicyRenewed";

    public const string PolicyReinstated = "PolicyReinstated";

    public const string PolicyLapsed = "PolicyLapsed";

    public const string PolicyNonRenewed = "PolicyNonRenewed";

    public const string ClaimNotified = "ClaimNotified";
}
