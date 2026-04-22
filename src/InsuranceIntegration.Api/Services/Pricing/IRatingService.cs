namespace InsuranceIntegration.Api.Services.Pricing;

public interface IRatingService
{
    RatingQuote Rate(string productCode, decimal annualRevenue, string currencyCode);
}
