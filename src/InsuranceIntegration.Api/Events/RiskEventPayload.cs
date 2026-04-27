using InsuranceIntegration.Api.CanonicalContracts.Risks;
using InsuranceIntegration.Api.Responses.Risks;

namespace InsuranceIntegration.Api.Events;

/// <summary>
/// PayloadJson schema for risk-flow domain events. Storing both the canonical request
/// and the final response keeps replay deterministic without re-running the flow service.
/// </summary>
public sealed record RiskEventPayload(CanonicalRiskRequest CanonicalRequest, FinalRiskResponse FinalResponse);
