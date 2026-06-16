using Bunit;
using InsuranceIntegration.Api.Components.Pages;
using InsuranceIntegration.Api.Security;

namespace InsuranceIntegration.Api.IntegrationTests.Ui;

/// <summary>
/// bUnit coverage for the read-only database browser page (<c>/database</c>): the disabled-environment
/// branch driven by <see cref="DatabaseBrowserGate"/>, the table dropdown, rendering a selected
/// table's rows with the total, and the empty "No data." branch.
/// </summary>
public sealed class DatabasePageTests : UiPageTestBase
{
    private static DatabaseBrowserGate Gate(bool enabled) =>
        new(new DatabaseBrowserOptions { Enabled = enabled }, isDevelopmentEnvironment: false);

    [Test]
    public void Database_ShowsTheDisabledMessage_WhenTheGateIsDisabled()
    {
        RegisterService(Gate(enabled: false));
        var stub = new UiGatewayStub();

        var cut = Render<Database>(stub);

        Assert.Multiple(() =>
        {
            Assert.That(cut.Markup, Does.Contain("disabled in this environment"));
            Assert.That(cut.FindAll("#table"), Is.Empty, "The table picker should not render when disabled.");
        });
    }

    [Test]
    public void Database_ListsTheTables_WhenEnabled()
    {
        RegisterService(Gate(enabled: true));
        var stub = new UiGatewayStub { Tables = ["Quotes", "Policies", "DomainEvents"] };

        var cut = Render<Database>(stub);

        Assert.Multiple(() =>
        {
            // One option per table, plus the "choose a table" placeholder.
            Assert.That(cut.FindAll("#table option"), Has.Count.EqualTo(4));
            Assert.That(cut.Markup, Does.Contain("Select a table to browse its rows."));
        });
    }

    [Test]
    public void Database_RendersRows_WhenATableIsSelected()
    {
        RegisterService(Gate(enabled: true));
        var stub = new UiGatewayStub
        {
            Tables = ["Quotes"],
            TablePageResult = UiTestData.Table(
                tableName: "Quotes",
                columns: ["QuoteReference", "ProductCode"],
                rows: [["QF-PROP-01", "COMMERCIAL_PROPERTY"]],
                totalRows: 1),
        };

        var cut = Render<Database>(stub);

        cut.Find("#table").Change("Quotes");

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll("thead th"), Has.Count.EqualTo(2));
            Assert.That(cut.FindAll("tbody tr"), Has.Count.EqualTo(1));
            Assert.That(cut.Markup, Does.Contain("1 row(s) total."));
            Assert.That(stub.LastQueriedTable, Is.EqualTo("Quotes"));
            Assert.That(stub.LastQueriedSkip, Is.EqualTo(0));
        });
    }

    [Test]
    public void Database_ShowsNoData_WhenTheSelectedTableHasNoColumns()
    {
        RegisterService(Gate(enabled: true));
        var stub = new UiGatewayStub
        {
            Tables = ["Empty"],
            TablePageResult = UiTestData.Table(tableName: "Empty", columns: [], rows: [], totalRows: 0),
        };

        var cut = Render<Database>(stub);

        cut.Find("#table").Change("Empty");

        Assert.That(cut.Markup, Does.Contain("No data."));
    }
}
