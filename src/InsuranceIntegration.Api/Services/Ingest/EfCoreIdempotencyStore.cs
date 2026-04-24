using System.Text.Json;
using InsuranceIntegration.Api.FinalMessages.Ingest;
using InsuranceIntegration.Api.Persistence;

namespace InsuranceIntegration.Api.Services.Ingest;

public sealed class EfCoreIdempotencyStore : IIdempotencyStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IntegrationDbContext _context;
    private readonly TimeProvider _timeProvider;

    public EfCoreIdempotencyStore(IntegrationDbContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public bool TryGet(string source, string envelopeId, out IngestAcceptedResult? existingResult)
    {
        var entry = _context.InboxMessages.Find(source, envelopeId);
        if (entry is null)
        {
            existingResult = null;
            return false;
        }

        existingResult = JsonSerializer.Deserialize<IngestAcceptedResult>(entry.ResultJson, SerializerOptions);
        return existingResult is not null;
    }

    public void Store(string source, string envelopeId, IngestAcceptedResult result)
    {
        var existing = _context.InboxMessages.Find(source, envelopeId);
        var json = JsonSerializer.Serialize(result, SerializerOptions);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (existing is null)
        {
            _context.InboxMessages.Add(new InboxMessageEntity
            {
                Source = source,
                EnvelopeId = envelopeId,
                Type = result.Type,
                HandlerName = result.HandlerName,
                CorrelationId = result.CorrelationId,
                ResultJson = json,
                ProcessedAtUtc = now
            });
        }
        else
        {
            existing.Type = result.Type;
            existing.HandlerName = result.HandlerName;
            existing.CorrelationId = result.CorrelationId;
            existing.ResultJson = json;
            existing.ProcessedAtUtc = now;
        }

        _context.SaveChanges();
    }
}
