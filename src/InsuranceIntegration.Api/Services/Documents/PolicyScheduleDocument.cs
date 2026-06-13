using InsuranceIntegration.Api.Snapshots.Policies;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace InsuranceIntegration.Api.Services.Documents;

/// <summary>
/// QuestPDF document that renders a one-page policy schedule from a <see cref="PolicySnapshot"/>.
/// </summary>
public sealed class PolicyScheduleDocument : IDocument
{
    private readonly PolicySnapshot _snapshot;
    private readonly DateTime _generatedAtUtc;

    public PolicyScheduleDocument(PolicySnapshot snapshot, DateTime generatedAtUtc)
    {
        _snapshot = snapshot;
        _generatedAtUtc = generatedAtUtc;
    }

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(36);
            page.DefaultTextStyle(text => text.FontSize(10).FontColor(Colors.Grey.Darken3));

            page.Header().Element(ComposeHeader);
            page.Content().PaddingVertical(12).Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Text("Policy Schedule").FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
            column.Item().Text($"Policy {_snapshot.PolicyReference}").FontSize(12).FontColor(Colors.Grey.Darken1);
            column.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(14);

            column.Item().Element(c => Section(c, "Policy", BuildPolicyRows()));
            column.Item().Element(c => Section(c, "Parties", BuildPartyRows()));
            column.Item().Element(c => Section(c, "Cover period", BuildDateRows()));
            column.Item().Element(c => Section(c, "Premium", BuildPremiumRows()));
            column.Item().Element(c => Section(c, "Coverage", BuildCoverageRows()));

            if (_snapshot.History.Count > 0)
            {
                column.Item().Element(ComposeHistory);
            }
        });
    }

    private static void Section(IContainer container, string title, IReadOnlyList<(string Label, string Value)> rows)
    {
        container.Column(column =>
        {
            column.Item().Text(title).FontSize(12).SemiBold().FontColor(Colors.Blue.Darken1);
            column.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(150);
                    columns.RelativeColumn();
                });

                foreach (var (label, value) in rows)
                {
                    table.Cell().PaddingVertical(2).Text(label).FontColor(Colors.Grey.Darken1);
                    table.Cell().PaddingVertical(2).Text(value);
                }
            });
        });
    }

    private void ComposeHistory(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Text("History").FontSize(12).SemiBold().FontColor(Colors.Blue.Darken1);
            column.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(120);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    header.Cell().Text("When (UTC)").SemiBold();
                    header.Cell().Text("Transaction").SemiBold();
                    header.Cell().Text("Source").SemiBold();
                });

                foreach (var entry in _snapshot.History)
                {
                    table.Cell().PaddingVertical(2).Text(entry.AtUtc.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
                    table.Cell().PaddingVertical(2).Text(Display(entry.TransactionType));
                    table.Cell().PaddingVertical(2).Text(Display(entry.Source));
                }
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
            column.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text($"Generated {_generatedAtUtc:yyyy-MM-dd HH:mm} UTC").FontSize(8).FontColor(Colors.Grey.Medium);
                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.DefaultTextStyle(s => s.FontSize(8).FontColor(Colors.Grey.Medium));
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        });
    }

    private IReadOnlyList<(string, string)> BuildPolicyRows() =>
    [
        ("Policy reference", Display(_snapshot.PolicyReference)),
        ("Quote reference", Display(_snapshot.QuoteReference)),
        ("Product", Display(_snapshot.ProductCode)),
        ("Underwriting year", _snapshot.UnderwritingYear.ToString(CultureInfo.InvariantCulture)),
        ("Status", Display(_snapshot.Lifecycle.PolicyStatus)),
        ("Phase", Display(_snapshot.Lifecycle.CurrentPhase))
    ];

    private IReadOnlyList<(string, string)> BuildPartyRows() =>
    [
        ("Insured", Display(_snapshot.Insured.Name)),
        ("Insured code", Display(_snapshot.Insured.Code)),
        ("Broker", Display(_snapshot.Broker.Name)),
        ("Broker code", Display(_snapshot.Broker.Code))
    ];

    private IReadOnlyList<(string, string)> BuildDateRows() =>
    [
        ("Inception", Display(_snapshot.Dates.InceptionDate)),
        ("Expiry", Display(_snapshot.Dates.ExpiryDate)),
        ("Bound", Display(_snapshot.Dates.BoundDate))
    ];

    private IReadOnlyList<(string, string)> BuildPremiumRows() =>
    [
        ("Base premium", Money(_snapshot.Premium.Base)),
        ("Adjusted premium", Money(_snapshot.Premium.Adjusted))
    ];

    private IReadOnlyList<(string, string)> BuildCoverageRows() =>
    [
        ("Sections", _snapshot.Coverage.SectionCount.ToString(CultureInfo.InvariantCulture)),
        ("Total sum insured", Money(_snapshot.Coverage.TotalSumInsured)),
        ("Total section premium", Money(_snapshot.Coverage.TotalSectionPremium)),
        ("Allocation balanced", _snapshot.Coverage.PremiumAllocationBalanced ? "Yes" : "No")
    ];

    private string Money(decimal? amount) =>
        amount.HasValue ? $"{_snapshot.CurrencyCode} {amount.Value.ToString("N2", CultureInfo.InvariantCulture)}" : "—";

    private static string Display(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "—" : value;

    private static string Display(DateOnly? value) =>
        value.HasValue ? value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "—";
}
