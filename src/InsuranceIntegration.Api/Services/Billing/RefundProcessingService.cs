namespace InsuranceIntegration.Api.Services.Billing;

/// <summary>
/// Decides the refund payable to the insured after a cancellation or endorsement.
/// <para>
/// Order of deduction is deliberate and mirrors how an insurer settles a return premium: the
/// refund is first capped to premium actually collected (you cannot give back money that was
/// never received), then the administrative fee is withheld, then any outstanding premium is
/// recovered. What remains is payable, and the channel's clearing time sets the payable date.
/// </para>
/// </summary>
public sealed class RefundProcessingService : IRefundProcessingService
{
    private readonly TimeProvider _timeProvider;

    public RefundProcessingService(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public RefundResult Process(RefundRequest request)
    {
        Validate(request);

        var effectiveDate = request.EffectiveDate ?? DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        var collectedRemaining = Math.Max(0m, request.CollectedPremium - request.PreviouslyRefunded);

        var refundableBase = Math.Min(request.ReturnPremium, collectedRemaining);
        var uncollected = request.ReturnPremium - refundableBase;
        var capped = uncollected > 0m;

        var adminFee = Math.Min(request.AdminFeeWithheld, refundableBase);
        var afterFee = refundableBase - adminFee;

        var arrearsOffset = Math.Min(request.OutstandingPremium, afterFee);
        var net = Math.Round(afterFee - arrearsOffset, 2, MidpointRounding.AwayFromZero);

        var outcome = DetermineOutcome(request.ReturnPremium, collectedRemaining, net);
        var payoutMethod = NormalisePayoutMethod(request.PayoutMethod);
        var payableDate = effectiveDate.AddDays(RefundPayoutMethod.LeadDaysFor(payoutMethod));

        return new RefundResult
        {
            PolicyReference = request.PolicyReference,
            TransactionType = request.TransactionType,
            TransactionReference = request.TransactionReference,
            RefundReference = $"RFD-{RefundTransactionType.PrefixFor(request.TransactionType)}-{request.TransactionReference}",
            ReturnPremium = request.ReturnPremium,
            UncollectedPortion = uncollected,
            RefundableBase = refundableBase,
            AdminFeeWithheld = adminFee,
            OffsetAgainstArrears = arrearsOffset,
            NetRefundAmount = net,
            Outcome = outcome,
            CurrencyCode = request.CurrencyCode,
            PayoutMethod = payoutMethod,
            PayableDate = payableDate,
            CappedByCollectedPremium = capped,
            Reasons = BuildReasons(request, collectedRemaining, refundableBase, uncollected, adminFee, arrearsOffset, net, outcome, payoutMethod)
        };
    }

    private static string DetermineOutcome(decimal returnPremium, decimal collectedRemaining, decimal net)
    {
        if (returnPremium <= 0m)
        {
            return RefundOutcome.NoRefund;
        }

        if (collectedRemaining <= 0m)
        {
            return RefundOutcome.Exhausted;
        }

        return net > 0m ? RefundOutcome.Payable : RefundOutcome.NoRefund;
    }

    private static string NormalisePayoutMethod(string value) => value.ToUpperInvariant() switch
    {
        "CARD" => RefundPayoutMethod.Card,
        "CREDIT" => RefundPayoutMethod.Credit,
        _ => RefundPayoutMethod.BankTransfer
    };

    private static List<string> BuildReasons(
        RefundRequest request,
        decimal collectedRemaining,
        decimal refundableBase,
        decimal uncollected,
        decimal adminFee,
        decimal arrearsOffset,
        decimal net,
        string outcome,
        string payoutMethod)
    {
        var reasons = new List<string>
        {
            $"Transaction: {request.TransactionType} {request.TransactionReference}",
            $"Return premium requested: {request.ReturnPremium:0.##}",
            $"Premium in hand: {collectedRemaining:0.##} (collected {request.CollectedPremium:0.##}, previously refunded {request.PreviouslyRefunded:0.##})",
            $"Refundable base: {refundableBase:0.##}"
        };

        if (uncollected > 0m)
        {
            reasons.Add($"Not refundable, premium never collected: {uncollected:0.##}");
        }

        reasons.Add(request.AdminFeeWithheld > 0m
            ? $"Admin fee withheld: {adminFee:0.##}"
            : "No admin fee withheld");

        reasons.Add(request.OutstandingPremium > 0m
            ? $"Offset against outstanding premium: {arrearsOffset:0.##}"
            : "No outstanding premium to offset");

        reasons.Add($"Net refund payable: {net:0.##}");
        reasons.Add($"Outcome: {outcome}");
        reasons.Add($"Payout via {payoutMethod}");

        return reasons;
    }

    private static void Validate(RefundRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PolicyReference))
        {
            throw new ArgumentException("A refund requires a policy reference.");
        }

        if (string.IsNullOrWhiteSpace(request.TransactionReference))
        {
            throw new ArgumentException("A refund requires the originating transaction reference.");
        }

        if (!RefundTransactionType.IsValid(request.TransactionType))
        {
            throw new ArgumentException(
                $"Transaction type '{request.TransactionType}' is not supported. Expected '{RefundTransactionType.Cancellation}' or '{RefundTransactionType.Endorsement}'.");
        }

        if (!RefundPayoutMethod.IsValid(request.PayoutMethod))
        {
            throw new ArgumentException(
                $"Payout method '{request.PayoutMethod}' is not supported. Expected '{RefundPayoutMethod.BankTransfer}', '{RefundPayoutMethod.Card}' or '{RefundPayoutMethod.Credit}'.");
        }

        if (request.ReturnPremium < 0m)
        {
            throw new ArgumentException("Return premium cannot be negative.");
        }

        if (request.CollectedPremium < 0m)
        {
            throw new ArgumentException("Collected premium cannot be negative.");
        }

        if (request.PreviouslyRefunded < 0m)
        {
            throw new ArgumentException("Previously refunded amount cannot be negative.");
        }

        if (request.AdminFeeWithheld < 0m)
        {
            throw new ArgumentException("Admin fee cannot be negative.");
        }

        if (request.OutstandingPremium < 0m)
        {
            throw new ArgumentException("Outstanding premium cannot be negative.");
        }
    }
}
