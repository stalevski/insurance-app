namespace InsuranceIntegration.Api.Services.Clearance;

public static class SubmissionClearanceOutcome
{
    public const string Cleared = "Cleared";

    public const string DuplicateSubmission = "DuplicateSubmission";

    public const string ConflictingBroker = "ConflictingBroker";

    public const string ManualClearanceReview = "ManualClearanceReview";
}
