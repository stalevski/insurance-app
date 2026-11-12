namespace InsuranceIntegration.Api.Services.Billing;

public interface IRefundProcessingService
{
    RefundResult Process(RefundRequest request);
}
