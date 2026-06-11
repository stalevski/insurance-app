using System.Text.Json;
using InsuranceIntegration.Api.Persistence;
using InsuranceIntegration.Api.Responses.Ingest;
using Microsoft.EntityFrameworkCore;

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

    public async Task<IngestReceipt> StoreAsync(string source, string envelopeId, IngestReceipt receipt, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(receipt, SerializerOptions);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var entry = new IngestEntryEntity
        {
            Source = source,
            EnvelopeId = envelopeId,
            MessageType = receipt.MessageType,
            ProcessedBy = receipt.ProcessedBy,
            CorrelationId = receipt.CorrelationId,
            OutcomeJson = json,
            ReceivedAtUtc = now
        };

        // Insert-first so the composite primary key (Source, EnvelopeId) enforces idempotency
        // atomically. If a concurrent identical envelope already inserted, the unique-constraint
        // violation means "already processed" — first writer wins and its receipt is returned.
        _context.IngestEntries.Add(entry);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return receipt;
        }
        catch (DbUpdateException)
        {
            _context.Entry(entry).State = EntityState.Detached;
            var existing = await _context.IngestEntries.FindAsync([source, envelopeId], cancellationToken)
                ?? throw new InvalidOperationException(
                    $"Idempotency insert for '{source}/{envelopeId}' failed but no existing entry was found.");
            return JsonSerializer.Deserialize<IngestReceipt>(existing.OutcomeJson, SerializerOptions)
                ?? throw new InvalidOperationException(
                    $"Stored idempotency outcome for '{source}/{envelopeId}' could not be deserialized.");
        }
    }
}
