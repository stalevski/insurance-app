namespace InsuranceIntegration.Api.Services.Catalog;

public interface ISourceSystemCatalogService
{
    IReadOnlyCollection<SourceSystemCatalogItem> GetSourceSystems();
}
