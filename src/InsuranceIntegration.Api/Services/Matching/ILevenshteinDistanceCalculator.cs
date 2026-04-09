namespace InsuranceIntegration.Api.Services.Matching;

public interface ILevenshteinDistanceCalculator
{
    int Calculate(string source, string target);
}
