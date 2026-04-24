using InsuranceIntegration.Api.Services.Correlation;

namespace InsuranceIntegration.Api.Tests.Correlation;

public sealed class CorrelationContextTests
{
    [Test]
    public void NewInstance_HasNonEmptyCorrelationIdAndNullCausation()
    {
        var context = new CorrelationContext();

        Assert.That(context.CorrelationId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(context.CausationId, Is.Null);
    }

    [Test]
    public void Set_UpdatesCorrelationAndCausation()
    {
        var context = new CorrelationContext();
        var correlation = Guid.CreateVersion7();
        var causation = Guid.CreateVersion7();

        context.Set(correlation, causation);

        Assert.That(context.CorrelationId, Is.EqualTo(correlation));
        Assert.That(context.CausationId, Is.EqualTo(causation));
    }

    [Test]
    public void Set_WithoutCausation_ClearsCausation()
    {
        var context = new CorrelationContext();
        context.Set(Guid.CreateVersion7(), Guid.CreateVersion7());

        context.Set(Guid.CreateVersion7());

        Assert.That(context.CausationId, Is.Null);
    }
}
