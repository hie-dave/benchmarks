using System;

namespace Dave.Benchmarks.Core.Models.Importer;

/// <summary>
/// Request model for importing model prediction data.
/// </summary>
public sealed class ImportModelPredictionRequest
{
    public required int DatasetId { get; init; }
    public required Quantity Quantity { get; init; }
}
