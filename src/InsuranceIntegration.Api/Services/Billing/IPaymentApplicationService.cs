namespace InsuranceIntegration.Api.Services.Billing;

public interface IPaymentApplicationService
{
    PaymentRecordResult RecordPayment(PaymentRecordRequest request);
}
