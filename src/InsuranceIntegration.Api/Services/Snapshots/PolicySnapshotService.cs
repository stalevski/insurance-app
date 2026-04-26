using System.Text.Json;
using InsuranceIntegration.Api.CanonicalContracts.Risks;
using InsuranceIntegration.Api.Persistence;
using InsuranceIntegration.Api.Responses.Risks;
using InsuranceIntegration.Api.Services.Ingest;
using InsuranceIntegration.Api.Snapshots.Policies;
using Microsoft.EntityFrameworkCore;

namespace InsuranceIntegration.Api.Services.Snapshots;

public sealed class PolicySnapshotService : IPolicySnapshotService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly IntegrationDbContext _context;
    private readonly IPolicySnapshotProjector _projector;

    public PolicySnapshotService(IntegrationDbContext context, IPolicySnapshotProjector projector)
    {
        _context = context;
        _projector = projector;
    }

    public PolicySnapshot? Find(string policyReference)
    {
        var entity = _context.PolicySnapshots.AsNoTracking().FirstOrDefault(record => record.PolicyReference == policyReference);
        return entity is null ? null : Deserialize(entity.SnapshotJson);
    }

    public IReadOnlyList<PolicySnapshotSummary> List(int skip = 0, int take = 100)
    {
        if (take <= 0) take = 100;
        if (take > 500) take = 500;
        if (skip < 0) skip = 0;

        return _context.PolicySnapshots
            .AsNoTracking()
            .OrderByDescending(record => record.LastUpdatedUtc)
            .Skip(skip)
            .Take(take)
            .Select(record => new PolicySnapshotSummary
            {
                PolicyReference = record.PolicyReference,
                QuoteReference = record.QuoteReference,
                ProductCode = record.ProductCode,
                UnderwritingYear = record.UnderwritingYear,
                CurrentPhase = record.CurrentPhase,
                LastUpdatedUtc = record.LastUpdatedUtc,
                Self = "/api/v1/policies/" + Uri.EscapeDataString(record.PolicyReference)
            })
            .ToList();
    }

    public PolicySnapshot Apply(CanonicalRiskRequest request, FinalRiskResponse response, IngestContext context)
    {
        var policyReference = request.Policy.PolicyReference
            ?? throw new InvalidOperationException("PolicySnapshotService requires Policy.PolicyReference on the canonical request.");

        var entity = _context.PolicySnapshots.FirstOrDefault(record => record.PolicyReference == policyReference);
        var current = entity is null ? null : Deserialize(entity.SnapshotJson);

        var projected = _projector.Apply(current, request, response, context);
        var json = JsonSerializer.Serialize(projected, SerializerOptions);

        if (entity is null)
        {
            _context.PolicySnapshots.Add(new PolicySnapshotEntity
            {
                PolicyReference = projected.PolicyReference,
                QuoteReference = projected.QuoteReference,
                ProductCode = projected.ProductCode,
                UnderwritingYear = projected.UnderwritingYear,
                CurrentPhase = projected.Lifecycle.CurrentPhase,
                SnapshotJson = json,
                LastUpdatedUtc = projected.LastUpdatedUtc
            });
        }
        else
        {
            entity.QuoteReference = projected.QuoteReference;
            entity.ProductCode = projected.ProductCode;
            entity.UnderwritingYear = projected.UnderwritingYear;
            entity.CurrentPhase = projected.Lifecycle.CurrentPhase;
            entity.SnapshotJson = json;
            entity.LastUpdatedUtc = projected.LastUpdatedUtc;
        }

        _context.SaveChanges();
        return projected;
    }

    private static PolicySnapshot Deserialize(string json)
    {
        return JsonSerializer.Deserialize<PolicySnapshot>(json, SerializerOptions)
            ?? new PolicySnapshot();
    }
}
