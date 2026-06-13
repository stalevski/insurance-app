using InsuranceIntegration.Api.Services.Billing;

namespace InsuranceIntegration.Api.Endpoints;

public static class BillingEndpoints
{
    public static IEndpointRouteBuilder MapBillingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/billing/payments", (PaymentRecordRequest request, IPaymentApplicationService payments) =>
        {
            try
            {
                var result = payments.RecordPayment(request);
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Invalid payment request");
            }
        });

        endpoints.MapPost("/api/v1/billing/delinquency", (DelinquencyAssessmentRequest request, IDelinquencyAssessmentService delinquency) =>
        {
            try
            {
                var result = delinquency.Assess(request);
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Invalid delinquency assessment request");
            }
        });

        return endpoints;
    }
}
