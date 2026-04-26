using System.Text.Json;
using InsuranceIntegration.Api.CanonicalContracts.Risks;
using InsuranceIntegration.Api.Persistence;
using InsuranceIntegration.Api.Responses.Risks;
using InsuranceIntegration.Api.Services.Ingest;
using InsuranceIntegration.Api.Snapshots.Quotes;
using Microsoft.EntityFrameworkCore;

namespace InsuranceIntegration.Api.Services.Snapshots;

public sealed class QuoteSnapshotService : IQuoteSnapshotService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly IntegrationDbContext _context;
    private readonly IQuoteSnapshotProjector _projector;

    public QuoteSnapshotService(IntegrationDbContext context, IQuoteSnapshotProjector projector)
    {
        _context = context;
        _projector = projector;
    }

    public QuoteSnapshot? Find(string quoteReference)
    {
        var entity = _context.QuoteSnapshots.AsNoTracking().FirstOrDefault(record => record.QuoteReference == quoteReference);
        return entity is null ? null : Deserialize(entity.SnapshotJson);
    }

    public IReadOnlyList<QuoteSnapshotSummary> List(int skip = 0, int take = 100)
    {
        if (take <= 0) take = 100;
        if (take > 500) take = 500;
        if (skip < 0) skip = 0;

        return _context.QuoteSnapshots
            .AsNoTracking()
            .OrderByDescending(record => record.LastUpdatedUtc)
            .Skip(skip)
            .Take(take)
            .Select(record => new QuoteSnapshotSummary
            {
                QuoteReference = record.QuoteReference,
                PolicyReference = record.PolicyReference,
                ProductCode = record.ProductCode,
                UnderwritingYear = record.UnderwritingYear,
                CurrentPhase = record.CurrentPhase,
                IsBound = record.IsBound,
                LastUpdatedUtc = record.LastUpdatedUtc,
                Self = "/api/v1/quotes/" + Uri.EscapeDataString(record.QuoteReference)
            })
            .ToList();
    }

    public QuoteSnapshot Apply(CanonicalRiskRequest request, FinalRiskResponse response, IngestContext context)
    {
        var quoteReference = request.Quote.QuoteReference ?? request.ExternalReference;
        if (string.IsNullOrWhiteSpace(quoteReference))
        {
            throw new InvalidOperationException("QuoteSnapshotService requires Quote.QuoteReference or ExternalReference on the canonical request.");
        }

        var entity = _context.QuoteSnapshots.FirstOrDefault(record => record.QuoteReference == quoteReference);
        var current = entity is null ? null : Deserialize(entity.SnapshotJson);

        var projected = _projector.Apply(current, request, response, context);
        var json = JsonSerializer.Serialize(projected, SerializerOptions);

        if (entity is null)
        {
            _context.QuoteSnapshots.Add(new QuoteSnapshotEntity
            {
                QuoteReference = projected.QuoteReference,
                PolicyReference = projected.PolicyReference,
                ProductCode = projected.ProductCode,
                UnderwritingYear = projected.UnderwritingYear,
                CurrentPhase = projected.Lifecycle.CurrentPhase,
                IsBound = projected.Lifecycle.IsBound,
                SnapshotJson = json,
                LastUpdatedUtc = projected.LastUpdatedUtc
            });
        }
        else
        {
            entity.PolicyReference = projected.PolicyReference;
            entity.ProductCode = projected.ProductCode;
            entity.UnderwritingYear = projected.UnderwritingYear;
            entity.CurrentPhase = projected.Lifecycle.CurrentPhase;
            entity.IsBound = projected.Lifecycle.IsBound;
            entity.SnapshotJson = json;
            entity.LastUpdatedUtc = projected.LastUpdatedUtc;
        }

        _context.SaveChanges();
        return projected;
    }

    private static QuoteSnapshot Deserialize(string json)
    {
        return JsonSerializer.Deserialize<QuoteSnapshot>(json, SerializerOptions)
            ?? new QuoteSnapshot();
    }
}
