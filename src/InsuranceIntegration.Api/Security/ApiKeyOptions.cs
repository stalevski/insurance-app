namespace InsuranceIntegration.Api.Security;

/// <summary>
/// Configuration for API-key gating of mutating HTTP requests and the database browser page.
/// Bound from the <c>ApiKey</c> configuration section. When no keys are configured, enforcement
/// is disabled so local development and tests run without credentials.
/// </summary>
public sealed class ApiKeyOptions
{
    public const string SectionName = "ApiKey";

    /// <summary>Header carrying the API key.</summary>
    public string HeaderName { get; set; } = "X-Api-Key";

    /// <summary>Accepted API keys. When empty, API-key enforcement is disabled.</summary>
    public IList<string> Keys { get; set; } = [];

    /// <summary>
    /// When true, the database browser page (<c>/database</c>) also requires a valid key.
    /// </summary>
    public bool ProtectDatabaseBrowser { get; set; } = true;
}
