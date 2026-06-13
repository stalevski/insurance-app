using InsuranceIntegration.Api.Snapshots.Policies;

namespace InsuranceIntegration.Api.Services.Documents;

public interface IPolicyScheduleService
{
    /// <summary>Renders the policy schedule for the given snapshot as a PDF document.</summary>
    byte[] GenerateSchedule(PolicySnapshot snapshot);
}
