namespace InsuranceIntegration.Api.Services.Catalog;

public sealed class SourceSystemCatalogItem
{
    public string SystemCode { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string BusinessPurpose { get; init; } = string.Empty;

    public string MessageType { get; init; } = string.Empty;

    public object ExamplePayload { get; init; } = new();
}
