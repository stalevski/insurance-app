using InsuranceIntegration.Api.Responses.Ingest;

namespace InsuranceIntegration.Api.Services.Ingest;

public interface IIdempotencyStore
{
    bool TryGet(string source, string envelopeId, out IngestReceipt? existingReceipt);

    IngestReceipt? Find(string source, string envelopeId);

    void Store(string source, string envelopeId, IngestReceipt receipt);
}
