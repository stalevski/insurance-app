namespace InsuranceIntegration.Api.Services.Outbox;

/// <summary>
/// Selects and configures the outbox transport. Bound from the <c>Outbox</c> configuration section.
/// Defaults to the logging transport so the app runs with no external infrastructure.
/// </summary>
public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    /// <summary>
    /// Transport to deliver outbox events with: <c>Logging</c> (default), <c>File</c>, or
    /// <c>Webhook</c>.
    /// </summary>
    public string Transport { get; set; } = OutboxTransport.Logging;

    /// <summary>Destination file for the <c>File</c> transport (JSON-lines, appended).</summary>
    public string FilePath { get; set; } = "outbox-events.jsonl";

    /// <summary>Destination URL for the <c>Webhook</c> transport (events POSTed as JSON).</summary>
    public string? WebhookUrl { get; set; }
}
