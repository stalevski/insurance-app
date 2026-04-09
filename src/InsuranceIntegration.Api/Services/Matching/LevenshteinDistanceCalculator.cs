namespace InsuranceIntegration.Api.Services.Matching;

public sealed class LevenshteinDistanceCalculator : ILevenshteinDistanceCalculator
{
    public int Calculate(string source, string target)
    {
        source ??= string.Empty;
        target ??= string.Empty;

        if (source.Length == 0)
        {
            return target.Length;
        }

        if (target.Length == 0)
        {
            return source.Length;
        }

        var matrix = new int[source.Length + 1, target.Length + 1];

        for (var i = 0; i <= source.Length; i++)
        {
            matrix[i, 0] = i;
        }

        for (var j = 0; j <= target.Length; j++)
        {
            matrix[0, j] = j;
        }

        for (var i = 1; i <= source.Length; i++)
        {
            for (var j = 1; j <= target.Length; j++)
            {
                var cost = source[i - 1] == target[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[source.Length, target.Length];
    }
}
