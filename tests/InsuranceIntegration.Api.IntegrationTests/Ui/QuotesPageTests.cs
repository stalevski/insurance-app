using Bunit;
using InsuranceIntegration.Api.Components.Pages;

namespace InsuranceIntegration.Api.IntegrationTests.Ui;

/// <summary>
/// bUnit coverage for the quotes list page (<c>/quotes</c>): one row per snapshot, the bound/unbound
/// badge, the paging summary, and the empty state.
/// </summary>
public sealed class QuotesPageTests : UiPageTestBase
{
    [Test]
    public void Quotes_RendersARowPerQuote()
    {
        var stub = new UiGatewayStub
        {
            Quotes =
            [
                UiTestData.Quote(reference: "QF-PROP-01", productCode: "COMMERCIAL_PROPERTY"),
                UiTestData.Quote(reference: "QF-LIAB-02", productCode: "LIABILITY"),
            ],
        };

        var cut = Render<Quotes>(stub);

        var rows = cut.FindAll("tbody tr");
        Assert.That(rows, Has.Count.EqualTo(2));

        var firstLink = cut.Find("tbody tr a");
        firstLink.ShouldLinkTo("quotes/QF-PROP-01", "QF-PROP-01");
        Assert.Multiple(() =>
        {
            Assert.That(cut.Markup, Does.Contain("COMMERCIAL_PROPERTY"));
            Assert.That(cut.Markup, Does.Contain("LIABILITY"));
        });
    }

    [Test]
    public void Quotes_FlagsBoundAndUnboundQuotes()
    {
        var stub = new UiGatewayStub
        {
            Quotes =
            [
                UiTestData.Quote(reference: "QF-PROP-01", currentPhase: "Bound", isBound: true),
                UiTestData.Quote(reference: "QF-PROP-02", currentPhase: "Quoted", isBound: false),
            ],
        };

        var cut = Render<Quotes>(stub);

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".badge.good").TextContent.Trim(), Is.EqualTo("Bound"));
            Assert.That(cut.Find("tbody .badge.muted").TextContent.Trim(), Is.EqualTo("No"));
        });
    }

    [Test]
    public void Quotes_ShowsThePagingSummary()
    {
        var stub = new UiGatewayStub
        {
            Quotes =
            [
                UiTestData.Quote(reference: "QF-PROP-01"),
                UiTestData.Quote(reference: "QF-PROP-02"),
            ],
        };

        var cut = Render<Quotes>(stub);

        Assert.That(cut.Find(".pager .muted").TextContent.Trim(), Is.EqualTo("Showing 1\u20132"));
    }

    [Test]
    public void Quotes_ShowsEmptyState_WhenNoQuotes()
    {
        var stub = new UiGatewayStub { Quotes = [] };

        var cut = Render<Quotes>(stub);

        cut.ShouldShowEmptyState("No quotes yet.");
    }
}
