using System.Text;

namespace InsuranceIntegration.Api.Services.Ui;

/// <summary>
/// Builds a Mermaid <c>flowchart</c> that renders the canonical lifecycle stages for a quote
/// or policy as a left-to-right track, highlighting where the aggregate currently sits.
/// Completed stages are green, the current stage is blue, and pending stages are muted.
/// </summary>
public static class LifecycleStageDiagram
{
    private static readonly string[] QuoteStages = ["Submitted", "Indicative", "Quoted", "ReadyToBind", "Bound"];

    private static readonly string[] PolicyStages = ["Submitted", "Quoted", "ReadyToBind", "Bound"];

    public static string BuildForQuote(string currentPhase)
    {
        return Build(QuoteStages, currentPhase);
    }

    public static string BuildForPolicy(string currentPhase)
    {
        // Post-bind states extend the track so the diagram can show where a live policy sits.
        var stages = currentPhase switch
        {
            "Endorsed" => PolicyStages.Append("Endorsed").ToArray(),
            "Renewed" => PolicyStages.Append("Renewed").ToArray(),
            "Cancelled" => PolicyStages.Append("Cancelled").ToArray(),
            "Reinstated" => PolicyStages.Append("Cancelled").Append("Reinstated").ToArray(),
            "Lapsed" => PolicyStages.Append("Lapsed").ToArray(),
            "NonRenewed" => PolicyStages.Append("NonRenewed").ToArray(),
            _ => PolicyStages,
        };

        return Build(stages, currentPhase);
    }

    private static string Build(IReadOnlyList<string> stages, string currentPhase)
    {
        var orderedStages = stages.ToList();

        // Surface off-track phases (e.g. a blocked quote) as their own node at the end.
        var currentIndex = orderedStages.FindIndex(stage => string.Equals(stage, currentPhase, StringComparison.OrdinalIgnoreCase));
        if (currentIndex < 0 && !string.IsNullOrWhiteSpace(currentPhase))
        {
            orderedStages.Add(currentPhase);
            currentIndex = orderedStages.Count - 1;
        }

        var builder = new StringBuilder();
        builder.Append("flowchart LR\n");

        for (var i = 0; i < orderedStages.Count; i++)
        {
            builder.Append("    s").Append(i).Append("[\"").Append(Sanitize(orderedStages[i])).Append("\"]\n");
        }

        for (var i = 0; i < orderedStages.Count - 1; i++)
        {
            builder.Append("    s").Append(i).Append(" --> s").Append(i + 1).Append('\n');
        }

        for (var i = 0; i < orderedStages.Count; i++)
        {
            if (i < currentIndex)
            {
                builder.Append("    class s").Append(i).Append(" done;\n");
            }
            else if (i == currentIndex)
            {
                builder.Append("    class s").Append(i).Append(" current;\n");
            }
            else
            {
                builder.Append("    class s").Append(i).Append(" pending;\n");
            }
        }

        builder.Append("    classDef done fill:#dcfce7,stroke:#16a34a,color:#166534;\n");
        builder.Append("    classDef current fill:#dbeafe,stroke:#2563eb,stroke-width:2px,color:#1e3a8a;\n");
        builder.Append("    classDef pending fill:#f1f5f9,stroke:#cbd5e1,color:#64748b;\n");

        return builder.ToString();
    }

    private static string Sanitize(string value)
    {
        return value.Replace("\"", string.Empty, StringComparison.Ordinal);
    }
}
