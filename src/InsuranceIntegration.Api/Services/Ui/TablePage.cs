namespace InsuranceIntegration.Api.Services.Ui;

public sealed class TablePage
{
    public string TableName { get; init; } = string.Empty;

    public IReadOnlyList<string> Columns { get; init; } = [];

    public IReadOnlyList<IReadOnlyList<string?>> Rows { get; init; } = [];

    public int TotalRows { get; init; }

    public int Skip { get; init; }

    public int Take { get; init; }
}
