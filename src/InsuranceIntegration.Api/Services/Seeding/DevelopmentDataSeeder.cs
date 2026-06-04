using System.Text.Json;
using InsuranceIntegration.Api.Persistence;
using InsuranceIntegration.Api.SourceContracts.Ingest;
using Microsoft.EntityFrameworkCore;

namespace InsuranceIntegration.Api.Services.Seeding;

public sealed class DevelopmentDataSeeder : IDevelopmentDataSeeder
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly IReadOnlyList<SeedProduct> Products =
    [
        new(
            "COMMERCIAL_PROPERTY",
            "PROP",
            [
                new("Northwind Storage Ltd", 1_500_000m, 25, 8, 11_800m),
                new("Harbourview Cold Chain", 3_200_000m, 60, 14, 18_400m),
                new("Granite Peak Logistics", 2_100_000m, 40, 10, 13_250m),
                new("Maple District Retail Group", 5_400_000m, 120, 22, 27_900m),
                new("Riverside Manufacturing Co", 8_700_000m, 210, 31, 41_600m),
                new("Beacon Hospitality Estates", 4_050_000m, 95, 17, 22_300m),
                new("Copperfield Warehousing", 1_950_000m, 33, 6, 12_700m),
            ]),
        new(
            "LIABILITY",
            "LIAB",
            [
                new("Harborline Services", 2_200_000m, 40, 8, 8_450m),
                new("Summit Facilities Management", 3_600_000m, 75, 12, 11_900m),
                new("Cedar & Wells Consulting", 1_100_000m, 18, 9, 6_300m),
                new("Irongate Security Group", 4_800_000m, 130, 16, 15_700m),
                new("Brightway Cleaning Partners", 950_000m, 60, 7, 5_900m),
                new("Pinnacle Events Co", 1_750_000m, 28, 5, 7_650m),
                new("Atlas Property Care", 2_900_000m, 54, 11, 9_800m),
            ]),
        new(
            "CYBER",
            "CYB",
            [
                new("Lumen Digital Health", 6_300_000m, 140, 9, 28_900m),
                new("Quanta Payments Inc", 12_500_000m, 320, 11, 54_200m),
                new("Northstar SaaS Labs", 3_400_000m, 70, 6, 18_700m),
                new("Vector Analytics Group", 4_900_000m, 95, 8, 23_400m),
                new("BlueOrbit Commerce", 7_800_000m, 180, 10, 33_100m),
                new("Helix Cloud Systems", 2_600_000m, 48, 5, 16_200m),
                new("Meridian Fintech", 9_100_000m, 240, 13, 42_800m),
            ]),
        new(
            "MOTOR",
            "MOT",
            [
                new("Citywide Courier Fleet", 2_800_000m, 65, 12, 14_600m),
                new("Redline Haulage Ltd", 5_900_000m, 140, 19, 26_800m),
                new("Greenfield Bus Lines", 4_200_000m, 110, 24, 21_300m),
                new("Apex Equipment Rentals", 3_100_000m, 58, 9, 16_900m),
                new("Coastal Distribution Co", 6_400_000m, 160, 15, 29_700m),
                new("Sterling Taxi Cooperative", 1_400_000m, 90, 28, 12_100m),
                new("Vanguard Logistics", 7_700_000m, 190, 17, 34_500m),
            ]),
    ];

    private static readonly IReadOnlyList<SeedBroker> Brokers =
    [
        new("BRK-044", "Summit Risk Partners"),
        new("BRK-071", "Harbor Broking"),
        new("BRK-108", "Meridian Insurance Brokers"),
        new("BRK-126", "Keystone Placement Group"),
    ];

    private readonly IntegrationDbContext _context;
    private readonly Ingest.IIngestDispatcher _dispatcher;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DevelopmentDataSeeder> _logger;

    public DevelopmentDataSeeder(
        IntegrationDbContext context,
        Ingest.IIngestDispatcher dispatcher,
        TimeProvider timeProvider,
        ILogger<DevelopmentDataSeeder> logger)
    {
        _context = context;
        _dispatcher = dispatcher;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _context.QuoteSnapshots.AnyAsync(cancellationToken))
        {
            return;
        }

        var underwritingYear = _timeProvider.GetUtcNow().Year;
        var quotes = 0;
        var policies = 0;

        foreach (var product in Products)
        {
            for (var i = 0; i < product.Insureds.Count; i++)
            {
                var insured = product.Insureds[i];
                var broker = Brokers[i % Brokers.Count];
                var quoteReference = $"QF-{product.Abbreviation}-{i + 1:D2}";
                var effective = new DateOnly(underwritingYear, ((i * 2) % 12) + 1, 1);
                var expiry = effective.AddYears(1).AddDays(-1);
                var technicalPremium = insured.BasePremium;
                var brokerPremium = decimal.Round(technicalPremium * 1.03m, 2, MidpointRounding.AwayFromZero);

                await DispatchAsync(
                    "QUOTEFORGE",
                    "QuoteRequest",
                    $"seed-quote-{quoteReference}",
                    new
                    {
                        quoteReference,
                        insuredName = insured.Name,
                        productLine = product.ProductCode,
                        brokerCode = broker.Code,
                        brokerName = broker.Name,
                        technicalPremium,
                        brokerPremium,
                        currencyCode = "USD",
                        effectiveDate = effective.ToString("yyyy-MM-dd"),
                        expiryDate = expiry.ToString("yyyy-MM-dd"),
                        underwritingYear,
                        insuredRevenue = insured.Revenue,
                        insuredEmployeeCount = insured.EmployeeCount,
                        insuredYearsInBusiness = insured.YearsInBusiness,
                    },
                    cancellationToken);
                quotes++;

                // Bind roughly the first third of each family into issued policies so the
                // UI shows a mix of "Quoted" and "Bound" lifecycle stages.
                if (i < 2)
                {
                    var policyReference = $"POL-{product.Abbreviation}-{i + 1:D2}";
                    await DispatchAsync(
                        "BINDPOINT",
                        "PolicyBindRequest",
                        $"seed-bind-{policyReference}",
                        new
                        {
                            policyReference,
                            quoteReference,
                            insuredName = insured.Name,
                            productCode = product.ProductCode,
                            brokerCode = broker.Code,
                            brokerName = broker.Name,
                            brokerHasDelegatedAuthority = false,
                            brokerIsPreferredPartner = true,
                            boundPremium = brokerPremium,
                            currencyCode = "USD",
                            inceptionDate = effective.ToString("yyyy-MM-dd"),
                            expiryDate = expiry.ToString("yyyy-MM-dd"),
                            boundDate = effective.AddDays(-3).ToString("yyyy-MM-dd"),
                            paymentMethod = "Invoice",
                            installmentCount = 4,
                        },
                        cancellationToken);
                    policies++;
                }
            }
        }

        _logger.LogInformation("Seeded {QuoteCount} quotes and {PolicyCount} policies across {ProductCount} product families.", quotes, policies, Products.Count);
    }

    private async Task DispatchAsync(string source, string type, string envelopeId, object payload, CancellationToken cancellationToken)
    {
        var envelope = new SourceIngestEnvelope
        {
            Id = envelopeId,
            Source = source,
            Type = type,
            SchemaVersion = "1.0",
            OccurredAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            CorrelationId = Guid.NewGuid().ToString(),
            Data = JsonSerializer.SerializeToElement(payload, JsonOptions),
        };

        await _dispatcher.DispatchAsync(envelope, cancellationToken);
    }

    private sealed record SeedProduct(string ProductCode, string Abbreviation, IReadOnlyList<SeedInsured> Insureds);

    private sealed record SeedInsured(string Name, decimal Revenue, int EmployeeCount, int YearsInBusiness, decimal BasePremium);

    private sealed record SeedBroker(string Code, string Name);
}
