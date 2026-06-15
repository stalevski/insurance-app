using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace InsuranceIntegration.Api.IntegrationTests.Ui;

/// <summary>
/// Shared bUnit assertions for the page tests, covering the markup checks that recur across the
/// list and dashboard pages.
/// </summary>
internal static class UiAssertions
{
    /// <summary>
    /// Asserts the page renders its empty-state copy (<paramref name="message"/>) and omits the data
    /// table entirely.
    /// </summary>
    public static void ShouldShowEmptyState<TPage>(this IRenderedComponent<TPage> cut, string message)
        where TPage : IComponent =>
        Assert.Multiple(() =>
        {
            Assert.That(cut.Markup, Does.Contain(message));
            Assert.That(cut.FindAll("table"), Is.Empty);
        });

    /// <summary>
    /// Asserts an anchor points at <paramref name="href"/> and shows <paramref name="text"/> as its
    /// trimmed content.
    /// </summary>
    public static void ShouldLinkTo(this IElement anchor, string href, string text) =>
        Assert.Multiple(() =>
        {
            Assert.That(anchor.GetAttribute("href"), Is.EqualTo(href));
            Assert.That(anchor.TextContent.Trim(), Is.EqualTo(text));
        });
}
