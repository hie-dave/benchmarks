using System.Collections.Generic;
using Dave.Benchmarks.Core.Models.Entities;

namespace Dave.Benchmarks.Core.Models;

/// <summary>
/// Constants used throughout the model.
/// </summary>
public static class ModelConstants
{
    /// <summary>
    /// Longitude layer name.
    /// </summary>
    public const string LonLayer = "Lon";

    /// <summary>
    /// Latitude layer name.
    /// </summary>
    public const string LatLayer = "Lat";

    /// <summary>
    /// Year layer name.
    /// </summary>
    public const string YearLayer = "Year";

    /// <summary>
    /// Day layer name.
    /// </summary>
    public const string DayLayer = "Day";

    /// <summary>
    /// Stand layer name.
    /// </summary>
    public const string StandLayer = "stand";

    /// <summary>
    /// Patch layer name.
    /// </summary>
    public const string PatchLayer = "patch";

    /// <summary>
    /// Individual layer name.
    /// </summary>
    public const string IndivLayer = "indiv";

    /// <summary>
    /// Name of the columns used in the monthly output files.
    /// </summary>
    public static readonly string[] MonthCols = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "Total"];

    /// <summary>
    /// Gets the metadata layers for a given aggregation level and temporal resolution.
    /// </summary>
    public static string[] GetMetadataLayers(AggregationLevel level, TemporalResolution resolution)
    {
        List<string> layers = new() { LonLayer, LatLayer, YearLayer };
        
        // Allow for a day column in annual outputs, because apparently some
        // annual outputs like to use that.
        if (resolution == TemporalResolution.Daily || resolution == TemporalResolution.Annual)
            layers.Add(DayLayer);

        switch (level)
        {
            case AggregationLevel.Stand:
                layers.Add(StandLayer);
                break;
            case AggregationLevel.Patch:
                layers.Add(StandLayer);
                layers.Add(PatchLayer);
                break;
            case AggregationLevel.Individual:
                layers.Add(StandLayer);
                layers.Add(PatchLayer);
                layers.Add(IndivLayer);
                break;
            case AggregationLevel.Gridcell:
                // No additional layers needed
                break;
        }

        return layers.ToArray();
    }
}
