using Bunit;
using InsuranceIntegration.Api.Components.Pages;
using InsuranceIntegration.Api.Events;

namespace InsuranceIntegration.Api.IntegrationTests.Ui;

/// <summary>
/// bUnit coverage for the policy detail page (<c>/policies/{reference}</c>): the not-found alert, the
/// loaded snapshot header/badge/parties, the optional quote back-link, and the embedded
/// <c>EventFlow</c> timeline (empty state and populated).
/// </summary>
public sealed class PolicyDetailPageTests : UiPageTestBase
{
    [Test]
    public void PolicyDetail_ShowsNotFound_WhenThePolicyIsMissing()
    {
        var stub = new UiGatewayStub { PolicyDetail = null };

        var cut = Render<PolicyDetail>(stub, parameters => parameters.Add(p => p.PolicyReference, "POL-NOPE"));

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".alert.error").TextContent, Does.Contain("POL-NOPE"));
            Assert.That(cut.Find(".alert.error").TextContent, Does.Contain("was not found"));
            Assert.That(stub.LastFindPolicyReference, Is.EqualTo("POL-NOPE"));
        });
    }

    [Test]
    public void PolicyDetail_RendersTheSnapshot_WhenFound()
    {
        var stub = new UiGatewayStub
        {
            PolicyDetail = UiTestData.PolicyDetail(reference: "POL-PROP-01", currentPhase: "Bound"),
        };

        var cut = Render<PolicyDetail>(stub, parameters => parameters.Add(p => p.PolicyReference, "POL-PROP-01"));

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("h1").TextContent, Does.Contain("POL-PROP-01"));
            Assert.That(cut.Find(".badge").TextContent.Trim(), Is.EqualTo("Bound"));
            Assert.That(cut.Markup, Does.Contain("Northwind Storage Ltd"));
        });
    }

    [Test]
    public void PolicyDetail_LinksBackToTheOriginatingQuote()
    {
        var stub = new UiGatewayStub
        {
            PolicyDetail = UiTestData.PolicyDetail(quoteReference: "QF-PROP-01"),
        };

        var cut = Render<PolicyDetail>(stub, parameters => parameters.Add(p => p.PolicyReference, "POL-PROP-01"));

        var quoteLink = cut.Find("a[href='quotes/QF-PROP-01']");
        Assert.That(quoteLink.TextContent.Trim(), Is.EqualTo("QF-PROP-01"));
    }

    [Test]
    public void PolicyDetail_OmitsTheQuoteLink_WhenThereIsNoOriginatingQuote()
    {
        var stub = new UiGatewayStub
        {
            PolicyDetail = UiTestData.PolicyDetail(quoteReference: null),
        };

        var cut = Render<PolicyDetail>(stub, parameters => parameters.Add(p => p.PolicyReference, "POL-PROP-01"));

        Assert.That(cut.FindAll("a[href^='quotes/']"), Is.Empty);
    }

    [Test]
    public void PolicyDetail_ShowsTheEventFlowEmptyState_WhenNoEventsRecorded()
    {
        var stub = new UiGatewayStub
        {
            PolicyDetail = UiTestData.PolicyDetail(),
            AggregateEvents = [],
        };

        var cut = Render<PolicyDetail>(stub, parameters => parameters.Add(p => p.PolicyReference, "POL-PROP-01"));

        Assert.Multiple(() =>
        {
            Assert.That(cut.Markup, Does.Contain("No domain events recorded for this policy."));
            Assert.That(stub.LastAggregateEventsKind, Is.EqualTo("Policy"));
            Assert.That(stub.LastAggregateEventsKey, Is.EqualTo("POL-PROP-01"));
        });
    }

    [Test]
    public void PolicyDetail_RendersTheEventTimeline_WhenEventsExist()
    {
        var stub = new UiGatewayStub
        {
            PolicyDetail = UiTestData.PolicyDetail(),
            AggregateEvents =
            [
                UiTestData.Event(
                    eventType: DomainEventType.PolicyBound,
                    aggregateKind: DomainEventAggregateKind.Policy,
                    aggregateKey: "POL-PROP-01"),
            ],
        };

        var cut = Render<PolicyDetail>(stub, parameters => parameters.Add(p => p.PolicyReference, "POL-PROP-01"));

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll("tbody tr"), Has.Count.EqualTo(1));
            Assert.That(cut.Markup, Does.Contain(DomainEventType.PolicyBound));
        });
    }
}
