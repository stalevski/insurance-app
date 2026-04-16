using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace InsuranceIntegration.Api.SourceContracts.Ingest;

public sealed class SourceIngestEnvelope
{
    [Required]
    public required string Id { get; init; }

    [Required]
    public required string Source { get; init; }

    [Required]
    public required string Type { get; init; }

    [Required]
    public required string SchemaVersion { get; init; }

    [Required]
    public required DateTime OccurredAtUtc { get; init; }

    public string? CorrelationId { get; init; }

    [Required]
    public required JsonElement Data { get; init; }
}
