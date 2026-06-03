using InsuranceIntegration.Api.CanonicalContracts.Risks;

namespace InsuranceIntegration.Api.Services.Risks;

/// <summary>
/// Resolves a <see cref="LineOfBusiness"/> from a product code, using the product
/// catalog first and falling back to keyword heuristics for unknown codes.
/// </summary>
public interface ILineOfBusinessResolver
{
    LineOfBusiness Resolve(string productCode);
}
