using System.Text.Json;
using InsuranceIntegration.Api.IntegrationTests.Infrastructure;
using InsuranceIntegration.Api.SourceContracts.Ingest;

namespace InsuranceIntegration.Api.IntegrationTests.Api;

/// <summary>
/// Coverage that the ingest dispatcher routes a source envelope to the correct handler by its
/// <c>Type</c> discriminator. The QuoteForge path is covered in <see cref="IngestEndpointsTests"/>;
/// here the billing, claims, and compliance source families are exercised end-to-end through
/// <c>POST /api/v1/ingest</c>, asserting the receipt names the handler that processed the message.
/// </summary>
public sealed class MultiSourceIngestEndpointsTests : ApiTestBase
{
    private static readonly DateTime OccurredAt = new(2026, 1, 15, 9, 30, 0, DateTimeKind.Utc);

    private static SourceIngestEnvelope Envelope(string source, string type, string id, object data) =>
        new()
        {
            Id = id,
            Source = source,
            Type = type,
            SchemaVersion = "1.0",
            OccurredAtUtc = OccurredAt,
            CorrelationId = $"corr-{id}",
            Data = JsonSerializer.SerializeToElement(data),
        };

    [Test]
    public async Task Ingest_RoutesAnInstallmentScheduleToTheBillingHandler()
    {
        var envelope = Envelope("PAYMENTRAIL", "InstallmentSchedule", "br-ingest-001", new
        {
            policyReference = "POL-PROP-01",
            installmentCount = 4,
            totalAmount = 11_800m,
            currencyCode = "USD",
            paidToDate = 0m,
            missedPayments = 0,
        });

        using var response = await PostAsync("/api/v1/ingest", envelope);

        var receipt = await response.ShouldReturnJsonAsync();
        Assert.Multiple(() =>
        {
            Assert.That(receipt.GetProperty("source").GetString(), Is.EqualTo("PAYMENTRAIL"));
            Assert.That(receipt.GetProperty("messageType").GetString(), Is.EqualTo("InstallmentSchedule"));
            Assert.That(receipt.GetProperty("processedBy").GetString(), Is.EqualTo("BillingIngestHandler"));
        });
    }

    [Test]
    public async Task Ingest_RoutesAClaimNoticeToTheClaimHandler()
    {
        var envelope = Envelope("CLAIMFORGE", "ClaimNotice", "cl-ingest-001", new
        {
            claimReference = "CLM-001",
            policyReference = "POL-PROP-01",
            claimantName = "Northwind Storage Ltd",
            lossDate = "2026-01-10",
            lossCause = "Fire",
            estimatedIncurred = 25_000m,
            estimatedReserved = 20_000m,
            paidAmount = 0m,
            currencyCode = "USD",
        });

        using var response = await PostAsync("/api/v1/ingest", envelope);

        var receipt = await response.ShouldReturnJsonAsync();
        Assert.Multiple(() =>
        {
            Assert.That(receipt.GetProperty("source").GetString(), Is.EqualTo("CLAIMFORGE"));
            Assert.That(receipt.GetProperty("messageType").GetString(), Is.EqualTo("ClaimNotice"));
            Assert.That(receipt.GetProperty("processedBy").GetString(), Is.EqualTo("ClaimIngestHandler"));
        });
    }

    [Test]
    public async Task Ingest_RoutesAComplianceResultToTheComplianceHandler()
    {
        var envelope = Envelope("SANCTIONSCAN", "ComplianceResult", "cm-ingest-001", new
        {
            partyName = "Northwind Storage Ltd",
            entityReference = "POL-PROP-01",
            screeningResult = "Clear",
            score = 5,
            isPoliticallyExposed = false,
            hasSanctionsHit = false,
        });

        using var response = await PostAsync("/api/v1/ingest", envelope);

        var receipt = await response.ShouldReturnJsonAsync();
        Assert.Multiple(() =>
        {
            Assert.That(receipt.GetProperty("source").GetString(), Is.EqualTo("SANCTIONSCAN"));
            Assert.That(receipt.GetProperty("messageType").GetString(), Is.EqualTo("ComplianceResult"));
            Assert.That(receipt.GetProperty("processedBy").GetString(), Is.EqualTo("ComplianceIngestHandler"));
        });
    }
}
