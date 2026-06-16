using Microsoft.Extensions.Logging;

namespace InsuranceIntegration.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Routes host log output produced while a test runs to the current NUnit test's output, filtered to
/// warnings and above. Server-side problems (for example an unhandled exception behind a 500) then
/// surface in the failing test's report instead of being swallowed, while green runs stay quiet and
/// free of the Info-level EF Core SQL noise.
/// </summary>
internal sealed class TestContextLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new TestContextLogger(categoryName);

    public void Dispose()
    {
    }

    private sealed class TestContextLogger(string categoryName) : ILogger
    {
        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            TestContext.Out.WriteLine($"[{logLevel}] {categoryName}: {formatter(state, exception)}");

            if (exception is not null)
            {
                TestContext.Out.WriteLine(exception);
            }
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            private NullScope()
            {
            }

            public void Dispose()
            {
            }
        }
    }
}
