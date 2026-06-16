using Bunit;
using InsuranceIntegration.Api.Components.Pages;

namespace InsuranceIntegration.Api.IntegrationTests.Ui;

/// <summary>
/// bUnit coverage for the quote detail page (<c>/quotes/{reference}</c>): the not-found alert, the
/// loaded snapshot header/badge, the optional bind-rejection note, the policy forward-link, and the
/// embedded <c>EventFlow</c> empty state.
/// </summary>
public sealed class QuoteDetailPageTests : UiPageTestBase
{
    [Test]
    public void QuoteDetail_ShowsNotFound_WhenTheQuoteIsMissing()
    {
        var stub = new UiGatewayStub { QuoteDetail = null };

        var cut = Render<QuoteDetail>(stub, parameters => parameters.Add(p => p.QuoteReference, "QF-NOPE"));

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".alert.error").TextContent, Does.Contain("QF-NOPE"));
            Assert.That(cut.Find(".alert.error").TextContent, Does.Contain("was not found"));
            Assert.That(stub.LastFindQuoteReference, Is.EqualTo("QF-NOPE"));
        });
    }

    [Test]
    public void QuoteDetail_RendersTheSnapshot_WhenFound()
    {
        var stub = new UiGatewayStub
        {
            QuoteDetail = UiTestData.QuoteDetail(reference: "QF-PROP-01", currentPhase: "Quoted"),
        };

        var cut = Render<QuoteDetail>(stub, parameters => parameters.Add(p => p.QuoteReference, "QF-PROP-01"));

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("h1").TextContent, Does.Contain("QF-PROP-01"));
            Assert.That(cut.Find(".badge").TextContent.Trim(), Is.EqualTo("Quoted"));
            Assert.That(cut.Markup, Does.Contain("Northwind Storage Ltd"));
        });
    }

    [Test]
    public void QuoteDetail_ShowsTheBindRejectionReason_WhenPresent()
    {
        var stub = new UiGatewayStub
        {
            QuoteDetail = UiTestData.QuoteDetail(bindRejectionReason: "Quote expired before bind."),
        };

        var cut = Render<QuoteDetail>(stub, parameters => parameters.Add(p => p.QuoteReference, "QF-PROP-01"));

        Assert.That(cut.Markup, Does.Contain("Quote expired before bind."));
    }

    [Test]
    public void QuoteDetail_LinksToTheResultingPolicy()
    {
        var stub = new UiGatewayStub
        {
            QuoteDetail = UiTestData.QuoteDetail(policyReference: "POL-PROP-01"),
        };

        var cut = Render<QuoteDetail>(stub, parameters => parameters.Add(p => p.QuoteReference, "QF-PROP-01"));

        var policyLink = cut.Find("a[href='policies/POL-PROP-01']");
        Assert.That(policyLink.TextContent.Trim(), Is.EqualTo("POL-PROP-01"));
    }

    [Test]
    public void QuoteDetail_ShowsTheEventFlowEmptyState_WhenNoEventsRecorded()
    {
        var stub = new UiGatewayStub
        {
            QuoteDetail = UiTestData.QuoteDetail(),
            AggregateEvents = [],
        };

        var cut = Render<QuoteDetail>(stub, parameters => parameters.Add(p => p.QuoteReference, "QF-PROP-01"));

        Assert.Multiple(() =>
        {
            Assert.That(cut.Markup, Does.Contain("No domain events recorded for this quote."));
            Assert.That(stub.LastAggregateEventsKind, Is.EqualTo("Quote"));
            Assert.That(stub.LastAggregateEventsKey, Is.EqualTo("QF-PROP-01"));
        });
    }
}
