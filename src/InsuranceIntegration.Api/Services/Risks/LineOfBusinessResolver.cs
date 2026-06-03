using InsuranceIntegration.Api.CanonicalContracts.Risks;
using InsuranceIntegration.Api.Services.Products;

namespace InsuranceIntegration.Api.Services.Risks;

public sealed class LineOfBusinessResolver : ILineOfBusinessResolver
{
    private readonly IProductCatalog _productCatalog;

    public LineOfBusinessResolver(IProductCatalog productCatalog)
    {
        _productCatalog = productCatalog;
    }

    public LineOfBusiness Resolve(string productCode)
    {
        if (string.IsNullOrWhiteSpace(productCode))
        {
            return LineOfBusiness.Unknown;
        }

        var product = _productCatalog.FindByProductCode(productCode);
        if (product is not null)
        {
            var byFamily = MapFamily(product.Family);
            if (byFamily != LineOfBusiness.Unknown)
            {
                return byFamily;
            }
        }

        return ResolveFromKeywords(productCode.ToUpperInvariant());
    }

    private static LineOfBusiness MapFamily(string family) => family.ToUpperInvariant() switch
    {
        "PROPERTY" => LineOfBusiness.Property,
        "LIABILITY" => LineOfBusiness.Liability,
        "CYBER" => LineOfBusiness.Cyber,
        "AUTO" => LineOfBusiness.Motor,
        "MOTOR" => LineOfBusiness.Motor,
        _ => LineOfBusiness.Unknown
    };

    private static LineOfBusiness ResolveFromKeywords(string productCode)
    {
        if (productCode.Contains("CYBER", StringComparison.Ordinal))
        {
            return LineOfBusiness.Cyber;
        }

        if (productCode.Contains("AUTO", StringComparison.Ordinal)
            || productCode.Contains("MOTOR", StringComparison.Ordinal)
            || productCode.Contains("FLEET", StringComparison.Ordinal))
        {
            return LineOfBusiness.Motor;
        }

        if (productCode.Contains("PROPERTY", StringComparison.Ordinal)
            || productCode.Contains("HOME", StringComparison.Ordinal)
            || productCode.Contains("COMMERCIAL", StringComparison.Ordinal))
        {
            return LineOfBusiness.Property;
        }

        if (productCode.Contains("LIABILITY", StringComparison.Ordinal)
            || productCode.Contains("PROFESSIONAL", StringComparison.Ordinal)
            || productCode.Contains("MISC", StringComparison.Ordinal))
        {
            return LineOfBusiness.Liability;
        }

        return LineOfBusiness.Unknown;
    }
}
