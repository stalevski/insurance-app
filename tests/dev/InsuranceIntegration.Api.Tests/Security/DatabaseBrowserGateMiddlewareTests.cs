using InsuranceIntegration.Api.Middleware;
using InsuranceIntegration.Api.Security;
using Microsoft.AspNetCore.Http;

namespace InsuranceIntegration.Api.Tests.Security;

public sealed class DatabaseBrowserGateMiddlewareTests
{
    private static DatabaseBrowserGate Gate(bool enabled) =>
        new(new DatabaseBrowserOptions { Enabled = enabled }, isDevelopmentEnvironment: false);

    private static async Task<(int StatusCode, bool NextCalled)> InvokeAsync(
        DatabaseBrowserGate gate, string path)
    {
        var nextCalled = false;
        var middleware = new DatabaseBrowserGateMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            gate);

        var context = new DefaultHttpContext();
        context.Request.Path = path;

        await middleware.InvokeAsync(context);

        return (context.Response.StatusCode, nextCalled);
    }

    [Test]
    public async Task InvokeAsync_Returns404AndShortCircuits_ForDatabasePath_WhenDisabled()
    {
        var (statusCode, nextCalled) = await InvokeAsync(Gate(enabled: false), "/database");

        Assert.Multiple(() =>
        {
            Assert.That(statusCode, Is.EqualTo(StatusCodes.Status404NotFound));
            Assert.That(nextCalled, Is.False);
        });
    }

    [Test]
    public async Task InvokeAsync_CallsNext_ForDatabasePath_WhenEnabled()
    {
        var (statusCode, nextCalled) = await InvokeAsync(Gate(enabled: true), "/database");

        Assert.Multiple(() =>
        {
            Assert.That(statusCode, Is.EqualTo(StatusCodes.Status200OK));
            Assert.That(nextCalled, Is.True);
        });
    }

    [Test]
    public async Task InvokeAsync_CallsNext_ForNonDatabasePath_WhenDisabled()
    {
        var (statusCode, nextCalled) = await InvokeAsync(Gate(enabled: false), "/api/v1/policies");

        Assert.Multiple(() =>
        {
            Assert.That(statusCode, Is.EqualTo(StatusCodes.Status200OK));
            Assert.That(nextCalled, Is.True);
        });
    }
}
