using InsuranceIntegration.Api.Services.Policies;

namespace InsuranceIntegration.Api.Tests.Policies;

public sealed class EndorsementSectionOperationsTests
{
    [Test]
    public void CalculateEndorsement_AggregatesSumInsuredAndDeductibleDeltasFromOperations()
    {
        var service = new PolicyAdjustmentService();
        var request = new EndorsementRequest
        {
            PolicyReference = "POL-OP-1",
            CurrentAnnualPremium = 1200m,
            NewAnnualPremium = 1500m,
            InceptionDate = new DateOnly(2026, 1, 1),
            ExpiryDate = new DateOnly(2027, 1, 1),
            EffectiveDate = new DateOnly(2026, 7, 1),
            SectionOperations =
            [
                new SectionEndorsementOperation
                {
                    OperationType = SectionEndorsementOperationType.UpdateLimit,
                    SectionCode = "PROP",
                    SubcoverCode = "FIRE",
                    SumInsuredDelta = 250_000m,
                    PremiumDelta = 200m,
                    Reason = "Building extension"
                },
                new SectionEndorsementOperation
                {
                    OperationType = SectionEndorsementOperationType.UpdateDeductible,
                    SectionCode = "PROP",
                    SubcoverCode = "FIRE",
                    DeductibleDelta = -500m,
                    PremiumDelta = 100m
                }
            ]
        };

        var result = service.CalculateEndorsement(request);

        Assert.That(result.SumInsuredDelta, Is.EqualTo(250_000m));
        Assert.That(result.DeductibleDelta, Is.EqualTo(-500m));
        Assert.That(result.OperationsApplied, Has.Count.EqualTo(2));
        Assert.That(result.OperationsApplied[0], Does.Contain("UpdateLimit"));
        Assert.That(result.OperationsApplied[0], Does.Contain("PROP/FIRE"));
        Assert.That(result.OperationsApplied[0], Does.Contain("Building extension"));
        Assert.That(result.Reasons, Has.Some.Contains("Section operations: 2"));
    }

    [Test]
    public void CalculateEndorsement_WithNoOperationsDoesNotIntroduceSectionDeltas()
    {
        var service = new PolicyAdjustmentService();
        var request = new EndorsementRequest
        {
            PolicyReference = "POL-OP-2",
            CurrentAnnualPremium = 1200m,
            NewAnnualPremium = 1300m,
            InceptionDate = new DateOnly(2026, 1, 1),
            ExpiryDate = new DateOnly(2027, 1, 1),
            EffectiveDate = new DateOnly(2026, 7, 1)
        };

        var result = service.CalculateEndorsement(request);

        Assert.That(result.SumInsuredDelta, Is.EqualTo(0m));
        Assert.That(result.DeductibleDelta, Is.EqualTo(0m));
        Assert.That(result.OperationsApplied, Is.Empty);
    }

    [Test]
    public void CalculateEndorsement_DescribesAddSectionOperationWithoutSubcoverCode()
    {
        var service = new PolicyAdjustmentService();
        var request = new EndorsementRequest
        {
            PolicyReference = "POL-OP-3",
            CurrentAnnualPremium = 1000m,
            NewAnnualPremium = 1100m,
            InceptionDate = new DateOnly(2026, 1, 1),
            ExpiryDate = new DateOnly(2027, 1, 1),
            EffectiveDate = new DateOnly(2026, 6, 1),
            SectionOperations =
            [
                new SectionEndorsementOperation
                {
                    OperationType = SectionEndorsementOperationType.AddSection,
                    SectionCode = "BI",
                    SumInsuredDelta = 100_000m,
                    PremiumDelta = 100m
                }
            ]
        };

        var result = service.CalculateEndorsement(request);

        Assert.That(result.OperationsApplied, Has.Count.EqualTo(1));
        Assert.That(result.OperationsApplied[0], Does.Contain("section BI"));
        Assert.That(result.OperationsApplied[0], Does.Not.Contain("/"));
    }
}
