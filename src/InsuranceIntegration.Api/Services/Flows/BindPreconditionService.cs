using InsuranceIntegration.Api.CanonicalContracts.Risks;
using InsuranceIntegration.Api.Services.Snapshots;

namespace InsuranceIntegration.Api.Services.Flows;

public sealed class BindPreconditionService : IBindPreconditionService
{
    private static readonly HashSet<string> BindableQuoteStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        QuoteStatusValue.Quoted,
        QuoteStatusValue.Indicative
    };

    private readonly IQuoteSnapshotService _quoteSnapshotService;
    private readonly TimeProvider _time;

    public BindPreconditionService(IQuoteSnapshotService quoteSnapshotService, TimeProvider time)
    {
        _quoteSnapshotService = quoteSnapshotService;
        _time = time;
    }

    public BindPreconditionResult Evaluate(CanonicalRiskRequest request)
    {
        if (!IsBindTransaction(request.TransactionType))
        {
            return BindPreconditionResult.Pass();
        }

        var quoteRef = !string.IsNullOrWhiteSpace(request.Quote.QuoteReference)
            ? request.Quote.QuoteReference
            : null;

        if (string.IsNullOrWhiteSpace(quoteRef))
        {
            return BindPreconditionResult.Reject("Bind request is missing a quote reference");
        }

        var quote = _quoteSnapshotService.Find(quoteRef);
        if (quote is null)
        {
            return BindPreconditionResult.Reject($"No quote found for reference '{quoteRef}'");
        }

        var version = quote.Lifecycle.Version > 0 ? quote.Lifecycle.Version : (int?)null;
        var validUntilUtc = quote.Lifecycle.ValidUntilUtc;
        var quoteStatus = quote.Lifecycle.QuoteStatus;

        if (quote.Lifecycle.IsBound && !string.Equals(quote.PolicyReference, request.Policy.PolicyReference, StringComparison.OrdinalIgnoreCase))
        {
            return BindPreconditionResult.Reject(
                $"Quote '{quoteRef}' is already bound to policy '{quote.PolicyReference}'",
                version,
                validUntilUtc,
                quoteStatus);
        }

        var nowUtc = _time.GetUtcNow().UtcDateTime;
        if (validUntilUtc is { } expiresAt && nowUtc > expiresAt)
        {
            return BindPreconditionResult.Reject(
                $"Quote '{quoteRef}' expired at {expiresAt:O}; re-quote required",
                version,
                validUntilUtc,
                quoteStatus);
        }

        if (!string.IsNullOrWhiteSpace(quoteStatus) && !BindableQuoteStatuses.Contains(quoteStatus))
        {
            return BindPreconditionResult.Reject(
                $"Quote '{quoteRef}' status '{quoteStatus}' is not bindable; expected Quoted or Indicative",
                version,
                validUntilUtc,
                quoteStatus);
        }

        return BindPreconditionResult.Pass(version, validUntilUtc, quoteStatus);
    }

    private static bool IsBindTransaction(string transactionType)
    {
        return string.Equals(transactionType, "PolicyBind", StringComparison.OrdinalIgnoreCase)
            || string.Equals(transactionType, QuoteTransactionType.Bind, StringComparison.OrdinalIgnoreCase);
    }
}
