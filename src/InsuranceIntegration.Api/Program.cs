using InsuranceIntegration.Api.Configuration;
using InsuranceIntegration.Api.Components;
using InsuranceIntegration.Api.Endpoints;
using InsuranceIntegration.Api.Middleware;
using InsuranceIntegration.Api.Persistence;
using InsuranceIntegration.Api.Services.Seeding;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiServices(builder.Configuration);
builder.Services.AddOpenApi();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();
    if (context.Database.GetPendingMigrations().Any())
    {
        context.Database.Migrate();
    }
    else if (!context.Database.GetAppliedMigrations().Any())
    {
        context.Database.EnsureCreated();
    }

    if (app.Environment.IsDevelopment())
    {
        var seeder = scope.ServiceProvider.GetRequiredService<IDevelopmentDataSeeder>();
        await seeder.SeedAsync();
    }
}

app.UseMiddleware<CorrelationIdMiddleware>();

app.MapOpenApi();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "InsuranceIntegration.Api v1");
        options.RoutePrefix = "swagger";
    });
}

app.MapStaticAssets();
app.UseAntiforgery();

app.MapHealthEndpoints();
app.MapSourceSystemEndpoints();
app.MapIngestEndpoints();
app.MapRiskEndpoints();
app.MapSchemaEndpoints();
app.MapProductEndpoints();
app.MapPolicyEndpoints();
app.MapPolicyReadEndpoints();
app.MapQuoteReadEndpoints();
app.MapBillingEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program;
