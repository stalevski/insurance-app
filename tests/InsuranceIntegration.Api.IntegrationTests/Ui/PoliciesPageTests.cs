using Bunit;
using InsuranceIntegration.Api.Components.Pages;

namespace InsuranceIntegration.Api.IntegrationTests.Ui;

/// <summary>
/// bUnit coverage for the policies list page (<c>/policies</c>): one row per snapshot, the link back
/// to the originating quote (with a placeholder when absent), and the empty state.
/// </summary>
[Category("Ui")]
public sealed class PoliciesPageTests
{
    [Test]
    public void Policies_RendersARowPerPolicy()
    {
        var stub = new UiGatewayStub
        {
            Policies =
            [
                UiTestData.Policy(reference: "POL-PROP-01"),
                UiTestData.Policy(reference: "POL-LIAB-02", quoteReference: "QF-LIAB-02", productCode: "LIABILITY"),
            ],
        };

        using var context = PageRenderer.ContextFor(stub);
        var cut = context.Render<Policies>();

        var rows = cut.FindAll("tbody tr");
        Assert.That(rows, Has.Count.EqualTo(2));

        var policyLink = cut.Find("tbody tr td:first-child a");
        Assert.Multiple(() =>
        {
            Assert.That(policyLink.GetAttribute("href"), Is.EqualTo("policies/POL-PROP-01"));
            Assert.That(policyLink.TextContent.Trim(), Is.EqualTo("POL-PROP-01"));
        });
    }

    [Test]
    public void Policies_LinksTheOriginatingQuote()
    {
        var stub = new UiGatewayStub
        {
            Policies = [UiTestData.Policy(reference: "POL-PROP-01", quoteReference: "QF-PROP-01")],
        };

        using var context = PageRenderer.ContextFor(stub);
        var cut = context.Render<Policies>();

        var quoteLink = cut.Find("tbody tr td:nth-child(2) a");
        Assert.That(quoteLink.GetAttribute("href"), Is.EqualTo("quotes/QF-PROP-01"));
    }

    [Test]
    public void Policies_ShowsAPlaceholder_WhenNoQuoteLinked()
    {
        var stub = new UiGatewayStub
        {
            Policies = [UiTestData.Policy(reference: "POL-PROP-01", quoteReference: null)],
        };

        using var context = PageRenderer.ContextFor(stub);
        var cut = context.Render<Policies>();

        var quoteCell = cut.Find("tbody tr td:nth-child(2)");
        Assert.Multiple(() =>
        {
            Assert.That(quoteCell.QuerySelector("a"), Is.Null, "An unlinked policy must not render a quote anchor.");
            Assert.That(quoteCell.TextContent.Trim(), Is.EqualTo("\u2014"));
        });
    }

    [Test]
    public void Policies_ShowsEmptyState_WhenNoPolicies()
    {
        var stub = new UiGatewayStub { Policies = [] };

        using var context = PageRenderer.ContextFor(stub);
        var cut = context.Render<Policies>();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Markup, Does.Contain("No policies yet."));
            Assert.That(cut.FindAll("table"), Is.Empty);
        });
    }
}
