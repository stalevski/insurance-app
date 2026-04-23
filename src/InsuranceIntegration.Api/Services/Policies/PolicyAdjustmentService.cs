namespace InsuranceIntegration.Api.Services.Policies;

public sealed class PolicyAdjustmentService : IPolicyAdjustmentService
{
    public CancellationResult CalculateCancellation(CancellationRequest request)
    {
        ValidatePolicyPeriod(request.InceptionDate, request.ExpiryDate);

        var totalDays = Math.Max(1, (request.ExpiryDate.ToDateTime(TimeOnly.MinValue) - request.InceptionDate.ToDateTime(TimeOnly.MinValue)).Days);
        var cancellationDate = ClampToPolicyPeriod(request.CancellationDate, request.InceptionDate, request.ExpiryDate);
        var elapsedDays = Math.Max(0, (cancellationDate.ToDateTime(TimeOnly.MinValue) - request.InceptionDate.ToDateTime(TimeOnly.MinValue)).Days);

        var earnedFraction = (decimal)elapsedDays / totalDays;
        var earnedPremium = Math.Round(request.AnnualPremium * earnedFraction, 2, MidpointRounding.AwayFromZero);
        var unearnedPremium = Math.Round(request.AnnualPremium - earnedPremium, 2, MidpointRounding.AwayFromZero);

        decimal shortRatePenalty = 0m;
        decimal returnPremium;

        if (string.Equals(request.Basis, CancellationBasis.ShortRate, StringComparison.OrdinalIgnoreCase))
        {
            shortRatePenalty = Math.Round(unearnedPremium * request.ShortRatePenaltyPercent, 2, MidpointRounding.AwayFromZero);
            returnPremium = Math.Max(0m, unearnedPremium - shortRatePenalty);
        }
        else
        {
            returnPremium = unearnedPremium;
        }

        var retainedPremium = request.AnnualPremium - returnPremium;
        if (retainedPremium < request.MinimumRetainedPremium)
        {
            var shortfall = request.MinimumRetainedPremium - retainedPremium;
            returnPremium = Math.Max(0m, returnPremium - shortfall);
            retainedPremium = request.AnnualPremium - returnPremium;
        }

        return new CancellationResult
        {
            PolicyReference = request.PolicyReference,
            EarnedPremium = earnedPremium,
            UnearnedPremium = unearnedPremium,
            ReturnPremium = returnPremium,
            ShortRatePenalty = shortRatePenalty,
            RetainedPremium = retainedPremium,
            Basis = request.Basis,
            Reasons =
            [
                $"Earned fraction: {earnedFraction:0.####} over {totalDays} days",
                $"Earned premium: {earnedPremium:0.##}",
                $"Unearned premium: {unearnedPremium:0.##}",
                $"Basis applied: {request.Basis}",
                shortRatePenalty > 0m ? $"Short-rate penalty: {shortRatePenalty:0.##}" : "No short-rate penalty applied",
                $"Return premium: {returnPremium:0.##}",
                $"Retained premium: {retainedPremium:0.##}"
            ]
        };
    }

    public EndorsementResult CalculateEndorsement(EndorsementRequest request)
    {
        ValidatePolicyPeriod(request.InceptionDate, request.ExpiryDate);

        var totalDays = Math.Max(1, (request.ExpiryDate.ToDateTime(TimeOnly.MinValue) - request.InceptionDate.ToDateTime(TimeOnly.MinValue)).Days);
        var effectiveDate = ClampToPolicyPeriod(request.EffectiveDate, request.InceptionDate, request.ExpiryDate);
        var remainingDays = Math.Max(0, (request.ExpiryDate.ToDateTime(TimeOnly.MinValue) - effectiveDate.ToDateTime(TimeOnly.MinValue)).Days);

        var premiumDelta = request.NewAnnualPremium - request.CurrentAnnualPremium;
        var proRataAdjustment = Math.Round(premiumDelta * remainingDays / totalDays, 2, MidpointRounding.AwayFromZero);
        var direction = proRataAdjustment switch
        {
            > 0m => "AdditionalPremium",
            < 0m => "ReturnPremium",
            _ => "NoAdjustment"
        };

        return new EndorsementResult
        {
            PolicyReference = request.PolicyReference,
            PremiumDelta = premiumDelta,
            ProRataAdjustment = proRataAdjustment,
            AdjustmentDirection = direction,
            Reasons =
            [
                $"Premium delta: {premiumDelta:0.##}",
                $"Remaining days: {remainingDays} of {totalDays}",
                $"Pro-rata adjustment: {proRataAdjustment:0.##}",
                $"Direction: {direction}"
            ]
        };
    }

    private static DateOnly ClampToPolicyPeriod(DateOnly target, DateOnly inception, DateOnly expiry)
    {
        if (target < inception)
        {
            return inception;
        }

        if (target > expiry)
        {
            return expiry;
        }

        return target;
    }

    private static void ValidatePolicyPeriod(DateOnly inception, DateOnly expiry)
    {
        if (expiry <= inception)
        {
            throw new ArgumentException("Policy expiry must be after inception.");
        }
    }
}
