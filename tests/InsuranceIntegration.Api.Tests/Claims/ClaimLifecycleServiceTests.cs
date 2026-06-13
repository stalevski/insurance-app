using InsuranceIntegration.Api.CanonicalContracts.Claims;
using InsuranceIntegration.Api.Events;
using InsuranceIntegration.Api.Services.Claims;

namespace InsuranceIntegration.Api.Tests.Claims;

public sealed class ClaimLifecycleServiceTests
{
    private static ClaimTransitionRequest Request(string current, string target) => new()
    {
        ClaimReference = "CLM-1",
        PolicyReference = "POL-1",
        CurrentStatus = current,
        TargetStatus = target
    };

    [TestCase(ClaimStatusValue.Notified, ClaimStatusValue.Open, DomainEventType.ClaimOpened)]
    [TestCase(ClaimStatusValue.Open, ClaimStatusValue.Declined, DomainEventType.ClaimDeclined)]
    [TestCase(ClaimStatusValue.Open, ClaimStatusValue.Closed, DomainEventType.ClaimClosed)]
    [TestCase(ClaimStatusValue.Reserved, ClaimStatusValue.Closed, DomainEventType.ClaimClosed)]
    [TestCase(ClaimStatusValue.Settled, ClaimStatusValue.Closed, DomainEventType.ClaimClosed)]
    [TestCase(ClaimStatusValue.Declined, ClaimStatusValue.Closed, DomainEventType.ClaimClosed)]
    public void Transition_AllowsValidMoves(string current, string target, string expectedEvent)
    {
        var service = new ClaimLifecycleService();

        var result = service.Transition(Request(current, target));

        Assert.Multiple(() =>
        {
            Assert.That(result.PreviousStatus, Is.EqualTo(current));
            Assert.That(result.Status, Is.EqualTo(target));
            Assert.That(result.EventType, Is.EqualTo(expectedEvent));
        });
    }

    [Test]
    public void Transition_ToClosed_MarksTerminal()
    {
        var service = new ClaimLifecycleService();

        var result = service.Transition(Request(ClaimStatusValue.Settled, ClaimStatusValue.Closed));

        Assert.That(result.IsTerminal, Is.True);
    }

    [Test]
    public void Transition_ReserveCapturesAmountAndEvent()
    {
        var service = new ClaimLifecycleService();
        var request = new ClaimTransitionRequest
        {
            ClaimReference = "CLM-1",
            PolicyReference = "POL-1",
            CurrentStatus = ClaimStatusValue.Open,
            TargetStatus = ClaimStatusValue.Reserved,
            ReserveAmount = 12500m
        };

        var result = service.Transition(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.EventType, Is.EqualTo(DomainEventType.ClaimReserved));
            Assert.That(result.ReserveAmount, Is.EqualTo(12500m));
            Assert.That(result.IsTerminal, Is.False);
        });
    }

    [Test]
    public void Transition_SettleCapturesAmountAndEvent()
    {
        var service = new ClaimLifecycleService();
        var request = new ClaimTransitionRequest
        {
            ClaimReference = "CLM-1",
            PolicyReference = "POL-1",
            CurrentStatus = ClaimStatusValue.Reserved,
            TargetStatus = ClaimStatusValue.Settled,
            SettlementAmount = 9800m
        };

        var result = service.Transition(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.EventType, Is.EqualTo(DomainEventType.ClaimSettled));
            Assert.That(result.SettlementAmount, Is.EqualTo(9800m));
        });
    }

    [TestCase(ClaimStatusValue.Notified, ClaimStatusValue.Reserved)]
    [TestCase(ClaimStatusValue.Notified, ClaimStatusValue.Settled)]
    [TestCase(ClaimStatusValue.Open, ClaimStatusValue.Settled)]
    [TestCase(ClaimStatusValue.Reserved, ClaimStatusValue.Open)]
    [TestCase(ClaimStatusValue.Settled, ClaimStatusValue.Reserved)]
    public void Transition_RejectsIllegalMoves(string current, string target)
    {
        var service = new ClaimLifecycleService();

        Assert.Throws<ArgumentException>(() => service.Transition(Request(current, target)));
    }

    [Test]
    public void Transition_FromClosed_Throws()
    {
        var service = new ClaimLifecycleService();

        Assert.Throws<ArgumentException>(() => service.Transition(Request(ClaimStatusValue.Closed, ClaimStatusValue.Open)));
    }

    [Test]
    public void Transition_UnknownStatus_Throws()
    {
        var service = new ClaimLifecycleService();

        Assert.Throws<ArgumentException>(() => service.Transition(Request("Frozen", ClaimStatusValue.Open)));
    }

    [Test]
    public void Transition_ReserveWithoutAmount_Throws()
    {
        var service = new ClaimLifecycleService();

        Assert.Throws<ArgumentException>(() => service.Transition(Request(ClaimStatusValue.Open, ClaimStatusValue.Reserved)));
    }

    [Test]
    public void Transition_SettleWithoutAmount_Throws()
    {
        var service = new ClaimLifecycleService();

        Assert.Throws<ArgumentException>(() => service.Transition(Request(ClaimStatusValue.Reserved, ClaimStatusValue.Settled)));
    }

    [Test]
    public void Transition_NegativeReserve_Throws()
    {
        var service = new ClaimLifecycleService();
        var request = new ClaimTransitionRequest
        {
            ClaimReference = "CLM-1",
            PolicyReference = "POL-1",
            CurrentStatus = ClaimStatusValue.Open,
            TargetStatus = ClaimStatusValue.Reserved,
            ReserveAmount = -1m
        };

        Assert.Throws<ArgumentException>(() => service.Transition(request));
    }
}
