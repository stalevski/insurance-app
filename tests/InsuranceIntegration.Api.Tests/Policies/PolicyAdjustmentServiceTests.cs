using InsuranceIntegration.Api.Services.Policies;

namespace InsuranceIntegration.Api.Tests.Policies;

public sealed class PolicyAdjustmentServiceTests
{
    [Test]
    public void CalculateCancellation_ProRataReturnsUnearnedPortionOfPremium()
    {
        var service = new PolicyAdjustmentService();
        var request = new CancellationRequest
        {
            PolicyReference = "POL-1",
            AnnualPremium = 1200m,
            InceptionDate = new DateOnly(2026, 1, 1),
            ExpiryDate = new DateOnly(2027, 1, 1),
            CancellationDate = new DateOnly(2026, 7, 1)
        };

        var result = service.CalculateCancellation(request);

        Assert.That(result.Basis, Is.EqualTo("ProRata"));
        Assert.That(result.ReturnPremium, Is.InRange(590m, 610m));
        Assert.That(result.ShortRatePenalty, Is.EqualTo(0m));
    }

    [Test]
    public void CalculateCancellation_ShortRateAppliesPenalty()
    {
        var service = new PolicyAdjustmentService();
        var request = new CancellationRequest
        {
            PolicyReference = "POL-1",
            AnnualPremium = 1200m,
            InceptionDate = new DateOnly(2026, 1, 1),
            ExpiryDate = new DateOnly(2027, 1, 1),
            CancellationDate = new DateOnly(2026, 7, 1),
            Basis = CancellationBasis.ShortRate,
            ShortRatePenaltyPercent = 0.10m
        };

        var result = service.CalculateCancellation(request);

        Assert.That(result.ShortRatePenalty, Is.GreaterThan(0m));
        Assert.That(result.ReturnPremium, Is.LessThan(result.UnearnedPremium));
    }

    [Test]
    public void CalculateEndorsement_ProRatesPremiumDeltaAcrossRemainingPeriod()
    {
        var service = new PolicyAdjustmentService();
        var request = new EndorsementRequest
        {
            PolicyReference = "POL-1",
            CurrentAnnualPremium = 1200m,
            NewAnnualPremium = 1500m,
            InceptionDate = new DateOnly(2026, 1, 1),
            ExpiryDate = new DateOnly(2027, 1, 1),
            EffectiveDate = new DateOnly(2026, 7, 1)
        };

        var result = service.CalculateEndorsement(request);

        Assert.That(result.PremiumDelta, Is.EqualTo(300m));
        Assert.That(result.AdjustmentDirection, Is.EqualTo("AdditionalPremium"));
        Assert.That(result.ProRataAdjustment, Is.InRange(140m, 160m));
    }

    [Test]
    public void CalculateEndorsement_NegativeDeltaBecomesReturnPremium()
    {
        var service = new PolicyAdjustmentService();
        var request = new EndorsementRequest
        {
            PolicyReference = "POL-1",
            CurrentAnnualPremium = 1200m,
            NewAnnualPremium = 900m,
            InceptionDate = new DateOnly(2026, 1, 1),
            ExpiryDate = new DateOnly(2027, 1, 1),
            EffectiveDate = new DateOnly(2026, 7, 1)
        };

        var result = service.CalculateEndorsement(request);

        Assert.That(result.AdjustmentDirection, Is.EqualTo("ReturnPremium"));
        Assert.That(result.ProRataAdjustment, Is.LessThan(0m));
    }

    [Test]
    public void CalculateReinstatement_GapInCover_DeductsLapsedPremiumAndChargesFeeOnly()
    {
        var service = new PolicyAdjustmentService();
        var request = new ReinstatementRequest
        {
            PolicyReference = "POL-1",
            AnnualPremium = 1200m,
            InceptionDate = new DateOnly(2026, 1, 1),
            ExpiryDate = new DateOnly(2027, 1, 1),
            CancellationDate = new DateOnly(2026, 4, 1),
            ReinstatementDate = new DateOnly(2026, 5, 1),
            ReinstatementFee = 50m,
            ChargeLapsedPremium = false
        };

        var result = service.CalculateReinstatement(request);

        Assert.That(result.GapInCoverage, Is.True);
        Assert.That(result.LapsedDays, Is.EqualTo(30));
        Assert.That(result.LapsedPremium, Is.GreaterThan(0m));
        Assert.That(result.AmountDueOnReinstatement, Is.EqualTo(50m));
        Assert.That(result.ReinstatedAnnualPremium, Is.EqualTo(1200m - result.LapsedPremium));
    }

    [Test]
    public void CalculateReinstatement_ContinuousCover_ChargesLapsedPremiumPlusFeeAndKeepsAnnualPremium()
    {
        var service = new PolicyAdjustmentService();
        var request = new ReinstatementRequest
        {
            PolicyReference = "POL-1",
            AnnualPremium = 1200m,
            InceptionDate = new DateOnly(2026, 1, 1),
            ExpiryDate = new DateOnly(2027, 1, 1),
            CancellationDate = new DateOnly(2026, 4, 1),
            ReinstatementDate = new DateOnly(2026, 5, 1),
            ReinstatementFee = 50m,
            ChargeLapsedPremium = true
        };

        var result = service.CalculateReinstatement(request);

        Assert.That(result.GapInCoverage, Is.False);
        Assert.That(result.ReinstatedAnnualPremium, Is.EqualTo(1200m));
        Assert.That(result.AmountDueOnReinstatement, Is.EqualTo(50m + result.LapsedPremium));
    }

    [Test]
    public void CalculateReinstatement_ReinstatementBeforeCancellation_Throws()
    {
        var service = new PolicyAdjustmentService();
        var request = new ReinstatementRequest
        {
            PolicyReference = "POL-1",
            AnnualPremium = 1200m,
            InceptionDate = new DateOnly(2026, 1, 1),
            ExpiryDate = new DateOnly(2027, 1, 1),
            CancellationDate = new DateOnly(2026, 5, 1),
            ReinstatementDate = new DateOnly(2026, 4, 1)
        };

        Assert.Throws<ArgumentException>(() => service.CalculateReinstatement(request));
    }

    [Test]
    public void CalculateLapse_EarnsProRataPremiumAndComputesOutstandingShortfall()
    {
        var service = new PolicyAdjustmentService();
        var request = new LapseRequest
        {
            PolicyReference = "POL-1",
            AnnualPremium = 1200m,
            InceptionDate = new DateOnly(2026, 1, 1),
            ExpiryDate = new DateOnly(2027, 1, 1),
            LapseDate = new DateOnly(2026, 7, 1),
            PaidToDate = 300m
        };

        var result = service.CalculateLapse(request);

        Assert.That(result.CoveredDays, Is.EqualTo(181));
        Assert.That(result.EarnedPremium, Is.InRange(590m, 600m));
        Assert.That(result.UnearnedPremium, Is.EqualTo(1200m - result.EarnedPremium));
        Assert.That(result.OutstandingPremium, Is.EqualTo(result.EarnedPremium - 300m));
    }

    [Test]
    public void CalculateLapse_PaidBeyondEarned_OutstandingIsZero()
    {
        var service = new PolicyAdjustmentService();
        var request = new LapseRequest
        {
            PolicyReference = "POL-1",
            AnnualPremium = 1200m,
            InceptionDate = new DateOnly(2026, 1, 1),
            ExpiryDate = new DateOnly(2027, 1, 1),
            LapseDate = new DateOnly(2026, 7, 1),
            PaidToDate = 1200m
        };

        var result = service.CalculateLapse(request);

        Assert.That(result.OutstandingPremium, Is.EqualTo(0m));
    }

    [Test]
    public void CalculateNonRenewal_EffectiveAtExpiryWithNoMidTermAdjustment()
    {
        var service = new PolicyAdjustmentService();
        var request = new NonRenewalRequest
        {
            PolicyReference = "POL-1",
            AnnualPremium = 1200m,
            InceptionDate = new DateOnly(2026, 1, 1),
            ExpiryDate = new DateOnly(2027, 1, 1),
            InitiatedBy = NonRenewalInitiator.Insured,
            NoticeDays = 45,
            Reason = "Broker moved book"
        };

        var result = service.CalculateNonRenewal(request);

        Assert.That(result.EffectiveDate, Is.EqualTo(new DateOnly(2027, 1, 1)));
        Assert.That(result.InitiatedBy, Is.EqualTo(NonRenewalInitiator.Insured));
        Assert.That(result.NoticeDays, Is.EqualTo(45));
    }

    [Test]
    public void CalculateNonRenewal_UnknownInitiator_Throws()
    {
        var service = new PolicyAdjustmentService();
        var request = new NonRenewalRequest
        {
            PolicyReference = "POL-1",
            AnnualPremium = 1200m,
            InceptionDate = new DateOnly(2026, 1, 1),
            ExpiryDate = new DateOnly(2027, 1, 1),
            InitiatedBy = "Regulator"
        };

        Assert.Throws<ArgumentException>(() => service.CalculateNonRenewal(request));
    }
}
