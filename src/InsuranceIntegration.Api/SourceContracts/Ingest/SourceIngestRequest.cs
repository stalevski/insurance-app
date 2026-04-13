using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace InsuranceIntegration.Api.SourceContracts.Ingest;

public sealed class SourceIngestRequest
{
    [Required]
    public required string SourceSystem { get; init; }

    [Required]
    public required string MessageType { get; init; }

    [Required]
    public required JsonElement Payload { get; init; }
}
