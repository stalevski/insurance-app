using InsuranceIntegration.Api.Services.Documents;
using InsuranceIntegration.Api.Snapshots.Policies;
using Microsoft.Extensions.Time.Testing;

namespace InsuranceIntegration.Api.Tests.Documents;

public sealed class PolicyScheduleServiceTests
{
    private static readonly byte[] PdfMagic = "%PDF"u8.ToArray();

    private static PolicyScheduleService CreateService()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 6, 13, 12, 0, 0, TimeSpan.Zero));
        return new PolicyScheduleService(time);
    }

    private static PolicySnapshot CreateSnapshot()
    {
        return new PolicySnapshot
        {
            PolicyReference = "POL-1001",
            QuoteReference = "QTE-2001",
            ProductCode = "PROPERTY",
            UnderwritingYear = 2026,
            CurrencyCode = "USD",
            Insured = new PolicyParty { Code = "INS-1", Name = "Acme Manufacturing" },
            Broker = new PolicyParty { Code = "BRK-9", Name = "Beta Brokers" },
            Lifecycle = new PolicyLifecycle { PolicyStatus = "Bound", CurrentPhase = "InForce" },
            Premium = new PolicyPremium { Base = 12000m, Adjusted = 12500m },
            Coverage = new PolicyCoverage
            {
                SectionCount = 2,
                TotalSumInsured = 5_000_000m,
                TotalSectionPremium = 12500m,
                PremiumAllocationBalanced = true
            },
            Dates = new PolicyDates
            {
                InceptionDate = new DateOnly(2026, 1, 1),
                ExpiryDate = new DateOnly(2026, 12, 31),
                BoundDate = new DateOnly(2025, 12, 20)
            },
            History =
            [
                new SnapshotHistoryEntry
                {
                    AtUtc = new DateTime(2025, 12, 20, 9, 30, 0, DateTimeKind.Utc),
                    Source = "BindPoint",
                    MessageType = "PolicyBind",
                    EnvelopeId = "env-1",
                    TransactionType = "NewBusiness"
                }
            ]
        };
    }

    [Test]
    public void GenerateSchedule_ProducesPdfBytes()
    {
        var service = CreateService();

        var pdf = service.GenerateSchedule(CreateSnapshot());

        Assert.That(pdf, Is.Not.Null);
        Assert.That(pdf, Is.Not.Empty);
        Assert.That(pdf.Take(PdfMagic.Length), Is.EqualTo(PdfMagic), "PDF should start with the %PDF magic header.");
    }

    [Test]
    public void GenerateSchedule_WithMinimalSnapshot_StillProducesPdf()
    {
        var service = CreateService();
        var snapshot = new PolicySnapshot { PolicyReference = "POL-EMPTY" };

        var pdf = service.GenerateSchedule(snapshot);

        Assert.That(pdf, Is.Not.Empty);
        Assert.That(pdf.Take(PdfMagic.Length), Is.EqualTo(PdfMagic));
    }

    [Test]
    public void GenerateSchedule_NullSnapshot_Throws()
    {
        var service = CreateService();

        Assert.Throws<ArgumentNullException>(() => service.GenerateSchedule(null!));
    }
}
