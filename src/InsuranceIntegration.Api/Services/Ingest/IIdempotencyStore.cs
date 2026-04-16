using InsuranceIntegration.Api.FinalMessages.Ingest;

namespace InsuranceIntegration.Api.Services.Ingest;

public interface IIdempotencyStore
{
    bool TryGet(string source, string envelopeId, out IngestAcceptedResult? existingResult);

    void Store(string source, string envelopeId, IngestAcceptedResult result);
}
