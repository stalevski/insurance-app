using InsuranceIntegration.Api.Configuration;
using InsuranceIntegration.Api.Endpoints;
using InsuranceIntegration.Api.Middleware;
using InsuranceIntegration.Api.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiServices(builder.Configuration);
builder.Services.AddOpenApi();

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

app.MapHealthEndpoints();
app.MapSourceSystemEndpoints();
app.MapIngestEndpoints();
app.MapRiskEndpoints();
app.MapSchemaEndpoints();
app.MapProductEndpoints();
app.MapPolicyEndpoints();

app.Run();

public partial class Program;
