using InsuranceIntegration.Api.CanonicalContracts.Risks;
using InsuranceIntegration.Api.Services.Matching;

namespace InsuranceIntegration.Api.Services.Clearance;

public sealed class SubmissionClearanceService : ISubmissionClearanceService
{
    private readonly ISubmissionRegistry _registry;
    private readonly ILevenshteinDistanceCalculator _distanceCalculator;

    public SubmissionClearanceService(ISubmissionRegistry registry, ILevenshteinDistanceCalculator distanceCalculator)
    {
        _registry = registry;
        _distanceCalculator = distanceCalculator;
    }

    public SubmissionClearanceResult Evaluate(CanonicalRiskRequest request)
    {
        var reasons = new List<string>();
        var insuredName = request.Insured.FullName ?? string.Empty;
        var tolerance = Math.Max(request.Clearance.FuzzyMatchTolerance, 0);
        var bestDistance = int.MaxValue;
        string? duplicateReference = null;
        var conflictingBroker = false;

        foreach (var known in _registry.GetKnownSubmissions())
        {
            if (!string.Equals(known.ProductCode, request.ProductCode, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (known.UnderwritingYear != request.Submission.UnderwritingYear)
            {
                continue;
            }

            var distance = _distanceCalculator.Calculate(insuredName, known.InsuredName);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                duplicateReference = known.ExternalReference;
            }

            if (distance <= tolerance
                && !string.IsNullOrWhiteSpace(known.BrokerCode)
                && !string.IsNullOrWhiteSpace(request.Broker.BrokerCode)
                && !string.Equals(known.BrokerCode, request.Broker.BrokerCode, StringComparison.OrdinalIgnoreCase))
            {
                conflictingBroker = true;
            }
        }

        if (bestDistance == int.MaxValue)
        {
            reasons.Add("No prior submissions found for insured/product/underwriting year");
            return new SubmissionClearanceResult
            {
                Outcome = SubmissionClearanceOutcome.Cleared,
                IsCleared = true,
                BestFuzzyMatchDistance = 0,
                BestFuzzyMatchDescription = "No comparable prior submissions",
                Reasons = reasons
            };
        }

        var matchDescription = !string.IsNullOrWhiteSpace(duplicateReference)
            ? $"Insured to known submission '{duplicateReference}'"
            : "Insured to known submissions";

        reasons.Add($"Best fuzzy distance against known submissions: {bestDistance}");

        if (conflictingBroker)
        {
            reasons.Add($"Conflicting broker detected against {duplicateReference}");
            return new SubmissionClearanceResult
            {
                Outcome = SubmissionClearanceOutcome.ConflictingBroker,
                IsCleared = false,
                DuplicateExternalReference = duplicateReference,
                BestFuzzyMatchDistance = bestDistance,
                BestFuzzyMatchDescription = matchDescription,
                Reasons = reasons
            };
        }

        if (bestDistance <= tolerance)
        {
            reasons.Add($"Potential duplicate submission for {duplicateReference}");
            return new SubmissionClearanceResult
            {
                Outcome = SubmissionClearanceOutcome.DuplicateSubmission,
                IsCleared = false,
                DuplicateExternalReference = duplicateReference,
                BestFuzzyMatchDistance = bestDistance,
                BestFuzzyMatchDescription = matchDescription,
                Reasons = reasons
            };
        }

        if (bestDistance <= tolerance + 2)
        {
            reasons.Add("Near-duplicate detected, manual clearance review recommended");
            return new SubmissionClearanceResult
            {
                Outcome = SubmissionClearanceOutcome.ManualClearanceReview,
                IsCleared = false,
                DuplicateExternalReference = duplicateReference,
                BestFuzzyMatchDistance = bestDistance,
                BestFuzzyMatchDescription = matchDescription,
                Reasons = reasons
            };
        }

        reasons.Add("No duplicates within clearance tolerance");
        return new SubmissionClearanceResult
        {
            Outcome = SubmissionClearanceOutcome.Cleared,
            IsCleared = true,
            BestFuzzyMatchDistance = bestDistance,
            BestFuzzyMatchDescription = matchDescription,
            Reasons = reasons
        };
    }
}
