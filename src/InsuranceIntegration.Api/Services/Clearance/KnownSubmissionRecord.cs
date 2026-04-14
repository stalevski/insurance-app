namespace InsuranceIntegration.Api.Services.Clearance;

public sealed class KnownSubmissionRecord
{
    public string ExternalReference { get; init; } = string.Empty;

    public string InsuredName { get; init; } = string.Empty;

    public string ProductCode { get; init; } = string.Empty;

    public int UnderwritingYear { get; init; }

    public string? BrokerCode { get; init; }
}
