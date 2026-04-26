using InsuranceIntegration.Api.CanonicalContracts.Risks;
using InsuranceIntegration.Api.Responses.Risks;
using InsuranceIntegration.Api.Services.Ingest;

namespace InsuranceIntegration.Api.Services.Snapshots;

public sealed class RiskSnapshotRouter : IRiskSnapshotRouter
{
    private readonly IPolicySnapshotService _policySnapshotService;
    private readonly IQuoteSnapshotService _quoteSnapshotService;

    public RiskSnapshotRouter(IPolicySnapshotService policySnapshotService, IQuoteSnapshotService quoteSnapshotService)
    {
        _policySnapshotService = policySnapshotService;
        _quoteSnapshotService = quoteSnapshotService;
    }

    public void Route(CanonicalRiskRequest request, FinalRiskResponse response, IngestContext context)
    {
        var hasQuoteReference = !string.IsNullOrWhiteSpace(request.Quote.QuoteReference)
            || !string.IsNullOrWhiteSpace(request.ExternalReference);

        if (hasQuoteReference)
        {
            _quoteSnapshotService.Apply(request, response, context);
        }

        if (!string.IsNullOrWhiteSpace(request.Policy.PolicyReference))
        {
            _policySnapshotService.Apply(request, response, context);
        }
    }
}
