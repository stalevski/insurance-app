using Bunit;
using InsuranceIntegration.Api.Services.Ui;
using Microsoft.Extensions.DependencyInjection;

namespace InsuranceIntegration.Api.IntegrationTests.Ui;

/// <summary>
/// Builds a fresh <see cref="BunitContext"/> per test (NUnit reuses a single fixture instance, so a
/// shared context would leak service registrations between tests) wired with a loose JS-interop
/// runtime and the supplied <see cref="IUiGateway"/> double. The caller owns the returned context
/// and disposes it with a <c>using</c> declaration.
/// </summary>
internal static class PageRenderer
{
    public static BunitContext ContextFor(IUiGateway gateway)
    {
        var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddSingleton(gateway);
        return context;
    }
}
