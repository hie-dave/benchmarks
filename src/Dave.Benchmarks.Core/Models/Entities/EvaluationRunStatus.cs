namespace Dave.Benchmarks.Core.Models.Entities;

/// <summary>
/// Status of an evaluation run lifecycle.
/// </summary>
public enum EvaluationRunStatus
{
    Pending = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3
}
