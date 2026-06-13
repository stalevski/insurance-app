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

        var sumInsuredDelta = request.SectionOperations.Sum(operation => operation.SumInsuredDelta);
        var deductibleDelta = request.SectionOperations.Sum(operation => operation.DeductibleDelta);
        var operationDescriptions = request.SectionOperations
            .Select(DescribeOperation)
            .ToList();

        var reasons = new List<string>
        {
            $"Premium delta: {premiumDelta:0.##}",
            $"Remaining days: {remainingDays} of {totalDays}",
            $"Pro-rata adjustment: {proRataAdjustment:0.##}",
            $"Direction: {direction}"
        };

        if (request.SectionOperations.Count > 0)
        {
            reasons.Add($"Section operations: {request.SectionOperations.Count}");
            reasons.Add($"Sum-insured delta: {sumInsuredDelta:0.##}");
            reasons.Add($"Deductible delta: {deductibleDelta:0.##}");
        }

        return new EndorsementResult
        {
            PolicyReference = request.PolicyReference,
            PremiumDelta = premiumDelta,
            ProRataAdjustment = proRataAdjustment,
            AdjustmentDirection = direction,
            SumInsuredDelta = sumInsuredDelta,
            DeductibleDelta = deductibleDelta,
            OperationsApplied = operationDescriptions,
            Reasons = reasons
        };
    }

    public ReinstatementResult CalculateReinstatement(ReinstatementRequest request)
    {
        ValidatePolicyPeriod(request.InceptionDate, request.ExpiryDate);

        if (request.ReinstatementDate < request.CancellationDate)
        {
            throw new ArgumentException("Reinstatement date cannot be earlier than the cancellation date.");
        }

        if (request.ReinstatementFee < 0m)
        {
            throw new ArgumentException("Reinstatement fee cannot be negative.");
        }

        var totalDays = Math.Max(1, (request.ExpiryDate.ToDateTime(TimeOnly.MinValue) - request.InceptionDate.ToDateTime(TimeOnly.MinValue)).Days);
        var cancellationDate = ClampToPolicyPeriod(request.CancellationDate, request.InceptionDate, request.ExpiryDate);
        var reinstatementDate = ClampToPolicyPeriod(request.ReinstatementDate, request.InceptionDate, request.ExpiryDate);
        var lapsedDays = Math.Max(0, (reinstatementDate.ToDateTime(TimeOnly.MinValue) - cancellationDate.ToDateTime(TimeOnly.MinValue)).Days);

        var lapsedPremium = Math.Round(request.AnnualPremium * lapsedDays / totalDays, 2, MidpointRounding.AwayFromZero);

        decimal amountDue;
        decimal reinstatedAnnualPremium;
        if (request.ChargeLapsedPremium)
        {
            // Continuous cover: insured pays for the gap plus the admin fee; premium unchanged.
            amountDue = request.ReinstatementFee + lapsedPremium;
            reinstatedAnnualPremium = request.AnnualPremium;
        }
        else
        {
            // Gap in cover: insured pays only the admin fee; the lapsed premium is not earned.
            amountDue = request.ReinstatementFee;
            reinstatedAnnualPremium = Math.Max(0m, request.AnnualPremium - lapsedPremium);
        }

        var reasons = new List<string>
        {
            $"Lapsed days: {lapsedDays} of {totalDays}",
            $"Lapsed premium (pro-rata): {lapsedPremium:0.##}",
            $"Reinstatement fee: {request.ReinstatementFee:0.##}",
            request.ChargeLapsedPremium
                ? "Continuous cover: lapsed premium charged to the insured"
                : "Gap in cover: lapsed premium deducted from annual premium",
            $"Amount due on reinstatement: {amountDue:0.##}",
            $"Reinstated annual premium: {reinstatedAnnualPremium:0.##}"
        };

        if (!string.IsNullOrWhiteSpace(request.Reason))
        {
            reasons.Add($"Reason: {request.Reason}");
        }

        return new ReinstatementResult
        {
            PolicyReference = request.PolicyReference,
            LapsedDays = lapsedDays,
            ReinstatementFee = request.ReinstatementFee,
            LapsedPremium = lapsedPremium,
            AmountDueOnReinstatement = amountDue,
            ReinstatedAnnualPremium = reinstatedAnnualPremium,
            GapInCoverage = !request.ChargeLapsedPremium,
            Reasons = reasons
        };
    }

    private static string DescribeOperation(SectionEndorsementOperation operation)
    {
        var target = string.IsNullOrWhiteSpace(operation.SubcoverCode)
            ? $"section {operation.SectionCode}"
            : $"{operation.SectionCode}/{operation.SubcoverCode}";

        var details = new List<string>();
        if (operation.SumInsuredDelta != 0m)
        {
            details.Add($"sum insured {operation.SumInsuredDelta:+0.##;-0.##}");
        }

        if (operation.DeductibleDelta != 0m)
        {
            details.Add($"deductible {operation.DeductibleDelta:+0.##;-0.##}");
        }

        if (operation.PremiumDelta != 0m)
        {
            details.Add($"premium {operation.PremiumDelta:+0.##;-0.##}");
        }

        var suffix = details.Count > 0 ? $" [{string.Join(", ", details)}]" : string.Empty;
        var reason = string.IsNullOrWhiteSpace(operation.Reason) ? string.Empty : $" ({operation.Reason})";

        return $"{operation.OperationType} on {target}{suffix}{reason}";
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
