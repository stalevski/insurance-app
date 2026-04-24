using InsuranceIntegration.Api.Services.Correlation;

namespace InsuranceIntegration.Api.Middleware;

public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    public const string CausationHeaderName = "X-Causation-Id";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICorrelationContext correlationContext)
    {
        var correlationId = ResolveGuid(context.Request.Headers[HeaderName], Guid.CreateVersion7());
        var causationId = ResolveOptionalGuid(context.Request.Headers[CausationHeaderName]);

        correlationContext.Set(correlationId, causationId);
        context.Response.Headers[HeaderName] = correlationId.ToString();

        var scope = new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        };
        if (causationId.HasValue)
        {
            scope["CausationId"] = causationId.Value;
        }

        using (_logger.BeginScope(scope))
        {
            await _next(context);
        }
    }

    private static Guid ResolveGuid(Microsoft.Extensions.Primitives.StringValues raw, Guid fallback)
    {
        if (raw.Count == 0)
        {
            return fallback;
        }

        return Guid.TryParse(raw.ToString(), out var parsed) ? parsed : fallback;
    }

    private static Guid? ResolveOptionalGuid(Microsoft.Extensions.Primitives.StringValues raw)
    {
        if (raw.Count == 0)
        {
            return null;
        }

        return Guid.TryParse(raw.ToString(), out var parsed) ? parsed : null;
    }
}
