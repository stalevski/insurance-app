namespace InsuranceIntegration.Api.Services.Seeding;

/// <summary>
/// Populates the database with representative quotes and policies across every product
/// (risk) family on first run, so the UI has data to explore in a development environment.
/// </summary>
public interface IDevelopmentDataSeeder
{
    /// <summary>
    /// Seeds sample data when the store is empty. Safe to call repeatedly — it is a no-op
    /// once any quote snapshot exists.
    /// </summary>
    Task SeedAsync(CancellationToken cancellationToken = default);
}
