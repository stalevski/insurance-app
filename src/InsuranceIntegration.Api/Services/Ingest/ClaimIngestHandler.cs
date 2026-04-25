using System.Text.Json;
using InsuranceIntegration.Api.CanonicalContracts.Claims;
using InsuranceIntegration.Api.Services.Flows;
using InsuranceIntegration.Api.SourceContracts.Claims;
using InsuranceIntegration.Api.SourceContracts.Ingest;

namespace InsuranceIntegration.Api.Services.Ingest;

public sealed class ClaimIngestHandler : IIngestHandler
{
    private static readonly HashSet<string> SupportedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "ClaimNotice",
        "LossRun"
    };

    private readonly IClaimFlowService _claimFlowService;

    public ClaimIngestHandler(IClaimFlowService claimFlowService)
    {
        _claimFlowService = claimFlowService;
    }

    public string Name => "ClaimIngestHandler";

    public bool CanHandle(SourceIngestEnvelope envelope)
    {
        return SupportedTypes.Contains(envelope.Type);
    }

    public object Handle(SourceIngestEnvelope envelope)
    {
        var payload = envelope.Data.Deserialize<ClaimNoticePayload>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Unable to deserialize claim notice payload.");

        var request = new CanonicalClaimRequest
        {
            EntityId = Guid.NewGuid(),
            ClaimReference = payload.ClaimReference,
            PolicyReference = payload.PolicyReference,
            ClaimantName = payload.ClaimantName,
            SourceSystem = envelope.Source,
            LossDate = payload.LossDate,
            ReportedAtUtc = envelope.OccurredAtUtc,
            LossCause = payload.LossCause,
            IncurredAmount = payload.EstimatedIncurred,
            ReservedAmount = payload.EstimatedReserved,
            PaidAmount = payload.PaidAmount,
            CurrencyCode = payload.CurrencyCode,
            FraudIndicator = payload.FraudIndicator,
            AffectedSectionCode = payload.AffectedSectionCode,
            AffectedSubcoverCode = payload.AffectedSubcoverCode,
            AffectedPerilCode = payload.AffectedPerilCode,
            DeductibleApplied = payload.DeductibleApplied,
            PerOccurrenceLimit = payload.PerOccurrenceLimit
        };

        return _claimFlowService.Process(request);
    }
}
