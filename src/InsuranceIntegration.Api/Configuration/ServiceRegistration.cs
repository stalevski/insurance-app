using InsuranceIntegration.Api.Mappers.Risks;
using InsuranceIntegration.Api.Persistence;
using InsuranceIntegration.Api.Services.Catalog;
using InsuranceIntegration.Api.Services.Clearance;
using InsuranceIntegration.Api.Services.Correlation;
using InsuranceIntegration.Api.Services.Events;
using InsuranceIntegration.Api.Services.Billing;
using InsuranceIntegration.Api.Services.Flows;
using InsuranceIntegration.Api.Services.Ingest;
using InsuranceIntegration.Api.Services.Matching;
using InsuranceIntegration.Api.Services.Outbox;
using InsuranceIntegration.Api.Services.Policies;
using InsuranceIntegration.Api.Services.Pricing;
using InsuranceIntegration.Api.Services.Products;
using InsuranceIntegration.Api.Services.Risks;
using InsuranceIntegration.Api.Services.Risks.Profiles;
using InsuranceIntegration.Api.Services.Schemas;
using InsuranceIntegration.Api.Services.Orchestration;
using InsuranceIntegration.Api.Services.Seeding;
using InsuranceIntegration.Api.Services.Snapshots;
using InsuranceIntegration.Api.Services.Ui;
using Microsoft.EntityFrameworkCore;

namespace InsuranceIntegration.Api.Configuration;

public static class ServiceRegistration
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddEndpointsApiExplorer();

        var connectionString = configuration?.GetConnectionString("Integration") ?? "Data Source=integration.db";
        services.AddSingleton<RowVersionInterceptor>();
        services.AddDbContext<IntegrationDbContext>((sp, options) =>
            options.UseSqlite(connectionString)
                   .AddInterceptors(sp.GetRequiredService<RowVersionInterceptor>()));

        services.TryAddSingletonTimeProvider();

        services.AddScoped<ICorrelationContext, CorrelationContext>();
        services.AddScoped<IOutboxWriter, OutboxWriter>();
        services.AddScoped<IOutboxPublisher, LoggingOutboxPublisher>();
        services.AddHostedService<OutboxDispatcher>();

        services.AddSingleton<ISourceSystemCatalogService, SourceSystemCatalogService>();
        services.AddSingleton<ILevenshteinDistanceCalculator, LevenshteinDistanceCalculator>();
        services.AddSingleton<IJsonSchemaService, JsonSchemaService>();
        services.AddScoped<ISubmissionRegistry, EfCoreSubmissionRegistry>();
        services.AddSingleton<IProductCatalog, ProductCatalog>();

        services.AddSingleton<ILineOfBusinessResolver, LineOfBusinessResolver>();
        services.AddSingleton<IRiskTypeProfile, PropertyRiskProfile>();
        services.AddSingleton<IRiskTypeProfile, LiabilityRiskProfile>();
        services.AddSingleton<IRiskTypeProfile, CyberRiskProfile>();
        services.AddSingleton<IRiskTypeProfile, MotorRiskProfile>();
        services.AddSingleton<IRiskProfileResolver, RiskProfileResolver>();

        services.AddScoped<IRatingService, RatingService>();
        services.AddScoped<IPolicyAdjustmentService, PolicyAdjustmentService>();
        services.AddScoped<IPolicyLifecycleService, PolicyLifecycleService>();
        services.AddScoped<IPolicyRenewalService, PolicyRenewalService>();

        services.AddScoped<ISubmissionClearanceService, SubmissionClearanceService>();
        services.AddScoped<IBindPreconditionService, BindPreconditionService>();
        services.AddScoped<IRiskFlowService, RiskFlowService>();
        services.AddScoped<IRiskIngestMapper, RiskIngestMapper>();
        services.AddScoped<ISourceRiskMapper, ContosoRiskMapper>();
        services.AddScoped<ISourceRiskMapper, QuoteForgeRiskMapper>();
        services.AddScoped<ISourceRiskMapper, BindPointRiskMapper>();

        services.AddScoped<IClaimFlowService, ClaimFlowService>();
        services.AddScoped<IBillingFlowService, BillingFlowService>();
        services.AddScoped<IComplianceFlowService, ComplianceFlowService>();
        services.AddScoped<IPaymentApplicationService, PaymentApplicationService>();
        services.AddScoped<IDelinquencyAssessmentService, DelinquencyAssessmentService>();

        services.AddSingleton<IPolicySnapshotProjector, PolicySnapshotProjector>();
        services.AddSingleton<IQuoteSnapshotProjector, QuoteSnapshotProjector>();
        services.AddScoped<IPolicySnapshotService, PolicySnapshotService>();
        services.AddScoped<IQuoteSnapshotService, QuoteSnapshotService>();
        services.AddScoped<IDomainEventLog, DomainEventLog>();
        services.AddScoped<ISnapshotRebuildService, SnapshotRebuildService>();
        services.AddScoped<IRiskSnapshotRouter, RiskSnapshotRouter>();

        services.AddScoped<IRiskSubmissionOrchestrator, RiskSubmissionOrchestrator>();

        services.AddScoped<IIdempotencyStore, EfCoreIdempotencyStore>();
        services.AddScoped<IIngestHandler, RiskIngestHandler>();
        services.AddScoped<IIngestHandler, ClaimIngestHandler>();
        services.AddScoped<IIngestHandler, BillingIngestHandler>();
        services.AddScoped<IIngestHandler, ComplianceIngestHandler>();
        services.AddScoped<IIngestDispatcher, IngestDispatcher>();

        services.AddSingleton<IUiGateway, UiGateway>();
        services.AddScoped<IDevelopmentDataSeeder, DevelopmentDataSeeder>();

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
