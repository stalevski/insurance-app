using Bunit;
using InsuranceIntegration.Api.Events;
using EventsPage = InsuranceIntegration.Api.Components.Pages.Events;

namespace InsuranceIntegration.Api.IntegrationTests.Ui;

/// <summary>
/// bUnit coverage for the domain-event log page (<c>/events</c>): one row per event, the empty state,
/// and that the aggregate/event-type filters forward the selected values to the gateway.
/// </summary>
public sealed class EventsPageTests : UiPageTestBase
{
    [Test]
    public void Events_RendersARowPerEvent()
    {
        var stub = new UiGatewayStub
        {
            Events =
            [
                UiTestData.Event(eventType: DomainEventType.QuoteIssued, aggregateKey: "QF-PROP-01", envelopeId: "qf-1001", correlationId: "corr-1001"),
                UiTestData.Event(eventType: DomainEventType.PolicyBound, aggregateKind: DomainEventAggregateKind.Policy, aggregateKey: "POL-PROP-01"),
            ],
        };

        var cut = Render<EventsPage>(stub);

        var rows = cut.FindAll("tbody tr");
        Assert.That(rows, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(cut.Markup, Does.Contain(DomainEventType.QuoteIssued));
            Assert.That(cut.Markup, Does.Contain(DomainEventType.PolicyBound));
            Assert.That(cut.Markup, Does.Contain("qf-1001"), "Envelope id should be shown.");
            Assert.That(cut.Markup, Does.Contain("corr-1001"), "Correlation id should be shown.");
        });
    }

    [Test]
    public void Events_ShowsEmptyState_WhenNoEventsMatch()
    {
        var stub = new UiGatewayStub { Events = [] };

        var cut = Render<EventsPage>(stub);

        Assert.Multiple(() =>
        {
            Assert.That(cut.Markup, Does.Contain("No events match the current filter."));
            Assert.That(cut.FindAll("tbody tr"), Is.Empty);
        });
    }

    [Test]
    public void Events_ForwardsTheSelectedAggregateKindToTheGateway()
    {
        var stub = new UiGatewayStub { Events = [UiTestData.Event()] };

        var cut = Render<EventsPage>(stub);

        cut.Find("#kind").Change(DomainEventAggregateKind.Policy);

        Assert.Multiple(() =>
        {
            Assert.That(stub.LastEventsAggregateKind, Is.EqualTo(DomainEventAggregateKind.Policy));
            Assert.That(stub.LastEventsEventType, Is.Null, "Leaving the event-type filter unset passes null.");
        });
    }

    [Test]
    public void Events_ForwardsTheSelectedEventTypeToTheGateway()
    {
        var stub = new UiGatewayStub { Events = [UiTestData.Event()] };

        var cut = Render<EventsPage>(stub);

        cut.Find("#type").Change(DomainEventType.PolicyCancelled);

        Assert.Multiple(() =>
        {
            Assert.That(stub.LastEventsEventType, Is.EqualTo(DomainEventType.PolicyCancelled));
            Assert.That(stub.LastEventsAggregateKind, Is.Null, "Leaving the aggregate filter unset passes null.");
        });
    }
}
