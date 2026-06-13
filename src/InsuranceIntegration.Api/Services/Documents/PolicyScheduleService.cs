using InsuranceIntegration.Api.Snapshots.Policies;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace InsuranceIntegration.Api.Services.Documents;

public sealed class PolicyScheduleService : IPolicyScheduleService
{
    static PolicyScheduleService()
    {
        // Self-declared QuestPDF Community license (free for individuals, non-profits,
        // open-source, and organizations under $1M USD annual revenue). Set once before any
        // document is generated, regardless of how the service is constructed.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private readonly TimeProvider _timeProvider;

    public PolicyScheduleService(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public byte[] GenerateSchedule(PolicySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var document = new PolicyScheduleDocument(snapshot, _timeProvider.GetUtcNow().UtcDateTime);
        return document.GeneratePdf();
    }
}
