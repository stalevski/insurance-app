using Bunit;
using InsuranceIntegration.Api.Components.Pages;

namespace InsuranceIntegration.Api.IntegrationTests.Ui;

/// <summary>
/// bUnit coverage for the manual ingest page (<c>/ingest</c>): the source-system template dropdown,
/// pre-filling the editor from a template, a successful submission rendering the receipt, and the
/// invalid-JSON guard never reaching the gateway.
/// </summary>
public sealed class IngestPageTests : UiPageTestBase
{
    [Test]
    public void Ingest_ListsTheSourceSystemTemplates()
    {
        var stub = new UiGatewayStub
        {
            SourceSystems =
            [
                UiTestData.SourceSystem(systemCode: "QUOTEFORGE", displayName: "QuoteForge", messageType: "QuoteRequest"),
                UiTestData.SourceSystem(systemCode: "PAYMENTRAIL", displayName: "PaymentRail", messageType: "InstallmentSchedule"),
            ],
        };

        var cut = Render<Ingest>(stub);

        Assert.Multiple(() =>
        {
            // One option per source system, plus the "choose a source system" placeholder.
            Assert.That(cut.FindAll("#template option"), Has.Count.EqualTo(3));
            Assert.That(cut.Markup, Does.Contain("QuoteForge (QuoteRequest)"));
            Assert.That(cut.Markup, Does.Contain("PaymentRail (InstallmentSchedule)"));
        });
    }

    [Test]
    public void Ingest_PrefillsTheEditor_WhenATemplateIsChosen()
    {
        var stub = new UiGatewayStub
        {
            SourceSystems =
            [
                UiTestData.SourceSystem(
                    systemCode: "QUOTEFORGE",
                    messageType: "QuoteRequest",
                    examplePayload: new { quoteReference = "QT-1" }),
            ],
        };

        var cut = Render<Ingest>(stub);

        cut.Find("#template").Change("QUOTEFORGE");

        var editor = cut.Find("#envelope").GetAttribute("value") ?? string.Empty;
        Assert.Multiple(() =>
        {
            Assert.That(editor, Does.Contain("QUOTEFORGE"));
            Assert.That(editor, Does.Contain("QuoteRequest"));
            Assert.That(editor, Does.Contain("quoteReference"), "The template's example payload should be embedded.");
        });
    }

    [Test]
    public void Ingest_ShowsAReceipt_WhenAValidEnvelopeIsSubmitted()
    {
        var stub = new UiGatewayStub
        {
            DispatchReceipt = UiTestData.Receipt(
                source: "QUOTEFORGE",
                envelopeId: "ui-xyz",
                processedBy: "QuoteForgeIngestHandler"),
        };

        var cut = Render<Ingest>(stub);

        const string envelopeJson = """
            {"id":"ui-xyz","source":"QUOTEFORGE","type":"QuoteRequest","schemaVersion":"1.0","occurredAtUtc":"2026-01-15T09:30:00Z","data":{"quoteReference":"QT-1"}}
            """;
        cut.Find("#envelope").Change(envelopeJson);
        cut.Find("button:not(.secondary)").Click();

        Assert.Multiple(() =>
        {
            var success = cut.Find(".alert.success").TextContent;
            Assert.That(success, Does.Contain("QuoteForgeIngestHandler"));
            Assert.That(success, Does.Contain("ui-xyz"));
            Assert.That(stub.LastDispatchedEnvelope, Is.Not.Null);
            Assert.That(stub.LastDispatchedEnvelope!.Source, Is.EqualTo("QUOTEFORGE"));
        });
    }

    [Test]
    public void Ingest_ShowsAnError_AndSkipsTheGateway_ForInvalidJson()
    {
        var stub = new UiGatewayStub();

        var cut = Render<Ingest>(stub);

        cut.Find("#envelope").Change("{ this is not valid json");
        cut.Find("button:not(.secondary)").Click();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".alert.error").TextContent, Does.Contain("Invalid JSON"));
            Assert.That(stub.LastDispatchedEnvelope, Is.Null, "A malformed envelope must never reach the gateway.");
        });
    }
}
