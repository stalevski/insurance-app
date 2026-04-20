using System.Text.Json;
using InsuranceIntegration.Api.CanonicalContracts.Billing;
using InsuranceIntegration.Api.Services.Flows;
using InsuranceIntegration.Api.SourceContracts.Billing;
using InsuranceIntegration.Api.SourceContracts.Ingest;

namespace InsuranceIntegration.Api.Services.Ingest;

public sealed class BillingIngestHandler : IIngestHandler
{
    private static readonly HashSet<string> SupportedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "InstallmentSchedule"
    };

    private readonly IBillingFlowService _billingFlowService;

    public BillingIngestHandler(IBillingFlowService billingFlowService)
    {
        _billingFlowService = billingFlowService;
    }

    public string Name => "BillingIngestHandler";

    public bool CanHandle(SourceIngestEnvelope envelope)
    {
        return SupportedTypes.Contains(envelope.Type);
    }

    public object Handle(SourceIngestEnvelope envelope)
    {
        var payload = envelope.Data.Deserialize<InstallmentSchedulePayload>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Unable to deserialize installment schedule payload.");

        var request = new CanonicalBillingRequest
        {
            EntityId = Guid.NewGuid(),
            PolicyReference = payload.PolicyReference,
            SourceSystem = envelope.Source,
            InstallmentCount = payload.InstallmentCount,
            TotalAmount = payload.TotalAmount,
            PaidToDate = payload.PaidToDate,
            MissedPayments = payload.MissedPayments,
            CurrencyCode = payload.CurrencyCode,
            FirstDueDate = payload.FirstDueDate
        };

        return _billingFlowService.Process(request);
    }
}
