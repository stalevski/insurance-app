using InsuranceIntegration.Api.Security;

namespace InsuranceIntegration.Api.Middleware;

/// <summary>
/// Returns <c>404 Not Found</c> for requests to the read-only database browser page
/// (<c>/database</c>) when it is disabled for the current environment. Responding with 404 (rather
/// than 403) avoids revealing that the page exists. When the browser is enabled the middleware is a
/// no-op.
/// </summary>
public sealed class DatabaseBrowserGateMiddleware
{
    private readonly RequestDelegate _next;
    private readonly DatabaseBrowserGate _gate;

    public DatabaseBrowserGateMiddleware(RequestDelegate next, DatabaseBrowserGate gate)
    {
        _next = next;
        _gate = gate;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_gate.IsEnabled)
        {
            var path = context.Request.Path.HasValue ? context.Request.Path.Value : null;
            if (DatabaseBrowserGate.IsDatabaseBrowserPath(path))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }
        }

        await _next(context);
    }
}
