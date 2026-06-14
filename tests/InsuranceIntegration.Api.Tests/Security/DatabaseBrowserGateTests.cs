using InsuranceIntegration.Api.Security;

namespace InsuranceIntegration.Api.Tests.Security;

public sealed class DatabaseBrowserGateTests
{
    private static DatabaseBrowserGate Gate(bool? enabled, bool isDevelopment) =>
        new(new DatabaseBrowserOptions { Enabled = enabled }, isDevelopment);

    [Test]
    public void IsEnabled_DefaultsToDevelopmentEnvironment_WhenNotConfigured()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Gate(enabled: null, isDevelopment: true).IsEnabled, Is.True);
            Assert.That(Gate(enabled: null, isDevelopment: false).IsEnabled, Is.False);
        });
    }

    [Test]
    public void IsEnabled_TrueWhenForcedOn_RegardlessOfEnvironment()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Gate(enabled: true, isDevelopment: false).IsEnabled, Is.True);
            Assert.That(Gate(enabled: true, isDevelopment: true).IsEnabled, Is.True);
        });
    }

    [Test]
    public void IsEnabled_FalseWhenForcedOff_RegardlessOfEnvironment()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Gate(enabled: false, isDevelopment: true).IsEnabled, Is.False);
            Assert.That(Gate(enabled: false, isDevelopment: false).IsEnabled, Is.False);
        });
    }

    [TestCase("/database", true)]
    [TestCase("/database/", true)]
    [TestCase("/Database", true)]
    [TestCase("/DATABASE/tables", true)]
    [TestCase("/databases", true)]
    [TestCase("/api/v1/policies", false)]
    [TestCase("/", false)]
    [TestCase("", false)]
    [TestCase(null, false)]
    public void IsDatabaseBrowserPath_MatchesDatabasePathsCaseInsensitively(string? path, bool expected)
    {
        Assert.That(DatabaseBrowserGate.IsDatabaseBrowserPath(path), Is.EqualTo(expected));
    }
}
