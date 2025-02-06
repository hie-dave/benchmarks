namespace Dave.Benchmarks.CLI.Models;

/// <summary>
/// Constants from the model.
/// </summary>
public static class ModelConstants
{
    /// <summary>
    /// Name of the longitude column in output files.
    /// </summary>
    public const string LonLayer = "Lon";

    /// <summary>
    /// Name of the latitude column in output files.
    /// </summary>
    public const string LatLayer = "Lat";

    /// <summary>
    /// Name of the year column in output files.
    /// </summary>
    public const string YearLayer = "Year";

    /// <summary>
    /// Name of the day column in output files.
    /// </summary>
    public const string DayLayer = "Day";

    /// <summary>
    /// Name of the patch column in output files.
    /// </summary>
    public const string PatchLayer = "patch";

    /// <summary>
    /// Name of the stand column in output files.
    /// </summary>
    public const string StandLayer = "stand";

    /// <summary>
    /// Name of the individual column in output files.
    /// </summary>
    public const string IndivLayer = "indiv";

    /// <summary>
    /// Name of the total column in trunk output files.
    /// </summary>
    public const string TrunkTotalLayer = "Total";

    /// <summary>
    /// Name of the total column in dave output files.
    /// </summary>
    public const string DaveTotalLayer = "total";

    /// <summary>
    /// Name of the mean column in dave output files.
    /// </summary>
    public const string DaveMeanLayer = "mean";

    /// <summary>
    /// Metadata layers which are common to all dave output files.
    /// </summary>
    public static readonly string[] TrunkMetadataLayers = { LonLayer, LatLayer, YearLayer, DayLayer };

    /// <summary>
    /// Metadata layers which are common to all dave output files.
    /// </summary>
    public static readonly string[] DaveMetadataLayers = { LonLayer, LatLayer, YearLayer, DayLayer, PatchLayer, StandLayer };

    /// <summary>
    /// Metadata layers which are common to all dave indiv-level output files.
    /// </summary>
    public static readonly string[] DaveIndivMetadataLayers = { LonLayer, LatLayer, YearLayer, DayLayer, PatchLayer, StandLayer, IndivLayer };
}
