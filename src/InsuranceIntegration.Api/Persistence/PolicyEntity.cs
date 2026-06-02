namespace InsuranceIntegration.Api.Persistence;

public sealed class PolicyEntity
{
    public Guid Id { get; set; }

    public Guid SubmissionId { get; set; }

    public Guid? QuoteId { get; set; }

    public string PolicyReference { get; set; } = string.Empty;

    public string? QuoteReference { get; set; }

    public string ProductCode { get; set; } = string.Empty;

    public string InsuredName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string TransactionType { get; set; } = string.Empty;

    public int UnderwritingYear { get; set; }

    public DateOnly? InceptionDate { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public decimal AnnualPremium { get; set; }

    public int PolicyVersion { get; set; } = 1;

    public Guid? PriorPolicyId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
