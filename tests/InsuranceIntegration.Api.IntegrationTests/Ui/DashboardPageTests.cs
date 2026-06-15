using Bunit;
using InsuranceIntegration.Api.Components.Pages;
using InsuranceIntegration.Api.Events;
using InsuranceIntegration.Api.Services.Ui;

namespace InsuranceIntegration.Api.IntegrationTests.Ui;

/// <summary>
/// bUnit coverage for the dashboard page (<c>/</c>). The page renders six metric cards from the
/// gateway's <see cref="DashboardSummary"/> and a recent-events table that collapses to an empty
/// state when no events have been recorded.
/// </summary>
public sealed class DashboardPageTests
{
    [Test]
    public void Dashboard_RendersTheSixMetricCardsInOrder()
    {
        var stub = new UiGatewayStub
        {
            Dashboard = new DashboardSummary
            {
                QuoteCount = 32,
                BoundQuoteCount = 8,
                PolicyCount = 6,
                DomainEventCount = 120,
                IngestEntryCount = 40,
                PendingOutboxCount = 3,
            },
        };

        using var context = PageRenderer.ContextFor(stub);
        var cut = context.Render<Home>();

        var metrics = cut.FindAll(".card .metric");
        Assert.That(metrics, Has.Count.EqualTo(6));
        Assert.Multiple(() =>
        {
            Assert.That(metrics[0].TextContent.Trim(), Is.EqualTo("32"), "Quotes");
            Assert.That(metrics[1].TextContent.Trim(), Is.EqualTo("8"), "Bound quotes");
            Assert.That(metrics[2].TextContent.Trim(), Is.EqualTo("6"), "Policies");
            Assert.That(metrics[3].TextContent.Trim(), Is.EqualTo("120"), "Domain events");
            Assert.That(metrics[4].TextContent.Trim(), Is.EqualTo("40"), "Ingest entries");
            Assert.That(metrics[5].TextContent.Trim(), Is.EqualTo("3"), "Pending outbox");
        });
    }

    [Test]
    public void Dashboard_RendersARowPerRecentEvent()
    {
        var stub = new UiGatewayStub
        {
            Dashboard = new DashboardSummary
            {
                RecentEvents =
                [
                    UiTestData.Recent(eventType: DomainEventType.QuoteIssued, aggregateKey: "QF-PROP-01"),
                    UiTestData.Recent(eventType: DomainEventType.PolicyBound, aggregateKind: DomainEventAggregateKind.Policy, aggregateKey: "POL-PROP-01"),
                ],
            },
        };

        using var context = PageRenderer.ContextFor(stub);
        var cut = context.Render<Home>();

        var rows = cut.FindAll("tbody tr");
        Assert.That(rows, Has.Count.EqualTo(2));
        var badges = cut.FindAll("tbody .badge");
        Assert.Multiple(() =>
        {
            Assert.That(badges[0].TextContent.Trim(), Is.EqualTo(DomainEventType.QuoteIssued));
            Assert.That(badges[1].TextContent.Trim(), Is.EqualTo(DomainEventType.PolicyBound));
        });
    }

    [Test]
    public void Dashboard_LinksAggregateKeysToTheirSnapshotPages()
    {
        var stub = new UiGatewayStub
        {
            Dashboard = new DashboardSummary
            {
                RecentEvents =
                [
                    UiTestData.Recent(aggregateKind: DomainEventAggregateKind.Quote, aggregateKey: "QF-PROP-01"),
                ],
            },
        };

        using var context = PageRenderer.ContextFor(stub);
        var cut = context.Render<Home>();

        var link = cut.Find("tbody a");
        Assert.Multiple(() =>
        {
            Assert.That(link.GetAttribute("href"), Is.EqualTo("quotes/QF-PROP-01"));
            Assert.That(link.TextContent.Trim(), Is.EqualTo("QF-PROP-01"));
        });
    }

    [Test]
    public void Dashboard_ShowsEmptyState_WhenNoEventsRecorded()
    {
        var stub = new UiGatewayStub { Dashboard = new DashboardSummary() };

        using var context = PageRenderer.ContextFor(stub);
        var cut = context.Render<Home>();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Markup, Does.Contain("No events recorded yet."));
            Assert.That(cut.FindAll("table"), Is.Empty, "The recent-events table should be absent when there are no events.");
        });
    }
}
