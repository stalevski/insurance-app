namespace InsuranceIntegration.Api.Services.Schemas;

public interface IJsonSchemaService
{
    object GenerateSchema<T>();
}
