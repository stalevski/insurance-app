using InsuranceIntegration.Api.Services.Matching;

namespace InsuranceIntegration.Api.Tests.Matching;

public sealed class LevenshteinDistanceCalculatorTests
{
    [TestCase("kitten", "sitting", 3)]
    [TestCase("risk", "risk", 0)]
    [TestCase("", "claim", 5)]
    [TestCase("policy", "", 6)]
    public void Calculate_ReturnsExpectedDistance(string source, string target, int expected)
    {
        var calculator = new LevenshteinDistanceCalculator();

        var result = calculator.Calculate(source, target);

        Assert.That(result, Is.EqualTo(expected));
    }
}
