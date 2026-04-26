using InsuranceIntegration.Api.CanonicalContracts.Risks;
using InsuranceIntegration.Api.Responses.Risks;
using InsuranceIntegration.Api.Services.Ingest;
using InsuranceIntegration.Api.Snapshots.Quotes;

namespace InsuranceIntegration.Api.Services.Snapshots;

public interface IQuoteSnapshotService
{
    QuoteSnapshot? Find(string quoteReference);

    IReadOnlyList<QuoteSnapshotSummary> List(int skip = 0, int take = 100);

    QuoteSnapshot Apply(CanonicalRiskRequest request, FinalRiskResponse response, IngestContext context);
}

public sealed class QuoteSnapshotSummary
{
    public string QuoteReference { get; init; } = string.Empty;

    public string? PolicyReference { get; init; }

    public string ProductCode { get; init; } = string.Empty;

    public int UnderwritingYear { get; init; }

    public string CurrentPhase { get; init; } = string.Empty;

    public bool IsBound { get; init; }

    public DateTime LastUpdatedUtc { get; init; }

    public string Self { get; init; } = string.Empty;
}
