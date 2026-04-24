using InsuranceIntegration.Api.Services.Ingest;
using InsuranceIntegration.Api.SourceContracts.Ingest;

namespace InsuranceIntegration.Api.Tests.Ingest;

internal sealed class StubIngestHandler : IIngestHandler
{
    private readonly bool _canHandle;
    private readonly object? _result;

    public StubIngestHandler(string name, bool canHandle, object? result)
    {
        Name = name;
        _canHandle = canHandle;
        _result = result;
    }

    public string Name { get; }

    public bool CanHandle(SourceIngestEnvelope envelope)
    {
        return _canHandle;
    }

    public object Handle(SourceIngestEnvelope envelope)
    {
        return _result ?? new object();
    }
}
