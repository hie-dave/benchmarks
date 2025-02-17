namespace Dave.Benchmarks.Core.Models.Entities;

/// <summary>
/// Represents a collection of site-level model runs performed with the same model version.
/// </summary>
public class SiteRunDataset : PredictionDataset
{
    /// <summary>
    /// The individual site runs that make up this dataset.
    /// </summary>
    public ICollection<SiteRun> Sites { get; set; } = new List<SiteRun>();
}
