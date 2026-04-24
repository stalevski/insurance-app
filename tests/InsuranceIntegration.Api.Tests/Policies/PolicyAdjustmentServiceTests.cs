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
}
