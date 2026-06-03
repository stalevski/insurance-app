using System.Text.Json.Serialization;

namespace InsuranceIntegration.Api.CanonicalContracts.Risks;

/// <summary>
/// Platform-owned classification of a risk by line of business. Drives which
/// risk-type profile (and therefore which enrichments) apply during processing.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<LineOfBusiness>))]
public enum LineOfBusiness
{
    Unknown = 0,
    Property = 1,
    Liability = 2,
    Cyber = 3,
    Motor = 4
}
