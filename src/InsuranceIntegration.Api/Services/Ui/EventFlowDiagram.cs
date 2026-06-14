using InsuranceIntegration.Api.Persistence;

namespace InsuranceIntegration.Api.Services.Ui;

/// <summary>
/// Builds a Mermaid <c>flowchart</c> definition from an ordered domain-event stream so the
/// lifecycle of a quote or policy can be visualised in the UI.
/// </summary>
public static class EventFlowDiagram
{
    public static string Build(IReadOnlyList<DomainEventEntity> events)
    {
        if (events.Count == 0)
        {
            return string.Empty;
        }

        var ordered = events
            .OrderBy(record => record.OccurredAtUtc)
            .ThenBy(record => record.RecordedAtUtc)
            .ToList();

        var builder = new System.Text.StringBuilder();
        builder.Append("flowchart LR\n");

        for (var i = 0; i < ordered.Count; i++)
        {
            var label = Sanitize(ordered[i].EventType);
            builder.Append("    n").Append(i).Append("[\"").Append(label).Append("\"]\n");
        }

        for (var i = 0; i < ordered.Count - 1; i++)
        {
            builder.Append("    n").Append(i).Append(" --> n").Append(i + 1).Append('\n');
        }

        builder.Append("    class n").Append(ordered.Count - 1).Append(" current;\n");
        builder.Append("    classDef current fill:#dbeafe,stroke:#2563eb,stroke-width:2px,color:#1e3a8a;\n");

        return builder.ToString();
    }

    private static string Sanitize(string value)
    {
        return value.Replace("\"", string.Empty, StringComparison.Ordinal);
    }
}
