using InsuranceIntegration.Api.CanonicalContracts.Risks;
using InsuranceIntegration.Api.Responses.Risks;
using InsuranceIntegration.Api.Services.Ingest;
using InsuranceIntegration.Api.Snapshots.Quotes;

namespace InsuranceIntegration.Api.Services.Snapshots;

public interface IQuoteSnapshotProjector
{
    QuoteSnapshot Apply(
        QuoteSnapshot? current,
        CanonicalRiskRequest request,
        FinalRiskResponse response,
        IngestContext context);
}
