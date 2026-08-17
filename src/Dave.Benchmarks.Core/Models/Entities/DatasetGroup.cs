namespace Dave.Benchmarks.Core.Models.Entities;

/// <summary>
/// Represents a logical grouping of related datasets, typically from the same model run or experiment.
/// </summary>
public class DatasetGroup
{
    /// <summary>
    /// Unique identifier for this group.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Name of this group.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of this group.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The time this group was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Indicates whether this group is complete and should not accept new datasets.
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>Whether this observation release is currently selected for evaluation.</summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Uniquely identifies an active observation collection. This is null for
    /// inactive releases, allowing the database to enforce one active version.
    /// </summary>
    public string? ActiveCollectionKey { get; set; }

    /// <summary>The kind of datasets this group may contain.</summary>
    public DatasetGroupKind Kind { get; set; }

    /// <summary>Stable source identifier for a versioned observation collection.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Version of this observation release.</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// The datasets that belong to this group.
    /// </summary>
    public ICollection<Dataset> Datasets { get; set; } = new List<Dataset>();

    /// <summary>
    /// Additional metadata about this group stored as a JSON document.
    /// This can include things like model version, climate scenario, etc.
    /// </summary>
    public string Metadata { get; set; } = "{}";
}
