namespace InsuranceIntegration.Api.Services.Claims;

public interface IClaimFinancialService
{
    ClaimFinancialResult Apply(ClaimFinancialRequest request);
}
