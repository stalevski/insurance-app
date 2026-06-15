using Bunit;
using InsuranceIntegration.Api.Services.Ui;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace InsuranceIntegration.Api.IntegrationTests.Ui;

/// <summary>
/// Base fixture for the bUnit page tests. NUnit reuses a single fixture instance per class, so a
/// fresh <see cref="BunitContext"/> is created for every test (in <see cref="CreateRenderingContext"/>)
/// and disposed afterwards — sharing one context would leak service registrations between tests.
/// Derived fixtures render through <see cref="Render{TPage}"/>, which registers the supplied
/// <see cref="IUiGateway"/> double before rendering. The <c>Ui</c> category is declared here and
/// inherited by every derived fixture.
/// </summary>
[Category("Ui")]
public abstract class UiPageTestBase : IDisposable
{
    private BunitContext? _context;
    private bool _disposed;

    [SetUp]
    public void CreateRenderingContext()
    {
        _context = new BunitContext();
        _context.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [TearDown]
    public void DisposeRenderingContext()
    {
        _context?.Dispose();
        _context = null;
    }

    /// <summary>
    /// Registers <paramref name="gateway"/> as the page's <see cref="IUiGateway"/> and renders
    /// <typeparamref name="TPage"/> in the current test's context.
    /// </summary>
    protected IRenderedComponent<TPage> Render<TPage>(IUiGateway gateway)
        where TPage : IComponent
    {
        _context!.Services.AddSingleton(gateway);
        return _context.Render<TPage>();
    }

    /// <summary>
    /// Registers <paramref name="gateway"/> and renders <typeparamref name="TPage"/> with the
    /// supplied parameters — used by the detail pages, which take a route parameter
    /// (<c>PolicyReference</c> / <c>QuoteReference</c>).
    /// </summary>
    protected IRenderedComponent<TPage> Render<TPage>(
        IUiGateway gateway,
        Action<ComponentParameterCollectionBuilder<TPage>> parameters)
        where TPage : IComponent
    {
        _context!.Services.AddSingleton(gateway);
        return _context.Render(parameters);
    }

    /// <summary>
    /// Registers an additional service the page under test resolves directly via
    /// <c>@inject</c> (for example the <c>Database</c> page's browser gate).
    /// </summary>
    protected void RegisterService<TService>(TService instance)
        where TService : class =>
        _context!.Services.AddSingleton(instance);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _context?.Dispose();
        }

        _disposed = true;
    }
}
