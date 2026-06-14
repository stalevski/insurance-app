namespace InsuranceIntegration.Api.Security;

/// <summary>
/// Configuration for the read-only database browser page (<c>/database</c>). Bound from the
/// <c>DatabaseBrowser</c> configuration section. The browser exposes raw table data, so it is
/// gated: by default it is available only in the Development environment.
/// </summary>
public sealed class DatabaseBrowserOptions
{
    public const string SectionName = "DatabaseBrowser";

    /// <summary>
    /// Explicit on/off switch. When <c>null</c> (the default), the browser is enabled only in the
    /// Development environment. Set <c>true</c> to expose it elsewhere (not recommended without a
    /// reverse-proxy auth/allowlist), or <c>false</c> to force it off even in Development.
    /// </summary>
    public bool? Enabled { get; set; }
}
