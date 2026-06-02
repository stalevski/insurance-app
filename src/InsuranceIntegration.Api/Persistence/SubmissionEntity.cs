namespace InsuranceIntegration.Api.Persistence;

public sealed class SubmissionEntity
{
    public Guid Id { get; set; }

    public string ExternalReference { get; set; } = string.Empty;

    public string ProductCode { get; set; } = string.Empty;

    public string SourceSystem { get; set; } = string.Empty;

    public string TransactionType { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string ClearanceDecision { get; set; } = string.Empty;

    public bool AutoCleared { get; set; }

    public int UnderwritingYear { get; set; }

    public string BrokerCode { get; set; } = string.Empty;

    public string InsuredName { get; set; } = string.Empty;

    public decimal AdjustedPremium { get; set; }

    public string? CorrelationId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
