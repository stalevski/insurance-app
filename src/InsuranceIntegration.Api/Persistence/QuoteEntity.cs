namespace InsuranceIntegration.Api.Persistence;

public sealed class QuoteEntity
{
    public Guid Id { get; set; }

    public Guid SubmissionId { get; set; }

    public string QuoteReference { get; set; } = string.Empty;

    public string ProductCode { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string TransactionType { get; set; } = string.Empty;

    public int UnderwritingYear { get; set; }

    public DateOnly? EffectiveDate { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public decimal AdjustedPremium { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
