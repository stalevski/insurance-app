using InsuranceIntegration.Api.CanonicalContracts.Risks;

namespace InsuranceIntegration.Api.Services.Flows;

/// <summary>
/// Inspects an existing quote (if any) and decides whether a bind transaction
/// is allowed against it. Implementations typically read the QuoteSnapshot.
/// Pure functions / non-bind transactions short-circuit to a "pass" result.
/// </summary>
public interface IBindPreconditionService
{
    BindPreconditionResult Evaluate(CanonicalRiskRequest request);
}

public sealed class BindPreconditionResult
{
    public required bool IsValid { get; init; }

    public string? RejectionReason { get; init; }

    public int? QuoteVersion { get; init; }

    public DateTime? QuoteValidUntilUtc { get; init; }

    public string? QuoteStatusAtCheck { get; init; }

    public static BindPreconditionResult Pass(int? version = null, DateTime? validUntilUtc = null, string? quoteStatus = null)
    {
        return new BindPreconditionResult
        {
            IsValid = true,
            QuoteVersion = version,
            QuoteValidUntilUtc = validUntilUtc,
            QuoteStatusAtCheck = quoteStatus
        };
    }

    public static BindPreconditionResult Reject(string reason, int? version = null, DateTime? validUntilUtc = null, string? quoteStatus = null)
    {
        return new BindPreconditionResult
        {
            IsValid = false,
            RejectionReason = reason,
            QuoteVersion = version,
            QuoteValidUntilUtc = validUntilUtc,
            QuoteStatusAtCheck = quoteStatus
        };
    }
}
