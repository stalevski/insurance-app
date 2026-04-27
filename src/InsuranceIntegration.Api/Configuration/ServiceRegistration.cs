using InsuranceIntegration.Api.Mappers.Risks;
using InsuranceIntegration.Api.Persistence;
using InsuranceIntegration.Api.Services.Catalog;
using InsuranceIntegration.Api.Services.Clearance;
using InsuranceIntegration.Api.Services.Correlation;
using InsuranceIntegration.Api.Services.Events;
using InsuranceIntegration.Api.Services.Flows;
using InsuranceIntegration.Api.Services.Ingest;
using InsuranceIntegration.Api.Services.Matching;
using InsuranceIntegration.Api.Services.Outbox;
using InsuranceIntegration.Api.Services.Policies;
using InsuranceIntegration.Api.Services.Pricing;
using InsuranceIntegration.Api.Services.Products;
using InsuranceIntegration.Api.Services.Schemas;
using InsuranceIntegration.Api.Services.Snapshots;
using Microsoft.EntityFrameworkCore;

namespace InsuranceIntegration.Api.Configuration;

public static class ServiceRegistration
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddEndpointsApiExplorer();

        var connectionString = configuration?.GetConnectionString("Integration") ?? "Data Source=integration.db";
        services.AddDbContext<IntegrationDbContext>(options => options.UseSqlite(connectionString));

        services.TryAddSingletonTimeProvider();

        services.AddScoped<ICorrelationContext, CorrelationContext>();
        services.AddScoped<IOutboxWriter, OutboxWriter>();
        services.AddHostedService<OutboxDispatcher>();

        services.AddSingleton<ISourceSystemCatalogService, SourceSystemCatalogService>();
        services.AddSingleton<ILevenshteinDistanceCalculator, LevenshteinDistanceCalculator>();
        services.AddSingleton<IJsonSchemaService, JsonSchemaService>();
        services.AddScoped<ISubmissionRegistry, EfCoreSubmissionRegistry>();
        services.AddSingleton<IProductCatalog, ProductCatalog>();

        services.AddScoped<IRatingService, RatingService>();
        services.AddScoped<IPolicyAdjustmentService, PolicyAdjustmentService>();
        services.AddScoped<IPolicyLifecycleService, PolicyLifecycleService>();

        services.AddScoped<ISubmissionClearanceService, SubmissionClearanceService>();
        services.AddScoped<IRiskFlowService, RiskFlowService>();
        services.AddScoped<IRiskIngestMapper, RiskIngestMapper>();
        services.AddScoped<ISourceRiskMapper, ContosoRiskMapper>();
        services.AddScoped<ISourceRiskMapper, QuoteForgeRiskMapper>();
        services.AddScoped<ISourceRiskMapper, BindPointRiskMapper>();

        services.AddScoped<IClaimFlowService, ClaimFlowService>();
        services.AddScoped<IBillingFlowService, BillingFlowService>();
        services.AddScoped<IComplianceFlowService, ComplianceFlowService>();

        services.AddSingleton<IPolicySnapshotProjector, PolicySnapshotProjector>();
        services.AddSingleton<IQuoteSnapshotProjector, QuoteSnapshotProjector>();
        services.AddScoped<IPolicySnapshotService, PolicySnapshotService>();
        services.AddScoped<IQuoteSnapshotService, QuoteSnapshotService>();
        services.AddScoped<IDomainEventLog, DomainEventLog>();
        services.AddScoped<ISnapshotRebuildService, SnapshotRebuildService>();
        services.AddScoped<IRiskSnapshotRouter, RiskSnapshotRouter>();

        services.AddScoped<IIdempotencyStore, EfCoreIdempotencyStore>();
        services.AddScoped<IIngestHandler, RiskIngestHandler>();
        services.AddScoped<IIngestHandler, ClaimIngestHandler>();
        services.AddScoped<IIngestHandler, BillingIngestHandler>();
        services.AddScoped<IIngestHandler, ComplianceIngestHandler>();
        services.AddScoped<IIngestDispatcher, IngestDispatcher>();

        return services;
    }

    private static void TryAddSingletonTimeProvider(this IServiceCollection services)
    {
        if (!services.Any(descriptor => descriptor.ServiceType == typeof(TimeProvider)))
        {
            services.AddSingleton(TimeProvider.System);
        }
    }
}
