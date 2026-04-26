using InsuranceIntegration.Api.CanonicalContracts.Risks;
using InsuranceIntegration.Api.Responses.Risks;
using InsuranceIntegration.Api.Services.Clearance;
using InsuranceIntegration.Api.Services.Matching;

namespace InsuranceIntegration.Api.Services.Flows;

public sealed class RiskFlowService : IRiskFlowService
{
    private readonly ILevenshteinDistanceCalculator _distanceCalculator;
    private readonly ISubmissionClearanceService _submissionClearanceService;
    private readonly ISubmissionRegistry _submissionRegistry;

    public RiskFlowService(
        ILevenshteinDistanceCalculator distanceCalculator,
        ISubmissionClearanceService submissionClearanceService,
        ISubmissionRegistry submissionRegistry)
    {
        _distanceCalculator = distanceCalculator;
        _submissionClearanceService = submissionClearanceService;
        _submissionRegistry = submissionRegistry;
    }

    public FinalRiskResponse Process(CanonicalRiskRequest request)
    {
        var coverageEnrichments = DeriveCoverageEnrichments(request, out var coverageWarnings);
        var allEnrichments = request.Enrichments
            .Concat(DeriveEnrichments(request))
            .Concat(coverageEnrichments)
            .ToList();

        var blockingEnrichmentCount = allEnrichments.Count(item => item.IsBlocking);
        var totalIncurred = request.Claims.Sum(item => item.IncurredAmount);
        var totalReserved = request.Claims.Sum(item => item.ReservedAmount);
        var basePremium = ResolveBasePremium(request);
        var enrichmentMultiplier = allEnrichments.Aggregate(1m, (current, item) => current * item.Multiplier);
        var adjustedPremium = Math.Round(basePremium * enrichmentMultiplier, 2, MidpointRounding.AwayFromZero);
        var (bestDistance, bestDescription) = CalculateBestDistance(request);
        var brokerDecision = ResolveBrokerDecision(request);
        var insuredDecision = ResolveInsuredDecision(request, totalIncurred, bestDistance, blockingEnrichmentCount);
        var submissionStatus = ResolveSubmissionStatus(blockingEnrichmentCount, insuredDecision);
        var quoteStatus = ResolveQuoteStatus(request, blockingEnrichmentCount, adjustedPremium, insuredDecision);
        var policyStatus = ResolvePolicyStatus(request, quoteStatus, brokerDecision, insuredDecision);
        var (clearanceDecision, autoCleared) = ResolveClearanceDecision(request, adjustedPremium, totalIncurred, blockingEnrichmentCount, bestDistance, brokerDecision, insuredDecision);

        var submissionClearance = EvaluateSubmissionClearance(request);
        if (submissionClearance is not null && !submissionClearance.IsCleared)
        {
            clearanceDecision = "ManualClearance";
            autoCleared = false;
        }

        var sectionActions = DescribeSectionActions(request);
        var (totalSumInsured, totalSectionPremium, premiumAllocationBalanced) = SummarizeCoverage(request, basePremium);
        var decisionReasons = BuildDecisionReasons(request, blockingEnrichmentCount, totalIncurred, totalReserved, bestDistance, bestDescription, brokerDecision, insuredDecision, autoCleared);
        decisionReasons.Add($"Total sum insured across active sections: {totalSumInsured:0.##}");
        decisionReasons.Add($"Total section premium: {totalSectionPremium:0.##}; allocation balanced: {premiumAllocationBalanced}");
        if (submissionClearance is not null)
        {
            decisionReasons.Add($"Submission clearance outcome: {submissionClearance.Outcome}");
            decisionReasons.AddRange(submissionClearance.Reasons);
        }

        RegisterSubmissionIfApplicable(request);

        var finalStatus = autoCleared && quoteStatus != QuoteStatusValue.Blocked ? "ReadyForDownstreamDispatch" : "ManualUnderwritingReview";

        return new FinalRiskResponse
        {
            EntityId = request.EntityId,
            ExternalReference = request.ExternalReference,
            ProductCode = request.ProductCode,
            SourceSystem = request.SourceSystem,
            TransactionType = request.TransactionType,
            SubmissionStatus = submissionStatus,
            QuoteStatus = quoteStatus,
            PolicyStatus = policyStatus,
            BrokerDecision = brokerDecision,
            InsuredDecision = insuredDecision,
            ClaimCount = request.Claims.Count,
            SectionCount = request.Sections.Count,
            InstallmentCount = request.Installments.Count,
            SectionOperationCount = request.SectionOperations.Count(item => item.SubcoverCode is null && !item.OperationType.Contains("Subcover", StringComparison.OrdinalIgnoreCase)),
            SubcoverOperationCount = request.SectionOperations.Count(item => item.SubcoverCode is not null || item.RemoveAllSubcovers),
            BlockingEnrichmentCount = blockingEnrichmentCount,
            ClearanceDecision = clearanceDecision,
            AutoCleared = autoCleared,
            TotalIncurredAmount = totalIncurred,
            TotalReservedAmount = totalReserved,
            BasePremium = basePremium,
            AdjustedPremium = adjustedPremium,
            BestFuzzyMatchDistance = bestDistance,
            BestFuzzyMatchDescription = bestDescription,
            DecisionReasons = decisionReasons,
            AppliedEnrichments = allEnrichments.Select(item => $"{item.Family}:{item.Code}").ToList(),
            SectionActions = sectionActions,
            TotalSumInsured = totalSumInsured,
            TotalSectionPremium = totalSectionPremium,
            PremiumAllocationBalanced = premiumAllocationBalanced,
            CoverageWarnings = coverageWarnings,
            FinalStatus = finalStatus
        };
    }

    private static (decimal TotalSumInsured, decimal TotalSectionPremium, bool PremiumAllocationBalanced) SummarizeCoverage(CanonicalRiskRequest request, decimal basePremium)
    {
        var activeSections = request.Sections.Where(section => SectionStatus.IsActive(section.Status)).ToList();
        var totalSumInsured = activeSections.Sum(section => section.SumInsured);
        var totalSectionPremium = activeSections.Sum(section => section.SectionPremium);

        var hasSectionPremium = activeSections.Any(section => section.SectionPremium > 0m);
        if (!hasSectionPremium || basePremium <= 0m)
        {
            return (totalSumInsured, totalSectionPremium, true);
        }

        var tolerance = Math.Max(1m, basePremium * 0.01m);
        var balanced = Math.Abs(totalSectionPremium - basePremium) <= tolerance;
        return (totalSumInsured, totalSectionPremium, balanced);
    }

    private static IReadOnlyCollection<EnrichmentItem> DeriveCoverageEnrichments(CanonicalRiskRequest request, out List<string> warnings)
    {
        warnings = [];
        var enrichments = new List<EnrichmentItem>();

        foreach (var section in request.Sections.Where(section => SectionStatus.IsActive(section.Status)))
        {
            var subcoverPremium = section.Subcovers.Sum(subcover => subcover.Premium);
            if (section.SectionPremium > 0m && subcoverPremium > 0m
                && Math.Abs(section.SectionPremium - subcoverPremium) > Math.Max(1m, section.SectionPremium * 0.01m))
            {
                warnings.Add($"Section {section.SectionCode}: declared premium {section.SectionPremium:0.##} differs from subcover total {subcoverPremium:0.##}");
            }

            foreach (var subcover in section.Subcovers)
            {
                if (subcover.Deductible > subcover.SumInsured && subcover.SumInsured > 0m)
                {
                    warnings.Add($"Subcover {section.SectionCode}/{subcover.SubcoverCode}: deductible {subcover.Deductible:0.##} exceeds sum insured {subcover.SumInsured:0.##}");
                }

                if (subcover.PerOccurrenceLimit is { } perOccurrence
                    && subcover.AggregateLimit is { } aggregate
                    && aggregate < perOccurrence)
                {
                    warnings.Add($"Subcover {section.SectionCode}/{subcover.SubcoverCode}: aggregate limit {aggregate:0.##} below per-occurrence limit {perOccurrence:0.##}");
                }

                if (!DeductibleType.IsRecognized(subcover.DeductibleType))
                {
                    warnings.Add($"Subcover {section.SectionCode}/{subcover.SubcoverCode}: unrecognized deductible type '{subcover.DeductibleType}'");
                }

                foreach (var peril in subcover.Perils)
                {
                    if (peril.SubLimit is { } sublimit && subcover.SumInsured > 0m && sublimit > subcover.SumInsured)
                    {
                        warnings.Add($"Peril {section.SectionCode}/{subcover.SubcoverCode}/{peril.Code}: sub-limit {sublimit:0.##} exceeds subcover sum insured {subcover.SumInsured:0.##}");
                    }
                }
            }
        }

        if (HasUncoveredPerilClaim(request))
        {
            enrichments.Add(new EnrichmentItem
            {
                Family = "Coverage",
                Code = "UNCOVERED_PERIL_CLAIM",
                Description = "Claim references a peril that is excluded or not covered",
                Multiplier = 1m,
                IsDerived = true,
                IsBlocking = true
            });
        }

        if (warnings.Count > 0)
        {
            enrichments.Add(new EnrichmentItem
            {
                Family = "Coverage",
                Code = "STRUCTURE_WARNING",
                Description = "Coverage structure produced warnings",
                Multiplier = 1m,
                IsDerived = true,
                IsBlocking = false
            });
        }

        return enrichments;
    }

    private static bool HasUncoveredPerilClaim(CanonicalRiskRequest request)
    {
        foreach (var claim in request.Claims)
        {
            if (string.IsNullOrWhiteSpace(claim.AffectedPerilCode))
            {
                continue;
            }

            var section = string.IsNullOrWhiteSpace(claim.AffectedSectionCode)
                ? null
                : request.Sections.FirstOrDefault(item => string.Equals(item.SectionCode, claim.AffectedSectionCode, StringComparison.OrdinalIgnoreCase));

            if (section is null)
            {
                continue;
            }

            var subcovers = string.IsNullOrWhiteSpace(claim.AffectedSubcoverCode)
                ? section.Subcovers
                : section.Subcovers.Where(item => string.Equals(item.SubcoverCode, claim.AffectedSubcoverCode, StringComparison.OrdinalIgnoreCase)).ToList();

            if (subcovers.Count == 0)
            {
                return true;
            }

            var matched = false;
            foreach (var subcover in subcovers)
            {
                if (subcover.Exclusions.Any(exclusion => string.Equals(exclusion, claim.AffectedPerilCode, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }

                var peril = subcover.Perils.FirstOrDefault(item => string.Equals(item.Code, claim.AffectedPerilCode, StringComparison.OrdinalIgnoreCase));
                if (peril is null)
                {
                    continue;
                }

                if (!peril.IsCovered)
                {
                    return true;
                }

                matched = true;
            }

            if (!matched && subcovers.Any(item => item.Perils.Count > 0))
            {
                return true;
            }
        }

        return false;
    }

    private static decimal ResolveBasePremium(CanonicalRiskRequest request)
    {
        return request.Submission.BrokerPremium
            ?? request.Submission.TechnicalPremium
            ?? request.AnnualizedGrossPremium
            ?? 0m;
    }

    private static IReadOnlyCollection<EnrichmentItem> DeriveEnrichments(CanonicalRiskRequest request)
    {
        var enrichments = new List<EnrichmentItem>();
        var productCode = request.ProductCode.ToUpperInvariant();

        enrichments.Add(new EnrichmentItem
        {
            Family = "Universal",
            Code = "TRIAGE_DERIVED",
            Description = "Derived intake triage signal",
            Multiplier = 1.01m,
            IsDerived = true,
            IsBlocking = false
        });

        if (productCode.Contains("AUTO") || productCode.Contains("MOTOR") || productCode.Contains("FLEET"))
        {
            enrichments.Add(new EnrichmentItem { Family = "Auto", Code = "DRIVING_HISTORY", Description = "Driving history review", Multiplier = 1.03m, IsDerived = true, IsBlocking = false });
            enrichments.Add(new EnrichmentItem { Family = "Auto", Code = "PAYMENT_HISTORY", Description = "Payment history check", Multiplier = 1.02m, IsDerived = true, IsBlocking = false });
        }

        if (productCode.Contains("PROPERTY") || productCode.Contains("HOME") || productCode.Contains("COMMERCIAL"))
        {
            enrichments.Add(new EnrichmentItem { Family = "Property", Code = "GEO_CAT", Description = "Geo-cat accumulation screening", Multiplier = 1.04m, IsDerived = true, IsBlocking = false });
            enrichments.Add(new EnrichmentItem { Family = "Property", Code = "BUILDING_PROFILE", Description = "Building age and attribute review", Multiplier = 1.02m, IsDerived = true, IsBlocking = false });
        }

        if (productCode.Contains("CYBER"))
        {
            enrichments.Add(new EnrichmentItem { Family = "Cyber", Code = "ATTACK_SURFACE", Description = "External attack surface review", Multiplier = 1.08m, IsDerived = true, IsBlocking = false });
            enrichments.Add(new EnrichmentItem { Family = "Cyber", Code = "RANSOMWARE_CONTROLS", Description = "Ransomware controls check", Multiplier = 1.05m, IsDerived = true, IsBlocking = false });
        }

        if (productCode.Contains("LIABILITY") || productCode.Contains("PROFESSIONAL") || productCode.Contains("MISC"))
        {
            enrichments.Add(new EnrichmentItem { Family = "Liability", Code = "SANCTIONS_SCREEN", Description = "Sanctions screening review", Multiplier = 1.01m, IsDerived = true, IsBlocking = false });
            enrichments.Add(new EnrichmentItem { Family = "Liability", Code = "FINANCIAL_HEALTH", Description = "Financial health review", Multiplier = 1.03m, IsDerived = true, IsBlocking = request.Insured.AnnualRevenue is < 100000m });
        }

        if (request.Submission.UnderwritingYear < DateTime.UtcNow.Year)
        {
            enrichments.Add(new EnrichmentItem { Family = "Universal", Code = "PRIOR_YEAR_CONTEXT", Description = "Prior underwriting year context", Multiplier = 1.01m, IsDerived = true, IsBlocking = false });
        }

        if (request.Claims.Count >= 3)
        {
            enrichments.Add(new EnrichmentItem { Family = "Universal", Code = "CLAIM_HISTORY_WEIGHT", Description = "Claim history burden enrichment", Multiplier = 1.07m, IsDerived = true, IsBlocking = true });
        }

        if (request.Sections.Sum(section => section.Subcovers.Count) >= 4)
        {
            enrichments.Add(new EnrichmentItem { Family = "Universal", Code = "SECTION_COMPLEXITY", Description = "Section structure complexity", Multiplier = 1.03m, IsDerived = true, IsBlocking = false });
        }

        var revenue = request.Insured.AnnualRevenue ?? request.Submission.Revenue ?? 0m;
        if (revenue >= 10000000m)
        {
            enrichments.Add(new EnrichmentItem { Family = "Universal", Code = "LARGE_ACCOUNT", Description = "Large insured revenue signal", Multiplier = 1.06m, IsDerived = true, IsBlocking = false });
        }

        return enrichments;
    }

    private (int Distance, string Description) CalculateBestDistance(CanonicalRiskRequest request)
    {
        var candidates = new List<(int Distance, string Description)>();
        var insuredName = request.Insured.FullName ?? string.Empty;

        foreach (var party in request.Parties)
        {
            candidates.Add((_distanceCalculator.Calculate(insuredName, party.Name), $"Insured to party '{party.Name}'"));
        }

        foreach (var claim in request.Claims)
        {
            candidates.Add((_distanceCalculator.Calculate(insuredName, claim.ClaimantName), $"Insured to claimant '{claim.ClaimantName}'"));
        }

        if (candidates.Count == 0)
        {
            return (0, "No matching candidates supplied");
        }

        return candidates.OrderBy(item => item.Distance).First();
    }

    private static string ResolveBrokerDecision(CanonicalRiskRequest request)
    {
        if (request.Broker.HasDelegatedAuthority)
        {
            return "DelegatedBindAuthority";
        }

        if (request.Broker.IsPreferredPartner)
        {
            return "PreferredBroker";
        }

        if (!string.IsNullOrWhiteSpace(request.Broker.BrokerCode))
        {
            return "ManualBrokerReview";
        }

        return "UnknownBroker";
    }

    private static string ResolveInsuredDecision(CanonicalRiskRequest request, decimal totalIncurred, int bestDistance, int blockingEnrichmentCount)
    {
        if (blockingEnrichmentCount > 0 || totalIncurred > 25000m)
        {
            return "Decline";
        }

        if (bestDistance > request.Clearance.FuzzyMatchTolerance || request.Insured.YearsInBusiness < 2)
        {
            return "ReferSeniorUnderwriter";
        }

        if (!string.IsNullOrWhiteSpace(request.Insured.FullName))
        {
            return "AcceptableInsured";
        }

        return "UnknownInsured";
    }

    private static string ResolveSubmissionStatus(int blockingEnrichmentCount, string insuredDecision)
    {
        return blockingEnrichmentCount == 0 && insuredDecision != "ReferSeniorUnderwriter"
            ? "Received"
            : "ReferralPending";
    }

    private static string ResolveQuoteStatus(CanonicalRiskRequest request, int blockingEnrichmentCount, decimal adjustedPremium, string insuredDecision)
    {
        if (blockingEnrichmentCount > 0 || insuredDecision == "Decline")
        {
            return QuoteStatusValue.Blocked;
        }

        var transactionType = request.TransactionType;

        if (string.Equals(transactionType, QuoteTransactionType.Bind, StringComparison.OrdinalIgnoreCase)
            || string.Equals(transactionType, QuoteTransactionType.Quoted, StringComparison.OrdinalIgnoreCase))
        {
            return adjustedPremium > 0m ? QuoteStatusValue.Quoted : QuoteStatusValue.NotQuoted;
        }

        if (string.Equals(transactionType, QuoteTransactionType.Quotable, StringComparison.OrdinalIgnoreCase))
        {
            return adjustedPremium > 0m ? QuoteStatusValue.Indicative : QuoteStatusValue.NotQuoted;
        }

        if (adjustedPremium > 0m && insuredDecision == "AcceptableInsured")
        {
            return QuoteStatusValue.Quoted;
        }

        if (adjustedPremium > 0m)
        {
            return QuoteStatusValue.Indicative;
        }

        return QuoteStatusValue.NotQuoted;
    }

    private static string ResolvePolicyStatus(CanonicalRiskRequest request, string quoteStatus, string brokerDecision, string insuredDecision)
    {
        var transactionType = request.TransactionType;
        var brokerEligible = brokerDecision is "DelegatedBindAuthority" or "PreferredBroker";
        var insuredEligible = insuredDecision == "AcceptableInsured";

        if (string.Equals(transactionType, PolicyTransactionType.Cancellation, StringComparison.OrdinalIgnoreCase))
        {
            return PolicyStatusValue.Cancelled;
        }

        if (string.Equals(transactionType, PolicyTransactionType.Reinstatement, StringComparison.OrdinalIgnoreCase))
        {
            return PolicyStatusValue.Reinstated;
        }

        if (string.Equals(transactionType, PolicyTransactionType.Renewal, StringComparison.OrdinalIgnoreCase))
        {
            return insuredEligible ? PolicyStatusValue.Renewed : PolicyStatusValue.Draft;
        }

        if (string.Equals(transactionType, PolicyTransactionType.MidTermAdjustment, StringComparison.OrdinalIgnoreCase))
        {
            return insuredEligible ? PolicyStatusValue.Endorsed : PolicyStatusValue.Draft;
        }

        if (string.Equals(transactionType, "PolicyBind", StringComparison.OrdinalIgnoreCase)
            || string.Equals(transactionType, QuoteTransactionType.Bind, StringComparison.OrdinalIgnoreCase))
        {
            return insuredEligible && brokerEligible ? PolicyStatusValue.Bound : PolicyStatusValue.Draft;
        }

        return quoteStatus == QuoteStatusValue.Quoted && brokerEligible && insuredEligible
            ? PolicyStatusValue.ReadyToBind
            : PolicyStatusValue.Draft;
    }

    private SubmissionClearanceResult? EvaluateSubmissionClearance(CanonicalRiskRequest request)
    {
        if (!SubmissionTransactionType.IsSubmissionTransaction(request.TransactionType))
        {
            return null;
        }

        return _submissionClearanceService.Evaluate(request);
    }

    private void RegisterSubmissionIfApplicable(CanonicalRiskRequest request)
    {
        if (!SubmissionTransactionType.IsSubmissionTransaction(request.TransactionType))
        {
            return;
        }

        _submissionRegistry.Register(new KnownSubmissionRecord
        {
            ExternalReference = request.ExternalReference,
            InsuredName = request.Insured.FullName ?? string.Empty,
            ProductCode = request.ProductCode,
            UnderwritingYear = request.Submission.UnderwritingYear,
            BrokerCode = request.Broker.BrokerCode
        });
    }

    private static (string Decision, bool AutoCleared) ResolveClearanceDecision(CanonicalRiskRequest request, decimal adjustedPremium, decimal totalIncurred, int blockingEnrichmentCount, int bestDistance, string brokerDecision, string insuredDecision)
    {
        var checksComplete = request.ContractChecks.All(item => item.IsComplete) && request.ComplianceChecks.All(item => item.IsComplete);
        var brokerEligible = brokerDecision is "DelegatedBindAuthority" or "PreferredBroker";
        var insuredEligible = insuredDecision == "AcceptableInsured";
        var autoCleared = request.Clearance.AutoClearanceEnabled
            && adjustedPremium <= request.Clearance.PremiumThreshold
            && totalIncurred <= 5000m
            && checksComplete
            && bestDistance <= request.Clearance.FuzzyMatchTolerance
            && blockingEnrichmentCount == 0
            && brokerEligible
            && insuredEligible;

        return autoCleared ? ("AutoCleared", true) : ("ManualClearance", false);
    }

    private static List<string> DescribeSectionActions(CanonicalRiskRequest request)
    {
        return request.SectionOperations.Select(operation =>
        {
            if (operation.RemoveAllSubcovers)
            {
                return $"Remove all subcovers from section {operation.SectionCode}";
            }

            if (!string.IsNullOrWhiteSpace(operation.SubcoverCode))
            {
                return $"{operation.OperationType} subcover {operation.SubcoverCode} in section {operation.SectionCode}";
            }

            return $"{operation.OperationType} section {operation.SectionCode}";
        }).ToList();
    }

    private static List<string> BuildDecisionReasons(CanonicalRiskRequest request, int blockingEnrichmentCount, decimal totalIncurred, decimal totalReserved, int bestDistance, string bestDescription, string brokerDecision, string insuredDecision, bool autoCleared)
    {
        return
        [
            $"Blocking enrichments: {blockingEnrichmentCount}",
            $"Claim burden incurred/reserved: {totalIncurred:0.##}/{totalReserved:0.##}",
            $"Best fuzzy match distance {bestDistance} from {bestDescription}",
            $"Broker decision: {brokerDecision}",
            $"Insured decision: {insuredDecision}",
            $"Auto clearance evaluated as {(autoCleared ? "eligible" : "manual review required")}",
            $"Contract checks complete: {request.ContractChecks.All(item => item.IsComplete)}; Compliance checks complete: {request.ComplianceChecks.All(item => item.IsComplete)}"
        ];
    }
}
