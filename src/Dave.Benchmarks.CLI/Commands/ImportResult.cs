namespace Dave.Benchmarks.CLI.Commands;

public sealed record ImportResult(int GroupId, IReadOnlyList<int> DatasetIds);
