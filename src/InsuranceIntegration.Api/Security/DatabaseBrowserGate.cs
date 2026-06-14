namespace InsuranceIntegration.Api.Security;

/// <summary>
/// Pure decision logic for whether the read-only database browser page is exposed. Resolves an
/// explicit <see cref="DatabaseBrowserOptions.Enabled"/> override, falling back to "enabled only in
/// Development". Kept free of <c>HttpContext</c> / <c>IHostEnvironment</c> so it can be unit-tested
/// in isolation.
/// </summary>
public sealed class DatabaseBrowserGate
{
    public DatabaseBrowserGate(DatabaseBrowserOptions options, bool isDevelopmentEnvironment)
    {
        IsEnabled = options.Enabled ?? isDevelopmentEnvironment;
    }

    /// <summary>Whether the read-only database browser page (<c>/database</c>) is available.</summary>
    public bool IsEnabled { get; }

    /// <summary>True when the request path targets the database browser page or a sub-path.</summary>
    public static bool IsDatabaseBrowserPath(string? path)
    {
        return !string.IsNullOrEmpty(path)
            && path.StartsWith("/database", StringComparison.OrdinalIgnoreCase);
    }
}
