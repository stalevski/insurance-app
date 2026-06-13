namespace InsuranceIntegration.Api.Services.Outbox;

/// <summary>Known outbox transport identifiers accepted by <see cref="OutboxOptions.Transport"/>.</summary>
public static class OutboxTransport
{
    public const string Logging = "Logging";
    public const string File = "File";
    public const string Webhook = "Webhook";

    /// <summary>
    /// Maps a configured transport value to a known identifier (case-insensitive). Unknown or blank
    /// values fall back to <see cref="Logging"/> so a misconfiguration never stops the app from running.
    /// </summary>
    public static string Normalize(string? transport)
    {
        if (string.Equals(transport, File, StringComparison.OrdinalIgnoreCase))
        {
            return File;
        }

        if (string.Equals(transport, Webhook, StringComparison.OrdinalIgnoreCase))
        {
            return Webhook;
        }

        return Logging;
    }
}
