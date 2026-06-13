using InsuranceIntegration.Api.Security;

namespace InsuranceIntegration.Api.Tests.Security;

public sealed class ApiKeyValidatorTests
{
    private static ApiKeyValidator WithKeys(params string[] keys) =>
        new(new ApiKeyOptions { Keys = keys });

    private static ApiKeyValidator WithOptions(ApiKeyOptions options) => new(options);

    [Test]
    public void IsEnabled_FalseWhenNoKeysConfigured()
    {
        var validator = WithOptions(new ApiKeyOptions());

        Assert.That(validator.IsEnabled, Is.False);
    }

    [Test]
    public void IsEnabled_FalseWhenOnlyBlankKeys()
    {
        var validator = WithKeys("", "   ");

        Assert.That(validator.IsEnabled, Is.False);
    }

    [Test]
    public void IsEnabled_TrueWhenKeyConfigured()
    {
        var validator = WithKeys("secret");

        Assert.That(validator.IsEnabled, Is.True);
    }

    [TestCase("POST")]
    [TestCase("PUT")]
    [TestCase("PATCH")]
    [TestCase("DELETE")]
    [TestCase("post")]
    public void RequiresApiKey_TrueForMutatingMethods_WhenEnabled(string method)
    {
        var validator = WithKeys("secret");

        Assert.That(validator.RequiresApiKey(method, "/api/v1/claims/transitions"), Is.True);
    }

    [Test]
    public void RequiresApiKey_FalseForGet_OnNormalPath()
    {
        var validator = WithKeys("secret");

        Assert.That(validator.RequiresApiKey("GET", "/api/v1/policies"), Is.False);
    }

    [Test]
    public void RequiresApiKey_FalseForEverything_WhenDisabled()
    {
        var validator = WithOptions(new ApiKeyOptions());

        Assert.Multiple(() =>
        {
            Assert.That(validator.RequiresApiKey("POST", "/api/v1/claims/transitions"), Is.False);
            Assert.That(validator.RequiresApiKey("GET", "/database"), Is.False);
        });
    }

    [Test]
    public void RequiresApiKey_TrueForDatabaseBrowserGet_WhenProtected()
    {
        var validator = WithKeys("secret");

        Assert.That(validator.RequiresApiKey("GET", "/database"), Is.True);
    }

    [Test]
    public void RequiresApiKey_FalseForDatabaseBrowser_WhenProtectionDisabled()
    {
        var validator = WithOptions(new ApiKeyOptions
        {
            Keys = ["secret"],
            ProtectDatabaseBrowser = false
        });

        Assert.That(validator.RequiresApiKey("GET", "/database"), Is.False);
    }

    [Test]
    public void Evaluate_NotRequired_ForUnprotectedRequest()
    {
        var validator = WithKeys("secret");

        Assert.That(validator.Evaluate("GET", "/api/v1/policies", null), Is.EqualTo(ApiKeyDecision.NotRequired));
    }

    [Test]
    public void Evaluate_Authorized_WithCorrectKey()
    {
        var validator = WithKeys("secret", "backup-key");

        Assert.That(validator.Evaluate("POST", "/api/v1/claims/transitions", "backup-key"), Is.EqualTo(ApiKeyDecision.Authorized));
    }

    [Test]
    public void Evaluate_Rejected_WithWrongKey()
    {
        var validator = WithKeys("secret");

        Assert.That(validator.Evaluate("POST", "/api/v1/claims/transitions", "nope"), Is.EqualTo(ApiKeyDecision.Rejected));
    }

    [Test]
    public void Evaluate_Rejected_WithMissingKey()
    {
        var validator = WithKeys("secret");

        Assert.That(validator.Evaluate("POST", "/api/v1/claims/transitions", null), Is.EqualTo(ApiKeyDecision.Rejected));
    }

    [Test]
    public void Evaluate_Rejected_ForProtectedDatabaseBrowser_WithoutKey()
    {
        var validator = WithKeys("secret");

        Assert.That(validator.Evaluate("GET", "/database", null), Is.EqualTo(ApiKeyDecision.Rejected));
    }

    [Test]
    public void Evaluate_Authorized_ForProtectedDatabaseBrowser_WithKey()
    {
        var validator = WithKeys("secret");

        Assert.That(validator.Evaluate("GET", "/database", "secret"), Is.EqualTo(ApiKeyDecision.Authorized));
    }

    [Test]
    public void HeaderName_DefaultsToXApiKey()
    {
        var validator = WithKeys("secret");

        Assert.That(validator.HeaderName, Is.EqualTo("X-Api-Key"));
    }

    [Test]
    public void HeaderName_RespectsConfiguredValue()
    {
        var validator = WithOptions(new ApiKeyOptions
        {
            Keys = ["secret"],
            HeaderName = "X-Custom-Key"
        });

        Assert.That(validator.HeaderName, Is.EqualTo("X-Custom-Key"));
    }
}
