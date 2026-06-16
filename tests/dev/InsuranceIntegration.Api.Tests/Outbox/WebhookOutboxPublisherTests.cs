using System.Net;
using InsuranceIntegration.Api.Persistence;
using InsuranceIntegration.Api.Services.Outbox;
using Microsoft.Extensions.Logging.Abstractions;

namespace InsuranceIntegration.Api.Tests.Outbox;

public sealed class WebhookOutboxPublisherTests
{
    [Test]
    public async Task PublishAsync_PostsEnvelopeToConfiguredUrl()
    {
        var handler = new StubHandler(HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler);
        var options = new OutboxOptions
        {
            Transport = OutboxTransport.Webhook,
            WebhookUrl = "https://consumer.example/outbox"
        };
        var publisher = new WebhookOutboxPublisher(httpClient, options, NullLogger<WebhookOutboxPublisher>.Instance);

        await publisher.PublishAsync(CreateMessage());

        Assert.Multiple(() =>
        {
            Assert.That(handler.LastRequestUri, Is.EqualTo(new Uri("https://consumer.example/outbox")));
            Assert.That(handler.LastContent, Does.Contain("PolicyBound"));
        });
    }

    [Test]
    public void PublishAsync_WhenResponseIsNotSuccess_Throws()
    {
        var handler = new StubHandler(HttpStatusCode.InternalServerError);
        using var httpClient = new HttpClient(handler);
        var options = new OutboxOptions { WebhookUrl = "https://consumer.example/outbox" };
        var publisher = new WebhookOutboxPublisher(httpClient, options, NullLogger<WebhookOutboxPublisher>.Instance);

        Assert.ThrowsAsync<HttpRequestException>(() => publisher.PublishAsync(CreateMessage()));
    }

    [Test]
    public void PublishAsync_WhenUrlIsMissing_Throws()
    {
        var handler = new StubHandler(HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler);
        var options = new OutboxOptions { WebhookUrl = null };
        var publisher = new WebhookOutboxPublisher(httpClient, options, NullLogger<WebhookOutboxPublisher>.Instance);

        Assert.ThrowsAsync<InvalidOperationException>(() => publisher.PublishAsync(CreateMessage()));
    }

    private static OutboxMessageEntity CreateMessage() => new()
    {
        EventId = Guid.CreateVersion7(),
        AggregateType = "Policy",
        AggregateId = Guid.CreateVersion7(),
        EventType = "PolicyBound",
        PayloadJson = "{\"reference\":\"POL-1\"}",
        OccurredAtUtc = DateTime.UtcNow
    };

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;

        public StubHandler(HttpStatusCode statusCode)
        {
            _statusCode = statusCode;
        }

        public Uri? LastRequestUri { get; private set; }

        public string? LastContent { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            if (request.Content is not null)
            {
                LastContent = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            return new HttpResponseMessage(_statusCode);
        }
    }
}
