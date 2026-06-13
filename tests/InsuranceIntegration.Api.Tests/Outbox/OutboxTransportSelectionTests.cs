using InsuranceIntegration.Api.Services.Outbox;

namespace InsuranceIntegration.Api.Tests.Outbox;

public sealed class OutboxTransportSelectionTests
{
    [TestCase("File", OutboxTransport.File)]
    [TestCase("file", OutboxTransport.File)]
    [TestCase("FILE", OutboxTransport.File)]
    [TestCase("Webhook", OutboxTransport.Webhook)]
    [TestCase("webhook", OutboxTransport.Webhook)]
    [TestCase("Logging", OutboxTransport.Logging)]
    [TestCase("logging", OutboxTransport.Logging)]
    [TestCase("", OutboxTransport.Logging)]
    [TestCase("   ", OutboxTransport.Logging)]
    [TestCase("nonsense", OutboxTransport.Logging)]
    [TestCase(null, OutboxTransport.Logging)]
    public void Normalize_MapsToKnownTransportOrLoggingFallback(string? input, string expected)
    {
        var result = OutboxTransport.Normalize(input);

        Assert.That(result, Is.EqualTo(expected));
    }
}
