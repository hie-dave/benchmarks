namespace Dave.Benchmarks.Core.Services.Evaluation;

/// <summary>
/// Executes evaluation runs persisted in the database.
/// </summary>
public interface IEvaluationEngine
{
    /// <summary>
    /// Execute the evaluation run with the provided ID.
    /// </summary>
    Task ExecuteAsync(int evaluationRunId, CancellationToken cancellationToken = default);
}
