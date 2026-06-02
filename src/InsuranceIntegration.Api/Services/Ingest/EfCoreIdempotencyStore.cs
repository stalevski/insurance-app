using System.Text.Json;
using InsuranceIntegration.Api.Persistence;
using InsuranceIntegration.Api.Responses.Ingest;

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

    public async Task<IngestReceipt?> FindAsync(string source, string envelopeId, CancellationToken cancellationToken = default)
    {
        var entry = await _context.IngestEntries.FindAsync([source, envelopeId], cancellationToken);
        return entry is null
            ? null
            : JsonSerializer.Deserialize<IngestReceipt>(entry.OutcomeJson, SerializerOptions);
    }

    public async Task StoreAsync(string source, string envelopeId, IngestReceipt receipt, CancellationToken cancellationToken = default)
    {
        var existing = await _context.IngestEntries.FindAsync([source, envelopeId], cancellationToken);
        var json = JsonSerializer.Serialize(receipt, SerializerOptions);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (existing is null)
        {
            _context.IngestEntries.Add(new IngestEntryEntity
            {
                Source = source,
                EnvelopeId = envelopeId,
                MessageType = receipt.MessageType,
                ProcessedBy = receipt.ProcessedBy,
                CorrelationId = receipt.CorrelationId,
                OutcomeJson = json,
                ReceivedAtUtc = now
            });
        }
        else
        {
            existing.MessageType = receipt.MessageType;
            existing.ProcessedBy = receipt.ProcessedBy;
            existing.CorrelationId = receipt.CorrelationId;
            existing.OutcomeJson = json;
            existing.ReceivedAtUtc = now;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
