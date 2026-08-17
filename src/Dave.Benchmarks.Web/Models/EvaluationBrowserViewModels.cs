using Dave.Benchmarks.Core.Models.Entities;

namespace Dave.Benchmarks.Web.Models;

public class EvaluationRunIndexViewModel
{
    public IReadOnlyList<MergeRequestSummaryViewModel> MergeRequests { get; init; } = [];
    public string? SelectedProjectId { get; init; }
    public string? SelectedMergeRequestId { get; init; }
    public IReadOnlyList<SubmissionRunsViewModel> Submissions { get; init; } = [];
}

public class MergeRequestSummaryViewModel
{
    public string ProjectId { get; init; } = string.Empty;
    public string MergeRequestId { get; init; } = string.Empty;
    public string SourceBranch { get; init; } = string.Empty;
    public string TargetBranch { get; init; } = string.Empty;
    public DateTime LastTestedAt { get; init; }
    public int SubmissionCount { get; init; }
    public int RunCount { get; init; }
    public bool? LatestPassed { get; init; }
    public EvaluationRunStatus? LatestStatus { get; init; }
}

public class SubmissionRunsViewModel
{
    public int SubmissionId { get; init; }
    public string PipelineId { get; init; } = string.Empty;
    public string CommitSha { get; init; } = string.Empty;
    public string? CommitMessage { get; init; }
    public string BenchmarkName { get; init; } = string.Empty;
    public BenchmarkSubmissionStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public IReadOnlyList<EvaluationRunSummaryViewModel> Runs { get; init; } = [];
}

public class EvaluationRunSummaryViewModel
{
    public int Id { get; init; }
    public EvaluationRunStatus Status { get; init; }
    public bool? Passed { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}

public class EvaluationRunDetailsViewModel
{
    public int Id { get; init; }
    public EvaluationRunStatus Status { get; init; }
    public bool? Passed { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? ErrorMessage { get; init; }
    public int SubmissionId { get; init; }
    public string ProjectId { get; init; } = string.Empty;
    public string MergeRequestId { get; init; } = string.Empty;
    public string PipelineId { get; init; } = string.Empty;
    public string CommitSha { get; init; } = string.Empty;
    public string? CommitMessage { get; init; }
    public string SourceBranch { get; init; } = string.Empty;
    public string TargetBranch { get; init; } = string.Empty;
    public string BenchmarkName { get; init; } = string.Empty;
    public IReadOnlyList<EvaluationDatasetDetailsViewModel> Datasets { get; init; } = [];
}

public class EvaluationDatasetDetailsViewModel
{
    public int CandidateDatasetId { get; init; }
    public string CandidateName { get; init; } = string.Empty;
    public string SimulationId { get; init; } = string.Empty;
    public string? BaselineName { get; init; }
    public EvaluationRunStatus Status { get; init; }
    public bool? Passed { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<EvaluationResultDetailsViewModel> Results { get; init; } = [];
}

public class EvaluationResultDetailsViewModel
{
    public string CandidateVariable { get; init; } = string.Empty;
    public string CandidateLayer { get; init; } = string.Empty;
    public string ObservationDataset { get; init; } = string.Empty;
    public string ObservationVariable { get; init; } = string.Empty;
    public string ObservationLayer { get; init; } = string.Empty;
    public IReadOnlyList<EvaluationMetricViewModel> Metrics { get; init; } = [];
}

public class EvaluationMetricViewModel
{
    public string Type { get; init; } = string.Empty;
    public double Value { get; init; }
}
