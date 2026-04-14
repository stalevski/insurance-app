using System.Collections.Concurrent;

namespace InsuranceIntegration.Api.Services.Clearance;

public sealed class InMemorySubmissionRegistry : ISubmissionRegistry
{
    private readonly ConcurrentBag<KnownSubmissionRecord> _records = new();

    public IReadOnlyCollection<KnownSubmissionRecord> GetKnownSubmissions()
    {
        return _records.ToArray();
    }

    public void Register(KnownSubmissionRecord record)
    {
        _records.Add(record);
    }
}
