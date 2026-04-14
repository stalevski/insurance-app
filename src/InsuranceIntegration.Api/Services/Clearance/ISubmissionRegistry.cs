namespace InsuranceIntegration.Api.Services.Clearance;

public interface ISubmissionRegistry
{
    IReadOnlyCollection<KnownSubmissionRecord> GetKnownSubmissions();

    void Register(KnownSubmissionRecord record);
}
