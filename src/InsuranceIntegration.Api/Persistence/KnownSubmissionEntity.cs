namespace InsuranceIntegration.Api.Persistence;

public sealed class KnownSubmissionEntity
{
    public int Id { get; set; }

    public string ExternalReference { get; set; } = string.Empty;

    public string InsuredName { get; set; } = string.Empty;

    public string ProductCode { get; set; } = string.Empty;

    public int UnderwritingYear { get; set; }

    public string? BrokerCode { get; set; }
}
