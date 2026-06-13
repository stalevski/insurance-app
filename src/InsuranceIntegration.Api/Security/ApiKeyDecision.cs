namespace InsuranceIntegration.Api.Security;

public enum ApiKeyDecision
{
    /// <summary>The request does not require an API key.</summary>
    NotRequired,

    /// <summary>A valid API key was supplied.</summary>
    Authorized,

    /// <summary>An API key is required but was missing or invalid.</summary>
    Rejected
}
