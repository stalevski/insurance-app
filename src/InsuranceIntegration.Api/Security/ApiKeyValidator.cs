using System.Security.Cryptography;
using System.Text;

namespace InsuranceIntegration.Api.Security;

/// <summary>
/// Pure decision logic for API-key gating. Determines whether a given request (by HTTP method and
/// path) must present an API key, and whether a supplied key is valid. Kept free of
/// <c>HttpContext</c> so it can be unit-tested in isolation.
/// </summary>
public sealed class ApiKeyValidator
{
    private static readonly HashSet<string> MutatingMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Post,
        HttpMethods.Put,
        HttpMethods.Patch,
        HttpMethods.Delete
    };

    private readonly ApiKeyOptions _options;

    public ApiKeyValidator(ApiKeyOptions options)
    {
        _options = options;
    }

    /// <summary>Enforcement is active only when at least one key is configured.</summary>
    public bool IsEnabled => _options.Keys.Any(key => !string.IsNullOrWhiteSpace(key));

    public string HeaderName => _options.HeaderName;

    /// <summary>
    /// A request requires a key when enforcement is enabled and it either mutates state
    /// (POST/PUT/PATCH/DELETE) or targets the protected database browser page.
    /// </summary>
    public bool RequiresApiKey(string method, string? path)
    {
        if (!IsEnabled)
        {
            return false;
        }

        if (MutatingMethods.Contains(method))
        {
            return true;
        }

        return _options.ProtectDatabaseBrowser && IsDatabaseBrowserPath(path);
    }

    public ApiKeyDecision Evaluate(string method, string? path, string? suppliedKey)
    {
        if (!RequiresApiKey(method, path))
        {
            return ApiKeyDecision.NotRequired;
        }

        return IsValidKey(suppliedKey) ? ApiKeyDecision.Authorized : ApiKeyDecision.Rejected;
    }

    private bool IsValidKey(string? suppliedKey)
    {
        if (string.IsNullOrWhiteSpace(suppliedKey))
        {
            return false;
        }

        var supplied = Encoding.UTF8.GetBytes(suppliedKey);
        var matched = false;

        // Compare against every configured key using a fixed-time comparison so a valid key is not
        // distinguishable from an invalid one by timing. Iterate all keys (no early exit).
        foreach (var configured in _options.Keys)
        {
            if (string.IsNullOrWhiteSpace(configured))
            {
                continue;
            }

            var candidate = Encoding.UTF8.GetBytes(configured);
            if (CryptographicOperations.FixedTimeEquals(supplied, candidate))
            {
                matched = true;
            }
        }

        return matched;
    }

    private static bool IsDatabaseBrowserPath(string? path)
    {
        return !string.IsNullOrEmpty(path)
            && path.StartsWith("/database", StringComparison.OrdinalIgnoreCase);
    }
}
