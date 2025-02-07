namespace Dave.Benchmarks.Core.Models.Entities;

/// <summary>
/// Defines the temporal resolution of output data.
/// </summary>
public enum TemporalResolution
{
    /// <summary>
    /// Daily output data, includes Year and Day columns.
    /// </summary>
    Daily,

    /// <summary>
    /// Monthly output data, includes Year column, and one data column per
    /// month.
    /// </summary>
    Monthly,

    /// <summary>
    /// Annual output data, includes only Year column.
    /// </summary>
    Annual
}
