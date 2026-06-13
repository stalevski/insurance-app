using InsuranceIntegration.Api.Security;

namespace InsuranceIntegration.Api.Middleware;

/// <summary>
/// Rejects mutating requests (and, optionally, the database browser page) that do not carry a
/// valid API key. When no keys are configured the middleware is a no-op.
/// </summary>
public sealed class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ApiKeyValidator _validator;
    private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;

    public ApiKeyAuthenticationMiddleware(RequestDelegate next, ApiKeyValidator validator, ILogger<ApiKeyAuthenticationMiddleware> logger)
    {
        _next = next;
        _validator = validator;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.HasValue ? context.Request.Path.Value : null;
        var suppliedKey = context.Request.Headers[_validator.HeaderName].ToString();

        var decision = _validator.Evaluate(context.Request.Method, path, suppliedKey);

        if (decision == ApiKeyDecision.Rejected)
        {
            _logger.LogWarning("Rejected unauthorized {Method} request to {Path}", context.Request.Method, path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers.WWWAuthenticate = $"ApiKey header=\"{_validator.HeaderName}\"";
            await context.Response.WriteAsJsonAsync(new { error = "A valid API key is required for this request." });
            return;
        }

        await _next(context);
    }
}
